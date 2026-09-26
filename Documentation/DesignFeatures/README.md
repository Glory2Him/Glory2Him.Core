# Feature, sub-feature and user story documents

What each feature does and how it is built, written by the planner. The epic — the
rules every feature inherits — lives in `Documentation/Design/`, and
[design.md](../Design/design.md) is the index to all of it: it defines epic,
feature, sub-feature, user story and task, and holds the conventions.

## Layout

```
Documentation/DesignFeatures/
  <Feature>.md           a feature, or a sub-feature naming its parent feature
  Backend/<Story>.md     a backend user story — one component at one level
  UI/<Story>.md          a UI user story — one component at one level
```

`Backend/` and `UI/` are created with their first user story.

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
- [Backend/StudentStorageBroker.md](Backend/StudentStorageBroker.md) — broker
- [Backend/StudentService.md](Backend/StudentService.md) — foundation
- [Backend/StudentsController.md](Backend/StudentsController.md) — exposer
- [UI/RegistrationPage.md](UI/RegistrationPage.md) — page
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
Parent: [StudentRegistration.md](../StudentRegistration.md)
Level: foundation — `IStudentService`
Inherits: §ARC3, §EVN2–§EVN7, StudentRegistration.md rules 1–4

## 1. AddStudentAsync (#512)
## 2. OnAddingStudentAsync (needs issue)
## Deviations
```

- **Parent** names the feature or sub-feature the user story belongs to — always,
  since the `Backend/` and `UI/` folders do not show it.
- One component at one level: a user story never spans levels.
- **One section per operation**, since each operation is one task. Each carries
  exactly one tag — the task that delivers it, or `(needs issue)` — and that task
  names this section as its parent on its `User story:` line.

List every new document in [design.md](../Design/design.md)'s map.
