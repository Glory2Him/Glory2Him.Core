"""Tally what each model budget actually bought.

Joins merged pull requests to the `Model - Effort` label on the issue they close,
and to the `Build cost:` comment /build-task posts on that issue, then reports
cost and rework per pull request within comparable size bands. Answers one
question and no other: is a cheaper effort costing us more fix rounds?

    python Documentation/ModelBudget/tally.py            # fetch and report
    python Documentation/ModelBudget/tally.py --cached   # reuse the last fetch

Requires `gh`, authenticated against the repository. Reads nothing but GitHub;
writes nothing but its cache. The repository is whatever `origin` points at, so
the same file works unchanged in the template and in anything generated from it.

Three caveats the output cannot carry on its own:

  * The build driver starts each task's session at the effort its label names,
    so for a task built by /build-feature the label is what ran, and its
    `Build cost:` comment records the dollars, the developer runs and the QA
    rounds. Older issues recorded what ran under `## Model usage` in the body;
    that is still read. A row with neither is the label alone — an intention,
    not a record — and the output says how many rows are.
  * Assignment is not random. Harder tasks were given larger efforts on purpose,
    so the raw split reflects difficulty, not effort. The size bands are a
    partial control and churn is a poor proxy for difficulty.
  * On a subscription the dollar figure is the API-equivalent cost, for
    comparison, not a bill.

A pull request reviewed more than once carries one QA verdict comment per round.
They are append-only, so the earlier ones are superseded rather than corrected —
the highest round is the live verdict and the ones before it are history.
`medBlocking` reports that verdict, never a sum across rounds, and a pull request
with no verdict at all is silent rather than clean: it is left out of the finding
columns instead of counted as a zero.
"""

import argparse
import json
import os
import re
import statistics
import random
import subprocess
import sys
import tempfile
from datetime import datetime

CACHE_ROOT = os.path.join(tempfile.gettempdir(), "g2h-model-tally")

# A fix round is a burst of commits landing after the pull request opened. QA runs
# after the pull request is opened, so post-open commits are the rework signal.
ROUND_GAP_SECONDS = 2 * 60 * 60

MODELS = ["Opus 5", "Opus 5.5", "Sonnet 5", "Fable 5"]

# .github/workflows/prLinter.yml accepts all of these to satisfy requireIssueOrTask.
CLOSES = re.compile(r"\b(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s*:?\s*#(\d+)", re.I)

# Legacy: what developers appended to the issue body under `## Model usage` before
# the build driver recorded Build cost: comments, one line per pull request:
#   "- PR #42 — Opus 5.5 - High"
# The label is the decision and stays the decision, so a comparison of what the
# models actually cost has to read this and never the label. Any of hyphen, en
# dash or em dash separates the two halves, because all three get typed.
USAGE = re.compile(r"^\s*[-*]\s*PR\s*#(\d+)\s*[-–—]\s*(.+?)\s*$", re.M)

# What /build-task posts on the task, and /tweak on its PR, once per build:
#   "Build cost: $3.42 (PR #42, 2026-10-15, developer opus/high, qa opus/max, 2 developer runs, QA PASS in 2 rounds, round 1 BLOCKING 1)"
# A re-run after a halt posts another; the one naming the merged PR is the record.
COST = re.compile(r"^(?:Build|Tweak) cost:\s*\$?([0-9.]+|n/a)\b([^\n]*)$", re.M | re.I)
COST_PR = re.compile(r"\bPR\s*#(\d+)")
COST_RUNS = re.compile(r"\b(\d+)\s+developer runs?\b", re.I)
COST_QA = re.compile(r"\bQA\s+(PASS|FAIL)\s+in\s+(\d+)\s+rounds?\b", re.I)
COST_R1 = re.compile(r"\bround 1 BLOCKING\s+(\d+)\b", re.I)

# Every design prefix .github/workflows/prLinter.yml accepts.
DESIGN_TITLE = re.compile(r"(?:(?:MINOR|MEDIUM|MAJOR) )?DESIGN:", re.I)

# A feature PR closes every task of its feature and a release PR builds nothing;
# counted here they would repeat each task's rework under one large row.
ROLLUP_TITLE = re.compile(r"(?:(?:MAJOR) )?FEATURES:|RELEASES:", re.I)

# The header qa.md asks for, one per review pass:
#   "QA round 2: FAIL - BLOCKING 3, ADVISORY 2 - MERGE READY: NO - at 4f2a9c1"
# A design PR carries the same header for QA's task-review rounds; main() leaves
# design PRs out, because those rounds judge the planner, not an implementation.
#
# Anchored to a single line on purpose. A pull request reviewed more than once
# carries one header per round and the later ones supersede the earlier, so this
# must never match a PASS on one round against a count from another. `[^\n]`
# rather than `.` is what stops that stitching.
VERDICT = re.compile(
    r"\bQA(?:\s+round\s+(\d+))?\s*:\s*(PASS|FAIL)\b[^\n]*?BLOCKING\s+(\d+)[^\n]*?ADVISORY\s+(\d+)",
    re.I)

PR_QUERY = """
query($endCursor: String) {
  repository(owner: "%s", name: "%s") {
    pullRequests(states: MERGED, first: 50, orderBy: {field: CREATED_AT, direction: DESC}, after: $endCursor) {
      pageInfo { hasNextPage endCursor }
      nodes {
        number title body createdAt mergedAt additions deletions changedFiles
        commits(first: 100) { totalCount nodes { commit { committedDate messageHeadline } } }
        comments(first: 50) { nodes { createdAt bodyText } }
        reviews(first: 50) { nodes { createdAt bodyText } }
      }
    }
  }
}
"""


def repository():
    """owner/name of the repository this checkout points at."""
    remote = run(["git", "remote", "get-url", "origin"]).strip()
    found = re.search(r"[:/]([^/:]+)/([^/]+?)(?:\.git)?$", remote)
    if not found:
        sys.exit("could not read an owner/repo out of origin: " + remote)
    return found.group(1), found.group(2)


def run(args):
    out = subprocess.run(args, capture_output=True, text=True, encoding="utf-8")
    if out.returncode != 0:
        sys.exit("gh failed: " + (out.stderr or "").strip())
    return out.stdout


def concatenated(text):
    """gh --paginate emits one JSON document per page, back to back."""
    decoder, i, pages = json.JSONDecoder(), 0, []
    while i < len(text):
        while i < len(text) and text[i] in " \r\n\t\x00":
            i += 1
        if i >= len(text):
            break
        page, i = decoder.raw_decode(text, i)
        pages.append(page)
    return pages


def fetch(cached):
    # Cache per repository, so running this in the template and in Core does not
    # have one overwrite the other's fetch.
    cache = os.path.join(CACHE_ROOT, "-".join(repository()))
    os.makedirs(cache, exist_ok=True)
    prs_path = os.path.join(cache, "prs.json")
    issues_path = os.path.join(cache, "issues.json")
    comments_path = os.path.join(cache, "comments.json")

    if not (cached and all(os.path.exists(p) for p in (prs_path, issues_path, comments_path))):
        # --paginate substitutes the cursor only into a variable named $endCursor.
        query = PR_QUERY % repository()
        raw = run(["gh", "api", "graphql", "--paginate", "-f", "query=" + query])
        with open(prs_path, "w", encoding="utf-8") as handle:
            handle.write(raw)
        # REST, like everything else in the workflow. The issues endpoint lists
        # pull requests too; they carry no Model - Effort label, so they drop out.
        owner, name = repository()
        raw = run(["gh", "api", "--paginate",
                   "repos/%s/%s/issues?state=all&per_page=100" % (owner, name)])
        with open(issues_path, "w", encoding="utf-8") as handle:
            handle.write(raw)
        raw = run(["gh", "api", "--paginate",
                   "repos/%s/%s/issues/comments?per_page=100" % (owner, name)])
        with open(comments_path, "w", encoding="utf-8") as handle:
            handle.write(raw)

    with open(prs_path, encoding="utf-8") as handle:
        pages = concatenated(handle.read())
    prs = {}
    for page in pages:
        for node in page["data"]["repository"]["pullRequests"]["nodes"]:
            prs[node["number"]] = node

    with open(issues_path, encoding="utf-8") as handle:
        issues = [i for page in concatenated(handle.read()) for i in page]
    with open(comments_path, encoding="utf-8") as handle:
        comments = [c for page in concatenated(handle.read()) for c in page]

    # Keyed by the pull request the comment names. A comment that names none
    # (an older /tweak) is keyed by the issue or PR it was posted on.
    costs = {}
    for comment in sorted(comments, key=lambda c: c["created_at"]):
        for found in COST.finditer(comment.get("body") or ""):
            rest = found.group(2)
            pr, runs = COST_PR.search(rest), COST_RUNS.search(rest)
            qa, r1 = COST_QA.search(rest), COST_R1.search(rest)
            number = pr.group(1) if pr else comment["issue_url"].rsplit("/", 1)[1]
            costs[int(number)] = dict(
                dollars=None if found.group(1).lower() == "n/a" else float(found.group(1)),
                developer_runs=int(runs.group(1)) if runs else None,
                qa_verdict=qa.group(1).upper() if qa else None,
                qa_rounds=int(qa.group(2)) if qa else None,
                round1_blocking=int(r1.group(1)) if r1 else None,
                issue=int(comment["issue_url"].rsplit("/", 1)[1]))

    # The label is what was decided before the code was read; `## Model usage` is
    # what happened. Actuals are keyed by pull request rather than by issue,
    # because one issue can take more than one attempt and the attempts need not
    # have run under the same budget.
    decided, actual = {}, {}
    for issue in issues:
        if "pull_request" in issue:
            continue
        hit = [l["name"] for l in issue["labels"]
               if any(l["name"].startswith(m + " - ") for m in MODELS)]
        if hit:
            decided[issue["number"]] = hit[0]
        for number, ran in USAGE.findall(issue.get("body") or ""):
            if any(ran.startswith(m + " - ") for m in MODELS):
                actual[int(number)] = ran
    # A Build cost: comment sits on a task whose label the driver ran at, so the
    # label is what ran for that pull request.
    for number, cost in costs.items():
        if number not in actual and cost["issue"] in decided:
            actual[number] = decided[cost["issue"]]
    return list(prs.values()), decided, actual, costs


def moment(text):
    return datetime.fromisoformat(text.replace("Z", "+00:00"))


def measure(pr, label, cost=None):
    opened = moment(pr["createdAt"])
    commits = sorted(moment(c["commit"]["committedDate"]) for c in pr["commits"]["nodes"])
    post = [c for c in commits if c > opened]

    rounds, previous = 0, None
    for when in post:
        if previous is None or (when - previous).total_seconds() > ROUND_GAP_SECONDS:
            rounds += 1
        previous = when

    # One verdict per review pass, and only the last round is live. Read each
    # comment on its own rather than joining them: a pull request that failed
    # round 1 and passed round 3 passed, and a pattern spanning two comments can
    # match a verdict that was never written. Sorted by timestamp because
    # comments and reviews are two lists interleaved in time, which the fallback
    # below depends on.
    verdicts = []
    for node in list(pr["comments"]["nodes"]) + list(pr["reviews"]["nodes"]):
        found = VERDICT.search(node["bodyText"] or "")
        if found:
            verdicts.append((moment(node["createdAt"]), found))
    verdicts.sort(key=lambda pair: pair[0])

    # Take the highest round, not the latest comment. Those agree in the ordinary
    # case and diverge exactly when comments land out of order, which is the case
    # the round number was added to settle — so reading the timestamp instead
    # would ignore the field that exists to answer this. Headers written before
    # the round number was required carry none; fall back to the timestamp only
    # for those.
    numbered = [(int(found.group(1)), when, found)
                for when, found in verdicts if found.group(1)]
    if numbered:
        live = max(numbered, key=lambda row: (row[0], row[1]))[2]
        # Distinct rounds, so a reposted header does not inflate the count.
        qa_round_count = len({number for number, _, _ in numbered})
    else:
        live = verdicts[-1][1] if verdicts else None
        qa_round_count = len(verdicts)

    return dict(
        pr=pr["number"],
        label=label,
        model=label.split(" - ")[0],
        churn=pr["additions"] + pr["deletions"],
        files=pr["changedFiles"],
        commits=len(commits),
        post=len(post),
        rounds=rounds,
        # Every commit dated after the pull request opened usually means a rebase
        # rewrote the dates, which inflates every post-open number for that row.
        rebased=bool(commits) and len(post) == len(commits),
        verdict=live.group(2).upper() if live else None,
        blocking=int(live.group(3)) if live else None,
        advisory=int(live.group(4)) if live else None,
        # How many times QA had to look at it, counted in distinct rounds. Once
        # verdicts are landing this is a cleaner rework signal than the commit
        # bursts above, which only guess at where a round started.
        qa_rounds=qa_round_count,
        dollars=cost["dollars"] if cost else None,
        developer_runs=cost["developer_runs"] if cost else None,
        round1_blocking=cost["round1_blocking"] if cost else None,
    )


def median(values):
    return round(statistics.median(values), 2) if values else None


def mean(values):
    return round(statistics.mean(values), 2) if values else None


def table(title, groups):
    print("\n" + title)
    head = ("group", "n", "medChurn", "medFiles", "medCommits", "medRounds",
            "meanRounds", "%reworked", "medBlocking", "medQaRounds", "medR1Block",
            "medCost$", "meanCost$")
    print("{:<22}{:>4}{:>10}{:>9}{:>11}{:>10}{:>11}{:>11}{:>12}{:>12}{:>11}{:>10}{:>11}".format(*head))
    for name, rows in groups:
        if not rows:
            continue
        # Only pull requests carrying a verdict say anything about findings; the
        # rest are silent, not clean, and must not be averaged in as zeroes.
        judged = [r for r in rows if r["verdict"]]
        r1 = [r["round1_blocking"] for r in rows if r["round1_blocking"] is not None]
        spent = [r["dollars"] for r in rows if r["dollars"] is not None]
        print("{:<22}{:>4}{:>10}{:>9}{:>11}{:>10}{:>11}{:>10}%{:>12}{:>12}{:>11}{:>10}{:>11}".format(
            name, len(rows),
            median([r["churn"] for r in rows]), median([r["files"] for r in rows]),
            median([r["commits"] for r in rows]), median([r["rounds"] for r in rows]),
            mean([r["rounds"] for r in rows]),
            round(100 * sum(1 for r in rows if r["rounds"]) / len(rows)),
            median([r["blocking"] for r in judged]) if judged else "-",
            median([r["qa_rounds"] for r in judged]) if judged else "-",
            median(r1) if r1 else "-",
            median(spent) if spent else "-",
            mean(spent) if spent else "-"))


def bootstrap(treatment, control, draws=20000, seed=0):
    """95% interval for the difference in mean rework, treatment minus control."""
    if not treatment or not control:
        return None
    random.seed(seed)
    diffs = sorted(statistics.mean(random.choices(treatment, k=len(treatment)))
                   - statistics.mean(random.choices(control, k=len(control)))
                   for _ in range(draws))
    edge = int(draws * 0.025)
    return diffs[edge], diffs[-edge - 1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--cached", action="store_true", help="reuse the last fetch")
    parser.add_argument("--band", default="200-2000",
                        help="churn band for the size-matched comparison")
    # Labels, not models: every label now names Opus 5.5 and only the effort varies.
    parser.add_argument("--treatment", default="Opus 5.5 - Medium")
    parser.add_argument("--control", default="Opus 5.5 - High")
    args = parser.parse_args()

    prs, decided, actual, costs = fetch(args.cached)
    # A design PR's QA rounds review the planner's design and tasks. Counted here
    # they would mix planner rework into every implementation column below.
    design = [pr for pr in prs if DESIGN_TITLE.match(pr.get("title") or "")]
    rollup = [pr for pr in prs if ROLLUP_TITLE.match(pr.get("title") or "")]
    prs = [pr for pr in prs if pr not in design and pr not in rollup]
    rows, measured = [], 0
    for pr in prs:
        closes = {int(n) for n in CLOSES.findall(pr.get("body") or "")}
        hit = sorted(n for n in closes if n in decided)
        # What ran beats what was planned wherever the developer recorded it.
        # Falling back to the label is not equivalent, and the count below says
        # how much of the comparison rests on the weaker of the two.
        ran = actual.get(pr["number"])
        if ran:
            measured += 1
        if ran or hit:
            rows.append(measure(pr, ran or decided[hit[0]], costs.get(pr["number"])))

    print("merged pull requests: {} (left out: {} design, {} feature or release)".format(
        len(prs), len(design), len(rollup)))
    print("joined to a model budget: {}".format(len(rows)))
    print("  of those, recording what actually ran: {} (the rest are the"
          " label, i.e. the plan)".format(measured))
    print("  of those, carrying a Build cost: comment: {}".format(
        sum(1 for r in rows if r["dollars"] is not None)))
    print("carrying a QA verdict comment: {} of {}".format(
        sum(1 for r in rows if r["verdict"]), len(rows)))
    print("rebase-suspect (every commit post-open): {}".format(
        sum(1 for r in rows if r["rebased"])))

    table("by model - raw, confounded by task size",
          [(m, [r for r in rows if r["model"] == m]) for m in MODELS])

    labelled = sorted({r["label"] for r in rows},
                      key=lambda x: (MODELS.index(x.split(" - ")[0]), x))
    table("by label", [(l, [r for r in rows if r["label"] == l]) for l in labelled])

    low, high = (int(x) for x in args.band.split("-"))
    band = [r for r in rows if low <= r["churn"] <= high]
    table("size-matched: churn {}-{}".format(low, high),
          [(m, [r for r in band if r["model"] == m]) for m in MODELS])

    table("size-matched by label: churn {}-{}".format(low, high),
          [(l, [r for r in band if r["label"] == l]) for l in labelled])

    treatment = [r["rounds"] for r in band if r["label"] == args.treatment]
    control = [r["rounds"] for r in band if r["label"] == args.control]
    interval = bootstrap(treatment, control)
    print("\nsize-matched rework, {} minus {}".format(args.treatment, args.control))
    if interval is None:
        print("  not enough pull requests in the band to compare")
    else:
        print("  {:<10} n={:<3} mean {:.2f}".format(
            args.control, len(control), statistics.mean(control)))
        print("  {:<10} n={:<3} mean {:.2f}".format(
            args.treatment, len(treatment), statistics.mean(treatment)))
        print("  95% interval for the difference: [{:+.2f}, {:+.2f}]".format(*interval))
        print("  " + ("straddles zero - no detectable penalty at this size"
                      if interval[0] < 0 < interval[1] else
                      "excludes zero - the difference is real at this size"))
        if len(treatment) < 25 or len(control) < 25:
            print("  fewer than 25 per arm: treat as indicative, not settled")


if __name__ == "__main__":
    main()
