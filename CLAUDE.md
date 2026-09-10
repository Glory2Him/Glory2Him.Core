# Glory2Him.Core

A collaborative content portal built to The Standard. See `INTENT.md` for what the
system does and `Documentation/G2H Design.md` for how it is designed.

## Where the rules live

- **The Standard** — `.claude/skills/the-standard-*`. These own the layer model,
  naming, testing discipline, and the commit, branch and PR formats. Load the
  skill for the layer you are working in rather than working from memory.
- **The design** — `Documentation/G2H Design.md` on main is authoritative. An
  issue that disagrees with it is stale intent, not an instruction; correct the
  issue.
- **The CI gates** — `.github/workflows/prLinter.yml` holds the authoritative PR
  title prefixes and fails any PR whose body has no `Closes #<n>`.

## Development workflow

Non-trivial work moves through four roles, defined in `.claude/agents/`. Each
hands over a durable artifact, not a conversation.

1. **architect** — settles layer placement, event contracts and the security
   boundary, recorded in `Documentation/G2H Design.md`. Skip only for changes
   touching a single file, no schema, no event and no boundary.
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
- No layer calls two layers below it.
- Schema changes are new migrations. Applied migrations are never edited, and a
  migration script must work as a single batch on the deploy path.
- Never add AI or assistant attribution to a commit message or PR description — it
  blocks the merge.
- Never implement behaviour that is not in an approved criterion.
- Adding a dependency, an event, or a layer change is an architect decision.

## Commands

Run from the repository root.

- Build: `dotnet build`
- Unit tests: `dotnet test Glory2Him.Core.Tests.Unit`
- Acceptance tests: `dotnet test Glory2Him.Core.Tests.Acceptance`
- Integration tests: `dotnet test Glory2Him.Core.Tests.Integration`
- Republish the branch to local IIS: `D:\Sites\Deploy-Glory2HimWebApp.ps1`

## Worktrees

The stash stack is shared across worktrees and other sessions may use it
concurrently. Never use bare `git stash` or `git stash pop`; prefer a WIP commit,
or `git stash push -u -m "<unique-tag>"` and apply by SHA.
