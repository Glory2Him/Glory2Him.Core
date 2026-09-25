# Glory2Him.Core

A collaborative content portal built to The Standard. See `INTENT.md` for what the
system does, and `Documentation/G2H Design.md` with
`Documentation/Design/Events.md` for how it is designed. `DEVELOPERS.md` walks a
person through the same workflow end to end — the four roles, the documentation
layout, and how a mockup becomes a design section, an issue, and merged code.

## Where the rules live

- **The Standard** — `.claude/skills/the-standard-*`. These own the layer model,
  naming, testing discipline, and the commit, branch and PR formats. Load the
  skill for the layer you are working in rather than working from memory.
- **The design** — `Documentation/G2H Design.md` on main is the index and entry
  point; the area files under `Documentation/Design/` are authoritative for their
  areas, and the map at the top of the index says which area is in which file. An
  issue that disagrees with the design is stale intent, not an instruction;
  correct the issue.
- **The CI gates** — `.github/workflows/prLinter.yml` holds the authoritative PR
  title prefixes and fails any PR whose body links no issue or task. `Closes
  #<n>` is the preferred form; `fixes`/`resolves` (and their past-tense
  variants) and `AB#<n>` are also accepted — see the workflow for the exact
  pattern.

## Development workflow

Non-trivial work moves through four roles, defined in `.claude/agents/`. Each
hands over a durable artifact, not a conversation.

1. **architect** — settles layer placement, event contracts and the security
   boundary, recorded in `Documentation/G2H Design.md`, or in
   `Documentation/Design/Events.md` where the subject is event design. Skip only
   for changes touching a single file, no schema, no event and no boundary.
2. **analyst** — writes numbered acceptance criteria into the GitHub issue.
   Requires approval before the developer starts.
3. **developer** — test first, `-> FAIL` then `-> PASS`, one criterion at a time.
4. **qa** — verifies the diff against the criteria in a fresh context. Reports
   BLOCKING and ADVISORY findings. Never fixes anything.

Failed QA goes back to whoever owns the finding: implementation to the developer,
missing or contradictory criteria to the analyst, a crossed boundary or a wrong
layer to the architect.

Every issue carries a `Model - Effort` label, spelled out in full, such as
`Opus 5.5 - Medium`. It is the decision and the issue body does not repeat it; what
actually ran is appended to the body under `## Model usage` when the PR opens.

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
  foundation service where none does.
  The narrowing to one kind — processing services or foundation services, never
  both — is owned by `.claude/agents/architect.md` and `qa.md`, as "same kind,
  never mixed". This deliberately overrides `the-standard-orchestrations`'
  blanket ban on it — see those two agent files for the reasoning.
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
    or whose title or description carries attribution. It is a required status
    check on `main`.
  - **The git hooks are the local layer.** `.githooks/` refuses the commit and the
    push on what git resolves. `--no-verify` skips them by design, which is why
    they are not the record. Never bypass them (`--no-verify`, `core.hooksPath`).
  - **The session hooks are best effort.** The hooks in `.claude/settings.json`
    each act on a closed list of forms. A form outside a list gets past them, and
    CI catches the result. They guarantee nothing.
- Never implement behaviour that is not in an approved criterion.
- Adding a dependency, an event, or a layer change is an architect decision.

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
  `D:\Sites\Deploy-Glory2HimWebApp.ps1 -Project <this checkout>\Websites\Glory2Him.WebApp\Glory2Him.WebApp.csproj`
  — `-Project` defaults to the main checkout, so a bare run from a worktree
  publishes `main` rather than the branch.

## Worktrees

The stash stack is shared across worktrees and other sessions may use it
concurrently. Never use bare `git stash` or `git stash pop`; prefer a WIP commit,
or `git stash push -u -m "<unique-tag>"` and apply by SHA.
