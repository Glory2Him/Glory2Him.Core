# Developing in this repository

How work moves from an idea to merged code here, and how to drive the four
Claude Code agents that do most of it.

`CLAUDE.md` is the short version an agent loads automatically. This is the long
version for a person: it explains the same workflow, plus the parts an agent
never sees — where a mockup goes, how to brief a fresh session, and which
conventions are enforced by tooling rather than by good intentions.

Read `INTENT.md` for what the system is for, and `Documentation/G2H Design.md`
for how it is designed.

---

## 1. The pipeline

```
mockup            (UI work only — Documentation/Mockups/)
   ↓
architect         settles layer, entities, events, storage → writes the design section
   ↓
analyst           turns the design into numbered acceptance criteria → writes them into a GitHub issue
   ↓
qa                checks the issues cover the design — coverage, completeness, size
   ↓
YOU               read the criteria, apply `status: ready-for-dev`
   ↓
developer         test first, one criterion at a time → commits, branch, PR
   ↓
qa                adversarial verification against the criteria → BLOCKING / ADVISORY findings
   ↓
YOU               merge, or send the findings back to whoever owns them
```

Every transition is yours. The agents do not hand work to each other —
none of them can invoke another, because none has a Task tool. **You are the
only thing that moves work between roles**, and the artifact each role leaves
behind is what the next one reads.

Skip stages deliberately, not by accident:

| Stage | Skip it when |
| --- | --- |
| mockup | there is no UI surface |
| architect | one file, no schema, no event, no layer boundary crossed |
| analyst | never — the developer refuses an issue with no approved criteria |
| qa on the issues | the feature is one issue and every criterion is obviously a test name — **never** when a feature spans more than one issue |
| developer | never |
| qa on the work | never for anything that ships |

---

## 2. Every agent runs in a fresh session

This is the rule people get wrong most often, so it is stated before anything
else.

**Each role is a separate session with an empty context.** The architect does
not remember writing the design when the analyst runs. The developer cannot see
the analyst's reasoning, only what the analyst wrote down. QA is deliberately
given a fresh context so it argues with the code rather than with the
developer's summary of it.

That has one consequence worth internalising: **if it is not in the artifact, it
does not exist.** A decision made in conversation with the architect and not
written into the design document is lost the moment that session ends.

### What each role leaves behind

| Role | Durable artifact | Where the next role reads it |
| --- | --- | --- |
| architect | a design section | `Documentation/Design/*.md` or `Documentation/G2H Design.md` |
| analyst | numbered acceptance criteria | the GitHub issue body, under `## Acceptance criteria` |
| developer | commits, a branch, a PR, a handoff report | the PR and its diff |
| qa | BLOCKING / ADVISORY findings | its final report — on the issue when it reviews issues, on the PR when it reviews code |

### How to brief a fresh session

Give the agent three things: **the role, the issue number, and where to read.**
Everything else it can find for itself.

```
Act as the architect. Read issue #512 and settle the design for it.
```

```
Act as the analyst. Issue #512 now has a design section at
Documentation/Design/Events.md §EVN23. Write acceptance criteria into the issue.
```

```
Act as the developer. Implement issue #512. The criteria are approved and the
issue carries `status: ready-for-dev`.
```

```
Act as QA. Verify PR #520 against the acceptance criteria on issue #512.
```

The agent will read the issue, the design and the code itself. Do not paste the
previous session's transcript in — if an agent needs something to do its job and
cannot find it, that is a signal the artifact is incomplete, and the fix is to
improve the artifact rather than to narrate it.

### Set the developer's model and effort before you invoke it

`.claude/agents/developer.md` pins no `model:` and no `effort:` on purpose. The
issue's `Model - Effort` label is the decision, made per issue rather than per
role — but **nothing in this repository reads that label and configures a
session.** No hook, no script, no mechanism. You set it by hand, before you
invoke the developer, because a session cannot change its own model once
running.

The developer can only detect the mismatch afterwards and stop. An issue with no
`Model - Effort` label is not ready to start.

The other three roles are pinned in their own files: architect and analyst run
`opus` / `high`; QA runs `opus` / `max` deliberately, so the reviewer is never
reasoning less hard than the implementer did.

---

## 3. The four agents

Defined in `.claude/agents/`. Each file is the authority on its own role; this
section tells you when to reach for which.

### architect — shape, not syntax

**Owns** layer placement, entity count, event contracts, the security boundary,
storage and migration shape.

**Produces** an update to the design document, in the section that already owns
the subject. Nothing else. "No design needed, hand to the analyst" is a valid
output and you should expect it often.

It settles seven things in order: the problem in one paragraph, layer placement,
**the entity count** (this is what decides the layer — one entity means
foundation or processing, two or three means orchestration, more than three is a
violation), event contracts in `<Subject>-<Verb>` form, storage and migration
shape including the seed consequence, risks split into reversible and not, and an
explicit out-of-scope list.

**Use it** before any non-trivial implementation, and again afterwards to review
whether the structure held — that second mode reports only structural findings,
marked BLOCKING or ADVISORY.

**Skip it** for a change touching a single file with no schema, no event and no
boundary crossed.

It never writes production code and never fixes defects.

### analyst — criteria, not code

**Owns** turning intent into acceptance criteria precise enough that a developer
can write a failing test from them without asking a question.

**Produces** criteria written into the GitHub issue body with `gh issue edit`,
under an `## Acceptance criteria` heading. **The issue is the spec.** There is no
parallel spec file, deliberately — a second document would drift from the issue.

Aim for five to eight criteria; ten is a ceiling, not a target. If it is past ten
and still on the happy path, the issue needs splitting, and the analyst will stop
and propose the split mid-draft rather than write a criteria list nobody can
finish.

Every criterion must be expressible as a single test name. If you cannot imagine
the test name, the criterion is not finished.

**Use it** for every piece of work, including work that skipped the architect.

It has no `Edit` and no `Write` tool — it changes the issue through `gh`, and
touches no file in the repository.

### developer — test first, one criterion at a time

**Owns** implementation. **Produces** commits, a branch, a PR, and a written
handoff naming the criteria implemented, the tests covering each, any migrations
added, and the commit SHAs — ending in its own verdict line, `MERGE READY: YES`
or `MERGE READY: NO`, which judges only whether its work is done and never
whether a human has approved it.

The loop per criterion is: write one test, run it and confirm it fails **for the
right reason**, commit it as `{TestName} -> FAIL`, implement the smallest change
that passes, commit as `{TestName} -> PASS`. No production code exists without a
failing test that demanded it.

**Use it** only when the issue carries approved criteria. It is the only agent
with a `Write` tool.

### qa — adversarial, never fixes

**Owns** finding the reasons a change should not ship. **Produces** findings
marked BLOCKING or ADVISORY. It never fixes anything, deliberately: the person
who broke it should fix it, and a reviewer who patches defects stops looking for
more.

It assumes the developer's summary is optimistic and verifies against the code.
Always run it in a fresh session — that is the whole point of it.

It has a **second mode**, defined in its agent file: reviewing **the issues before
any code exists**. Does the design have an issue behind every section, do those
issues together capture the whole feature, is any of them too big, can every
criterion become a test name. Where a feature needed more than one issue that
review is mandatory — each issue was sized on its own, and nothing else in the
pipeline ever asks whether the set is complete. Say which mode you want when you
brief it; verifying a diff is the default and it will otherwise go looking for a
diff that does not exist.

**Route failures by owner**: implementation defects to the developer, missing or
contradictory criteria to the analyst, a crossed boundary or wrong layer to the
architect.

---

## 4. The Documentation folder

```
Documentation/
  G2H Design.md            the main design document (~3,700 lines, numbered sections)
  Design/                  area-scoped design documents, each with its own section prefix
    Events.md              §EVN0 … §EVN22  (event design)
  Mockups/                 Claude Design exports awaiting or feeding a design section
  Images/                  static visual assets referenced from issues and design docs
  DependencyGraph/         generated architecture graph and its viewer
  Glory 2 Him.drawio       the original design sketch, cited by §1.3 as a source input
  Prompt-CreateFoundationService.md   a standalone prompt template, predating the agents
```

### The split, and why sections carry prefixes

`G2H Design.md` is being broken into area-scoped files under
`Documentation/Design/` (issue #481). `Events.md` is the first and currently the
only one. The remaining areas — architecture, domain, security, UI — are planned
and their prefixes are already reserved: `ARC`, `DOM`, `SEC`, `UI`.

Sections in a split file carry a **flat, prefixed number** — `§EVN1`, `§EVN2` —
rather than restarting at 1, so that a bare citation stays unambiguous once
several `Design/*.md` files exist side by side. A relocated section also keeps a
`(formerly §10.X)` annotation naming its old position, so the dozens of C#
comments that cite the old number still resolve by grep.

**If you add a new split file, copy that convention from `Events.md`'s intro
block.** Pick the reserved prefix for the area, number flat, and annotate every
relocated section with where it came from.

Two cautions learned the hard way:

- **Resolving is not the same as being right.** The annotation maps an old number
  to a new one; it says nothing about whether the section was the correct one to
  cite originally.
- **Nothing validates citations.** No CI step, no script. The guarantee that an
  old `§10.X` still resolves is maintained by the annotation convention and
  nothing else.

### Citing design from code

Code comments cite design sections constantly, and this is the main reason the
numbering discipline matters:

```csharp
// design §14.6 rule 2: either service must be safe when called alone
// (§EVN2 rule 4, §EVN18(a))
```

Most of the codebase still cites the pre-split `§N.N` form. That is expected —
the `(formerly …)` annotations exist precisely so those citations keep working.

---

## 5. From a Claude Design mockup to a design section

UI work usually starts as a picture. The job of this stage is to get the picture
into the repository and then **out of the critical path**, because a picture is
not a test name and a mockup left as a second source of truth will eventually
contradict the design.

### 5.1 Put the export in the repository

A Claude Design export is a single self-contained HTML file. Save it to:

```
Documentation/Mockups/<feature-slug>/<screen-name>.html
```

Alongside it, save a **flattened image** of each screen — `.webp` or `.png` — in
the same folder. The image is what you embed in the issue; the HTML is what
someone opens when they need to see hover states, spacing or the real DOM.

Bundled exports run to several megabytes. Keep the HTML when the interaction
matters and the image alone when it does not, and do not commit five versions of
the same screen because a design iterated.

Every mockup folder gets a `README.md` with three lines: what it shows, the issue
it came from, and — once the architect has written the design — **the design
section it produced**. That last line is what stops the mockup becoming a rival
spec:

```markdown
# Content item search panel
Source: Claude Design export, 2026-09-11. Issue: #398.
Superseded by the design at `Documentation/Design/Ui.md` §UI4 — that section
wins wherever the two disagree.
```

Commit it on its own, with a `DOCUMENTATION:` prefix.

### 5.2 Embed it in the issue by pinned commit

When you reference an image from a GitHub issue, use a **raw URL pinned to the
full 40-character commit SHA**, never a branch:

```markdown
![Cards](https://raw.githubusercontent.com/Glory2Him/Glory2Him.Core/29e95e9f47d50c43c80864ac8b517ab6e493abc0/Documentation/Images/ContentItemSearchPanel/redesign-cards.png)
```

Pinned to a SHA, the picture in the issue cannot change under it later. Issue
#398 is the worked precedent and is worth reading before you do this the first
time — it pairs the images with prose, a component tree and an event-hook table,
which is the level of written detail the analyst needs.

Drag-and-dropping an image into the GitHub comment box also works and is hosted
by GitHub, but it lives nowhere in the repository. Use it for a throwaway
annotation, not for the spec.

### 5.3 Ask the architect to turn it into a design section

```
Act as the architect. Issue #512 has a mockup at
Documentation/Mockups/saved-searches/ and images embedded in the issue body.
Settle the UI design for it and write it into the design document.
```

The architect writes the section, gives it a prefixed number, and tags the
heading (§6). From that moment the design section is authoritative and the mockup
is history — go back and add the "Superseded by" line to the mockup's README.

### 5.4 Then the analyst writes criteria in words

```
Act as the analyst. Issue #512's design is at Documentation/Design/Ui.md §UI7.
Write acceptance criteria into the issue.
```

Criteria must be derived from the design in words. "Matches the mockup" is not a
criterion, because it cannot be a test name.

---

## 6. Linking design and issues

Two mechanisms, deliberately different, answering two different questions.

### Heading tags — "what issue defines this section?"

**Proposed, not yet applied.** This is #498's criterion 1 and no heading carries
a tag today — all 23 headings in `Events.md` are still bare. Adopt it as you
touch sections; do not read it as an invariant you can rely on.

Every numbered heading in `Documentation/Design/*.md` should carry exactly one of
two tags, never bare:

```markdown
## UI7. Saved searches panel (#512)
## UI8. Search result density (needs issue)
```

`(#N)` names the **most recent** issue that authoritatively defined the section —
not an accumulating list, because `git log` and `git blame` already give the full
history for free. `(needs issue)` is an explicit, greppable flag for design
content nobody has scheduled yet.

The tag is mandatory rather than inferred, because a bare heading is ambiguous:
deliberately skipped, or just missed? Requiring a tag forces the decision every
time a section is touched. **The architect sets these**, and may not leave a
heading bare when it writes or substantially expands a section.

### Area labels — "show me everything that touched this area"

One label per `Design/*.md` file — `design: events`, `design: ui` — applied by the
analyst when it writes an issue's criteria. That gives a live query that never
goes stale, because it is GitHub's own index:

```bash
gh issue list --label "design: events" --state all
```

### Sweep mode — generating issues from the gaps

**Proposed, not yet available.** This is #498's criterion 3. The checked-in
`analyst.md` has no sweep mode, and it could not complete the last step of one
even if asked: it holds no `Edit` or `Write` tool and is told "never edit a file
in the working tree", so it cannot rewrite a heading tag. Until the agent
contract is updated, treat the flow below as the intended design and do the tag
rewrite yourself.

The idea is a second way in. Instead of "turn this feature description into
criteria", you point the analyst at the design documents:

```
Act as the analyst in sweep mode. Find design sections with no issue behind them
and propose issues for them.
```

The sweep itself is a grep you can run today:

```bash
grep -rn "(needs issue)" Documentation/Design/*.md
```

For each hit the analyst does exactly what it does for a human-described feature
— the size check, splitting if too big, criteria into a new issue, the
`Model - Effort` label, the area label. Same skill, different starting point.
Flipping the heading tag from `(needs issue)` to `(#<new-issue-number>)` is then
an edit to the design document, which belongs to you or to the architect.

---

## 7. Approval is a label

**Proposed, not yet in force.** This is #498's criterion 4. The `status:` labels
do not exist yet, and neither `developer.md` nor `qa.md` mentions them — grep
both and you get nothing. Until the labels are created and those two agent files
updated, the lifecycle below is the intended process and the gate is your own
judgement, not something an agent will refuse to proceed without.

There is no PR-gated approval for a spec, and no approval file. Approval is a
label on the issue, applied by you:

```
status: needs-scoping → status: ready-for-dev → status: in-progress → status: in-qa → status: done
```

The analyst leaves an issue at `status: needs-scoping`. You read the criteria and,
when satisfied, apply `status: ready-for-dev` by hand. That is the same judgement
a PR approval would have expressed, as a label toggle instead of a merge.

**The developer's rule, once the labels land: never start without
`status: ready-for-dev`.** QA's verdict then says which label should come next —
`status: done`, or back to `status: in-progress` on a BLOCKING finding. Both are
changes #498 makes to `developer.md` and `qa.md`; today neither agent checks a
status label, so applying it is a discipline you keep rather than one they
enforce.

**The honest trade-off:** a label has a thinner audit trail than a PR review. To
see who changed a status and when, read the issue's timeline:

```bash
gh api "repos/Glory2Him/Glory2Him.Core/issues/512/timeline?per_page=100" \
  --jq '.[] | select(.event=="labeled" or .event=="unlabeled") | "\(.event) \(.label.name) by \(.actor.login) at \(.created_at)"'
```

`gh issue view --json timelineItems` does **not** work — there is no timeline
field on that command. Use the REST endpoint above.

---

## 8. A worked example

Issue #512, "add a saved-searches panel". UI work, so it starts with a picture.

**1 — Mockup.** Export from Claude Design, save to
`Documentation/Mockups/saved-searches/panel.html` plus `panel.webp`, write the
folder README, commit:

```
DOCUMENTATION: Add The Saved Searches Panel Mockup
```

**2 — Issue.** Open it, embed `panel.webp` by pinned-SHA raw URL, describe the
behaviour in prose. First line of the body:

```markdown
**Model - Effort:** Opus 5 - Medium
```

Apply the matching `Opus 5 - Medium` label.

**3 — Architect.** Fresh session: *"Act as the architect. Issue #512 has a mockup
at Documentation/Mockups/saved-searches/. Settle the design."* It writes
`Documentation/Design/Ui.md` §UI7, tagged `(#512)`, and commits with a `DESIGN:`
prefix.

**4 — Analyst.** Fresh session: *"Act as the analyst. Issue #512's design is at
Documentation/Design/Ui.md §UI7. Write acceptance criteria into the issue."* It
writes six numbered criteria and applies `design: ui` and
`status: needs-scoping`.

**5 — QA, on the issues.** Before a line of code exists:

```
Act as QA, reviewing the issues rather than a change. Issue #512's design is at
Documentation/Design/Ui.md §UI7. There is no code yet — do not look for any.
```

It checks that every section of the design for this feature has an issue behind
it, that the issues together capture the whole of it, that none is too big, and
that every criterion can become a test name. A criterion that cannot costs
minutes here and a wasted implementation later. Findings route to the analyst.

**Where a feature needed more than one issue this step is not optional.** Each
issue was sized on its own; nothing before this asks whether the set covers the
feature.

**6 — You.** Read the criteria yourself — QA advises, you decide. If they are
right, apply `status: ready-for-dev`. If a criterion cannot become a test name,
send it back.

**7 — Developer.** Set the session to **Opus 5 · Medium** first, to match the
label. Fresh session: *"Act as the developer. Implement issue #512."* It branches
`users/cjdutoit/components-savedsearches-add`, then per criterion commits
`ShouldRenderSavedSearchesPanelAsync -> FAIL` followed by
`ShouldRenderSavedSearchesPanelAsync -> PASS`, and opens a PR titled:

```
COMPONENTS: Add A Saved Searches Panel
```

with `Closes #512` in the body. You move the issue to `status: in-progress`.

**8 — QA, on the work.** A *different* fresh session from step 5: *"Act as QA.
Verify PR #520 against the acceptance criteria on issue #512."* Move the issue to
`status: in-qa`. QA reports two ADVISORY findings and no BLOCKING ones.

**9 — Merge**, and set `status: done`.

### The same example, starting from a sweep

If §UI8 "Search result density" had been written by the architect and left
`(needs issue)`, step 2 inverts: you run the analyst in sweep mode, it finds the
tag, opens issue #513 with criteria already written, applies `design: ui`,
`Opus 5 - Medium` and `status: needs-scoping`, and rewrites the heading to
`## UI8. Search result density (#513)`. You pick up at step 5.

That inversion is where the coverage check in step 5 earns its place: a design
section the sweep missed has no issue at all, and a gap like that is invisible
from the issue list.

---

## 9. Asking for it — copy-paste openers

### Ask the architect

```
Act as the architect. Read issue #512 and settle the design.
```

```
Act as the architect. Review PR #520 against the design at
Documentation/Design/Ui.md §UI7 and report structural findings only.
```

### Ask the analyst

```
Act as the analyst. Write acceptance criteria into issue #512.
```

```
Act as the analyst in sweep mode. Find design sections tagged (needs issue)
and propose issues for them.
```

```
Act as the analyst. Issue #512 looks too big — check its size and split it if it
needs splitting, before writing criteria.
```

### Ask the developer

**Set the session model and effort to the issue's `Model - Effort` label first.**
Nothing does this for you.

```
Act as the developer. Implement issue #512. It carries `status: ready-for-dev`.
```

```
Act as the developer. Address the QA findings on PR #520. The criteria are on
issue #512.
```

### Ask QA

```
Act as QA. Verify PR #520 against the acceptance criteria on issue #512.
```

```
Act as QA. PR #520 has had a round of fixes since your last pass. Re-verify.
```

```
Act as QA, reviewing the issues rather than a change. Issue #512's design is at
Documentation/Design/Ui.md §UI7. There is no code yet — do not look for any.
```

That last one is QA's second mode. Name it explicitly — verifying a diff is the
default. The checklist is in `.claude/agents/qa.md`; §8 step 5 has the reasoning.

---

## 10. Conventions that bite

**Branches.** `users/<your-github-handle>/<category>-<entity>-<action>`, lowercase
after the handle. Claude Code worktree sessions generate `claude/<slug>-<hex>`
branches of their own; those are accepted in practice and several merged PRs came
from them. Never commit to `main`. Contributors do **not** fork — everything is
an in-repo branch, whatever the vendored branching skill says.

**Commits.** TDD work commits as `{TestName} -> FAIL` then `{TestName} -> PASS`.
Everything else is `CATEGORY: Pascal Case Description`.

**PR titles.** Same `CATEGORY:` prefix. The authoritative list of prefixes is in
`.github/workflows/prLinter.yml` — not in any skill, which carries a shorter and
differently-spelled list. Common ones: `FOUNDATIONS:`, `PROCESSINGS:`,
`ORCHESTRATIONS:`, `COMPONENTS:`, `CONTROLLERS:`, `DOCUMENTATION:`, `DESIGN:`,
`CONFIG:`, `CODE RUB:`, `MINOR FIX:`, `MEDIUM FIX:`, `MAJOR FIX:`. There is no
bare `FIX:`.

Be aware of what is actually enforced: **an unrecognised prefix does not fail the
build, it is silently left unlabelled.** The job matches the first prefix it
recognises and stops; a title matching none simply gets no label and no
complaint. (The job can still fail for its own reasons — the `addLabels` call is
not wrapped in a try/catch, so an API or permissions error would red it — but
never because of your title.) The convention is real; tooling will not catch you
breaking it.

**PR body.** Must link an issue or the PR linter fails:

```markdown
Closes #512
```

`fixes` and `resolves` (and their past-tense forms) and `AB#<n>` also match. This
is the one PR-linter job that can fail. `Build` is the only status check the
branch ruleset requires green.

**Issue labels.** Every issue carries a `Model - Effort` line as the first line of
the body and the matching label. The label set is not a tidy matrix — these
eleven exist, and nothing else:

| Model | Efforts available |
| --- | --- |
| Opus 5 | Small, Medium, High, Extra, Max |
| Sonnet 5 | Low, Medium, High |
| Fable 5 | Low, Medium, High |

There is no `Opus 5 - Low`, no `Sonnet 5 - Max`, no `Fable 5 - Extra`.

**Never add AI or assistant attribution** to a commit message or PR description.
It trips the unattributed-changes rule and blocks the merge.

**Skills** live in `.claude/skills/` and are vendored from upstream via
`skills-lock.json`. Treat them as read-only and reference them by name. In
practice a few have been edited locally; that is drift, not licence — if a skill
is wrong for this repo, say so in the design rather than patching the skill
quietly.

---

## 11. Commands

```powershell
dotnet build

# one suite
dotnet test Glory2Him.Core.Tests.Unit

# all of a kind
Get-ChildItem -Filter "*Tests.Unit*.csproj" -Recurse | % { dotnet test $_.FullName }

# React, from the app's own directory
npm run lint; npm run test; npm run build

# republish the branch to local IIS
D:\Sites\Deploy-Glory2HimWebApp.ps1
```

These are PowerShell — `Get-ChildItem` and the `.ps1` path assume it, and `&&`
is not a valid statement separator in Windows PowerShell 5.1, so the React line
uses `;`.

`.github/workflows/build.yml` is authoritative for what CI runs. It discovers
every `*Tests.Unit*`, `*Tests.Acceptance*` and `*Tests.Integration*` project
recursively, so a test project outside `Glory2Him.Core.Tests.*` still runs.

---

## 12. What does not exist yet

Stated plainly so nobody goes looking:

- **The `status:` labels are not created yet.** The lifecycle in §7 is the process
  issue #498 introduces; the labels must be created before it is real.
- **The `design: <area>` labels are not created yet** either. Note the trap: an
  all-caps `DESIGN` label exists, auto-created by the PR linter from a `DESIGN:`
  title prefix. It is a category label on PRs, not an area label on issues.
- **`Documentation/Design/` holds only `Events.md`.** The architecture, domain,
  security and UI documents are issue #481, still open. Their prefixes are
  reserved; their filenames are not decided, so this document uses
  `Design/Ui.md` illustratively rather than authoritatively.
- **`Documentation/Design/` has no index.** #481 calls for one.
- **`Documentation/Mockups/` is introduced by this document.** The two existing
  precedents are `Documentation/Images/ContentItemSearchPanel/` and issue #398.
- **Nothing validates design citations**, and nothing reads the `Model - Effort`
  label to configure a session.
- **`Documentation/Prompt-CreateFoundationService.md`** predates the agents and is
  referenced by nothing. Treat the four-agent workflow as current.
