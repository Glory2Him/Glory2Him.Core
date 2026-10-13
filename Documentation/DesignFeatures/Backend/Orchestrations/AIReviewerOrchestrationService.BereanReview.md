# AI reviewer orchestration service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: orchestration — `IAIReviewerOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/AIReviewers/`)
Inherits: §APR8.6.2, *Assignment*, §APR8.6.2.1, §ARC12.5 (entry 4, and *an orchestration holds brokers*), §EVN2, §EVN18(e), §EVN19 rules 1 and 4, §EVN23, §SEC14.6 rule 4, §SEC14.6.1, BereanReview.md rules 4, 6–8, 16–20, *Layer placement* items 3 and 5

Berean's lifecycle: asking for it, the start of its run, and the record of what the run answered. The service spans `Approval`, `AIReviewerAssignment` and — from §3 — `ApprovalComment`, three entity services and each a foundation, within two-to-three (§ARC12.5). It reaches the item under review only by its set-confidence request address (BereanReview.md, *Layer placement* item 2), so no item service is added. It gains `IEventBroker` to publish, and `IApprovalCommentWorkflowService` (`Backend/Foundations/ApprovalCommentService.md §1`), both registered by the host.

## 1. RequestAIReviewerAsync — no re-request (#1162)

```csharp
ValueTask<AIReviewerAssignment> RequestAIReviewerAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **Asking Berean onto a round whose live assignment is completed changes nothing**: it answers the standing row and writes nothing, as asking onto a pending one already does (BereanReview.md rules 4 and 8). It no longer resets a completed assignment to pending through `IAIReviewerAssignmentService.ModifyAIReviewerAssignmentAsync`, which this service then calls nowhere.
2. **Everything else is unchanged**: a round with no live assignment gets one, through the caller-facing add, under the caller's identity, gated on the resolved offer and an open round.
3. **The comment over `ValidateUserMayRequestAIReviewer`** stops saying *"an assignment sets nothing"*: an assignment holds the round shut while Berean's review is pending (BereanReview.md rule 11). The requesting tier still asks no HR-3 term, because asking Berean decides nothing — it moves the round only toward a block.
4. The interface's summary of the operation stops calling it *"asks it again once a prior assignment has completed"*.

## 2. OnAIReviewerAssignmentAddedAsync — the start (#1176)

```csharp
ValueTask OnAIReviewerAssignmentAddedAsync(
    EventEnvelope<AIReviewerAssignment> envelope,
    CancellationToken cancellationToken = default);
```

A new subscription on `AIReviewerAssignment-Added`, registered under stable identifiers (`EventBrokerIdentifiers.AIReviewerOrchestration.cs`, `EventSubscriptionRegistration`). It hears every assignment's arrival — a person's request, the automatic assignment, and the fresh assignment after a content change — and so it is the one start (BereanReview.md rule 4).

1. **The envelope verifies** against the accepted name `"AIReviewerAssignmentAdded"`, through `IEnvelopeIntegrityBroker` (§SEC14.6 rule 4). A null, contentless or tampered envelope starts nothing.
2. **The assignment on the envelope is not soft deleted**, and the round its `ApprovalId` names reads back, through `IApprovalWorkflowService.RetrieveApprovalByIdAsync`, not soft deleted and `Submitted`. Otherwise the start does nothing.
3. **Whether the item is owed a run** is read through `IAccessBroker.RetrieveApprovalSubjectConfidenceAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §3`), never through a caller-facing read (BereanReview.md rule 20):
   - an item that is not available starts nothing, and the assignment stays pending (rule 16);
   - an item that already holds a score is not run again (rule 7): the assignment is completed at once, through `IAIReviewerAssignmentWorkflowService.CompleteAIReviewerAssignmentAsync` with `isAIReviewCommentsPresent = false`;
   - an item with no score is owed a run: the start publishes `ContentAnalysis-Scoring`, whose content is `ContentAnalysisRequest` — `ApprovalId`, `EntityType`, `EntityId` and `AIReviewerAssignmentId` — under `IEventEnvelopeBroker.CreateSystemAsync` (rule 19). The address is minted here (`EventBrokerIdentifiers.ContentAnalysis.cs`). The confidence process that hears it is not built (BereanReview.md, *Out of scope*).
4. **It records no `ProcessedEvent`** (§EVN19 rules 1 and 4). A redelivered fact on an assignment the first delivery completed finds it completed and does nothing. One redelivered while the run is in flight publishes a second request, whose answer the write-back discards (§3 rule 2) — the cost of a redelivery, recorded under the feature's *Risks*.
5. **A failure is logged and contained** (§EVN23), and the assignment stays pending, so the round stays held. The person's way back is BereanReview.md rule 16's.
6. **The comment over the automatic assignment's two subscriptions** in `EventSubscriptionRegistration.cs` — the code's copy of §EVN18(e)'s point 1, *"it reads no §8.5 predicate and moves none"* — is rewritten to the restated point 1: it moves one predicate, and only toward a block.

## 3. OnContentAnalysisScoredAsync — the write-back (#1177)

```csharp
ValueTask OnContentAnalysisScoredAsync(
    EventEnvelope<ContentAnalysisResult> envelope,
    CancellationToken cancellationToken = default);
```

A new subscription on `ContentAnalysis-Scored`, the confidence process's answer, minted here with its content model `ContentAnalysisResult`: `ApprovalId`, `AIReviewerAssignmentId`, `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId`, `ModelVersion`, and `Comments`, a list of `(string Text, ApprovalCommentType CommentType)`, empty where the run files none (BereanReview.md rule 6).

1. **The envelope verifies** against the accepted name `"ContentAnalysisScored"`. A null, contentless or tampered envelope writes nothing.
2. **The run's assignment must still be the round's live one, and not yet completed** (BereanReview.md rule 17). That is read through `IAccessBroker.RetrieveLiveAIReviewerAssignmentAsync` for the fact's `ApprovalId` (`Backend/Brokers/AccessBroker.BereanReview.md §1`). Where no live assignment stands, where it is another, or where it is already completed, nothing is written: the run was withdrawn, dismissed, superseded or already recorded.
3. **The item is the round's**, read through `IApprovalWorkflowService.RetrieveApprovalByIdAsync`, its `EntityType` and `EntityId` — never the fact's own claim of either.
4. **Three writes, in this order, each under `CreateSystemAsync`** (rule 19):
   1. **the score**: `<EntityType>-SettingConfidence` is published for the item, carrying the four fields, and its reply is awaited. The item's set-confidence lands it only if the item has none (rule 7);
   2. **the comments**: each one the fact carries, through `IApprovalCommentWorkflowService.AddAIReviewerApprovalCommentAsync`, in the order the fact lists them, and only while the round is still `Submitted`: on a round decided since the run began, none is filed;
   3. **the completion**: `IAIReviewerAssignmentWorkflowService.CompleteAIReviewerAssignmentAsync`, with `isAIReviewCommentsPresent` `true` where the fact carries at least one comment.
5. **Until the completion lands the round stays held** (BereanReview.md rule 11), so a question Berean files is on the round before the round can pass. The completion's `AIReviewerAssignment-Modified` re-tests the round (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §5`).
6. **A failed write ends the write-back there, and faults the delivery**, which the substrate delivers again. A redelivery finds a completed assignment and does nothing (rule 2). One that finds it pending writes again: the score, which lands on an unscored item only, the comments, and the completion. So a delivery that failed after filing a comment files that comment a second time — an accepted cost, recorded under the feature's *Risks*. A question filed twice is settled twice, and nothing is lost.
7. **`CreateElevatedAsync`'s summary** stops drawing the line between the two mints at whether a person asked (*"This one is for an act a person asked for but is not permitted to write directly, and records the person"*, `IEventEnvelopeBroker.cs`): a person's request of Berean is written under the system mint (§SEC14.6.1).
