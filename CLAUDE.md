# Glory2Him.Core

A collaborative content portal built to The Standard. See `INTENT.md` for what the
system does and `Documentation/Design/design.md` — the index to the design
documents — for how it is designed. `DEVELOPERS.md` walks a person through the
same workflow end to end — the three roles, the documentation layout, and how a
mockup becomes a feature, its user stories and tasks, and merged code.

## Where the rules live

- **The Standard** — `.claude/skills/the-standard-*`. These own the layer model,
  naming, testing discipline, and the commit, branch and PR formats. Load the
  skill for the layer you are working in rather than working from memory.
- **The agents and skills** — `.claude/agents/` and the shared skills under
  `.claude/skills/` are maintained in Glory2Him.Template and refreshed into this
  repository byte for byte. Never change them here: a change is made in the
  Template and arrives with the next refresh. `update-dependency-graph` is this
  repository's own skill and is maintained here.
- **The design** — `Documentation/Design/design.md` is the index. The global
  documents beside it (`Architecture.md`, `Security.md`, `Events.md`,
  `Domain.md`) hold the epic-level rules every feature inherits, and
  `Documentation/G2H Design.md` holds the overview and principles, the numbering
  and citation rules (§IDX1.5), and the map of pre-split section numbers. `Approval.md` and `UI.md` hold two areas designed before features had
  documents of their own. The feature and user story documents under
  `Documentation/DesignFeatures/` — and, for a presentation component, its
  documents under `Documentation/Design/UI/Components/` (§UI20.6.4) — hold what
  each feature does and how it is built, citing the rules above them and never
  restating them, with any deviation recorded and reasoned. A page's document
  under `Documentation/Design/UI/Pages/` (§UI20.5.1) holds its layout, the
  components it renders and what it does with each of their hooks. The design
  on main is authoritative. An issue that disagrees with it is stale intent, not
  an instruction; correct the issue.
- **The CI gates** — `.github/workflows/prLinter.yml` holds the authoritative PR
  title prefixes and fails any PR whose body links no issue or task. `Closes
  #<n>` is the preferred form; `fixes`/`resolves` (and their past-tense
  variants) and `AB#<n>` are also accepted — see the workflow for the exact
  pattern.
- **The labels** — `.github/labels.json` is the org label set and
  `.github/workflows/labels.yml` applies it. Neither it nor `prLinter.yml` is
  edited by hand: `prLinter.yml` is emitted by `GeneratePrLintScript` in
  `Glory2Him.Core.Infrastructure` (§ARC12.11), and `labels.json` is generated
  from it by `.github/generate-labels.py`. Change the generator, regenerate
  `prLinter.yml`, then regenerate `labels.json`.

## Development workflow

Work breaks down the way Azure DevOps does it, and `design.md` defines each
level:

- **Epic** — the whole product (`INTENT.md` and the global design documents).
- **Feature** — ships and works on its own; a feature document.
- **Sub-feature** — a part of a feature too large to plan in one go.
- **User story** — one component at one level, such as a foundation service or
  a page; it need not work on its own, and never spans levels. A feature can span
  several screens — each screen is its own user story.
- **Task** — one operation of a user story: one GitHub issue.

Each level names its parent. Work moves through three roles, defined in `.claude/agents/`, and through a
process sized to its risk. Each role hands over a durable artifact, not a
conversation.

1. **planner** — plans top-down; for tier 1 writes the design first, under a
   design task — global rules, feature and user story documents — settling layer placement, event
   contracts and the security boundary; carves each user story into tasks, one
   per operation, its validations and exception handling included; and writes
   each task's criteria as a sign-off checklist: logic tests (happy path and
   negative path under the security context), validation tests, exception tests.
   Pushes back with a design task when the design is too high-level to plan
   from. Its tasks need QA's sign-off before the developer starts.
2. **developer** — test first, `-> FAIL` then `-> PASS`, one criterion at a time.
   Starts only on a task carrying `ready for development`, and opens the PR once
   the work is done, before handing it to QA.
3. **qa** — always a fresh session with no context from the other roles. Before
   a task's code is written, or when a ruling changes a task, reviews the design
   and the tasks and signs off each task it clears with `ready for development`;
   once the developer has opened a PR, verifies it against the criteria and
   labels it `ready for review` when no finding is open against it. Reports
   BLOCKING and ADVISORY findings. Never fixes anything.

Every handover to QA is a fresh session briefed with pointers and, at most, the
environment facts the paragraph below defines — never another agent's account
of its work. The planner always hands its design and tasks to QA and corrects
what QA finds, until QA labels the design PR, where there is one,
`ready for review`, as QA's signals below gate it — or, where there is none,
until every task is signed off and no finding against the design or the set is
open. The developer hands over its PR, fixes QA's findings as commits on it,
and hands back until QA labels it `ready for review` with no finding open.
After the first round, QA reviews only what changed since its last round and
what that touches. The developer or QA may hand a question to the planner with
context, and a planner change to an open, signed-off task, or to the design it
cites, goes back through QA before the developer acts on it.

The agent that finishes a handover to QA makes it, and the user does not,
whenever the agent's session has the Agent tool — what decides it is whether the
session has that tool, not whether it is top-level. The developer hands over
once it has opened its PR; the planner once it has its tasks, and any design PR,
up. The agent launches QA itself, as a fresh `qa` subagent, briefed with the
brief its own agent file gives: `.claude/agents/developer.md` "Handing over"
defines the developer's brief and its brief after a fix round, and
`.claude/agents/planner.md` "Handing over to QA" the planner's briefs. It may
add environment facts to that brief, and nothing else is an environment fact:
the checkout and worktree paths; for a checkout QA shares with the session that
launched it, the branch it has checked out, and that QA leaves its branch and
files as it found them; which tools are missing or refused, such as a missing
`gh` or a hook that refuses a command; a tool that reaches GitHub but whose
writes the repository refuses, such as `gh` present with every write it makes
gaining an attribution footer; and where scratch work may go. Each tool fact
comes with what QA does about it, as the shared checkout comes with leaving it
as QA found it: where QA cannot post its round or apply its labels, it posts
nothing and returns, as its final message, its round word for word, header line
included, and the labels its verdict carries. Nothing else goes in the brief:
never the agent's own account of the work — what it did, what it changed or
fixed, or what to look at. The agent relays QA's verdict to the user and fixes
every finding QA names as its own, BLOCKING and ADVISORY alike, on a FAIL as on
a PASS, and hands back again until QA passes the work with no finding open. It
fixes each finding of its own the way its agent file has it fix any finding of
its own — `.claude/agents/developer.md` "Handing over" for the developer,
`.claude/agents/planner.md` "Handing over to QA" and "Handling changes" for the
planner. The loop ends for a PR when QA labels it `ready for review`, with no
finding open; for a task review with a design PR, when QA labels the design PR
`ready for review`, as QA's signals below gate it; and for a task review with
no design PR, once every task is signed off and no finding against the design
or the set is open. Because `ready for development` is decided per task, every
task signed off is not on its own the end of a task review with no design PR:
a finding against the design or the set, against no one task, keeps the loop
running. A finding still open once the work QA reviewed has merged is new work:
a task the planner raises for it. A finding QA names as another role's goes to
that role: the agent launches the role that owns the finding as a fresh
subagent, briefed with QA's brief for that owner, and once that role returns,
launches QA again with the brief that role ends with. Every finding QA named as
the planner's holds the developer's fix round, BLOCKING and ADVISORY alike, as
`.claude/agents/developer.md` "Handing over" orders it. A finding the agent
disputes is not worked around: it goes where the agent's file sends it — the
developer's to the planner when it is about the task or the design and to the
user otherwise, the planner's about its own work to the user. The agent
launches the planner itself, as a fresh subagent, for a question the developer
or QA hands to the planner, and for a finding the developer disputes about the
task or the design. The planner is briefed with the context the handing role's
agent file gives that handover, as `.claude/agents/developer.md` "Handing over"
gives it under "What you cannot resolve goes to the planner". The planner's
answer reaches the role that asked as `.claude/agents/developer.md` "Handing
over" and `.claude/agents/planner.md` "Handling changes" have it. An answer
that changes the task or the design goes to QA with the brief the planner ends
with, and the agent launches QA with it; the agent launches the role that asked
again once that role may carry on. A finding that a ruling settles without a
fix — the planner's on a dispute, or the user's — stays open until the planner
has recorded the ruling in the task the work delivers, or in the design
document the finding is against, as a change made under its usual approval
rule (`.claude/agents/planner.md` "Handling changes" and "No revision
history"), and QA reads it there on its next round. That deliberately overrides
`.claude/agents/planner.md` "Handling changes" ("If the task and the design
already answer it, say where: nothing changed, so there is nothing for QA to
agree"), for a ruling that a QA finding stands unfixed. The agent launches no
session except these: QA's; the role that owns a finding QA names; the planner,
for a question or a dispute as above; the role a user's ruling or action goes
back to, as below; and the role a session relaunches as below. A role whose
session has no Agent tool — a role running as a subagent, or a main thread
started as that role — cannot launch QA: it ends with the brief as its agent
file says, and the session that launched it carries the handover in its place.
That session launches QA with that brief, adding only the environment facts
above, relays QA's verdict to the user, and launches the role that owns each
finding QA names — the role it launched or another — briefed with QA's brief
for that owner; once the role returns, the session hands back to QA with the
brief that role ends with. It launches a role for its findings, BLOCKING and
ADVISORY alike, on a FAIL as on a PASS, and the role fixes every finding QA
named as its own. When a round names no finding as a role's own, the session
launches nobody for it. It hands the work back to QA itself once every finding
that holds the role's fix round is settled, with the brief the role's agent file
gives for handing back after a fix round — `.claude/agents/developer.md`
"Handing over" for the developer, `.claude/agents/planner.md` "Handing over to
QA" for the planner. The session carries what the agent carries above, in the
same way: a question or a dispute for the planner, a question QA hands to the
planner included; the planner's answer; and the user's ruling or action. Where
QA cannot post its round or apply its labels itself, the session that launched
QA posts the round word for word where `.claude/agents/qa.md` says it goes, and
applies or removes the labels QA's verdict carries and no others:
`.claude/agents/qa.md` "The label is your mandatory outcome" ties each verdict
to its labels, including the labels a verdict takes off —
`status: needs-scoping` from a task QA signs off, for one.
The round and the labels stay QA's ruling. That holds on the session's own work
too — the planner applying `ready for development` to its own tasks and
`ready for review` to its own design PR, the developer applying
`ready for review` to its own PR — and deliberately overrides
`.claude/agents/planner.md` "Writing the tasks" (only QA applies
`ready for development`), "Handing over to QA" (QA labels the design PR
`ready for review`) and "Hard rules" (`ready for development` is QA's label),
and `.claude/agents/developer.md` "Handing over" (QA labels the PR
`ready for review`). The override is of who applies the label only: signing off
a task and passing a PR or a design PR stay QA's call. On QA's behalf the
session applies only the labels QA's verdict carries, and never signs off a task
or passes a PR on its own judgment. The labels the agent files have a role apply
in its own right are not affected — the planner's `Model - Effort`, `design:`
and `status: needs-scoping` on a task it writes, and its taking
`ready for development` off before it edits a signed-off task. Nor is taking a
stale `ready for review` off: the developer's off its PR when it pushes after
the label went on (`.claude/agents/developer.md` "Branch and pull request"), and
the planner's off its design PR when it corrects a passed design
(`.claude/agents/planner.md` "Handing over to QA"). Each applies whether QA
applied the label or the session applied it on QA's verdict.

The loop waits for the user only for these three kinds of thing:

- merging the design PR and the work — when a planner answer or fix round
  changes the design, the developer does not act on that change until the user
  has merged it to `main`;
- starting the planner or the developer on a task, the task the planner raises
  for a finding still open once the work has merged included — the agent and
  the session launch a role only to carry a handover on work already under way;
- anything an agent file hands to the user, or stops on for the user to answer
  or decide.

The third kind is an open list: wherever an agent file hands something to the
user, the loop stops there, whether or not this file names it. Among them, and
not the whole of them, are what the planner cannot rule on — an owner ruling it
returns as a question, and a finding it disputes about its own work; a finding
the developer disputes that is not about the task or the design, which
`.claude/agents/developer.md` "Handing over" sends to the user; the planner's
proposed split (`.claude/agents/planner.md` "Check the size"); a deviation the
planner proposes, which only the user's approval grants
(`.claude/agents/planner.md` "Writing the design (tier 1)": "You propose it;
the user's approval grants it", and "Boundaries you enforce": "A deviation
requires clear justification AND explicit signoff. It is not
self-grantable"); the developer's effort mismatch
(`.claude/agents/developer.md`, before "Reading the task"); and a signed-in
journey that must be driven in a real browser (`.claude/agents/developer.md`
"Verifying your own work": "that is the one case to hand back to the user"). A
failure QA reports as looking unrelated or pre-existing is not a stop: it is a
finding with an owner, like any other. Beside the three kinds, one case more
waits on the user. Where no session launched the role, the user carries the
handover, as before. At each stop, the agent or the session tells the user what
is waiting, and launches nothing further on that work until the user has acted.
Once the user has acted, the agent or the session carries the user's ruling or
action back into the loop, as it carries any handover, with no further step
from the user: to the planner, where a ruling must be recorded, as above, or
applied, as with an owner ruling the planner returned as a question; to the
developer waiting on a design change, once the user has merged it; and
otherwise to the role that stopped.

QA's signals follow Glory2Him.Template#34. `MERGE READY: YES` closes only a
round that reports no finding: a round that reports any finding closes
`MERGE READY: NO`, a PASS with ADVISORY findings included. A PASS still means
no BLOCKING finding, and QA's round header line is unchanged. In change
verification a PR gets `ready for review` only from a pass that reports no
finding, as its verdict line reads `MERGE READY: YES`, and a pass that reports
any finding takes off a label an earlier pass applied. `ready for development`
is decided per task: a task with any finding against it, ADVISORY included,
does not get the label, and a task with no finding against it is signed off,
even when the same round has a finding against another task. A later round
that finds any defect in a signed-off task takes the label off and returns the
task to `status: needs-scoping`. A design PR's verdict line reads
`MERGE READY: YES` only on a round that reports no finding, and that round also
leaves every task the design PR carries signed off, with no finding against the
design. A finding against neither a carried task nor the design still holds the
line at `MERGE READY: NO` — a thin design PR description, say, or a finding
against a signed-off task outside the design PR whose cited rule the design PR
changed. The design PR gets `ready for review` only when its verdict line reads
`MERGE READY: YES`, as a PR's label is tied to its verdict line; a round whose
verdict line reads `MERGE READY: NO` takes off a label an earlier round
applied, and a later round that finds any defect in the design takes the label
off. A thin PR description, once QA reports it, is an ADVISORY finding like any
other: it goes to its owner, and it holds `ready for review` until it is fixed.
The owner of a PR's description is the developer, and of a design PR's
description the planner, who opens the design PR (`.claude/agents/planner.md`
"How you work"). The user merges the design PR once it carries
`ready for review`, which QA applies only as this paragraph gates it. These
signals deliberately override `.claude/agents/qa.md`: its frontmatter
`description` ("labels it ready for review when it passes"); "Output format"
(the completeness verdict, and the design PR's verdict line, "with nothing
BLOCKING against the design"); "The label is your mandatory outcome" (the label
table), and under it "Reviewing tasks" ("A task earns `ready for development`
when all three of these are true", which they qualify — a task that passes all
three still does not earn the label while a finding against it is open; "A task
carrying any BLOCKING finding does not get the label"; "finds a BLOCKING defect
in it"; "with nothing BLOCKING against the design"; and "If a later round finds
a BLOCKING defect in it"), "Verifying a change" ("A FAIL never gets the label")
and "What the labels are not" ("It is not a verdict on how well the task or the
PR is *written up*", and "A thin PR description covering sound work is at most
an advisory note; it is not a reason to withhold `ready for review`"). Merging
the design PR on its label deliberately overrides `.claude/agents/planner.md`
"Writing the design (tier 1)" ("The user merges the design PR once QA has
passed it"). The end of a task review, as the paragraph beginning "The agent
that finishes a handover to QA makes it" has it, deliberately overrides the
planner's two loop-ends: `.claude/agents/planner.md` "Handing over to QA" ("The
cycle ends when QA has signed off every task with `ready for development` and,
where there is one, labelled the design PR `ready for review`.") and its
frontmatter `description` ("corrects what QA finds until QA signs the tasks
off"). The planner's cycle ends as that paragraph has it, and does not end
while a finding against the design or the set is open, even with every task
signed off.

QA reports a failure that looks unrelated or pre-existing as a finding with its
owner, like any other: the owner fixes it, or disputes it on the dispute route
`.claude/agents/developer.md` and `.claude/agents/planner.md` give. That
deliberately overrides `.claude/agents/qa.md` "Hard rules" ("You do not pass
work because a failure looks unrelated or pre-existing. Report it and let a
human decide."), for its second sentence only. Every finding is fixed, unless a
recorded ruling settles it as above, which deliberately overrides
`.claude/agents/qa.md` "Output format" ("ADVISORY findings are fixed only if
the user asks") and `.claude/agents/developer.md` "Branch and pull request"
("When QA returns a BLOCKING finding that is yours to fix"). QA ends every
round that has a finding with the brief for each owner that has one, a PASS as
well as a FAIL, in the order, and with the developer's wait, that "Output
format" gives for a FAIL, which deliberately overrides `.claude/agents/qa.md`
"Output format" ("End a FAIL with the brief for each owner", and "on a FAIL the
brief for each owner"). In a later round QA checks each ADVISORY finding of its
last round against the change that claims to fix it, as it checks its BLOCKING
ones, and reports again any that is still open, which deliberately overrides
`.claude/agents/qa.md` "Later rounds review only what changed": its step 2
checks BLOCKING findings only, and it says "do not raise an ADVISORY finding
again unless its code changed".

| Tier | Applies to | The planner writes |
| --- | --- | --- |
| **1: design** | A new entity, a schema change or migration, a new event, a change to the security boundary, or a new service, layer or dependency | The design, then the tasks |
| **2: behaviour** | New behaviour inside the existing design | The tasks, criteria only |
| **3: fix or tweak** | A bug fix, a copy or styling change — one file, no schema, no event, no boundary | The task, cut down to the outcome and the criteria that pin the change |

Every tier ends in approved tasks: the PR linter needs an issue to close, and
the developer builds nothing that is not in an approved criterion.

Every QA finding, on a FAIL or a PASS, goes back to whoever owns it:
implementation to the developer, including code that departs from a sound
design; missing or contradictory criteria, or a design that got a boundary or a
layer wrong, to the planner.

Every task carries a `Model - Effort` label, spelled out in full, such as
`Opus 5.5 - Medium`. The model is always Opus 5.5 — only the effort varies, and
developer work defaults to `High`. The label is the decision and the task's body
does not repeat it; what actually ran is appended to the body under
`## Model usage` when the PR opens.

## Non-negotiables

- No production code without a failing test that demanded it, committed as
  `{TestName} -> FAIL` before the implementation.
- Identity travels on the signed event envelope, never an ambient accessor, and an
  identity-filtered read never decides an invariant.
- Brokers hold no logic and get no unit tests. A storage broker also authors no
  query condition and never queries through its `DbContext` — the caller writes
  the condition as a query-shaping function and the storage client awaits the
  terminal operator (§ARC12.2.1, ruled 2026-09-22, conversion not yet built).
- No layer calls two layers below it — **except** an orchestration depending
  only on foundation services (never a mix of foundation and processing, and
  never a *storage* broker). `Documentation/Design/Architecture.md` §ARC12.1
  rule 2 and §ARC12.5 record the exception and no more: an orchestration
  reaches each entity through its processing service where one exists, its
  foundation service where none does. `.claude/agents/planner.md` and `qa.md`
  enforce that as "same kind, never mixed", and deliberately override
  `the-standard-orchestrations`' blanket ban on it — the reasoning is in those
  two files.
- Schema changes are new migrations. Applied migrations are never edited, and a
  migration script must work as a single batch on the deploy path.
- Never add AI or assistant attribution to a commit message or PR description — it
  blocks the merge.
- Every commit is authored and committed under the identity of the person
  responsible for it, never as an AI tool, in cloud sessions as much as local
  ones. A tool identity is a name that, trimmed and compared case-insensitively,
  is exactly `Claude`, `Claude Code` or `claude[bot]`, or any `anthropic.com`
  address; a person whose name only contains the word (`Jean Claude`) is not one.
  If git would commit as a tool, stop and set `git config user.name` /
  `user.email` to that person. `Documentation/Design/Architecture.md` §ARC12.11
  rules how far each layer that checks this can be relied on:
  - **CI is the enforcement of record.** `rejectAiAttribution` ("Reject AI
    Identity And Attribution", emitted into `prLinter.yml` by the generator) fails
    a PR whose own commits have a tool author or committer or carry attribution,
    or whose title or description carries attribution. It runs on every pull
    request into `main`; only `Build` is a required status check on `main` today,
    and making
    this one required too is the owner's setting.
  - **The git hooks are the local layer.** `.githooks/` refuses the commit and the
    push on what git resolves. `--no-verify` skips them by design, which is why
    they are not the record. Never bypass them (`--no-verify`, `core.hooksPath`).
  - **The session hooks are best effort.** The hooks in `.claude/settings.json`
    each act on a closed list of forms. A form outside a list gets past them, and
    CI catches the result. They guarantee nothing.
- Never implement behaviour that is not in an approved criterion.
- Adding a dependency, an event, or a layer change is a planner decision, made
  in the design as tier 1 work.

## Commands

Run from the repository root. `.github/workflows/build.yml` is the authoritative
list — it discovers every `*Tests.Unit*.csproj`, `*Tests.Acceptance*.csproj` and
`*Tests.Integration*.csproj` recursively, so a test project outside
`Glory2Him.Core.Tests.*` (e.g. under `Clients/`, `Websites/`) still runs in CI
even if not named here explicitly.

- Build: `dotnet build`
- All unit tests: `Get-ChildItem -Filter "*Tests.Unit*.csproj" -Recurse | % { dotnet test $_.FullName }`
  — or target one directly, e.g. `dotnet test Glory2Him.Core.Tests.Unit`
- All acceptance tests: same pattern with `*Tests.Acceptance*.csproj`
- All integration tests: same pattern with `*Tests.Integration*.csproj`
- React: `npm run lint`, `npm run test`, `npm run build` (from the React app's
  own directory) — CI runs all three and `npm run build` also type-checks both
  `tsconfig` projects.
- Identity guard: `bash .githooks/tests/identity-guard.test.sh` (CI runs it in
  `prLinter.yml`, not `build.yml`)
- Republish the branch to local IIS:
  `D:\Sites\Deploy-Glory2HimWebApp.ps1 -Project .\Websites\Glory2Him.WebApp\Glory2Him.WebApp.csproj`
  — `-Project` defaults to the main checkout, so a bare run publishes whatever
  that checkout has checked out rather than this branch.

## Worktrees

The stash stack is shared across worktrees and other sessions may use it
concurrently. Never use bare `git stash` or `git stash pop`; prefer a WIP commit,
or `git stash push -u -m "<unique-tag>"` and apply by SHA.
