# AI reviewer assignment service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IAIReviewerAssignmentService` and its workflow seam `IAIReviewerAssignmentWorkflowService` (`Glory2Him.Core/Services/Foundations/AIReviewerAssignments/`)
Inherits: §APR8.6.2, *Assignment*, §APR8.6.2.1, *The write is a seam verb*, §ARC16.7.1, §SEC14.6.1, BereanReview.md rules 4, 8, 9, 16–18

Berean's assignment is its review: pending until a run completes it (BereanReview.md rule 8). Every write the workflow makes on it is the system's own and goes through the seam, which mints the system identity itself, because the caller-facing gate asks for a review-tier role and the system holds none (§APR8.6.2.1, *The write is a seam verb*). After this story the seam carries three verbs: the automatic add, the completion and the dismissal. The return to pending, which the dismissal replaces (BereanReview.md rule 9), and the caller-facing modify, which nothing calls once the re-request is gone (rule 4), leave the service.

## 1. CompleteAIReviewerAssignmentAsync (#1159)

```csharp
ValueTask<AIReviewerAssignment> CompleteAIReviewerAssignmentAsync(
    Guid aiReviewerAssignmentId,
    bool isAIReviewCommentsPresent,
    CancellationToken cancellationToken = default);
```

1. **It marks a live, pending assignment completed**: `IsAIReviewCompleted = true`, and `IsAIReviewCommentsPresent` as the caller says, under the system identity the seam mints (§SEC14.6.1). `UpdatedBy` names the system.
2. **On an assignment that is already completed, or withdrawn or dismissed, it writes nothing**, publishes nothing and answers the stored row, so a redelivered result completes nothing twice (BereanReview.md rule 17). A missing id is not found.
3. It publishes `AIReviewerAssignment-Modified`, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §5`).

## 2. DismissStaleAIReviewerAssignmentAsync (#1160)

```csharp
ValueTask<AIReviewerAssignment> DismissStaleAIReviewerAssignmentAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

1. **It soft-deletes a live assignment, pending or completed**, under the system identity, with the `DeletionReason` *"Dismissed: the content changed."* — so a reader can tell it apart from a withdrawal, whose `DeletedBy` names the person (BereanReview.md rule 9). `DeletedBy` names the system.
2. **On an assignment already withdrawn or dismissed, it writes nothing** and answers the stored row. A missing id is not found.
3. It publishes `AIReviewerAssignment-Removed`, as the withdrawal does, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §6`).

## 3. OnModifyingAIReviewerAssignmentAsync, retired (#1164)

The `AIReviewerAssignment-Modifying` door goes: the address, its subscription, its accepted name and the handler. Nothing publishes to it, and once §4's verb goes it has nothing to reach. Its tests go with it.

## 4. ModifyAIReviewerAssignmentAsync, retired (#1165)

The caller-facing modify goes from `IAIReviewerAssignmentService`, with its validation, once nothing calls it: its only caller is the re-request, which `Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §1` removes. The two flags then have no caller-facing writer, which is what §APR8.6.2 calls them, *"system-only"*. The comment over `ValidateAgainstStorageAIReviewerAssignmentOnModify`, *"Only the two system-facing bools and the audit trail may change on a modify"*, goes with it.

## 5. ReturnStaleAIReviewerAssignmentToPendingAsync, retired (#1168)

The seam's return to pending goes once neither the reset nor the Modified flow calls it (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1–§2`). The seam's summary is rewritten to say what its three verbs are for: the automatic add, the completion and the dismissal.
