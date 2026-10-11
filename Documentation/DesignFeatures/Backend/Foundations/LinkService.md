# Link service
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: foundation — `ILinkService` (`Glory2Him.Core/Services/Foundations/Links/`)
Inherits: §APR8.6.3 rules 3–5, §APR9.7.1 rules 1 and 2, §DOM3.4 rule 8, §SEC14.7 posture A rule 6, §ARC12.2.1 rules 3 and 5

`Link` carries the amender record (§APR8.6.3 rule 2), and this service is where it is derived: the add writes it and the general modify extends it, each from storage and the signed caller, never from the caller's copy (§SEC14.7 posture A rule 6). Both operations change a write path that already exists; their event doors, `OnAddingLinkAsync` and `OnModifyingLinkAsync`, run the same `DoAddLinkAsync` and `DoModifyLinkAsync`, so the rules reach them by construction (ts-foundations-013) and are proven on the direct path. The property is not stored until `Backend/Models/IAmendment.md §2` (§APR8.6.3 rule 10), so these rules are proven on what the service hands storage.

## 1. AddLinkAsync — the record a new row starts with (#999)

1. **A row that starts a new group is written with an empty record**, whatever the caller's copy carries (§APR8.6.3 rule 3.1).
2. **A row that joins an existing group — a version fork — is written with the union of the records of every row the group already holds, soft-deleted rows included**, each id once, and nothing from the caller's copy (§APR8.6.3 rule 5).
3. **The group's rows are read through `SelectLinksAsync`** (`Backend/Brokers/StorageBroker.Amenders.md §1`), whose condition this service authors — every row whose `GroupId` is the new row's, deleted or not — and which returns whole rows. It never projects the record, so it runs the same before the column exists and after (§APR8.6.3 rule 10.2).

## 2. ModifyLinkAsync — whoever changes the content is recorded (#1000)

1. **The written row carries the stored record**, whatever the caller's copy carries; the caller's value is neither compared nor refused (§APR8.6.3 rule 4).
2. **The caller's id is appended when the write changes content** — when `Name`, `Url` or `LinkType` differs from the stored row's, compared ordinally — and the id is not already recorded (§APR8.6.3 rule 3). That holds whether or not the same write moves the status between `Draft` and `Submitted` (§APR9.2 rules 4–6).
3. **A write that changes no content field records nobody**, the carve-out's status move alone included.
4. **The id is the one that stamps `UpdatedBy`**, from the envelope's signed security context through `ISecurityAuditBroker.GetUserIdAsync`; a blank id is never recorded.
