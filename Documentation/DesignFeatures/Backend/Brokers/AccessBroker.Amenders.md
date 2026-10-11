# Access broker — the amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`), the policy gatherer of §APR8.6.1 rule 3. The Likes feature's work on the same broker is `Backend/Brokers/AccessBroker.md`.
Inherits: §APR8.6.1 rules 3 and 4, §APR8.6.3 rules 1, 5 and 7, §ARC12.2.1 rules 3–6, §ARC16.7.4 (*One gather, three rules*), §APR9.7.4

`AccessBroker` gathers and does not decide; its gathers are unit-tested, which is where this solution tests them (`AccessBrokerTests.*.Logic.cs`). Every reader of the amender record asks it here (§APR8.6.3 rule 7), never a caller-facing read. The record is read off the entity's own row, by the same per-`EntityType` read of the row behind an approval that already finds the entity's author (`ResolveEntityAsync`): a row's record answers for all of its text, a fork's inherited part included (§APR8.6.3 rule 5), so no reader reads a version group. An entity type that carries no record — `Association`, `Attachment` (§APR8.6.3 rule 2) — and a row that cannot be read answer an empty record, as such a row answers a blank author. Blank ids are dropped and each id is given once. The arms here read rows by id or through a query-shaping function they author (§ARC12.2.1 rule 3), and copy none of the arms that still run their terminal operator in memory.

## 1. MayRecordApprovalReviewAsync — the record for HR-5 (#1012)

The review gate's gather hands the decision the record of the entity behind the approval as `RecordReviewRequest.EntityAmenderUserIds` (`Backend/Clients/AccessClient.md §1`), beside the author it already hands as `EntityCreatedBy`. Nothing else about the gather changes.

## 2. RetrieveApprovalReviewerScopeByEntityAsync — the record on the reviewer scope (#1013)

`ApprovalReviewerScope` gains `EntityAmenderUserIds`, `required`, beside `EntityCreatedBy`, so the reviewer orchestration can refuse an invitation aimed at an amender and leave amenders out of the candidates (`Backend/Orchestrations/ApprovalReviewerOrchestrationService.md §1 and §2`). The entity-keyed form is the one the orchestration calls as built (§ARC12.5.4 business rule 2).

## 3. RetrieveApprovalReviewerScopeByIdAsync — the same record, found by the round's id (#1014)

The by-id form carries the same `EntityAmenderUserIds`, read the same way, so that the orchestration finds it when it moves to the round's id (§ARC12.5.4 business rule 2, #699).

## 4. FindAmenderApprovalReviewIdsAsync — an amender's standing review (#1015)

```csharp
ValueTask<List<Guid>> FindAmenderApprovalReviewIdsAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

1. **It returns the ids of the round's active reviews whose author the entity's record names** — the approval's reviews that are not soft deleted and not `Dismissed`, as `FindDismissableApprovalReviewIdsAsync` defines active, whose `CreatedBy` the record holds (§APR8.6.3 rule 7.4).
2. **It is unfiltered by the caller**, for the reason `FindDismissableApprovalReviewIdsAsync` records: the flow that reads it runs as whoever made the change, who may see none of the round's reviews, and an identity-filtered read never decides an invariant (§APR9.7.4).
3. **The reviews are read through `SelectApprovalReviewsAsync`** (`Backend/Brokers/StorageBroker.md §5`), with a condition this broker authors, so the matching runs in SQL.
4. **An empty record, or an id no approval carries, answers an empty list.**

Its caller is the Modified flow (`Backend/Orchestrations/ApprovalOrchestrationService.Amenders.md §1`).
