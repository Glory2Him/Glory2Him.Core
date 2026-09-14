#!/bin/bash
# ────────────────────────────────────────────────────────────────────────────────
# Completeness audit for the area-scoped design-document split (issue #481).
#
# `Documentation/G2H Design.md` was broken into area files under
# `Documentation/Design/`, one extraction per issue (#481, #552-#556). The split
# is complete. A move of load-bearing prose has exactly one failure mode worth
# automating against: a section that is dropped, or one that is invented. A
# read-through does not catch either, so this script remains the proof the
# split's gates asked for, re-runnable against a named baseline as an audit
# record of a completed migration.
#
# The surviving authority for the rules this script implements is
# `Documentation/G2H Design.md` §IDX1.5 (which area is in which file) and
# `DEVELOPERS.md`'s "The split, and why sections carry prefixes". The former
# rulings file that predated those two was deleted at `838ba82e`, and this
# script names none of its numbering below — every rule it stated that this
# script still needs has been restated here against the surviving authority.
#
# G1-G4 prove that no section was dropped or invented; G3's citation check is
# PARTIAL rather than exhaustive — see the `--gate` note below for why. G5 and
# G6 are standing exemptions, verified where G3's subtraction list is defined
# in code, and are subtracted by G3 rather than reported.
#
# Usage:
#   Tools/design-split-audit.sh [--baseline <ref>] [--scope <20|sec|arc|apr|dom|all>] [--gate <g1|g2|g3|g4|verbatim|all>]
#
# Examples:
#   Tools/design-split-audit.sh                        # every gate, scoped to §20
#   Tools/design-split-audit.sh --scope sec            # every gate, scoped to §14 and §18
#   Tools/design-split-audit.sh --scope arc            # every gate, scoped to §12, §16 and §17
#   Tools/design-split-audit.sh --scope apr            # every gate, scoped to §7, §8, §9 and §13
#   Tools/design-split-audit.sh --scope dom            # every gate, scoped to §2, §3, §4, §5, §6, §11, §19
#   Tools/design-split-audit.sh --scope all --baseline 854515f2
#                                                       # every gate but VERBATIM, whole document; --scope all
#                                                       # NEEDS the pre-split baseline passed explicitly (see
#                                                       # the --scope all note below) rather than the resolved
#                                                       # default, which drifts with every extraction that
#                                                       # landed. Now that the split is complete this is
#                                                       # expected to print an all-clear and exit 0.
#   Tools/design-split-audit.sh --gate g2 --scope all --baseline 854515f2
#                                                       # the whole-document completeness gate alone
#   Tools/design-split-audit.sh --gate verbatim --scope apr
#                                                       # the prose-identity proof alone, for one extraction
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
#                         `apr` is issue #554's extraction — §7, §8, §9 and §13,
#                         landing in `Approval.md` under the `APR` prefix; §10 to
#                         §12 stand between §9 and §13 in `G2H Design.md` but the
#                         four ranges are contiguous in `Approval.md`.
#                         `dom` is issue #555's extraction — §2, §3, §4, §5, §6,
#                         §11 and §19, landing in `Domain.md` under the `DOM`
#                         prefix; §7 to §10 stand between §6 and §11, and §12 to
#                         §18 stand between §11 and §19, in `G2H Design.md`, but
#                         the seven ranges are contiguous in `Domain.md`. This is
#                         the first scope whose section numbers are proper
#                         prefixes of other sections' numbers (`2` of `20`/`21`,
#                         `5` of `534`), so its G3 citation pattern is
#                         right-anchored against a following digit — see the note
#                         below.
#                         `all` is the whole-document mode. The split is
#                         complete, so `--scope all --baseline 854515f2` is now
#                         expected to report nothing and exit 0. It still needs
#                         the PRE-SPLIT baseline passed explicitly — the
#                         resolved default moves with `origin/main`, and once an
#                         extraction has merged its sections no longer stand in
#                         the "old" heading set a later baseline would read, so
#                         each already-extracted section would report as
#                         `invented` instead of the gate running clean. `all`
#                         also SKIPS VERBATIM rather than counting it as a
#                         failure: VERBATIM is defined per extraction (one
#                         baseline range against one area file), not for the
#                         whole document, so under `--scope all` there is
#                         nothing for it to diff. It still prints a line saying
#                         why it did not run, but that line does not add to the
#                         failure total.
#   --gate      all       G1-G4 prove that no section was dropped or invented.
#                         `verbatim` is the extra check #481 criterion 2 asks
#                         for — that no sentence was reworded inside the move —
#                         which no section-level gate can see. VERBATIM is now a
#                         DATED MOVE-TIME RECORD rather than a live gate: it
#                         proved each of the five moves at the time of that
#                         move, and all five extractions ran it green at their
#                         own scope; it is deliberately NOT re-baselined (that
#                         would diff the design against itself and report OK
#                         forever); a difference it reports from here on is a
#                         later legitimate edit to the area file, not a rewrite
#                         smuggled into a move; and the differences standing at
#                         any moment are not an inventory kept in this header —
#                         area files keep being edited below their `---` rule,
#                         so the set grows, and a reader who wants today's set
#                         runs the narrow scope rather than reading this file.
#
#                         G3's citation check is PARTIAL, not exhaustive: it
#                         builds its citation corpus by grepping the BASELINE
#                         tree, so a citation written after that baseline is
#                         invisible to it, and G1/G2/G4 compare against the
#                         pre-split snapshot at that same baseline. A green run
#                         is evidence that the split preserved meaning, not
#                         evidence that every citation in the repository
#                         resolves today.
#
# Exit code: 0 when every gate in scope reports nothing, 1 otherwise.
#
# Notes:
# - `Tools/design-split-audit.sh` excludes ITSELF from G3's citation grep. Its
#   own header and comments name section numbers in prose — "§12, §16 and §17",
#   "the seven ranges are contiguous" — and G3 greps every tracked, non-binary
#   file outside `Documentation/` for a citation-shaped token, which would
#   otherwise include this file. The instrument is not a consumer of the design
#   it audits.
# - `Documentation/Design/Events.md` IS an area file, but it renumbered rather
#   than prefix-preserving, and it carries sections (`EVN0`, `EVN10`, `EVN20`,
#   `EVN21`, `EVN22`) that had no former life in `G2H Design.md`. It therefore
#   counts for G2 and is exempt from G1 and G4.
# - KNOWN LIMITATION: a heading's `*(formerly §N)*` / `*(new; ...)*` annotation
#   can cite ANOTHER document's numbering, and G3's anchored resolution pattern
#   cannot tell that citation apart from an own-number anchor. `Events.md`
#   carries four annotations of this kind — `EVN10` (`*(new; corrects
#   EventSubstrate.md §5.10)*`), `EVN20` (`*(new; from EventSubstrate.md §2-3,
#   §30)*`), `EVN21` (`*(new; from EventSubstrate.md §34)*`) and `EVN23`
#   (`*(new; added as §10.19 before §10 moved here)*`). Between them they carry
#   five section-signed tokens — `§5.10`, `§2`, `§30`, `§34`, `§10.19` — and
#   exactly one, `§2`, also resolves against a real anchor of this design
#   (`## DOM2. Domain Model Overview *(formerly §2)*`). `DOM2` is the one
#   collision; the other four each resolve to exactly one heading too (the
#   `EVN` annotation itself, or nothing), so a citation of one of them would
#   pass while pointing at the wrong section — a silent failure this gate
#   cannot detect. None of the five is cited from outside `Documentation/`
#   today, so nothing fails at `--baseline 854515f2`. This is accepted rather
#   than fixed: the repair, the day one does bite, is to move the foreign
#   citation off the heading line into the body, not to add a branch to the
#   gate.
# - Greps for an old number are RIGHT-ANCHORED — `§20\.6($|[^0-9.])`. An
#   unanchored `§20.6` matches the `§20.6.1` heading too, and every parent number
#   then reports a false duplicate against its own children.
# - `--scope dom`'s G3 citation-extraction pattern is ALSO right-anchored against
#   a following DIGIT (`git grep -P`, a lookahead), which none of the other four
#   scopes needed. `2`, `3`, `4`, `5`, `6`, `11` and `19` are the first section
#   numbers in this split that are themselves proper prefixes of other sections'
#   numbers — no number in `20|14|18|12|16|17|7|8|9|13` is a prefix of a longer
#   one, so those four scopes were right by luck rather than by anchoring.
#   Unanchored, `§2` matches inside `§20` and `§21`, and `§5` matches inside
#   `§534` (the mis-sigiled issue number G6 already exempts) — phantom tokens
#   that still resolve to exactly one heading each, so this is a correctness fix
#   to what the gate measures, not a red-to-green fix for a prior finding.
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
#   Forward normalisation has no such ambiguity — it rewrites a literal `§10.x`
#   (never a `§EVN...` that was already there) and the two `](Design/Events.md)`
#   links (#553 criterion 2's fifth permitted difference) — so it is used instead.
# - `--scope apr`'s VERBATIM pairing normalises the BASELINE forward for the same
#   reason. §7/§8/§9/§13 carry thirteen pre-split `§10.x` citations into event
#   design (`§10.2`, `§10.5`, `§10.7`, `§10.17`, and the lettered `§10.17(a)`)
#   alongside six `§EVN` occurrences (`§EVN2`, `§EVN18`, `§EVN18(e)`, `§EVN19`,
#   `§EVN23` twice) that an earlier extraction already resolved. Two of the four
#   numbers this extraction converts — `§10.2`→`§EVN2` and `§10.17`→`§EVN18` —
#   land on a token a pre-existing citation already used, so the two forms are
#   byte-identical once written and a backward reversal cannot tell them apart.
#   Unlike `--scope arc`, this body carries no markdown link at all, so the
#   link-target rewrite that pairing needs has no counterpart here.
# - `--scope dom`'s VERBATIM pairing also normalises the BASELINE forward, for
#   consistency with `--scope arc` and `--scope apr` rather than necessity: the
#   DOM body carries exactly one `§10.x` citation (`§10.4`, in §5.6.4) and no
#   pre-existing `§EVN` occurrence, so there is no output-token collision and a
#   backward reversal would be unambiguous too. `§10.4` resolves to `§EVN4` —
#   looked up in `Events.md`'s own *(formerly §10.4)* annotation, never derived.
#   The body carries one markdown link (`](/media/{attachmentId})`, §5.6.6, an
#   absolute site path inside inline code), which needs no rewrite: it is
#   unaffected by the prose moving one directory down, so this pairing needs no
#   link-target rule.
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

case "$SCOPE" in 20|sec|arc|apr|dom|all) ;; *) echo "--scope must be 20, sec, arc, apr, dom or all" >&2; exit 2 ;; esac
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
    # The one file a narrow scope resolves to, named for a "not extracted yet"
    # message. Meaningless under --scope all, which resolves to every area file.
    case "$SCOPE" in
        20)  echo "$AREA_DIR/UI.md" ;;
        sec) echo "$AREA_DIR/Security.md" ;;
        arc) echo "$AREA_DIR/Architecture.md" ;;
        apr) echo "$AREA_DIR/Approval.md" ;;
        dom) echo "$AREA_DIR/Domain.md" ;;
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
    elif [ "$SCOPE" = "apr" ]; then
        grep -E '^(7|8|9|13)($|\.)'
    elif [ "$SCOPE" = "dom" ]; then
        grep -E '^(2|3|4|5|6|11|19)($|\.)'
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
        # §1 and §10 are retained in the index; §15 and §21 are retired.
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
    local pattern citations body="" grep_flag="-E"

    if [ "$SCOPE" = "20" ]; then
        pattern='§20[0-9.]*'
    elif [ "$SCOPE" = "sec" ]; then
        pattern='§(14|18)(\.[0-9]+)*'
    elif [ "$SCOPE" = "arc" ]; then
        pattern='§(12|16|17)(\.[0-9]+)*'
    elif [ "$SCOPE" = "apr" ]; then
        pattern='§(7|8|9|13)(\.[0-9]+)*'
    elif [ "$SCOPE" = "dom" ]; then
        # 2, 3, 4, 5, 6, 11 and 19 are proper prefixes of other section numbers
        # (20, 21, 534, ...), which the other four scopes never had to be safe
        # against. A `-E` alternation stops at the first matching alternative —
        # `§2` inside `§20` — rather than continuing to consume the trailing
        # digit, so extraction here needs a real lookahead: `-P`, checked
        # against a following digit.
        pattern='§(2|3|4|5|6|11|19)(\.[0-9]+)*(?![0-9])'
        grep_flag="-P"
    else
        pattern='§[0-9]+(\.[0-9]+)*'
    fi

    # `-I` excludes binary files. Without it `git grep` emits a `Binary file
    # <rev>:<path> matches` line into the citation stream, each of which is then
    # judged as a citation and reported as resolving to no heading — a finding the
    # whole-document mode could never be run to empty.
    #
    # `Tools/design-split-audit.sh` is excluded too. It is not a consumer of
    # the design it audits, but its own header and inline comments name
    # section numbers in prose — "§12, §16 and §17", "the seven ranges are
    # contiguous" — which would otherwise pollute this corpus with tokens that
    # cite nothing but this file's own commentary.
    citations="$(git grep -I -h -o "$grep_flag" "$pattern" "$BASELINE" -- . \
            ':(exclude)Documentation/' ':(exclude)Tools/design-split-audit.sh' \
        | sort -u -V)"

    if [ "$SCOPE" = "all" ] || [ "$SCOPE" = "arc" ]; then
        # §12.4.7 dangled before the split, and the split neither creates nor
        # repairs it. §12.4.4 was `ApprovalOrchestrationService`; commit
        # 757a8100 renumbered that section to §ARC12.5.3, and its citers
        # (`ApprovalEntityProbeReadTests.cs`,
        # `AssociationServiceTests.TransitionApproval.Validations.cs`,
        # `ApprovalEntityMatch.cs` twice, `IApprovalService.cs`,
        # `IAssociationService.cs`, and `G2H Design.md` §9.7.x) were repointed
        # to §ARC12.5.3 at issue #559 — but this scope's corpus is built from
        # `$BASELINE`, the pre-split tree, where §12.4.4 still appears, so the
        # number survives here regardless of the repointing. Both are
        # subtracted rather than reported: a gate nobody can run green stops
        # being run.
        citations="$(echo "$citations" | grep -Ev '^§(12\.4\.7|12\.4\.4)$')"
    fi

    if [ "$SCOPE" = "all" ]; then
        # §10 is out of G3's scope by design. §1.1.3, §1.8 and §534 are G6
        # exemptions: none of the three cites this design.
        #
        # §1.1.3 is The Standard's exception rule.
        #
        # §1.8 is `exposer skill §1.8` at three sites —
        # `ApprovalReviewTests.NestedNavigation.cs:29`,
        # `ApprovalReviewTests.OwnerOnly.cs:182` and
        # `ApiBroker.ApprovalReviews.cs:49` — citing
        # `.claude/skills/the-standard-exposers/SKILL.md` 1.8, not this
        # document. §1 runs §1.1 to §1.5 here (§IDX1.1-§IDX1.5) and has no
        # §1.8 of its own, so there is nothing for the split to have preserved
        # or broken. All three sites stand byte-identical at baseline
        # `854515f2`, so this is pre-existing rather than caused by the split.
        #
        # §534 is a mis-sigiled issue number.
        citations="$(echo "$citations" | grep -Ev '^§10($|\.)' \
            | grep -Ev '^§(1\.1\.3|1\.8|534)$')"
    fi

    while IFS= read -r citation; do
        [ -z "$citation" ] && continue
        local number anchored count
        number="${citation#§}"
        anchored="§$(echo "$number" | sed 's/\./\\./g')(\$|[^0-9.])"
        count="$(grep -rIE '^#{1,6} ' --include='*.md' "Documentation/" \
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
# --scope 20, --scope sec, --scope arc or --scope apr, and each later
# extraction adds its own pairing.
gate_verbatim() {
    local file old new difference

    case "$SCOPE" in
        20)  file="$AREA_DIR/UI.md" ;;
        sec) file="$AREA_DIR/Security.md" ;;
        arc) file="$AREA_DIR/Architecture.md" ;;
        apr) file="$AREA_DIR/Approval.md" ;;
        dom) file="$AREA_DIR/Domain.md" ;;
        *)
            # `--scope all` deliberately does NOT call `report` here: VERBATIM
            # is defined per extraction, not for the whole document, so there
            # is nothing to diff at this scope — not running is not a finding,
            # and must not add to FAILURES the way an empty `body` given to
            # `report` would.
            echo "VERBATIM: not applicable at --scope all"
            echo "    defined per extraction, not for the whole document — run it with" \
                "--scope 20, --scope sec, --scope arc, --scope apr or --scope dom"
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

    if [ "$SCOPE" = "apr" ]; then
        # §7, §8, §9 and §13 are non-contiguous in G2H Design.md (§10-§12
        # stand between §9 and §13) but contiguous in Approval.md, so the
        # baseline body is four ranges concatenated rather than one.
        old="$({
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 7\. /,/^## 8\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 8\. /,/^## 9\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 9\. /,/^## 10\. /p'  | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 13\. /,/^## 14\. /p' | sed '$d'
        } | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        new="$(sed -n '/^---$/,$p' "$file" \
            | tail -n +2 \
            | sed -e '/./,$!d' \
            | sed -E 's/^(#{2,6} )APR((7|8|9|13)[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\4/' \
            | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
            | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        # As with --scope arc, the four `§10.x` conversions are normalised on
        # the BASELINE side rather than reversed on the new side: `§10.2` ->
        # `§EVN2` and `§10.17` -> `§EVN18` both land on a token this body
        # already carried before the extraction, so a backward reversal
        # cannot tell the two forms apart. The right anchor is `($|[^0-9])`
        # rather than `($|[^0-9.])` so that the lettered `§10.17(a)` is
        # converted too rather than left behind. This body carries no
        # markdown link, so it needs no link-target rule.
        old_forward="$(printf '%s\n' "$old" \
            | sed -E 's/§10\.2($|[^0-9])/§EVN2\1/g' \
            | sed -E 's/§10\.5($|[^0-9])/§EVN5\1/g' \
            | sed -E 's/§10\.7($|[^0-9])/§EVN7\1/g' \
            | sed -E 's/§10\.17($|[^0-9])/§EVN18\1/g')"

        difference="$(diff <(echo "$old_forward") <(echo "$new") || true)"

        report "VERBATIM" "normalised differences against the baseline §7/§8/§9/§13 body" "$difference"
        return
    fi

    if [ "$SCOPE" = "dom" ]; then
        # §2, §3, §4, §5, §6, §11 and §19 are non-contiguous in G2H Design.md
        # (§7-§10 stand between §6 and §11, §12-§18 between §11 and §19) but
        # contiguous in Domain.md, so the baseline body is seven ranges
        # concatenated rather than one, in section order.
        old="$({
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 2\. /,/^## 3\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 3\. /,/^## 4\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 4\. /,/^## 5\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 5\. /,/^## 6\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 6\. /,/^## 7\. /p'   | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 11\. /,/^## 12\. /p' | sed '$d'
            git show "$BASELINE:$DESIGN_DOC" | sed -n '/^## 19\. /,/^## 20\. /p' | sed '$d'
        } | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        new="$(sed -n '/^---$/,$p' "$file" \
            | tail -n +2 \
            | sed -e '/./,$!d' \
            | sed -E 's/^(#{2,6} )DOM((2|3|4|5|6|11|19)[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\4/' \
            | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
            | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

        # Only one pre-split `§10.x` citation stands in this body — `§10.4`, in
        # §5.6.4 — and no pre-existing `§EVN` occurrence, so unlike `--scope arc`
        # and `--scope apr` there is no output-token collision here; forward
        # normalisation is used anyway, for consistency with those two rather
        # than necessity. This body carries no relative `Events.md` link either,
        # so it needs no link-target rule.
        old_forward="$(printf '%s\n' "$old" \
            | sed -E 's/§10\.4($|[^0-9])/§EVN4\1/g')"

        difference="$(diff <(echo "$old_forward") <(echo "$new") || true)"

        report "VERBATIM" "normalised differences against the baseline §2/§3/§4/§5/§6/§11/§19 body" "$difference"
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
    # rewrites a literal `§10.x` (a pre-existing `§EVN18` or `§EVN19` never
    # matches that pattern, so it is never touched) and, separately below, the
    # two relative `Events.md` links.
    new="$(sed -n '/^---$/,$p' "$file" \
        | tail -n +2 \
        | sed -e '/./,$!d' \
        | sed -E 's/^(#{2,6} )ARC((12|16|17)[0-9.]*)( .*) \*\(formerly §[0-9][0-9.]*\)\*$/\1\2\4/' \
        | sed -E 's/§(DOM|APR|ARC|SEC|UI)([0-9])/§\2/g' \
        | sed -e :a -e '/^[[:space:]]*$/{$d;N;ba' -e '}')"

    # The body also carries two relative links to `Events.md`. At the baseline
    # they resolved from `Documentation/`, so they read `](Design/Events.md)`;
    # the prose now sits one directory down in `Documentation/Design/`, so the
    # identical target would 404, and the corrected file reads `](Events.md)`
    # instead (#553 criterion 2's fifth permitted difference). The baseline
    # carries no occurrence of the bare `](Events.md)` form, so — as with the
    # `§10.17`/`§10.18` collision above — this is resolved by normalising the
    # baseline forward rather than the new file backward, and the direction is
    # unambiguous either way. This matches the link target only, never the link
    # text or an anchor, so a rewrite of either still fails the diff below.
    old_forward="$(printf '%s\n' "$old" \
        | sed -E 's/§10\.2($|[^0-9])/§EVN2\1/g' \
        | sed -E 's/§10\.4($|[^0-9])/§EVN4\1/g' \
        | sed -E 's/§10\.5($|[^0-9])/§EVN5\1/g' \
        | sed -E 's/§10\.7($|[^0-9])/§EVN7\1/g' \
        | sed -E 's/§10\.17($|[^0-9])/§EVN18\1/g' \
        | sed -E 's/§10\.18($|[^0-9])/§EVN19\1/g' \
        | sed -E 's/\]\(Design\/Events\.md\)/](Events.md)/g')"

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
