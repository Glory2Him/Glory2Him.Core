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

1. **Berean's half runs only where the flow dismisses the round's reviews under `RequireReapprovalOnChange = true`, on a content change** (BereanReview.md rule 9). A content change is a `-Modified` of `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` or `BibleReference` whose write changed a content field, by the test §APR8.6.3 rule 3 applies to the amender record. An echo of the stored row is none, and neither is a status move between `Draft` and `Submitted` alone (BereanReview.md R1, user ruling 2026-10-14). The `-Submitted` ears, `Association-Modified` and `Association-Repointed` carry none. A redelivered `-Modified` runs it again, as it runs the review dismissal again (§APR9.7.4, *One residual*). **How each `-Modified` ear learns whether its fact's write changed a field, and hands that to the flow, is designed under #1215**, because the fact carries the row after the write and never the row before it. This section's task (#1167) is reworked and carried under #1215, and waits for that design on `main` (BereanReview.md, *What ships together* step 6).
2. **Before the round follows the entity, the live assignment is read.** The flow reads the round's live Berean assignment, if one stands, through `IAccessBroker.RetrieveLiveAIReviewerAssignmentAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §1`) — never a caller-facing read, because the flow runs as the editor — before it moves a `Draft` round to `Submitted` with the entity (§APR9.2 rule 3). That is the assignment rule 3.2 dismisses. One the round gains during the flow is the new round's and stands: the automatic assignment hears the move's `Approval-Modified` inside the move's publish, because delivery is synchronous (§EVN11), so on a first submission through the general modify that also changes a content field it has already assigned Berean, and started a run, by the time the flow reaches rule 3 (BereanReview.md rule 9). A submission that changes no content field runs no Berean half at all (rule 1), and the automatic assignment is its one start. That run never reads a score rule 3.1 then clears: only a run writes a score on these six items, and only an assignment starts a run, so an item holding a score has an assignment row on its round, and gate 6 keeps the automatic assignment off any round with one (§APR8.6.2.1).
3. **Before the round is evaluated, two writes, in this order**, each under `CreateSystemAsync` (rule 19):
   1. where the item holds a score — read through `IAccessBroker.RetrieveApprovalSubjectConfidenceAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §3`) — its four `IConfidence` fields are cleared, by publishing `<Entity>-SettingConfidence` with all four null and awaiting the reply. The address is each item's door (`Backend/Foundations/<Entity>Service.BereanReview.md §4`). An item with no score is sent nothing;
   2. the assignment rule 2 read, if one stood and is still live, is dismissed through `IAIReviewerAssignmentWorkflowService.DismissStaleAIReviewerAssignmentAsync`.
   A failure of either faults the flow, as a failure of the review dismissal before them does, so the evaluation never runs over a score or an assignment that describes content that has gone. A failure of the dismissal after the clear leaves a completed assignment over an item with no score, which holds the round (BereanReview.md rule 11) until a person withdraws Berean: the flow fails closed.

   **The dismissal's `AIReviewerAssignment-Removed` is re-tested inside the flow, and that is ruled rather than overlooked.** The round's ear on that address (§6) runs inside the dismissal's publish, and §EVN18(d)'s suppression does not cover it, because its window is the review dismissal's loop alone. It is not suppressed: by the time it runs, the reviews are dismissed, the score is cleared and Berean is off the round, so the re-test evaluates the round exactly as the flow's own evaluation then does, and the flow's evaluation finds the round as the re-test left it. That is why the clear comes first. Widening §EVN18(d)'s window to a second address would buy nothing here.
4. **After the evaluation, Berean is assigned for the new round** where `IAccessBroker.ResolveAIReviewerPolicyByIdAsync` answers `IsAutomaticallyRequested` — the offer, a round that requires review, and the automatic switch (`Backend/Clients/AccessClient.BereanReview.md §3`) — the round is still `Submitted`, and no live assignment stands on it, so an assignment the round gained during the flow is not doubled (rule 2). It goes through `IAIReviewerAssignmentWorkflowService.AddAutomaticAIReviewerAssignmentAsync`, whatever happened to Berean on the content that has gone, a withdrawal included (BereanReview.md rule 9). Its `AIReviewerAssignment-Added` starts a fresh run (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §2`). A failure is logged and not propagated, as §APR8.6.2.1's automatic assignment's is: what it costs is one click.
5. **The return to pending goes** from this flow, and so does the helper `ResetStaleAIReviewerAssignmentAsync` once §1 has removed its other call. The comment over the flow's AI half is rewritten to BereanReview.md rule 9.

## 3. RetrieveApprovalVerdictAsync — the words for Berean's two blocks (#1172)

```csharp
ValueTask<ApprovalVerdict> RetrieveApprovalVerdictAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **`DescribeBlockReason` gives each of Berean's blocks its words**, which the verdict's block reasons carry and `DecideApprovalAsync`'s refusal shares:
   - `BlockedByRejectConfidenceScore`: *"Berean's confidence score is at or below the rejection threshold. Only a recorded bypass can approve it, where its policy allows one."* — in place of *"This item scored zero on content confidence. Correct the score or approve with a bypass."*;
   - `AwaitingAIReviewerConfidenceScore`, new: *"Berean's review is pending. Wait for it to finish or withdraw Berean, or approve it as a recorded bypass where its policy allows one."*

   Neither promises a bypass, because `DoNotAllowBypassingSettings` closes it on a tier that sets it (§APR8.6 HR-4 route 3), as the self-approval message already hedges with *"where this content type allows one"*.

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
