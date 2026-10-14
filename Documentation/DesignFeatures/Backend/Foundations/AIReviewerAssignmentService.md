# AI reviewer assignment service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IAIReviewerAssignmentService` and its workflow seam `IAIReviewerAssignmentWorkflowService` (`Glory2Him.Core/Services/Foundations/AIReviewerAssignments/`)
Inherits: §APR8.6.2, *Assignment*, §APR8.6.2.1, *The write is a seam verb*, §ARC16.7.1, §SEC14.6.1, BereanReview.md rules 4, 8, 9, 18, 19 and 21

Berean's assignment is its review: it records where the review stands (BereanReview.md rule 21). Every write the workflow makes on it is the system's own and goes through the seam, which mints the system identity itself, because the caller-facing gate asks for a review-tier role and the system holds none (§APR8.6.2.1, *The write is a seam verb*). After this story the seam carries three verbs for the approval process: the automatic add, the dismissal and the return to pending, which clears the review's tracking fields. The writers of the review's start and completion are the review process's, designed under #1216. The caller-facing modify, which nothing calls once the re-request is gone (BereanReview.md rule 4), leaves the service.

## 1. CompleteAIReviewerAssignmentAsync, moved to #1216

Retired from this design. It marked a live, pending assignment completed for the write-back (#1159, closed). The review process now writes both the start and the completion (BereanReview.md rule 21), and #1216 designs their verbs with it. The number is kept so that §2–§6 keep theirs.

## 2. DismissStaleAIReviewerAssignmentAsync (#1160)

```csharp
ValueTask<AIReviewerAssignment> DismissStaleAIReviewerAssignmentAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

1. **It soft-deletes a live assignment, started or not and completed or not**, under the system identity, with the `DeletionReason` *"Dismissed: the content changed."* — so a reader can tell it apart from a withdrawal, whose `DeletedBy` names the person (BereanReview.md rule 9, step 1). `DeletedBy` names the system.
2. **In the same write it clears the assignment's five tracking fields** — `IsAIReviewStarted` and `IsAIReviewCompleted` to `false`, `AIReviewQueuedAt` and `AIReviewCompletedAt` to null, and `IsAIReviewCommentsPresent` to `false` (BereanReview.md rule 9, step 3).
3. **On an assignment already withdrawn or dismissed, it writes nothing** and answers the stored row. A missing id is not found.
4. It publishes `AIReviewerAssignment-Removed`, as the withdrawal does, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §6`).

## 3. OnModifyingAIReviewerAssignmentAsync, retired (#1164)

The `AIReviewerAssignment-Modifying` door goes: the address, its subscription, its accepted name and the handler. Nothing publishes to it, and once §4's verb goes it has nothing to reach. Its tests go with it.

## 4. ModifyAIReviewerAssignmentAsync, retired (#1165)

The caller-facing modify goes from `IAIReviewerAssignmentService`, with its validation, once nothing calls it: its only caller is the re-request, which `Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §1` removes. The two flags then have no caller-facing writer, which is what §APR8.6.2 calls them, *"system-only"*. The comment over `ValidateAgainstStorageAIReviewerAssignmentOnModify`, *"Only the two system-facing bools and the audit trail may change on a modify"*, goes with it.

## 5. ReturnStaleAIReviewerAssignmentToPendingAsync — every tracking field cleared (#1168)

```csharp
ValueTask<AIReviewerAssignment> ReturnStaleAIReviewerAssignmentToPendingAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

The seam's return to pending stays, as the approval process's step 3 under `RequireReapprovalOnChange = false`, where Berean stays assigned and its review starts again on the changed data (BereanReview.md rule 9, steps 1 and 3; I3.4, I4.4).

1. **It clears all five tracking fields of a live assignment**, under the system identity: `IsAIReviewStarted`, `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` to `false`, and `AIReviewQueuedAt` and `AIReviewCompletedAt` to null. Today it clears the two flags of a completed assignment alone.
2. **It acts on any live assignment with one of them set**: one started and not yet completed as well as one completed. An assignment with none set, or one withdrawn or dismissed, is answered as stored, and nothing is written or published.
3. It publishes `AIReviewerAssignment-Modified`, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §5`).
4. **Its callers change**: the reset stops calling it (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1`), and the Modified flow calls it under `RequireReapprovalOnChange = false` alone (§2), where today it calls it under `true` on every dismissing `-Modified`. The seam's summary is rewritten to say what its three verbs are for: the automatic add, the dismissal and this clear. Its summary of this verb stops saying that *"only the two flags recording what its pass reported go back"*.

## 6. AddAIReviewerAssignmentAsync — the tracking fields refused (needs issue)

```csharp
ValueTask<AIReviewerAssignment> AddAIReviewerAssignmentAsync(
    AIReviewerAssignment aiReviewerAssignment,
    CancellationToken cancellationToken = default);
```

1. **An add that carries `IsAIReviewStarted` `true`, or either of `AIReviewQueuedAt` and `AIReviewCompletedAt` set, is refused**, with a validation failure naming each field, as the add already refuses `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` set (`IsSetOnAdd`): an assignment is born pending, and a row claiming a review that has started or finished claims work that never ran (BereanReview.md rule 21).
2. It is asked on the direct path and on the `AIReviewerAssignment-Adding` door alike, because both converge on the add's one validation. The automatic add (`AddAutomaticAIReviewerAssignmentAsync`) mints its row itself, with all five fields empty.
3. **It builds on `Backend/Models/AIReviewerAssignment.md §1`**, which gives the model the three fields, and lands before §2 there maps them.
