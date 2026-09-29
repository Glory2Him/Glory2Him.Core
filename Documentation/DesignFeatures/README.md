# Feature, sub-feature and user story documents

What each feature does and how it is built, written by the planner. The epic — the
rules every feature inherits — lives in `Documentation/Design/`, and
[design.md](../Design/design.md) is the index to all of it: it defines epic,
feature, sub-feature, user story and task, and holds the conventions.

## Layout

```
Documentation/DesignFeatures/
  <Feature>.md                  a feature, or a sub-feature naming its parent feature
  Backend/<Level>/<Story>.md    a backend user story — one component at one level
  UI/<Level>/<Story>.md         a UI user story — one component at one level
```

`<Level>` is the folder the story's level has in code — `Brokers`, `Foundations`,
`Orchestrations`, `Controllers`, `Hooks` and the rest — so a story is found where its
code is. [design.md](../Design/design.md) lists the folders, and each is created with
its first user story.

A presentation component's documents and a page's are not here: they live under
[`Documentation/Design/UI/Components/`](../Design/UI/Components/) (§UI20.6.4) and
[`Documentation/Design/UI/Pages/`](../Design/UI/Pages/) (§UI20.5.1), so `UI/` has no
`Components` or `Pages` folder.

## A feature document

```markdown
# Student registration
Epic: [INTENT.md](../../INTENT.md)
Inherits: §ARC3, §EVN2–§EVN7, §SEC1
Mockups: [student-registration](../Mockups/student-registration/)

## Problem
## Business rules
1. A student registers with an email address no other student has.
2. …
## User stories
- [Backend/Brokers/StudentStorageBroker.md](Backend/Brokers/StudentStorageBroker.md) — broker
- [Backend/Foundations/StudentService.md](Backend/Foundations/StudentService.md) — foundation
- [Backend/Controllers/StudentsController.md](Backend/Controllers/StudentsController.md) — exposer
- [UI/Pages/RegistrationPage.md](../Design/UI/Pages/RegistrationPage.md) — page, in its page document (§UI20.5.1)
## Entity count, events and storage
## Risks
## Out of scope
## Deviations
```

- **Business rules** are everything the feature must do, numbered — including
  every rule a mockup shows. "Matches the mockup" is not a rule.
- A feature too large to plan in one go lists **sub-features** instead of user
  stories. A sub-feature document has this same shape, with `Parent:` naming its
  feature in place of `Epic:`.
- No **Deviations** section means no deviations.

## A user story document

```markdown
# Student service
Parent: [StudentRegistration.md](../../StudentRegistration.md)
Level: foundation — `IStudentService`
Inherits: §ARC3, §EVN2–§EVN7, StudentRegistration.md rules 1–4

## 1. AddStudentAsync (#512)
## 2. OnAddingStudentAsync (needs issue)
## Deviations
```

- **Parent** names the feature or sub-feature the user story belongs to — always,
  since its folder shows its level and not its parent.
- One component at one level: a user story never spans levels.
- **One section per operation**, since each operation is one task. Each carries
  exactly one tag — the task that delivers it, or `(needs issue)` — and that task
  names this section as its parent on its `User story:` line.

List every new document in [design.md](../Design/design.md)'s map.
