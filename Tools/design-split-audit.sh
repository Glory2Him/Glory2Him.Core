#!/bin/bash
# ────────────────────────────────────────────────────────────────────────────────
# Standing two-gate check for the area-scoped design documents under
# `Documentation/Design/`.
#
# The split of `Documentation/G2H Design.md` into area files is finished, and
# the baseline-relative proofs that a relocation was faithful (G2, symmetric
# heading difference; G3, citation resolution; VERBATIM, prose-identity diff)
# have outlived their subject — they get louder with every legitimate later
# edit, not quieter, so they were retired rather than kept as an audit record.
# Retired in issue #586; #562 records the reasoning for VERBATIM and the four
# extraction issues (#481, #552-#555) it once proved.
#
# Two gates remain, of different kinds, and neither reads a baseline:
#
#   G1 — heading PROVENANCE. Every heading in a non-exempt area file must
#        declare where it came from: a relocation, `*(formerly §N.M)*`, or a
#        new section, `*(new; <source or reason>)*` or bare `*(new)*`. This
#        reads the tree as it stands today; it needs no history and no
#        `origin/main`.
#   G4 — citation FORM. Inside a non-exempt area file, a `§N.M` citation must
#        carry its area prefix (`§UI20.6`, never bare `§20.6`) — the rule
#        `Documentation/G2H Design.md` §IDX1.5 states. HEAD-only, no baseline.
#
# The surviving authority for what each gate checks is
# `Documentation/G2H Design.md` §IDX1.5 (which area is in which file, and the
# citation-prefix rule) and `DEVELOPERS.md`'s "The split, and why sections
# carry prefixes".
#
# Usage:
#   Tools/design-split-audit.sh [--scope <20|sec|arc|apr|dom|all>] [--gate <g1|g4|all>]
#
# Examples:
#   Tools/design-split-audit.sh                        # every gate, scoped to §20
#   Tools/design-split-audit.sh --scope sec            # every gate, scoped to §14 and §18
#   Tools/design-split-audit.sh --scope arc            # every gate, scoped to §12, §16 and §17
#   Tools/design-split-audit.sh --scope apr            # every gate, scoped to §7, §8, §9 and §13
#   Tools/design-split-audit.sh --scope dom            # every gate, scoped to §2, §3, §4, §5, §6, §11, §19
#   Tools/design-split-audit.sh --gate g4 --scope all  # citation form alone, whole document
#   Tools/design-split-audit.sh --gate g1 --scope all  # heading provenance alone, whole document
#
# Defaults:
#   --scope     20        §20 is the extraction #481 performed. `sec` is issue #552's
#                         extraction — §14 and §18, both landing in
#                         `Security.md` under the same `SEC` prefix, non-contiguous
#                         with each other and with everything either side of them.
#                         `arc` is issue #553's extraction — §12, §16 and §17,
#                         landing in `Architecture.md` under the `ARC` prefix; §13
#                         to §15 stand between §12 and §16 in `G2H Design.md` but
#                         the three ranges are contiguous in `Architecture.md`.
#                         `apr` is issue #554's extraction — §7, §8, §9 and §13,
#                         landing in `Approval.md` under the `APR` prefix; §10 to
#                         §12 stand between §9 and §13 in `G2H Design.md` but the
#                         four ranges are contiguous in `Approval.md`.
#                         `dom` is issue #555's extraction — §2, §3, §4, §5, §6,
#                         §11 and §19, landing in `Domain.md` under the `DOM`
#                         prefix; §7 to §10 stand between §6 and §11, and §12 to
#                         §18 stand between §11 and §19, in `G2H Design.md`, but
#                         the seven ranges are contiguous in `Domain.md`.
#                         `all` is the whole-document mode: every `*.md` under
#                         `Documentation/Design/`. Both gates skip
#                         `Documentation/Design/Events.md` regardless of scope —
#                         see the note below — and that skip lives inside each
#                         gate, not in file selection.
#   --gate      all       G1 is heading provenance, G4 is citation form. Neither
#                         reads a baseline or `origin/main`.
#
# Exit code: 0 when every gate in scope reports nothing, 1 otherwise. An
# invocation naming a retired gate (`g2`, `g3`, `verbatim`) or the retired
# `--baseline` argument is refused with exit code 2 and a message naming what
# was retired, rather than quietly succeeding.
#
# Notes:
# - `Documentation/Design/Events.md` IS an area file, but it renumbered rather
#   than prefix-preserving, and it carries sections (`EVN0`, `EVN10`, `EVN20`,
#   `EVN21`, `EVN22`, `EVN24`, ...) that had no former life in `G2H Design.md`.
#   It is exempt from both G1 and G4 — see each gate's own comment for why the
#   exemption is permanent.
# ────────────────────────────────────────────────────────────────────────────────

set -u

export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

SCOPE="20"
GATE="all"

while [ $# -gt 0 ]; do
    case "$1" in
        --baseline)
            echo "--baseline was retired in issue #586: neither surviving gate (G1, G4)" >&2
            echo "reads a baseline, so the argument has nothing left to satisfy." >&2
            exit 2
            ;;
        --scope)    SCOPE="$2";    shift 2 ;;
        --gate)     GATE="$2";     shift 2 ;;
        -h|--help)  awk 'NR == 1 { next } /^# [^A-Za-z0-9]*$/ { if (++rule == 2) exit; next } { print }' "$0"; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

case "$SCOPE" in 20|sec|arc|apr|dom|all) ;; *) echo "--scope must be 20, sec, arc, apr, dom or all" >&2; exit 2 ;; esac
case "$GATE" in
    g1|g4|all) ;;
    g2|g3|verbatim)
        echo "--gate $GATE was retired in issue #586, along with G2, G3 and VERBATIM —" >&2
        echo "the baseline-relative proofs that a relocation was faithful. They get louder" >&2
        echo "with every legitimate later edit, and the split they proved is finished." >&2
        exit 2
        ;;
    *) echo "--gate must be g1, g4 or all" >&2; exit 2 ;;
esac

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT" || exit 2

AREA_DIR="Documentation/Design"
EVENTS="$AREA_DIR/Events.md"

FAILURES=0

area_files() {
    # Every area file. Scope 20 is UI.md alone; scope sec is
    # Security.md alone (§14 and §18, issue #552); scope arc is Architecture.md
    # alone (§12, §16 and §17, issue #553); scope apr is Approval.md alone
    # (§7, §8, §9 and §13, issue #554); scope dom is Domain.md alone
    # (§2, §3, §4, §5, §6, §11 and §19, issue #555).
    if [ "$SCOPE" = "20" ]; then
        [ -f "$AREA_DIR/UI.md" ] && echo "$AREA_DIR/UI.md"
        return
    fi

    if [ "$SCOPE" = "sec" ]; then
        [ -f "$AREA_DIR/Security.md" ] && echo "$AREA_DIR/Security.md"
        return
    fi

    if [ "$SCOPE" = "arc" ]; then
        [ -f "$AREA_DIR/Architecture.md" ] && echo "$AREA_DIR/Architecture.md"
        return
    fi

    if [ "$SCOPE" = "apr" ]; then
        [ -f "$AREA_DIR/Approval.md" ] && echo "$AREA_DIR/Approval.md"
        return
    fi

    if [ "$SCOPE" = "dom" ]; then
        [ -f "$AREA_DIR/Domain.md" ] && echo "$AREA_DIR/Domain.md"
        return
    fi

    for file in "$AREA_DIR"/*.md; do
        [ -f "$file" ] || continue
        echo "$file"
    done
}

expected_area_file() {
    # The one file a narrow scope resolves to, named when it is missing.
    # Meaningless under --scope all, which resolves to every area file.
    case "$SCOPE" in
        20)  echo "$AREA_DIR/UI.md" ;;
        sec) echo "$AREA_DIR/Security.md" ;;
        arc) echo "$AREA_DIR/Architecture.md" ;;
        apr) echo "$AREA_DIR/Approval.md" ;;
        dom) echo "$AREA_DIR/Domain.md" ;;
        *)   echo "$AREA_DIR/*.md" ;;
    esac
}

report() {
    local gate="$1" finding="$2" body="$3"

    if [ -z "$body" ]; then
        echo "$gate: OK"
    else
        echo "$gate: $finding"
        echo "$body" | sed 's/^/    /'
        FAILURES=$((FAILURES + 1))
    fi
}

# ── G1 — every heading declares its provenance, a relocation or a new section ───
gate_g1() {
    local files body=""
    files="$(area_files)"

    if [ -z "$files" ]; then
        report "G1" "area file in scope is missing" "expected $(expected_area_file)"
        return
    fi

    while IFS= read -r file; do
        # `Events.md` renumbered rather than prefix-preserving and carries
        # sections with no former life in `G2H Design.md` — see the header
        # note above for why the exemption is permanent.
        [ "$file" = "$EVENTS" ] && continue
        local hits
        # A heading declares its provenance one of two ways: a relocation,
        # `*(formerly §N.M)*` — the anchor by which 103 of the 106 distinct
        # pre-split section numbers still cited outside `Documentation/` (3,502
        # of 3,522 occurrences; §1.1.3, §1.8 and §12.4.7 are pre-existing
        # dangling citations, not resolved by any heading, and this comment
        # does not claim otherwise) resolve to a heading, and in every closed
        # issue and merged PR body, which can never be swept — or a new
        # section, in either spelling already in use in `Events.md`:
        # `*(new; <source or reason>)*` where there is something to name, and
        # bare `*(new)*` where there is not. Measured at `eca067e1` (this
        # branch's start point on `main`, cited rather than a branch-local
        # commit so the ref survives a squash-merge), with the commands below
        # run at that commit:
        #   $ git grep -oIE '§[0-9]+(\.[0-9]+)+' eca067e1 -- . \
        #       ':(exclude)Documentation/*' ':(exclude)Tools/design-split-audit.sh' | wc -l
        #   3522                                     # bare-citation occurrences
        #   $ git grep -hoIE '§[0-9]+(\.[0-9]+)+' eca067e1 -- . \
        #       ':(exclude)Documentation/*' ':(exclude)Tools/design-split-audit.sh' \
        #       | grep -oE '§[0-9]+(\.[0-9]+)+' | sort -u > /tmp/all-106
        #   $ wc -l < /tmp/all-106
        #   106                                      # distinct pre-split numbers
        #   $ git grep -hoIE 'formerly §[0-9]+(\.[0-9]+)*\)' eca067e1 -- Documentation/ \
        #       | grep -oE '§[0-9]+(\.[0-9]+)*' | sort -u > /tmp/annotated
        #   $ comm -23 /tmp/all-106 /tmp/annotated
        #   §1.1.3  §1.8  §12.4.7                    # the 3 that do not resolve
        # 103 of the 106 resolve; at the occurrence level that is 3,502 of the
        # 3,522 (3,522 minus the 20 occurrences of those three numbers).
        # Measured at `eca067e1`: 224 headings across the six area files, 217
        # carrying `(formerly §`, 5 carrying `(new;` or `(new)` (all in
        # `Events.md`), and 2 (`EVN0`, `EVN22`, both in the exempt file)
        # carrying neither.
        #   $ git grep -cE '^#{2,6} ' eca067e1 -- Documentation/Design/*.md | \
        #       awk -F: '{s+=$2} END {print s}'
        #   224                                      # total headings
        #   $ git grep -hE '^#{2,6} ' eca067e1 -- Documentation/Design/*.md | \
        #       grep -c '(formerly §'
        #   217
        #   $ git grep -hE '^#{2,6} ' eca067e1 -- Documentation/Design/*.md | \
        #       grep -cE '\(new[;,)]'
        #   5
        #   $ git grep -hE '^#{2,6} ' eca067e1 -- Documentation/Design/*.md | \
        #       grep -v '(formerly §' | grep -vcE '\(new[;,)]'
        #   2
        hits="$(grep -nE '^#{2,6} ' "$file" | grep -v '(formerly §' | grep -v '(new[;,)]' || true)"
        [ -n "$hits" ] && body="$body$(echo "$hits" | sed "s|^|$file:|")
"
    done <<< "$files"

    report "G1" "headings carrying no relocation or new-section annotation" "$(echo "$body" | sed '/^$/d')"
}

# ── G4 — inside Documentation/, citations are prefixed ──────────────────────────
gate_g4() {
    local files body=""
    files="$(area_files)"

    if [ -z "$files" ]; then
        report "G4" "area file in scope is missing" "expected $(expected_area_file)"
        return
    fi

    while IFS= read -r file; do
        # `Events.md` legitimately holds bare dotted numbers — in fenced
        # quotations, in prose whose subject IS a number, and in
        # `EventSubstrate.md`'s own numbering — so ending this exemption would
        # cost new strips for one file, and it therefore never ends.
        [ "$file" = "$EVENTS" ] && continue
        local hits
        # Both the strip below and the citation grep after it are literal-digit
        # patterns: neither can tell an annotation-shaped token
        # (`*(formerly §N.M)*`) apart from a genuine citation by anything other
        # than the digits, so a heading annotation citing a number that also
        # happens to look like a bare citation would silently confuse the two
        # in either direction. The *(formerly §N.M)* annotation is an anchor
        # for code and closed issues, not a citation, so it is stripped before
        # the line is judged. The repair for a false positive here is to
        # REWORD THE PROSE, never to widen the strip — widening it risks
        # hiding a real bare citation the same way.
        hits="$(grep -n '' "$file" \
            | sed -E 's/\*?\(formerly §[0-9][0-9.]*\)\*?//g' \
            | grep -E '§[0-9]+\.[0-9]' || true)"
        [ -n "$hits" ] && body="$body$(echo "$hits" | sed "s|^|$file:|")
"
    done <<< "$files"

    report "G4" "bare unprefixed §N.M citations" "$(echo "$body" | sed '/^$/d')"
}

echo "design-split-audit — scope §$SCOPE, gate $GATE"
echo

[ "$GATE" = "g1" ] || [ "$GATE" = "all" ] && gate_g1
[ "$GATE" = "g4" ] || [ "$GATE" = "all" ] && gate_g4

echo
if [ "$FAILURES" -eq 0 ]; then
    echo "All gates in scope report nothing."
    exit 0
fi

echo "$FAILURES gate(s) reported findings."
exit 1
