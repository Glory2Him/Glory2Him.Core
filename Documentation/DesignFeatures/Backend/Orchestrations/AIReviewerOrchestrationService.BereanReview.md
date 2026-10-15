# AI reviewer orchestration service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: orchestration — `IAIReviewerOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/AIReviewers/`)
Inherits: §APR8.6.2, *Assignment*, §APR8.6.2.1, §ARC12.5 (entry 4, and *an orchestration holds brokers*), §EVN2, §EVN18(e), §EVN19 rules 1 and 4, §EVN23, §SEC14.6 rule 4, §SEC14.6.1, BereanReview.md rules 3, 4, 8, 11, 18 and 19, *Layer placement* items 3–5

Berean's caller-facing lifecycle: a person's request and the review event it publishes, the withdrawal and its gate, and the status read. The service spans `Approval` and `AIReviewerAssignment`, two entity services, each a foundation, within two-to-three (§ARC12.5). It gains `IEventBroker`, to publish the review event, registered by the host. Its automatic assignment leaves it for the approval process (BereanReview.md, *Layer placement* item 4), and with it its two subscriptions on `Approval` facts. What answers the review event is the review process, designed under #1216.

## 1. RequestAIReviewerAsync — no re-request (#1162)

```csharp
ValueTask<AIReviewerAssignment> RequestAIReviewerAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **Asking Berean onto a round whose live assignment is completed changes nothing**: it answers the standing row and writes nothing, as asking onto a pending one already does (BereanReview.md rules 4 and 8). It no longer resets a completed assignment to pending through `IAIReviewerAssignmentService.ModifyAIReviewerAssignmentAsync`, which this service then calls nowhere.
2. **Everything else is unchanged**: a round with no live assignment gets one, through the caller-facing add, under the caller's identity, gated on the resolved offer and an open round.
3. **The comment over `ValidateUserMayRequestAIReviewer`** stops saying *"an assignment sets nothing"*: an assignment holds the round shut until Berean's review has completed (BereanReview.md rule 11). The requesting tier still asks no HR-3 term, because asking Berean decides nothing — it moves the round only toward a block.
4. The interface's summary of the operation stops calling it *"asks it again once a prior assignment has completed"*.

## 2. RequestAIReviewerAsync — the review event (#1222)

1. **Once the request has run, the round's live assignment is read**, and where one stands, `AIReviewerOrchestration-Reviewing` is published once, carrying it (BereanReview.md rule 4; I6, I7). That holds whether the request has just added it or found it standing, pending or completed: a person who asks while Berean stands assigned publishes the event again (I7). Where none stands — the add failed — nothing is published. The read is the one the request already makes, through `IAIReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync`, made again once the add has answered.
2. **The event is minted under `IEventEnvelopeBroker.CreateSystemAsync`** (BereanReview.md rule 19): the review it asks for acts for nobody, though a person asked for Berean. The add stays under the caller's identity.
3. **This section mints the address.** `AIReviewerOrchestration-Reviewing` is a request address `AIReviewerOrchestrationService` owns, registered under stable identifiers in `EventBrokerIdentifiers.AIReviewerOrchestration.cs`, its content `AIReviewerAssignment`. Nothing subscribes to it until #1216 builds the review process, and publishing to it with no subscriber succeeds, as `Association-ConfidenceSet` is published today with none (§APR9.7.1 rule 5). The approval process's two publishes build on it (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §7–§8`).
4. **A failed publish is logged and does not fault the request**, because the assignment it follows is written and the person's request has done what they asked: the round stays held by the pending block (BereanReview.md rule 11), and asking again publishes the event again (I7). A failed add publishes nothing and faults as it does today.
5. **The service gains `IEventBroker`**, through its constructor, registered by the host. It is a broker of the kind an orchestration may hold, outside the two-to-three (§ARC12.5).

## 3. The write-back, moved to #1216 (#1177, closed)

Retired from this design. It heard the confidence process's answer on `ContentAnalysis-Scored` and recorded the score, Berean's comments and the completion (#1177, closed). The review process now does all of it (BereanReview.md rule 6), and #1216 designs it. `ContentAnalysis-Scoring`, `ContentAnalysis-Scored`, `ContentAnalysisRequest` and `ContentAnalysisResult` were never built. The number is kept so that §4–§6 keep theirs.

## 4. WithdrawAIReviewerAsync — only before Berean's review starts (#1223)

```csharp
ValueTask<AIReviewerAssignment> WithdrawAIReviewerAsync(
    EntityType entityType,
    Guid entityId,
    CancellationToken cancellationToken = default);
```

1. **A withdrawal of a live assignment whose `IsAIReviewStarted` is `true` is refused**, with the service's validation exception and the message *"Berean's review has started, so Berean can no longer be withdrawn."* (BereanReview.md rule 18; I1.5c).
2. **A withdrawal on a round whose resolved policy asks for Berean automatically is refused**, with the message *"This round's policy asks for Berean automatically, so Berean is removed only by changing the policy."* The automatic request is the resolved `AIReviewerPolicyVerdict.IsAutomaticallyRequested`, read through `IAccessBroker.ResolveAIReviewerPolicyByIdAsync` — `IsAIReviewerOffered && IsAIReviewerAutomaticallyRequested`, with the round requiring review (`Backend/Clients/AccessClient.BereanReview.md §3`). So a policy whose offer is switched off lets an assignment made under it be withdrawn, as the withdrawal's own summary requires (*"taking Berean off a round is the one act that has to keep working after the switch closes"*). (I2.B)
3. **Otherwise the withdrawal is unchanged**: it soft-deletes the live assignment under the caller's identity, and nothing standing is a no-op. It changes no tracking field and no score (BereanReview.md rule 18). Its read of the assignment is the one it makes today, through `IAIReviewerAssignmentService.RetrieveAIReviewerAssignmentByApprovalIdAsync`.
4. **Rule 1 is asked before rule 2**, so a started review is refused for being started whatever the policy says.
5. **The operation's summary** stops calling the withdrawal *"Unconditional, unlike the human withdrawal"*, and says what it now refuses.

## 5. OnApprovalAddedAsync, retired (#1226)

The automatic assignment's subscription on `Approval-Added` goes: the subscription, its accepted name and identifiers, the handler and its tests. The approval process's step 4 on an add assigns Berean in its place (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §7`), and must be on `main` first (BereanReview.md, *What ships together* step 7). The private body both subscriptions share stays until §6 retires the other.

## 6. OnApprovalModifiedAsync, retired (#1227)

The automatic assignment's subscription on `Approval-Modified` goes, as §5's goes, after the approval process's step 4 on an update (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §8`) is on `main`. With it go the body both shared (`AIReviewerOrchestrationService.Substrate.cs`), the six gates' code, and the comment over the two subscriptions in `EventSubscriptionRegistration.cs`. **Two dependencies leave the constructor with it**, because that body is their only caller: `IAIReviewerAssignmentWorkflowService`, whose automatic add the approval process now calls through the seam it holds, and `IEnvelopeIntegrityBroker`, which verified the two facts' envelopes. `IEventEnvelopeBroker` stays, for §2. The entity-service count is unchanged: the seam shares the foundation class's arm with `IAIReviewerAssignmentService` (§ARC16.7.1 finding 2).
