# Tag service
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: foundation — `ITagService` (`Glory2Him.Core/Services/Foundations/Tags/`)
Inherits: §APR8.6.3 rules 3 and 4, §APR9.7.1 rules 1 and 2, §ARC12.3.1 shared rules 1 and 2, §SEC14.7 posture A rule 6

`Tag` carries the amender record (§APR8.6.3 rule 2), and this service is where it is derived: the add writes it and the general modify extends it, each from storage and the signed caller, never from the caller's copy (§SEC14.7 posture A rule 6). `Tag` is Single-Row (§APR7.5.1), so no row of it joins a version group and nothing is inherited. Both operations change a write path that already exists; their event doors, `OnAddingTagAsync` and `OnModifyingTagAsync`, run the same `DoAddTagAsync` and `DoModifyTagAsync`, so the rules reach them by construction (ts-foundations-013) and are proven on the direct path. The property is not stored until `Backend/Models/IAmendment.md §2` (§APR8.6.3 rule 10), so these rules are proven on what the service hands storage.

## 1. AddTagAsync — a new row records nobody (#1001)

1. **The row is written with an empty record**, whatever the caller's copy carries (§APR8.6.3 rule 3.1). Its creator is its owner, whom HR-1 already refuses.

## 2. ModifyTagAsync — whoever changes the content is recorded (#1002)

1. **The written row carries the stored record**, whatever the caller's copy carries; the caller's value is neither compared nor refused (§APR8.6.3 rule 4).
2. **The caller's id is appended when the write changes content** — when `Name` differs from the stored row's, compared ordinally — and the id is not already recorded (§APR8.6.3 rule 3). That holds whether or not the same write moves the status between `Draft` and `Submitted` (§APR9.2 rules 4–6).
3. **A write that changes no content field records nobody**, the carve-out's status move alone included.
4. **The id is the one that stamps `UpdatedBy`**, from the envelope's signed security context through `ISecurityAuditBroker.GetUserIdAsync`; a blank id is never recorded.
