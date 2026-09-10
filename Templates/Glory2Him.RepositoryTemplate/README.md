# Glory 2 Him repository template

**Shipped.** `template/` was pushed to
[Glory2Him/Glory2Him.Template](https://github.com/Glory2Him/Glory2Him.Template), which
is marked as a template repository and carries all 112 labels. That repository is now
the source of truth — this folder is the working copy it was built from and can be
deleted. Do not edit it expecting the template to change; edit the template repository.

Nothing here is wired into Glory2Him.Core's build.

## What is in the payload

| File | Origin |
|---|---|
| `.editorconfig` | Copied verbatim from the repository root — the G2HSL file header template and `dotnet_sort_system_directives_first` |
| `.gitattributes` | Copied verbatim — `*.sh text eol=lf` |
| `.gitignore` | The root `VisualStudio.gitignore` with three Glory2Him.Core rules removed (see below) |
| `LICENSE.txt` | The root G2HSL v1.0 licence, with the repository name and year turned into placeholders |
| `README.md` | Written fresh: banner, the org Introduction verbatim, then placeholder sections |
| `.github/labels.json` | Generated from the 103 title prefixes in `prLinter.yml`, plus 9 model-effort labels |
| `.github/workflows/labels.yml` | New — creates and updates labels from the manifest |
| `.github/workflows/prLinter.yml` | Copied verbatim — title prefix labelling, issue link check, author assignment |
| `.claude/agents/` | The four roles, copied and genericized — see below |
| `.claude/skills/` | The 43 vendored The Standard skills, copied verbatim |
| `skills-lock.json` | Copied verbatim — the provenance record for those skills |

Images are **not** in the payload. `create-template-repo.sh` copies `Glory2Him.ico`
and `Glory2Him-Square.png` from `Resources/Images` when it assembles. The README
references the banner by raw URL from `Glory2Him/Glory2Him`, exactly as the
Glory2Him.Core README does, so the 1.3 MB banner and the 2.3 MB logo are not
duplicated into every new repository.

`build.yml` is deliberately excluded — it globs for `*Tests.Unit*.csproj` and assumes
a .NET solution, so it belongs to a .NET repository rather than to every repository.

## The `.claude` folder

`skills/` is 43 skill directories and 3.7 MB, copied verbatim. They are **vendored**
from `hassanhabib/the-standard-skills` — `skills-lock.json` records the six source
packs and their content hashes, which is why it comes along. Copying freezes them:
every repository made from the template gets exactly these rules and works offline,
but nothing pulls upstream fixes in. The alternative is to ship only
`skills-lock.json` and have each repository install from it, which stays current at
the cost of a step before the skills exist. Copying is the choice here because a
template that does nothing until someone runs an installer is a template people
forget to finish.

Two things were **left out**:

- **`launch.json`** — every entry in it names a Glory2Him.Core project
  (`Websites/Glory2Him.WebApp`, the dependency graph server). A launch config
  pointing at projects that do not exist is worse than no launch config.
- **The `update-dependency-graph` skill** — it reads and rewrites
  `Documentation/DependencyGraph/graph.yml` and `projects/*.yml`, a subsystem the
  template does not ship. It would trigger on "refresh the dependency graph" and
  then fail on missing files.

The four agents were copied and genericized — 13 references across 884 lines:

- `Documentation/G2H Design.md` became `Documentation/Design.md` in all four. That
  is now the org convention for a new repository's design document; the agents treat
  it as authoritative over any issue that disagrees with it.
- "the architect for Glory2Him.Core" became "the architect for this repository".
- The local IIS republish step (`D:\Sites\Deploy-Glory2HimWebApp.ps1`) was dropped
  from `developer.md`.
- The three routes for verifying work under a mocked security context are kept in
  `developer.md` and `qa.md` as shapes rather than paths — the React
  `authProvider.tsx` and `TestAuthHandler.cs` file names are gone, and the text asks
  each repository to record its own once they exist. The rule that matters survives
  intact: never extend the test auth handler into the shipped host.

Two `Glory2Him` mentions remain inside the vendored skills — an example root
namespace in a comment in `the-standard-foundations`, and a `using
G2H.StorageClient.Clients;` in the `the-standard-brokers` storage broker template.
Both were left alone: editing vendored content creates a third variant of a file
whose hash is recorded in `skills-lock.json`.

A `CLAUDE.md` is **not** in the payload. The agents assume one — it is what points at
the design document and states the non-negotiables — but the Glory2Him.Core copy is
mostly about migrations, the React app and this solution's layout. Worth writing a
generic one for the template.

## The three `.gitignore` rules that were removed

`/Websites/Glory2Him.WebApp/appsettings.Development.json`, `Documentation/api.bible/`
and `Glory2Him.Core.Database.sql`. Everything else was kept, including two rules the
client repositories are missing:

- **`*\\*`** — MSBuild on Linux emits paths with literal backslashes that `[Oo]bj/`
  never matches, and Windows cannot check them out at all. This broke CI once already.
- **`DeleteMe/`** — the throw-away broker wire-up probe convention.

`Clients/SecurityClients/.gitignore` and `Clients/StorageClients/.gitignore` are both
420 lines against the root's 455 and predate both rules. Worth re-syncing from the
template once it exists.

## Labels

`labels.json` is generated from `prLinter.yml`, so the linter can never apply a label
the manifest does not define. Colours and descriptions were read from the live
Glory2Him.Core labels through the API.

**The live label set is incomplete.** `prLinter.yml` applies a label with
`issues.addLabels`, which silently *creates* any label that does not exist, coloured
GitHub's default grey with no description. That is why `FOUNDATIONS` is curated purple
with "The foundations category" while `BROKERS`, `DATA` and `DESIGN` are grey and blank —
they were auto-created by a pull request title. Of the 103 prefixes, most of the
`MINOR`/`MEDIUM`/`MAJOR` variants have never been created at all: `MINOR FOUNDATIONS`,
`COORDINATIONS`, `BASE`, `MIGRATIONS`, `DOCUMENTATION`, `STANDARD`, `PLANNING`,
`MENTORSHIP`, `DISCUSSION`, `IMPORTS`, `REVIEWS` and `STATUS` all return 404 today.

So the manifest is not a snapshot of the live set — it is the set the live one was
meant to be. 20 base categories and the three `FIX` labels carry their real colour and
wording; the `MINOR`/`MEDIUM`/`MAJOR` variants inherit their base category's colour with
a tier suffix on the description. **14 colours were invented** because the label was grey
or missing: `DATA`, `BROKERS`, `COORDINATIONS`, `BASE`, `DOCUMENTATION`, `STANDARD`,
`DESIGN`, `MIGRATIONS`, `PLANNING`, `MENTORSHIP`, `DISCUSSION`, `IMPORTS`, `REVIEWS` and
`STATUS` — each marked `# INVENTED` in the generator. They follow the existing convention
of CSS named colours. Review those.

The 9 model-effort labels are `Opus 5`, `Sonnet 5` and `Haiku 4.5` against `Low`,
`Medium` and `High`, all in the `fbca04` and wording of the live `Opus 5 - Medium`.
Only that one exists today, so the other eight are a proposal — the agent files show
`Opus 5 - Medium` and `Sonnet 5 - High` but never state the full vocabulary.

The sync workflow **never deletes**. It creates what is missing and corrects the colour
and description of what drifted, and leaves everything else alone.

## Running it

```bash
# Assemble the template repository itself, placeholders intact
Templates/Glory2Him.RepositoryTemplate/create-template-repo.sh /tmp/Glory2Him.Template

# Or scaffold a real repository directly, placeholders substituted
Templates/Glory2Him.RepositoryTemplate/create-template-repo.sh /tmp/G2H.Thing G2H.Thing 2026

# Regenerate the manifest after editing prLinter.yml's prefix list or the colour table
Templates/Glory2Him.RepositoryTemplate/generate-labels.py \
    Templates/Glory2Him.RepositoryTemplate/template/.github/workflows/prLinter.yml \
    Templates/Glory2Him.RepositoryTemplate/template/.github/labels.json
```

`generate-labels.py` fails loudly on a prefix it has no colour for, so adding a prefix to
`prLinter.yml` without giving it a colour cannot silently produce a grey label again.

The script prints the `gh` commands for creating and pushing the repository, and for
ticking **Template repository**.

## What a template still cannot carry

Labels ride along because the manifest and workflow are files. These do not, and stay
manual or move to an org-level ruleset: branch protection and rulesets, secrets and
variables, Actions permissions, topics, and the repository description.

Also worth knowing: an org-level `.github` repository supplies default community health
files (CONTRIBUTING, SECURITY, issue and PR templates) to every repository without
copying them. It does **not** cover README or LICENSE — GitHub needs the licence in the
repository itself for detection — which is why the copyright belongs here.
