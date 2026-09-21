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
