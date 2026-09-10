# Glory 2 Him repository template

Staged for review. `template/` is the payload that becomes
`Glory2Him/Glory2Him.Template` — the repository new org repos are created from with
**Use this template**. Nothing here is wired into Glory2Him.Core's build; this folder
is a holding area and can be deleted once the template repository exists.

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

Images are **not** in the payload. `create-template-repo.sh` copies `Glory2Him.ico`
and `Glory2Him-Square.png` from `Resources/Images` when it assembles. The README
references the banner by raw URL from `Glory2Him/Glory2Him`, exactly as the
Glory2Him.Core README does, so the 1.3 MB banner and the 2.3 MB logo are not
duplicated into every new repository.

`build.yml` is deliberately excluded — it globs for `*Tests.Unit*.csproj` and assumes
a .NET solution, so it belongs to a .NET repository rather than to every repository.

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
