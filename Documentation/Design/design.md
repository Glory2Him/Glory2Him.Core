# Design

How this system is built: layer placement, entity boundaries, event contracts,
the security boundary, storage and migration shape. `INTENT.md` says what the
system is for; the design says how it is put together.

**The design is authoritative.** An issue that disagrees with it is stale intent,
not an instruction — correct the issue.

This file is the index. It holds no design of its own: it says where each part of
the design lives, and the conventions every design document follows. The planner
writes the design. Nobody else does.

---

## How the work breaks down

| Level | What it is | Example | Where it lives |
| --- | --- | --- | --- |
| **Epic** | the whole product | Student Portal | `INTENT.md`, and the global documents below |
| **Feature** | something that ships and works on its own | Student registration | a feature document, `DesignFeatures/<Feature>.md` |
| **Sub-feature** | a part of a feature too large to plan in one go, coherent on its own | Password reset, inside Account management | a sub-feature document, `DesignFeatures/<SubFeature>.md`, naming its parent feature |
| **User story** | one component at one level — it can do something, but need not work on its own | the Student foundation service; a master page | a user story document, `DesignFeatures/Backend/<Story>.md` or `DesignFeatures/UI/<Story>.md`, naming its parent feature or sub-feature |
| **Task** | one operation of a user story | `IStudentService.AddStudentAsync` | a GitHub issue, naming its parent user story |

A feature is built by several user stories, usually at several levels — a storage
broker, a foundation service, a controller, a page. A user story never spans
levels, so it is always either backend or UI. Its operations are its tasks, and
each task is one GitHub issue carrying its own sign-off checklist.

A feature can span several screens, and each screen is its own user story. A user
portal is one feature: its master page and its detail page are two UI user
stories, and the operations they offer between them — add a user, search for a
user, view, modify and remove one — are their tasks.

**Every level names its parent.** A sub-feature document names its feature and a
user story document names its feature or sub-feature, on a `Parent:` line — the
`Backend/` and `UI/` folders do not show it. A task names its user story, on its
`User story:` line. Each parent lists its children in turn.

Writing or changing design documents is work too, tracked as a **design task** — a
`DESIGN:` issue.

---

## Map

### Epic — the rules every feature inherits

| Document | Prefix | Governs |
| --- | --- | --- |
| [G2H Design.md](<../G2H Design.md>) | `IDX` | the design overview and principles, the numbering and citation rules (§IDX1.5), and the map from every pre-split section number to the file it lives in now |
| [Architecture.md](Architecture.md) | `ARC` | layers, services and the dependencies between them |
| [Security.md](Security.md) | `SEC` | authentication, authorisation, identity on the signed envelope |
| [Events.md](Events.md) | `EVN` | event contracts, and how a service implements eventing |
| [Domain.md](Domain.md) | `DOM` | entities, their invariants and their relationships |

### Areas designed before feature documents

| Document | Prefix | Governs |
| --- | --- | --- |
| [Approval.md](Approval.md) | `APR` | the approval process |
| [UI.md](UI.md) | `UI` | the user interface and user experience |

These two hold feature-level design in the area layout, because they were written
before features had documents of their own. A new feature gets a feature document.

### Features

*None yet.* Add each feature as its document is written, with its sub-features
and user stories beside it:

| Feature | Sub-features | User stories |
| --- | --- | --- |

---

## Conventions

**Epic, feature or user story — decide before writing.** A rule every feature must
follow — how a service implements eventing, how identity travels, how a user
authenticates — belongs to the epic, in the global document that owns its area.
What one feature does belongs in its feature document, and what one component does
in its user story document. A global document stays global: the screens a security
rule needs, such as sign-in or 2FA, are UI user stories that cite `Security.md`,
not UI written into it.

**Inherit, never duplicate.** Feature and user story documents inherit every global
rule, and a user story inherits its feature's business rules. They cite the rule —
`per §EVN2`, `per StudentRegistration.md rule 3` — and never restate it: a copy is
a second statement that will drift from the first.

**Record every deviation where it happens.** A feature or user story that must
depart from a global rule says so in its document's **Deviations** section: the
rule it departs from, the reason, and how it is done instead. The planner proposes
a deviation and the user's approval grants it. Another document's deviation is
never the reason for one.

**Numbering.** The epic and area documents follow the rules in §IDX1.5: a section
keeps its number and gains its file's prefix — `§ARC12.5`, `§EVN2` — so a bare
citation stays unambiguous across files. A feature document numbers its business
rules, and a user story document numbers its operation sections; both are cited
with their path under `DesignFeatures/`. Number rules within a section too, so a
citation can say which one it means:

```csharp
// design §EVN2 rule 4: the subject is the service, not the entity
// design StudentRegistration.md rule 3
// design Backend/StudentService.md §1
```

**Every operation section in a user story document carries exactly one tag,
never bare:**

```markdown
## 1. AddStudentAsync (#512)
## 2. RetrieveStudentByIdAsync (needs issue)
```

`(#N)` names the task — the GitHub issue — that delivers the operation.
`(needs issue)` is an explicit, greppable flag for an operation nobody has
scheduled yet, and is what the planner's sweep mode looks for:

```bash
grep -rnE --include=*.md "^#{2,3} .*\(needs issue\)" Documentation/DesignFeatures
```

The tag is mandatory rather than inferred, because a bare heading is ambiguous —
deliberately skipped, or just missed? Requiring the tag forces the decision every
time an operation is touched.

**Relocations.** Annotate every relocated section with `(formerly §X)` so code
comments citing the old number still resolve by grep; §IDX1.5 has the full rules,
including where a retired number may still stand. Nothing in CI validates
citations. `Tools/design-split-audit.sh` checks them when someone runs it by
hand; otherwise the annotation convention is the whole guarantee.

**Keep the map current.** A document this index does not list is one nobody
finds.

`DEVELOPERS.md` §1, §4 and §6 are the long version of all of this.
