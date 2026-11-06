#!/usr/bin/env bash
# Builds every task of one approved feature, in order, one claude -p session per task.
#
#   feature-build.sh <feature-issue>
#
# Runs on the developer's own machine: under Git Bash on Windows, or bash on Linux or
# macOS. Each task is built in its own worktree, branched from the feature branch, at the
# effort its `Model - Effort` label sets, then merged into the feature branch with a merge
# commit once QA passes and CI is green. When every task is in, /verify-feature checks the
# feature as a whole and the feature PR is taken out of draft for the maintainer to merge
# to main.
#
# A legacy feature — one labelled `legacy`, taken in by QA rather than planned — builds the
# same way once legacy intake has signed it: its Signed: line is checked like any other.
#
# Re-running resumes: tasks labelled `status: done` are skipped, and /build-task resumes a
# task whose PR already exists. Needs bash, git, gh (authenticated), jq, claude and node on
# PATH — on Windows, `winget install jqlang.jq` for jq. Uses nothing Git Bash lacks: no
# flock, setsid or /proc, and no associative arrays, so macOS's older bash runs it too.
set -u

FEATURE=${1:?usage: feature-build.sh <feature-issue>}
LOGDIR="${TMPDIR:-/tmp}/feature-$FEATURE"
mkdir -p "$LOGDIR"

here=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPO=${FEATURE_REPO:-$(dirname "$(git -C "$here" rev-parse --path-format=absolute --git-common-dir)")}
cd "$REPO" || exit 1

MODEL=claude-opus-5-5
export PATH="$HOME/.local/bin:$PATH"
# Without this, `claude -p` stops background agents and CI waits at 600s.
export CLAUDE_CODE_PRINT_BG_WAIT_CEILING_MS=0

ts() { date -u +%Y-%m-%dT%H:%M:%SZ; }
# The time 30 minutes from now: GNU date (Linux, Git Bash) or BSD date (macOS).
in_30_minutes() { date -u -d '+30 minutes' +%H:%MZ 2>/dev/null || date -u -v+30M +%H:%MZ; }
# -j prints no newline, so a native jq.exe under Git Bash adds no carriage return either.
uri() { jq -jn --arg v "$1" '$v|@uri'; }

# ---- progress: one line per milestone, in the log and in one comment on the feature ----
PROGRESS_ID=""
progress_comment() {
  [ -n "$PROGRESS_ID" ] && return
  PROGRESS_ID=$(gh api "repos/{owner}/{repo}/issues/$FEATURE/comments" --paginate \
    --jq ".[] | select(.body | startswith(\"Build progress — feature #$FEATURE\")) | .id" | head -1)
  if [ -z "$PROGRESS_ID" ]; then
    PROGRESS_ID=$(gh api -X POST "repos/{owner}/{repo}/issues/$FEATURE/comments" \
      -f body="Build progress — feature #$FEATURE" --jq .id)
  fi
}
note() {
  local line
  line="[$(ts)] $*"
  echo "$line" | tee -a "$LOGDIR/summary.log"
  progress_comment
  local body
  body=$(gh api "repos/{owner}/{repo}/issues/comments/$PROGRESS_ID" --jq .body)
  gh api -X PATCH "repos/{owner}/{repo}/issues/comments/$PROGRESS_ID" \
    -f body="$body
- $line" >/dev/null 2>&1 || true
}
add_label()    { gh api -X POST "repos/{owner}/{repo}/issues/$1/labels" -f "labels[]=$2" >/dev/null 2>&1 || true; }
remove_label() { gh api -X DELETE "repos/{owner}/{repo}/issues/$1/labels/$(uri "$2")" >/dev/null 2>&1 || true; }
has_label()    { gh api "repos/{owner}/{repo}/issues/$1" --jq '.labels[].name' | grep -qxF "$2"; }
# A halt posts its own comment, so whoever is watching the issue is notified; the progress
# comment is edited in place and notifies nobody.
halt() {
  note "HALT $*"
  gh api -X POST "repos/{owner}/{repo}/issues/$FEATURE/comments" \
    -f body="Build halted: $*

Resolve it, answer any question here, then re-run \`/build-feature $FEATURE\`." >/dev/null 2>&1 || true
  add_label "$FEATURE" "status: blocked"; rm -f "$LOGDIR/driver.pid"; exit 1
}

# ---- one driver per feature ----
if [ -f "$LOGDIR/driver.pid" ] && kill -0 "$(cat "$LOGDIR/driver.pid")" 2>/dev/null; then
  echo "a driver is already running for feature #$FEATURE"; exit 1
fi
echo $$ >"$LOGDIR/driver.pid"

for tool in git gh jq claude node; do
  command -v "$tool" >/dev/null || { echo "no $tool on PATH"; rm -f "$LOGDIR/driver.pid"; exit 1; }
done
# A verified feature waits for the maintainer: re-running must not reopen or relabel it.
if has_label "$FEATURE" "status: done"; then
  note "#$FEATURE is already built and verified; its feature PR waits for the maintainer to merge"
  rm -f "$LOGDIR/driver.pid"
  exit 0
fi
remove_label "$FEATURE" "status: blocked"

# ---- gates ----
# Only a person adds `approved`, never an agent. The SHA-256 hashes below are what protect
# the approved plan: a body changed after QA signed it halts the build.
has_label "$FEATURE" approved || halt "feature #$FEATURE is not approved"
me=$(gh api user --jq .login)
[ -n "$me" ] || halt "gh is not signed in"

# Build order: by layer, from the category in each task's title — groundwork, broker,
# foundation, processing, orchestration, exposer, UI — then by **Depends on**, which wins
# where the two disagree. Ties keep the planner's sub-issue order. STORY_MAP holds one
# "<task> <story>" line per task.
STORIES=$(gh api "repos/{owner}/{repo}/issues/$FEATURE/sub_issues" --paginate --jq '.[].number')
[ -n "$STORIES" ] || halt "feature #$FEATURE has no user stories"
STORY_MAP=""
listing=""
for s in $STORIES; do
  while IFS=$'\t' read -r t title deps; do
    [ -n "$t" ] || continue
    STORY_MAP+="$t $s"$'\n'
    listing+="$t"$'\t'"$title"$'\t'"$deps"$'\n'
  done < <(gh api "repos/{owner}/{repo}/issues/$s/sub_issues" --paginate --jq '.[] |
             [(.number | tostring), .title,
              ((.body // "") | [scan("\\*\\*Depends on:\\*\\*[^\n]*")] | first // ""
                             | [scan("#([0-9]+)")[0]] | join(" "))] | @tsv')
done
story_of() { awk -v t="$1" '$1 == t { print $2; exit }' <<<"$STORY_MAP"; }
TASKS=$(printf '%s' "$listing" | node scripts/build-order.mjs) || halt "cannot order the tasks: $(printf '%s' "$listing" | node scripts/build-order.mjs 2>&1 >/dev/null)"
[ -n "$TASKS" ] || halt "feature #$FEATURE has no tasks"
for t in $TASKS; do
  has_label "$t" "ready for development" || halt "task #$t is not signed off (ready for development)"
done

# QA's sign-off hashes, from the latest plan round that recorded them. A legacy intake
# records its Signed: line in a Plan QA round comment too.
SIGNED=$(gh api "repos/{owner}/{repo}/issues/$FEATURE/comments" --paginate \
  --jq '.[] | select(.body | startswith("Plan QA round")) | .body' | tr -d '\r' | grep -E '^Signed:' | tail -1)
[ -n "$SIGNED" ] || halt "no Signed: hash line in a Plan QA round on #$FEATURE"
check_hash() {
  local n=$1 want got
  want=$(grep -oE "#$n sha256:[0-9a-f]{16}([^0-9a-f]|$)" <<<"$SIGNED" | head -1 | grep -oE '[0-9a-f]{16}')
  [ -n "$want" ] || halt "#$n has no full 16-character hash on the Signed: line — QA did not sign it"
  got=$(bash scripts/issue-hash.sh "$n")
  [ "$got" = "$want" ] || halt "#$n changed after QA signed it"
}

# ---- the feature branch and its draft PR ----
git fetch origin main --quiet
# The issue form no longer prefixes titles, but older issues may carry "Feature: ".
TITLE=$(gh api "repos/{owner}/{repo}/issues/$FEATURE" --jq .title | sed -E 's/^[Ff]eature:[[:space:]]*//')
read -r FEATURE_PR FEATURE_BRANCH < <(gh api "repos/{owner}/{repo}/pulls?state=open&base=main&per_page=100" --paginate \
  --jq ".[] | select(.head.ref | test(\"^users/[^/]+/feature-$FEATURE-\")) | \"\(.number) \(.head.ref)\"" | head -1)
if [ -z "${FEATURE_PR:-}" ]; then
  slug=$(tr '[:upper:]' '[:lower:]' <<<"$TITLE" | sed -E 's/[^a-z0-9]+/-/g; s/^-+|-+$//g' | cut -c1-40 | sed -E 's/-+$//')
  FEATURE_BRANCH="users/$me/feature-$FEATURE-$slug"
  # GitHub opens no PR on a branch with no commits ahead of its base, so start with one.
  start=$(git commit-tree "origin/main^{tree}" -p origin/main -m "FEATURES: Start $TITLE")
  git push --quiet origin "$start:refs/heads/$FEATURE_BRANCH" || halt "could not push $FEATURE_BRANCH"
  FEATURE_PR=$(gh api -X POST "repos/{owner}/{repo}/pulls" -f base=main -f head="$FEATURE_BRANCH" \
    -f title="FEATURES: $TITLE" -F draft=true -f body="Closes #$FEATURE" --jq .number) \
    || halt "could not open the feature PR"
  note "opened $FEATURE_BRANCH and draft PR #$FEATURE_PR"
fi
remove_label "$FEATURE" "status: awaiting-approval"
add_label "$FEATURE" "status: building"
note "building #$FEATURE on $FEATURE_BRANCH (PR #$FEATURE_PR), tasks:$TASKS"

effort_of() {
  case $(gh api "repos/{owner}/{repo}/issues/$1" --jq '.labels[].name' | grep -oE '^Opus 5\.5 - [A-Za-z]+$' | head -1) in
    "Opus 5.5 - Low") echo low ;; "Opus 5.5 - Medium") echo medium ;;
    "Opus 5.5 - High") echo high ;; "Opus 5.5 - Extra") echo xhigh ;;
    "Opus 5.5 - Max") echo max ;; *) echo "" ;;
  esac
}

# The task's branch: its **Branch:** line, with <handle> filled in. A legacy task written
# before the switch has no such line, so its branch is derived from its number and title;
# /build-task tells the developer to use the branch its worktree is already on.
branch_of() {
  gh api "repos/{owner}/{repo}/issues/$1" --jq .body | tr -d '\r' |
    sed -n 's/.*\*\*Branch:\*\*[[:space:]]*\([^[:space:]][^[:space:]]*\).*/\1/p' | head -1 | sed "s/<handle>/$me/"
}
legacy_branch_of() {
  local title slug
  title=$(gh api "repos/{owner}/{repo}/issues/$1" --jq .title)
  slug=$(tr '[:upper:]' '[:lower:]' <<<"$title" | sed -E 's/[^a-z0-9]+/-/g; s/^-+|-+$//g' | cut -c1-50 | sed -E 's/-+$//')
  echo "users/$me/legacy-$1-$slug"
}

# The feature branch's ruleset allows no direct pushes, so main comes in through a
# short-lived PR whose head is the merge commit. Its name does not match users/*/feature-*,
# so the ruleset does not stop it being deleted afterwards.
merge_main_in() {
  git fetch origin main "$FEATURE_BRANCH" --quiet
  git merge-base --is-ancestor origin/main "origin/$FEATURE_BRANCH" && return 0
  local tree merge mb pr before
  before=$(git rev-parse "origin/$FEATURE_BRANCH")
  mb="users/$me/merge-main-into-feature-$FEATURE-$(git rev-parse --short origin/main)"
  pr=$(gh api "repos/{owner}/{repo}/pulls?state=open&base=$(uri "$FEATURE_BRANCH")&per_page=100" \
    --paginate --jq ".[] | select(.head.ref == \"$mb\") | .number" | head -1)
  if [ -n "$pr" ]; then
    merge=$(gh api "repos/{owner}/{repo}/pulls/$pr" --jq .head.sha)
  else
    tree=$(git merge-tree --write-tree "origin/$FEATURE_BRANCH" origin/main) \
      || halt "main conflicts with $FEATURE_BRANCH — merge it in by hand through a PR, then re-run"
    merge=$(git commit-tree "$tree" -p "origin/$FEATURE_BRANCH" -p origin/main \
      -m "Merge main into $FEATURE_BRANCH") || halt "could not commit the main merge"
    git push --quiet origin "$merge:refs/heads/$mb" || halt "could not push $mb"
    pr=$(gh api -X POST "repos/{owner}/{repo}/pulls" -f base="$FEATURE_BRANCH" -f head="$mb" \
      -f title="FEATURES: Merge Main Into $TITLE" \
      -f body="Closes #$FEATURE

Brings \`main\` into the feature branch before the next task." --jq .number) \
      || halt "could not open the PR merging main into $FEATURE_BRANCH"
  fi
  sleep 20
  bash scripts/gh-pr-wait-checks.sh "$pr" >>"$LOGDIR/feature.log" 2>&1 \
    || halt "CI red on PR #$pr, which merges main into $FEATURE_BRANCH"
  gh api -X PUT "repos/{owner}/{repo}/pulls/$pr/merge" -f merge_method=merge -f sha="$merge" \
    >>"$LOGDIR/feature.log" 2>&1 || halt "PR #$pr would not merge main into $FEATURE_BRANCH"
  gh api -X DELETE "repos/{owner}/{repo}/git/refs/heads/$mb" >/dev/null 2>&1 || true
  note "main merged into $FEATURE_BRANCH (PR #$pr)"
  git fetch origin "$FEATURE_BRANCH" --quiet
  if ! bash scripts/ci-covers-merge.sh "$REPO" "$merge" "$before" "origin/$FEATURE_BRANCH"; then
    sleep 20
    bash scripts/gh-pr-wait-checks.sh "$FEATURE_PR" >>"$LOGDIR/feature.log" 2>&1 \
      || halt "CI red on PR #$FEATURE_PR after merging main"
  fi
}

# One claude -p session, waiting out usage limits rather than halting on them. A QUESTION:
# line wins over everything: it is never mistaken for a limit, whatever else it says.
# $1 directory, $2 prompt, $3 effort, $4 log stem, $5 what it is (for the progress line).
run_session() {
  local attempt out until
  for attempt in $(seq 1 16); do
    out="$4.run$attempt.log"
    (cd "$1" && claude -p "$2" --model "$MODEL" --effort "$3" --permission-mode auto 2>&1 | tr -d '\r' >"$out")
    cat "$out" >>"$4.log"
    LAST_OUT=$out
    grep -q '^QUESTION:' "$out" && return 0
    if tail -n 20 "$out" | grep -qiE "usage limit reached|hit your (usage )?limit|limit (will )?resets? (at|in)"; then
      until=$(in_30_minutes)
      note "$5 paused on a usage limit until $until (attempt $attempt)"
      gh api -X POST "repos/{owner}/{repo}/issues/$FEATURE/comments" \
        -f body="Build paused on a usage limit until $until; it resumes on its own." >/dev/null 2>&1 || true
      sleep 1800
      continue
    fi
    return 0
  done
  halt "$5: still on a usage limit after 16 attempts"
}

# ---- the tasks ----
for n in $TASKS; do
  if has_label "$n" "status: done"; then continue; fi

  merge_main_in
  avail_kb=$(df -Pk "$REPO" | awk 'NR==2 {print $4}')
  [ "${avail_kb:-0}" -gt 10485760 ] || halt "#$n: less than 10 GB free — run node scripts/prune-merged-worktrees.mjs --apply"

  check_hash "$FEATURE"; check_hash "$(story_of "$n")"; check_hash "$n"
  effort=$(effort_of "$n"); [ -n "$effort" ] || halt "#$n has no Opus 5.5 Model - Effort label"

  branch=$(branch_of "$n")
  if [ -z "$branch" ]; then
    has_label "$n" legacy || halt "#$n has no **Branch:** line"
    branch=$(legacy_branch_of "$n")
  fi

  wt="$REPO/.worktrees/task-$n"
  if [ ! -d "$wt" ]; then
    git fetch origin "$FEATURE_BRANCH" --quiet
    if git ls-remote --exit-code --heads origin "$branch" >/dev/null; then
      git fetch origin "$branch" --quiet
      git worktree add -B "$branch" "$wt" "origin/$branch" >>"$LOGDIR/$n.log" 2>&1 || halt "#$n: worktree failed"
    else
      git worktree add -b "$branch" "$wt" "origin/$FEATURE_BRANCH" >>"$LOGDIR/$n.log" 2>&1 || halt "#$n: worktree failed"
    fi
  fi

  add_label "$n" "status: in-progress"
  note "#$n start ($branch, effort $effort)"

  run_session "$wt" "/build-task $n" "$effort" "$LOGDIR/$n" "#$n"
  q=$(grep -h '^QUESTION:' "$LAST_OUT" | tail -1)
  [ -n "$q" ] && halt "#$n asked: ${q#QUESTION: }"
  cost=$(gh api "repos/{owner}/{repo}/issues/$n/comments" --paginate --jq '.[].body' | tr -d '\r' | grep '^Build cost:' | tail -1)
  [ -n "$cost" ] || halt "#$n: no Build cost: comment — the session was cut off; re-run to resume"
  grep -q 'QA PASS' <<<"$cost" || halt "#$n: $cost"

  pr=$(gh api "repos/{owner}/{repo}/pulls?state=open&base=$(uri "$FEATURE_BRANCH")&per_page=100" \
    --paginate --jq ".[] | select(.head.ref == \"$branch\") | .number" | head -1)
  [ -n "$pr" ] || halt "#$n: no open PR from $branch into $FEATURE_BRANCH"
  # One commit, three checks: CI, QA and the merge must all be about the same head. A push
  # after QA or during the CI wait sends the task back rather than merging unreviewed code.
  tested=$(gh api "repos/{owner}/{repo}/pulls/$pr" --jq .head.sha)
  bash scripts/gh-pr-wait-checks.sh "$pr" >>"$LOGDIR/$n.log" 2>&1 || halt "#$n: CI red on PR #$pr"
  [ "$(gh api "repos/{owner}/{repo}/pulls/$pr" --jq .head.sha)" = "$tested" ] \
    || halt "#$n: PR #$pr's head moved while CI ran — re-run so CI and QA check the new head"
  qa=$(gh api "repos/{owner}/{repo}/issues/$pr/comments" --paginate --jq '.[].body' | tr -d '\r' | grep -E '^QA round [0-9]+:' | tail -1)
  case $qa in *": PASS"*) ;; *) halt "#$n: the latest QA round on PR #$pr is not a PASS" ;; esac
  qa_sha=$(grep -oE '\bat [0-9a-f]{7,40}' <<<"$qa" | tail -1 | cut -c4-)
  { [ -n "$qa_sha" ] && [ "${tested#"$qa_sha"}" != "$tested" ]; } \
    || halt "#$n: QA passed ${qa_sha:-an unnamed commit} but PR #$pr's head is ${tested:0:7} — re-run so QA checks the head"
  git fetch origin "$FEATURE_BRANCH" "$branch" --quiet
  before=$(git rev-parse "origin/$FEATURE_BRANCH")
  # The SHA guard: a commit pushed after QA and CI ran cannot merge unreviewed.
  gh api -X PUT "repos/{owner}/{repo}/pulls/$pr/merge" -f merge_method=merge -f sha="$tested" \
    >>"$LOGDIR/$n.log" 2>&1 || halt "#$n: PR #$pr would not merge into $FEATURE_BRANCH (or its head moved after CI)"
  gh api -X DELETE "repos/{owner}/{repo}/git/refs/heads/$branch" >/dev/null 2>&1 || true
  remove_label "$n" "status: in-progress"; add_label "$n" "status: done"
  note "#$n merged (PR #$pr) — $cost"

  # The merge re-runs CI on the feature PR. Wait for it unless the task PR's own green run
  # already tested exactly this tree — the usual case, when nothing else moved the branch.
  git fetch origin "$FEATURE_BRANCH" --quiet
  if bash scripts/ci-covers-merge.sh "$REPO" "$tested" "$before" "origin/$FEATURE_BRANCH"; then
    note "#$n feature CI not awaited — PR #$pr's green run tested the same tree"
  else
    sleep 20
    bash scripts/gh-pr-wait-checks.sh "$FEATURE_PR" >>"$LOGDIR/feature.log" 2>&1 \
      || halt "#$n merged but CI is red on PR #$FEATURE_PR — a cross-task break"
  fi
  git worktree remove --force "$wt" >>"$LOGDIR/$n.log" 2>&1 || true
done

# ---- the feature as a whole ----
merge_main_in
add_label "$FEATURE" "status: in-feature-qa"
note "all tasks merged — feature verification"
bash scripts/gh-pr-wait-checks.sh "$FEATURE_PR" >>"$LOGDIR/feature.log" 2>&1 \
  || halt "checks are red on PR #$FEATURE_PR"

wt="$REPO/.worktrees/feature-$FEATURE"
git fetch origin "$FEATURE_BRANCH" --quiet
# A worktree left by a halted verification is moved to the feature branch's new tip, so
# the re-run sees the fixes — unless it holds work that would be lost.
if [ -d "$wt" ]; then
  [ -z "$(git -C "$wt" status --porcelain)" ] || halt "$wt has uncommitted changes — put them in a PR or discard them, then re-run"
  [ "$(git -C "$wt" rev-list --count HEAD --not --remotes)" = 0 ] || halt "$wt has commits on no remote — push them in a PR or drop them, then re-run"
  git -C "$wt" checkout --quiet --detach "origin/$FEATURE_BRANCH" >>"$LOGDIR/feature.log" 2>&1 || halt "could not refresh $wt"
else
  git worktree add --detach "$wt" "origin/$FEATURE_BRANCH" >>"$LOGDIR/feature.log" 2>&1 || halt "could not create $wt"
fi
run_session "$wt" "/verify-feature $FEATURE" max "$LOGDIR/verify" "feature verification"
out=$LAST_OUT
q=$(grep -h '^QUESTION:' "$out" | tail -1)
[ -n "$q" ] && halt "feature verification asked: ${q#QUESTION: }"
grep -q 'FEATURE PASS' "$out" || halt "feature verification did not pass — see the Feature QA round on PR #$FEATURE_PR"
handover=$(gh api "repos/{owner}/{repo}/issues/$FEATURE/comments" --paginate --jq '.[].body' | tr -d '\r' |
  awk '/^Handover:/{found=1; buf=""} found{buf=buf $0 "\n"} END{printf "%s", buf}')
[ -n "$handover" ] || halt "no Handover: comment on #$FEATURE"
git worktree remove --force "$wt" >/dev/null 2>&1 || true

closes="Closes #$FEATURE"
for s in $STORIES; do closes="$closes
Closes #$s"; done
for t in $TASKS; do closes="$closes
Closes #$t"; done
gh api -X PATCH "repos/{owner}/{repo}/pulls/$FEATURE_PR" -f body="$closes

## Handover

${handover#Handover: }" >/dev/null || halt "could not update the body of PR #$FEATURE_PR"

# Marking a PR ready for review exists only in GitHub's GraphQL API, reached through gh api.
pr_node=$(gh api "repos/{owner}/{repo}/pulls/$FEATURE_PR" --jq .node_id | tr -d '\r')
gh api graphql -f query='mutation($id: ID!) { markPullRequestReadyForReview(input: {pullRequestId: $id}) { pullRequest { isDraft } } }' \
  -f id="$pr_node" >/dev/null || halt "could not take PR #$FEATURE_PR out of draft"
remove_label "$FEATURE" "status: building"; remove_label "$FEATURE" "status: in-feature-qa"
add_label "$FEATURE" "status: done"
note "feature PR #$FEATURE_PR ready for the maintainer to review and merge (merge commit)"
rm -f "$LOGDIR/driver.pid"
note "all done"
