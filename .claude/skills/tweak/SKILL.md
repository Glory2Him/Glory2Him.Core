---
name: tweak
description: Build one tier 3 change — a bug fix, copy or styling change in one file, with no schema, no event and no boundary — with one developer and QA on the PR, straight to main. Your request is the approval. Run as /tweak <what to change> or /tweak <issue-number> on your own machine; /tweak --base release/vX.Y.Z <…> builds a hotfix against a release branch.
---

You coordinate one tier 3 change. You check it is tier 3, open the cut-down task, launch
one developer, and run QA on the PR. You do not build or review anything yourself.

The base is `main`, unless the request starts `--base release/vX.Y.Z` — a hotfix the
releaser cut from the last tag. Then use that branch wherever these steps say `main`.

Every GitHub call goes through `gh api` (REST).

## Steps

1. **Check the tier.** The change is tier 3 only if all of these hold — read just enough
   code to answer them:
   - no migration, table, policy, event, security boundary or session change;
   - no new behaviour: nothing a person can trigger that they couldn't before;
   - no new package;
   - it fits one file, plus assets it uses.

   If any fails, stop, say which part is not tier 3, and suggest splitting it: the
   presentation as a `/tweak`, the rest as a feature request.
2. **Open the task** (or rewrite the issue you were given) as type **Task** (or labelled
   `type: task`), labels `tier: 3` and `Opus 5.5 - Medium` unless the change needs more —
   never `ready for development`, which only QA applies.
   Title it `MINOR FIX: …` or `CODE RUB: …`; body: `Tier: 3`, the outcome, the one or two
   criteria that pin the change, which of the operation's paths it touches with the rest
   ruled out in one line, and `**Branch:** users/<handle>/fix-<slug>`. Your request is the
   approval for tier 3 — record it on the task as `Requested by @<you> via /tweak`.
3. **Launch the `developer`** with: `Build task #<n>. This is tier 3: base your branch on
   <base> and open the PR against <base>.` Add: `Test first as always: add the focused
   regression test that fails without the fix — test files do not count toward the one
   file. If an existing test asserts the exact old value this changes, update that value
   and list the test in the PR. Any other test change, stop and report it. If you reach
   about 60 tool calls, stop and report — it is probably not tier 3.`
4. **Wait for CI** with `bash scripts/gh-pr-wait-checks.sh <pr>` (Bash `timeout` 600000 ms).
   On a failure, send the failing output to a new developer once; on a second failure on
   the same cause, stop and ask.
5. **Launch `qa`** at high effort: `Act as QA. Verify PR #<pr> against task #<n>.` Fix
   rounds as in `/build-task`; at most two.
6. **Record the cost** on the task: `Tweak cost: $<cost> (PR #<pr>, <date>, <agent
   model/effort list>, <n> developer runs, QA <PASS|FAIL> in <n> rounds, round 1 BLOCKING
   <n>)`, from `scripts/token-usage.mjs` when it exists. On a subscription the figure is
   the API-equivalent cost, for comparison, not a bill.
7. **Finish** with the PR link, QA's verdict, the cost, and anything to check by eye. The
   maintainer reviews and merges it with a merge commit; a merge to `main` deploys to DEV.
