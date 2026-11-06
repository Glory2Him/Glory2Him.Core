---
name: build-task
description: Build one signed-off task in its own session — developer agent, PR into the feature branch, CI, a fresh QA agent, fix rounds — and record its cost. Launched by the build driver as claude -p "/build-task <n>" at the task's labelled effort; can also be run by hand.
---

You coordinate one task's build. The task body, its user story and its feature are the
whole scope. You launch agents and relay their results; you do not build, review or
re-verify anything yourself.

Every GitHub call goes through `gh api` (REST).

## Keep this session small

- Read the task's body and labels once. Do not read comments, diffs, test output or
  `Documentation/`, except the `QA round` headers in step 6.
- If this session has already built a task, stop.
- If you were started by the build driver you cannot ask anyone: when a step below says
  stop and ask, write `QUESTION: <the question>` as your last line and stop. The driver
  halts the feature on it. When an agent you launched ends on a `QUESTION:` line, repeat
  that line, word for word, as your own last line and stop.

## Steps

1. **Check the task.** Stop if it lacks `ready for development`, if its feature lacks
   `approved`, or if its `Model - Effort` label asks for more effort than this session has.
2. **Resume, don't restart.** The task's branch is its `**Branch:**` line — or, for a task
   labelled `legacy` that has none, the branch this worktree is on. If that branch already
   exists with an open PR, skip to step 4. If a `Build cost:` comment already reports
   `QA PASS`, stop: it is built.
3. **Launch the `developer`** with: `Build task #<n>. Base your branch on <feature branch>.`
   For a `legacy` task with no `**Branch:**` line, add: `Use the branch this worktree is
   on, <branch>.` The feature branch is the head of the feature's draft PR. Wait for it to
   finish and relay its report in at most three lines.
4. **Check the PR merges and CI passes.** `gh api repos/{owner}/{repo}/pulls/<pr>` —
   `.mergeable_state`; on `unknown`, wait a few seconds and ask again. On `dirty`, launch a
   new `developer` with the task, the PR and the instruction to merge the feature branch
   in, resolve the conflicts, and push. Then wait for CI with
   `bash scripts/gh-pr-wait-checks.sh <pr>` (Bash `timeout` at its 600000 ms maximum; if
   the call is moved to the background, wait on it with `TaskOutput` and `block: true`). If
   a check fails, launch a new `developer` with the task, the PR and the failing job's log
   trimmed to the failures — `gh api repos/{owner}/{repo}/actions/jobs/<job-id>/logs`, the
   job id from the check run's `details_url`.
5. **Launch `qa`** with exactly `Act as QA. Verify PR #<pr> against task #<n>.`, at max
   effort for round 1, and for any later round whose fixes touched security, a policy, the
   schema or a migration; at high effort otherwise.
6. **Fix rounds.** On FAIL, or on a PASS that closes `MERGE READY: NO` because a finding
   is still open, findings owned by `planner` → stop and ask. Findings owned by
   `developer` → launch a new `developer` with `Address the QA findings on PR #<pr>.`, wait
   for CI as in step 4, then QA again as in step 5 with `Act as QA. Re-verify PR #<pr>.`
   Stop and ask after three failed rounds or a check failing twice on the same cause.
7. **Record the cost.** Post one comment on the task:

   ```
   Build cost: $<cost> (PR #<pr>, <date>, <agent model/effort list>, <n> developer runs, QA <PASS|FAIL> in <n> rounds, round 1 BLOCKING <n>)
   ```

   `<cost>` is the Cost line of `node scripts/token-usage.mjs --worktree` run in this
   task's worktree, when the script exists, else `n/a`. `--worktree` counts every session
   run there, so a task resumed after a usage limit or a halt reports its whole cost, and
   the latest `Build cost:` comment is always the total. The `QA PASS` or `QA FAIL` text is what the driver reads.
   Write `QA PASS` only when QA's last round closed `MERGE READY: YES`.
   On a subscription the figure is the API-equivalent cost, for comparison, not a bill.
8. **Finish** with the PR number, QA's verdict, the number of developer runs and the cost.
   Never merge the PR: the driver does that.
