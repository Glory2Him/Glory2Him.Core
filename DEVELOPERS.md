# Developing in this repository

How work moves from an idea to merged code here, and how to drive the three
Claude Code agents that do most of it.

`CLAUDE.md` is the short version an agent loads automatically. This is the long
version for a person: it explains the same workflow, plus the parts an agent
never sees — where a mockup goes, how to brief a fresh session, and which
conventions are enforced by tooling rather than by good intentions.

Read `INTENT.md` for what the system is for, and `Documentation/Design/design.md` —
the index to the design documents — for how it is designed. This repository's
design predates feature and user story documents: the global and area documents
hold it, and `design.md` maps them (§4).

---

## 1. How work breaks down

Work is broken down the way Azure DevOps does it:

| Level | What it is | Example | Where it lives |
| --- | --- | --- | --- |
| **Epic** | the whole product | Student Portal | `INTENT.md`, and the global design documents |
| **Feature** | something that ships and works on its own | Student registration | a feature document |
| **Sub-feature** | a part of a feature too large to plan in one go | Password reset, inside Account management | a sub-feature document |
| **User story** | one component at one level — it can do something, but need not work on its own | the Student foundation service; a master page | a user story document, under `Backend/` or `UI/` |
| **Task** | one operation of a user story | `IStudentService.AddStudentAsync` | a GitHub issue |

A feature is built by several user stories, usually at several levels — a storage
broker, a foundation service, a controller, a page. A user story never spans
levels. Its operations are its tasks: **one task per operation**, with that
operation's logic, validations and exception handling together, never split
apart. The direct path and its event path are two methods, so two tasks of the
same user story.

A feature can span several screens, and each screen is its own user story. A user
portal is one feature, not two: its master page and its detail page are two UI
user stories, and the operations they offer between them — add a user, search
for a user, view, modify and remove one — are their tasks.

Every level names its parent: a sub-feature its feature, a user story its feature
or sub-feature, a task its user story. `Documentation/Design/design.md` defines
the levels, and §4 shows where each document lives.

Only tasks are GitHub issues. Writing the design is work too, tracked the same
way: a **design task** is a `DESIGN:` issue whose output is design documents
rather than code.

### The pipeline

```
mockup            (UI work only — Documentation/Mockups/)
   ↓
planner           the design, when the work needs it: feature and user story documents,
                  every business rule the mockups show, any epic-level rule
   ↓
planner           the tasks: one GitHub issue per operation, each with a sign-off checklist
   ↓
qa                reviews the design and the tasks, fresh — coverage, completeness, size
   ↕              findings go back to the planner, who corrects them and hands back
                  `ready for development` on each task it signs off,
                  `ready for review` on the design PR once all of them are
   ↓
YOU               merge the design PR, if the work had one
   ↓
developer         test first, one criterion at a time → commits, branch, then the PR
   ↓
qa                adversarial verification against the criteria, fresh
   ↕              findings go back to the developer, who fixes them as commits on the PR
                  `ready for review` on the PR once it passes
   ↓
YOU               merge
```

**Every arrow is yours.** The agents do not hand work to each other — none of
them can invoke another, because none has a Task tool. You are the only thing
that moves work between roles, and the artifact each role leaves behind is the
whole of the handoff. Two of the rows are also decisions only you can make:
merging the design, and merging the work. Approving the criteria is QA's: its
`ready for development` label is what the developer starts on.

**Every handover to QA is fresh.** QA starts with an empty context and a brief
that points — the tasks, the design PR, the PR — never the other agent's summary
or reasoning: it reviews what was delivered, not the account of it. The planner
and the developer are the opposite: QA's findings go back to them with context,
as its round comment. The work comes to QA on a PR — the design on its design
PR, the code on the PR the developer opens before handing over — and every
correction is a further commit on that same PR, or an edit to a task. The loop
repeats until QA signs the work off, and each round is smaller than the last:
after round 1, QA reviews only what changed since its previous round and what
that touches. The developer or QA may hand a question to the planner, with
context. A planner change to anything an open, signed-off task depends on — the
task, or the design it cites — takes `ready for development` off until QA has
agreed it, and the developer waits for any design change to reach `main`, so it
never acts on criteria QA has not seen. When a round has findings for both the
planner and the developer, route the planner's first: the developer's fix round
waits until the changed task is signed off again — or, when the planner settles
its findings without changing the task, until your brief says so. A disputed
finding comes to you: the planner rules when the developer disputes one about a
task or the design, and anything else — including a finding the planner disputes
about its own work — is yours to decide.

**The planner pushes back when the design is too high-level.** It goes straight
from the design to tasks when the change is simple, but it will not invent the
rules a task encodes. With no feature document, or one whose mockups have not
been mined for their business rules, or no user story to carve a task from, it
stops and proposes a design task first. §8 walks that path end to end.

The planner sizes the process to the risk of the change, and states the tier on
each task's first line:

| Tier | Applies to | The planner writes |
| --- | --- | --- |
| **1: design** | A new entity, a schema change or migration, a new event, a change to the security boundary, or a new service, layer or dependency | The design the work needs — feature and user story documents, any epic-level rule — then the tasks |
| **2: behaviour** | New behaviour inside the existing design — no schema, no event, no boundary crossed | The tasks, criteria only |
| **3: fix or tweak** | A bug fix, a copy or styling change — one file, no schema, no event, no boundary | The task, cut down to the outcome and the one or two criteria that pin the change |

No tier skips the task, the approval or QA on the work: the PR linter fails a PR
that closes no issue, and the developer builds nothing that is not in an
approved criterion. What a lower tier skips is the design.

Skip stages deliberately, not by accident:

| Stage | Skip it when |
| --- | --- |
| mockup | there is no UI surface |
| the planner's design | tier 2 or 3 — no schema, no event, no layer boundary crossed |
| planner | never — the developer refuses a task with no approved criteria |
| qa on the tasks | never — its `ready for development` label is the approval, and the developer refuses a task without it |
| developer | never |
| qa on the work | never for anything that ships |

---

## 2. Every agent runs in a fresh session

This is the rule people get wrong most often, so it is stated before anything
else.

**Each role is a separate session with an empty context.** The developer cannot
see the planner's reasoning, only what the planner wrote down. QA is deliberately
given a fresh context so it argues with the code rather than with the
developer's summary of it.

That has one consequence worth internalising: **if it is not in the artifact, it
does not exist.** A decision made in conversation with the planner and not
written into the design documents or the task is lost the moment that session
ends.

### What each role leaves behind

| Role | Durable artifact | Where the next role reads it |
| --- | --- | --- |
| planner | for tier 1, the design | feature and user story documents under `Documentation/DesignFeatures/`, and any epic-level rule in `Documentation/Design/`, listed in `design.md` |
| planner | the tasks — one issue per operation, each naming its user story, with the tier and a sign-off checklist | the GitHub issue body, under `## Acceptance criteria` |
| developer | commits, a branch, a PR, a handoff report | the PR and its diff |
| qa | BLOCKING / ADVISORY findings, each naming its owner | a numbered round comment — on the PR, on the design PR, or on each task when a task review has no design PR — and its labels: `ready for development` on each task it signs off, `ready for review` on a PR or design PR it passes |

### How to brief a fresh session

Give the agent three things: **the role, the issue number, and where to read.**
Everything else it can find for itself.

```
Act as the planner. Read issue #512 and plan it.
```

```
Act as the planner. The user story for issue #512 is at
Documentation/DesignFeatures/Backend/SavedSearchService.md §1. Write its
acceptance criteria into the issue.
```

```
Act as the developer. Implement issue #512. QA has signed its criteria off and
it carries `ready for development`.
```

```
Act as QA. Verify PR #520 against the acceptance criteria on issue #512.
```

The agent will read the task, the design and the code itself. Do not paste the
previous session's transcript in — if an agent needs something to do its job and
cannot find it, that is a signal the artifact is incomplete, and the fix is to
improve the artifact rather than to narrate it.

QA's brief is the strictest: the mode and the numbers, nothing more — never what
the other agent did, what it fixed, or what to look at. Each agent ends its run
with the brief for the next one — QA's for whoever owns its findings — so you
rarely write one. The planner and the developer, by contrast, get QA's findings
as context: their brief points at the round comment.

#### Two briefs that need more than a pointer

Both hand the planner a **source** instead of an issue to read. Say what the
source is, how much authority it carries, and what you want out of it — otherwise
the planner has to guess whether it is describing a decision or proposing one.

**From a Claude Design mockup.** The job is to turn a picture into words, because
a picture cannot become a test name:

```
Act as the planner. Issue #511 is the design task for saved searches, with
Claude Design mockups at Documentation/Mockups/saved-searches/ — panel.html for
the interaction and panel.webp for the screens, with the images embedded in the
issue body. Open the HTML, not just the image: hover states, spacing and the
real DOM are in there.

Write the feature document, linked to the mockups, with every business rule they
show. Name the user stories that build it — one component at one level each —
and write their documents, one section per operation. Name the component
boundaries, the state each owns, the events they raise and what the server
re-decides regardless of what the client shows.

Anything you cannot state in words is not design yet. Say so rather than citing
the picture — the tasks are written from these documents alone, and "matches the
mockup" is not a criterion.
```

When it is done, go back and add the **Superseded by** line to the mockup folder's
README (§5.1). The feature document is authoritative from that moment and the
mockup is history.

**From a design written somewhere else.** Porting an existing document — a sketch,
a specification, a wiki page, a design carried over from another repository:

```
Act as the planner. There is an existing design at
Documentation/Imported/legacy-search.md, written before this repository existed.
It describes behaviour we intend to keep, but it is a source input and not an
authority.

Port what still applies into the design documents in their conventions —
epic-level rules into the global document that owns the area, what a feature does
into its feature document, what a component does into its user story document.
Do not restate it wholesale — the parts that no longer apply must not survive the
move just because they were written down once.

Where it disagrees with what is already in the design documents, they win, and
say so explicitly rather than silently choosing. List at the end what you
deliberately dropped and why, and anything you could not verify against the
code — a claim you could not check is not a design decision you can make on its
behalf.
```

Record where it came from. A ported section that does not name its source reads
as a decision someone made here, and the next person cannot tell which parts were
inherited and which were chosen.

### Set the developer's effort before you invoke it

`.claude/agents/developer.md` pins `model: opus`, because every label names Opus
5.5 (§10), and pins no `effort:` on purpose: the task's `Model - Effort` label
decides the effort, per task rather than per role — but **nothing in this
repository reads that label and configures a session.** No hook, no script, no
mechanism. You set the effort by hand, before you invoke the developer, because
a session cannot change its own effort once running.

The developer can only detect a mismatch afterwards and stop, and where it cannot
see its own effort it says which effort the label asks for so you can confirm
it. A task with no `Model - Effort` label is not ready to start.

All three roles are pinned to `opus` in their own files. The planner also pins
`high`; QA pins `max` deliberately, so the reviewer is never reasoning less hard
than the implementer did.

---

## 3. The three agents

Defined in `.claude/agents/`. Each file is the authority on its own role; this
section tells you when to reach for which.

### planner — design and tasks, not code

**Owns** the breakdown — feature, sub-feature, user story, task — the size check
and the risk tier, and everything the design decides: the business rules, layer
placement, entity count, event contracts, the security boundary, storage and
migration shape. Then the acceptance criteria, precise enough that a developer
can write a failing test from them without asking a question.

**Produces**, for tier 1, the design first:

- any epic-level rule, in the global document that owns its area;
- a feature document — its problem, its business rules including every rule its
  mockups show, the user stories that build it and their levels, the entity count
  (this is what decides the layer — one entity means foundation or processing,
  two or three means orchestration, more than three is a violation), event
  contracts in `<Subject>-<Verb>` form, storage and migration shape including the
  seed consequence, risks split into reversible and not, and an explicit
  out-of-scope list;
- one user story document per component, naming its parent and giving each
  operation its own section.

Every document cites the rules above it instead of restating them, and records
any deviation with its reason. "No design needed" is a valid outcome and you
should expect it often — that is tier 2 or 3.

**Then the tasks, one per operation** — one public method on one component's
interface — with everything the method needs: its logic, its validations and its
exception handling, in one task and never split apart. That is the unit The
Standard's branch name encodes (`foundations-student-add`: one category, one
entity, one action) and the unit `the-standard-testing` requires every path for.
The direct path (`AddStudentAsync`) and its event path (`OnAddingStudentAsync`)
are two methods, so two tasks of the same user story, though both converge on one
private `DoAddStudentAsync`. The planner writes a feature's tasks as a set in
build order, bottom-up, each naming the tasks it builds on.

Each task carries the tier on its first line, its parent user story and the
operation it delivers on the next two, and the criteria under an
`## Acceptance criteria` heading as a sign-off checklist — the three kinds of test
The Standard writes for every operation:

```markdown
- [ ] Logic tests
  - [ ] Happy path — succeeds under a security context allowed to do this
  - [ ] Negative path — refused under a security context that may not do this
- [ ] Validation tests
- [ ] Exception tests
```

Each numbered criterion is a box under its group. The developer ticks a box when
its test goes green, and QA checks every tick against the test behind it. An
operation open to every caller has no negative path, and says so. The task's
title is the one its PR will carry, `CATEGORY: Description In Pascal Case`, the
category naming the layer. **The task is the unit of work.** There is no parallel
file for the criteria, deliberately — a second document would drift from the task.

Aim for five to eight logic criteria; an operation's validations and exceptions
never count toward its size. If the logic alone runs past about eight criteria,
the operation does too much, and the planner raises it as a design question
rather than splitting the operation's paths across tasks.

Every criterion is written Given/When/Then and must be expressible as a single
test name. If you cannot imagine the test name, the criterion is not finished.

**Use it** for every piece of work; the tier decides how much it writes.
Everything it writes goes to QA before any developer sees it. Expect it to push
back — proposing a design task — when the design above the work is too
high-level to plan from.

It never writes production code or tests, and edits no file outside
`Documentation/` — tasks change through `gh`. It does not review finished work
either: structural findings on a PR — a skipped layer, a decision in a broker —
are part of QA's checklist.

### developer — test first, one criterion at a time

**Owns** implementation. **Produces** commits, a branch, a PR, and a written
handoff naming the criteria implemented, the tests covering each, any migrations
added, the presentation decisions no criterion made, and the commit SHAs —
ending in its own verdict line, `MERGE READY: YES` or `MERGE READY: NO`, which
judges only whether its work is done and never whether a human has approved it.

The loop per criterion is: write one test, run it and confirm it fails **for the
right reason**, commit it as `{TestName} -> FAIL`, implement the smallest change
that passes, commit as `{TestName} -> PASS`, and tick the criterion's box. No
production code exists without a failing test that demanded it.

**Use it** only when the task carries `ready for development` — QA's sign-off
on its criteria. It builds the one operation the task names and stops if the
work needs another. It opens the PR once the work is done and before it hands
over to QA, and fixes QA's findings as further commits on that PR.

### qa — adversarial, never fixes

**Owns** finding the reasons a change should not ship. **Produces** findings
marked BLOCKING or ADVISORY. It never fixes anything, deliberately: the person
who broke it should fix it, and a reviewer who patches defects stops looking for
more.

It verifies against the code, never against another agent's account: always run
it in a fresh session, briefed with pointers only — that is the whole point of
it. After round 1, each round reviews only what changed since the last one and
what that touches.

It has a **second mode**, defined in its own agent file: reviewing **the tasks**
before their code is written, or when a ruling changes one — §8 step 4. Does
every operation in the feature's user stories have a task, do the tasks together
cover every business rule, does every level name its parent, is any task too
big, can every criterion become a test name. That review is never skipped: it
signs off each task it clears with `ready for development`, the approval the
developer starts on, and labels the design PR `ready for review` once every task
it carries is signed off — and where a feature has more than one task, nothing
else in the pipeline ever asks whether the set is complete.

Say which mode you want when you brief it; verifying a diff is the default.

**Route failures by owner** — QA names one on every finding: implementation
defects to the developer, including code that departs from a sound design;
missing or contradictory criteria, or a design that got a boundary or a layer
wrong, to the planner.

---

## 4. The Documentation folder

```
INTENT.md                  the epic — what the product is for
Documentation/
  G2H Design.md            the overview (§IDX1): principles, the numbering and citation
                           rules, and the map from every pre-split section to its file
  Design/
    design.md              the index — the levels, a map of every design document, the conventions
    Architecture.md        epic: layers, services, dependencies          §ARC12, §ARC16, §ARC17
    Security.md            epic: authentication, authorisation, identity §SEC14, §SEC18
    Events.md              epic: event contracts, how a service implements events   §EVN0 … §EVN23
    Domain.md              epic: entities and their invariants           §DOM2 … §DOM6, §DOM11, §DOM19
    Approval.md            the approval process, designed before feature documents  §APR7, §APR8, §APR9, §APR13
    UI.md                  UI / UX design, designed before feature documents        §UI20
  DesignFeatures/
    <Feature>.md           a feature, or a sub-feature naming its parent feature
    Backend/<Story>.md     a backend user story — one component at one level
    UI/<Story>.md          a UI user story — one component at one level
  Mockups/                 Claude Design exports, linked from the features they feed
  Images/                  static visual assets referenced from issues and design docs
  DependencyGraph/         generated architecture graph and its viewer
  ModelBudget/             tally.py, the review of what the model budget bought
  Glory 2 Him.drawio       the original design sketch, cited by §IDX1.3 as a source input
  Prompt-CreateFoundationService.md   a standalone prompt template, predating the agents
```

`design.md` holds no design of its own. It defines the levels, maps every design
document, and carries the conventions they follow — keep it current, because a
document it does not list is one nobody finds.

### Epic rules, features and user stories

Each level holds its own kind of decision, and the planner decides which level a
decision belongs to before writing it:

- **The global documents** hold the epic-level rules every feature must follow —
  how a service implements eventing, how a user authenticates, how layers depend
  on each other. They stay global: the screens a security rule needs, such as
  sign-in or 2FA, are UI user stories that cite `Security.md`, not UI written
  into it.
- **A feature document** holds what one feature does: its business rules —
  including every rule its mockups show — and the user stories that build it. A
  feature too large to plan in one go lists sub-features instead, each in its own
  document naming its parent.
- **A user story document** holds what one component does, one section per
  operation, and names its parent feature or sub-feature.

**Nothing restates what is above it.** A feature cites the global rules —
`per §EVN2` — and a user story cites its feature's business rules, so there is
only ever one copy to keep true. A document that must depart from a global rule
records it in its own **Deviations** section: the rule, the reason, and how it is
done instead. The planner proposes a deviation; your approval grants it.

`Approval.md` and `UI.md` were written before features had documents of their
own, so they hold feature-level design in the area layout. A new feature gets a
feature document.

### Numbering and citation

`G2H Design.md` was broken into area-scoped files under `Documentation/Design/`
(issue #481), and the map at the top of it still says, for every one of the 21
original sections, which file it is in and what to cite it as. §IDX1.5 holds the
numbering and citation rules in full.

Sections in the global and area documents carry a **prefixed number** and never
one that restarts at 1, so that a bare citation stays unambiguous with several
files side by side. A relocated section keeps the number it already had and
gains its file's prefix — `§20.6.1` became `§UI20.6.1` — and carries a
`(formerly §20.6.1)` annotation naming its old position, so the C# comments that
cite the old number still resolve by grep. `Events.md` is the one file that
renumbered instead, flat as `§EVN1`, `§EVN2`, because it merged two independently
numbered documents; §IDX1.5 records that as an exception rather than the pattern
to copy. **If you add a new area file, copy the convention from `UI.md`'s intro
block.**

A feature document numbers its business rules, and a user story document its
operation sections; both are cited by their path under `DesignFeatures/`, the
form §IDX1.5 gives a document that carries no prefix.

Two cautions learned the hard way:

- **Resolving is not the same as being right.** The annotation maps an old number
  to a new one; it says nothing about whether the section was the correct one to
  cite originally.
- **Nothing validates citations on every commit.** No CI step.
  `Tools/design-split-audit.sh` checks the split's completeness and citation
  gates when you run it, and nothing runs it for you. The guarantee that an old
  `§10.X` still resolves is maintained by the annotation convention and that
  script, run by hand.

### Citing design from code

Code comments cite design sections constantly, and this is the main reason the
numbering discipline matters:

```csharp
// design §14.6 rule 2: either service must be safe when called alone
// (§EVN2 rule 4, §EVN18(a))
// design SavedSearches.md rule 3
// design Backend/SavedSearchService.md §1
```

Most of the codebase still cites the pre-split `§N.N` form. That is expected —
the `(formerly …)` annotations exist precisely so those citations keep working.

---

## 5. From a Claude Design mockup to a feature

UI work usually starts as a picture. The job of this stage is to get the picture
into the repository and then **out of the critical path** — every rule it shows
written into a feature document — because a picture is not a test name and a
mockup left as a second source of truth will eventually contradict the design.

### 5.1 Put the export in the repository

A Claude Design export is a single self-contained HTML file. Save it to:

```
Documentation/Mockups/<feature-slug>/<screen-name>.html
```

Alongside it, save a **flattened image** of each screen — `.webp` or `.png` — in
the same folder. The image is what you embed in the issue; the HTML is what
someone opens when they need to see hover states, spacing or the real DOM.

Bundled exports run to several megabytes. Keep the HTML when the interaction
matters and the image alone when it does not, and do not commit five versions of
the same screen because a design iterated.

Every mockup folder gets a `README.md` with three lines: what it shows, the issue
it came from — or `Issue: none yet, this started the work` when the mockup came
first — and, once the planner has written the design, **the feature document it
produced**. That last line is what stops the mockup becoming a rival source of
truth:

```markdown
# Saved searches panel
Source: Claude Design export, 2026-09-11. Issue: #511.
Superseded by `Documentation/DesignFeatures/SavedSearches.md` — its business rules
win wherever the two disagree.
```

Commit it on its own, with a `DOCUMENTATION:` prefix.

### 5.2 Embed it in the issue by pinned commit

When you reference an image from a GitHub issue, use a **raw URL pinned to the
full 40-character commit SHA**, never a branch:

```markdown
![Panel](https://raw.githubusercontent.com/<owner>/<repo>/<40-char-sha>/Documentation/Mockups/saved-searches/panel.webp)
```

Pinned to a SHA, the picture in the issue cannot change under it later. Pair the
images with prose, a component tree and an event-hook table — that is the level
of written detail the planner needs to write the design and criteria from.

Drag-and-dropping an image into the GitHub comment box also works and is hosted
by GitHub, but it lives nowhere in the repository. Use it for a throwaway
annotation, not for the design.

### 5.3 Ask the planner for the design task

```
Act as the planner. Issue #511 has a mockup at
Documentation/Mockups/saved-searches/ and images embedded in the issue body.
Treat it as the design task: write the feature document, linked to the mockup,
with every business rule it shows, and the user story documents that build it.
```

The planner writes the documents, numbers the business rules and the operation
sections, and tags each operation `(needs issue)` (§6). From that moment the
feature document is authoritative and the mockup is history — go back and add the
"Superseded by" line to the mockup's README.

The tasks come second, derived from the design in words. "Matches the mockup" is
not a criterion, because it cannot be a test name.

---

## 6. Linking design and tasks

Two mechanisms, deliberately different, answering two different questions.

### Operation tags — "which task delivers this operation?"

Every operation section in a user story document carries exactly one of two tags,
never bare:

```markdown
## 1. AddSavedSearchAsync (#512)
## 2. RemoveSavedSearchByIdAsync (needs issue)
```

`(#N)` names the task that delivers the operation. `(needs issue)` is an explicit,
greppable flag for an operation nobody has scheduled yet. The task names the
section back, on its **User story** line, so the link runs both ways.

The tag is mandatory rather than inferred, because a bare heading is ambiguous:
deliberately skipped, or just missed? Requiring a tag forces the decision every
time an operation is touched. **The planner sets these**, and may not leave an
operation section bare.

### Area labels — "show me everything that touched this area"

One label per design area — `design: events`, `design: security` — applied by the
planner to every task it writes. That gives a live query that never goes stale,
because it is GitHub's own index:

```bash
gh issue list --label "design: events" --state all
```

### Sweep mode — carving tasks from the gaps

The planner has a second way in. Instead of "plan this request", point it at the
design documents:

```
Act as the planner in sweep mode. Find operations with no task behind them and
write tasks for them.
```

It runs:

```bash
grep -rnE --include=*.md "^#{2,3} .*\(needs issue\)" Documentation/DesignFeatures
```

and for each hit does exactly what it does for a described request — the size
check, the tier, the task and its sign-off checklist, the `Model - Effort` label,
the area label — then flips the tag from `(needs issue)` to
`(#<new-issue-number>)`. Same skill, different starting point. It skips a match
inside a code fence: the README there carries a fenced example. Flipping a tag
edits the design, so the sweep runs under a design task and its PR carries the
tags.

---

## 7. Approval is a label

There is no PR-gated approval for a task, and no approval file. Approval is a
label on the task, applied by QA:

```
status: needs-scoping → ready for development → status: in-progress → status: in-qa → status: done
```

The planner leaves a task at `status: needs-scoping`, and QA's task review in §8
step 4 happens while the task sits there. On each task it clears, QA applies
`ready for development` and takes `status: needs-scoping` off. That is the same
judgement a PR approval would have expressed, as a label instead of a merge. It
is QA's ruling, so it stays on the task while that ruling stands: the `status:`
labels after it track the work and never replace it, and if a later task review
finds a BLOCKING defect in the task, QA takes the label off and returns it to
`status: needs-scoping`. The planner does the same before it changes an open,
signed-off task or the design it cites, so the change goes back through QA
before the developer acts on it.

The design is approved the way any change is: a design task's documents reach
`main` through its PR, which you merge. The planner carves the feature's tasks
on that same branch, so the PR carries the design and the tasks' tags together
and QA reviews the tasks against it. Merge the design PR once QA has passed it —
it carries `ready for review` — and before the developer starts any of its
tasks: the developer reads the design from `main`, and waits while an open PR
is still changing it. Merge any PR only when its head is the commit QA's latest
round names (`at`): a commit pushed after QA passed it has not been reviewed.

**The developer's hard rule: never start without `ready for development`.** The
`status:` labels after it are yours to move, and QA's verdict on the work says
which comes next — `status: done`, or back to `status: in-progress` on a
BLOCKING finding.

**The honest trade-off:** a label has a thinner audit trail than a PR review. To
see who changed a status and when, read the task's timeline:

```bash
gh api "repos/<owner>/<repo>/issues/512/timeline?per_page=100" \
  --jq '.[] | select(.event=="labeled" or .event=="unlabeled") | "\(.event) \(.label.name) by \(.actor.login) at \(.created_at)"'
```

`gh issue view --json timelineItems` does **not** work — there is no timeline
field on that command. Use the REST endpoint above.

---

## 8. A worked example

"Add a saved-searches panel." UI work, and **there is no design yet** — someone
has a picture and an intention. The design comes first and the tasks fall out of
it.

**1 — Mockup.** Export from Claude Design, save to
`Documentation/Mockups/saved-searches/panel.html` plus `panel.webp`, write the
folder README — what it shows, and that no issue exists yet — and commit:

```
DOCUMENTATION: Add The Saved Searches Panel Mockup
```

**2 — Planner, the design task.** Open a design task for the feature — **#511**,
`DESIGN: Design The Saved Searches Feature` — and point the planner at it and the
mockup (§2 has the long form of this brief):

```
Act as the planner. Issue #511 is the design task for saved searches, with
Claude Design mockups at Documentation/Mockups/saved-searches/. Open the HTML as
well as the images, extract every business rule they show, and write the feature
document and the user story documents that build it.
```

It writes `SavedSearches.md`, the feature: its business rules — rule 3 among them,
*a saved search can be deleted from the panel*, which only the mockup's hover
menu showed — and two user stories. `Backend/SavedSearchService.md` is the
foundation service, and `UI/SavedSearchesPanel.md` the panel component. Each
names `SavedSearches.md` as its parent and gives each operation its own section,
tagged `(needs issue)`: `AddSavedSearchAsync` in the service, the panel in the
component. It lists all three documents in `design.md`'s map and commits with a
`DESIGN:` prefix on the design task's branch. Then go back and add the
**Superseded by** line to the mockup folder's README.

**That tag is what makes the next step possible.** An untagged operation is
invisible to the sweep, and the work is then only in someone's memory.

**3 — Planner, the tasks.** With the documents written, the planner carves the
tasks on the same branch — in the same session, or a fresh one in sweep mode:

```
Act as the planner in sweep mode, on the design task branch for #511. Find
operations tagged (needs issue) and write tasks for them.
```

It greps and finds two operations. `AddSavedSearchAsync` — its logic, validations
and exceptions together — becomes **issue #512**, `FOUNDATIONS: Add A Saved
Search`, tier 1 since it is a new service, naming
`Backend/SavedSearchService.md` §1 as its user story. The panel becomes **#513**,
`COMPONENTS: Add A Saved Searches Panel`, tier 2, naming
`UI/SavedSearchesPanel.md` §1. Each gets its operation line, its sign-off
checklist — logic tests for the happy path and the negative path, validation
tests, exception tests — a `Model - Effort` label, a `design:` area label and
`status: needs-scoping`, and the planner recommends the build order: the
foundation first, since the panel builds on it. It retags both sections with
their tasks, pushes, and opens design PR #519, which closes #511 — the design and
its tags travel together — and ends with the brief for QA.

A real feature carries more user stories than these two — the storage user
story's model, migration and broker methods beneath the service, and a controller
between the service and the panel. The example leaves them out so the flow stays
visible.

**4 — QA, on the design and the tasks.** Before a line of code exists. The unit
of review here is the **feature**, not one task. Run the planner's brief in a
fresh session:

```
Act as QA, reviewing the tasks rather than a change. The saved-searches feature
is designed in design PR #519 — Documentation/DesignFeatures/SavedSearches.md
and its user stories — and the planner has carved tasks #512 and #513 from them.
There is no code yet — do not look for any.
```

That is the whole brief. `.claude/agents/qa.md` defines the mode and carries the
checklist — coverage, completeness across the feature, the parent chain, size,
criteria quality and the `Model - Effort` label — so you name the feature and the
tasks, and say there is no code. Naming the mode matters: the default is
verifying a diff, and it will go looking for one.

**This step is never optional: its label is the approval.** Where a feature has
more than one task it does a second job as well. One operation, one task is a
*mechanism* — it makes each operation traceable. It is not a guarantee that the
operations between them deliver the whole feature. The planner sized each task
in isolation, and nothing else ever asks whether the set is complete.

QA reports two BLOCKING findings:

- `SavedSearches.md` rule 3 — a saved search can be deleted from the panel — is
  covered by no task: the service has no operation to delete one, and the panel
  has no operation for its delete action. It is in the design and in no task.
- Criterion 4 on #513 says the panel "feels responsive", which cannot become a
  test name.

It posts them to the design PR as round 1, at the commit it reviewed, with the
planner as the owner of both. Neither finding is against #512, so QA signs it
off: `ready for development` goes on and `status: needs-scoping` comes off.
#513 keeps `status: needs-scoping`.

Both findings route to the planner (§3), with context: *"Act as the planner.
Address QA's round 1 findings on design PR #519."* On the design branch — the
PR has not merged — it adds §2 `RemoveSavedSearchByIdAsync` to
`Backend/SavedSearchService.md` and §2, the panel's delete action, to
`UI/SavedSearchesPanel.md` as further commits, sweeps to open **#514** and
**#515** for them, and rewrites #513's criterion 4 as something assertable. Then
it hands back with the same brief, naming #514 and #515 as well.

**QA's round 2** is another fresh session, and a smaller review. It reads its
round 1 comment and diffs the design PR from the commit that round recorded.
Both §2 sections are new, and nothing #512 cites changed, so #512 keeps its
sign-off unread. It reviews #513, #514 and #515 in full, since none carries
`ready for development`, checks both round 1 findings against the fixes, and
stops there. All three pass: it signs them off and labels design PR #519
`ready for review`.

**5 — You merge the design PR.** It carries `ready for review`: QA has passed
the design and signed off every task it carries, so the design is the one
decision left to you before any code. Merge it once its head is the commit QA's
round 2 names — the developer reads the design from `main`, so it lands before
any of its tasks starts. Build #512 first: it is
the foundation the panel builds on.

**6 — Developer.** Set the session effort to match the task's `Model - Effort`
label first; nothing does this for you. Fresh session: *"Act as the developer.
Implement issue #512. It carries `ready for development`."* It branches
`users/<your-handle>/foundations-savedsearch-add`, then per criterion commits
`ShouldAddSavedSearchAsync -> FAIL` followed by `ShouldAddSavedSearchAsync -> PASS`,
ticking the criterion's box, and opens a PR titled:

```
FOUNDATIONS: Add A Saved Search
```

with `Closes #512` in the body — opened once every criterion is committed, and
before it hands over, since QA reviews a PR and never a branch. It ends with the
brief for QA. You move the task to `status: in-progress`.

**7 — QA, on the work.** A *different* fresh session from step 4 — carrying the
criteria review's context into the code review is exactly what fresh contexts are
for: *"Act as QA. Verify PR #520 against the acceptance criteria on issue #512."*
Move the task to `status: in-qa`. Round 1 finds one BLOCKING gap — the test for
criterion 3 asserts less than the criterion does — and posts it to PR #520 at the
commit it reviewed, with the developer as its owner. The task goes back to
`status: in-progress`, and the finding goes to the developer with context:
*"Act as the developer. Address the QA findings on PR #520."* It pushes the fix
as a commit on the same PR — never a new one — and hands back: *"Act as QA.
Re-verify PR #520."* Round 2, back at `status: in-qa`, reads round 1's finding
and the one commit since, checks the fix and what it touches, runs the suite,
and stops: nothing else changed, so nothing else is reviewed. It passes, and QA
applies `ready for review` to PR #520 itself — a label on the PR, separate from
the task's `status:` lifecycle.

**8 — Merge** once the PR's head is the commit QA's round 2 names, and set
`status: done`. #513, #514 and #515 were signed off in QA's second round at
step 4, so they are ready to build: #514 next, the service's remove operation,
then #513, the panel, and #515, its delete action.

### The same example when an issue already exists

Someone files issue #510 asking for the panel in prose. The planner reads it,
finds no feature document for saved searches, and pushes back: it proposes
turning #510 into the design task — `DESIGN: Design The Saved Searches Feature` —
rather than carving tasks from a request whose rules nobody has written down.
Once you agree, it retitles #510, step 2 runs with #510 as its design task, and
everything from step 3 on is identical.

---

## 9. Asking for it — copy-paste openers

### Ask the planner

```
Act as the planner. Read issue #512 and plan it.
```

```
Act as the planner. Issue #512 looks too big — check its size and split it into
features or sub-features if it needs splitting, before planning further.
```

```
Act as the planner. Issue #511 is the design task for <feature>. Write the
feature document — mining the mockups at Documentation/Mockups/<slug>/ for every
business rule — and the user story documents that build it.
```

```
Act as the planner in sweep mode. Find operations tagged (needs issue) and write
tasks for them.
```

```
Act as the planner. Address QA's findings on design PR #519.
```

```
Act as the planner. The developer on issue #512 needs a ruling: <the question,
the criterion or section, and what they found>.
```

```
Act as the planner. Port the design at <path> into the design documents in their
conventions — epic-level rules into the global documents, what a feature does
into its feature document, components into user story documents. It is a source
input, not an authority — where it disagrees with what is already there, the
design documents win. List what you dropped and why, and anything you could not
verify against the code.
```

§2 has the long forms of the mockup and port briefs, with the reasoning. A review
of finished work against the design has no opener here: structural findings — a
skipped layer, a decision in a broker — are part of QA's checklist on every PR.

### Ask the developer

**Set the session effort to the task's `Model - Effort` label first.**
Nothing does this for you.

```
Act as the developer. Implement issue #512. It carries `ready for development`.
```

```
Act as the developer. Address the QA findings on PR #520. The criteria are on
issue #512.
```

```
Act as the developer. Address the QA findings on PR #520. The planner's findings
were settled without changing issue #512.
```

### Ask QA

```
Act as QA. Verify PR #520 against the acceptance criteria on issue #512.
```

```
Act as QA. Re-verify PR #520.
```

```
Act as QA, reviewing the tasks rather than a change. The saved-searches feature
is designed in design PR #519, with tasks #512 and #513. No code exists yet.
```

```
Act as QA, reviewing the tasks rather than a change. Task #512 changed under a
ruling. Code for it exists; do not review it.
```

The last two are QA's second mode. Name it explicitly — verifying a diff is the
default, and it will go looking for one. §8 step 4 has the reasoning; the
checklist is in `.claude/agents/qa.md`. A re-review uses the same brief as the
first round, naming any task added since. None of these briefs says what
changed or what to look at: QA works that out from its last round.

---

## 10. Conventions that bite

**Branches.** `users/<your-github-handle>/<category>-<entity>-<action>`, lowercase
after the handle. Claude Code worktree sessions generate `claude/<slug>-<hex>`
branches of their own; those are accepted in practice and several merged PRs came
from them. Never commit to `main`. Contributors do **not** fork — everything is
an in-repo branch, whatever the vendored branching skill says.

**Commits.** TDD work commits as `{TestName} -> FAIL` then `{TestName} -> PASS`.
Everything else is `CATEGORY: Pascal Case Description`.

**PR titles.** Same `CATEGORY:` prefix. The authoritative list of prefixes is in
`.github/workflows/prLinter.yml` — not in any skill, which carries a shorter and
differently-spelled list. Common ones: `FOUNDATIONS:`, `PROCESSINGS:`,
`ORCHESTRATIONS:`, `COMPONENTS:`, `CONTROLLERS:`, `DOCUMENTATION:`, `DESIGN:`,
`CONFIG:`, `CODE RUB:`, `MINOR FIX:`, `MEDIUM FIX:`, `MAJOR FIX:`. There is no
bare `FIX:`.

Be aware of what is actually enforced: **an unrecognised prefix does not fail the
build, it is silently left unlabelled.** The job matches the first prefix it
recognises and stops; a title matching none simply gets no label and no
complaint. (The job can still fail for its own reasons — the `addLabels` call is
not wrapped in a try/catch, so an API or permissions error would red it — but
never because of your title.) The convention is real; tooling will not catch you
breaking it.

**PR body.** Must link an issue or the PR linter fails — on any PR you open. The
check is skipped only for `dependabot[bot]`, so a dependency PR with no issue
link is exempt by design rather than broken:

```markdown
Closes #512
```

`fixes` and `resolves` (and their past-tense forms) and `AB#<n>` also match. This
is the only PR-linter job that fails on the pull request's own content. The
labelling job can still red for its own reasons, as above. `Build` is the only
status check the branch ruleset requires green.

**Issue labels.** Every issue carries a `Model - Effort` label. The body does not
repeat it — the label is the decision, and the body is where what actually ran is
recorded afterwards. **The model is always Opus 5.5; the effort is the only
choice.** The effort ladder is **Low / Medium / High / Extra / Max**:

| Model | Efforts available |
| --- | --- |
| Opus 5.5 | Low, Medium, High, Extra, Max |

Labels for other models exist on the same ladder and are deliberately absent from
the table: `Opus 5 - *`, `Sonnet 5 - *`, `Fable 5 - *` and `Haiku 4.5 - *` carry
closed issues genuinely built under those models, so they are history rather than a
choice. Only the Opus families carry the top two rungs — there is no
`Sonnet 5 - Max` and no `Fable 5 - Extra`.

The bottom rung was once called `Small` on the five-rung models and `Low` on the rest,
which meant the cheapest Opus rung was the one name that did not work. `Small` is
retired and the labels carrying it were renamed, so no issue changed rung.

**The label set is generated, not hand-maintained.** `.github/generate-labels.py` reads
the authoritative prefix list out of `.github/workflows/prLinter.yml` and writes
`.github/labels.json`; `.github/workflows/labels.yml` syncs that manifest to the
repository on every push to `main` that touches it. Two properties worth knowing before
you trust it: it **never deletes a label**, so anything added by hand survives, and it
**re-creates any manifest entry that is missing** — so renaming a live label without
changing the manifest brings the old name straight back. The generator also fails rather
than guessing when a prefix has no colour, which is what stops a new prefix becoming a
silent grey label the first time someone uses it in a PR title.

**Choosing the label.** The default for developer work is `Opus 5.5 - High`.
Opus 5.5 is fast and capable enough that the model no longer needs rationing, so
every label names it and what you choose is the effort, up or down from `High`.
`High` is enough for most implementation: it follows a pattern that already exists
in the solution, and the phase is fenced on both sides — the criteria are approved
before it starts and QA verifies adversarially after it finishes. The top rungs
belong where there is no oracle and a wrong call produces no failing test, which
is why QA is pinned at `Max`. Raise the developer's effort to `Extra` or `Max`
only on one of these:

1. **First of its kind** — a service, layer or component with no sibling in the
   solution to pattern-match against.
2. **Breadth sweeps** — where the risk is whether every site was found, not
   whether any one of them was changed correctly.
3. **The security boundary** — identity and access decisions. A miss here
   usually has no failing test to catch it.
4. **Migrations and SQL** — deploy-path, hard to reverse, and the traps survive a
   passing test.
5. **Thin criteria** — if the planner left open questions, the developer is doing
   planner work and needs more than the default.

Lower it for trivial work — a rename, a config change or a doc relocation is
`Opus 5.5 - Low` or `Opus 5.5 - Medium`. Between two efforts, take the lower one
and let the escalate-on-scope-discovered rule correct it. Over-spending is
invisible and nobody else is watching for it.

**The label is the decision, and it is the only place the decision lives.** It
shows in the issue list, so whoever is about to pick the issue up can see what to
set their session to without opening anything — which is the whole reason it is a
label rather than a line of prose. The body does not carry a copy: a second
statement of the same thing only creates the question of which one is right. It
is chosen when the issue is written and it does not change afterwards, so the
agent writing the issue must get it right; a wrong label sends the next person to
the wrong model before anyone has read a line of code.

**The body records what it actually cost.** When the pull request opens, the
developer appends a line under a `## Model usage` heading at the end of the
issue body:

```markdown
## Model usage

- PR #<n> — Opus 5.5 - High
```

One line per pull request, appended and never rewritten, so an issue that took
two attempts shows both. This is where the record goes precisely because the
label cannot hold it — the label has a job already, and overloading it would
leave a reader unable to tell a plan from an outcome.

Nothing in this repository reads either automatically, so that section and QA's
verdict comment on the pull request are the only durable record of what a piece
of work cost and what the cost bought. Keep both honest or the next review of
this policy has nothing to measure. `Documentation/ModelBudget/tally.py` is that
review.

**Never add AI or assistant attribution** to a commit message or PR description.
It trips the unattributed-changes rule and blocks the merge.

**Every commit carries the identity of the person responsible for it**, never an
AI tool's — whoever opens the PR owns the code, and the history says so. A *tool
identity* is one whose name, trimmed and compared case-insensitively, is exactly
`Claude`, `Claude Code` or `claude[bot]`, or whose address is at `anthropic.com`;
`Jean Claude` is a person. *Attribution* is whatever the attribution pattern in
`.githooks/identity-guard.sh` matches: a co-author trailer naming the tool, a
session link, a "Generated with" footer. Three layers check this.
`Documentation/Design/Architecture.md` §ARC12.11 rules how far each can be relied
on, and only the first is relied on:

- **CI is the enforcement of record.** `rejectAiAttribution` ("Reject AI Identity
  And Attribution") in `.github/workflows/prLinter.yml` runs the guard's own test,
  then fails any PR that adds a commit (`--not origin/<base>`) with a tool author
  or committer or with attribution in its message, and any PR whose title or
  description carries attribution. It is emitted by `GeneratePrLintScript` in
  `Glory2Him.Core.Infrastructure`, so change the generator and regenerate, never
  the YAML. It runs on every pull request, but only `Build` is a required status
  check on `main` today; making this one required is the owner's setting.
- **The git hooks are the local layer.** `.githooks/` refuses the commit and the
  push. `pre-commit` and `pre-merge-commit` judge the identity git resolves,
  however it was set; `commit-msg` judges the message; `pre-push` judges every
  commit the push publishes, which also catches rewritten history and anything
  committed through a path that runs no hook. They stop a tool commit before it
  leaves the machine, but `--no-verify` skips them by design, which is why they
  are not the record. A clone outside Claude Code turns them on once with
  `git config core.hooksPath .githooks`.
- **The session hooks are best effort.** `.claude/settings.json` turns Claude
  Code's own attribution off and wires hooks that each act on a closed list of
  forms. A form outside a list gets past them, and a hook that errors or times
  out lets the call through; CI catches the result, and the hooks guarantee
  nothing.
  - At session start, git is pointed at `.githooks/`, and a tool identity is
    switched to the signed-in person (`CLAUDE_CODE_USER_EMAIL`, or
    `G2H_GIT_USER_NAME` / `G2H_GIT_USER_EMAIL` set in a cloud environment's
    settings). When no person can be found it warns and changes nothing, so every
    commit is refused.
  - Before each shell command, `pre-bash` restores `core.hooksPath` and refuses a
    command whose text holds a hook-skipping token (`--no-veri`, `hookspath`,
    `git_config`, `alias.`, `include.`, `includeIf.`, or a `-n` cluster after
    `commit`), even one that only mentions it. It reads no shell grammar and
    judges neither identity nor messages; the git hooks judge both.
  - For a `gh` command that writes GitHub text, `pre-bash` checks the command's
    text and every file it names as a text source, and refuses a source it cannot
    read (stdin, a substitution, a variable, `~`, a missing file). Write the text
    to a file and pass it with `--body-file <path>`.
  - `pre-github` checks the title, body and commit-message fields of a GitHub
    connector write, never a file's contents. The connector appends its own
    "Generated by" footer server-side after that check, so `post-github` tells
    the session to edit it away at once.

  Comments, reviews and issues never pass through CI, so for them the session
  hooks are the only check, and a best-effort one.

All three share one rule set, `.githooks/identity-guard.sh`; no other hook
carries its own copy. `.githooks/tests/identity-guard.test.sh` drives every layer
end to end in scratch repositories, and `rejectAiAttribution` runs it first.

**Agents and skills** live in `.claude/`. They are maintained in
Glory2Him.Template and refreshed into this repository byte for byte — The
Standard's skills reach the Template from upstream through `skills-lock.json`.
Treat them as read-only here and reference skills by name. A change to one is made
in the Template and arrives with the next refresh; a copy edited here is drift
that the next refresh reverts. The exception is `update-dependency-graph`, this
repository's own skill, which is maintained here.

---

## 11. Commands

```powershell
dotnet build

# one suite
dotnet test Glory2Him.Core.Tests.Unit

# every suite CI runs, guarded so an earlier failure is not masked by a later pass
foreach ($kind in "*Tests.Unit*.csproj", "*Tests.Acceptance*.csproj", "*Tests.Integration*.csproj") {
  foreach ($project in Get-ChildItem -Filter $kind -Recurse) {
    dotnet test $project.FullName
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
  }
}

# React, from the app's own directory — `;` sequences, it does not stop on failure
npm run lint
if ($LASTEXITCODE -ne 0) { throw "lint failed" }
npm run test
if ($LASTEXITCODE -ne 0) { throw "tests failed" }
npm run build

# republish the branch to local IIS, from the repository root — -Project defaults
# to the main checkout, so pass this checkout's project or you publish whatever
# the main checkout has checked out
D:\Sites\Deploy-Glory2HimWebApp.ps1 -Project .\Websites\Glory2Him.WebApp\Glory2Him.WebApp.csproj
```

These are PowerShell — `Get-ChildItem` and the `.ps1` path assume it. `&&` is not
a valid statement separator in Windows PowerShell 5.1, and `;` sequences without
stopping on a non-zero native exit code, which is why the guards are there rather
than a bare `;` chain: without them a failed lint followed by a passing build
reports success.

`.github/workflows/build.yml` is authoritative for what CI runs. It discovers
every `*Tests.Unit*`, `*Tests.Acceptance*` and `*Tests.Integration*` project
recursively, so a test project outside `Glory2Him.Core.Tests.*` still runs.

---

## 12. What does not exist yet

Stated plainly so nobody goes looking:

- **No feature has a document yet.** `Documentation/DesignFeatures/` holds only
  its README; the design that predates it lives in the global and area documents
  `design.md` maps.
- **Nothing validates design citations automatically.** `Tools/design-split-audit.sh`
  exists and is run by hand; no CI step runs it. Nothing reads the
  `Model - Effort` label to configure a session either.
- **`Documentation/Prompt-CreateFoundationService.md`** predates the agents. It is
  listed in `Glory2Him.Core.slnx`, but no agent reads it. Treat the three-agent
  workflow as current.

Note one trap while you are here: an all-caps `DESIGN` label exists, auto-created
by the PR linter from a `DESIGN:` title prefix. It is a category label on PRs, not
one of the `design: <area>` labels in §6.
