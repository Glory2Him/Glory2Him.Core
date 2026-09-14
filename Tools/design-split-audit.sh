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
#   Tools/design-split-audit.sh [--baseline <ref>] [--scope <20|sec|arc|all>] [--gate <g1|g2|g3|g4|verbatim|all>]
#
# Examples:
#   Tools/design-split-audit.sh                        # every gate, scoped to §20
#   Tools/design-split-audit.sh --scope sec            # every gate, scoped to §14 and §18
#   Tools/design-split-audit.sh --scope arc            # every gate, scoped to §12, §16 and §17
#   Tools/design-split-audit.sh --scope all            # every gate, whole document
#   Tools/design-split-audit.sh --gate g2 --scope all  # the completeness gate alone
#   Tools/design-split-audit.sh --gate verbatim        # the prose-identity proof alone
#
# Defaults:
#   --baseline  resolved  `git merge-base origin/main HEAD` — the commit the split
#                         branches from. It is RESOLVED and never pinned: a pinned
#                         SHA is orphaned by any history rewrite, and then resolves
#                         on the machine that pinned it and nowhere else. Override
#                         to re-baseline a later extraction. A purely local `main`
#                         is refused rather than used, and a `main` that disagrees
#                         with `origin/main` is reported; the header prints the
#                         baseline's date and subject so a stale one is visible.
#   --scope     20        §20 is the extraction issue #481 performs. `sec` is
#                         issue #552's extraction — §14 and §18, both landing in
#                         `Security.md` under the same `SEC` prefix, non-contiguous
#                         with each other and with everything either side of them.
#                         `arc` is issue #553's extraction — §12, §16 and §17,
#                         landing in `Architecture.md` under the `ARC` prefix; §13
#                         to §15 stand between §12 and §16 in `G2H Design.md` but
#                         the three ranges are contiguous in `Architecture.md`.
#                         `all` is the whole-document mode: it reports every
#                         section not yet extracted and is expected to be
#                         NON-EMPTY until the last extraction (#556) runs it to
#                         empty.
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
# - `--scope arc`'s VERBATIM pairing normalises the BASELINE forward rather than
#   the new file backward, unlike `--scope sec`. §12/§16/§17 carry 21 pre-split
#   `§10.x` citations into event design (`§10.2`, `§10.4`, `§10.5`, `§10.7`,
#   `§10.17`, `§10.18`) that this extraction resolves to their `Events.md`
#   `(formerly §10.X)` anchors — but the same body already carries fifteen
#   `§EVN` occurrences that were resolved that way by an earlier extraction and
#   must not move again. Two of those six numbers, `§10.17`→`§EVN18` and
#   `§10.18`→`§EVN19`, collide on their output token with a pre-existing `§EVN18`
#   / `§EVN19` that already stood in the body, so the two forms are byte-identical
#   once written and a backward reversal cannot tell them apart by content alone.
#   Forward normalisation has no such ambiguity — it only ever rewrites a literal
#   `§10.x`, never a `§EVN...` that was already there — so it is used instead.
# ────────────────────────────────────────────────────────────────────────────────

set -u

export MSYS_NO_PATHCONV=1
export MSYS2_ARG_CONV_EXCL='*'

BASELINE=""
SCOPE="20"
GATE="all"

while [ $# -gt 0 ]; do
    case "$1" in
        --baseline) BASELINE="$2"; shift 2 ;;
        --scope)    SCOPE="$2";    shift 2 ;;
        --gate)     GATE="$2";     shift 2 ;;
        -h|--help)  awk 'NR == 1 { next } /^# [^A-Za-z0-9]*$/ { if (++rule == 2) exit; next } { print }' "$0"; exit 0 ;;
        *) echo "Unknown argument: $1" >&2; exit 2 ;;
    esac
done

case "$SCOPE" in 20|sec|arc|all) ;; *) echo "--scope must be 20, sec, arc or all" >&2; exit 2 ;; esac
case "$GATE" in g1|g2|g3|g4|verbatim|all) ;; *) echo "--gate must be g1, g2, g3, g4, verbatim or all" >&2; exit 2 ;; esac

REPO_ROOT="$(git rev-parse --show-toplevel)"
cd "$REPO_ROOT" || exit 2

# The baseline is resolved, not pinned. A pinned SHA survives only in the object
# store of the machine that pinned it: reword the branch, clone it, or run a
# `git gc`, and the pin dangles while every gate reports the whole extraction as a
# difference. The merge base is the same commit by construction and stays correct
# for each later extraction without being re-pinned.
#
# Resolution is, however, the one part of this script that depends on the machine
# it runs on. `origin/main` is a local pointer, and a clone, a stale fetch or a
# fork can leave it behind the real main. A baseline a few commits early still
# passes every gate whose sections those commits did not touch, so the audit then
# reports success about the wrong commit and says nothing about it. Nothing inside
# a repository can prove that pointer is current, so this does the three things
# that are possible: it refuses a purely local `main`, it says so when the two
# candidates disagree, and it prints the baseline's subject and date so a wrong
# one is recognisable on sight rather than only as a SHA.
BASELINE_SOURCE="the --baseline argument"

if [ -z "$BASELINE" ]; then
    REMOTE_BASE=""
    LOCAL_BASE=""

    if git rev-parse --verify --quiet "origin/main^{commit}" >/dev/null; then
        REMOTE_BASE="$(git merge-base origin/main HEAD)"
    fi

    if git rev-parse --verify --quiet "main^{commit}" >/dev/null; then
        LOCAL_BASE="$(git merge-base main HEAD)"
    fi

    if [ -n "$REMOTE_BASE" ]; then
        BASELINE="$REMOTE_BASE"
        BASELINE_SOURCE="git merge-base origin/main HEAD"

        if [ -n "$LOCAL_BASE" ] && [ "$LOCAL_BASE" != "$REMOTE_BASE" ]; then
            echo "WARNING: origin/main and main disagree about the baseline." >&2
            echo "         origin/main -> $REMOTE_BASE  (used)" >&2
            echo "         main        -> $LOCAL_BASE" >&2
            echo "         One of the two is stale. Fetch, or pass --baseline <ref>." >&2
            echo >&2
        fi
    elif [ -n "$LOCAL_BASE" ]; then
        echo "Refusing to baseline on a local 'main': there is no origin/main to check it" >&2
        echo "against. A local branch pointer is not evidence of where this work branches" >&2
        echo "from, and a baseline even a few commits early passes every gate whose sections" >&2
        echo "those commits did not touch. Pass the commit explicitly:" >&2
        echo >&2
        echo "    Tools/design-split-audit.sh --baseline $LOCAL_BASE" >&2
        exit 2
    fi
fi

if [ -z "$BASELINE" ]; then
    echo "Could not resolve a baseline: no origin/main to take a merge base from." >&2
    echo "Pass one explicitly with --baseline <ref>." >&2
    exit 2
fi

if ! git rev-parse --verify --quiet "$BASELINE^{commit}" >/dev/null; then
    echo "Baseline '$BASELINE' does not resolve to a commit in this repository." >&2
    exit 2
fi

DESIGN_DOC="Documentation/G2H Design.md"
AREA_DIR="Documentation/Design"
RULINGS="$AREA_DIR/Split.md"
EVENTS="$AREA_DIR/Events.md"

FAILURES=0

area_files() {
    # Every area file, rulings excluded. Scope 20 is UI.md alone; scope sec is
    # Security.md alone (§14 and §18, issue #552); scope arc is Architecture.md
    # alone (§12, §16 and §17, issue #553).
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

    for file in "$AREA_DIR"/*.md; do
        [ -f "$file" ] || continue
        [ "$file" = "$RULINGS" ] && continue
        echo "$file"
    done
}

expected_area_file() {
    # The one file a narrow scope resolves to, named for a "not extracted yet"
    # message. Meaningless under --scope all, which resolves to every area file.
    case "$SCOPE" in
        20)  echo "$AREA_DIR/UI.md" ;;
        sec) echo "$AREA_DIR/Security.md" ;;
        arc) echo "$AREA_DIR/Architecture.md" ;;
        *)   echo "$AREA_DIR/*.md" ;;
    esac
}

in_scope() {
    # Filters a stream of heading numbers down to the scope.
    if [ "$SCOPE" = "20" ]; then
        grep -E '^20($|\.)'
    elif [ "$SCOPE" = "sec" ]; then
        grep -E '^(14|18)($|\.)'
    elif [ "$SCOPE" = "arc" ]; then
        grep -E '^(12|16|17)($|\.)'
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
        report "G1" "no area file in scope exists yet" "expected $(expected_area_file)"
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
    elif [ "$SCOPE" = "sec" ]; then
        pattern='§(14|18)(\.[0-9]+)*'
    elif [ "$SCOPE" = "arc" ]; then
        pattern='§(12|16|17)(\.[0-9]+)*'
    else
        pattern='§[0-9]+(\.[0-9]+)*'
    fi

    # `-I` excludes binary files. Without it `git grep` emits a `Binary file
    # <rev>:<path> matches` line into the citation stream, each of which is then
    # judged as a citation and reported as resolving to no heading — a finding the
    # whole-document mode could never be run to empty.
    citations="$(git grep -I -h -o -E "$pattern" "$BASELINE" -- . ':(exclude)Documentation/' \
        | sort -u -V)"

    if [ "$SCOPE" = "all" ] || [ "$SCOPE" = "arc" ]; then
        # §12.4.7 is G5's standing exemption (Split.md §S6): it dangled before
        # the split and the split neither creates nor repairs it. §12.4.4 is a
        # second, previously unrecorded dangle found by issue #553 — cited as
        # "§12.4.4 BR14" / "§12.4.4 rule 11" from six files
        # (`ApprovalEntityProbeReadTests.cs`,
        # `AssociationServiceTests.TransitionApproval.Validations.cs`,
        # `ApprovalEntityMatch.cs` twice, `IApprovalService.cs`,
        # `IAssociationService.cs`) plus one more inside `G2H Design.md` §9.7.x —
        # §12.4 has only §12.4.1 and §12.4.2, and no BR14 or rule 11 stands
        # anywhere under it today. It is subtracted the same way as §12.4.7
        # rather than reported, on the same §S4.1 precedent: a gate nobody can
        # run green stops being run. The issue this raises is linked from the
        # PR that introduced this subtraction.
        citations="$(echo "$citations" | grep -Ev '^§(12\.4\.7|12\.4\.4)$')"
    fi

    if [ "$SCOPE" = "all" ]; then
        # §10 is out of G3's scope by design, and §1.1.3 and §534 are the
        # remaining exemptions verified in Split.md §S6 (gate G6).
        citations="$(echo "$citations" | grep -Ev '^§10($|\.)' \
            | grep -Ev '^§(1\.1\.3|534)$')"
    fi

    while IFS= read -r citation; do
        [ -z "$citation" ] && continue
        local number anchored count
        number="${citation#§}"
        anchored="§$(echo "$number" | sed 's/\./\\./g')(\$|[^0-9.])"
        count="$(grep -rIhE '^#{1,6} ' --include='*.md' "Documentation/" \
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
        report "G4" "no area file in scope exists yet" "expected $(expected_area_file)"
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
# --scope 20, --scope sec or --scope arc, and each later extraction adds its
# own pairing.
gate_verbatim() {
    local file old new difference

    case "$SCOPE" in
        20)  file="$AREA_DIR/UI.md" ;;
        sec) file="$AREA_DIR/Security.md" ;;
        arc) file="$AREA_DIR/Architecture.md" ;;
        *)
            report "VERBATIM" "defined per extraction, not for the whole document" \
                "run it with --scope 20, --scope sec or --scope arc"
            return
            ;;
    esac

    if [ ! -f "$file" ]; then
        report "VERBATIM" "no area file in scope exists yet" "expected $file"
        return
    fi

    if [ "$SCOPE" = "20" ]; then
        old="$(git show "$BASELINE:$DESIGN_DOC" \
            | sed -n '/^## 20\. /,/^## 21\. /p' \
            | sed '$d' \
            | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        # Everything below the rule closing the header block is the relocated
        # body. The heading annotation is dropped rather than checked here,
        # because G1 and G2 have already proved every one of them carries the
        # right old number.
        new="$(sed -n '/^---$/,$p' "$file" \
            | tail -n +2 \
            | sed -e '/./,$!d' \
            | sed -E 's/^(#{2,6} )UI(20[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\3/' \
            | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
            | sed -E 's/§EVN18/§10.17/g' \
            | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        difference="$(diff <(echo "$old") <(echo "$new") || true)"

        report "VERBATIM" "normalised differences against the baseline §20 body" "$difference"
        return
    fi

    if [ "$SCOPE" = "sec" ]; then
        # §14 and §18 are non-contiguous in G2H Design.md (§15-§17 stand between
        # them) but contiguous in Security.md, so the baseline body is two
        # ranges concatenated rather than one.
        old="$({
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 14\. /,/^## 15\. /p' | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 18\. /,/^## 19\. /p' | sed '$d'
        } | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        # §14.6 already cited `Events.md` as `§EVN18(d)` at the baseline — Events
        # had already been extracted, so that citation was never `§10.17` to begin
        # with and must not be reversed alongside the ones that were. It is
        # protected behind a placeholder for the run of the `§EVN18` -> `§10.17`
        # reversal below, then restored.
        new="$(sed -n '/^---$/,$p' "$file" \
            | tail -n +2 \
            | sed -e '/./,$!d' \
            | sed -E 's/^(#{2,6} )SEC((14|18)[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\4/' \
            | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
            | sed -E 's/§EVN18\(d\)/§XEVN18DX/g' \
            | sed -E 's/§EVN18/§10.17/g' \
            | sed -E 's/§XEVN18DX/§EVN18(d)/g' \
            | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        difference="$(diff <(echo "$old") <(echo "$new") || true)"

        report "VERBATIM" "normalised differences against the baseline §14/§18 body" "$difference"
        return
    fi

    # SCOPE = arc. §12, §16 and §17 are non-contiguous in G2H Design.md (§13-§15
    # stand between §12 and §16) but contiguous in Architecture.md, so the
    # baseline body is three ranges concatenated rather than one.
    old="$({
        git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 12\. /,/^## 13\. /p' | sed '$d'
        git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 16\. /,/^## 17\. /p' | sed '$d'
        git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 17\. /,/^## 18\. /p' | sed '$d'
    } | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

    # This body already carries fifteen `§EVN` occurrences resolved by an
    # earlier extraction, alongside the twenty-one `§10.x` citations this
    # extraction resolves the same way. Two of the six numbers this extraction
    # converts — `§10.17` -> `§EVN18` and `§10.18` -> `§EVN19` — land on a token
    # that a pre-existing citation already used, so the two are byte-identical
    # in the new file and a blind reversal cannot tell them apart by content.
    # Unlike the `sec` pairing above, this is resolved by normalising the
    # BASELINE forward rather than the new file backward: the forward direction
    # only ever rewrites a literal `§10.x`, and a pre-existing `§EVN18` or
    # `§EVN19` never matches that pattern, so it is never touched.
    new="$(sed -n '/^---$/,$p' "$file" \
        | tail -n +2 \
        | sed -e '/./,$!d' \
        | sed -E 's/^(#{2,6} )ARC((12|16|17)[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\4/' \
        | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
        | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

    old_forward="$(printf '%s\n' "$old" \
        | sed -E 's/§10\.2($|[^0-9])/§EVN2\1/g' \
        | sed -E 's/§10\.4($|[^0-9])/§EVN4\1/g' \
        | sed -E 's/§10\.5($|[^0-9])/§EVN5\1/g' \
        | sed -E 's/§10\.7($|[^0-9])/§EVN7\1/g' \
        | sed -E 's/§10\.17($|[^0-9])/§EVN18\1/g' \
        | sed -E 's/§10\.18($|[^0-9])/§EVN19\1/g')"

    difference="$(diff <(echo "$old_forward") <(echo "$new") || true)"

    report "VERBATIM" "normalised differences against the baseline §12/§16/§17 body" "$difference"
}

echo "design-split-audit — baseline $BASELINE, scope §$SCOPE, gate $GATE"
echo "  $(git log -1 --format='%ad  %s' --date=short "$BASELINE")"
echo "  resolved by $BASELINE_SOURCE; $(git rev-list --count "$BASELINE..HEAD") commit(s) under audit"
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
