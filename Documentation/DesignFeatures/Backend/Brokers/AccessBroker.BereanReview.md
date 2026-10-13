# Access broker — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`). The Likes feature's work on the same broker is `Backend/Brokers/AccessBroker.md`, and the amender record's `Backend/Brokers/AccessBroker.Amenders.md`.
Inherits: §APR8.6.1 rules 2–4, §APR8.6.2.1, *The security boundary* rule 3, §ARC12.2.1, §ARC12.5, BereanReview.md rules 9–11, 17 and 20

The broker gathers what the decision function and the workflow need, unfiltered by identity, because each answer decides an invariant (§APR8.6.2.1, *The security boundary* rule 3). Its gathers are unit-tested, as every `AccessBroker` gather is. None adds a storage-broker member: the live assignment is read through the existing keyed `IStorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync`, which answers the round's one live row (`UX_AIReviewerAssignments_ApprovalId`), and each item through the read of its row the subject traversal already makes.

## 1. RetrieveLiveAIReviewerAssignmentAsync (#1157)

```csharp
ValueTask<AIReviewerAssignment?> RetrieveLiveAIReviewerAssignmentAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

1. **It answers the round's live Berean assignment, pending or completed, or `null` where there is none** — none ever, or one withdrawn or dismissed. The Modified flow dismisses what it answers (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`), and the write-back checks the run's assignment against it (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §3`).
2. **No identity filters it**: the Modified flow runs as whoever changed the content, who may see nothing on the round (§APR9.7.4), and the write-back as the system, which holds no role.

## 2. FindResettableAIReviewerAssignmentIdAsync, retired (#1169)

Once the reset and the Modified flow stop returning Berean to pending (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1–§2`), nothing calls the gather. It leaves `IAccessBroker` and `AccessBroker`, with its tests.

## 3. RetrieveApprovalSubjectConfidenceAsync (#1158)

```csharp
ValueTask<ApprovalSubjectConfidence?> RetrieveApprovalSubjectConfidenceAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

`ApprovalSubjectConfidence` is `(bool IsSubjectAvailable, decimal? ConfidenceScore)`, in `Models/Securities/`.

1. **It answers whether the round's item reads back, and not soft deleted, and that item's stored score**, read through the subject traversal for every approvable type with a row (BereanReview.md rule 2). `null` where no round carries the id.
2. **It reads the item unfiltered**, so a non-public item answers its own score, and an item that is gone answers `IsSubjectAvailable = false` — never a null score that could pass for "not yet scored" (BereanReview.md rule 20).
3. The start reads it to decide whether a run is owed (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §2`).

## 4. EvaluateApprovalConditionsByIdAsync — the item's score and Berean's assignment (#1180)

1. **The subject traversal reads every item type's stored score**, where today only the `Association` arm reads one and every other arm hands the decision `null` (`AccessBroker.cs`, the traversal's arms). `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` and `BibleReference` each hand their row's `ConfidenceScore`.
2. **`IsAIReviewerAssigned` and `IsAIReviewCompleted` are gathered** from the round's live assignment, read as §1 reads it: `true` and that row's `IsAIReviewCompleted` where one stands, and `false` and `false` where none does. They replace the `false` `Backend/Clients/AccessClient.BereanReview.md §5` passes.
3. **It waits for the confidence process to answer** (BereanReview.md, *What ships together* step 4): once it is merged, a round Berean is assigned to is held until a score lands.

## 5. MayDecideApprovalAsync — Berean's assignment (#1181)

1. **`IsAIReviewerAssigned` and `IsAIReviewCompleted` are gathered** for the round the query names, as §4 rule 2 gathers them, in place of the `false` `Backend/Clients/AccessClient.BereanReview.md §6` passes. The score is the foundation's stored one, which the query carries (each item's `Transition<Entity>ApprovalAsync`, `Backend/Foundations/<Entity>Service.BereanReview.md §5`).
2. It waits as §4 does.

## 6. MayDecideApprovalByIdAsync — the item's score and Berean's assignment (#1182)

The approval record's own decision door (§APR9.7.5, *Built on the `Approval` row*), which reads the item through the same traversal.

1. **The item's stored score is the traversal's**, for every item type, as §4 rule 1 reads it, and **`IsAIReviewerAssigned` and `IsAIReviewCompleted` are gathered** as §4 rule 2 gathers them.
2. It waits as §4 does.
