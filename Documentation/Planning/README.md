# Planning

Yours: existing documentation that came before this workflow — requirement
documents, specifications, an older design, notes from a discovery phase. It stays
here once ported, so the pinned links that issues carry keep resolving.

**A document here is never a source.** Nothing is planned, built, reviewed or
documented from it. What it says counts only once it has been ported through an
issue form, so every feature and rule is logged the same way, with the same fields,
labels and approvals, whatever it started as:

- **A feature** it describes → the **Feature request** form, one issue per feature,
  with the document linked in the form's **Ported from** field.
- **A rule** every feature must follow, or **a term** → the Global Rules issue: its
  **Global rules** form if it has not been filed yet, otherwise a change to it.

`/grill-with-docs Documentation/Planning/<document>` ports one with you: it reads the
document, splits it into features, rules and terms, grills you on what it leaves open,
and drafts each feature request field by field, ready to paste into the form. Rules
and terms go through the Global Rules change and QA as any other. No other agent
opens this folder, with the one exception below.
[porting-planning-documents.md](../Procedures/porting-planning-documents.md) walks
through it step by step, and on to getting each feature built.

## Legacy

A repository made from the older template kept its design as files, and may hold
tasks already signed `ready for development` against them. Moving it onto this
workflow moves those files here, in one rename-only commit, keeping their sub-paths:

| Was | Now |
| --- | --- |
| `Documentation/Design/<X>` | `Documentation/Planning/Design/<X>` |
| `Documentation/DesignFeatures/<X>` | `Documentation/Planning/DesignFeatures/<X>` |
| any other loose design document `<name>` | `Documentation/Planning/<name>` |

Mockups stay in `Documentation/Mockups/`.

**In Glory2Him.Core** the move took `Documentation/Design/`, `Documentation/DesignFeatures/`
and `Documentation/G2H Design.md` (Glory2Him/Glory2Him.Core#1404). The images, the draw.io
model, the foundation-service prompt and `ModelBudget/` stayed where they were. This
repository has **no `legacy` tasks**: its open issues from the old process were closed
as superseded instead (Glory2Him/Glory2Him.Core#1381, decision C13), each closing comment
naming its document, where it has one, at commit `e764625a58029eb076c0490c2d1c92b14fcb2d00`, the last
before the move. The exception below never applies here; every document is ported
through `/grill-with-docs`.

**The one exception to "never a source".** A task labelled `legacy` — and only such a
task — is built and verified against the documents it cites, read at their new path.
Its body still says `Documentation/Design/…` or `Documentation/DesignFeatures/…`; the
developer and QA read it at `Documentation/Planning/Design/…` or
`Documentation/Planning/DesignFeatures/…`. The body is never rewritten to say so.

- Only a task signed `ready for development` before the switch carries `legacy`. It
  sits under a Feature issue and a User Story issue created for it, both labelled
  `legacy`. Each body links the old document it came from, where it has one, by a URL
  pinned to a 40-character commit SHA; a feature with no feature document says so.
- The planner never adds `legacy`, and no new task is written against a document
  here.
- A change to a legacy task's design is new work — `/grill-with-docs`, then
  `/plan-feature` — never an edit to the old document.
- Rules too large for one issue stay here, indexed by section under the Global Rules
  issue's **Legacy rules**. An unstruck line binds legacy features only. Each is settled
  through `/grill-with-docs` — `(ported)` under its own number, `(retired: <reason>)`,
  or `(feature: #<n>)` — and its line is struck through, never deleted.
- A service designed before the switch, whose deviation sat in an architecture
  document's register, gets a decision record under `Documentation/Decisions/`
  carrying `**Deviation:** <ClassName> — <the rule>`, so
  `scripts/list-deviations.sh` finds it.

[migrating-an-existing-repository.md](../Procedures/migrating-an-existing-repository.md)
walks through the move.

## Ported

Once a document is fully ported, add its row. A document with no row has not been
ported in full: only what has already been filed as an issue from it counts. An old
design document gets its row only when everything in it is in issues — its rules and
terms in the Global Rules issue, its features as feature issues, legacy or new.

| Document | Ported to | By | Date |
| --- | --- | --- | --- |
| | | | |

Link a document from an issue by a URL pinned to the full 40-character commit SHA,
never a branch, so what the issue points at cannot change under it:

```markdown
[Bookings specification](https://github.com/<owner>/<repo>/blob/<40-char-sha>/Documentation/Planning/bookings-spec.docx)
```
