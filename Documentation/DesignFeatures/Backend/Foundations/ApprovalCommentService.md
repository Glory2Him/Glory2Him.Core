# Approval comment service — Berean's comments
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — a new workflow seam, `IApprovalCommentWorkflowService`, on the `ApprovalCommentService` class (`Glory2Him.Core/Services/Foundations/ApprovalComments/`)
Inherits: §APR7.8, §APR8.5 rule 7, §ARC16.7.1, §SEC14.6.1, §SEC14.7 posture D, BereanReview.md rules 6 and 19

A run may file comments on the round (BereanReview.md rule 6). The caller-facing add cannot serve it: its gate asks the review tier, and the system identity holds no role. So the comment goes through a seam that mints the system identity itself, the shape §ARC16.7.1 gives every write the workflow makes on nobody's behalf. The seam is `internal`, as the other workflow seams are, so an exposer cannot bind to it.

## 1. AddAIReviewerApprovalCommentAsync (needs issue)

```csharp
ValueTask<ApprovalComment> AddAIReviewerApprovalCommentAsync(
    ApprovalComment approvalComment,
    CancellationToken cancellationToken = default);
```

1. **It adds a comment on the round its `ApprovalId` names, under the system identity**, so `CreatedBy` names the system and the thread shows it as Berean's (§SEC14.6.1).
2. **A `Question` is created outstanding, and a `Comment` is created settled** (§APR7.8). An ask born settled is refused, as on the caller-facing add (`AccessDenialReason.SettledAskNotPermitted`).
3. **The round must be `Submitted`.** A comment on a round that is decided, missing or soft deleted is refused, read from the round through the gather the comment gates already read it from. It asks no review tier: the seam's caller is the workflow.
4. **The shape is the caller-facing add's**: the text required and within its length, the id minted, the audit stamped by the seam.
5. It publishes `ApprovalComment-Added`, which re-tests the round (§EVN18(a)).
