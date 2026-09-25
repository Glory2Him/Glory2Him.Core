#!/usr/bin/env bash
# Keeps AI identities and AI attribution out of this repository's history.
#
# Every commit is checked in under the identity of the person who owns it — the
# person opening the PR is responsible for the code, so the history says so. No
# commit may be authored or committed as an AI tool (a "Claude" name, an
# @anthropic.com address), and no commit message or PR description may carry
# assistant attribution (Co-Authored-By trailers, session links, "Generated
# with" footers).
#
# One script, four callers: the git hooks in this directory, the Claude Code
# hooks in .claude/hooks, and the rejectAiAttribution job in
# .github/workflows/prLinter.yml.
#
# Usage:
#   identity-guard.sh check-ident <label> "<Name <email> ...>"
#   identity-guard.sh check-current            (the identity git would commit as)
#   identity-guard.sh check-message-file <file>
#   identity-guard.sh check-text               (reads stdin)
#   identity-guard.sh check-range <rev-list args...>

set -u

# A tool identity, as git prints it ("Name <email> 1700000000 +0000").
AI_IDENT_RE='(^|[[:space:]])claude([[:space:]]+code)?[[:space:]]*<|@anthropic\.com>|<noreply@anthropic\.com'

# Attribution a tool adds to a commit message or a PR description. A co-author
# trailer is matched within its own name and address, never past them, so that a
# human co-author followed on the same line by, say, a branch name is not refused.
AI_ATTRIBUTION_RE='co-authored-by[[:space:]]*[:=][^<>"'"'"']*(claude|anthropic)|co-authored-by[[:space:]]*[:=][^<>"'"'"']*<[^<>"'"'"']*anthropic|claude-session:|generated (with|by) \[?claude|claude\.ai/code/session|noreply@anthropic\.com'

fail() {
    printf 'identity-guard: %s\n' "$1" >&2
    printf 'identity-guard: commits are checked in under the identity of the person responsible for them.\n' >&2
    printf 'identity-guard: set it with: git config user.name "<Your Name>" && git config user.email "<you@example.com>"\n' >&2
    exit 1
}

check_ident() {
    label="$1"
    ident="$2"
    if [ -z "$ident" ]; then
        fail "no $label identity is configured."
    fi
    if printf '%s\n' "$ident" | grep -Eiq "$AI_IDENT_RE"; then
        fail "the $label identity is an AI tool ($(printf '%s' "$ident" | sed -E 's/ [0-9]+ [-+][0-9]{4}$//'))."
    fi
}

check_text() {
    label="$1"
    # Comment lines are stripped by git before the message is stored.
    if grep -v '^#' | grep -Eiq "$AI_ATTRIBUTION_RE"; then
        fail "$label carries AI attribution. Remove the Co-Authored-By / Claude-Session / \"Generated with\" lines."
    fi
}

command="${1:-}"
[ $# -gt 0 ] && shift

case "$command" in
    check-ident)
        check_ident "$1" "${2:-}"
        ;;
    check-current)
        check_ident author "$(git var GIT_AUTHOR_IDENT 2>/dev/null)"
        check_ident committer "$(git var GIT_COMMITTER_IDENT 2>/dev/null)"
        ;;
    check-message-file)
        check_text "the commit message" < "$1"
        ;;
    check-text)
        check_text "${GUARD_LABEL:-the text}"
        ;;
    check-range)
        idents=$(git log --format='%an <%ae>%n%cn <%ce>' "$@") || fail "could not read the commits in $*."
        offending=$(printf '%s\n' "$idents" | grep -Ei "$AI_IDENT_RE" | sort -u)
        if [ -n "$offending" ]; then
            fail "commits in $* are authored or committed as an AI tool: $(printf '%s' "$offending" | tr '\n' ';')"
        fi
        # A here-string, not a pipe: check_text must fail this shell, not a subshell.
        messages=$(git log --format='%B' "$@") || fail "could not read the commits in $*."
        check_text "a commit message in $*" <<<"$messages"
        ;;
    *)
        printf 'usage: %s check-ident|check-current|check-message-file|check-text|check-range\n' "$0" >&2
        exit 2
        ;;
esac

exit 0
