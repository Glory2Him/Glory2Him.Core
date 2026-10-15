# Approval orchestration service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: orchestration — `IApprovalOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Approvals/`). The Likes feature's work on the same service is `Backend/Orchestrations/ApprovalOrchestrationService.md`, and the amender record's `ApprovalOrchestrationService.Amenders.md`.
Inherits: §APR8.6 HR-4, §APR8.8, §APR9.7.3, §APR9.7.4, §APR9.7.7, §ARC12.5.3, §ARC16.7.1, §ARC16.7.5, §EVN18(a), (d) and (h), §EVN20 rule 4, §SEC14.6.1, BereanReview.md rules 4, 9–11, 13, 14, 19, 20, 22 and 23

The round's rules for Berean, and the approval process's step 4. The service adds no dependency: it clears an item's score through the item's `<Entity>-SettingConfidence` request door, as it decides an item through `<Entity>-Approving`, and it publishes the review event, both with the `IEventBroker` it holds. It dismisses, clears and assigns Berean through the `IAIReviewerAssignmentWorkflowService` seam it holds, and it reads through the `IAccessBroker` it holds. It keeps its three entity services.

**Order in the Modified flow.** Every dismissal the flow makes comes before every write it makes that the round re-tests, so that no re-test inside the flow counts a review the flow is about to dismiss. The re-tests are not suppressed, because §EVN18(d)'s window is the review dismissal's loop alone; the order is what makes them safe.

1. **The amender's dismissal comes first**, once the round is resolved and before the round follows the entity (`ApprovalOrchestrationService.Amenders.md §1` rule 2).
2. **The dismissal of the round's active reviews under `RequireReapprovalOnChange = true`** (§APR8.8 rule 1), **and of Berean's vote alone under `false`** (§2 rule 4), come before §2 rule 3's two writes and §8's assignment. Each of those three publishes a fact the round re-tests (§4–§6).
3. **The round's move between `Draft` and `Submitted` publishes `Approval-Modified`**, which the automatic assignment hears, and assigns Berean inside, until its subscription is retired (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §6`). The three ears (§4–§6) are wired only once both of its subscriptions are retired, so that no re-test runs inside the move — which comes before the review dismissal under `true` — nor inside the reset's write of the round's status, which comes before the reset's dismissal (§ARC16.7.5).

So by the time any re-test runs inside the flow, every review the flow dismisses is dismissed, and what it counts is what the flow's own evaluation then counts. Under `false` that includes the people's standing reviews, which the flow's own evaluation counts too. Without the order, an amender's approval meeting a count of one under `false` would be counted by a re-test inside the move, before the amender's dismissal ran, and could approve the round on the amender's own vouch (§APR8.6 HR-5).

## 1. ResetApprovalAsync — Berean's review left as it is (#1166)

```csharp
ValueTask<ApprovalOutcome> ResetApprovalAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **The reset no longer returns Berean's assignment to pending** (`ApprovalOrchestrationService.Resets.cs`, the call to `ResetStaleAIReviewerAssignmentAsync`). It re-opens the same content, which is no data change, so Berean's assignment, its tracking fields and the item's score stand on the re-opened round (BereanReview.md rules 7, 9 and 23; I1.4, I2.D, R5). Everything else the reset does is unchanged.
2. **The reset fires nothing of Berean's** (BereanReview.md rule 23; R4): it assigns no Berean and publishes no review event. Its command's `-Submitted`, under the system identity, never reaches step 4, because every ear of the Modified flow drops a fact carrying that identity (§ARC16.7.5), and that guard stays (§8 rule 2). *(This rule used to have the re-opening reach step 4, which published the review event so that Berean voted again on its standing score.)*
3. **The reset's dismissal spares Berean's vote** (BereanReview.md rule 23; R5: *"Every human reviewer is dismissed. Only Berean remains unchanged."*), which narrows §APR8.8 regardless-rule 1 for Berean alone. No round holds a vote of Berean's until the review process casts one, so the exemption is built with the vote, under #1216, as §2 rule 4's dismissal of Berean's vote alone is; this section builds none of it, and the reset's dismissal is unchanged until then.

## 2. ProcessEntityModifiedAsync — the data change (#1167)

```csharp
ValueTask<ApprovalOutcome> ProcessEntityModifiedAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **Steps 1–3 run only on a data change** (BereanReview.md rule 9). A data change here is a `-Modified` of `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment` or `BibleReference` whose write changed a content field, by the test §APR8.6.3 rule 3 applies to the amender record. An echo of the stored row is none, and neither is a status move between `Draft` and `Submitted` alone (BereanReview.md R1). The `-Submitted` ears, `Association-Modified` and `Association-Repointed` carry none. A redelivered `-Modified` runs them again, as it runs the review dismissal again (§APR9.7.4, *One residual*). **How each `-Modified` ear learns whether its fact's write changed a field, and hands that to the flow, is designed under #1215**, because the fact carries the row after the write and never the row before it. This section's task (#1167) is reworked and carried under #1215, and waits for that design on `main` (BereanReview.md, *What ships together* step 6).
2. **Before the round follows the entity, the live assignment is read.** The flow reads the round's live Berean assignment, if one stands, through `IAccessBroker.RetrieveLiveAIReviewerAssignmentAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §1`) — never a caller-facing read, because the flow runs as the editor — before it moves a `Draft` round to `Submitted` with the entity (§APR9.2 rule 3). That is the assignment rule 3 acts on. One the round gains during the flow is the new round's and stands: until the automatic assignment's subscription on `Approval-Modified` is retired (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §6`), it hears the move's `Approval-Modified` inside the move's publish, because delivery is synchronous (§EVN11), so on a first submission through the general modify it has already assigned Berean by the time the flow reaches rule 3 (BereanReview.md rule 9).
3. **Before the round is evaluated, and after the flow's dismissal of the round's active reviews under `RequireReapprovalOnChange = true`** (§APR8.8 rule 1), which dismisses Berean's vote with them where it cast one, **two writes, in this order**, each under `CreateSystemAsync` (BereanReview.md rule 19):
   1. **the score, under either setting.** Where the item holds a score — read through `IAccessBroker.RetrieveApprovalSubjectConfidenceAsync` (`Backend/Brokers/AccessBroker.BereanReview.md §3`) — its four `IConfidence` fields are cleared, by publishing `<Entity>-SettingConfidence` with all four null and awaiting the reply (rule 9, step 2). The address is each item's door (`Backend/Foundations/<Entity>Service.BereanReview.md §4`). An item with no score is sent nothing;
   2. **the assignment rule 2 read, if one stood and is still live.** Under `true` it is dismissed through `IAIReviewerAssignmentWorkflowService.DismissStaleAIReviewerAssignmentAsync`, which clears its five tracking fields in the same write (`Backend/Foundations/AIReviewerAssignmentService.md §2`; rule 9, steps 1 and 3). Under `false` it is returned to pending through `IAIReviewerAssignmentWorkflowService.ReturnStaleAIReviewerAssignmentToPendingAsync`, which clears its five tracking fields and leaves it assigned (`Backend/Foundations/AIReviewerAssignmentService.md §5`; rule 9, step 3).

   **The clear comes first so that no review can skip over a stale score.** The review process skips the scoring where a score stands (BereanReview.md rule 6); with the assignment cleared and the old score still standing, a fresh review would record nothing over the changed content. A failure of either write faults the flow, as a failure of the review dismissal before them does, so the evaluation never runs over a score or an assignment that describes content that has gone, and step 4 (§8) does not run. Nothing in this host delivers the fact again (§EVN23 rule 6):
   - **a clear that fails** leaves the old score and Berean's assignment as they were, over content that has changed. Under `true` the people's reviews are already dismissed, so the count holds the round as it does today. Under `false` the people's reviews stand, and the old score keeps holding the round where it is low and Berean is assigned (rule 10) and blocks nothing where it is not. The next data change clears it;
   - **an assignment write that fails after the clear** leaves Berean's assignment as it was over an item with no score. A completed review is then held by the low-score block where it is on, since a null score reads as `0.00` (rule 10), and a review not yet completed by the pending or in-flight block (rule 11). The next data change clears it.

   **The dismissal's `AIReviewerAssignment-Removed`, and the return's `-Modified`, are re-tested inside the flow, and the order is what makes that safe** (*Order in the Modified flow*). The round's ears on those addresses (§5, §6) run inside the write's publish, and §EVN18(d)'s suppression does not cover them, because its window is the review dismissal's loop alone. By the time either runs, every review the flow dismisses is dismissed — the amender's, the round's under `true`, and Berean's vote under `false` — and the score is cleared, so the re-test counts only what the flow's own evaluation then counts. *(This paragraph used to say that the re-tests were harmless because the reviews were dismissed, which held under `true` alone.)*
4. **Under `RequireReapprovalOnChange = false`, Berean's vote alone is dismissed** where it cast one (BereanReview.md rule 9, step 1; I5.2), before rule 3's two writes and §8's assignment (*Order in the Modified flow*). That dismissal is built with the vote, under #1216, which designs how the flow finds Berean's vote; this section builds none of it.
5. **The return to pending's other path goes** from this flow: the call under `RequireReapprovalOnChange = true` on every dismissing `-Modified` (`ApprovalOrchestrationService.Flows.cs`, its AI half), which rule 3 replaces. The helper `ResetStaleAIReviewerAssignmentAsync` goes once §1 has removed its other call, and its gather with it (`Backend/Brokers/AccessBroker.BereanReview.md §2`). The comment over the flow's AI half is rewritten to BereanReview.md rule 9.
6. **Step 4 then runs as §8 has it**: it finds the assignment rule 3 dismissed gone, and assigns Berean afresh where the policy asks for it automatically, or the assignment rule 3 returned to pending standing, and publishes the event for it.

## 3. RetrieveApprovalVerdictAsync — the words for Berean's three blocks (#1172)

```csharp
ValueTask<ApprovalVerdict> RetrieveApprovalVerdictAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **`DescribeBlockReason` gives each of Berean's blocks its words**, which the verdict's block reasons carry and `DecideApprovalAsync`'s refusal shares:
   - `BlockedByRejectConfidenceScore`: *"Berean's confidence score is below the rejection threshold. Only a recorded bypass can approve it, where its policy allows one."* — in place of *"This item scored zero on content confidence. Correct the score or approve with a bypass."*;
   - `BlockedByPendingAIReview`, new: *"The Berean review has not started."*, the owner's words (BereanReview.md rule 11; I3.6);
   - `BlockedByAIReviewInFlight`, new: *"The Berean review has not completed."*, the owner's words (I1.5a).

   The low-score words do not promise a bypass, because `DoNotAllowBypassingSettings` closes it on a tier that sets it (§APR8.6 HR-4 route 3), as the self-approval message already hedges with *"where this content type allows one"*. The two review blocks carry the owner's words alone.

## 4. OnAIReviewerAssignmentAddedAsync — the round re-tested (#1173)

```csharp
ValueTask<EventEnvelope<AIReviewerAssignment>?> OnAIReviewerAssignmentAddedAsync(
    EventEnvelope<AIReviewerAssignment> envelope,
    CancellationToken cancellationToken = default);
```

1. **A new subscription on `AIReviewerAssignment-Added`** re-tests the round its `ApprovalId` names, through `ReactToWorkflowRecordFactAsync`, with the accepted name `"AIReviewerAssignmentAdded"`, as the comment and review facts are re-tested (§EVN18(a)). It is heard because every fact of a record that can move a predicate is heard (§EVN18(a)), which `WorkflowRecordFactTests` derives from the operation enum: §6, the last of the three ears, extends that derivation to `AIReviewerAssignmentEventOperation`. It publishes no review event (BereanReview.md rule 4).
2. **An assignment can only hold the round, but its re-test reads every other input as it stands when the fact arrives**, and can decide on them. So the ear is wired only where every dismissal has run before the assignment is written: inside the Modified flow by the order this story's opening rule gives, and never inside the round's move to `Submitted` or the reset's write of the round's status, because this ear, and §5's and §6's after it, land only once the automatic assignment's two subscriptions are retired (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §5–§6`). *(This section used to say that an assignment can only hold the round, so the re-test changes nothing.)*

## 5. OnAIReviewerAssignmentModifiedAsync — the round re-tested (#1174)

1. **As §4, on `AIReviewerAssignment-Modified`**, with the accepted name `"AIReviewerAssignmentModified"`. The review's start moves the round from the pending block to the in-flight one, and its completion lifts the in-flight block (BereanReview.md rule 11), so a round whose conditions are then met is approved automatically where its policy says so. A return to pending (§2 rule 3) holds it again.

## 6. OnAIReviewerAssignmentRemovedAsync — the round re-tested (#1175)

1. **As §4, on `AIReviewerAssignment-Removed`**, with the accepted name `"AIReviewerAssignmentRemoved"`. Withdrawing Berean, or §2's dismissal, lifts Berean's blocks.
2. **It extends `WorkflowRecordFactTests`' derivation to `AIReviewerAssignmentEventOperation`**, after §4 and §5, so the suite finds all three of the assignment's facts subscribed when it starts deriving them.

## 7. ProcessEntityAddedAsync — step 4: Berean assigned and its review started (#1224)

```csharp
ValueTask<ApprovalOutcome> ProcessEntityAddedAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **Where the round is created at `Submitted`, before it is evaluated**, the flow resolves Berean's policy for it through `IAccessBroker.ResolveAIReviewerPolicyByIdAsync` (BereanReview.md rule 9, step 4). A round created at `Draft` ends the flow as it does today (§APR9.7.3 rule 1), and this section runs nothing on it.
2. **Where `IsAutomaticallyRequested` is `true` and no live assignment stands**, read through `IAccessBroker.RetrieveLiveAIReviewerAssignmentAsync`, Berean is assigned through `IAIReviewerAssignmentWorkflowService.AddAutomaticAIReviewerAssignmentAsync` (step 4.2; I4.2). `IsAutomaticallyRequested` carries the offer and a round that requires review (`Backend/Clients/AccessClient.BereanReview.md §3`), so a round that requires no review is assigned nothing, and is approved by the evaluation that follows (rule 22). Where a live assignment already stands, nothing is added: until the subscription on `Approval-Added` is retired (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §5`), it has assigned Berean inside the round's own publish (§EVN11).
3. **Where a live assignment then stands**, read again once rule 2 has run, `AIReviewer-Reviewing` is published once, carrying it, under `CreateSystemAsync` (step 4.3; BereanReview.md rule 4; I5.1, I6, I7). Where none stands, nothing is published (I6). The address is minted by `Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §2`.
4. **The round is then evaluated as today** (§APR9.7.3 rule 3, §APR9.7.7), and only a round no condition blocks is approved (step 4.4).
5. **A failure of the assignment faults the flow before the evaluation**, so the round stays `Submitted` and unevaluated: the policy requires Berean (I2.B), and a round evaluated without it on such a tier is one Berean was meant to hold. *(The automatic assignment this replaces logged its failure and let the round proceed, §APR8.6.2.1, Failure posture, because a missed assignment then cost one click.)* **A failure of the publish is logged and the flow goes on**, as §2 rule 4 of the AI reviewer story has it: Berean is assigned, its review has not started, and the pending block holds the round (BereanReview.md rule 11) until a person asks for Berean again, which publishes it (I7).
6. **It records no `ProcessedEvent`**, as the flow does not (§EVN19 rules 1 and 4). A redelivered `-Added` finds the live assignment rule 2 made, adds nothing, and publishes the event again (rule 3), which the review process hears more than once (BereanReview.md, *Risks*).

## 8. ProcessEntityModifiedAsync — step 4: Berean assigned and its review started (#1225)

1. **On every fact the Modified flow hears for a round that stands `Submitted`** — `-Modified`, `-Submitted` and `Association-Repointed`, a data change or not, and each a person's, since every ear drops a fact carrying the system identity (§ARC16.7.5; BereanReview.md rule 9, step 4; R4) — once the round has followed the entity, every dismissal the flow makes has run (*Order in the Modified flow*) and any data change's steps (§2) have run, and before the round is evaluated, the flow does as §7 rules 2 and 3 do: it assigns Berean where the policy asks for it automatically and no live assignment stands, and then publishes the review event once where one stands. A round that stands `Draft` after the flow has followed the entity runs none of it.
2. **The reset never reaches this step**, and this section adds no case for it. Its `-Submitted` carries the system identity and is dropped before the flow runs (§ARC16.7.5), and the owner ruled that the reset fires nothing (BereanReview.md rule 23; R4). The guard stays: lifted, it would let step 4.4 approve again the round the reset had just re-opened. *(This rule used to treat a `-Submitted` that found its round already `Submitted` as the reset's, assigning nothing on it and publishing the review event.)*
3. **So the review event is published again on a person's update while Berean stands assigned**, whatever the update was: under `RequireReapprovalOnChange = false` after a data change, which is what the owner asks (I4.4); on an echo; and on a submission from `Draft`. What the review process does with it is #1216's (BereanReview.md rule 17).
4. **Its failures are §7 rule 5's**: a failed assignment faults the flow before the evaluation, and a failed publish is logged.
5. **It replaces the automatic assignment's subscription on `Approval-Modified`**, which hears the same submissions (§APR8.6.2.1), and must reach `main` before that subscription is retired (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §6`; BereanReview.md, *What ships together* step 7). It does not wait for #1215: it asks nothing of whether the fact was a data change.
