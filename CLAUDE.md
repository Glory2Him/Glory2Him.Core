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
   labels it `ready for review` when it passes. Reports BLOCKING and ADVISORY
   findings. Never fixes anything.

Every handover to QA is a fresh session briefed with pointers and, at most, the
environment facts the paragraph below defines — never another agent's account
of its work. The planner always hands its design and
tasks to QA and corrects what QA finds, until QA has signed every task off —
and passed the design PR, where there is one, with `ready for review`. The
developer hands over its PR, fixes QA's findings as commits on it, and hands
back until QA labels it `ready for review`. After the first round, QA reviews
only what changed since its last round and what that touches. The developer or
QA may hand a question to the planner with context, and a planner change to an
open, signed-off task, or to the design it cites, goes back through QA before
the developer acts on it.

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
fixed, or what to look at. The agent relays QA's verdict to the user, fixes the
BLOCKING findings QA names as its own, and hands back again, until QA passes the
work. An ADVISORY finding QA names as the agent's own is fixed only when the
user asks for it, as `.claude/agents/qa.md` "Output format" rules. The agent
relays each ADVISORY finding to the user with QA's verdict, on a PASS as on a
FAIL, and a PASS carrying only ADVISORY findings ends the loop. Once the user
asks the agent to fix an ADVISORY finding of its own while the work QA reviewed
is still open, the agent fixes it the way its agent file has it fix any finding
of its own — `.claude/agents/developer.md` "Handing over" for the developer,
`.claude/agents/planner.md` "Handing over to QA" and "Handling changes" for the
planner — and hands back to QA. Once that work has merged, an ADVISORY finding
the user asks to be fixed is new work: a task the planner raises for it. A
finding QA names as another role's goes to the user with QA's brief for that
owner, and a finding QA named as the planner's holds the developer's fix round
only when it is BLOCKING, or ADVISORY and the user has asked for it to be fixed.
An ADVISORY one the user has not asked for counts as settled without changing
the task, which deliberately overrides, for such a finding,
`.claude/agents/developer.md` "Handing over" (findings named as the planner's go
first) and `.claude/agents/qa.md` "Output format" (the developer's fix round
waits until the changed task is signed off again). A finding the agent disputes
is not worked around: it goes where the agent's file sends it — the developer's
to the planner when it is about the task or the design and to the user
otherwise, the planner's about its own work to the user. The agent launches no
session but QA's, and the role a session relaunches as below: a finding it sends
to the planner reaches the planner through the user, as `DEVELOPERS.md` §1 "A
disputed finding comes to you" has it. A role whose session has no Agent tool —
a role running as a subagent, or a main thread started as that role — cannot
launch QA: it ends with the brief as its agent file says, and the session that
launched it carries the handover in its place. That session launches QA with
that brief, adding only the environment facts above, relays QA's verdict to the
user, sends a finding QA names as another role's to the user as above, and, for
a fix round of the role it launched, launches that role again with QA's brief
for it and hands back to QA once the role returns. It relaunches a role for
BLOCKING findings only, and the relaunched role fixes the BLOCKING findings QA
named as its own. An ADVISORY finding of a relaunched role that the user wants
fixed is the user's to start that role on, as they start the planner and the
developer on a task; the session does not carry the user's ask to the role.
Where no session launched the role, the user carries the handover, as before.
Where QA cannot post its round or apply its labels itself, the session that
launched QA posts the round word for word where `.claude/agents/qa.md` says it
goes, and applies or removes the labels QA's verdict carries and no others:
`.claude/agents/qa.md` "The label is your mandatory outcome" ties each verdict
to its labels, including the labels a verdict takes off —
`status: needs-scoping` from a task QA signs off, for one. The round and the
labels stay QA's ruling. That holds on the session's own work too — the planner
applying `ready for development` to its own tasks and `ready for review` to its
own design PR, the developer applying `ready for review` to its own PR — and
deliberately overrides `.claude/agents/planner.md` "Writing the tasks" (only QA
applies `ready for development`), "Handing over to QA" (QA labels the design PR
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

| Tier | Applies to | The planner writes |
| --- | --- | --- |
| **1: design** | A new entity, a schema change or migration, a new event, a change to the security boundary, or a new service, layer or dependency | The design, then the tasks |
| **2: behaviour** | New behaviour inside the existing design | The tasks, criteria only |
| **3: fix or tweak** | A bug fix, a copy or styling change — one file, no schema, no event, no boundary | The task, cut down to the outcome and the criteria that pin the change |

Every tier ends in approved tasks: the PR linter needs an issue to close, and
the developer builds nothing that is not in an approved criterion.

Failed QA goes back to whoever owns the finding: implementation to the developer,
including code that departs from a sound design; missing or contradictory
criteria, or a design that got a boundary or a layer wrong, to the planner.

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
