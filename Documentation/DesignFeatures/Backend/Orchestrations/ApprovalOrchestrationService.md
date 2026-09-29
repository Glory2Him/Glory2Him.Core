# Approval orchestration service
Parent: [Likes.md](../../Likes.md)
Level: orchestration — `IApprovalOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Approvals/`)
Inherits: §APR7.5.1 rule 3, §APR8.8 regardless-rule 1, §APR9.7.4, §APR9.7.5, §APR9.8, §ARC16.2.2, §ARC16.7, §DOM4.5 rule 4, §EVN19 rule 4, §EVN20 rule 4, §SEC14.6 rule 4, Likes.md rule 9

A changed reaction goes back through the same approval process as any modification, in a round that starts with no reviews (§DOM4.5 rule 4, ruled on #685). §APR9.7.4 designs how, and §ARC16.2.2 names the subscription; neither is built. This user story is that one subscription and what the Modified flow does when it is the one that called.

## 1. OnAssociationRepointedAsync (#727)

```csharp
ValueTask<EventEnvelope<Association>?> OnAssociationRepointedAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **It is one more ear, not a new flow or a new dependency** (§ARC16.2.2). It subscribes to `Association-Repointed` under a stable subscription identifier and name beside the service's other `Association` ears, verifies the envelope and drops a fact carrying the system identity as they do, and runs the body `ProcessEntityModifiedAsync` runs.
2. **It hands the flow one thing more: the change's `UpdatedWhen`**, read from the verified fact's content. Its presence is how the flow knows the call is a changed reaction, because only this ear passes it, and it is the bound of the checks below. It reaches the flow's own body; the public `ProcessEntityModifiedAsync` and every other ear pass none, and behave exactly as they do today (§APR9.7.4, *The flow is told which change it is running for*).
3. **With the bound, a decided round is returned to `Submitted` before anything else, in exactly three cases** (§APR9.7.4, *A changed reaction's return and dismissal*): the round was decided before the change; it was approved after the change while one of the old pair's reviews still stood; or it was rejected after the change by a standing rejection while one of the old pair's rejections still stood and none of the new pair's did. A **direct** rejection — one the workflow did not record under its own identity (`WorkflowAttribution`) — is never returned. The association follows its round to `Submitted` and is unpublished until it is approved again (§APR9.8, §APR9.7.4's table).
4. **With the bound, the old pair's reviews are dismissed whatever `RequireReapprovalOnChange` says** — every active review created before the change — and the AI reviewer's assignment returns to pending with them, as every dismissal does today (§APR8.8 regardless-rule 1). The new pair's reviews stand.
5. **Then the round is evaluated**, with the reviews that are left. Under the seeded `(Association, IsPersonal = true)` tier it is approved again in the same act (§DOM4.5 rule 4).
6. **The round's reviews are read through `IAccessBroker.FindDismissableApprovalReviewsAsync`** (`Backend/Brokers/AccessBroker.md §2`), unfiltered and unbounded, and the flow compares each review's `CreatedWhen` with the change's itself. The flow runs as the reader, who may see none of the round's reviews, so the caller-facing read is never asked (§APR9.7.4). `DismissStaleApprovalReviewsAsync` takes the same bound and dismisses only the reviews created before it; its other callers pass none.
7. **A redelivered fact finds nothing left to do.** Once a change is processed no old-pair review stands and no round decided before it is left decided, so a second delivery returns nothing, dismisses nothing and runs only the re-evaluation (§EVN19 rule 4's second shape, §EVN20 rule 4).

A changed reaction's comments and its outstanding review requests stay as they are. The owner ruled that it keeps to the generic approval process, which dismisses the open reviews and clears nothing else (§APR9.7.4, *What else starts empty*, ruled 2026-09-28). The ear dismisses reviews only, and holds no service that could change either of the others. The `-Modified` and `-Submitted` dismissal's own missing redelivery check predates this and is not closed here (§APR9.7.4, *One residual*).
