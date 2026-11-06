# Developing in this repository

How work moves from a feature request to merged code here, and how to drive the
Claude Code skills and agents that do most of it.

`Documentation/Procedures/workflow.md` is the one-page procedure — who does what,
which artefacts, which commands — and
`Documentation/Decisions/0001-issue-based-workflow.md` records why the workflow is
shaped this way. Three step-by-step walkthroughs sit beside the procedure:
`Documentation/Procedures/new-feature.md`, for taking a feature request through Claude
Code to `main`; `Documentation/Procedures/porting-planning-documents.md`, for turning
an existing document into feature requests and global rules; and
`Documentation/Procedures/migrating-an-existing-repository.md`, for moving a repository
made from the older template onto this workflow. `CLAUDE.md` is the short version an
agent loads automatically. This is the long version for a person: it explains the same
workflow, plus the parts an agent never sees — where a mockup goes, who approves what,
what to do when a build halts, and which conventions are enforced by tooling rather
than by good intentions.

Read `INTENT.md` for what the system is for, and the **Global Rules** issue — the one
open issue labelled `global rules` — for the rules every feature inherits and the
terms they use. This repository was made from the older template, with its design
kept as documents, and is moving onto this workflow: Glory2Him/Glory2Him.Core#1381
tracks the move. Every open issue from the old process is closed as superseded and
its work re-planned the new way, one feature at a time, from the documents it was
planned against; this repository has no `legacy` features, though the shared agents
and skills, and this guide, describe them for other repositories. Until the Global
Rules issue carries `approved` there is no new design; `CLAUDE.md` says where the old
documents are meanwhile.

Glory2Him has one maintainer. Where this guide says "the maintainer", that is the
person who approves plans and merges to `main`; it may well be you.

---

## 1. How work breaks down

Three levels of work are GitHub issues, linked as sub-issues. The rules shared by
every feature are one more issue, the **Global Rules** issue.

| Level | What it is | Example | Where it lives |
| --- | --- | --- | --- |
| **Epic** | the whole product | Student Portal | `INTENT.md` |
| **Global rules** | rules more than one feature cites, and the terms they use | identity travels on the envelope | the one open issue labelled `global rules`, changed through QA and the maintainer's `approved` |
| **Feature** | something that ships and works on its own | Saved searches | an issue of type **Feature** |
| **User story** | one component at one level — it can do something, but need not work on its own | the SavedSearch foundation service; the panel | an issue of type **User Story**, a sub-issue of its feature |
| **Task** | one operation of a user story | `ISavedSearchService.AddSavedSearchAsync` | an issue of type **Task**, a sub-issue of its user story |

The rule of thumb for where a decision lives: **if more than one feature cites it,
it is a global rule.** Everything else about a feature — its business rules, its
user stories, its tasks — is in its issues. Every feature adheres to the global
rules; one that must depart from a rule records a documented reason in its user
story's **Deviations**.

A feature is built by several user stories, usually at several levels — a storage
broker, a foundation service, a controller, a page. A user story never spans
levels. Its operations are its tasks: **one task per operation**, with that
operation's logic, validations and exception handling together, never split
apart. The direct path and its event path are two methods, so two tasks of the
same user story.

A feature can span several screens, and each screen is its own user story. A user
portal is one feature, not two: its master page and its detail page are two UI
user stories, and the operations they offer between them are their tasks.

Issue types are an organisation feature. The organisation has Feature, Task and Bug;
an organisation owner adds **User Story**. Where types are not enabled, the labels
`type: feature`, `type: user-story` and `type: task` stand in. Sub-issues are
required: the build driver finds a feature's stories and tasks only through them.

### The pipeline

```
YOU (anyone)      a feature request, through the issue form — behaviour, rules, mockups
   ↓
/grill-with-docs  optional: you and the requester are grilled on it, round by round;
                  new terms or rules go to the Global Rules issue, the rest to a
                  `Settled before planning` comment on the feature
   ↓
/plan-feature     planner: the feature's design sections, user stories and tasks
   ↕              open questions in grilling rounds — numbered, each with a
                  recommended answer — relayed to whoever wrote the request
   ↕              qa (plan review, fresh): findings back to the planner until clean
   ↓              `ready for development` on every task, `status: awaiting-approval`
THE MAINTAINER    reads the feature and adds `approved` — a person, never an agent
   ↓
/build-feature    the driver, on your machine, task by task in build order:
                    developer → PR into the feature branch → CI → qa (fresh) → fix rounds
                    → merge into the feature branch → CI on the feature PR
                  then qa (feature verification, fresh, max effort) → handover
   ↓
THE MAINTAINER    reviews the feature PR and merges it into main with a merge commit
   ↓
main → DEV        deployed automatically
   ↓
/release          releaser: version, the documentation in Documentation/Design/, the
                  dependency graph, notes → RELEASES: PR → qa, documentation included
   ↓
THE MAINTAINER    merges it; the release workflow tags it and publishes the GitHub Release
```

People do four things: write the request, answer the planner's questions, approve
the plan, and merge the finished feature. Agents do everything between, and the
skills move work from one agent to the next — you never copy briefs between
sessions. `main` only ever receives whole, verified features.

**The planner pushes back when the request is too thin.** It will not invent the
rules a task encodes. A request with no behaviour scenarios, a mockup whose rules
nobody has written down, or a feature that needs a new global rule stops at
`/plan-feature`, with the questions posted on the feature.

### Tiers

The planner sizes the process to the risk of the change, and states the tier on
each task's first line:

| Tier | Applies to | Plan | Approval | Build |
| --- | --- | --- | --- | --- |
| **1: design** | A new entity, a schema change or migration, a new event, a change to the security boundary, or a new service, layer or dependency | The feature's design sections, a change to the Global Rules issue first if a global rule changes, then the tasks | QA plan review, then the maintainer's `approved` | `/build-feature` |
| **2: behaviour** | New behaviour inside the existing design — no schema, no event, no boundary crossed | Tasks under an existing or new user story | As tier 1 | `/build-feature` |
| **3: fix or tweak** | A bug fix, a copy or styling change — one file, no schema, no event, no boundary | A cut-down task opened by `/tweak` | The `/tweak` request itself (see below) | `/tweak`: one developer, QA on a PR straight to `main` |

No tier skips the task or QA on the work: the PR linter fails a PR that closes no
issue, and the developer builds nothing that is not in a criterion. What a lower
tier skips is the design.

**Tier 3 is a deliberate exception to the approval chain.** For tier 1 and 2 work,
QA's `ready for development` on each task and the maintainer's `approved` on the
feature are both needed before code. For tier 3, the person who runs `/tweak` is
the approval — recorded on the task as `Requested by @<handle> via /tweak` with a
`tier: 3` label — and QA's review of the PR is the only independent check. That is
why `/tweak` refuses anything that fails the tier 3 test, and why the PR still goes
through QA before the maintainer merges it.

---

## 2. Every agent runs in a fresh session

**Each agent is a separate session with an empty context.** The developer cannot
see the planner's reasoning, only what the planner wrote into the issues. QA is
deliberately fresh so it argues with the code rather than with the developer's
summary of it.

That has one consequence worth internalising: **if it is not in the issue, it does
not exist.** A decision made in conversation and not written into the feature, the
user story or the task is lost the moment that session ends. The task body is
self-contained: every fact a criterion needs is copied into it, so the developer
reads three bodies — the task, its user story, its feature — plus the sections of the
Global Rules issue the task cites, and nothing else. A `legacy` task (§4) also reads
the documents it cites, at their Planning path.

### The skills brief the agents

You never write briefs between agents. Each skill launches its agents with a
one-line brief that points at issue numbers, carries every handover between them,
and relays each agent's report to you in a few lines. An agent never waits on you
mid-handover, and every handover to QA is a fresh session briefed with pointers
only — never another agent's account of its work.

| Skill | Runs | Ends with |
| --- | --- | --- |
| `/grill-with-docs [n \| area]` | an interview with you, in rounds; QA's global rules review if a rule or term changed | terms and rules in the Global Rules issue, decision records, `INTENT.md`, or a `Settled before planning` comment |
| `/grill-me` | the same interview, nothing written down | a shared understanding |
| `/plan-feature <n>` | planner, its questions in grilling rounds, then QA plan review, looping until clean | `status: awaiting-approval` |
| `/build-feature <n>` | the driver in the background; you follow its milestones | the feature PR ready for the maintainer, or a halt |
| `/build-task <n>` | started by the driver, one `claude -p` per task | `QA PASS`, or `QUESTION:` |
| `/verify-feature <n>` | started by the driver once every task is in | `FEATURE PASS`, and the handover |
| `/tweak <request>` | one developer, QA on the PR | a tier 3 PR to `main` |
| `/release` | the releaser, then QA's release check, documentation included | a `RELEASES:` PR |
| `/standard-compliance [area]` | the layer check, a sub-agent per layer, an HTML report, then an interview on the finding you pick | a feature request or a `/tweak` to run; never code |

The coordinating sessions are kept small on purpose: they read the issue and the
agents' reports, never the code or the diffs, so a session that lives for a whole
feature does not pay for everything it saw on every later call.

**Every finding goes to its owner.** QA names an owner for each finding, BLOCKING
and ADVISORY alike: the developer for implementation, the planner for missing or
contradictory criteria or a design that got a boundary or a layer wrong. A failure
QA reports as looking unrelated or pre-existing is a finding with an owner like any
other — never silently fixed, never ignored.

**Questions halt, they are not guessed.** A session the driver started cannot ask
you anything. When it reaches something only a person can decide, it writes a
`QUESTION:` line and stops, and the driver halts the feature with the question
posted on the feature issue and `status: blocked`. Answer it there and re-run
`/build-feature <n>`. The things that stop for a person form an open list, not a
closed one: wherever an agent file hands something to you, the work stops there.

You can still run an agent by hand — `Act as QA, reviewing the plan for feature
#500.` — and the agent files remain the authority on each role. Do that for a
one-off, not as the workflow; a legacy feature is taken in through `/plan-feature` (§4).

---

## 3. The agents

Defined in `.claude/agents/`. Each file is the authority on its own role; this
section says what each is for.

### planner — design and tasks, not code

**Owns** the breakdown — feature, user story, task — the size check and the risk
tier, and everything the design decides: the business rules, layer placement,
entity count, event contracts, the security boundary, storage and migration
shape. Then the acceptance criteria, precise enough that a developer can write a
failing test from them without asking a question.

**Writes into the feature issue**, below the request exactly as the form wrote it:
the problem; the business rules `BR-1` … `BR-n`, including every rule the
scenarios and mockups show; the user stories and their levels; the entity count
(one entity means foundation or processing, two or three means orchestration,
more than three is a violation); event contracts in `<Subject>-<Verb>` form;
storage and migration shape including the seed consequence; security, citing
`§SEC`; risks, out of scope, deviations and open questions.

**Opens a user story sub-issue per component**, naming its parent, its level and
interface, the rules it cites, and an **Operations** table — `OP-n`, method, path,
task. **Then a task sub-issue per operation**, with the tier, the feature, the user
story and operation, the branch, and what it depends on in its header, and the
criteria as a sign-off checklist:

```markdown
- [ ] Logic tests
  - [ ] Happy path — succeeds under a security context allowed to do this
  - [ ] Negative path — refused under a security context that may not do this
- [ ] Validation tests
- [ ] Exception tests
```

Each criterion is written Given/When/Then and must be expressible as a single test
name. Aim for five to eight logic criteria; validations and exceptions never count
toward an operation's size. Past about eight, the operation does too much, and the
planner raises it as a design question rather than splitting its paths across
tasks. The task's title is the one its PR will carry, `CATEGORY: Description In
Pascal Case`, the category naming the layer.

Every task gets a `Model - Effort` label (§10) and a `design: <area>` label for
each global rule area it touches. The planner never applies `ready for
development`, `approved` or `legacy`, and never edits a body carrying either
approval without first taking it off (§7).

**Asks in grilling rounds.** When the request leaves a decision open, the planner
posts one `Questions round <r>` comment: only the questions it can ask now, each
numbered, with its recommended answer worded so that "yes" accepts it. Anything the
code or the issues can answer it looks up instead of asking. `/plan-feature` relays
the round whole and posts your answers as `Answers round <r>`; the next planner asks
the next round if the answers opened new questions.

It never writes production code or tests, and edits no file in the repository: its
writes are issue bodies and comments, and changes to the Global Rules issue through
the process in §4.

### developer — test first, one criterion at a time

**Owns** implementation. Starts only on a task carrying `ready for development`
whose feature carries `approved` — or on a `tier: 3` task opened by `/tweak`.
Reads the task, its user story, its feature and the sections of the Global Rules
issue the task cites; nothing else from GitHub. For a `legacy` task it also reads the
documents the task cites, at their Planning path.

The loop per criterion: write one test, run it and confirm it fails **for the right
reason**, commit it as `{TestName} -> FAIL`, implement the smallest change that
passes, commit as `{TestName} -> PASS`, and tick the criterion's box. No production
code exists without a failing test that demanded it. A test that passes on its first
run is pinned, not forced to fail: the developer breaks the production code it covers
on purpose, watches it fail for the expected reason, reverts, commits the test alone
as `{TestName} -> PASS`, and names it in the PR with the change that made it fail.

It runs the test projects it touched while working and the full checks once before
opening the PR, into the feature branch, with `Closes #<task>` and a **Decisions not
in the task** section. Its report ends `MERGE READY: YES` or `MERGE READY: NO` —
whether its work is done, never whether a person has approved it.

### qa — adversarial, never fixes

**Owns** finding the reasons a change should not ship. Produces findings marked
BLOCKING or ADVISORY, each naming its owner — the developer for implementation,
the planner for missing or contradictory criteria or a design that got a boundary
or layer wrong. It never fixes anything: the person who broke it should fix it, and
a reviewer who patches defects stops looking for more. Each label it owns means one
thing — it checked, and no finding is open — so it withholds `ready for development`
and `ready for review` while any finding is open. It has six modes:

| Mode | When | Signs off with |
| --- | --- | --- |
| **Global rules review** | after a `Change <n>:` to the Global Rules issue, from `/grill-with-docs` or the planner | a `Rules QA round` comment with the `Signed:` hash, and `status: awaiting-approval` on the issue |
| **Plan review** | `/plan-feature`, before any code | `ready for development` on each task it clears, a `Plan QA round` comment with the `Signed:` hashes, and `status: awaiting-approval` when all clear |
| **Legacy intake** | a `legacy` feature, in place of a plan review: `Act as QA, taking in legacy feature #<n>.` | the `Signed:` hashes and `status: awaiting-approval`; the tasks keep the `ready for development` they already carry |
| **Change verification** | `/build-task` and `/tweak`, on each PR | `ready for review` on the PR, `MERGE READY: YES` |
| **Feature verification** | `/verify-feature`, once every task is in | `FEATURE PASS` on the feature PR |
| **Release check** | `/release`, on the `RELEASES:` PR — the generated documentation held to its sources | `ready for review` on the release PR |

Feature verification is the gate before `main`. It runs fresh, at max effort, on
the feature branch as a whole: every business rule has an acceptance test through
the exposer that would fail without it; every role the Security section allows or
refuses has a test through the API; the full suite passes; each migration applies
as a single batch to an empty container database and stays inside what Azure SQL
Database supports (`§ARC1`); and the handover lists migrations, configuration,
secrets and deploy order correctly.

After round 1, each round reviews only what changed since the last one and what
that touches.

### releaser — one release, never the version alone

**Owns** preparing a release from everything merged to `main` since the last tag:
proposing the SemVer bump with its evidence, cutting `release/vX.Y.Z`, setting the
version in `Directory.Build.props` and each app's `package.json` — every tracked
`package-lock.json`'s folder whose `package.json` is not marked `linguist-vendored` in
`.gitattributes` — updating the README, OpenAPI document and `CHANGELOG.md`, generating the documentation in
`Documentation/Design/` and the dependency graph in `Documentation/DependencyGraph/architecture.md`
from the issues (§4), the package report, and the release notes in
`Documentation/Releases/vX.Y.Z.md`. It opens a `RELEASES:` PR. It never edits
production code or tests, never chooses the version alone, and never merges, tags
or deploys. §11 has the rest.

---

## 4. The Documentation folder

```
INTENT.md                  the epic — what the product is for
CHANGELOG.md               one entry per release, newest first
Documentation/
  Design/                  the releaser's: generated at each release, never edited by hand
    design.md              epic level — the intent, the global rules, every released feature
    Features/<FeatureName>/<FeatureName>.md    one released feature, from its issues
    Features/<FeatureName>/assets/             the images and diagrams attached to its issue
  DependencyGraph/         the releaser's architecture.md: the layer graph at each release
  Diagrams/                yours: a scratchpad for draft diagrams; no agent opens it
  Planning/                yours: existing documents from before this workflow; they
                           stay once ported into issues, so pinned links resolve
  Mockups/                 yours: UI wireframes and Claude Design exports
  Decisions/               decision records, added by DESIGN: PR
  Procedures/              how the workflow runs, step by step
  Releases/                release notes, one file per version, written by the releaser
  ModelBudget/             tally.py — what each effort cost, and what it bought
```

**Who owns what is the point of this layout.** `Design/` and
`DependencyGraph/architecture.md` are outputs: the releaser builds them only from `INTENT.md`, the Global Rules issue, the
feature issues and their attachments, and the code — and no other agent reads them as
a source. `Diagrams/` is a scratchpad: everything there is a draft, no agent opens it
(`.claude/settings.json` denies Claude's file tools access), and the releaser never
documents from it. When a diagram is done, upload it to the feature issue that uses
it; that copy is the one the documentation is built from.

**Existing documentation goes in `Planning/`, and counts once it is ported.** A
requirements document, a specification or an older design from before this workflow
is kept there, but nothing is planned, built or documented from it. Port it through
the issue forms, so every feature and rule is logged the same way whatever it started
as: each feature it describes becomes a **Feature request**, with the document in the
form's **Ported from** field, and each rule or term goes into the Global Rules issue.
`/grill-with-docs Documentation/Planning/<document>` does it with you — it reads the
document, grills you on what it leaves open or what the code contradicts, drafts each
feature request field by field for you to submit through the form, and puts the rules
and terms through the Global Rules change. When a document is fully ported, its row in
`Documentation/Planning/README.md` names the issues it became.

**The one exception: a `legacy` task** — in other repositories; this one closed its
old tasks instead and re-plans their work (Glory2Him/Glory2Him.Core#1381). A repository made from the older template kept
its design as files and may hold tasks signed `ready for development` against them.
Moving it onto this workflow puts those files under `Planning/` in one rename-only
commit, keeping their sub-paths — `Documentation/Design/X` becomes
`Documentation/Planning/Design/X`, `Documentation/DesignFeatures/X` becomes
`Documentation/Planning/DesignFeatures/X`. Each task signed before the switch is
labelled `legacy` and hung under a Feature and a User Story issue created for it, also
`legacy`. That task, and only that task, is built and verified against the documents
it cites, read at their new path; its body is never rewritten. The planner never adds
`legacy`, no new task is written against `Planning/`, and a change to a legacy task's
design is new work. Rules too large for the Global Rules issue are indexed in its
**Legacy rules** section instead: an unstruck line binds legacy features only, and a
new feature that needs one waits until `/grill-with-docs` settles it — ported under its
own number, retired, or left to the one legacy feature it serves.
`Documentation/Procedures/migrating-an-existing-repository.md` has
the steps, and the **Legacy** section of `Documentation/Planning/README.md` the
mapping.

### Global rules, features and user stories

Each level holds its own kind of decision, and the planner decides which level a
decision belongs to before writing it:

- **The Global Rules issue** holds the rules every feature must follow — how a
  service implements eventing, how a user authenticates, how layers depend on each
  other — and the **Terms**, the system's own words. It is filed once from its form
  and changed like an approved plan: take `approved` off, edit, post a `Change <n>:`
  comment, run QA's global rules review, and the maintainer adds `approved` again.
  `/grill-with-docs` does all of that with you. A feature that needs a rule changed
  stops at `/plan-feature` until the issue is approved again.
- **A feature issue** holds what one feature does: its business rules — including
  every rule its mockups show — and the user stories that build it.
- **A user story issue** holds what one component does, as its operations table,
  and names its feature.

**Nothing restates what is above it.** A feature cites the global rules — `per
§EVN2` — and a user story cites its feature's business rules, so there is only ever
one copy to keep true. A feature or story that must depart from a global rule
records it in its own **Deviations** section: the rule, the reason, and the
alternatives rejected, as one line naming the class in bold so
`scripts/list-deviations.sh` can find it. The planner proposes a deviation; QA's plan
review and the maintainer's `approved` grant it. A service designed before this
workflow, with no user story issue to carry its deviation, has a decision record in
`Documentation/Decisions/` instead, carrying `**Deviation:** <ClassName> — <the rule>`.

### Numbering and citation

The Global Rules issue numbers its sections **flat and prefixed** — `§EVN1`, `§EVN2` —
so a bare citation stays unambiguous. A moved repository keeps its own scheme, nested
numbers included, so its old citations (`§ARC12.3.1`) still resolve. Terms are cited by the term itself. A relocated
section keeps a `(formerly §X)` annotation naming its old position, so code comments
citing the old number still resolve by grep. Nothing validates citations; the
annotation convention is the whole guarantee.

Features and user stories are cited by issue and number: a business rule as
`#500 BR-3`, a user story's operation as `#502 OP-1`. GitHub turns each into a
backlink, so a feature shows every story, task and PR that cites it. Numbers are
never reused within an issue.

Code comments cite the same way:

```csharp
// design §EVN2 rule 4: the subject is the service, not the entity
// design #500 BR-3: a member never sees another member's saved searches
// design #502 OP-1
```

---

## 5. From a feature request to a plan

### 5.1 Raise the feature

Anyone raises a feature with the **Feature request** form (New issue → Feature
request). Blank issues are off. The form asks for a summary, who uses it, the
behaviour as Given/When/Then scenarios, what must not happen, mockups, data, how you
will know it is done, what is out of scope, and any deadline. It sets the issue type
**Feature** and the labels `type: feature` and `status: needs-design`.

Write it in the language of the service, not of the code. A short, precise answer
beats a long, vague one: the planner reads it word for word and comes back with
questions where it is thin.

`Documentation/Procedures/new-feature.md` walks the developer through every step from
here — what to type in Claude Code, how to answer the question rounds, what the issue
shows at each stage. A feature that starts life in an existing document is ported into
this form first: `Documentation/Procedures/porting-planning-documents.md`.

### 5.2 Mockups

UI work usually starts as a picture. The job is to get every rule the picture shows
into words, because a picture is not a test name and a mockup left as a second
source of truth will eventually contradict the plan.

- **Images** — drag them into the feature request's Mockups field. Name each
  screen.
- **HTML exports** from Claude Design, when the interaction matters — hover states,
  spacing, the real DOM — go in `Documentation/Mockups/<feature-slug>/<screen>.html`,
  committed on their own with a `DOCUMENTATION:` prefix, and linked from the feature
  issue. Reference them by a URL pinned to the full 40-character commit SHA, never
  a branch, so the picture in the issue cannot change under it:

  ```markdown
  [Panel](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Mockups/saved-searches/panel.html)
  ```

  Each mockup folder gets a `README.md`: what it shows and the feature issue it
  feeds. Bundled exports run to several megabytes; do not commit five versions of a
  screen because a design iterated.

The planner opens the HTML as well as the images, and writes every rule they show as
a numbered business rule. From that moment the business rules are authoritative and
the mockup is history. "Matches the mockup" is never a rule or a criterion.

### 5.3 Plan it

On your machine, in the repository:

```
/plan-feature 500
```

The skill checks the feature is ready to plan — and that no more than two other
features sit planned but unbuilt, so planning stays just in time — then launches
the planner. If the planner has open questions it posts them as one `Questions round
<r>` comment on the feature and stops; the skill relays the round to you whole. Each
question is numbered and carries the planner's recommended answer, so "yes" accepts
it and you only write out the ones you disagree with. Answer, or ask whoever wrote the
request to, and the skill posts the answers as `Answers round <r>` and relaunches the
planner, which asks another round only if your answers opened new questions.

**Want fewer rounds?** Run `/grill-with-docs 500` with the requester first. It
interviews the two of you about the request, writes any new term or global rule to
the Global Rules issue, and posts the rest as a `Settled before planning` comment the
planner reads as answers.

When the plan is complete, a fresh QA reviews it as a set: every business rule
covered by a criterion, every operation row backed by a task, every level naming its
parent, size, paths, tier and labels. Findings go back to the planner and round
again, until QA signs every task off and sets `status: awaiting-approval`. The
skill records the planning session's cost as a `Spec cost:` comment.

---

## 6. Linking the tree

Two mechanisms, answering two different questions.

### Sub-issues and operation rows — "is every operation planned?"

Each user story's **Operations** table has one row per operation, and each row names
the task that delivers it, or says `(needs task)`:

```markdown
| OP | Method | Path | Task |
| --- | --- | --- | --- |
| OP-1 | AddSavedSearchAsync | direct | #520 |
| OP-2 | RemoveSavedSearchByIdAsync | direct | (needs task) |
```

The task names the row back on its **User story** line — `#502 OP-1` — so the link
runs both ways. The tasks are the user story's sub-issues, so QA's plan review
compares the table with `GET /repos/{owner}/{repo}/issues/{story}/sub_issues`: a row
with no task, or a task with no row, is a finding. `(needs task)` is never left on
an approved feature.

### Area labels — "show me everything that touched this area"

One label per global rule area — `design: events`, `design: security` — applied by
the planner to every task that touches it. That gives a live query that never goes
stale:

```bash
gh api "repos/{owner}/{repo}/issues?labels=design:%20events&state=all" --jq '.[] | "#\(.number) \(.title)"'
```

---

## 7. Approval is two labels, and a hash

Two labels are approvals, and neither is ever applied by the agent that did the
work:

- **`ready for development`**, on each task, applied by QA only, at plan review.
- **`approved`**, on the feature, applied by **the maintainer** — a person, never an
  agent. The Global Rules issue carries `approved` the same way, added after QA's
  global rules review. Agents act through your account, so nothing in GitHub can tell
  a label you added from one an agent added: every agent file and skill forbids it,
  and the body hashes below protect what you approved.

Everything else is status:

```
feature: status: needs-design → status: in-plan-review → status: awaiting-approval
         → approved → status: building → status: in-feature-qa → status: done
         → closed by the feature PR
                       (status: blocked while halted on a question)
task:    status: needs-scoping → ready for development → status: in-progress → status: done
```

A task merged into the feature branch is set `status: done` by the driver — `Closes`
does not fire on a branch other than `main`. The feature PR's body carries a `Closes`
line for the feature, every user story and every task, so they all close when it
merges.

### Freezing the approved plan

Issue bodies stay editable after approval, and REST cannot show their edit history.
So QA records what it signed, in its plan round comment:

```
Plan QA round 2: PASS — BLOCKING 0, ADVISORY 0 — at main 4f2a9c1
Signed: #500 sha256:9c1e…  #502 sha256:41ab…  #520 sha256:d07f…  #521 sha256:77e2…
```

`scripts/issue-hash.sh` takes the hash with line endings and trailing whitespace
normalised and every `- [x]` reset to `- [ ]`, so the developer ticking boxes does
not break it. Before each task the driver recomputes the hashes of the task, its user
story and the feature; a mismatch halts the build with `Build halted: #521 changed after
QA signed it`. Any edit breaks it, a typo fix included — that is intended, and a
small correction costs a QA round.

### Changing an approved plan

The planner takes `ready for development` off the affected tasks and `approved` off
the feature, makes the edits, and posts one `Change <n>:` comment on the feature
listing every issue it changed. QA reviews that set, and the maintainer approves
again. A feature mid-build halts at the next hash check until that is done. A `legacy`
task is never changed this way: a change to its design is new work.

The Global Rules issue changes the same way: `approved` off, the edit, a `Change <n>:`
comment naming every section and term changed and every open feature citing one,
QA's global rules review with its `Signed:` hash, and the maintainer's `approved`.
QA's plan review checks the rules still match that hash, so a plan is never signed
against rules nobody approved.

### The audit trail

To see who labelled what, and when, read the timeline:

```bash
gh api "repos/{owner}/{repo}/issues/500/timeline?per_page=100" \
  --jq '.[] | select(.event=="labeled" or .event=="unlabeled") | "\(.event) \(.label.name) by \(.actor.login) at \(.created_at)"'
```

---

## 8. Building a feature

### 8.1 Where it runs

Claude Code runs on your own machine, where the code is checked out. Windows with Git
Bash is the reference setup; Linux and macOS work too. The build driver is a bash
script and needs `git`, `gh`, `jq` (`winget install jqlang.jq` on Windows), `node` and
`claude` on the path. The integration tests, and the WebApp and storage-client
acceptance tests, need LocalDB, so on your machine they run on Windows only; CI runs
them on Linux against its own SQL Server container.

You sign in to Claude Code with your Claude Pro account (`/login`), and to GitHub with
your own account (`gh auth login`). Builds run under your identity; there is no
shared runner.

A build runs for hours, so leave the machine awake and online while it does. Remote
Control is optional: start the session with it if you want to follow the build, or
answer a halt, from a phone or a browser.

Working on a branch by hand is fine and expected — open the worktree in your editor
and commit as usual — and a hand commit on a task branch goes through the same CI and
QA as the agent's.

### 8.2 Run the build

Once the feature carries `approved`, on your machine:

```
/build-feature 500
```

The skill checks the gates — `approved` on the feature, `ready for development` on
every task, enough disk — tells you the build order, and starts the driver
(`.claude/skills/build-feature/feature-build.sh`) in the background. The driver:

1. Opens the feature branch `users/<handle>/feature-<n>-<slug>` from `main`, with a
   draft PR to `main` titled `FEATURES: <Feature>`, so CI runs on every merge into it.
2. Before each task, if `main` has moved, brings it into the feature branch through a
   short-lived PR (the branch takes no direct pushes) and waits for CI. A conflict halts
   the build.
3. Picks the next task in build order — groundwork, broker, foundation, processing,
   orchestration, exposer, page, then **Depends on** — and rechecks the plan hashes.
4. Builds it in its own worktree with `claude -p "/build-task <n>"` at the effort its
   `Model - Effort` label names: developer → PR into the feature branch → CI → fresh
   QA → fix rounds → a `Build cost:` comment.
5. On `QA PASS` and green CI, merges the task PR into the feature branch with a merge
   commit — only at the commit CI and QA saw — sets the task `status: done`, and waits
   for CI on the feature PR before the next task.
6. When every task is in, runs `/verify-feature` at max effort. Developer findings
   become a fix PR into the feature branch, at most two rounds; findings against the
   plan halt. On `FEATURE PASS`, QA labels the feature PR `ready for review` and
   `/verify-feature` posts the `Handover:` comment on the feature.
7. Puts the handover and the `Closes` lines in the feature PR body and takes the PR out
   of draft.

Each milestone is one line in a `Build progress` comment on the feature issue, and one
line relayed by the coordinating session.

**A halt** — a `QUESTION:` from a build session, a verdict other than `QA PASS`, a plan
hash that changed, a conflict with `main`, red CI after a merge — posts a `Build halted:`
comment on the feature, sets `status: blocked`, and stops. Fix the cause, answer the
question on the issue, and re-run `/build-feature 500`: it skips every task already
`status: done`, and `/build-task` picks up a task whose PR is already open.

**A usage limit** is not a halt. The driver posts `paused until <time>` on the feature
and runs the same session again after half an hour, up to sixteen times. On a Pro
account a long feature may pause more than once.

### 8.3 Merge it

The feature PR is large — every task of the feature — but each task passed QA on its
own and the feature passed feature QA as a whole, so the maintainer's review is about
integration and the handover: the migrations to run and when, configuration and
secrets, ADVISORY findings left open, and the cost. Merge it into `main` with a
**merge commit**. Every merge to `main` deploys to DEV (§11).

### 8.4 Tier 3: `/tweak`

```
/tweak The bookings table header says "Bokings"
```

The skill checks the change is tier 3 — no migration, policy, event, boundary or
session change, no new behaviour, no new package, one file — and stops with a
suggested split if it is not. Otherwise it opens a cut-down task labelled `tier: 3`,
launches one developer on a branch from `main`, waits for CI, runs QA on the PR, and
records a `Tweak cost:` comment. The maintainer merges the PR.

---

## 9. A worked example: saved searches

One feature, four user stories and thirteen tasks. The issue numbers are
illustrative.

**1 — Request.** A requester fills in the feature form: *"Members can save a search
so they can rerun it."* Three behaviour scenarios, one rule under What must not
happen — *a member never sees another member's saved searches* — and two Claude
Design screenshots. It becomes **#500**, `status: needs-design`.

**2 — Plan.** A developer runs `/plan-feature 500`. The planner reads the Global Rules
issue and the code, writes BR-1 … BR-6 from the scenarios and mockups — BR-3, *a saved
search can be deleted from the panel*, is one only the mockup's hover menu showed —
counts one entity, so a foundation service is enough, and opens the tree:

```
#500 Feature  Saved searches
 ├ #501 User story  Storage (model, migration, broker)
 │   ├ #510 DATA: Add Saved Search Model
 │   ├ #511 MIGRATIONS: Add Saved Searches Table
 │   ├ #512 BROKERS: Insert Saved Search
 │   ├ #513 BROKERS: Select All Saved Searches
 │   └ #514 BROKERS: Delete Saved Search
 ├ #502 User story  SavedSearchService (Foundation)
 │   ├ #520 FOUNDATIONS: Add Saved Search
 │   ├ #521 FOUNDATIONS: Retrieve All Saved Searches
 │   └ #522 FOUNDATIONS: Remove Saved Search By Id
 ├ #503 User story  SavedSearchesController → 3 tasks
 └ #504 User story  Saved searches panel (UI) → 2 tasks
```

**3 — Questions.** The planner has two: the most saved searches a member may keep,
and whether a saved search survives a role change. It posts both in one comment and
stops; `/plan-feature` relays them. The requester answers on the issue — "25", "no" —
and the planner writes them into BR-4 and BR-6.

**4 — Plan review.** A fresh QA finds #521 has no negative-path criterion for another
member's rows: BLOCKING, owned by the planner. The planner adds it; QA's round 2
passes with no finding open, labels all thirteen tasks `ready for development`,
records eighteen hashes in its `Signed:` line, and sets `status: awaiting-approval`.
The skill posts `Spec cost:`.

**5 — Approval.** The maintainer reads #500 and adds `approved`.

**6 — Build.** The developer runs `/build-feature 500` on their machine and leaves it
running. The driver cuts `users/<handle>/feature-500-saved-searches`, opens draft PR
`FEATURES: Saved Searches`, and builds the thirteen tasks in order, each in its own
session at its labelled effort. #520 takes two QA rounds — its round 1 found a test
asserting less than its criterion — and the rest take one. Each merges into the
feature branch as it passes. Midway the account reaches a usage limit; the driver
posts `paused until <time>` and carries on after it.

**7 — Feature QA.** A fresh QA at max effort finds BR-4, the 25-search limit, has a
foundation test but no acceptance test through the API. A fix PR adds it; round 2
passes with `FEATURE PASS`.

**8 — Merge.** The driver posts the handover — the migration runs before the app
deploys, no new secrets, the spec and build cost — and takes the feature PR out of
draft. The maintainer reviews and merges it; #500, its four stories and its thirteen
tasks close, and `main` deploys to DEV.

---

## 10. Conventions that bite

**Branches.**

- Feature: `users/<handle>/feature-<n>-<slug>`, cut from `main` by the driver, with a
  draft `FEATURES:` PR to `main`.
- Task: `users/<handle>/<category>-<entity>-<action>`, lowercase after the handle, cut
  from the feature branch. An event-path handler uses its handler name
  (`foundations-savedsearch-onadding`). A name is never reused.
- Decision record or `INTENT.md`: `users/<handle>/design-<area>-<action>`, a `DESIGN:`
  PR to `main`. Global rules are not files: they change on their issue.
- Tier 3: `users/<handle>/fix-<slug>`, from `main`, PR to `main` — or, for a hotfix, from
  and to the release branch.
- Bringing `main` in: `users/<handle>/merge-main-into-feature-<n>-<sha>`, opened and
  merged by the driver, then deleted.
- Release: `release/vX.Y.Z`, cut by the releaser.

Never commit to `main`. Contributors do **not** fork — everything is an in-repo branch,
whatever the vendored branching skill says. Claude Code worktree sessions name their
own branches `claude/<slug>-<hex>`, and several PRs merged from them before the move;
new work takes the names above.

**Commits.** TDD work commits as `{TestName} -> FAIL` then `{TestName} -> PASS`.
Everything else is `CATEGORY: Pascal Case Description`. `FEATURES` is added to the
non-TDD categories for work spanning a feature's layers — the driver's commit that
starts the feature branch, for one.

**Merges.** Every merge is a merge commit — task into feature, feature into `main`,
release into `main`. Squash and rebase are off in the rulesets, and the rulesets
require no reviewer, since a sole maintainer cannot approve their own pull request. A
squash erases the FAIL/PASS record and the SHAs QA reviewed; a rebase rewrites them and
puts deliberately red commits on `main`. Bisect with `git bisect --first-parent` to
step over the FAIL commits.

**PR titles.** Same `CATEGORY:` prefix. The authoritative list is in
`.github/workflows/prLinter.yml` — not in any skill, which carries a shorter and
differently-spelled list. Common ones: `FEATURES:`, `FOUNDATIONS:`, `PROCESSINGS:`,
`ORCHESTRATIONS:`, `COMPONENTS:`, `CONTROLLERS:`, `DOCUMENTATION:`, `DESIGN:`,
`CONFIG:`, `CODE RUB:`, `MINOR FIX:`, `MEDIUM FIX:`, `MAJOR FIX:`, `RELEASES:`. There
is no bare `FIX:`. The labelling job **never fails** — an unrecognised prefix is silently
left unlabelled.

**PR body.** Must link an issue or the PR linter fails: `Closes #512`. `fixes` and
`resolves` (and their past-tense forms) and `AB#<n>` also match. Both the linter and the
Build check run on PRs into `main`, into `users/*/feature-*` and into `release/v*`. A
`RELEASES:` PR is exempt from the linked-issue check: everything it carries is already
merged and closed.

**`gh api` only.** The agents and scripts use `gh api` for every GitHub call — REST, bar the one GraphQL call that takes a feature PR out of draft —
never the `gh issue` or `gh pr` subcommands, so they behave the same everywhere. Do the
same in anything you add to the workflow.

**Effort labels.** Every task carries a `Model - Effort` label, spelled out in full.
**The model is always Opus 5.5; the effort is the only choice**, on the ladder **Low /
Medium / High / Extra / Max**. The driver reads the label and starts the task's session
at that effort — `low`, `medium`, `high`, `xhigh`, `max` — so the label is the setting,
not a note.

`.github/labels.json` also defines labels for other models on the same ladder — `Opus 5 -
*`, `Sonnet 5 - *`, `Fable 5 - *`, `Haiku 4.5 - *` — kept only so closed issues built
under them stay tidy. They are not a choice for new work.

The default for developer work is `Opus 5.5 - High`. It is enough for most
implementation: the phase is fenced on both sides — the criteria are signed before it
starts and QA verifies adversarially after it finishes. Raise it to `Extra` or `Max`
only for:

1. **First of its kind** — a service, layer or component with no sibling in the solution.
2. **Breadth sweeps** — where the risk is whether every site was found.
3. **The security boundary** — identity and access decisions, where a miss usually has
   no failing test to catch it.
4. **Migrations and SQL** — deploy-path, hard to reverse, and the traps survive a
   passing test.
5. **Thin criteria** — open questions left for the developer to settle.

Lower it for trivial work: a rename, a config change or a doc relocation is `Low` or
`Medium`. Between two, take the lower; over-spending is invisible, and on one Pro
account it brings the next usage limit closer.

The other seats are set by their skills: the planner at High, QA's plan review, legacy
intake and feature verification at Max, QA's change verification at Max on round 1 or
any round touching security or schema and High otherwise, the coordinating skills at
Medium, the releaser at High.

**Cost is recorded as comments**, so the plan hashes are untouched: `Spec cost:` on the
feature from `/plan-feature`, `Build cost:` on each task from `/build-task` — dollars,
model and effort per agent, developer runs, QA rounds, BLOCKING at round 1 — and the
total in the handover. On a subscription the dollar figure is the API-equivalent cost,
for comparing features and efforts, not a bill. A session on a model
`scripts/token-usage.mjs` has no price for reports that rather than an understated
figure. `Documentation/ModelBudget/tally.py` reads these comments to answer one
question: is a cheaper effort costing more fix rounds?

**Never add AI or assistant attribution** to a commit message or PR description. It
trips the unattributed-changes rule and blocks the merge.

**Every commit carries the identity of the person responsible for it**, never an AI
tool's — whoever opens the PR owns the code, and the history says so. A *tool
identity* is one whose name, trimmed and compared case-insensitively, is exactly
`Claude`, `Claude Code` or `claude[bot]`, or whose address is at `anthropic.com`;
`Jean Claude` is a person. *Attribution* is whatever the attribution pattern in
`.githooks/identity-guard.sh` matches: a co-author trailer naming the tool, a session
link, a "Generated with" footer. Three layers check this. `§ARC12.11` rules how far
each can be relied on, and only the first is relied on:

- **CI is the enforcement of record.** `rejectAiAttribution` ("Reject AI Identity And
  Attribution") in `.github/workflows/prLinter.yml` runs the guard's own test, then
  fails any PR that adds a commit (`--not origin/<base>`) with a tool author or
  committer or with attribution in its message, and any PR whose title or description
  carries attribution. It is emitted by `GeneratePrLintScript` in
  `Glory2Him.Core.Infrastructure`, so change the generator and regenerate, never the
  YAML. It runs on every PR into `main`, a feature branch or a release branch, but only
  `Build` is a required status check today; making this one required is the owner's
  setting.
- **The git hooks are the local layer.** `.githooks/` refuses the commit and the push.
  `pre-commit` and `pre-merge-commit` judge the identity git resolves, however it was
  set; `commit-msg` judges the message; `pre-push` judges every commit the push
  publishes, which also catches rewritten history and anything committed through a
  path that runs no hook. They stop a tool commit before it leaves the machine, but
  `--no-verify` skips them by design, which is why they are not the record. A clone
  outside Claude Code turns them on once with `git config core.hooksPath .githooks`.
- **The session hooks are best effort.** `.claude/settings.json` turns Claude Code's
  own attribution off and wires hooks that each act on a closed list of forms. A form
  outside a list gets past them, and a hook that errors or times out lets the call
  through; CI catches the result, and the hooks guarantee nothing.
  - At session start, git is pointed at `.githooks/`, and a tool identity is switched
    to the signed-in person (`CLAUDE_CODE_USER_EMAIL`, or `G2H_GIT_USER_NAME` /
    `G2H_GIT_USER_EMAIL` set in a cloud environment's settings). When no person can be
    found it warns and changes nothing, so every commit is refused.
  - Before each shell command, `pre-bash` restores `core.hooksPath` and refuses a
    command whose text holds a hook-skipping token (`--no-veri`, `hookspath`,
    `git_config`, `alias.`, `include.`, `includeIf.`, or a `-n` cluster after
    `commit`), even one that only mentions it. It reads no shell grammar and judges
    neither identity nor messages; the git hooks judge both.
  - For a `gh` command that writes GitHub text, `pre-bash` checks the command's text
    and every file it names as a text source, and refuses a source it cannot read
    (stdin, a substitution, a variable, `~`, a missing file). Write the text to a file
    and pass it with `-F body=@<path>` or `--body-file <path>`.
  - `pre-github` checks the title, body and commit-message fields of a GitHub
    connector write, never a file's contents. The connector appends its own
    "Generated by" footer server-side after that check, so `post-github` tells the
    session to edit it away at once.

  Comments, reviews and issues never pass through CI, so for them the session hooks
  are the only check, and a best-effort one.

All three share one rule set, `.githooks/identity-guard.sh`; no other hook carries its
own copy. `.githooks/tests/identity-guard.test.sh` drives every layer end to end in
scratch repositories, and `rejectAiAttribution` runs it first.

**Skills and agents.** `.claude/skills/the-standard-*` are vendored from
`hassanhabib/the-standard-skills`, and `grilling` and `grill-me` from
`mattpocock/skills`, all through `skills-lock.json`. Treat them as read-only and
reference them by name; a locally edited skill is drift, and its hash in
`skills-lock.json` stops matching. If one is wrong for this repository, say so in the
design. The workflow skills — `plan-feature`, `build-feature`, `build-task`,
`verify-feature`, `tweak`, `release` — and `grill-with-docs` and `standard-compliance`,
adapted from `mattpocock/skills` with the credit in each folder, are the template's
own, and so are the agents in `.claude/agents/`. **They are maintained in
`Glory2Him.Template`:** in any other Glory2Him repository they are a copy refreshed
from the Template byte for byte, so a change to one is made in the Template, under its
own issue and PR, and reaches the other repositories through the next refresh. A skill
only one repository uses is that repository's own and changes there by PR — here,
`update-dependency-graph`, which maintains `Documentation/DependencyGraph/graph.yml`
and the pages beside it.

**Utility brokers.** `scripts/` is refreshed byte for byte too, so
`scripts/utility-brokers.txt` is the Template's seed — `LoggingBroker`,
`DateTimeBroker`, `IdentifierBroker` — and is never edited outside the Template.
`scripts/architecture-graph.py` reads it beside
`Documentation/DependencyGraph/utility-brokers.txt`, the repository's own list, which no
refresh copies or removes. Add a utility broker this repository gains — an
`IdentityBroker identity`, an `AccessBroker access` — to that second file, in the PR that
adds the broker. A class the two lists give different kinds is an error. Which of this
repository's existing brokers belong there is settled with the Global Rules issue; until
then the list carries only its header.

**Interactive skills stay interactive.** `/grill-with-docs`, `/grill-me` and
`/standard-compliance` interview you in rounds and wait for your answers, so they are
never started from a `claude -p` session; nothing in the build driver calls them.
Matt Pocock's other skills — `to-spec`, `to-tickets`, `implement`, `tdd`,
`code-review` — are deliberately not installed: they would run a second workflow
beside this one, without its approvals, and `tdd` would compete with
`the-standard-testing` inside a developer's session. Do not install his plugin on a
machine you build on for the same reason.

---

## 11. Environments and releases

### CI, DEV, and PROD later

Containers prove the code; Azure proves the configuration.

| Stage | Database | Proves | Gate to the next |
| --- | --- | --- | --- |
| Your machine | LocalDB | Logic, validation, exceptions, security tests | Task QA, CI green |
| CI on task and feature PRs | One SQL Server 2022 container for the job | The full suite on the merged tree, and the migration script under `sqlcmd` defaults | Feature QA |
| DEV | Azure SQL | Managed-identity sign-in, network, Key Vault, migrations as deployed | — |
| PROD (later) | Azure SQL | — | A release, through the `Production` environment's approval |

This is where the repository differs from the template's `§ARC2`, which has every
integration test start its own SQL Server 2025, Azurite and Service Bus emulator
containers through Testcontainers. Here the integration suites read their connection
from each project's `appsettings.json` — LocalDB through the trusted connection on your
machine — and CI's `Start SQL Server` step starts one container for the whole job and
points every suite at it through test-only keys, never a production key. The fixtures
replace each catalogue with one named for the process, so parallel runs do not
collide. Whether to move onto Testcontainers is a question for the Global Rules issue,
where `§ARC2` and this repository's own rules meet.

CI also rebuilds the migration script with `Tools/new-database-script.sh`, proves a
copy without its `SET` header fails under `sqlcmd`'s defaults, applies the real one,
and checks no model change is missing a migration. Schema and queries stay inside what
Azure SQL Database supports (`§ARC1`, in the Global Rules issue); there is no separate
check against Azure SQL, so QA's check of every migration against `§ARC1` is the
control, and the DEV deployment is where a migration first meets Azure SQL.

**Every merge to `main` deploys to DEV**, from the repository's own `build.yml`: the
web app to `g2h-dev`, through the `Development` environment, refusing to deploy while
it runs on more than one instance (`§ARC12.12`). DEV is
where connection, token and retry handling first meet Azure, so check there what a
feature's handover asks you to check.

**PROD comes later.** When it does, it will be deployed from a release tag through the
GitHub `Production` environment's required-reviewer approval.

### Releases

On your machine:

```
/release
```

The releaser gathers everything merged since the last `v*` tag, proposes the version
with its evidence — MAJOR for a broken public contract, MINOR for any `FEATURES:` PR or
new endpoint or event, PATCH for fixes only — and waits for you to confirm it. It then
cuts `release/vX.Y.Z`, writes the version, `CHANGELOG.md`, the package report and
`Documentation/Releases/vX.Y.Z.md`, generates the documentation — `design.md` at epic
level and a page per released feature under `Documentation/Design/Features/`, with the
images and diagrams attached to its issue — and the layer graph in
`Documentation/DependencyGraph/architecture.md`, and opens `RELEASES: vX.Y.Z`. A fresh QA checks it:
every merged PR appears in the notes, the bump matches the evidence, no code changed,
the migrations listed exist and are in order, and the documentation says what its
issues say — every business rule, role and diagram traced to its source.

**The maintainer merges it** with a merge commit. `.github/workflows/release.yml` then
checks the branch carries the version and the notes, tags the head of the release
branch `vX.Y.Z`, generates the CycloneDX SBOM, and publishes the GitHub Release with
the notes and SBOM. A release deploys nothing. The npm SBOM covers the same apps whose
version the releaser bumps; a vendored third-party package is marked
`<path>/** linguist-vendored` in `.gitattributes` and left out of both, so its `npm ci`
never stops the tag. Here that is everything under `Templates/` — the Blogzine and
CoreUI themes — so the SBOM and the version bump cover `Websites/Glory2Him.WebApp.React`.

**A hotfix** when `main` holds unreleased work: `/release hotfix <tag>` cuts
`release/vX.Y.(Z+1)` from the last tag, `/tweak --base release/vX.Y.(Z+1) <the fix>`
builds the fix against it, and running `/release hotfix <tag>` again prepares the
release on that branch. Its `RELEASES:` PR into `main` both tags the fix alone and brings
it into `main`. CI and the PR linter run on PRs into `release/v*` too.

A breaking API change arrives under a new `/api/v{major}/` route with the old one kept
until consumers move. One that did not is a finding for the planner, not a silent MAJOR
bump.

---

## 12. Commands and what this repository still has to create

### Commands

`CLAUDE.md` carries the list this repository actually uses. Keep the two in step. The
build and tests are PowerShell — `Get-ChildItem` and the `.ps1` path assume it:

```powershell
dotnet build

# one suite
dotnet test Glory2Him.Core.Tests.Unit

# every suite CI runs, guarded so an earlier failure is not masked by a later pass
foreach ($kind in "*Tests.Unit*.csproj", "*Tests.Acceptance*.csproj", "*Tests.Integration*.csproj") {
  foreach ($project in Get-ChildItem -Filter $kind -Recurse) {
    dotnet test $project.FullName
    if ($LASTEXITCODE -ne 0) { throw "$($project.Name) failed" }
  }
}

# React, from the app's own directory — `;` sequences, it does not stop on failure
npm run lint
if ($LASTEXITCODE -ne 0) { throw "lint failed" }
npm run test
if ($LASTEXITCODE -ne 0) { throw "tests failed" }
npm run build

# regenerate build.yml and prLinter.yml after changing the generator
dotnet run --project Glory2Him.Core.Infrastructure

# republish the branch to local IIS, from the repository root — -Project defaults
# to the main checkout, so pass this checkout's project or you publish whatever
# the main checkout has checked out
D:\Sites\Deploy-Glory2HimWebApp.ps1 -Project .\Websites\Glory2Him.WebApp\Glory2Him.WebApp.csproj
```

`&&` is not a statement separator in Windows PowerShell 5.1, and `;` carries on past a
non-zero native exit code, which is why the guards are there rather than a bare `;`
chain: without them a failed lint followed by a passing build reports success. The
generator writes CRLF on Windows; the committed workflows are LF, so convert them
before you commit.

The workflow's own scripts run in Git Bash:

```bash
node --test scripts/*.test.mjs                          # the workflow scripts' tests
bash scripts/list-deviations.sh > "${TMPDIR:-/tmp}/deviations.txt"   # services with a recorded deviation
python3 scripts/architecture-graph.py --check --deviations "${TMPDIR:-/tmp}/deviations.txt"   # the layer rules
bash scripts/progress.sh                                # features by status
node scripts/token-usage.mjs --current                  # this session's cost
node scripts/prune-merged-worktrees.mjs --apply         # disk housekeeping on your machine
bash .githooks/tests/identity-guard.test.sh             # the identity guard, as CI runs it
```

`.github/workflows/build.yml` is authoritative for what CI runs. It discovers every
`*Tests.Unit*`, `*Tests.Acceptance*` and `*Tests.Integration*` project recursively, so
a test project outside `Glory2Him.Core.Tests.*` still runs.

### Still to create

What the move still has to put in place, step by step in
Glory2Him/Glory2Him.Core#1381. Until each exists, the sections that depend on it are
description rather than mechanism:

- **One run of the label sync** — Actions → Labels → Run workflow — so the status,
  type, approval, `legacy` and effort labels exist.
- **The User Story issue type and sub-issues** enabled for the organisation. The build
  driver finds a feature's stories and tasks only through sub-issues.
- **Rulesets** on `main` and `users/*/feature-*`: a PR required, the `Build` check
  green, merge commits only, no required reviewers. The organisation's ruleset covers
  every branch today, `users/*` included, so pushing to or deleting a task branch
  needs the maintainer's bypass.
- **Your machine** set up: `git` and Git Bash, `gh`, `jq`, `node`, `claude` signed in
  to your Pro account, the .NET SDK and LocalDB.
- **The Global Rules issue**, filed lean with a **Legacy rules** index over the old
  global sections not yet ported, and the presentation component rules in full,
  reviewed by QA and approved by the maintainer. Until then the planner has nothing to
  cite, and `/plan-feature` refuses to start.
- **The features**, ported one at a time from the old documents through
  `/grill-with-docs` and planned with `/plan-feature`: Presentation Components first,
  then Like.

Stated plainly so nobody goes looking:

- **Nothing validates design citations automatically.** `Tools/design-split-audit.sh`
  exists and is run by hand; no CI step runs it.
- **`Documentation/Prompt-CreateFoundationService.md`** predates the agents. It is
  listed in `Glory2Him.Core.slnx`, but no agent reads it.
- **An all-caps `DESIGN` label** exists, created by the PR linter from a `DESIGN:`
  title prefix. It is a category label on PRs, not one of the `design: <area>` labels.
