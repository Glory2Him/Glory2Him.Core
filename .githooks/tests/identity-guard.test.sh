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

# pre_powershell <command>: runs the session hook over a PowerShell call in $session.
pre_powershell() {
    printf '{"session_id":"test","hook_event_name":"PreToolUse","cwd":%s,"tool_name":"PowerShell","tool_input":{"command":%s}}' \
        "$(json_string "$session")" "$(json_string "$1")" | CLAUDE_PROJECT_DIR="$session" bash "$hook" pre-bash
}

powershell_allows() { expect_exit 0 "pre-bash allows PowerShell: $1" pre_powershell "$@"; }
powershell_blocks() { expect_exit 2 "pre-bash blocks PowerShell: $1" pre_powershell "$@"; }

# pre_github <tool> <tool_input JSON>
pre_github() {
    printf '{"session_id":"test","hook_event_name":"PreToolUse","tool_name":"mcp__github__%s","tool_input":%s}' "$1" "$2" | \
        CLAUDE_PROJECT_DIR="$session" bash "$hook" pre-github
}

github_allows() { expect_exit 0 "pre-github allows $1: $2" pre_github "$@"; }
github_blocks() { expect_exit 2 "pre-github blocks $1: $2" pre_github "$@"; }

# json_value <file> <key> [key ...]: prints, as JSON, the value at that path in
# the JSON file, or "undefined" when the path is absent.
json_value() {
    if command -v python3 >/dev/null 2>&1; then
        python3 -c 'import json,sys
v = json.load(open(sys.argv[1]))
for k in sys.argv[2:]:
    if not isinstance(v, dict) or k not in v:
        print("undefined"); sys.exit()
    v = v[k]
print(json.dumps(v))' "$@"
    else
        node -e 'let v = JSON.parse(require("fs").readFileSync(process.argv[1], "utf8"));
for (const k of process.argv.slice(2)) {
    if (v === null || typeof v !== "object" || !(k in v)) { console.log("undefined"); process.exit(); }
    v = v[k];
}
console.log(JSON.stringify(v));' "$@"
    fi
}

# is_json <text>: succeeds when the text parses as JSON.
is_json() {
    if command -v python3 >/dev/null 2>&1; then
        python3 -c 'import json,sys; json.loads(sys.argv[1])' "$1"
    else
        node -e 'JSON.parse(process.argv[1])' "$1"
    fi
}

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

ShouldTurnOffClaudeCodesOwnAttributionInSettings() {
    assert_equal 'attribution.commit' '""' "$(json_value "$settings" attribution commit)"
    assert_equal 'attribution.pr' '""' "$(json_value "$settings" attribution pr)"
    assert_equal 'attribution.sessionUrl' false "$(json_value "$settings" attribution sessionUrl)"
    assert_equal 'includeCoAuthoredBy' false "$(json_value "$settings" includeCoAuthoredBy)"
}

ShouldRefuseOnlyAToolIdentityOnCheckIdent() {
    refused 'the tool identity' bash "$guard" check-ident author "$tool_ident"
    refused 'a tool name' bash "$guard" check-ident author "$tool_name Code <someone@example.com>"
    refused 'a tool address' bash "$guard" check-ident author "Someone <someone@anthro""pic.com>"
    refused 'no identity' bash "$guard" check-ident author ''
    # The name, trimmed and in any case, is exactly one of the tool's names.
    refused 'a padded, lower-case tool name' bash "$guard" check-ident author " $tool_lower code  <someone@example.com>"
    refused 'an upper-case tool name' bash "$guard" check-ident author "CLAU""DE <someone@example.com>"
    refused 'the bot name' bash "$guard" check-ident author "$tool_lower[bot] <1+bot@users.noreply.github.com>"
    refused 'the bot name as git prints it' bash "$guard" check-ident author "$tool_lower[bot] <1+bot@users.noreply.github.com> 1700000000 +0000"
    # Any address at the tool's domain, in any case.
    refused 'an upper-case tool address' bash "$guard" check-ident author "Someone <SOMEONE@ANTHRO""PIC.COM>"
    refused 'a tool address as git prints it' bash "$guard" check-ident author "Jane <jane@anthro""pic.com> 1700000000 +0000"
    allowed 'a person' bash "$guard" check-ident author "$person"
    allowed 'a person as git prints them' bash "$guard" check-ident author "$person 1700000000 +0000"
    allowed 'a person named Jean Claude' bash "$guard" check-ident author "Jean $tool_name <jc@example.com>"
    allowed 'a person named Claude Monet' bash "$guard" check-ident author "$tool_name Monet <cm@example.com>"
    allowed 'a person whose address holds the word' bash "$guard" check-ident author "Jean <jean.$tool_lower@example.fr>"
    allowed 'a person named Claudette' bash "$guard" check-ident author "${tool_name}tte <c@example.com>"
    allowed 'a person at a domain that only ends in the name' bash "$guard" check-ident author "Jane <jane@not""anthro""pic.com>"
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

ShouldRefuseAToolIdentityOnPreMergeCommit() {
    repo="$scratch/pre-merge-commit"
    new_repo "$repo"
    git -C "$repo" commit -q --allow-empty -m 'Base'
    git -C "$repo" checkout -q -b feature
    git -C "$repo" commit -q --allow-empty -m 'On the feature'
    git -C "$repo" checkout -q main
    git -C "$repo" commit -q --allow-empty -m 'On main'
    refused 'a merge commit by a tool' \
        git -C "$repo" -c user.name="$tool_name" -c user.email="$tool_email" merge -q --no-ff --no-edit feature
    git -C "$repo" merge --abort 2>/dev/null
    allowed 'a merge commit by a person' git -C "$repo" merge -q --no-ff --no-edit feature
    assert_equal 'merge author' "$person_name" "$(git -C "$repo" log -1 --format=%an)"
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

ShouldCheckAForcePushOverARemoteTipThisCloneHasNotSeen() {
    remote="$scratch/unseen-remote.git"
    git init -q --bare "$remote"
    other="$scratch/unseen-other"
    clone_repo "$remote" "$other"
    git -C "$other" commit -q --allow-empty -m 'Base'
    git -C "$other" push -q origin main

    repo="$scratch/unseen"
    clone_repo "$remote" "$repo"
    git -C "$other" commit -q --allow-empty -m 'Pushed elsewhere, never fetched here'
    git -C "$other" push -q origin main

    git -C "$repo" commit -q --allow-empty -m 'By a person'
    allowed 'a force push of commits by a person' git -C "$repo" push -q --force origin main
    raw_commit "$repo" "$tool_ident" "$tool_ident" 'Tool-authored' >/dev/null
    git -C "$other" fetch -q origin
    git -C "$other" reset -q --hard origin/main
    git -C "$other" commit -q --allow-empty -m 'Pushed elsewhere again'
    git -C "$other" push -q origin main
    output=$(git -C "$repo" push -q --force origin main 2>&1)
    assert_equal 'the tool commit is not published' "$(git -C "$other" rev-parse HEAD)" "$(git -C "$remote" rev-parse main)"
    assert_contains 'the refusal names the tool identity' 'authored or committed as an AI tool' "$output"
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
    bash_blocks 'git commit --no-verify -m x'
    bash_blocks 'git -c core.hooksPath=/dev/null commit -m x'
    bash_allows 'git commit -m "Add the thing"'
    bash_allows 'git status'
    bash_allows 'ls -la'
    git -C "$session" config --unset core.hooksPath
    bash_allows 'git push origin main'
    assert_equal 'hooks pointed at .githooks' .githooks "$(git -C "$session" config core.hooksPath)"
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
    # Every file source, in every form: --option value, --option=value, -x value, -xvalue.
    mkdir -p "$session/docs"
    cp "$session/attributed.md" "$session/docs/attributed.md"
    bash_blocks 'gh pr create --title t --body-file=attributed.md'
    bash_blocks 'gh pr create -t t -Fattributed.md'
    bash_blocks 'gh pr comment 5 -F docs/attributed.md'
    bash_blocks 'gh pr review 5 --comment --body-file attributed.md'
    bash_blocks 'gh pr merge 5 --squash --body-file attributed.md'
    bash_blocks 'gh issue edit 5 --body-file attributed.md'
    bash_blocks 'gh issue comment 5 -Fattributed.md'
    bash_blocks 'gh release create v1 --notes-file attributed.md'
    bash_blocks 'gh release edit v1 --notes-file=attributed.md'
    bash_blocks 'gh release create v1 -F attributed.md'
    bash_blocks 'gh api repos/o/r/issues/1/comments --field body=@attributed.md'
    bash_blocks 'gh api repos/o/r/issues/1/comments -Fbody=@attributed.md'
    bash_blocks 'gh api repos/o/r/issues/1/comments --field=body=@attributed.md'
    bash_blocks 'gh api repos/o/r/issues/1 -X PATCH --input attributed.md'
    bash_blocks 'gh api repos/o/r/issues/1 -X PATCH --input=attributed.md'
    bash_blocks '/usr/local/bin/gh pr comment 5 --body-file attributed.md'
    bash_blocks 'cd . && gh pr comment 5 --body-file attributed.md && echo done'
    # Every inline source.
    bash_blocks "gh pr create --title \"$session_link\" --body x"
    bash_blocks "gh pr create -t \"$session_link\" -b x"
    bash_blocks "gh pr comment 5 -b\"$trailer\""
    bash_blocks "gh pr comment 5 --body=\"$trailer\""
    bash_blocks "gh release edit v1 -n \"$footer\""
    bash_blocks "gh api repos/o/r/issues/1/comments --raw-field \"body=$footer\""
    bash_blocks "gh api repos/o/r/issues -f title=t --field \"body=$footer\""
    bash_allows 'gh pr create --title t --body "Closes #1"'
    bash_allows 'gh pr edit 5 --body-file clean.md'
    bash_allows 'gh pr create -t t -Fclean.md'
    bash_allows 'gh release create v1 --notes-file=clean.md'
    bash_allows 'gh api repos/o/r/issues/1/comments -F body=@clean.md'
    bash_allows 'gh api repos/o/r/issues/1 -X PATCH --input clean.md'
    bash_allows 'gh pr view 5'
    bash_allows 'gh api repos/o/r/pulls/5'
    # gh writes no commit, so it never needs the configured git identity.
    git -C "$session" config user.name "$tool_name"
    bash_allows 'gh issue comment 5 --body "Looks good"'
}

ShouldRestoreTheHooksPathBeforeEachCommandOnPreBash() {
    new_session restore
    hooks_path() { git -C "$session" config core.hooksPath; }
    git -C "$session" config --unset core.hooksPath
    pre_bash 'ls -la' >/dev/null 2>&1
    assert_equal 'restored when unset, before an ordinary command' .githooks "$(hooks_path)"
    git -C "$session" config core.hooksPath /dev/null
    pre_bash 'echo hi' >/dev/null 2>&1
    assert_equal 'restored when redirected' .githooks "$(hooks_path)"
    git -C "$session" config core.hooksPath ./.githooks
    pre_bash 'git commit --no-verify -m x' >/dev/null 2>&1
    assert_equal 'restored before a refused command' .githooks "$(hooks_path)"
    git -C "$session" config core.hooksPath /dev/null
    pre_powershell 'Get-ChildItem' >/dev/null 2>&1
    assert_equal 'restored before a PowerShell command' .githooks "$(hooks_path)"
    git -C "$session" config core.hooksPath /dev/null
    printf '{"tool_name":"Bash","tool_input":{}}' | CLAUDE_PROJECT_DIR="$session" bash "$hook" pre-bash >/dev/null 2>&1
    assert_equal 'restored for a payload with no command' .githooks "$(hooks_path)"
}

ShouldRefuseEveryHookSkippingTokenOnPreBash() {
    new_session tokens
    # --no-veri, and every abbreviation or extension of it.
    bash_blocks 'git commit --no-verify -m x'
    bash_blocks 'git commit --NO-VERI -m x'
    bash_blocks 'git push --No-Verify'
    bash_blocks 'git merge --no-verify-signatures feature'
    bash_blocks 'git commit -m x -m "--no-verify is only mentioned"'
    # hookspath, set, read or only mentioned.
    bash_blocks 'git -c core.hooksPath=/dev/null commit -m x'
    bash_blocks 'git -c CORE.HOOKSPATH=/dev/null push'
    bash_blocks 'git config core.hooksPath .githooks'
    bash_blocks 'git config core.hooksPath'
    bash_blocks 'git commit -m "Document core.hooksPath"'
    bash_blocks 'git config core.hooksPath .githooks && git commit -m x'
    # git_config, as in the GIT_CONFIG_* variables.
    bash_blocks 'GIT_CONFIG_COUNT=1 GIT_CONFIG_KEY_0=core.editor GIT_CONFIG_VALUE_0=vi git commit -m x'
    bash_blocks 'export git_config_global=/tmp/other; git commit -m x'
    # alias., include. and includeIf. config keys.
    bash_blocks 'git config alias.ci commit'
    bash_blocks 'git -c ALIAS.ci=commit ci -m x'
    bash_blocks 'git config --global include.path /tmp/other.gitconfig'
    bash_blocks 'git config includeIf.gitdir:~/src/.path /tmp/other.gitconfig'
    # After the word commit, a single-dash cluster of letters that holds n.
    bash_blocks 'git commit -n -m x'
    bash_blocks 'git commit -N -m x'
    bash_blocks 'git commit -anm x'
    bash_blocks 'git commit -qn -m x'
    bash_blocks 'git commit -m "-n is not an option here"'
    bash_blocks 'git COMMIT --amend -n'
    bash_blocks "git commit -m x
git log -n 1"
    # Redirections, wrappers and other shells change nothing: it is all text.
    bash_blocks 'git commit -m x 2>&1 --no-verify'
    bash_blocks 'git 2>/dev/null commit -n -m x'
    bash_blocks '>/dev/null git commit -n -m x'
    bash_blocks 'git push >&2 --no-verify'
    bash_blocks "bash -o pipefail -c 'git commit -n -m x'"
    bash_blocks "bash -c -- 'git commit --no-verify -m x'"
    bash_blocks 'powershell -Command "git push --no-verify"'
    bash_blocks 'cmd /c "git commit -n -m x"'
    powershell_blocks 'git commit --no-verify -m x'
    powershell_blocks 'git commit `
    -n -m x'
    powershell_blocks '$env:GIT_CONFIG_GLOBAL = "C:\temp\other"; git commit -m x'
    powershell_blocks 'pwsh -Command "git -c core.hooksPath=NUL commit -m x"'
    # None of the tokens: allowed.
    bash_allows 'git push -n origin main'
    bash_allows 'git log -n 5'
    bash_allows 'git commit -m "Add the new-feature flag"'
    bash_allows 'git commit --amend --no-edit'
    bash_allows 'git commit -m x && git log --oneline'
    powershell_allows 'git commit -m "Add the thing"'
}

ShouldNameTheMatchedTokenInTheRefusalOnPreBash() {
    new_session token-names
    names() {
        output=$(pre_bash "$1" 2>&1)
        assert_contains "the refusal of: $1" "\"$2\"" "$output"
    }
    names 'git push --No-Verify-Signatures' '--No-Veri'
    names 'git -c Core.HooksPath=/dev/null push' 'HooksPath'
    names 'export Git_Config_Global=/tmp/x; git commit -m x' 'Git_Config'
    names 'git config Alias.ci commit' 'Alias.'
    names 'git config --global Include.path /tmp/x' 'Include.'
    names 'git config IncludeIf.gitdir:~/src/.path /tmp/x' 'IncludeIf.'
    names 'git commit -m x -qNa' '-qNa'
    names 'git commit -m "-n is not an option here"' '-n'
}

ShouldNotJudgeIdentityOrMessagesOnPreBash() {
    new_session not-judged
    # Identities set on the command: git's own hooks judge what git resolves.
    bash_allows "git -c user.name=$tool_name commit -m x"
    bash_allows "git -c user.name=\"$tool_name Code\" -c user.email=$tool_email commit -m x"
    bash_allows "GIT_AUTHOR_NAME=$tool_name git commit -m x"
    bash_allows "git commit --author=\"$tool_ident\" -m x"
    bash_allows "git config user.email $tool_email"
    # Messages carrying attribution: commit-msg and pre-push judge them.
    bash_allows "git commit -m x -m \"$trailer\""
    bash_allows "git commit -m x -m \"$session_link\""
    bash_allows "git commit -m x -m \"$footer\""
    bash_allows "git tag -a v1 -m \"$trailer\""
    # A tool identity configured: nothing is refused because of it.
    git -C "$session" config user.name "$tool_name"
    git -C "$session" config user.email "$tool_email"
    bash_allows 'git commit -m x'
    bash_allows 'git tag -a v1 -m "A release"'
    bash_allows 'git pull'
    bash_allows 'git merge --no-ff feature'
}

ShouldNotRefuseOrdinaryCommandsOnPreBash() {
    new_session ordinary
    bash_allows 'git commit -m x' 'Commit, rather than with --no-verify'
    bash_allows "git commit -m x -m \"Co-Authored-By: $person\" && git push -u origin $tool_lower/issue-700-abc"
    bash_allows "git commit -m \"Mention $tool_name in the message\""
    # Commands that record no identity run whatever the identity is.
    git -C "$session" config user.name "$tool_name"
    bash_allows 'git pull --ff-only'
    bash_allows 'git tag -l'
    bash_allows 'git tag v1'
    bash_allows 'git notes list'
    bash_allows 'git status'
    bash_allows 'git log --oneline'
}

ShouldRefuseAttributionOnPreGithub() {
    new_session github
    github_blocks create_pull_request "{\"title\":\"t\",\"body\":$(json_string "Closes #1

$footer")}"
    github_blocks add_issue_comment "{\"issue_number\":1,\"body\":$(json_string "$trailer")}"
    github_blocks update_pull_request "{\"body\":$(json_string "$session_link")}"
    github_allows create_pull_request "{\"title\":\"t\",\"body\":\"Closes #1\"}"
}

ShouldRemindTheSessionToEditAwayTheConnectorFooterOnPostGithub() {
    output=$(printf '{"tool_name":"mcp__github__create_pull_request"}' | bash "$hook" post-github)
    assert_contains 'post-github output' '"additionalContext"' "$output"
    assert_contains 'post-github names the footer' 'Generated by' "$output"
    allowed 'post-github output is JSON' is_json "$output"
}

ShouldCheckOnlyTheTextFieldsOnPreGithub() {
    new_session github-fields
    # A file's contents are code, not GitHub text: the guard itself names the patterns.
    github_allows push_files "{\"branch\":\"b\",\"message\":\"CONFIG: Update The Guard\",\"files\":[{\"path\":\"a.sh\",\"content\":$(json_string "$trailer")}]}"
    github_allows create_or_update_file "{\"path\":\"a.md\",\"message\":\"DOCUMENTATION: Explain\",\"content\":$(json_string "$footer")}"
    github_blocks push_files "{\"branch\":\"b\",\"message\":$(json_string "x

$trailer"),\"files\":[{\"path\":\"a.sh\",\"content\":\"echo\"}]}"
    github_blocks create_or_update_file "{\"path\":\"a.md\",\"message\":$(json_string "$session_link"),\"content\":\"x\"}"
    github_blocks merge_pull_request "{\"pullNumber\":1,\"commit_title\":\"t\",\"commit_message\":$(json_string "$trailer")}"
    github_blocks merge_pull_request "{\"pullNumber\":1,\"commit_title\":$(json_string "$footer")}"
    github_blocks issue_write "{\"method\":\"create\",\"title\":$(json_string "$session_link"),\"body\":\"x\"}"
    github_blocks discussion_comment_write "{\"body\":$(json_string "$footer")}"
    github_blocks pull_request_review_write "{\"method\":\"create\",\"body\":$(json_string "$trailer")}"
}

ShouldRunTheGithubHooksOnExactlyTheToolsThatWriteText() {
    matchers=$(awk '/"matcher"/ { m = $0; sub(/.*"matcher": *"/, "", m); sub(/".*/, "", m) }
        /git-identity-guard\.sh/ { mode = $0; sub(/.*git-identity-guard\.sh\\" */, "", mode); sub(/".*/, "", mode); print mode "\t" m }' "$settings")
    matcher_for() { printf '%s\n' "$matchers" | awk -F '\t' -v mode="$1" '$1 == mode { print $2 }'; }
    matches() { printf '%s\n' "$2" | grep -Eq -- "$(matcher_for "$1")"; }
    for tool in create_pull_request update_pull_request push_files create_or_update_file merge_pull_request \
        issue_write create_issue add_issue_comment update_issue_comment add_reply_to_pull_request_comment \
        add_comment_to_pending_review add_pull_request_review_comment pull_request_review_write \
        create_pull_request_review submit_pending_pull_request_review discussion_comment_write delete_file; do
        allowed "pre-github runs on $tool" matches pre-github "mcp__github__$tool"
    done
    for tool in create_pull_request issue_write create_issue add_issue_comment add_reply_to_pull_request_comment \
        add_pull_request_review_comment pull_request_review_write create_pull_request_review discussion_comment_write; do
        allowed "post-github runs on $tool" matches post-github "mcp__github__$tool"
    done
    for tool in sub_issue_write issue_read get_me list_issues; do
        refused "pre-github does not run on $tool" matches pre-github "mcp__github__$tool"
        refused "post-github does not run on $tool" matches post-github "mcp__github__$tool"
    done
    refused 'pre-github does not run on another server' matches pre-github 'mcp__other__create_pull_request'
    allowed 'pre-bash runs on Bash' matches pre-bash 'Bash'
    allowed 'pre-bash runs on PowerShell' matches pre-bash 'PowerShell'
    refused 'pre-bash does not run on a tool merely named like it' matches pre-bash 'mcp__x__Bash'
    new_session github-delete
    github_blocks delete_file "{\"path\":\"a.md\",\"branch\":\"b\",\"message\":$(json_string "x

$trailer")}"
    github_allows delete_file '{"path":"a.md","branch":"b","message":"CONFIG: Remove The Old File"}'
}

ShouldSkipCommentLinesOnlyInACommitMessageGitWillStrip() {
    # Markdown headings and stored messages keep their "#" lines: they are judged.
    refused 'a heading footer in a PR description' \
        bash -c "printf '%s\n' 'Closes #1' '## $footer' | bash '$guard' check-text"
    new_session comments
    github_blocks create_pull_request "{\"title\":\"t\",\"body\":$(json_string "Closes #1

## $footer")}"
    base=$(git -C "$session" rev-parse HEAD)
    raw_commit "$session" "$person" "$person" "x

# $trailer" >/dev/null
    refused 'a stored "#" line' bash -c "cd '$session' && bash '$guard' check-range '$base..HEAD'"
    git -C "$session" update-ref HEAD "$base"

    # An edited message: git strips its comment lines and all below the scissors.
    cat >"$scratch/editor" <<EDITOR
#!/usr/bin/env bash
{ printf 'Subject\n# %s\n' '$trailer'; cat "\$1"; } >"\$1.new" && mv "\$1.new" "\$1"
EDITOR
    chmod +x "$scratch/editor"
    printf '%s\n' "$trailer" >"$session/notes.txt"
    git -C "$session" add notes.txt
    allowed 'a comment line and a diff below the scissors' \
        env GIT_EDITOR="$scratch/editor" git -C "$session" commit -q -v
    assert_equal 'stored message' 'Subject' "$(git -C "$session" log -1 --format=%B | sed '/^$/d')"
}

# session_repo <dir>: a clone as a session finds it: the guard's hooks present
# but not yet switched on, one earlier commit by the person, and git's global
# configuration set to the tool's identity, as a cloud container's is.
session_repo() {
    repo="$scratch/$1"
    git init -q "$repo"
    cp -R "$root/.githooks" "$repo/.githooks"
    printf '.githooks/\n' >>"$repo/.git/info/exclude"
    raw_commit "$repo" "$person" "$person" 'An earlier commit by the person' >/dev/null
    git config --file "$scratch/$1.gitconfig" user.name "$tool_name"
    git config --file "$scratch/$1.gitconfig" user.email "$tool_email"
    tool_config="$scratch/$1.gitconfig"
}

# start [VAR=value ...]: runs session start in $repo under the tool's global identity.
start() { env GIT_CONFIG_GLOBAL="$tool_config" CLAUDE_PROJECT_DIR="$repo" "$@" bash "$hook" session-start; }

ShouldPointGitAtTheRepositoryHooksOnSessionStart() {
    session_repo session-hooks
    start CLAUDE_CODE_USER_EMAIL="$person_email" >/dev/null
    assert_equal 'hooks pointed at .githooks for a tool identity' .githooks "$(git -C "$repo" config core.hooksPath)"

    git -C "$repo" config user.name "$person_name"
    git -C "$repo" config user.email "$person_email"
    git -C "$repo" config core.hooksPath /dev/null
    output=$(start)
    assert_equal 'hooks pointed at .githooks for a person' .githooks "$(git -C "$repo" config core.hooksPath)"
    assert_equal 'a person is kept' "$person_name" "$(git -C "$repo" config user.name)"
    assert_contains 'the person is named' "$person_name" "$output"
}

ShouldSwitchAToolIdentityToTheSignedInPersonOnSessionStart() {
    session_repo session-switch
    start CLAUDE_CODE_USER_EMAIL="$person_email" >/dev/null
    assert_equal 'name from history' "$person_name" "$(git -C "$repo" config user.name)"
    assert_equal 'email from the session' "$person_email" "$(git -C "$repo" config user.email)"
    allowed 'a commit as the person' env GIT_CONFIG_GLOBAL="$tool_config" git -C "$repo" commit -q --allow-empty -m 'By the person'
    assert_equal 'the commit author' "$person" "$(git -C "$repo" log -1 --format='%an <%ae>')"

    git -C "$repo" config user.name 'Someone Else'
    git -C "$repo" config user.email 'someone.else@example.com'
    start CLAUDE_CODE_USER_EMAIL="$person_email" >/dev/null
    assert_equal "a person's name is not changed" 'Someone Else' "$(git -C "$repo" config user.name)"
    assert_equal "a person's email is not changed" 'someone.else@example.com' "$(git -C "$repo" config user.email)"
}

ShouldPreferTheG2hOverridesOnSessionStart() {
    session_repo session-overrides
    start CLAUDE_CODE_USER_EMAIL="$person_email" G2H_GIT_USER_NAME='Other Person' G2H_GIT_USER_EMAIL='other@example.com' >/dev/null
    assert_equal 'overridden name' 'Other Person' "$(git -C "$repo" config user.name)"
    assert_equal 'overridden email' 'other@example.com' "$(git -C "$repo" config user.email)"

    git -C "$repo" config --unset user.name
    git -C "$repo" config --unset user.email
    start CLAUDE_CODE_USER_EMAIL="$person_email" G2H_GIT_USER_NAME='Other Person' >/dev/null
    assert_equal 'the name override over the name in history' 'Other Person' "$(git -C "$repo" config user.name)"
    assert_equal 'the session email with only a name override' "$person_email" "$(git -C "$repo" config user.email)"

    git -C "$repo" config --unset user.name
    git -C "$repo" config --unset user.email
    raw_commit "$repo" 'Other Person <other@example.com>' "$person" 'A commit by the other person' >/dev/null
    start CLAUDE_CODE_USER_EMAIL="$person_email" G2H_GIT_USER_EMAIL='other@example.com' >/dev/null
    assert_equal 'the email override over the session email' 'other@example.com' "$(git -C "$repo" config user.email)"
    assert_equal 'the name found under the overriding email' 'Other Person' "$(git -C "$repo" config user.name)"
}

ShouldWarnAndChangeNothingWhenNoPersonIsFoundOnSessionStart() {
    session_repo session-nobody
    no_person() {
        output=$(start "$@")
        assert_contains "the warning ($1)" 'WARNING' "$output"
        assert_contains "how to set the identity ($1)" 'git config user.name' "$output"
        assert_equal "user.name unchanged ($1)" '' "$(git -C "$repo" config --local user.name)"
        assert_equal "user.email unchanged ($1)" '' "$(git -C "$repo" config --local user.email)"
        refused "a commit after session start ($1)" \
            env GIT_CONFIG_GLOBAL="$tool_config" git -C "$repo" commit -q --allow-empty -m 'x'
    }
    no_person CLAUDE_CODE_USER_EMAIL=
    no_person CLAUDE_CODE_USER_EMAIL='nobody@example.com'
    no_person CLAUDE_CODE_USER_EMAIL="$person_email" G2H_GIT_USER_NAME="$tool_name"
    no_person G2H_GIT_USER_NAME="$person_name" G2H_GIT_USER_EMAIL="$tool_email"
    raw_commit "$repo" "$tool_name <tool.run@example.com>" "$person" 'Committed under the tool name' >/dev/null
    no_person CLAUDE_CODE_USER_EMAIL='tool.run@example.com'
    assert_equal 'commits made' 2 "$(git -C "$repo" rev-list --count HEAD)"
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

ShouldFindTheSignedInPersonWhateverTheCaseOfTheirEmailOnSessionStart() {
    session_repo session-case
    raw_commit "$repo" "$person_name <Jane.Person+g2h@Example.com>" "$person" 'An earlier commit' >/dev/null
    start CLAUDE_CODE_USER_EMAIL='jane.person+G2H@example.COM' >/dev/null
    assert_equal 'name from history' "$person_name" "$(git -C "$repo" config user.name)"
    assert_equal 'email from the session' 'jane.person+G2H@example.COM' "$(git -C "$repo" config user.email)"
}

ShouldWarnWhenAToolIdentityFromTheEnvironmentOutlivesTheSwitchOnSessionStart() {
    session_repo session-env
    output=$(start CLAUDE_CODE_USER_EMAIL="$person_email" GIT_AUTHOR_NAME="$tool_name" GIT_COMMITTER_EMAIL="$tool_email")
    assert_equal 'the configured identity is still switched' "$person_name" "$(git -C "$repo" config user.name)"
    assert_contains 'the warning' 'WARNING' "$output"
    assert_contains 'the author variables named' 'GIT_AUTHOR_' "$output"
    assert_contains 'the committer variables named' 'GIT_COMMITTER_' "$output"
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
