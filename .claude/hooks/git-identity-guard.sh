#!/usr/bin/env bash
# Claude Code side of .githooks/identity-guard.sh — wired up in .claude/settings.json.
#
#   session-start  Points git at .githooks, and if git would commit as an AI tool,
#                  switches this repository to the signed-in person's identity.
#   pre-bash       Refuses a git command that would commit or push as an AI tool,
#                  carry AI attribution, or skip the git hooks.
#   pre-github     Refuses a GitHub pull request or commit whose text carries AI
#                  attribution.
#
# A non-zero exit of 2 blocks the tool call and shows the reason to Claude.

set -u

project_dir="${CLAUDE_PROJECT_DIR:-$(pwd)}"
guard="$project_dir/.githooks/identity-guard.sh"

block() {
    printf '%s\n' "$1" >&2
    exit 2
}

use_hooks() {
    git -C "$project_dir" config core.hooksPath .githooks 2>/dev/null
}

# The hook payload is one line of JSON; turn its escaped newlines back into lines
# so each message line is matched on its own.
read_payload() {
    sed 's/\\n/\n/g; s/\\r//g'
}

case "${1:-}" in
    session-start)
        use_hooks
        if ( cd "$project_dir" && bash "$guard" check-current ) >/dev/null 2>&1; then
            printf 'Git identity for commits: %s <%s>.\n' \
                "$(git -C "$project_dir" config user.name)" "$(git -C "$project_dir" config user.email)"
            exit 0
        fi

        email="${G2H_GIT_USER_EMAIL:-${CLAUDE_CODE_USER_EMAIL:-}}"
        name="${G2H_GIT_USER_NAME:-}"
        if [ -z "$name" ] && [ -n "$email" ]; then
            name=$(git -C "$project_dir" log --all -1 --format='%an' --author="<$email>" 2>/dev/null)
        fi

        if [ -n "$name" ] && [ -n "$email" ] \
            && bash "$guard" check-ident author "$name <$email>" >/dev/null 2>&1; then
            git -C "$project_dir" config user.name "$name"
            git -C "$project_dir" config user.email "$email"
            printf 'Git identity for commits set to %s <%s> for this repository. Never commit as an AI tool.\n' "$name" "$email"
            exit 0
        fi

        printf 'WARNING: git would commit as an AI tool and the signed-in person could not be resolved. '
        printf 'Every commit will be refused. Ask the user for the name and email to commit under, then run: '
        printf 'git config user.name "<name>" && git config user.email "<email>". '
        printf 'In a cloud environment, set G2H_GIT_USER_NAME and G2H_GIT_USER_EMAIL in the environment settings.\n'
        exit 0
        ;;

    pre-bash)
        payload=$(read_payload)
        git_write='git([[:space:]]+-[cC][[:space:]]+[^[:space:]]+)*[[:space:]]+(commit|commit-tree|push|merge|pull|rebase|cherry-pick|revert|am|tag|notes|filter-branch|filter-repo)([^a-z-]|$)'
        printf '%s\n' "$payload" | grep -Eq "$git_write" || exit 0

        use_hooks

        if printf '%s\n' "$payload" | grep -Eq -- '--no-verify|core\.hooksPath'; then
            block 'Refused: this repository does not allow skipping or redirecting its git hooks (--no-verify, core.hooksPath). They keep AI identities and attribution out of the history.'
        fi
        if printf '%s\n' "$payload" | grep -Eiq '(GIT_(AUTHOR|COMMITTER)_(NAME|EMAIL)|user\.(name|email))=[^[:space:]]*(claude|anthropic)|--author[= ]+["'"'"']?claude'; then
            block 'Refused: commits are never authored or committed as an AI tool. They are checked in under the identity of the person responsible for them.'
        fi
        if ! reason=$(printf '%s\n' "$payload" | GUARD_LABEL='this git command' bash "$guard" check-text 2>&1); then
            block "Refused: $reason"
        fi
        if ! reason=$( cd "$project_dir" && bash "$guard" check-current 2>&1 ); then
            block "Refused: $reason"
        fi
        exit 0
        ;;

    pre-github)
        if ! reason=$(read_payload | GUARD_LABEL='this pull request or commit' bash "$guard" check-text 2>&1); then
            block "Refused: $reason"
        fi
        exit 0
        ;;
esac

exit 0
