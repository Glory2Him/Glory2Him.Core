# Glory2Him.Core

A collaborative content portal built to The Standard. See `INTENT.md` for what the
system does and `Documentation/G2H Design.md` for how it is designed.

## Where the rules live

- **The Standard** — `.claude/skills/the-standard-*`. These own the layer model,
  naming, testing discipline, and the commit, branch and PR formats. Load the
  skill for the layer you are working in rather than working from memory.
- **The design** — `Documentation/G2H Design.md` on main is authoritative, with
  event design split out into `Documentation/Design/Events.md`; §10 of the main
  document is a pointer to it. An issue that disagrees with either is stale
  intent, not an instruction; correct the issue.
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

Every issue carries a `Model - Effort` line in its body and the matching label
spelled out in full, such as `Opus 5 - Medium`.

## Non-negotiables

- No production code without a failing test that demanded it, committed as
  `{TestName} -> FAIL` before the implementation.
- Identity travels on the signed event envelope, never an ambient accessor, and an
  identity-filtered read never decides an invariant.
- Brokers hold no logic and get no unit tests.
- No layer calls two layers below it — **except** an orchestration depending
  only on foundation services (never a mix of foundation and processing, and
  never a broker). `Documentation/G2H Design.md` §12.1 rule 2 and §12.5 record
  the shape — each entity reached through its processing service where one
  exists, its foundation service where none does — and
  `.claude/agents/architect.md` and `qa.md` enforce the never-a-mix half as
  "same kind, never mixed". This deliberately overrides `the-standard-orchestrations`'
  blanket ban on it — see those two agent files for the reasoning.
- Schema changes are new migrations. Applied migrations are never edited, and a
  migration script must work as a single batch on the deploy path.
- Never add AI or assistant attribution to a commit message or PR description — it
  blocks the merge.
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
- Republish the branch to local IIS: `D:\Sites\Deploy-Glory2HimWebApp.ps1`

## Worktrees

The stash stack is shared across worktrees and other sessions may use it
concurrently. Never use bare `git stash` or `git stash pop`; prefer a WIP commit,
or `git stash push -u -m "<unique-tag>"` and apply by SHA.
