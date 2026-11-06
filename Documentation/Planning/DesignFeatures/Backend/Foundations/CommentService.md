# Comment service
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: foundation — `ICommentService` (`Glory2Him.Core/Services/Foundations/Comments/`)
Inherits: §APR8.6.3 rules 3 and 4, §APR9.7.1 rules 1 and 2, §ARC12.3.1 shared rules 1 and 2, §SEC14.7 posture A rule 6

`Comment` carries the amender record (§APR8.6.3 rule 2), and this service is where it is derived: the add writes it and the general modify extends it, each from storage and the signed caller, never from the caller's copy (§SEC14.7 posture A rule 6). `Comment` is Single-Row (§APR7.5.1), so no row of it joins a version group and nothing is inherited. Both operations change a write path that already exists; their event doors, `OnAddingCommentAsync` and `OnModifyingCommentAsync`, run the same `DoAddCommentAsync` and `DoModifyCommentAsync`, so the rules reach them by construction (ts-foundations-013) and are proven on the direct path. The property is not stored until `Backend/Models/IAmendment.md §2` (§APR8.6.3 rule 10), so these rules are proven on what the service hands storage.

## 1. AddCommentAsync — a new row records nobody (#1005)

1. **The row is written with an empty record**, whatever the caller's copy carries (§APR8.6.3 rule 3.1). Its creator is its owner, whom HR-1 already refuses.

## 2. ModifyCommentAsync — whoever changes the content is recorded (#1006)

1. **The written row carries the stored record, extended as §APR8.6.3 rules 3 and 4 have it** — what changes content, how a field is compared, whose id is appended and what is never recorded are theirs, and the caller's copy is ignored.
2. **Its content field is `Content`**, this entity's fourth group of §APR9.7.1 rule 2's subtraction test, which rule 3 compares.
