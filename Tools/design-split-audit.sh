#!/bin/bash
# ────────────────────────────────────────────────────────────────────────────────
# Completeness audit for the area-scoped design-document split (issue #481).
#
# `Documentation/G2H Design.md` is being broken into area files under
# `Documentation/Design/`, one extraction per issue. A move of load-bearing prose
# has exactly one failure mode worth automating against: a section that is
# dropped, or one that is invented. A read-through does not catch either, so
# this script is the proof the split's gates ask for.
#
# It implements gates G1–G4 of `Documentation/Design/Split.md` §S6. G5 and G6 are
# standing exemptions, verified there, and are subtracted by G3 rather than
# reported.
#
# Usage:
#   Tools/design-split-audit.sh [--baseline <ref>] [--scope <20|all>] [--gate <g1|g2|g3|g4|verbatim|all>]
#
# Examples:
#   Tools/design-split-audit.sh                        # every gate, scoped to §20
#   Tools/design-split-audit.sh --scope all            # every gate, whole document
#   Tools/design-split-audit.sh --gate g2 --scope all  # the completeness gate alone
#   Tools/design-split-audit.sh --gate verbatim        # the prose-identity proof alone
#
# Defaults:
#   --baseline  55c461e2  the commit the split branches from, with #548 merged.
#                         Override to re-baseline a later extraction.
#   --scope     20        §20 is the extraction issue #481 performs. `all` is the
#                         whole-document mode: it reports every section not yet
#                         extracted and is expected to be NON-EMPTY until the last
#                         extraction (#556) runs it to empty.
#   --gate      all       G1-G4 are Split.md §S6's gates. `verbatim` is the extra
#                         check #481 criterion 2 asks for — that no sentence was
#                         reworded inside the move — which no section-level gate
#                         can see.
#
# Exit code: 0 when every gate in scope reports nothing, 1 otherwise.
#
# Notes:
# - `Documentation/Design/Split.md` is NOT an area file. It is the rulings file,
#   and it quotes old heading forms inside fenced examples that begin with `##`,
#   so scanning it would invent sections that were never moved. It is excluded
#   everywhere, and retires with the split.
# - `Documentation/Design/Events.md` IS an area file, but it renumbered rather
#   than prefix-preserving, and it carries sections (`EVN0`, `EVN10`, `EVN20`,
#   `EVN21`, `EVN22`) that had no former life in `G2H Design.md`. It therefore
#   counts for G2 and is exempt from G1 and G4 (§S6).
# - Greps for an old number are RIGHT-ANCHORED — `§20\.6($|[^0-9.])`. An
#   unanchored `§20.6` matches the `§20.6.1` heading too, and every parent number
#   then reports a false duplicate against its own children.
# ────────────────────────────────────────────────────────────────────────────────

set -u

export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

BASELINE="55c461e2"
SCOPE="20"
GATE="all"

while [ $# -gt 0 ]; do
    case "$1" in
        --baseline) BASELINE="$2"; shift 2 ;;
        --scope)    SCOPE="$2";    shift 2 ;;
        --gate)     GATE="$2";     shift 2 ;;
        -h|--help)  sed -n '2,46p' "$0"; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

case "$SCOPE" in 20|all) ;; *) echo "--scope must be 20 or all" >&2; exit 2 ;; esac
case "$GATE" in g1|g2|g3|g4|verbatim|all) ;; *) echo "--gate must be g1, g2, g3, g4, verbatim or all" >&2; exit 2 ;; esac

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT" || exit 2

DESIGN_DOC="Documentation/G2H Design.md"
AREA_DIR="Documentation/Design"
RULINGS="$AREA_DIR/Split.md"
EVENTS="$AREA_DIR/Events.md"

FAILURES=0

area_files() {
    # Every area file, rulings excluded. Scope 20 is Ui.md alone.
    if [ "$SCOPE" = "20" ]; then
        [ -f "$AREA_DIR/Ui.md" ] && echo "$AREA_DIR/Ui.md"
        return
    fi

    for file in "$AREA_DIR"/*.md; do
        [ -f "$file" ] || continue
        [ "$file" = "$RULINGS" ] && continue
        echo "$file"
    done
}

in_scope() {
    # Filters a stream of heading numbers down to the scope.
    if [ "$SCOPE" = "20" ]; then
        grep -E '^20($|\.)'
    else
        cat
    fi
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

# The heading numbers that stood in G2H Design.md at the baseline commit.
baseline_heading_numbers() {
    git show "$BASELINE:$DESIGN_DOC" \
        | grep -E '^#{2,6} [0-9]+' \
        | sed -E 's/^#+ ([0-9]+(\.[0-9]+)*)\.?[[:space:]].*/\1/' \
        | sort -u
}

# The old numbers the area files claim, read off their *(formerly §N.M)* annotations.
relocated_heading_numbers() {
    local files
    files="$(area_files)"
    [ -z "$files" ] && return

    echo "$files" | while IFS= read -r file; do
        grep -hE '^#+ ' "$file"
    done \
        | grep -oE '\(formerly §[0-9][0-9.]*\)' \
        | sed -E 's/\(formerly §([0-9][0-9.]*)\)/\1/' \
        | sort -u
}

# ── G1 — every relocated heading carries its former number literally ────────────
gate_g1() {
    local files body=""
    files="$(area_files)"

    if [ -z "$files" ]; then
        report "G1" "no area file in scope exists yet" "expected $AREA_DIR/Ui.md"
        return
    fi

    while IFS= read -r file; do
        [ "$file" = "$EVENTS" ] && continue
        local hits
        hits="$(grep -nE '^#{2,6} ' "$file" | grep -v '(formerly §' || true)"
        [ -n "$hits" ] && body="$body$(echo "$hits" | sed "s|^|$file:|")
"
    done <<< "$files"

    report "G1" "headings carrying no *(formerly §N.M)* annotation" "$(echo "$body" | sed '/^$/d')"
}

# ── G2 — the old set and the new set are the same set ───────────────────────────
gate_g2() {
    local old new only_old only_new body=""

    old="$(baseline_heading_numbers | in_scope)"
    new="$(relocated_heading_numbers | in_scope)"

    if [ "$SCOPE" = "all" ]; then
        # §1 and §10 are retained in the index; §15 and §21 are retired (§S4).
        # Both sides are filtered, not just the old one: §10's subsections left
        # G2H Design.md before the baseline and live in Events.md under their own
        # annotations, so allowing for §10 means allowing for it in both sets.
        local retained='^(1|10|15|21)($|\.)'
        old="$(echo "$old" | grep -Ev "$retained")"
        new="$(echo "$new" | grep -Ev "$retained")"
    fi

    only_old="$(comm -23 <(echo "$old" | sed '/^$/d') <(echo "$new" | sed '/^$/d'))"
    only_new="$(comm -13 <(echo "$old" | sed '/^$/d') <(echo "$new" | sed '/^$/d'))"

    [ -n "$only_old" ] && body="$body$(echo "$only_old" | sort -V | sed 's/^/dropped  §/')
"
    [ -n "$only_new" ] && body="$body$(echo "$only_new" | sort -V | sed 's/^/invented §/')
"

    report "G2" "symmetric difference between the baseline headings and the relocated ones" \
        "$(echo "$body" | sed '/^$/d')"
}

# ── G3 — every old citation from code still resolves, to exactly one heading ────
gate_g3() {
    local pattern citations body=""

    if [ "$SCOPE" = "20" ]; then
        pattern='§20[0-9.]*'
    else
        pattern='§[0-9]+(\.[0-9]+)*'
    fi

    citations="$(git grep -h -o -E "$pattern" "$BASELINE" -- . ':(exclude)Documentation/' \
        | sort -u -V)"

    if [ "$SCOPE" = "all" ]; then
        # §10 is out of G3's scope by design, and §12.4.7, §1.1.3 and §534 are the
        # three exemptions verified in Split.md §S6 (gates G5 and G6).
        citations="$(echo "$citations" | grep -Ev '^§10($|\.)' \
            | grep -Ev '^§(12\.4\.7|1\.1\.3|534)$')"
    fi

    while IFS= read -r citation; do
        [ -z "$citation" ] && continue
        local number anchored count
        number="${citation#§}"
        anchored="§$(echo "$number" | sed 's/\./\\./g')(\$|[^0-9.])"
        count="$(grep -rhE '^#{1,6} ' --include='*.md' "Documentation/" \
            | grep -cE "$anchored" || true)"
        [ "$count" = "1" ] || body="$body$citation resolves to $count headings, expected 1
"
    done <<< "$citations"

    report "G3" "old citations that do not resolve to exactly one heading" \
        "$(echo "$body" | sed '/^$/d')"
}

# ── G4 — inside Documentation/, citations are prefixed ──────────────────────────
gate_g4() {
    local files body=""
    files="$(area_files)"

    if [ -z "$files" ]; then
        report "G4" "no area file in scope exists yet" "expected $AREA_DIR/Ui.md"
        return
    fi

    while IFS= read -r file; do
        [ "$file" = "$EVENTS" ] && continue
        local hits
        # The *(formerly §N.M)* annotation is an anchor for code and closed issues,
        # not a citation, so it is stripped before the line is judged.
        hits="$(grep -n '' "$file" \
            | sed -E 's/\*?\(formerly §[0-9][0-9.]*\)\*?//g' \
            | grep -E '§[0-9]+\.[0-9]' || true)"
        [ -n "$hits" ] && body="$body$(echo "$hits" | sed "s|^|$file:|")
"
    done <<< "$files"

    report "G4" "bare unprefixed §N.M citations" "$(echo "$body" | sed '/^$/d')"
}

# ── VERBATIM — the relocated prose is the prose that was there ──────────────────
# G1 to G4 prove that no SECTION was lost. They say nothing about whether a
# sentence inside one was reworded, and a read-through is not a substitute for
# knowing. This normalises the extracted file back towards the baseline body and
# diffs the two, permitting exactly the four differences #481 criterion 2 allows:
# a heading gaining its prefix and its *(formerly §N.M)* annotation, a citation
# token gaining its prefix, the file's own header block and contents list, and
# its title line. Anything else is a rewrite hidden inside a move.
#
# It is defined per extraction rather than per document, so it runs under
# --scope 20 and each later extraction adds its own pairing.
gate_verbatim() {
    local file="$AREA_DIR/Ui.md" old new difference

    if [ "$SCOPE" != "20" ]; then
        report "VERBATIM" "defined per extraction, not for the whole document" \
            "run it with --scope 20"
        return
    fi

    if [ ! -f "$file" ]; then
        report "VERBATIM" "no area file in scope exists yet" "expected $file"
        return
    fi

    old="$(git show "$BASELINE:$DESIGN_DOC" \
        | sed -n '/^## 20\. /,/^## 21\. /p' \
        | sed '$d' \
        | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

    # Everything below the rule closing the header block is the relocated body.
    # The heading annotation is dropped rather than checked here, because G1 and
    # G2 have already proved every one of them carries the right old number.
    new="$(sed -n '/^---$/,$p' "$file" \
        | tail -n +2 \
        | sed -e '/./,$!d' \
        | sed -E 's/^(#{2,6} )UI(20[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\3/' \
        | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
        | sed -E 's/§EVN18/§10.17/g' \
        | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

    difference="$(diff <(echo "$old") <(echo "$new") || true)"

    report "VERBATIM" "normalised differences against the baseline §20 body" "$difference"
}

echo "design-split-audit — baseline $BASELINE, scope §$SCOPE, gate $GATE"
echo

[ "$GATE" = "g1" ] || [ "$GATE" = "all" ] && gate_g1
[ "$GATE" = "g2" ] || [ "$GATE" = "all" ] && gate_g2
[ "$GATE" = "g3" ] || [ "$GATE" = "all" ] && gate_g3
[ "$GATE" = "g4" ] || [ "$GATE" = "all" ] && gate_g4
[ "$GATE" = "verbatim" ] || [ "$GATE" = "all" ] && gate_verbatim

echo
if [ "$FAILURES" -eq 0 ]; then
    echo "All gates in scope report nothing."
    exit 0
fi

echo "$FAILURES gate(s) reported findings."
exit 1
