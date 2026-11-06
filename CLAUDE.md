# Glory2Him.Core

A collaborative content portal built to The Standard. See `INTENT.md` for what the
system does and the **Global Rules** issue — the one open issue labelled `global
rules` — for the rules every feature inherits and the terms they use.
`Documentation/Procedures/` holds the step-by-step walkthroughs a person follows — a
new feature, porting an existing document, and moving a repository made from the older
template onto this workflow. `DEVELOPERS.md` walks a person through the same workflow
end to end: the issue tree, the skills and agents, approval, the build, environments
and releases.

## Moving onto the issue-based workflow

This repository was made from the older template, with its design kept as documents,
and is moving onto the issue-based workflow. Glory2Him/Glory2Him.Core#1381 tracks the
move step by step, following
`Documentation/Procedures/migrating-an-existing-repository.md`. Until #1381 closes:

- **No new design.** No feature is grilled or planned, and no design document
  changes, until the Global Rules issue carries `approved`: there are no settled rules
  to plan against yet.
- **No building the old way, and no legacy lane.** Every open issue from the old
  process is closed as superseded, its comment naming the document its design lives
  in. That work is re-planned the new way, one feature at a time — ported from its
  documents through `/grill-with-docs`, planned with `/plan-feature`, approved, then
  built — starting with Presentation Components, then Like. This repository has no
  `legacy` features; the shared agents and skills support them for other repositories.
- **Where the old design is.** Under `Documentation/Planning/`, at the sub-paths it
  had before the move: `Design/`, `DesignFeatures/`, `G2H Design.md` and the draw.io
  model `Glory 2 Him.drawio`. An old issue or comment naming `Documentation/<path>` for
  any of them means `Documentation/Planning/<path>`; the Planning README's table is the
  mapping. The
  old design is never a source for new work. `Documentation/Design/` is now the
  releaser's, and holds only its placeholder until the first release under this workflow.

Delete this section once #1381 closes.

## Where the rules live

- **The Standard** — `.claude/skills/the-standard-*`. These own the layer model,
  naming, testing discipline, and the commit, branch and PR formats. Load the
  skill for the layer you are working in rather than working from memory.
- **The global rules** — the one open issue labelled `global rules`, filed from
  `.github/ISSUE_TEMPLATE/global-rules.yml`: conventions, Architecture, Security,
  Events and Domain rules cited as `§ARC1`, and the **Terms**. Every feature adheres
  to them; a deviation needs a documented reason in its user story. They change only
  through a `Change <n>:` comment, QA's rules review and the maintainer's
  `approved`. An issue that disagrees with them is stale intent, not an instruction;
  correct the issue. Its **Legacy rules** section indexes the old rule sections not
  yet ported. With no `legacy` features here, an unstruck line binds nothing: a
  feature that needs one waits until `/grill-with-docs` ports or retires it. This
  repository keeps its own rule numbers,
  nested ones included (`§ARC12.5.4`, `§APR8.6.2.1`), so existing citations still
  resolve; only `§ARC1`–`§ARC5` are reserved for the form's pre-filled rules.
- **Features, user stories and tasks** — GitHub issues, linked as sub-issues. A
  feature's business rules are cited `#500 BR-3`, a user story's operations
  `#502 OP-1`.
- **Decision records** — `Documentation/Decisions/`, one file per decision that is
  hard to reverse, surprising without context and the result of a real trade-off,
  added by `DESIGN:` PR. A service designed before this workflow records its
  deviation there, as a `**Deviation:** <ClassName> — <the rule>` line.
- **Who owns `Documentation/`** — `Design/`, and `DependencyGraph/architecture.md`,
  are the releaser's, generated at each release from the issues and the code and never
  a source for anyone else; any other file in `DependencyGraph/` is the repository's
  own — here `graph.yml`, `index.html`, `projects/` and `README.md`, which the
  `update-dependency-graph` skill maintains, and `utility-brokers.txt`, the
  repository's own utility brokers. `Images/`, `ModelBudget/` and
  `Prompt-CreateFoundationService.md` are the repository's own too, from before this
  workflow; `Images/` holds pictures issues embed by pinned URL, and none of the three is
  a source for planning. `Diagrams/` is the user's scratchpad: no
  agent opens it, and `.claude/settings.json` denies it. `Mockups/` is the user's.
  `Planning/` holds existing documents from before this workflow: never a source, they
  count only once ported through the issue forms — features through the Feature
  request form, rules and terms into the Global Rules issue — which `/grill-with-docs
  Documentation/Planning/<document>` does with a person, and no other agent opens it.
- **The agents and shared skills** — `.claude/agents/`, and the skills under
  `.claude/skills/` the Template carries, are maintained in `Glory2Him.Template`.
  In any other Glory2Him repository they are a copy refreshed from the Template:
  never edit, add, rename or delete one there — make the change in the Template, and
  let the next refresh carry it. A skill only one repository uses is that
  repository's own and is maintained where it lives: here, `update-dependency-graph`.
- **The CI gates** — `.github/workflows/prLinter.yml` holds the authoritative PR
  title prefixes and fails any PR whose body links no issue (`Closes #<n>`
  preferred). It and `build.yml` run on PRs into `main`, into `users/*/feature-*`
  and into `release/v*`. Neither is edited by hand: `GenerateBuildScript` and
  `GeneratePrLintScript` in `Glory2Him.Core.Infrastructure` write them. Change the
  generator, run `dotnet run --project Glory2Him.Core.Infrastructure`, and commit what
  it writes with LF line endings.
- **The version** — `Directory.Build.props` holds the application's one version, which
  the releaser sets. Three client libraries are packages versioned on their own and
  keep their own `<Version>`: `G2H.Security.Client`, `G2H.StorageClient` and
  `G2H.EventEnvelope.Client`. That is deliberate; the releaser leaves them alone.
- **The labels** — `.github/labels.json`, generated by `.github/generate-labels.py`
  from the linter's prefixes and applied by `.github/workflows/labels.yml`. Edit the
  generator, never the JSON; after a change to the prefixes, regenerate `prLinter.yml`
  first, then `labels.json`.

## Development workflow

Work is an issue tree: a **Feature** (raised through the issue form by anyone) →
**User stories** (one component at one level each, sub-issues of the feature) →
**Tasks** (one operation each — its logic, validations and exceptions together —
sub-issues of the user story). Every task body is self-contained. The skills below
launch the agents and carry every handover between them; an agent never waits on the
user mid-handover, and every handover to QA is a fresh session briefed with issue and
PR numbers only — never another agent's account of its work.

Skills move work between four agents (`.claude/agents/`):

| Skill | What it does |
| --- | --- |
| `/grill-with-docs [n \| area]` | interviews a person in rounds, each question with a recommended answer, and writes what settles: terms and rules to the Global Rules issue (then a fresh **qa** rules review), decision records, `INTENT.md`, or a `Settled before planning` comment on a feature |
| `/grill-me` | the same interview with nothing written down — for any plan or decision |
| `/plan-feature <n>` | **planner** writes the feature's design sections, user stories and tasks, asking its questions in grilling rounds; a fresh **qa** reviews the plan until it signs every task off with `ready for development`. On a `legacy` feature it runs QA's legacy intake instead of planning |
| `/build-feature <n>` | after the maintainer adds `approved`: the driver builds each task in its own `claude -p "/build-task <n>"` session at its labelled effort — **developer** → PR into the feature branch → CI → fresh **qa** → fix rounds → merge into the feature branch — then `/verify-feature` at max effort and the handover. A `legacy` feature first passes QA's legacy intake in `/plan-feature` |
| `/tweak <request>` | tier 3 only: a cut-down task, one developer, QA on a PR straight to `main` |
| `/release` | **releaser** proposes the SemVer bump, writes version, changelog, the documentation in `Documentation/Design/`, the dependency graph and release notes, and opens a `RELEASES:` PR that **qa** checks, documentation included |
| `/standard-compliance [area]` | surveys the code for breaches of The Standard and the global rules — the layer check plus a sub-agent per layer — reports them as HTML, grills through the one picked, and drafts a feature request for you to file through the form, or words a `/tweak`; never changes code |

1. **planner** — writes into issues, never code. Settles layer placement, entity
   count, event contracts, storage and the security boundary; writes each task's
   criteria as a sign-off checklist: logic tests (happy path and negative path under
   the security context), validation tests, exception tests. Asks its questions in
   grilling rounds — numbered, each with a recommended answer. Never applies
   `ready for development`, `approved` or `legacy`.
2. **developer** — test first, `-> FAIL` then `-> PASS`, one criterion at a time.
   Starts only on a task carrying `ready for development` whose feature carries
   `approved`, or a `tier: 3` task from `/tweak`. Reads the task, its user story, its
   feature and the global rules it cites — nothing else, except that a `legacy` task
   also reads the documents it cites, through the Planning mapping.
3. **qa** — always fresh, briefed with issue and PR numbers only. Six modes: global
   rules review, plan review, legacy intake, change verification, feature
   verification, release check (the generated documentation included). Reports
   BLOCKING and ADVISORY findings, each with an owner — implementation to the
   developer; missing or contradictory criteria, or a design that got a boundary or
   a layer wrong, to the planner. A pre-existing failure is a finding for its owner
   too, never silently fixed or ignored. It withholds `ready for development` and
   `ready for review` while any finding is open. Never fixes anything.
4. **releaser** — prepares a release and generates `Documentation/Design/` and
   `Documentation/DependencyGraph/architecture.md` from the issues and the code; never edits code, never chooses
   the version alone, never merges, tags or deploys.

`/grill-with-docs`, `/grill-me` and `/standard-compliance` are interactive: a person
answers their rounds. Never run them in a `claude -p` session. `grilling` and
`grill-me` are vendored from `mattpocock/skills` and locked in `skills-lock.json`, like
The Standard; `grill-with-docs` and `standard-compliance` are adapted from that
repository and carry their credit in their folders.

A session started by the driver cannot ask anything: when only a person can decide,
write `QUESTION: <the question>` as the last line and stop. The driver halts the
feature on it. The list of things that stop for a person is open, not exhaustive:
wherever an agent file hands something to the user, the work stops there.

| Tier | Applies to | Approval | Built by |
| --- | --- | --- | --- |
| **1: design** | A new entity, a schema change or migration, a new event, a change to the security boundary, or a new service, layer or dependency | QA plan review, then the maintainer's `approved` | `/build-feature` |
| **2: behaviour** | New behaviour inside the existing design | As tier 1 | `/build-feature` |
| **3: fix or tweak** | A bug fix, a copy or styling change — one file, no schema, no event, no boundary | The `/tweak` request — a deliberate exception | `/tweak` |

**Approval integrity.** QA's plan round records a hash of every body it signed
(`scripts/issue-hash.sh`); the driver refuses to build when one has changed. To
change an approved plan, take `ready for development` and `approved` off first, edit,
post a `Change <n>:` comment, and send it back through QA. Only a person adds
`approved` — never an agent.

Every task carries a `Model - Effort` label, spelled out in full, such as
`Opus 5.5 - High`. The model is always Opus 5.5 — only the effort varies, and
developer work defaults to `High`. The driver starts the task's session at that
effort. Cost is recorded as comments — `Spec cost:` on the feature, `Build cost:` on
each task — never in issue bodies.

## Non-negotiables

- No production code without a failing test that demanded it, committed as
  `{TestName} -> FAIL` before the implementation.
- Identity travels on the signed event envelope, never an ambient accessor, and an
  identity-filtered read never decides an invariant.
- Brokers hold no logic and get no unit tests.
- **A storage broker writes no query condition.** It composes no query operator —
  `Where`, `Select`, `OrderBy` or any other — and calls no predicate-taking
  terminal operator, and no broker member queries through its `DbContext` (a
  `DbSet<T>` is model registration, not a query source). The query belongs in the
  foundation service, where it can be tested (`§ARC12.2.1`, ruled 2026-09-22; the
  existing storage brokers are not converted yet): the foundation service authors the
  condition as a query-shaping function using `System.Linq` only and passes it
  down; the broker applies it and awaits the result. The query still runs in SQL —
  a list materialised and then filtered above the broker is still wrong. A keyed
  read's test executes the query-shaping function against a near-miss set.
- No layer calls two layers below it — **except** an orchestration depending
  only on foundation services. An orchestration's service dependencies are one
  kind — all processing or all foundation, never mixed. `§ARC12.1` rule 2 and
  `§ARC12.5` add how it chooses: it reaches each entity through its processing service
  where one exists, its foundation service where none does.
- **Six kinds of utility broker** carry no entity's data, and any service — an
  orchestration included — may hold them without breaking the same-kind or skip
  rules:
  1. **Logging**
  2. **DateTime**
  3. **Identifier**
  4. **Identity** — puts the caller's identity on a signed envelope
  5. **Events** — publishes events or verifies the ones received
  6. **Access** — answers "may this person do this?" with a verdict, gathering only
     the facts that question needs; it never hands an entity's data back

  Every other broker is off limits to an orchestration: a **storage** broker first,
  and any other broker that hands an entity's data back (an API broker over a third
  party's records, say) — that goes through its foundation service.
  `scripts/architecture-graph.py` reads the utility brokers' class names from two
  lists: `scripts/utility-brokers.txt`, the Template's seed, copied byte for byte by
  every refresh and never edited outside the Template; and
  `Documentation/DependencyGraph/utility-brokers.txt`, the repository's own, which no
  refresh touches. A repository adds its own utility brokers to the second.
- An orchestration has two or three service dependencies (Florance). **No broker
  counts toward the two-to-three.** One service dependency means it is not an
  orchestration; a fourth means a split under a coordination service, never a wider
  orchestration; a deviation is recorded by name in its user story.
- These two rules deliberately override `the-standard-orchestrations` 1.1/Don'ts#1,
  `ts-orchestrations-001`, `ts-orchestrations-002` and `the-standard-core`
  `ts-core-003`. `.claude/agents/planner.md` and `qa.md` enforce them as "same kind,
  never mixed" and carry the reasoning; do not correct them back to the skills.
- Schema changes are new migrations. Applied migrations are never edited, and a
  migration script must work as a single batch on the deploy path, inside what Azure
  SQL Database supports (`§ARC1`).
- Only a person adds `approved` — never an agent, on a feature, the Global Rules
  issue or anything else.
- Never add AI or assistant attribution to a commit message or PR description — it
  blocks the merge.
- Every commit is authored and committed under the identity of the person
  responsible for it, never as an AI tool, in cloud sessions as much as local ones. A
  tool identity is a name that, trimmed and compared case-insensitively, is exactly
  `Claude`, `Claude Code` or `claude[bot]`, or any `anthropic.com` address; a person
  whose name only contains the word (`Jean Claude`) is not one. If git would commit as
  a tool, stop and set `git config user.name` / `user.email` to that person.
  `§ARC12.11` rules how far each layer that checks this can be relied on:
  - **CI is the enforcement of record.** `rejectAiAttribution` ("Reject AI Identity
    And Attribution", emitted into `prLinter.yml` by the generator) fails a PR whose
    own commits have a tool author or committer or carry attribution, or whose title
    or description carries attribution. It runs on every PR the linter runs on; only
    `Build` is a required status check today, and making this one required too is the
    owner's setting.
  - **The git hooks are the local layer.** `.githooks/` refuses the commit and the
    push on what git resolves. `--no-verify` skips them by design, which is why they
    are not the record. Never bypass them (`--no-verify`, `core.hooksPath`).
  - **The session hooks are best effort.** The hooks in `.claude/settings.json` each
    act on a closed list of forms. A form outside a list gets past them, and CI
    catches the result. They guarantee nothing.
- Never implement behaviour that is not in an approved criterion.
- Adding a dependency, an event, or a layer change is a planner decision, made
  in the design as tier 1 work.
- Every GitHub call goes through `gh api` — REST, except the one GraphQL call that takes
  a feature PR out of draft, which REST cannot do — never the `gh issue` or `gh pr`
  subcommands.
- Every merge is a merge commit — task into feature, feature into `main`. Never
  squash, never rebase.

## Commits, branches and PRs

- Commits: `{TestName} -> FAIL` / `-> PASS` for TDD work; `CATEGORY: Description In
  Pascal Case` otherwise. **`FEATURES` is added to the non-TDD categories** of
  `the-standard-team-commits` for work spanning a feature's layers; the vendored
  skill cannot be edited, so the addition is recorded here.
- Branches: feature `users/<handle>/feature-<n>-<slug>` (from `main`, with a draft
  `FEATURES:` PR); task `users/<handle>/<category>-<entity>-<action>` (from the
  feature branch); decision record or `INTENT.md` `users/<handle>/design-<area>-<action>`; tier 3
  `users/<handle>/fix-<slug>` (from `main`); release `release/vX.Y.Z`.
- Task PRs go into the feature branch with `Closes #<task>`; the driver marks the task
  `status: done`, since `Closes` fires only on `main`. The feature PR closes the
  feature, every user story and every task.

## Commands

Run from the repository root. `.github/workflows/build.yml` is the authoritative
list — this section describes it rather than competing with it.

- Build: `dotnet build`
- All unit tests: `Get-ChildItem -Filter "*Tests.Unit*.csproj" -Recurse | % { dotnet test $_.FullName }`
  — or target one directly, e.g. `dotnet test Glory2Him.Core.Tests.Unit`
- All acceptance tests: same pattern with `*Tests.Acceptance*.csproj`
- All integration tests: same pattern with `*Tests.Integration*.csproj`. Locally they,
  and the WebApp and storage-client acceptance tests, reach LocalDB through each
  project's `appsettings.json`; CI starts one SQL Server container for the job and
  points every suite at it through test-only keys.
- React: `npm run lint`, `npm run test`, `npm run build` (from the React app's own
  directory) — CI runs all three, and `npm run build` also type-checks both
  `tsconfig` projects.
- Workflow scripts: `node --test scripts/*.test.mjs`
- Layer rules: `python3 scripts/architecture-graph.py --check --deviations <file>`,
  the file from `bash scripts/list-deviations.sh`
- Identity guard: `bash .githooks/tests/identity-guard.test.sh` (CI runs it in
  `prLinter.yml`, not `build.yml`)
- Regenerate the workflows: `dotnet run --project Glory2Him.Core.Infrastructure`
- Republish the branch to local IIS:
  `D:\Sites\Deploy-Glory2HimWebApp.ps1 -Project .\Websites\Glory2Him.WebApp\Glory2Him.WebApp.csproj`
  — `-Project` defaults to the main checkout, so a bare run publishes whatever that
  checkout has checked out rather than this branch.

`build.yml` runs on `ubuntu-latest` and discovers every `*Tests.Unit*.csproj`,
`*Tests.Acceptance*.csproj` and `*Tests.Integration*.csproj` recursively, so a test
project outside `Glory2Him.Core.Tests.*` (under `Clients/` or `Websites/`, say) still
runs in CI. Every push to `main` then deploys the web app to DEV (`g2h-dev`, the
`Development` environment).

## Worktrees

The build driver gives each task its own worktree under `.worktrees/`; prune
finished ones with `node scripts/prune-merged-worktrees.mjs --apply`. The stash
stack is shared across worktrees and other sessions may use it concurrently. Never
use bare `git stash` or `git stash pop`; prefer a WIP commit, or
`git stash push -u -m "<unique-tag>"` and apply by SHA.
