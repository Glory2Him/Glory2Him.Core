# A new feature, step by step

What you type, and what happens, from a feature request to a feature merged into
`main`. It is written for the developer who owns the feature: you start every step
that Claude runs. [workflow.md](workflow.md) is the one-page overview of the whole
workflow, and `DEVELOPERS.md` explains why it works this way.

Have an existing document — a specification, a requirements document — rather than a
request? Port it first: [porting-planning-documents.md](porting-planning-documents.md).
It ends with feature requests, which then follow this procedure from step 3. Tasks
already signed against an older template's design documents follow
[migrating-an-existing-repository.md](migrating-an-existing-repository.md) instead.

## At a glance

| Step | Who | What you do | What you type | It ends with |
| --- | --- | --- | --- | --- |
| [1. Request](#1-raise-the-request) | Anyone | Fill in the Feature request form | — | Issue `#n`, `status: needs-design` |
| [2. Sharpen](#2-sharpen-it--optional) | You, with the requester | Answer an interview about the request | `/grill-with-docs <n>` | A `Settled before planning` comment |
| [3. Plan](#3-plan-it) | You | Start planning; answer the planner's question rounds | `/plan-feature <n>` | `status: awaiting-approval` |
| [4. Approve](#4-approve-it) | The maintainer | Read the plan and add `approved` | — | `approved` |
| [5. Build](#5-build-it) | You | Start the build; answer it if it halts | `/build-feature <n>` | The feature PR, ready for review |
| [6. Merge](#6-merge-it) | The maintainer | Review and merge with a merge commit | — | The feature in `main`, deployed to DEV |
| [7. Release](#7-release-it) | The maintainer | When ready to ship | `/release` | A `RELEASES:` PR |

Step 2 is optional. Every other step happens for every feature. A small change — a bug
fix, copy or styling in one file, with no schema, event or security change — skips all
of this: `/tweak <what to change>`, as [workflow.md](workflow.md#small-changes-and-hotfixes)
describes.

## Before the first feature

Once per repository, and once per developer. `/plan-feature` refuses to start until the
first two are true.

- **The Global Rules issue** is filed and carries `approved`. Find it with the
  `global rules` label in the repository's issue list. If it does not exist, file it
  from **Issues → New issue → Global rules** and fill it in with `/grill-with-docs`.
- **`INTENT.md`** exists, saying what the system is for.
- **Your machine is set up**: `git` and Git Bash, `gh`, `jq`, `node`, the .NET SDK and
  Docker installed; Claude Code signed in with your Claude Pro account (`/login`), and
  GitHub with your own account (`gh auth login`).

## Starting Claude Code

Every command below runs in Claude Code on your own machine, in the repository's
checkout. On Windows, open Git Bash:

```bash
cd <the-repository>
git switch main
git pull
claude
```

The terminal in VS Code works too: open the repository and start `claude` there. A
build runs for hours, on your machine, so leave it awake while it does. If you want to
follow it, and answer it, from a phone or a browser, you can start that session with
Remote Control — it is optional:

```bash
claude --remote-control "feature-500"
```

Start a **fresh session for each step**. Each skill keeps its own session small, and
`/plan-feature` refuses a session that has already planned a feature.

---

## 1. Raise the request

Anyone can raise one. In GitHub: **Issues → New issue → Feature request**. Blank issues
are off.

| Field | What makes it good |
| --- | --- |
| **Summary** | One or two sentences: what someone can do that they cannot today, and why it matters |
| **Who uses it** | Every role involved, and who must *not* be able to use it |
| **Behaviour** | Given/When/Then scenarios, one per block — the usual path and the awkward ones: empty, too many, out of order, someone else's record |
| **What must not happen** | What would be wrong even if every scenario passed |
| **Mockups** | Drag in screenshots and finished diagrams, or link an HTML export under `Documentation/Mockups/`. A draft in `Documentation/Diagrams/` does not count |
| **Data** | What is stored or read, who owns it, how long it is kept; any personal or sensitive data |
| **Done when** | What you will try in DEV, and what you expect to see |
| **Out of scope** | What it deliberately does not do |
| **Ported from** | Only for a request ported from `Documentation/Planning/` |

The issue gets the type **Feature** and the labels `type: feature` and
`status: needs-design`. Note its number — `500` in the examples below.

The planner reads the request word for word and plans from nothing else. A rule written
only in a mockup, a chat or a meeting does not exist for it.

## 2. Sharpen it — optional

Worth doing for anything bigger than a few scenarios, and whenever the request leaves
you with questions of your own. Sit down with the requester, or have them to hand, and
start a session:

```
/grill-with-docs 500
```

Claude reads the request, the Global Rules issue and the code, then interviews you in
**rounds**. Each round asks only the questions that can be answered now, numbered, each
with a recommended answer:

```
❓ Q1 — Who may see a saved search (BR-2): only the member who saved it, or everyone
in their group?

➡️ Only the member who saved it. Sharing is out of scope.

---

❓ Q2 — …
```

Answer the whole round in one message. "Yes" accepts a recommendation, so you only write
out what you would decide differently:

```
1 yes. 2 no — keep them for a year, then delete. 3 yes.
```

It looks up anything the code or the issues can answer rather than asking you, and asks
the next round until nothing is left open. It never edits the request itself. What you
settle goes to:

- **a `Settled before planning` comment** on the feature, which the planner reads as
  answers;
- **the Global Rules issue**, for a new term or a rule other features will follow too.
  That change is reviewed by QA and then needs the maintainer's `approved` before
  `/plan-feature` will plan against it.

## 3. Plan it

In a fresh session:

```
/plan-feature 500
```

**It checks first** and stops, saying why, if the issue is not a Feature, if it is
already approved or building, if the Global Rules issue is missing or not approved, or
if two other features already wait between planning and building — it says which to
build first.

**Then the planner works.** It writes the feature's design sections below the request —
business rules `BR-1`…, user stories, entity count, events, storage, security, risks —
and opens a user story sub-issue for each component and a task sub-issue for each
operation, each task with its acceptance checklist and its `Model - Effort` label.

**If it has questions**, it posts them on the feature as `Questions round 1` and stops.
The skill shows you the round in full, in the same numbered format as above. Answer in
the session the same way — or bring in the requester for the ones only they can answer.
The skill posts your answers as `Answers round 1` and starts a new planner, which asks
`Questions round 2` only if your answers opened new questions.

**Then QA reviews the plan**, fresh and at maximum effort: every business rule covered
by a criterion, every operation backed by a task, size, paths, tiers and labels.
Findings go back to the planner and round again until QA signs every task off with
`ready for development`, with no finding open. You watch; there is nothing to type.

**It ends** by setting the feature to `status: awaiting-approval`, posting a
`Spec cost:` comment, and telling you how many user stories and tasks there are, their
tiers and any migrations.

**It stops and asks you when** the planner proposes splitting the feature (each part
becomes its own request), the feature needs a new global rule (run
`/grill-with-docs` for it, wait for its approval, then run `/plan-feature 500` again),
it needs a new package, event or dependency nobody asked for, or QA fails the plan three
times. That list is not the whole of it: wherever an agent hands something to you, the
work stops there.

## 4. Approve it

The maintainer reads the feature issue: the business rules, the user stories, the
security section, the risks and any deviations, and skims the tasks. If content, they
add the label **`approved`** to the feature. Only a person adds it — never an agent.

If something needs to change, do not edit the issues in place: QA signed a hash of
every body, and the build refuses to start on one that changed. Follow
[Changing an approved plan](workflow.md#when-things-stop) instead.

## 5. Build it

On your machine, in a fresh session — started with Remote Control if you want to
follow it from elsewhere:

```
/build-feature 500
```

**It checks first** — that the feature carries `approved`, that every task carries
`ready for development`, and that the disk has room — then shows you the build order:
each task with its tier and effort, and which carry a migration.

**Then it builds in the background.** For each task in order, a separate Claude session
at the task's labelled effort writes the tests first, implements, opens a PR into the
feature branch, waits for CI and gets a fresh QA review, with fix rounds until QA
passes. The driver merges each task PR into the feature branch before starting the
next. Every milestone appears as a line in the session and in a `Build progress`
comment on the feature issue. Keep the machine awake until it finishes.

**When every task is in**, a fresh QA verifies the whole feature at maximum effort —
every business rule through the API, every role allowed and refused, the full suite, the
migrations — and fixes what it finds in up to two rounds. Then it posts a `Handover:`
comment and takes the feature PR out of draft, labelled `ready for review`.

**If it halts**, the feature gets a `Build halted:` comment and `status: blocked`.
Usually a build session reached something only a person can decide, and the comment
holds its question. Answer it on the issue, or fix the cause, then run
`/build-feature 500` again — in a fresh session if the old one has grown long. It
skips every task already done. The table under
[When things stop](workflow.md#when-things-stop) covers each kind of halt.

**If it pauses** on a Claude usage limit, it says when it will resume, and does so on
its own. On a Pro account a long feature can pause more than once.

## 6. Merge it

The maintainer reviews the feature PR — how the tasks fit together, and the handover:
migrations and their order, configuration and secrets, open advisory findings and the
cost — and merges it with **Create a merge commit**, never squash or rebase. The
feature, its user stories and its tasks close, and the merge deploys to **DEV**.

## 7. Release it

When you want to ship what has merged since the last release, the maintainer runs
`/release`, confirms the version it proposes, and merges the `RELEASES:` PR once QA has
checked it. That release also writes the feature's page under
`Documentation/Design/Features/`. [workflow.md](workflow.md#8-release--the-maintainer)
has the steps.

---

## What you will see on the feature issue

| When | Comment or label |
| --- | --- |
| Raised | `type: feature`, `status: needs-design` |
| Sharpened | `Settled before planning` |
| Planning | `Questions round <r>` and `Answers round <r>`; `status: in-plan-review`; `Plan QA round <r>` |
| Planned | `ready for development` on every task; `status: awaiting-approval`; `Spec cost:` |
| Approved | `approved` |
| Building | `status: building`; `Build progress`; a `Build cost:` comment on each task |
| Halted | `Build halted:`; `status: blocked` |
| Verified | `status: done`; `Handover:`; the feature PR out of draft |
| Merged | Closed, with every user story and task |
