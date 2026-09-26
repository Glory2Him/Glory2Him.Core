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
  `Documentation/DesignFeatures/` hold what each feature does and how it is
  built, citing the rules above them and never restating them, with any
  deviation recorded and reasoned. The design on main is authoritative. An issue
  that disagrees with it is stale intent, not an instruction; correct the issue.
- **The CI gates** — `.github/workflows/prLinter.yml` holds the authoritative PR
  title prefixes and fails any PR whose body links no issue or task. `Closes
  #<n>` is the preferred form; `fixes`/`resolves` (and their past-tense
  variants) and `AB#<n>` are also accepted — see the workflow for the exact
  pattern.
- **The labels** — `.github/labels.json` is the org label set and
  `.github/workflows/labels.yml` applies it. A PR title prefix that has no label
  in the manifest is a prefix the linter cannot label, so the two are edited
  together.

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

Every handover to QA is a fresh session briefed with pointers only — never
another agent's account of its work. The planner always hands its design and
tasks to QA and corrects what QA finds, until QA has signed every task off —
and passed the design PR, where there is one, with `ready for review`. The
developer hands over its PR, fixes QA's findings as commits on it, and hands
back until QA labels it `ready for review`. After the first round, QA reviews
only what changed since its last round and what that touches. The developer or
QA may hand a question to the planner with context, and a planner change to an
open, signed-off task, or to the design it cites, goes back through QA before
the developer acts on it.

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
    request; only `Build` is a required status check on `main` today, and making
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
