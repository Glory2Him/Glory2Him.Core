# Association orchestration service — rejected endpoints
Parent: [RejectedEndpoints.md](../../RejectedEndpoints.md)
Level: orchestration — `IAssociationOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Associations/`), existing. Its other user story is `Backend/Orchestrations/AssociationOrchestrationService.md`, the Likes feature's.
Inherits: §ARC12.3.1 rule 5a, §ARC12.5.2 (`Association-Adding`'s write flow), §ARC16.2.1, §ARC16.8.1, §DOM4.10 rules 6–9, §SEC14.5 rules 1 and 2, §SEC14.6 rules 1 and 4, §SEC14.7 posture A rule 4 and posture A′ rule 1, `Backend/Orchestrations/AssociationOrchestrationService.md` §1, §2, §3 and *Deviations* 1, RejectedEndpoints.md rules 1–10

The refusal of a rejected tag or Bible reference on the association write (RejectedEndpoints.md rules 1 and 2). It is one step in `DeriveAssociationToAddAsync` (`AssociationOrchestrationService.cs`), the write flow `UpsertAssociationAsync` and the `Association-Adding` door share. So §1 builds it on the direct path, and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`ORCHESTRATIONS: Pin That …`). §3 pins that the pair-keyed withdrawal, which shares the endpoint resolution but not the flow, is not refused (RejectedEndpoints.md rule 3). Each section changes or pins an existing member, as `Backend/Foundations/AssociationService.SoftDeletion.md` §1 records another feature's change to an existing member. No member and no dependency is added (RejectedEndpoints.md, *No dependency, no operation and no deviation*).

**Build order.** §1 first. §2 and §3 after it, in either order.

## 1. UpsertAssociationAsync — refuse a rejected tag or Bible reference (#1358)

```csharp
ValueTask<AssociationSuggestionResult> UpsertAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

An existing member, changed. Its signature, its working copy (`AssociationOrchestrationService.md §6`), its branch, both arms, its result and its catch arms are unchanged.

1. **The flow gains one step**, between `AssociationOrchestrationService.md §1` rule 1's step 4, an editorial pair's endpoint veto, and its step 5, the `UserId` derivation (RejectedEndpoints.md rule 5). Where either endpoint is a `Tag` or a `BibleReference` whose `ApprovalStatus` is `Rejected`, the pair is refused, whoever the caller and whichever arm the pair would take (RejectedEndpoints.md rules 1 and 7).
2. **It reads the status endpoint resolution has already read.** Resolution reads a `Tag` endpoint through `ITagService.RetrieveTagByIdAsync`, and a `BibleReference` endpoint through `IBibleReferenceService.RetrieveBibleReferenceByIdAsync`, each under the caller's envelope (`AssociationOrchestrationService.Endpoints.cs`, `ResolveEndpointCoreAsync`), and today discards the row. It keeps that row's `ApprovalStatus` for the refusal, as it keeps a reaction's name for the facet gate. Nothing is read a second time, and no other entity's status is kept or asked (RejectedEndpoints.md rule 10).
3. **The refusal is `InvalidAssociationOrchestrationException`, with the message `"The {A|B} endpoint is rejected, so it cannot be paired."`**, naming the rejected endpoint, `A` where both are rejected. The service maps it to `AssociationOrchestrationValidationException` and logs it as its other validation failures are logged (`AssociationOrchestrationService.Exceptions.cs`, `CreateAndLogValidationExceptionAsync`). `POST api/Associations` answers it `400` through `AssociationsController.md`'s shared mapping, unchanged (RejectedEndpoints.md rule 8).
4. **A refused pair writes nothing and asks nothing more**: neither `IAssociationService.AddAssociationAsync` nor `UpsertPersonalAssociationAsync` is called, the editorial arm's two pair probes are not run, and `IAccessBroker.RetrieveEffectiveContentItemSettingsAsync`, the facet gate's read, is not asked (RejectedEndpoints.md rules 5 and 7).
5. **The personal arm is refused whatever the foundation would have answered** — `Created`, `Restored`, `Repointed`, `Unchanged` or `TakenDown` — because the step comes before the branch (RejectedEndpoints.md rule 7). Its host is a `BibleReference` on endpoint A (§DOM4.10 rule 9).
6. **A caller who may not see the rejected row is answered as today**: not-found, from resolution, before the step is reached (RejectedEndpoints.md rule 4). That is everyone but the row's owner and the review roles (§SEC14.7 posture A rule 4). The step never turns a not-found into a refusal that names the rejection.
7. **An editorial pair's endpoint veto is asked first** (RejectedEndpoints.md rule 5). A caller the veto blocks is refused unauthorised, as today, whatever the endpoint's status.
8. **A `Tag` or `BibleReference` at `Draft`, `Submitted` or `Approved` is paired as today.** Only `Rejected` is refused: the precondition is still never *"approved"* (§ARC12.3.1 rule 5a).
9. **The step is not in the endpoint resolution.** `ResolvePairEndpointsAsync` is shared with the pair-keyed withdrawal, `RemoveAssociationByPairAsync` (`AssociationOrchestrationService.PairRemoval.cs`), which no rejection refuses (RejectedEndpoints.md rules 3 and 6; §3 below). The step reads what resolution kept, inside `DeriveAssociationToAddAsync`.
10. **The code text the step makes untrue or incomplete is rewritten in the same change.** The interface's summary of `UpsertAssociationAsync` (`IAssociationOrchestrationService.cs`), which says an occupied editorial pair *"is returned as it stands"* and that a reaction the reader already holds answers by its status: each now holds only where no endpoint is rejected, and the summary gains the refusal, asked before either arm. The comment above `ResolvedEndpoint`, which lists what an endpoint resolves to, the group id, the content type, the scope and a reaction's name (`AssociationOrchestrationService.Endpoints.cs`). And the comment *"Each resolution is kept for the facet gate."* in `DeriveAssociationToAddAsync` (`AssociationOrchestrationService.cs`). The last two gain the rejected status and the step that reads it.

## 2. OnAddingAssociationAsync — the refusal on the event door (#1359)

```csharp
ValueTask<EventEnvelope<Association>?> OnAddingAssociationAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

An existing member, unchanged. §1's step is in the write flow this door runs (`AssociationOrchestrationService.Substrate.cs`; §ARC12.5.2), so the door refuses a rejected tag or Bible reference by construction (§ARC16.2.1, *the gate is reached on both entry paths*). This section proves it, as `AssociationOrchestrationService.md §5` proves the facet gate on the same door.

1. **An editorial pair with a rejected `Tag` or `BibleReference` endpoint is refused here as §1 refuses it**, with the same exception and message, when the signed caller is the row's owner or holds a review role. The endpoint reads carry the inbound envelope, so who may see the row is the signed caller's question, never an ambient one (§ARC12.5.2).
2. **A refused envelope writes nothing**: the foundation's `OnAddingAssociationAsync` is not called, so no row is written, no `Association-Added` is published and no `ProcessedEvents` row is recorded. The occupancy probes are not run.
3. **The door's earlier refusals come first**, unchanged: the envelope's verification, the deduplication check and the refusal of a personal pair (`AssociationOrchestrationService.md §2`) all run before the flow, so a replayed or personal envelope never reaches the step.
4. **Its tests are pins.** §1 puts the step into the flow this door already runs, so they pass as soon as they are written, and are committed as pins.

## 3. RemoveAssociationByPairAsync — a withdrawal on a rejected reference (#1360)

```csharp
ValueTask<AssociationRemovalResult> RemoveAssociationByPairAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

An existing member, unchanged (`AssociationOrchestrationService.md §3`). No removal is among the doors the refusal reaches (RejectedEndpoints.md rule 3), and this member resolves its endpoints through the resolution §1 changes (§1 rule 9). This section pins that it is not refused.

1. **A reader withdraws their own live reaction on a rejected Bible reference** as on any other: the row is soft-deleted through `IAssociationService.RemoveAssociationByIdAsync` and the answer is `Removed`, with the row's id. The reader can see the host, being its owner or holding a review role, so resolution finds it.
2. **Its tests are pins.** The withdrawal is unchanged by §1, so they pass as soon as they are written, and are committed as pins.

## Deviations

None. The service's count of seven entity services is `AssociationOrchestrationService.md`'s *Deviations* 1, and this user story adds no dependency and no member (RejectedEndpoints.md, *No dependency, no operation and no deviation*).
