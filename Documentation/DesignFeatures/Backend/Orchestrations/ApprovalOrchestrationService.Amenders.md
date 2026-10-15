# Approval orchestration service — the amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: orchestration — `IApprovalOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Approvals/`). The Likes feature's work on the same service is `Backend/Orchestrations/ApprovalOrchestrationService.md`.
Inherits: §APR8.6 HR-5 and residual 1, §APR8.6.3 rule 7, §APR8.8, §APR9.7.4, §APR7.7 rule 7, §ARC12.5.3 business rule 12, §EVN20 rule 4

A review its author filed before amending the content vouched for text they have since changed, and HR-5 would refuse it now; while `RequireReapprovalOnChange` is off nothing dismissed it (§APR8.6 residual 1). The Modified flow dismisses it whatever that setting says (§APR8.6.3 rule 7.4). This adds no dependency and no subscription: the flow already dismisses through `IApprovalReviewWorkflowService` and gathers through `IAccessBroker` (§ARC16.7.1).

## 1. ProcessEntityModifiedAsync — an amender's standing review is dismissed (#1016)

```csharp
ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

The Modified flow, run by every `-Modified` and `-Submitted` ear and by `Association-Repointed`'s. Its body is the one every ear runs, so the rule reaches every ear by construction. If #699's stage 2 has made the method private when this is built, its tests are written against the ears as #699 rules (§ARC16.7).

1. **On every run, every active review whose author the entity's amender record names is dismissed, whatever `RequireReapprovalOnChange` says**, through `IApprovalReviewWorkflowService.DismissStaleApprovalReviewAsync` under the system identity, as every dismissal is (§APR7.7 rule 7). The reviews are found through `IAccessBroker.FindAmenderApprovalReviewIdsAsync` (`Backend/Brokers/AccessBroker.Amenders.md §4`), never a caller-facing read.
2. **The dismissal comes first**, once the round is resolved: before the round follows the entity, and before every other write the flow makes, so neither the evaluation nor a re-test that runs inside the flow can approve on a review it dismisses — on the path that dismisses under the setting and on the path that does not. A re-test runs inside the flow wherever one of the flow's writes publishes a fact the round re-tests, as Berean's assignment's facts do once their ears are wired (`ApprovalOrchestrationService.BereanReview.md`, *Order in the Modified flow*). *(This rule used to place the dismissal anywhere before the evaluation.)*
3. **Dismissing an amender's review does nothing to Berean of its own** (user rulings 2026-10-13 and 2026-10-14, `DesignFeatures/BereanReview.md` rule 9). The data change it comes with clears the item's score and Berean's review, under either `RequireReapprovalOnChange`, and that is the flow's own rule (`ApprovalOrchestrationService.BereanReview.md §2`), which this one neither adds to nor takes from. *(This rule used to return the assignment to pending, as every path that dismissed did under the ruling of 2026-10-05.)*
4. **It changes nothing else the flow does.** The setting's own dismissal and a changed reaction's return and dismissal stay as they are (§APR9.7.4), and each review is dismissed once, however many of the flow's rules name it.
5. **A redelivered fact finds nothing left to dismiss**, because HR-5 refuses an amender every new review (§EVN20 rule 4).
