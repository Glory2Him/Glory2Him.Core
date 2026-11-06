# Moving an existing repository onto this workflow

How a repository made from the older template — its design kept as files under
`Documentation/Design/` and `Documentation/DesignFeatures/`, and its tasks signed
`ready for development` against them — moves onto the issue-based workflow without
losing or re-planning the work already signed off.

The move is gradual. Nothing is deleted, nothing already signed is rewritten, and a
task signed before the switch is built against the document it was signed against.
What changes is where new design lives: in the Global Rules issue and in feature, user
story and task issues, as [workflow.md](workflow.md) describes.

**Three pieces of advice before you start:**

- **Freeze new design from the refresh (step 2) until the Global Rules issue carries
  `approved` (step 5).** No new feature is grilled or planned, and no design document
  is changed, in between: every new feature is planned against the global rules, and
  `/plan-feature` refuses to start until they are approved. **Building the old way stops
  at the refresh too:** the refreshed developer agent refuses a task whose feature does
  not carry `approved`, so every signed task waits for its legacy feature's intake.
- **Do it one feature at a time.** Inventory and group the signed tasks once (step 8),
  then file, take in and build one legacy feature before you file the next. A
  half-filed feature is easy to finish; a hundred half-filed ones are not.
- **Large rules need not be ported first.** A repository whose rules run far past what
  one issue holds — one with about 1.2 MB of rules, say — files the Global Rules issue
  lean and indexes the rest (step 5). An unstruck index line binds legacy features only,
  and a new feature that needs one waits until it is settled.

## At a glance

| Step | Who | What happens | It ends with |
| --- | --- | --- | --- |
| [1. Prepare](#1-prepare) | The maintainer | In-flight work finished; organisation settings and rulesets in place | A repository ready to move |
| [2. Refresh](#2-refresh-the-workflow-files) | You | The Template's workflow files copied; the repository's own files merged by hand; labels synced | The workflow in place; new design and old-process builds stopped |
| [3. Move the documents](#3-move-the-documents--a-rename-commit-then-a-link-fix-commit) | You | One rename-only commit into `Documentation/Planning/`, then one commit fixing the paths and links it breaks | The old design, unchanged, at its Planning path |
| [4. Add the new scaffold](#4-add-the-new-documentation-scaffold) | You | The Template's `Documentation/` files, in a commit of their own | The releaser's `design.md`, the Planning README, the procedures |
| [5. Global Rules](#5-file-and-fill-the-global-rules-issue) | You, then QA, then the maintainer | File the Global Rules issue in full, or lean with a **Legacy rules** index | The Global Rules issue carrying `approved`; the freeze lifted |
| [6. Record old deviations](#6-record-the-old-deviations) | You | One decision record per entry in the old deviation register | `**Deviation:**` lines `list-deviations.sh` finds |
| [7. Close open design tasks](#7-close-the-open-design-tasks) | You, with the maintainer | Each task that asks for a document closes as superseded | Every design task linking its replacement, and none left to group |
| [8. Inventory and group](#8-inventory-and-group-the-signed-tasks) | You, then the maintainer | List every open task signed `ready for development`; group them into legacy features | A feature list the maintainer has confirmed |
| [9. File one feature](#9-file-one-legacy-feature) | You | A Feature, a User Story per document and feature pair, the tasks as sub-issues; `legacy` on every level | One legacy feature tree |
| [10. Legacy intake](#10-legacy-intake) | QA | Checks the tree, each task's buildability, and the global rules | `Signed:` hashes; `status: awaiting-approval` |
| [11. Approve and build](#11-approve-and-build) | The maintainer, then you | `approved`, then `/build-feature` | The legacy feature in `main` |
| [12. Record what is ported](#12-record-each-document-once-it-is-settled) | You | The document's row in the Ported table | The old document accounted for |

Repeat steps 9 to 11 for each legacy feature, and step 12 whenever a document has no
unstruck index line and its features are filed.

---

## 1. Prepare

- **Finish or close what is in flight.** Merge or close every open PR that implements a
  task or changes a design document. A task whose PR merged is done; a task whose PR you
  closed unmerged keeps `ready for development` and goes through the legacy lane below.
  A design PR left open across the move would land on a path that no longer exists.
  Settle uncommitted work and stale branches, and bring your local `main` up to date.
- **Organisation settings.** The issue types **Feature**, **User Story** and **Task**
  exist and sub-issues are enabled; an organisation owner adds **User Story** if it is
  missing. Sub-issues are required: the build driver finds a feature's stories and
  tasks only through them.
- **Rulesets.** `main` and `users/*/feature-*` require a PR and the `Build` check,
  allow merge commits only, and require no reviewers.
- **Your machine.** `git` and Git Bash, `gh` (signed in with `gh auth login`), `jq`,
  `node`, `claude` (signed in with `/login`), the .NET SDK and Docker. Every command
  below runs in Git Bash from the repository root; `{owner}/{repo}` is filled in by
  `gh api` from the checkout.

## 2. Refresh the workflow files

In a refresh PR of its own, copy from `Glory2Him.Template` **byte for byte**: the
agents, the shared skills, `scripts/`, and `.github/` — the forms, `labels.json` and its
generator, and the workflows, unless the repository generates a workflow, which goes
through its generator as below. Never edit them in this repository; a change belongs in the
Template. A skill of the repository's own, one the Template does not carry, stays.
Leave `Documentation/` out, apart from the utility broker list below: steps 3 and 4
handle it, in that order, so the Template's new `Documentation/Design/design.md` never
collides with the old one.

**The repository's own files are merged by hand, never overwritten:**

- **A workflow generated by code.** Where a build or PR-linter workflow is written by a
  generator in the solution, port the Template's change into the generator and
  regenerate. A hand edit to the YAML is undone by the next generation.
- **`.claude/settings.json`** keeps the repository's hooks — an identity guard, say —
  and adds the Template's deny rules for `Documentation/Diagrams/`.
- **`CLAUDE.md`, `DEVELOPERS.md` and `README.md`** keep the repository's own content:
  what the system is, its commands, its conventions. Bring the Template's workflow
  sections in around it.
- **`.gitignore` and `.gitattributes`** keep the repository's rules and add the
  Template's. `.gitignore` must not ignore `.claude/skills/release/` or
  `Documentation/Releases/`: the Visual Studio rules ignore every `release/` folder, so
  the Template's `!` exceptions come across too. Check that this prints nothing:

  ```bash
  git check-ignore -v .claude/skills/release/SKILL.md
  ```

  **A vendored third-party npm package** — a theme or a copied library with its own
  `package-lock.json` — is marked in `.gitattributes`, `<path>/** linguist-vendored`.
  The release workflow then leaves it out of the npm SBOM, so its `npm ci` cannot stop
  the tag, and the releaser leaves its `package.json` version alone. Check that only the
  repository's own apps end `unspecified`:

  ```bash
  git ls-files -- '*package-lock.json' | grep -Ev '(^|/)node_modules/' \
    | sed 's/package-lock\.json$/package.json/' | git check-attr --stdin linguist-vendored
  ```

- **The repository's utility brokers** move out of `scripts/`.
  `scripts/utility-brokers.txt` is the Template's seed and the copy above replaces it,
  so any line the repository added there — an `AccessBroker access`, say — goes into
  `Documentation/DependencyGraph/utility-brokers.txt`, in this same PR, starting from
  the Template's copy of that file if the repository has none.
  `scripts/architecture-graph.py` reads both lists; no refresh copies or removes the
  second. Run the layer check before
  and after the refresh: it reports no new "not a utility broker" violation.
- **A folder the repository already uses for something else** keeps its files. A
  repository with its own `Documentation/DependencyGraph/` — its own graph and the
  skill that updates it — keeps them; the releaser's `architecture.md` sits beside them.
- **A release job of its own** gives way to the Template's `release.yml`, with one
  version in `Directory.Build.props`.

Then **run the label sync** — Actions → Labels → Run workflow — so `legacy`,
`global rules`, `approved`, the `type:` fallbacks and the statuses exist, and add any
issue forms the merge left out.

**The freeze starts when this PR merges.** No new design until step 5 is approved, and
no more building the old way: the refreshed developer agent refuses a task whose
feature lacks `approved`. Every signed task waits for its legacy feature's intake
(step 10).

## 3. Move the documents — a rename commit, then a link-fix commit

Every existing design document moves under `Documentation/Planning/`, keeping its
sub-path:

| Was | Now |
| --- | --- |
| `Documentation/Design/<X>` | `Documentation/Planning/Design/<X>` |
| `Documentation/DesignFeatures/<X>` | `Documentation/Planning/DesignFeatures/<X>` |
| any other loose design document `<name>` | `Documentation/Planning/<name>` |

`Documentation/Mockups/` does not move.

The move PR holds two commits, in this order.

**The rename commit** holds renames and nothing else — no edited link, no fixed typo —
so git records every file as a 100% rename, `git log --follow` keeps each file's
history, and the mapping above is the whole difference between a task's cited path and
the file. File an issue for the move first — the PR linter needs one to close — then:

```bash
git switch main
git pull
git switch -c users/<handle>/documentation-design-move-to-planning

mkdir -p Documentation/Planning
git mv Documentation/Design        Documentation/Planning/Design
git mv Documentation/DesignFeatures Documentation/Planning/DesignFeatures
# any other loose design document, one line each:
git mv Documentation/<name>.md     Documentation/Planning/<name>.md
```

Check git sees renames, and only renames, before committing:

```bash
git status --short                                  # every line starts with R
git diff --cached --name-status -M100% | grep -v '^R100'   # prints nothing
```

A line from the second command is a file git does not see as an unchanged rename: undo
whatever edited it (`git restore --staged --worktree <path>` on the new path, then
`git mv` again). Then commit and check the commit itself:

```bash
git commit -m "DOCUMENTATION: Move Design Documents Into Planning"
git show --stat -M HEAD | tail -n 3                 # N files changed, 0 insertions(+), 0 deletions(-)
git log --follow --oneline -- Documentation/Planning/Design/Architecture.md | head -n 3
```

**The link-fix commit** fixes what the move broke, and nothing else:

- **Code comments and files citing the old paths.** Find them with
  `git grep -n -e "Documentation/Design/" -e "Documentation/DesignFeatures/" -- ':!Documentation/Planning'`.
- **Relative links** in the moved documents, broken by the extra folder depth —
  `../../INTENT.md` becomes `../../../INTENT.md`, `../Mockups/` becomes
  `../../Mockups/`. Find them with
  `git grep -n -E '\]\((\.\./)+' -- Documentation/Planning`.
- **Solution and project entries** naming a moved file — a solution folder's
  `<File Path="Documentation/Design/…">`, a project's `<None Include>`. Find them with
  `git grep -n "Documentation[/\\\\]Design" -- '*.sln' '*.slnx' '*.csproj' '*.props'`.

```bash
git commit -am "DOCUMENTATION: Fix Links Broken By The Planning Move"
```

Open the PR, `DOCUMENTATION: Move Design Documents Into Planning`, with
`Closes #<move-issue>`, and merge it with a merge commit, so both commits stay.
Issue bodies that name the old paths are **never** edited: the mapping resolves them.

## 4. Add the new documentation scaffold

On a new branch from `main`, copy in the Template's documentation files, in a commit
of their own:

- `Documentation/Design/design.md` — the releaser's placeholder, now in an empty folder;
- `Documentation/Planning/README.md` — its **Legacy** section holds the path mapping;
- `Documentation/Diagrams/README.md`, `Documentation/DependencyGraph/README.md` — merged
  by hand with a README the folder already has. `Documentation/DependencyGraph/utility-brokers.txt`
  is not copied again: step 2 brought it, and from then on it is the repository's own;
- `Documentation/Procedures/` and `Documentation/Decisions/`.

Merge it as a `DOCUMENTATION:` PR. From here on, `Documentation/Planning/` is never a
source — except for a task labelled `legacy`, as the Planning README says.

## 5. File and fill the Global Rules issue

An issue body holds at most 65,536 characters. Measure the old rule documents first —
every document that holds rules more than one feature follows, not only architecture,
security, events and domain: approval rules, UI rules, an index document that numbers
them all:

```bash
cat Documentation/Planning/Design/*.md | wc -m
```

**File it in full** when the rules fit in one issue and each document distils in one
sitting. **File it lean** when they do not. Either way, the pre-filled `§ARC1`–`§ARC5`
stay, in full: they are never deleted, and no rule of the repository's own, ported or
indexed, counts as covering one.

### In full

1. **File it:** Issues → New issue → **Global rules**. The form arrives with the
   conventions and the pre-filled `§ARC1`–`§ARC5`. Security, Events, Domain and Terms
   start empty, and **Legacy rules** keeps its default, `_None …_`.
2. **Port each old rule document into it**, one session each, architecture first:

   ```
   /grill-with-docs Documentation/Planning/Design/Architecture.md
   /grill-with-docs Documentation/Planning/Design/Security.md
   /grill-with-docs Documentation/Planning/Design/Events.md
   /grill-with-docs Documentation/Planning/Design/Domain.md
   /grill-with-docs Documentation/Planning/Design/Design.md
   ```

   Each session lists the rules and terms the document holds, grills you on what it
   leaves open and on everything it says that the code or the pre-filled rules
   contradict — an old rule that put a narrow read's condition in the storage broker,
   say, is now contradicted — and, with your go-ahead, posts a `Change <n>:` comment and
   runs QA's global rules review. `Design.md` carries the old conventions and terms;
   port what is still true.

### Lean, with a Legacy rules index

1. **File it** from the form with:
   - the conventions, unchanged;
   - the pre-filled `§ARC1`–`§ARC5`, all five, in full;
   - Security, Events, Domain and Terms as the form leaves them, or the few rules you
     port in the same sitting;
   - **Legacy rules**: in place of the default, an index of **every** section of every
     old rule document, in each document's order, one line each. A line carries the
     section's number — and its old number, `(formerly §9.7)`, where the document
     records one, so a task citing `§9.7.7` still resolves — its title, and its own
     link, pinned to a 40-character commit SHA at its Planning path, the section's
     anchor on it. Index to the depth you will port at — `§ARC12.5`, not each
     `§ARC12.5.4` — so the index itself fits; a section's sub-sections go with it. If
     the index would still pass 65,536 characters with the rest of the issue, index one
     level shallower; if even the top-level sections will not fit, stop and put it to
     the maintainer before filing. A section of the repository's own numbered
     `§ARC1`–`§ARC5` is indexed under the next free number from the start, marked
     `(formerly §ARCn)`, since those five numbers belong to the template's rules.

   List a document's sections from its headings, at the pinned commit:

   ```bash
   sha=$(git rev-parse origin/main)    # 40 characters, after step 3 merged
   git show "$sha:Documentation/Planning/Design/Architecture.md" | grep -E '^#{2,3} '
   ```

   ```markdown
   - §ARC12.5 (formerly §9.7) — Feature flags — [Architecture.md](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Planning/Design/Architecture.md#arc125-feature-flags)
   - §ARC12.6 — Paging — [Architecture.md](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Planning/Design/Architecture.md#arc126-paging)
   - §APR8.6 — Amender record — [Approval.md](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Planning/Design/Approval.md#apr86-amender-record)
   ```

2. **Send it to QA** for its global rules review, as `/grill-with-docs` does, and have
   the maintainer approve it. The index lines are not rules: they say which old sections
   still bind legacy features.
3. **Settle each line afterwards**, area by area, one session each, whenever a new
   feature needs an area or you choose to:

   ```
   /grill-with-docs §ARC12.5
   /grill-with-docs eventing
   ```

   A line is never deleted. Each session ends it in one of three struck states:

   - `(ported)` — distilled to checkable rules under its own number, the history and
     explanation left in the old document:
     `- ~~§ARC12.5 (formerly §9.7) — Feature flags — [Architecture.md](…)~~ (ported)`;
   - `(retired: <reason>)` — history, a map or conventions, which no longer binds
     anything;
   - `(feature: #<n>)` — feature-level, not a global rule: only the tasks of that filed legacy
     feature cite it, it binds them alone, and it is re-planned with them.

   Ported rules are distilled so the issue body stays under 65,536 characters. If a port
   would push it over, the session stops and puts it to the maintainer, before any
   change is posted. Then the usual Global Rules change and QA review follow.

**What the index binds.** An unstruck line binds legacy features only: a legacy task is
built and verified against the sections it cites, read at their Planning path. A new
feature never cites one. A new feature that touches an area with an unstruck line stops
at `/plan-feature` until that line is settled. A struck line no longer blocks anything.

### Either way

- **Numbering.** A new repository numbers flat. A moved repository keeps its own scheme,
  nested numbers included: code comments and legacy tasks cite the old numbers
  (`§ARC12.5.4`, `design §EVN2`), so a ported section keeps its number and its
  sub-section numbers (`§ARC12.3.1`). Where one must move, it keeps a `(formerly §X)`
  annotation so the old citation still resolves by grep. The one number range that must
  move is `§ARC1`–`§ARC5`, reserved for the form's pre-filled rules because the agents
  cite them by number: a rule of the repository's own that already uses one takes the
  next free number instead.
- **The template's rules beside the repository's own.** Where a rule of the
  repository's own says the same thing as one of `§ARC1`–`§ARC5`, both stand. Where
  they conflict, that is a question for the maintainer through `/grill-with-docs`.
- **Approve it.** When QA's last `Rules QA round` passes with no finding open, the
  maintainer reads the issue and adds `approved`. Only a person adds it — never an
  agent. That lifts the freeze.

## 6. Record the old deviations

**A register entry becomes a decision record.** An architecture document may keep a
register of approved deviations — usually two-to-three (Florance) deviations — for
services designed before user story documents existed. Those services have no user
story issue to carry the deviation, so each gets a decision record under
`Documentation/Decisions/`, numbered from the next free `NNNN`:

```markdown
# 0002 SavedSearchOrchestrationService depends on four services

2 November 2026 · <handle> · **Status: agreed**

**Deviation:** SavedSearchOrchestrationService — two or three service dependencies (Florance)

Approved before the issue-based workflow, in the deviation register of
`Documentation/Planning/Design/Architecture.md` at <40-char-sha>.

## Reason

…as the register gave it.

## Alternatives rejected

…as the register gave them.
```

The `**Deviation:** <ClassName> — <the rule>` line is what `scripts/list-deviations.sh`
finds, so it stands on a line of its own with the class name exactly as the code spells
it. Check:

```bash
bash scripts/list-deviations.sh > "${TMPDIR:-/tmp}/deviations.txt"
python3 scripts/architecture-graph.py --check --deviations "${TMPDIR:-/tmp}/deviations.txt"
```

Open a `DESIGN:` PR from `users/<handle>/design-deviations-record`, closing an issue
filed for it, and merge it. A register entry with no reason recorded is not an
approved deviation: leave it out, and let the layer check report it as a finding.

**A user story document's deviations** go to its legacy User Story issue instead, when
you file it in step 9. List them now, so none is missed:

```bash
git grep -n -i "deviation" -- Documentation/Planning/DesignFeatures
```

## 7. Close the open design tasks

An open task that asks for a document to be written — a `DESIGN:` task, say — has no
document to write any more, and must not be swept into a legacy feature. Before you
group anything, draft each one's replacement with the maintainer: a feature request
filed through the form, or a **Legacy rules** line to settle through `/grill-with-docs`.
Then close the task as superseded, linking its replacement — the feature request's
number, or the URL of the Global Rules issue's `Change <n>:` comment that settles the
line, or, until that change is made, the Global Rules issue itself with the line named:

```bash
gh api -X POST  repos/{owner}/{repo}/issues/<task>/comments -f body='Superseded by <#replacement or link>: <one line on what replaces it>.'
gh api -X PATCH repos/{owner}/{repo}/issues/<task> -f state=closed -f state_reason=not_planned
```

## 8. Inventory and group the signed tasks

Every open task signed `ready for development` before the switch is a legacy task —
except a design task, which step 7 closed. Count them, then list them with their labels
and the documents each body cites, leaving out any whose title starts `DESIGN:`:

```bash
gh api -X GET search/issues \
  -f q='repo:<owner>/<repo> is:issue is:open label:"ready for development"' \
  --jq '.total_count'

gh api --paginate "repos/{owner}/{repo}/issues?labels=ready%20for%20development&state=open&per_page=100" \
  --jq '.[] | select(.pull_request | not) | select(.title | startswith("DESIGN:") | not)
        | [.number, .title,
           ([.labels[].name] - ["ready for development"] | join(",")),
           ((.body // "") | [scan("Documentation/Design(?:Features)?/[^ )`\\]>\"]+")] | unique | join(" "))]
        | @tsv' > "${TMPDIR:-/tmp}/legacy-tasks.tsv"
```

A task whose body says it is a design task is skipped too, whatever its title or labels:
it belongs in step 7. If the list's filter drops any task here, step 7 missed it: stop,
go back to step 7 and close it before you group anything, so no signed task is left
belonging to no feature. `gh api` fills in `{owner}/{repo}` in a path, never inside a
search query, so type the repository's full name in `q`. Sort the list by the feature
document each task cites (`Documentation/DesignFeatures/…`) to see the groups, then
group them:

- **A legacy feature's source** is one of three: a feature document; a global section
  that stands as a feature — a soft-deletion section in `Architecture.md`, say — linked
  at its pinned commit; or none, when no document owns the work, and its body says so.
- **A sub-feature** — a document that extends another feature — becomes a legacy
  Feature of its own, naming the feature it extends in its body.
- **Each user story document** becomes one legacy User Story issue **per document and
  feature pair**, and always links its document. A shared page or component document
  used by two features gives two User Story issues, each linking the same document;
  each feature's tasks go under its own.
- **A task with no feature label** is grouped by the feature document its user story
  document cites. A task that cites only global documents goes under the feature whose
  code it touches.

**Cross-check against the existing labels.** A feature label a task already carries is
the old grouping: where it disagrees with the grouping by document, find out why before
filing.

**Confirm the list with the maintainer before filing anything:** each legacy feature,
its source, its user stories, and the task numbers under each.

Open tasks **without** `ready for development` are not legacy: they were never signed
off, and their design is ported as new work — `/grill-with-docs` on its document, then
`/plan-feature` — after which the old issues are closed as superseded, as step 7 does.

## 9. File one legacy feature

For one group, create the Feature issue, a User Story issue per document and feature
pair, and hang the tasks under them. Every level is labelled `legacy`; each user story
links its document, and the feature its source, if it has one, by a URL pinned to a
40-character commit SHA — a `main` commit after step 3 merged, at the document's
Planning path:

```bash
sha=$(git rev-parse origin/main)    # 40 characters
echo "https://github.com/<owner>/<repo>/blob/$sha/Documentation/Planning/DesignFeatures/SavedSearches.md"
```

**The feature.** Its body states what it is and links its source:

```bash
cat > "${TMPDIR:-/tmp}/feature.md" <<EOF
**Legacy feature.** Designed and signed off before this repository moved to issues.
Its tasks are built and verified against the document below, read at its Planning path.

**Design:** [SavedSearches.md](https://github.com/<owner>/<repo>/blob/$sha/Documentation/Planning/DesignFeatures/SavedSearches.md)
EOF

gh api repos/{owner}/{repo}/issues \
  -f title='Saved searches' -f type=Feature \
  -F body=@"${TMPDIR:-/tmp}/feature.md" \
  -f 'labels[]=legacy' -f 'labels[]=type: feature' \
  --jq '"#\(.number) id \(.id)"'
```

A feature whose source is a global section links that section, its anchor on the
pinned URL. A feature with no source says so in place of the **Design:** line —
`**Design:** none; each user story links its own document.` A sub-feature's body adds `**Extends:** #<feature>`; it is never a
sub-issue of that feature, whose sub-issues the build driver reads as its user stories.

**Each user story**, the same way, with `-f 'type=User Story'`, the label
`type: user-story`, and a body naming its parent, its level and its document, with the
document's approved deviations copied under `## Deviations` in the format
`scripts/list-deviations.sh` reads — the class name in bold, as the code spells it:

```markdown
**Legacy user story.** **Parent:** #<feature> · **Level:** Orchestration

**Design:** [SavedSearchOrchestrationService.md](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Planning/DesignFeatures/SavedSearches/Orchestrations/SavedSearchOrchestrationService.md)

## Deviations

- **SavedSearchOrchestrationService** — two or three service dependencies (Florance) — <the reason, as the document gives it> — rejected: <the alternatives, as the document gives them>
```

A deviation the document records with no reason is not approved: leave it out. A user
story document with no deviations gets no `## Deviations` section.

**Link the tree.** A sub-issue is added by the child's numeric REST `id`, never its
issue number:

```bash
# the user story under the feature
id=$(gh api repos/{owner}/{repo}/issues/<story> --jq .id)
gh api -X POST repos/{owner}/{repo}/issues/<feature>/sub_issues -F sub_issue_id="$id"

# each task under its user story
id=$(gh api repos/{owner}/{repo}/issues/<task> --jq .id)
gh api -X POST repos/{owner}/{repo}/issues/<story>/sub_issues -F sub_issue_id="$id"
```

A task that already has a parent from the old workflow is moved with
`-F replace_parent=true` on the same call.

**Label and type the tasks** — labels and type only; the body is never touched:

```bash
gh api -X POST  repos/{owner}/{repo}/issues/<task>/labels -f 'labels[]=legacy'
gh api -X PATCH repos/{owner}/{repo}/issues/<task> -f type=Task
```

The tasks keep `ready for development`. Check the tree reads back whole:

```bash
gh api repos/{owner}/{repo}/issues/<feature>/sub_issues --jq '.[] | "#\(.number) \(.title)"'
gh api repos/{owner}/{repo}/issues/<story>/sub_issues   --jq '.[] | "#\(.number) \(.title) \([.labels[].name] | join(", "))"'
```

## 10. Legacy intake

In a fresh Claude Code session, run:

```
/plan-feature <feature>
```

On a feature labelled `legacy`, `/plan-feature` plans nothing: it launches QA with
`Act as QA, taking in legacy feature #<feature>.`, and the planner for any finding QA
names as the planner's. The legacy intake takes the place of a plan review. QA does not
re-derive the design; it checks:

- the tree is complete — every task under a user story, every user story under the
  feature, every level labelled `legacy`, each user story linking its document by a
  pinned URL, and the feature linking its source — a feature document or a global
  section — or saying it has none;
- each task is buildable from its own body plus the documents it cites — its legacy
  rules among them — read at their Planning path;
- nothing in it contradicts the approved Global Rules issue. A deviation copied into a
  user story's `## Deviations` is granted, not a contradiction.

A finding goes to its owner like any other: a gap in the tree or a contradiction with
the global rules is the planner's. The planner links a story or task that exists but
sits under the wrong parent; a parent that does not exist yet is yours to create, as in
step 9. A contradiction is your decision: change the global rule through
`/grill-with-docs`, or withdraw the task and plan its operation as new work — never an
edit to the old document or the task's body. A task with a finding against it loses
`ready for development`, and QA gives it back in a later round once none is open. When
no finding is open, QA records the `Signed:` hash list — the feature, every user story,
every task — and sets `status: awaiting-approval`.

From here the bodies are frozen: the build driver refuses a body that changed after QA
signed it.

## 11. Approve and build

The maintainer reads the legacy feature — its tree, its documents, QA's intake round —
and adds `approved`. Then, as for any feature:

```
/build-feature <feature>
```

The developer agent reads each legacy task's body and the documents it cites — its
legacy rules among them — at their Planning path; QA verifies each PR the same way;
feature verification, the handover and the merge into `main` follow as
[new-feature.md](new-feature.md) describes from step 5.

## 12. Record each document once it is settled

An old document gets its row in the **Ported** table of
`Documentation/Planning/README.md` only when it has no unstruck index line — each
ported, retired or left to its legacy feature — its terms are in the Global Rules
issue, and its features are feature issues, legacy or new. Add the row in a
`DOCUMENTATION:` PR:

```markdown
| `Design/Architecture.md` | Global Rules #12 (`§ARC6`–`§ARC11`); decision records 0002–0004 | <handle> | 2026-11-02 |
```

The global documents get their rows when no index line for them is left unstruck and
step 6 is merged; a feature document gets its row when its legacy feature, and any new
feature carved from what it left unsigned, are filed.

---

## What never happens

- **No new design between the refresh and the Global Rules issue's `approved`,** and
  no building the old way after the refresh.
- **The refresh never overwrites the repository's own files.** A generated workflow
  changes in its generator; `settings.json`, `CLAUDE.md`, `DEVELOPERS.md`, `README.md`,
  `.gitignore` and the repository's own folders are merged by hand.
- **A legacy task's body is never rewritten** — not to fix a path, not to fix a typo.
  The mapping in the Planning README resolves its old paths, and QA's hash protects it.
- **No new task is written against `Documentation/Planning/`,** and none cites a section
  on an unstruck line of **Legacy rules**. Only a task signed before the switch carries `legacy`;
  everything new is planned from the Global Rules issue and its feature issue.
- **A Legacy rules index line is never deleted,** and a ported section is never
  renumbered. Its line is struck through as `(ported)`, `(retired: <reason>)` or
  `(feature: #<n>)`.
- **`§ARC1`–`§ARC5` are never deleted,** and no rule of the repository's own covers
  them.
- **No design task is grouped into a legacy feature.** Step 7 closes it first.
- **The planner never adds `legacy`,** and no agent adds `approved`. Only a person
  adds `approved`, on a legacy feature as on any other.
- **The old documents are never edited** to change a legacy task's design. The
  link-fix commit in step 3 is the only change they get. A change is new work:
  `/grill-with-docs`, then `/plan-feature`.
- **Nothing in `Documentation/Planning/` is deleted.** It is the record of what the
  legacy tasks were signed against, and of the rules still indexed.
