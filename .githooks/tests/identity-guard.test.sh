#!/usr/bin/env bash
# Drives every layer of the identity guard end to end: .githooks/identity-guard.sh
# and the git hooks that call it, the Claude Code hooks in
# .claude/hooks/git-identity-guard.sh, and the rejectAiAttribution job in
# .github/workflows/prLinter.yml. Each test builds its own scratch repositories
# in a temporary directory, isolated from the caller's git configuration, and
# nothing is left behind.
#
# Usage: bash .githooks/tests/identity-guard.test.sh [TestName ...]
# Exits non-zero when any test fails. CI runs it from rejectAiAttribution.

set -u

root=$(cd "$(dirname "$0")/../.." && pwd -P)
guard="$root/.githooks/identity-guard.sh"
hook="$root/.claude/hooks/git-identity-guard.sh"
workflow="$root/.github/workflows/prLinter.yml"
settings="$root/.claude/settings.json"

scratch=$(mktemp -d "${TMPDIR:-/tmp}/identity-guard-test.XXXXXX")
trap 'rm -rf "$scratch"' EXIT

# Isolate every git call from the caller's configuration and identity.
export HOME="$scratch/home"
export GIT_CONFIG_NOSYSTEM=1
export GIT_CONFIG_GLOBAL="$HOME/.gitconfig"
mkdir -p "$HOME"
unset GIT_AUTHOR_NAME GIT_AUTHOR_EMAIL GIT_AUTHOR_DATE GIT_COMMITTER_NAME \
    GIT_COMMITTER_EMAIL GIT_COMMITTER_DATE EMAIL GIT_CONFIG_PARAMETERS \
    GIT_CONFIG_COUNT GIT_DIR GIT_WORK_TREE GIT_INDEX_FILE CLAUDE_PROJECT_DIR \
    CLAUDE_CODE_USER_EMAIL G2H_GIT_USER_NAME G2H_GIT_USER_EMAIL GUARD_LABEL
git config --global init.defaultBranch main
git config --global advice.detachedHead false
git config --global core.autocrlf false

# Built at run time, so that this file carries no attribution for any scanner.
tool_name='Clau''de'
tool_lower='clau''de'
tool_email="noreply@anthro""pic.com"
tool_ident="$tool_name <$tool_email>"
trailer="Co-Authored-""By: $tool_ident"
session_link="$tool_name-Session: https://$tool_lower.ai/code/session_01AbCdEf"
footer="Generated with [$tool_name Code](https://$tool_lower.com/$tool_lower-code)"

person_name='Jane Person'
person_email='jane.person@example.com'
person="$person_name <$person_email>"

failed_tests=0
test_failed=0

# ---------------------------------------------------------------- assertions

fail_check() {
    test_failed=1
    printf '    FAILED: %s\n' "$1"
    if [ -s "$scratch/out" ]; then sed -e 's/^/      | /' "$scratch/out" | head -8; fi
}

# allowed/refused <description> <command...>: the command must exit 0 / non-zero.
allowed() {
    desc=$1; shift
    if "$@" >"$scratch/out" 2>&1; then :; else fail_check "expected allowed: $desc (exit $?)"; fi
}
refused() {
    desc=$1; shift
    if "$@" >"$scratch/out" 2>&1; then fail_check "expected refused: $desc"; fi
}

# expect_exit <code> <description> <command...>: the command must exit with <code>.
expect_exit() {
    code=$1 desc=$2; shift 2
    "$@" >"$scratch/out" 2>&1
    actual=$?
    [ "$actual" = "$code" ] || fail_check "expected exit $code, got $actual: $desc"
}

assert_equal() {
    [ "$2" = "$3" ] || { : >"$scratch/out"; fail_check "$1: expected '$2', got '$3'"; }
}

assert_contains() {
    case "$3" in
        *"$2"*) ;;
        *) printf '%s\n' "$3" >"$scratch/out"; fail_check "$1: expected output containing '$2'" ;;
    esac
}

# --------------------------------------------------------------- scratch repos

# A repository set up the way a clone of this one is: hooks on, a person's identity.
new_repo() {
    git init -q "$1"
    install_hooks "$1"
    git -C "$1" config user.name "$person_name"
    git -C "$1" config user.email "$person_email"
}

install_hooks() {
    cp -R "$root/.githooks" "$1/.githooks"
    printf '.githooks/\n' >>"$1/.git/info/exclude"
    git -C "$1" config core.hooksPath .githooks
}

clone_repo() {
    git clone -q "$1" "$2" 2>/dev/null
    install_hooks "$2"
    git -C "$2" config user.name "$person_name"
    git -C "$2" config user.email "$person_email"
}

# raw_commit <dir> <author> <committer> <message>: commits on HEAD through the
# plumbing, which runs no hook, and prints the new commit.
raw_commit() {
    dir=$1 author=$2 committer=$3 message=$4
    tree=$(git -C "$dir" write-tree)
    parent=$(git -C "$dir" rev-parse -q --verify HEAD)
    sha=$(printf '%s\n' "$message" | \
        GIT_AUTHOR_NAME="${author% <*}" GIT_AUTHOR_EMAIL="$(ident_email "$author")" \
        GIT_COMMITTER_NAME="${committer% <*}" GIT_COMMITTER_EMAIL="$(ident_email "$committer")" \
        git -C "$dir" commit-tree "$tree" ${parent:+-p "$parent"})
    git -C "$dir" update-ref HEAD "$sha"
    printf '%s\n' "$sha"
}

ident_email() {
    email=${1#*<}
    printf '%s' "${email%>}"
}

# ------------------------------------------------------------- hook payloads

json_string() {
    s=$1
    s=${s//\\/\\\\}
    s=${s//\"/\\\"}
    s=${s//$'\n'/\\n}
    s=${s//$'\r'/\\r}
    s=${s//$'\t'/\\t}
    printf '"%s"' "$s"
}

bash_payload() {
    printf '{"session_id":"test","hook_event_name":"PreToolUse","cwd":%s,"tool_name":"Bash","tool_input":{"command":%s,"description":%s}}' \
        "$(json_string "$session")" "$(json_string "$1")" "$(json_string "${2:-Runs a command}")"
}

# pre_bash <command> [description]: runs the session hook over a Bash call in $session.
pre_bash() {
    bash_payload "$@" | CLAUDE_PROJECT_DIR="$session" bash "$hook" pre-bash
}

bash_allows() { expect_exit 0 "pre-bash allows: $1" pre_bash "$@"; }
bash_blocks() { expect_exit 2 "pre-bash blocks: $1" pre_bash "$@"; }

# pre_github <tool> <tool_input JSON>
pre_github() {
    printf '{"session_id":"test","hook_event_name":"PreToolUse","tool_name":"mcp__github__%s","tool_input":%s}' "$1" "$2" | \
        CLAUDE_PROJECT_DIR="$session" bash "$hook" pre-github
}

github_allows() { expect_exit 0 "pre-github allows $1: $2" pre_github "$@"; }
github_blocks() { expect_exit 2 "pre-github blocks $1: $2" pre_github "$@"; }

# A session's project: hooks available, a person's identity, one commit.
new_session() {
    session="$scratch/$1"
    new_repo "$session"
    git -C "$session" commit -q --allow-empty -m 'Start'
}

# ------------------------------------------------------------------- the CI job

# The run lines of the rejectAiAttribution job, in order, other than this test.
ci_run_lines() {
    sed -n '/^  rejectAiAttribution:/,/^  [A-Za-z][A-Za-z]*:$/p' "$workflow" | \
        sed -n 's/^ *run: //p' | grep -v 'identity-guard\.test\.sh'
}

# ci_job <dir>: runs every step of the job in <dir>, as a checked-out PR head.
ci_job() {
    ( cd "$1" && ci_run_lines | while IFS= read -r line; do
        bash -c "$line" || exit 1
    done )
}

# ======================================================================= tests

ShouldRefuseOnlyAToolIdentityOnCheckIdent() {
    refused 'the tool identity' bash "$guard" check-ident author "$tool_ident"
    refused 'a tool name' bash "$guard" check-ident author "$tool_name Code <someone@example.com>"
    refused 'a tool address' bash "$guard" check-ident author "Someone <someone@anthro""pic.com>"
    refused 'no identity' bash "$guard" check-ident author ''
    allowed 'a person' bash "$guard" check-ident author "$person"
    allowed 'a person named Claude Monet' bash "$guard" check-ident author "$tool_name Monet <cm@example.com>"
    allowed 'a person whose address holds the word' bash "$guard" check-ident author "Jean <jean.$tool_lower@example.fr>"
    allowed 'a person named Claudette' bash "$guard" check-ident author "${tool_name}tte <c@example.com>"
}

ShouldRefuseAToolIdentityOnCheckCurrent() {
    repo="$scratch/current"
    new_repo "$repo"
    allowed 'a person configured' bash -c "cd '$repo' && bash '$guard' check-current"
    refused 'a tool author from the environment' \
        bash -c "cd '$repo' && GIT_AUTHOR_NAME='$tool_name' bash '$guard' check-current"
    refused 'a tool committer from the environment' \
        bash -c "cd '$repo' && GIT_COMMITTER_EMAIL='$tool_email' bash '$guard' check-current"
    git -C "$repo" config user.name "$tool_name"
    git -C "$repo" config user.email "$tool_email"
    refused 'a tool configured' bash -c "cd '$repo' && bash '$guard' check-current"
}

ShouldRefuseAToolIdentityOnPreCommit() {
    repo="$scratch/pre-commit"
    new_repo "$repo"
    allowed 'a person' git -C "$repo" commit -q --allow-empty -m 'By a person'
    refused '-c user.name' git -C "$repo" -c user.name="$tool_name" commit -q --allow-empty -m 'x'
    refused '--author' git -C "$repo" commit -q --allow-empty --author="$tool_ident" -m 'x'
    refused 'the environment' env GIT_AUTHOR_EMAIL="$tool_email" git -C "$repo" commit -q --allow-empty -m 'x'
    git -C "$repo" config user.name "$tool_name"
    git -C "$repo" config user.email "$tool_email"
    refused 'a tool configured' git -C "$repo" commit -q --allow-empty -m 'x'
    assert_equal 'commits made' 1 "$(git -C "$repo" rev-list --count HEAD)"
}

ShouldRefuseAnAttributionTrailerOnCommitMsg() {
    repo="$scratch/commit-msg"
    new_repo "$repo"
    refused 'a co-author trailer' git -C "$repo" commit -q --allow-empty -m 'x' -m "$trailer"
    refused 'a session link' git -C "$repo" commit -q --allow-empty -m 'x' -m "$session_link"
    refused 'a footer' git -C "$repo" commit -q --allow-empty -m 'x' -m "$footer"
    printf 'x\r\n\r\n%s\r\n' "$trailer" >"$scratch/crlf-message"
    refused 'a CRLF message' git -C "$repo" commit -q --allow-empty -F "$scratch/crlf-message"
    allowed 'a human co-author' git -C "$repo" commit -q --allow-empty -m 'x' -m "Co-Authored-By: $person"
    assert_equal 'commits made' 1 "$(git -C "$repo" rev-list --count HEAD)"
}

ShouldRefuseAToolAuthoredCommitOnCheckRange() {
    repo="$scratch/check-range"
    new_repo "$repo"
    base=$(raw_commit "$repo" "$person" "$person" 'Base')
    raw_commit "$repo" "$person" "$person" 'By a person' >/dev/null
    allowed 'commits by a person' bash -c "cd '$repo' && bash '$guard' check-range '$base..HEAD'"
    raw_commit "$repo" "$tool_ident" "$person" 'Tool-authored' >/dev/null
    refused 'a tool-authored commit' bash -c "cd '$repo' && bash '$guard' check-range '$base..HEAD'"
    git -C "$repo" update-ref HEAD HEAD~1
    raw_commit "$repo" "$person" "$tool_ident" 'Tool-committed' >/dev/null
    refused 'a tool-committed commit' bash -c "cd '$repo' && bash '$guard' check-range '$base..HEAD'"
}

ShouldRefuseATrailerOnlyCommitOnCheckRangeAndPrePush() {
    remote="$scratch/trailer-remote.git"
    git init -q --bare "$remote"
    repo="$scratch/trailer"
    clone_repo "$remote" "$repo"
    base=$(raw_commit "$repo" "$person" "$person" 'Base')
    git -C "$repo" push -q origin HEAD:main
    raw_commit "$repo" "$person" "$person" "By a person

$trailer" >/dev/null
    refused 'check-range on a co-author trailer' bash -c "cd '$repo' && bash '$guard' check-range '$base..HEAD'"
    refused 'pre-push of a co-author trailer' git -C "$repo" push -q origin HEAD:main
    git -C "$repo" update-ref HEAD "$base"
    raw_commit "$repo" "$person" "$person" "By a person

$session_link" >/dev/null
    refused 'check-range on a session link' bash -c "cd '$repo' && bash '$guard' check-range '$base..HEAD'"
    refused 'pre-push of a session link on a new branch' git -C "$repo" push -q origin HEAD:refs/heads/trailer-branch
    assert_equal 'remote main untouched' "$base" "$(git -C "$remote" rev-parse main)"
    refused 'no trailer branch published' git -C "$remote" rev-parse -q --verify trailer-branch
}

ShouldRefuseAToolAuthoredCommitOnPrePush() {
    remote="$scratch/push-remote.git"
    git init -q --bare "$remote"
    repo="$scratch/push"
    clone_repo "$remote" "$repo"
    git -C "$repo" commit -q --allow-empty -m 'Base'
    allowed 'a person pushing main' git -C "$repo" push -q origin main

    git -C "$repo" checkout -q -b tool-branch
    raw_commit "$repo" "$tool_ident" "$tool_ident" 'Tool-authored' >/dev/null
    refused 'a new branch carrying a tool commit' git -C "$repo" push -q origin tool-branch

    git -C "$repo" checkout -q main
    raw_commit "$repo" "$tool_ident" "$person" 'Tool-authored on main' >/dev/null
    refused 'an updated branch carrying a tool commit' git -C "$repo" push -q origin main

    git -C "$repo" reset -q --hard origin/main
    git -C "$repo" commit -q --allow-empty -m 'By a person'
    allowed 'an updated branch by a person' git -C "$repo" push -q origin main
    git -C "$repo" update-ref HEAD HEAD~1
    raw_commit "$repo" "$tool_ident" "$tool_ident" 'Rewritten as a tool' >/dev/null
    refused 'rewritten history carrying a tool commit' git -C "$repo" push -q --force origin main
}

ShouldPushPublishedToolHistoryMergedIn() {
    remote="$scratch/published-remote.git"
    git init -q --bare "$remote"
    seed="$scratch/published-seed"
    git clone -q "$remote" "$seed" 2>/dev/null
    raw_commit "$seed" "$person" "$person" 'Base' >/dev/null
    raw_commit "$seed" "$tool_ident" "$tool_ident" 'Tool-authored, already on main' >/dev/null
    git -C "$seed" push -q origin HEAD:main

    repo="$scratch/published"
    clone_repo "$remote" "$repo"
    git -C "$repo" checkout -q -b feature
    git -C "$repo" commit -q --allow-empty -m 'By a person'
    allowed 'a new branch cut from main' git -C "$repo" push -q origin feature

    raw_commit "$seed" "$tool_ident" "$tool_ident" 'Tool-authored, merged to main later' >/dev/null
    git -C "$seed" push -q origin HEAD:main
    git -C "$repo" fetch -q origin
    git -C "$repo" merge -q --no-edit origin/main
    allowed 'a merge of main' git -C "$repo" push -q origin feature
    allowed 'the same branch under a new name' git -C "$repo" push -q origin feature:feature-renamed
    git -C "$repo" tag -a v1 -m 'A tag' origin/main
    allowed 'an annotated tag on main' git -C "$repo" push -q origin v1
}

ShouldRefuseTheCanonicalFormsOnPreBash() {
    new_session canonical
    bash_blocks "git commit -m x -m \"$trailer\""
    bash_blocks "git commit -m x -m \"$session_link\""
    bash_blocks "git commit -m x -m \"$footer\""
    bash_blocks 'git commit --no-verify -m x'
    bash_blocks 'git -c core.hooksPath=/dev/null commit -m x'
    bash_blocks "GIT_AUTHOR_NAME=$tool_name git commit -m x"
    bash_blocks "git commit --author=$tool_name -m x"
    bash_blocks "git -c user.name=$tool_name commit -m x"
    bash_allows 'git commit -m "Add the thing"'
    bash_allows 'git status'
    bash_allows 'ls -la'
    git -C "$session" config --unset core.hooksPath
    bash_allows 'git push origin main'
    assert_equal 'hooks pointed at .githooks' .githooks "$(git -C "$session" config core.hooksPath)"
    git -C "$session" config user.name "$tool_name"
    bash_blocks 'git commit -m x'
}

ShouldJudgeEveryIdentityOverrideByTheSharedRuleOnPreBash() {
    new_session overrides
    # People whose names or addresses merely contain the word: check-ident allows them.
    bash_allows "git commit --author='$tool_name Monet <cm@example.com>' -m x"
    bash_allows "GIT_AUTHOR_EMAIL=jean.$tool_lower@example.fr git commit -m x"
    bash_allows "git -c user.name=${tool_name}tte commit -m x"
    bash_allows "git config user.name '$tool_name Monet'"
    # Tool identities, however they are spelled: check-ident refuses them.
    bash_blocks "git commit --author=\"$tool_name <$tool_lower@example.com>\" -m x"
    bash_blocks "git commit --author \"$tool_ident\" -m x"
    bash_blocks "git -c user.name=\"$tool_name Code\" commit -m x"
    bash_blocks "git -c \"user.name=$tool_name Code\" commit -m x"
    bash_blocks "git -c USER.EMAIL=$tool_email commit -m x"
    bash_blocks "git -c author.name=$tool_name commit -m x"
    bash_blocks "GIT_COMMITTER_EMAIL=someone@anthro""pic.com git commit -m x"
    bash_blocks "export GIT_AUTHOR_NAME='$tool_name Code'; git commit -m x"
    bash_blocks "env GIT_AUTHOR_NAME=$tool_name git commit -m x"
    bash_blocks "GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=user.name GIT_CONFIG_VALUE_0=$tool_name git commit -m x"
    bash_blocks "GIT_CONFIG_PARAMETERS=\"'user.email'='$tool_email'\" git commit -m x"
    bash_blocks "git config user.email $tool_email"
    bash_blocks "git config --local user.name \"$tool_name Code\""
    bash_blocks "git -c user.name=\"$tool_name Code\" -c user.email=$tool_email commit -n -m x -m \"$trailer\""
}

ShouldRefuseEveryFormThatSkipsTheHooksOnPreBash() {
    new_session skips
    bash_blocks 'git commit -n -m x'
    bash_blocks 'git commit -anm x'
    bash_blocks 'git commit -qn -m x'
    bash_blocks 'git commit --no-verif -m x'
    bash_blocks 'git commit --no-ve -m x'
    bash_blocks 'git push --no-verify'
    bash_blocks 'git merge --no-verify feature'
    bash_blocks 'git -c core.hookspath=/dev/null commit -m x'
    bash_blocks 'git -c CORE.HOOKSPATH=/dev/null push'
    bash_blocks 'git -c "core.hooksPath=/dev/null" push'
    bash_blocks 'git --config-env=core.hooksPath=HOOKS push'
    bash_blocks 'GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.hookspath GIT_CONFIG_VALUE_0=/dev/null git commit -m x'
    bash_blocks "GIT_CONFIG_PARAMETERS=\"'core.hooksPath'='/dev/null'\" git push"
    bash_blocks 'git --no-pager commit -n -m x'
    bash_blocks 'git -P push --no-verify'
    bash_blocks 'git -C "C:\Users\First Last\repo" commit --no-verify -m x'
    bash_blocks 'git --git-dir .git --work-tree . commit -n -m x'
    bash_blocks '/usr/bin/git commit -n -m x'
    bash_blocks 'cd sub && git commit -n -m x'
    bash_blocks 'bash -c "git commit --no-verify -m x"'
    bash_blocks 'git rebase --exec "git commit --amend --no-edit --no-verify" HEAD~1'
    bash_blocks 'git config core.hooksPath /dev/null'
    bash_blocks 'git config --unset core.hooksPath'
    # Values that look like the options are not the options.
    bash_allows 'git commit -m "-n is not an option here"'
    bash_allows 'git commit -m x -m "--no-verify is not an option here either"'
    bash_allows 'git push -n origin main'
    bash_allows 'git merge --no-verify-signatures feature'
    bash_allows 'git config core.hooksPath .githooks'
    bash_allows 'git config core.hooksPath'
    bash_allows 'git -C "C:\Users\First Last\repo" status'
}

ShouldRefuseAttributionInGithubTextWrittenWithGhOnPreBash() {
    new_session gh
    printf 'Closes #1\n\n%s\n' "$footer" >"$session/attributed.md"
    printf 'Closes #1\n' >"$session/clean.md"
    bash_blocks "gh pr create --title t --body \"Closes #1

$footer\""
    bash_blocks "gh pr edit 5 --body-file attributed.md"
    bash_blocks "gh pr comment 5 --body \"$session_link\""
    bash_blocks "gh pr review 5 --comment -b \"$trailer\""
    bash_blocks "gh pr merge 5 --squash --body \"$trailer\""
    bash_blocks "gh issue create -t t -F attributed.md"
    bash_blocks "gh issue comment 5 --body \"$trailer\""
    bash_blocks "gh issue edit 5 --body \"$footer\""
    bash_blocks "gh api repos/o/r/issues/1/comments -f body='$footer'"
    bash_blocks "gh api repos/o/r/issues/1/comments -F body=@attributed.md"
    bash_blocks "gh release create v1 --notes \"$footer\""
    bash_blocks "/usr/bin/gh pr create --title t --body \"$footer\""
    bash_allows 'gh pr create --title t --body "Closes #1"'
    bash_allows 'gh pr edit 5 --body-file clean.md'
    bash_allows 'gh pr view 5'
    # gh writes no commit, so it never needs the configured git identity.
    git -C "$session" config user.name "$tool_name"
    bash_allows 'gh issue comment 5 --body "Looks good"'
}

ShouldNotRefuseOrdinaryCommandsOnPreBash() {
    new_session ordinary
    bash_allows 'git commit -m x' 'Commit, rather than with --no-verify'
    bash_allows "git commit -m x -m \"Co-Authored-By: $person\" && git push -u origin $tool_lower/issue-700-abc"
    bash_allows 'git commit -m "Document core.hooksPath"'
    bash_allows 'git config core.hooksPath .githooks && git commit -m x'
    bash_allows "git commit -m \"Mention $tool_name in the message\""
    # Commands that record no identity run whatever the identity is.
    git -C "$session" config user.name "$tool_name"
    bash_allows 'git pull --ff-only'
    bash_allows 'git tag -l'
    bash_allows 'git tag v1'
    bash_allows 'git notes list'
    bash_allows 'git status'
    bash_allows 'git log --oneline'
    bash_blocks 'git tag -a v1 -m "A release"'
    bash_blocks 'git pull'
}

ShouldRefuseAttributionOnPreGithub() {
    new_session github
    github_blocks create_pull_request "{\"title\":\"t\",\"body\":$(json_string "Closes #1

$footer")}"
    github_blocks add_issue_comment "{\"issue_number\":1,\"body\":$(json_string "$trailer")}"
    github_blocks update_pull_request "{\"body\":$(json_string "$session_link")}"
    github_allows create_pull_request "{\"title\":\"t\",\"body\":\"Closes #1\"}"
    output=$(printf '{"tool_name":"mcp__github__create_pull_request"}' | bash "$hook" post-github)
    assert_contains 'post-github output' '"additionalContext"' "$output"
    if command -v python3 >/dev/null 2>&1; then
        allowed 'post-github output is JSON' python3 -c 'import json,sys; json.loads(sys.argv[1])' "$output"
    fi
}

ShouldSwitchAToolIdentityToTheSignedInPersonOnSessionStart() {
    repo="$scratch/session-start"
    git init -q "$repo"
    cp -R "$root/.githooks" "$repo/.githooks"
    raw_commit "$repo" "$person" "$person" 'An earlier commit by the person' >/dev/null
    git config --file "$scratch/tool.gitconfig" user.name "$tool_name"
    git config --file "$scratch/tool.gitconfig" user.email "$tool_email"
    start() { env GIT_CONFIG_GLOBAL="$scratch/tool.gitconfig" CLAUDE_PROJECT_DIR="$repo" "$@" bash "$hook" session-start; }

    output=$(start CLAUDE_CODE_USER_EMAIL="$person_email")
    assert_equal 'hooks pointed at .githooks' .githooks "$(git -C "$repo" config core.hooksPath)"
    assert_equal 'name from history' "$person_name" "$(git -C "$repo" config user.name)"
    assert_equal 'email from the session' "$person_email" "$(git -C "$repo" config user.email)"

    git -C "$repo" config --unset user.name
    git -C "$repo" config --unset user.email
    start CLAUDE_CODE_USER_EMAIL="$person_email" G2H_GIT_USER_NAME='Other Person' G2H_GIT_USER_EMAIL='other@example.com' >/dev/null
    assert_equal 'overridden name' 'Other Person' "$(git -C "$repo" config user.name)"
    assert_equal 'overridden email' 'other@example.com' "$(git -C "$repo" config user.email)"

    git -C "$repo" config --unset user.name
    git -C "$repo" config --unset user.email
    output=$(start G2H_GIT_USER_NAME="$tool_name" G2H_GIT_USER_EMAIL='other@example.com')
    assert_equal 'a tool override is not applied' '' "$(git -C "$repo" config --local user.name)"
    assert_contains 'a tool override is refused' 'WARNING' "$output"

    git -C "$repo" config user.name "$person_name"
    git -C "$repo" config user.email "$person_email"
    git -C "$repo" config --unset core.hooksPath
    output=$(start)
    assert_equal 'a person is kept' "$person_name" "$(git -C "$repo" config user.name)"
    assert_equal 'hooks pointed at .githooks for a person too' .githooks "$(git -C "$repo" config core.hooksPath)"
}

ShouldCheckOnlyThePullRequestsOwnCommitsInCi() {
    remote="$scratch/ci-range-remote.git"
    git init -q --bare "$remote"
    seed="$scratch/ci-range-seed"
    git clone -q "$remote" "$seed" 2>/dev/null
    base=$(raw_commit "$seed" "$person" "$person" 'Base, the PR base when it opened')
    git -C "$seed" push -q origin HEAD:main

    repo="$scratch/ci-range"
    clone_repo "$remote" "$repo"
    git -C "$repo" checkout -q -b feature
    git -C "$repo" commit -q --allow-empty -m 'By a person'
    raw_commit "$seed" "$tool_ident" "$tool_ident" 'Tool-authored, merged to main later' >/dev/null
    git -C "$seed" push -q origin HEAD:main
    git -C "$repo" fetch -q origin
    git -C "$repo" merge -q --no-edit origin/main
    clean=$(git -C "$repo" rev-parse HEAD)
    raw_commit "$repo" "$person" "$person" "By a person

$trailer" >/dev/null
    trailer_only=$(git -C "$repo" rev-parse HEAD)
    git -C "$repo" update-ref HEAD "$clean"
    raw_commit "$repo" "$tool_ident" "$person" 'Tool-authored in the PR' >/dev/null
    tool_authored=$(git -C "$repo" rev-parse HEAD)
    # A checkout holds the PR head; the base branch is the job's to fetch.
    git -C "$repo" checkout -q --detach "$clean"
    git -C "$repo" update-ref -d refs/remotes/origin/main

    ci() { ( export BASE_SHA="$base" BASE_REF=main HEAD_SHA="$1" PR_TITLE='CONFIG: Do The Thing' PR_BODY='Closes #1'; ci_job "$repo" ); }
    allowed 'a PR that merged in tool history already on main, with a stale base sha' ci "$clean"
    refused 'a PR with a trailer-only commit' ci "$trailer_only"
    refused 'a PR with a tool-authored commit' ci "$tool_authored"
}

ShouldFailAnAttributedPullRequestTitleOrDescriptionInCi() {
    remote="$scratch/ci-text-remote.git"
    git init -q --bare "$remote"
    repo="$scratch/ci-text"
    git clone -q "$remote" "$repo" 2>/dev/null
    install_hooks "$repo"
    base=$(raw_commit "$repo" "$person" "$person" 'Base')
    git -C "$repo" push -q origin HEAD:main
    head=$(raw_commit "$repo" "$person" "$person" 'By a person')
    ci() { ( export BASE_SHA="$base" BASE_REF=main HEAD_SHA="$head" PR_TITLE="$1" PR_BODY="$2"; ci_job "$repo" ); }
    allowed 'a clean PR' ci 'CONFIG: Do The Thing' 'Closes #1'
    refused 'a footer in the description' ci 'CONFIG: Do The Thing' "Closes #1

$footer"
    refused 'a CRLF description' ci 'CONFIG: Do The Thing' "Closes #1"$'\r\n\r\n'"$footer"$'\r\n'
    refused 'a session link in the title' ci "CONFIG: $session_link" 'Closes #1'
}

# ======================================================================= runner

all_tests=$(declare -F | sed -n 's/^declare -f \(Should[A-Za-z]*\)$/\1/p')
selected=${*:-$all_tests}
for name in $selected; do
    test_failed=0
    printf '%s\n' "$name"
    case " $(printf '%s ' $all_tests)" in
        *" $name "*) "$name" ;;
        *) printf '    FAILED: no such test\n'; test_failed=1 ;;
    esac
    if [ "$test_failed" = 0 ]; then
        printf '  PASS\n'
    else
        printf '  FAIL\n'
        failed_tests=$((failed_tests + 1))
    fi
done

printf '\n%s test(s) failed.\n' "$failed_tests"
[ "$failed_tests" = 0 ]
