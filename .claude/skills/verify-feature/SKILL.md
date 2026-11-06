---
name: verify-feature
description: Verify a whole feature branch before it goes to main — a fresh QA at max effort against every business rule, role and migration, developer fix PRs into the feature branch for at most two rounds, then the handover. Launched by the build driver as claude -p "/verify-feature <n>" --effort max once every task is merged.
---

You coordinate the feature-level check. You launch agents and relay their results; you do
not review anything yourself.

Every GitHub call goes through `gh api` (REST). You cannot ask anyone: when a step says
stop and ask, write `QUESTION: <the question>` as your last line and stop.

## Steps

1. **Check readiness.** Every task under the feature carries `status: done`, and the
   checks on the feature PR have passed. If not, stop and ask. QA proves the migrations
   through the test run's own SQL Server container; DEV is where they first meet Azure
   SQL, once the feature merges.
2. **Launch `qa`** at max effort with exactly
   `Act as QA, verifying feature #<n> on PR #<p>.`
3. **Fix rounds, at most two.** On FAIL, or on a PASS that closes `MERGE READY: NO`
   because a finding is still open:
   - Findings owned by `planner` → stop and ask.
   - Findings owned by `developer` → launch a `developer` with
     `Fix the feature QA findings on PR #<p> on a branch users/<handle>/fix-feature-<n>-<round>, PR'd into <feature branch>.`
     Wait for that PR's CI (`bash scripts/gh-pr-wait-checks.sh <pr>`), launch `qa` with
     `Act as QA. Verify PR #<fix-pr> against the feature QA findings on PR #<p>.`, and when it
     passes, merge it into the feature branch with a merge commit. Then run step 2 again.
   - Still failing after two fix rounds → stop and ask.
4. **Write the handover** from `.claude/skills/build-feature/handover-template.md`, filled
   from the feature issue, its tasks' PR descriptions, their `Build cost:` comments and the
   feature QA rounds. Post it as a comment on the feature issue beginning `Handover:`.
5. **Finish** with `FEATURE PASS`, the number of feature QA rounds, and the handover's
   external steps in one line each.
