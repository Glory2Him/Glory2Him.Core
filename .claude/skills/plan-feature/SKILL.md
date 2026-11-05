---
name: plan-feature
description: Plan one feature issue into user stories and tasks, put the open questions to the requester in grilling rounds, each question with a recommended answer, and run QA's plan review until every task is signed off. Run as /plan-feature <feature-issue> on your own machine, on Opus 5.5 at high effort.
---

You coordinate the planning of one feature. The `planner` agent writes the plan and the
`qa` agent reviews it; you launch them and relay their reports. You do not write
criteria, review tasks, or read code yourself.

Every GitHub call goes through `gh api` (REST).

## Keep this session small

- Read the feature issue's body and labels once. Do not read the user stories or tasks
  the planner writes, the code, or `Documentation/`.
- Relay each agent's report to the user in at most three lines — except a round of
  questions, which you relay whole.
- If this session has already planned a feature, stop and ask for a new session.

## Steps

1. **Check the feature.** Stop and say so if:
   - it is not type **Feature** (or labelled `type: feature`), or it is the Global Rules
     issue — that is changed through `/grill-with-docs`, not planned;
   - the one open issue labelled `global rules` is missing or lacks `approved`: there are
     no settled rules to plan against yet;
   - `INTENT.md` does not exist: there is no statement of what the system is for to plan
     against — `/grill-with-docs` writes it with the user;
   - it already carries `approved`, `status: building` or `status: done`;
   - two other features already wait at `status: in-plan-review`,
     `status: awaiting-approval` or `approved` without having started building — say
     which should be built first.
   If the feature carries `legacy`, it is taken in, not planned: follow **Legacy intake**
   below instead of steps 2 to 5.
2. **Launch the `planner`** with: `Plan feature #<n>.` Nothing else.
3. **Questions, one round at a time.** If the planner stops on open questions, it has
   posted them as one `Questions round <r>` comment on the feature: numbered, each with
   its recommended answer. Relay the round as written — the numbering and the
   recommendations unchanged — and wait. "Yes" to a question accepts its recommendation.
   Post the answers as one comment, `Answers round <r>`, each against its question
   number, and launch a new `planner` with:
   `Plan feature #<n>. Questions round <r> is answered in the latest comment.`
   Repeat while the planner opens new rounds. Never answer a question yourself, and
   never let a "yes" stand for a question the user did not see.
4. **Plan review.** When the planner reports the tree complete, launch a new `qa` agent
   at max effort with exactly: `Act as QA, reviewing the plan for feature #<n>.`
5. **Fix rounds.** If QA fails the plan, launch a new `planner` with:
   `Address QA's findings on feature #<n>.` Then launch a new `qa` at max effort with the
   same brief as step 4. Stop and ask the user after the third failed round, or if a
   finding is disputed.
6. **Record the cost.** If `scripts/token-usage.mjs` exists, post one comment on the
   feature: `Spec cost: $<cost> (session <first 8 chars>, <agent model/effort list>)`
   from its Cost line. If the script reports an unpriced model, say so instead. On a
   subscription the figure is the API-equivalent cost, for comparison, not a bill.
7. **Finish.** When QA has set `status: awaiting-approval`, tell the user: the feature,
   the number of user stories and tasks, the tiers, any migrations, and that they — the
   maintainer — read the feature and add `approved` themselves. Only a person adds
   `approved`; you never do. Then `/build-feature <n>`.

## Legacy intake

A feature labelled `legacy` holds tasks signed off before this repository moved its
design into issues. QA takes it in instead of reviewing a plan;
`Documentation/Procedures/migrating-an-existing-repository.md` covers how it got here.

1. **Launch a new `qa`** at max effort with exactly
   `Act as QA, taking in legacy feature #<n>.`
2. **Fix rounds.** If QA fails the intake, findings owned by `planner` → launch a new
   `planner` with `Address QA's legacy intake findings on feature #<n>.`, then QA again as
   in step 1. A missing parent issue, or the planner's question whether a global rule
   changes or a task is withdrawn, goes to the user. Stop and ask after the third failed
   round.
3. **Finish** as steps 6 and 7 do, without user story or tier counts: a legacy feature
   is not re-planned.

## Stop and ask the user when

The list is open: stop on anything else a person should decide.

- The planner proposes splitting the feature.
- The feature needs a new global rule: that is a change to the Global Rules issue first —
  `/grill-with-docs` — and planning resumes once the maintainer has approved it again.
- The feature touches an area with an unstruck line under the Global Rules issue's
  **Legacy rules**: that line binds legacy features only, so it is settled first —
  `/grill-with-docs §<number>` — and planning resumes once the maintainer has approved
  the change.
- The feature needs a new dependency, package or event the request did not ask for.
- QA fails the plan three times, or a finding is disputed.
