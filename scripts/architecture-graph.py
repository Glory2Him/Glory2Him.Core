#!/usr/bin/env python3
"""Draw The Standard's layer graph from the C# code, and check the layer rules.

    python3 scripts/architecture-graph.py                 # Mermaid graph to stdout
    python3 scripts/architecture-graph.py --check         # also list rule violations; exit 1 if any
    python3 scripts/architecture-graph.py --deviations Documentation/deviations.txt
    python3 scripts/architecture-graph.py --utility-brokers scripts/utility-brokers.txt

On Windows, run it as `python` where `python3` is the Microsoft Store stub.

It reads every *.cs file outside test projects, bin/, obj/, node_modules/, .git/,
.worktrees/ (task copies of the source) and .claude/ (the vendored skills' examples), finds
classes that are brokers, services or controllers by The Standard's naming, and merges the
interfaces their constructors take across every part of a partial class. It is a
heuristic — it follows names, not a compiler — so it can miss a dependency injected some
other way. What it checks:

  * no layer skips a level, except an orchestration depending on foundation services;
  * an orchestration's service dependencies are one kind: all processing or all
    foundation, never a mix;
  * a utility broker is exempt from the same-kind and skip rules: any service may hold
    one. The utility brokers are the classes named in scripts/utility-brokers.txt, one
    `<ClassName> <kind>` per line, the kind one of logging, datetime, identifier,
    identity, events or access; any other kind is an error;
  * an orchestration — or a coordination, management or aggregation above it — holds no
    other broker: a storage broker first, and any other broker that hands an entity's
    data back;
  * an orchestration has two or three service dependencies (Florance), unless the service
    is named in the deviations file. No broker counts toward the two-to-three;
  * an exposer (controller) has exactly one dependency;
  * a broker depends on no service;
  * a storage broker composes no query operator (Where, Select, OrderBy and the like) and
    calls no terminal operator with a lambda predicate. The query-shaping function the
    foundation service passes down is invoked, not composed, so applying it is never
    flagged. Comments, strings and files whose name holds "Configuration" are ignored. A
    predicate passed by variable, and a query run through the DbContext, are not detected:
    those are left to judgement.

The deviations file, if given, lists one service class name per line that has an approved
deviation recorded against it — in its user story issue, or in a decision record for a
service that has none (scripts/list-deviations.sh writes it).
"""
import argparse
import re
import sys
from pathlib import Path

LAYERS = ["Broker", "Foundation", "Processing", "Orchestration", "Coordination",
          "Management", "Aggregation", "Exposer"]
RANK = {name: i for i, name in enumerate(LAYERS)}

# The layers that orchestrate: none of them may hold a broker other than a utility broker.
ORCHESTRATING = {"Orchestration", "Coordination", "Management", "Aggregation"}

UTILITY_KINDS = ("logging", "datetime", "identifier", "identity", "events", "access")
DEFAULT_UTILITY_BROKERS = Path(__file__).resolve().parent / "utility-brokers.txt"

CLASS_RE = re.compile(r"\b(?:public|internal)\s+(?:sealed\s+|partial\s+)*class\s+(\w+)")
CTOR_RE = r"\b{name}\s*\(([^)]*)\)"
PARAM_RE = re.compile(r"\b(I\w+)\s+\w+")

# A query operator composed in a storage broker. Join is left out: string.Join is common.
QUERY_OPERATOR_RE = re.compile(
    r"\.(Where|Select|SelectMany|OrderBy|OrderByDescending|ThenBy|ThenByDescending|"
    r"GroupBy|Skip|Take|Distinct)\s*(?:<[^<>()]*>)?\s*\(")
# A terminal operator handed a lambda predicate: .AnyAsync(x => ...), .First((a, b) => ...).
PREDICATE_TERMINAL_RE = re.compile(
    r"\.(First|FirstOrDefault|Single|SingleOrDefault|Last|LastOrDefault|Any|All|Count|"
    r"LongCount)(?:Async)?\s*\(\s*(?:\w+|\([^()]*\))\s*=>")


class UtilityBrokerError(Exception):
    pass


def read_utility_brokers(path: Path) -> dict[str, str]:
    """Reads `<ClassName> <kind>` lines, skipping blanks and # comments."""
    brokers: dict[str, str] = {}
    for number, raw in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
        line = raw.split("#", 1)[0].strip()
        if not line:
            continue
        fields = line.split()
        if len(fields) != 2:
            raise UtilityBrokerError(
                f"{path}:{number}: expected '<ClassName> <kind>', got '{raw.strip()}'")
        name, kind = fields
        if kind not in UTILITY_KINDS:
            raise UtilityBrokerError(
                f"{path}:{number}: unknown utility broker kind '{kind}' for {name} "
                f"(expected one of: {', '.join(UTILITY_KINDS)})")
        brokers[name] = kind
    return brokers


def layer_of(class_name: str, path: Path) -> str | None:
    parts = {p.lower() for p in path.parts}
    if class_name.endswith("Broker"):
        return "Broker"
    if class_name.endswith("Controller"):
        return "Exposer"
    if not class_name.endswith("Service"):
        return None
    for layer, folder in [("Foundation", "foundations"), ("Processing", "processings"),
                          ("Orchestration", "orchestrations"), ("Coordination", "coordinations"),
                          ("Management", "managements"), ("Aggregation", "aggregations")]:
        if folder in parts or class_name.endswith(f"{layer}Service"):
            return layer
    return "Foundation"


def is_storage_broker(name: str) -> bool:
    return name.endswith("Broker") and "Storage" in name


def strip_comments_and_strings(text: str) -> str:
    """Blanks out C# comments and string and char literals, keeping every newline, so a
    match's line number is still right and nothing inside them is mistaken for code."""
    out: list[str] = []
    i, n = 0, len(text)

    def blank(segment: str) -> str:
        return "".join("\n" if c == "\n" else " " for c in segment)

    while i < n:
        c = text[i]
        two = text[i:i + 2]
        if two == "//":
            end = text.find("\n", i)
            end = n if end == -1 else end
            out.append(blank(text[i:end]))
            i = end
        elif two == "/*":
            end = text.find("*/", i + 2)
            end = n if end == -1 else end + 2
            out.append(blank(text[i:end]))
            i = end
        elif c == '"' or (c in "@$" and text[i + 1:i + 2] in ('"', "@", "$")):
            j = i
            verbatim = False
            while j < n and text[j] in "@$":
                verbatim = verbatim or text[j] == "@"
                j += 1
            if j >= n or text[j] != '"':
                out.append(c)
                i += 1
                continue
            j += 1
            while j < n:
                if verbatim and text[j] == '"' and text[j + 1:j + 2] == '"':
                    j += 2
                elif not verbatim and text[j] == "\\":
                    j += 2
                elif text[j] == '"':
                    j += 1
                    break
                else:
                    j += 1
            out.append(blank(text[i:j]))
            i = j
        elif c == "'":
            match = re.match(r"'(?:\\.|[^'\\])'", text[i:])
            if match:
                out.append(blank(match.group(0)))
                i += len(match.group(0))
            else:
                out.append(c)
                i += 1
        else:
            out.append(c)
            i += 1
    return "".join(out)


def query_composition(text: str, relative: Path) -> list[str]:
    """Where a storage broker file composes a query operator or a lambda predicate."""
    code = strip_comments_and_strings(text)
    found = []
    for regex in (QUERY_OPERATOR_RE, PREDICATE_TERMINAL_RE):
        for match in regex.finditer(code):
            line = code.count("\n", 0, match.start()) + 1
            found.append(f"{match.group(1)} at {relative.as_posix()}:{line}")
    return found


# Not application code: build output, dependencies, task worktrees (copies of the source)
# and the vendored skills, whose C# examples are deliberately incomplete or wrong.
SKIP_DIRS = {"bin", "obj", "node_modules", ".git", ".worktrees", ".claude"}


def scan(root: Path):
    classes: dict[str, tuple[str, list[str]]] = {}
    queries: dict[str, list[str]] = {}
    for path in root.rglob("*.cs"):
        relative = path.relative_to(root)
        if any(p in SKIP_DIRS for p in relative.parts) or "Tests" in relative.as_posix():
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        for name in CLASS_RE.findall(text):
            layer = layer_of(name, relative)
            if not layer:
                continue
            deps: list[str] = []
            for args in re.findall(CTOR_RE.format(name=re.escape(name)), text):
                deps += PARAM_RE.findall(args)
            # A partial class spreads over several files (StudentService.Validations.cs);
            # its dependencies are the union across them, whatever order they are read in.
            known = classes.get(name, (layer, []))[1]
            classes[name] = (layer, sorted(set(known) | set(deps)))
            if is_storage_broker(name) and "Configuration" not in path.name:
                queries.setdefault(name, [])
                for hit in query_composition(text, relative):
                    if hit not in queries[name]:
                        queries[name].append(hit)
    return classes, queries


def resolve(dep: str, classes) -> str | None:
    impl = dep[1:] if dep.startswith("I") else dep
    return impl if impl in classes else None


def check(classes, deviations: set[str], utility: dict[str, str],
          queries: dict[str, list[str]] | None = None) -> list[str]:
    problems = []
    for name, (layer, deps) in sorted(classes.items()):
        targets = [(t, classes[t][0]) for t in (resolve(d, classes) for d in deps) if t]
        service_deps = [t for t, l in targets if l != "Broker"]
        kinds = {l for _, l in targets if l != "Broker"}
        if layer == "Broker" and service_deps:
            problems.append(f"{name}: a broker depends on {', '.join(service_deps)}")
        if layer == "Exposer" and len(targets) != 1:
            problems.append(f"{name}: an exposer has {len(targets)} dependencies, not one")
        if layer in ORCHESTRATING:
            held = [t for t, l in targets if l == "Broker" and t not in utility]
            # A storage broker first: it skips every layer beneath at once.
            held.sort(key=lambda t: (not is_storage_broker(t), t))
            for broker in held:
                what = "a storage broker" if is_storage_broker(broker) else "a broker that is not a utility broker"
                problems.append(f"{name}: an {layer.lower()} holds {what} ({broker})")
        if layer == "Orchestration":
            if {"Processing", "Foundation"} <= kinds:
                problems.append(f"{name}: mixed processing and foundation dependencies")
            if not 2 <= len(service_deps) <= 3 and name not in deviations:
                problems.append(f"{name}: {len(service_deps)} service dependencies (Florance: 2-3) with no recorded deviation")
        for target, tlayer in targets:
            if target in utility:
                continue
            # A non-utility broker on an orchestrating service is reported above.
            if tlayer == "Broker" and layer in ORCHESTRATING:
                continue
            gap = RANK[layer] - RANK[tlayer]
            # The skip rule is checked from Orchestration down. An exposer may front a
            # service at any level, and the layers above Orchestration vary by system.
            allowed = gap == 1 or (layer == "Orchestration" and tlayer == "Foundation") \
                or RANK[layer] > RANK["Orchestration"]
            if gap > 1 and not allowed:
                problems.append(f"{name} ({layer}) skips to {target} ({tlayer})")
            if gap <= 0 and layer != "Exposer":
                problems.append(f"{name} ({layer}) depends sideways or upwards on {target} ({tlayer})")
    for name, hits in sorted((queries or {}).items()):
        for hit in hits:
            # ASCII only: a Windows console may not print anything wider.
            problems.append(f"{name}: a storage broker composes a query ({hit}); "
                            "move the condition into the foundation service as a query-shaping function")
    return problems


def mermaid(classes) -> str:
    lines = ["graph TD"]
    for layer in reversed(LAYERS):
        members = [n for n, (l, _) in sorted(classes.items()) if l == layer]
        if members:
            lines.append(f"  subgraph {layer}")
            lines += [f"    {n}" for n in members]
            lines.append("  end")
    for name, (_, deps) in sorted(classes.items()):
        for d in deps:
            t = resolve(d, classes)
            if t:
                lines.append(f"  {name} --> {t}")
    return "\n".join(lines)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=".")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--deviations")
    ap.add_argument("--utility-brokers", default=str(DEFAULT_UTILITY_BROKERS))
    args = ap.parse_args()
    deviations = set()
    if args.deviations and Path(args.deviations).exists():
        deviations = {l.strip() for l in Path(args.deviations).read_text().splitlines() if l.strip()}
    try:
        utility = read_utility_brokers(Path(args.utility_brokers))
    except (OSError, UtilityBrokerError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 2
    classes, queries = scan(Path(args.root))
    print(mermaid(classes))
    if args.check:
        problems = check(classes, deviations, utility, queries)
        for p in problems:
            print(f"VIOLATION: {p}", file=sys.stderr)
        return 1 if problems else 0
    return 0


if __name__ == "__main__":
    sys.exit(main())
