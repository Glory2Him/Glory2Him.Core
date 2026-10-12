# Content item service — the amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`). The Likes feature's work on the same service is `Backend/Foundations/ContentItemService.md`, and the live updates feature's `Backend/Foundations/ContentItemService.LiveUpdates.md` (`design.md`, *Conventions*).
Inherits: §APR8.6.3 rules 3–5, §APR9.7.1 rules 1 and 2, §DOM3.4 rule 8, §SEC14.7 posture A rule 6, §ARC12.2.1 rules 3 and 5

`ContentItem` carries the amender record (§APR8.6.3 rule 2), and this service is where it is derived: the add writes it and the general modify extends it, each from storage and the signed caller, never from the caller's copy (§SEC14.7 posture A rule 6). Both operations change a write path that already exists; their event doors, `OnAddingContentItemAsync` and `OnModifyingContentItemAsync`, run the same `DoAddContentItemAsync` and `DoModifyContentItemAsync`, so the rules reach them by construction (ts-foundations-013) and are proven on the direct path. The property is not stored until `Backend/Models/IAmendment.md §2` (§APR8.6.3 rule 10), so these rules are proven on what the service hands storage.

## 1. AddContentItemAsync — the record a new row starts with (#997)

1. **A row that starts a new group is written with an empty record**, whatever the caller's copy carries (§APR8.6.3 rule 3.1).
2. **A row that joins an existing group — a version fork — is written with the union of the records of every row the group already holds, soft-deleted rows included**, each id once, and nothing from the caller's copy (§APR8.6.3 rule 5).
3. **The group's rows are read through `SelectContentItemsAsync`** (`Backend/Brokers/StorageBroker.md §2`), whose condition this service authors — every row whose `GroupId` is the new row's, deleted or not — and which returns whole rows. It never projects the record, so it runs the same before the column exists and after (§APR8.6.3 rule 10.2).

## 2. ModifyContentItemAsync — whoever changes the content is recorded (#998)

1. **The written row carries the stored record, extended as §APR8.6.3 rules 3 and 4 have it** — what changes content, how a field is compared, whose id is appended and what is never recorded are theirs, and the caller's copy is ignored.
2. **Its content fields are `Title`, `Author`, `Content`, `ShareabilityBasis` and `SharePermission`**, this entity's fourth group of §APR9.7.1 rule 2's subtraction test, which rule 3 compares.
