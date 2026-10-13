# Approval orchestration service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: orchestration — `IApprovalOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Approvals/`). The Likes feature's work on the same service is `Backend/Orchestrations/ApprovalOrchestrationService.md`, and the amender record's `ApprovalOrchestrationService.Amenders.md`.
Inherits: §APR8.6 HR-4, §APR8.8, §APR9.7.4, §APR9.7.7, §ARC12.5.3, §ARC16.7.1, §ARC16.7.5, §EVN18(a), (d) and (h), §EVN20 rule 4, §SEC14.6.1, BereanReview.md rules 9–11, 13, 14 and 19

The round's rules for Berean. The service adds no dependency: it clears an item's score through the item's `<Entity>-SettingConfidence` request door, as it decides an item through `<Entity>-Approving`, with the `IEventBroker` it holds. It dismisses and adds Berean through the `IAIReviewerAssignmentWorkflowService` seam it holds, and it reads through the `IAccessBroker` it holds. It keeps its three entity services.

## 1. ResetApprovalAsync — Berean's review left as it is (#1166)

```csharp
ValueTask<ApprovalOutcome> ResetApprovalAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **The reset no longer returns Berean's assignment to pending** (`ApprovalOrchestrationService.Resets.cs`, the call to `ResetStaleAIReviewerAssignmentAsync`). It re-opens the same content, whose score never changes (BereanReview.md rules 7 and 9), so Berean's completed review and the item's score stand on the re-opened round, and a pending one stays pending. Everything else the reset does is unchanged.

## 2. ProcessEntityModifiedAsync — Berean's review cleared on a content change (#1167)

```csharp
ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **Berean's half runs only where the flow dismisses the round's reviews under `RequireReapprovalOnChange = true`, on a content change** (BereanReview.md rule 9). A content change is a fact on the `-Modified` ear of `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` or `BibleReference`. The `-Submitted` ears, `Association-Modified` and `Association-Repointed` carry none. Each ear hands the flow whether its fact is a content change, as the `Association-Repointed` ear hands it the change's `UpdatedWhen` (§APR9.7.4, *The flow is told which change it is running for*). A redelivered `-Modified` runs it again, as it runs the review dismissal again (§APR9.7.4, *One residual*).
2. **Before the round is evaluated, two writes**, each under `CreateSystemAsync` (rule 19):
   1. where the item holds a score — read through `IAccessBroker.RetrieveApprovalSubjectConfidenceAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §3`) — its four `IConfidence` fields are cleared, by publishing `<Entity>-SettingConfidence` with all four null and awaiting the reply. An item with no score is sent nothing, so no item whose foundation has no set-confidence door yet is sent a request (BereanReview.md, *What ships together* step 5);
   2. the round's live Berean assignment, if one stands, is dismissed through `IAIReviewerAssignmentWorkflowService.DismissStaleAIReviewerAssignmentAsync`, found through `IAccessBroker.RetrieveLiveAIReviewerAssignmentAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §1`) — never a caller-facing read, because the flow runs as the editor.
   A failure of either faults the flow, as a failure of the review dismissal before them does, so the evaluation never runs over a score or an assignment that describes content that has gone.
3. **After the evaluation, Berean is assigned for the new round** where `IAccessBroker.ResolveAIReviewerPolicyByIdAsync` answers `IsAutomaticallyRequested` — the offer, a round that requires review, and the automatic switch (`Backend/Clients/AccessClient.BereanReview.md §3`) — and the round is still `Submitted`. It goes through `IAIReviewerAssignmentWorkflowService.AddAutomaticAIReviewerAssignmentAsync`, whatever happened to Berean on the content that has gone, a withdrawal included (BereanReview.md rule 9). Its `AIReviewerAssignment-Added` starts a fresh run (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §2`). A failure is logged and not propagated, as §APR8.6.2.1's automatic assignment's is: what it costs is one click.
4. **The return to pending goes** from this flow, and so does the helper `ResetStaleAIReviewerAssignmentAsync` once §1 has removed its other call. The comment over the flow's AI half is rewritten to rule 9.

## 3. RetrieveApprovalVerdictAsync — the words for Berean's two blocks (#1172)

```csharp
ValueTask<ApprovalVerdict> RetrieveApprovalVerdictAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **`DescribeBlockReason` gives each of Berean's blocks its words**, which the verdict's block reasons carry and `DecideApprovalAsync`'s refusal shares:
   - `BlockedByRejectConfidenceScore`: *"Berean's confidence score is at or below the rejection threshold. Approve with a bypass."* — in place of *"This item scored zero on content confidence. Correct the score or approve with a bypass."*;
   - `AwaitingAIReviewerConfidenceScore`, new: *"Berean's review is pending. Wait for it to finish, withdraw Berean, or approve with a bypass."*

## 4. OnAIReviewerAssignmentAddedAsync — the round re-tested (#1173)

```csharp
ValueTask<EventEnvelope<AIReviewerAssignment>?> OnAIReviewerAssignmentAddedAsync(
    EventEnvelope<AIReviewerAssignment> envelope,
    CancellationToken cancellationToken = default);
```

1. **A new subscription on `AIReviewerAssignment-Added`** re-tests the round its `ApprovalId` names, through `ReactToWorkflowRecordFactAsync`, with the accepted name `"AIReviewerAssignmentAdded"`, as the comment and review facts are re-tested (§EVN18(a)). The assignment can only hold the round, so the re-test changes nothing today. It is heard because every fact of a record that can move a predicate is heard (§EVN18(a)), which `WorkflowRecordFactTests` derives from the operation enum: §6, the last of the three ears, extends that derivation to `AIReviewerAssignmentEventOperation`.

## 5. OnAIReviewerAssignmentModifiedAsync — the round re-tested (#1174)

1. **As §4, on `AIReviewerAssignment-Modified`**, with the accepted name `"AIReviewerAssignmentModified"`. Berean's review completing lifts the pending block (BereanReview.md rule 11), so a round whose conditions are then met is approved automatically where its policy says so.

## 6. OnAIReviewerAssignmentRemovedAsync — the round re-tested (#1175)

1. **As §4, on `AIReviewerAssignment-Removed`**, with the accepted name `"AIReviewerAssignmentRemoved"`. Withdrawing Berean, or §2's dismissal, lifts the pending block.
2. **It extends `WorkflowRecordFactTests`' derivation to `AIReviewerAssignmentEventOperation`**, after §4 and §5, so the suite finds all three of the assignment's facts subscribed when it starts deriving them.
