# Approval comment service — Berean's comments
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — a new workflow seam, `IApprovalCommentWorkflowService`, on the `ApprovalCommentService` class (`Glory2Him.Core/Services/Foundations/ApprovalComments/`)
Inherits: §APR7.8, §APR8.5 rule 7, §ARC16.7.1, §SEC14.6.1, §SEC14.7 posture D, BereanReview.md rules 6 and 19

A run may file comments on the round (BereanReview.md rule 6). The caller-facing add cannot serve it: its gate asks the review tier, and the system identity holds no role. So the comment goes through a seam that mints the system identity itself, the shape §ARC16.7.1 gives every write the workflow makes on nobody's behalf. The seam is `internal`, as the other workflow seams are, so an exposer cannot bind to it.

## 1. AddAIReviewerApprovalCommentAsync (#1161)

```csharp
ValueTask<ApprovalComment> AddAIReviewerApprovalCommentAsync(
    ApprovalComment approvalComment,
    CancellationToken cancellationToken = default);
```

1. **It adds a comment on the round its `ApprovalId` names, under the system identity**, so `CreatedBy` names the system and the thread shows it as Berean's (§SEC14.6.1).
2. **A `Question` is created outstanding, and a `Comment` is created settled** (§APR7.8). The seam sets `IsResolved` from the type and takes none from its caller, so an ask born settled cannot arise.
3. **It gates on the system identity alone**, as `ApprovalReviewService`'s dismissal seam does (`ValidateDismissalIsTheWorkflowsOwnAct`): the seam mints the context itself, and the private body it delegates to refuses any context that is not the system's. It asks no review tier, because the system holds none.
4. **The shape is the caller-facing add's**: the round's id and the text required, the text within its length, the id minted, the audit stamped by the seam. The round's key refuses an id no round carries.
5. **Whether the round is still open is its caller's to ask**, because a single-entity foundation cannot read the round (§SEC14.3): the write-back reads it and files nothing on a round that is no longer `Submitted` (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §3`).
6. It publishes `ApprovalComment-Added`, which re-tests the round (§EVN18(a)).
