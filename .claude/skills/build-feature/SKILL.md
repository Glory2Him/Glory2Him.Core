---
name: build-feature
description: Build every task of one approved feature on this machine — each in its own claude -p session and worktree at its labelled effort, merged into the feature branch with CI between — then verify the feature and hand it over for the maintainer to merge to main. Run as /build-feature <feature-issue> on your own machine; re-run it to resume after a halt or to see status.
---

You coordinate one feature's build. Every task is built by a separate
`claude -p "/build-task <n>"` session that the driver starts; you never build, review or
re-verify anything, and you never read a diff or test output.

## Keep this session small

This session stays alive for the whole feature, so what it reads is paid for on every later
call. Read the feature issue's body and labels once. Everything else arrives as one-line
milestones from the driver's log. Relay each in at most three lines. If your context passes
about 100K tokens, tell the user to start a fresh session and re-run `/build-feature <n>` —
the driver skips every task already labelled `status: done`, and `/build-task` picks up a
task whose PR is already open.

## Why a session per task

Effort is fixed when a session starts, and each task's `Model - Effort` label sets its
own. A separate `claude -p` per task is the only way each runs at its labelled effort. It
also inherits no context, keeps its own worktree, and lets `scripts/token-usage.mjs` cost
one task at a time. The price is that it cannot ask a question — see below.

## Where it runs

On your own machine: Windows under Git Bash is the reference setup, and Linux and macOS
work too. The driver needs `git`, `gh` (signed in), `jq`, `node` and `claude` on `PATH` —
on Windows, `winget install jqlang.jq`. One Claude Pro account is enough: each task runs
at its labelled effort, and the driver waits out a usage limit and resumes on its own.

## Steps

1. **Check before starting.** Stop and say so if:
   - the feature lacks `approved` — only a person adds it, never an agent;
   - any task under it lacks `ready for development`;
   - `df -h .` shows less than 10 GB free — run `node scripts/prune-merged-worktrees.mjs --apply`.

   A feature labelled `legacy` builds like any other once QA's legacy intake has signed
   it and the maintainer has added `approved`: the driver checks its `Signed:` hashes the
   same way.
2. **Tell the user the build order** — task number, tier, title and effort — and which
   tasks carry a migration.
3. **Start the driver in the background and follow its log:**

   ```bash
   bash .claude/skills/build-feature/feature-build.sh <feature-issue>
   ```

   It writes one line per milestone to `${TMPDIR:-/tmp}/feature-<n>/summary.log` and the
   same lines to a `Build progress` comment on the feature issue. Follow the log with the
   Monitor tool and relay new lines as they arrive. Do not go looking for more.
4. **When the driver halts**, read only what it names — a task's `Build cost:` comment, or a
   `QUESTION:` from a build session — and put it to the user. After they answer, re-run the
   driver; it resumes where it stopped — tasks labelled `status: done` are skipped.
5. **When it pauses for a usage limit**, tell the user the resume time. The driver resumes
   on its own.
6. **When the driver reports the feature PR ready**, finish with: the feature PR link, each
   task's PR and QA verdict, the feature QA verdict, the total cost, and the handover's
   external steps. The maintainer reviews and merges the feature PR into `main` with a
   merge commit, and that merge deploys to DEV. You never merge to `main`.

## The gate between tasks

Task N+1 starts only when task N has merged into the feature branch **and** CI is green on
the feature PR. Before each task the driver also brings `main` into the feature branch if
`main` has moved — through a short-lived PR, since the feature branch takes no direct
pushes — and waits for CI on that too; it halts rather than resolving a conflict.
Do not work around a slow gate.

## Questions

`claude -p` cannot ask anything. A build session that hits a stop condition writes
`QUESTION: <text>` and exits, and the driver halts the feature. That is the whole channel —
a story likely to raise one belonged in `/plan-feature`.

## Stop and ask the user when

The list is open: stop on anything else a person should decide.

- The driver halts: a missing `Build cost:` comment, a verdict other than `QA PASS`, a
  `QUESTION:`, a plan hash that no longer matches, a conflict with `main`, or red CI after a
  merge — a cross-task break, not the next task's problem.
