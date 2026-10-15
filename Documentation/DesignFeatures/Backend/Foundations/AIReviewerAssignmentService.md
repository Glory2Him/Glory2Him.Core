# AI reviewer assignment service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IAIReviewerAssignmentService` and its workflow seam `IAIReviewerAssignmentWorkflowService` (`Glory2Him.Core/Services/Foundations/AIReviewerAssignments/`)
Inherits: §APR8.6.2, *Assignment*, §APR8.6.2.1, *The write is a seam verb*, §ARC16.7.1, §SEC14.6.1, BereanReview.md rules 4, 8, 9, 18, 19 and 21

Berean's assignment is its review: it records where the review stands (BereanReview.md rule 21). Every write the workflow makes on it is the system's own and goes through the seam, which mints the system identity itself, because the caller-facing gate asks for a review-tier role and the system holds none (§APR8.6.2.1, *The write is a seam verb*). After this story the seam carries four verbs for the approval process: the automatic add; the dismissal and the return to pending, which clear the review's tracking fields on a data change; and the removal from a round that no longer offers Berean, which leaves them as they are (§7). The writers of the review's start and completion are the review process's, designed under #1216. The caller-facing modify, which nothing calls once the re-request is gone (BereanReview.md rule 4), leaves the service.

**Each verb's task writes that verb's own summary, and each build rewrites the words of the seam's summary it makes untrue.** The seam's interface summary (`IAIReviewerAssignmentWorkflowService.cs`) says why the seam exists and how its writes differ from a person's, and four builds change what it describes. Each rewrites the words it makes untrue in its own pull request, so that none waits for a later task (`design.md`, *A gap that is built stays in its list*), and each takes the summary as the builds before it leave it:
- **the re-request's removal** (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §1` rule 5) rewrites its account of the re-request, *"A re-request and a reset are different acts"*;
- **the caller-facing modify's retirement** (§4) rewrites its words on the modify — *"the public modify verb cannot serve it: its gate asks for a review-tier role"*, *"or to call the public verb as the EDITOR"* and *"a bypass bolted onto <c>ModifyAIReviewerAssignmentAsync</c>"* — which say that the caller-facing verbs cannot serve it;
- **the reset's change** (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1`) rewrites its account of §8.6 HR-4's reset as a reason the seam exists, since the reset no longer touches Berean's assignment;
- **the data change** (§2 there, #1167) rewrites what is left of its account of the Modified flow's return to pending, which the data change replaces with the dismissal and the return to pending it calls, once the reset's change, the dismissal (§2 here), the caller-facing modify's retirement (§4), the widened return to pending (§5) and the removal (§7) are as this story leaves them.

The modify's retirement and the reset's change may land in either order, and rewrite different sentences of it. A verb that joins the seam makes none of the summary untrue: the summary says why the seam exists, and each verb's own summary says what that verb does. **The widened return to pending (§5) lands after the re-request's removal**, so that it too makes none of it untrue: while the re-request stands, the summary's paragraph on it says that the re-request and the return to pending *"write the same two fields"*, and §5 has the return clear five, so landed first it would make those words untrue. The re-request's removal rewrites that paragraph first.

## 1. CompleteAIReviewerAssignmentAsync, moved to #1216 (#1159, closed)

Retired from this design. It marked a live, pending assignment completed for the write-back (#1159, closed). The review process now writes both the start and the completion (BereanReview.md rule 21), and #1216 designs their verbs with it. The number is kept so that §2–§6 keep theirs.

## 2. DismissStaleAIReviewerAssignmentAsync (#1160)

```csharp
ValueTask<AIReviewerAssignment> DismissStaleAIReviewerAssignmentAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

1. **It soft-deletes a live assignment, started or not and completed or not**, under the system identity, with the `DeletionReason` *"Dismissed: the content changed."* — so a reader can tell it apart from a withdrawal, whose `DeletedBy` names the person (BereanReview.md rule 9, step 1). `DeletedBy` names the system.
2. **In the same write it clears the assignment's five tracking fields** — `IsAIReviewStarted` and `IsAIReviewCompleted` to `false`, `AIReviewQueuedAt` and `AIReviewCompletedAt` to null, and `IsAIReviewCommentsPresent` to `false` (BereanReview.md rule 9, step 3).
3. **On an assignment already withdrawn or dismissed, it writes nothing** and answers the stored row. A missing id is not found.
4. It publishes `AIReviewerAssignment-Removed`, as the withdrawal does, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §6`).
5. **It records no `ProcessedEvent`**, as the seam's other verbs record none (§EVN19): it has no event door, and its caller, the Modified flow, records none either.
6. **Its failures map as the automatic add's do** (`AIReviewerAssignmentServiceTests.AddAutomatic.cs`), through the class's one `TryCatch`: a SQL error on the read or the update, a critical `AIReviewerAssignmentDependencyException`; a database-update error, an `AIReviewerAssignmentDependencyException`; a concurrency conflict on the update, an `AIReviewerAssignmentDependencyValidationException`; a dependency's `OperationCanceledException` under a token that was not cancelled, an `AIReviewerAssignmentDependencyException` wrapping `TimeoutAIReviewerAssignmentException`; a token cancelled before the call, rethrown as it is; an unexpected failure, an `AIReviewerAssignmentServiceException`; and the envelope broker's or the event broker's failure, an `AIReviewerAssignmentDependencyException`. A failure before the write publishes nothing.
7. **Its own summary** says that it dismisses Berean's live assignment on a data change, under the system identity, and clears where its review stood in the same write.

## 3. OnModifyingAIReviewerAssignmentAsync, retired (#1164)

The `AIReviewerAssignment-Modifying` door goes. Nothing publishes to it, nothing wires it — `EventSubscriptionRegistration` subscribes to no `AIReviewerAssignment` address, and `AIReviewerAssignmentService.Substrate.cs` says so (*"Nothing wires these handlers to a live subscription yet"*) — and once §4's verb goes it has nothing to reach.

1. **What goes**: the handler `OnModifyingAIReviewerAssignmentAsync`, from the service and its interface, with its tests; the operation `AIReviewerAssignmentEventOperation.Modifying`, and with it its address — `AIReviewerAssignmentModifyingEventAddressId` and both of its entries in `EventBrokerIdentifiers.AIReviewerAssignment.cs` — together, so that `EventAddressCompletenessTests.EveryEventOperationShouldHaveAnAddress` still finds an address for every operation; and the subscription's identifier, `AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionId`, which nothing registers.
2. **What stays for §4**: the subscription's name, `AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName`, under which `DoModifyAIReviewerAssignmentAsync` records its `ProcessedEvent` pair on the direct path as well as on the door (`AIReviewerAssignmentService.cs`, `AIReviewerAssignmentServiceTests.Modify.Logic.cs`). §4 removes it with that body.
3. **No subscription count moves**, because the door was never wired: §SEC14.6.1's count and the tests that pin it (`EventSubscriptionResolutionTests.ExpectedBoundHandlerCount`, `EventSubscriptionRegistrationTests`) stay as they are.

## 4. ModifyAIReviewerAssignmentAsync, retired (#1165)

The caller-facing modify goes from `IAIReviewerAssignmentService`, with its validation, once nothing calls it: its only caller is the re-request, which `Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §1` removes, and its event door, which §3 removes. The two flags then have no caller-facing writer, which is what §APR8.6.2 calls them, *"system-only"*.

1. **What goes with it**: `DoModifyAIReviewerAssignmentAsync`, its two validations, and the subscription's name §3 left, `AIReviewerAssignmentOnModifyingAIReviewerAssignmentSubscriptionName`, whose last user is that body. So do the four members whose only callers go with them, which no analyzer would flag as dead: `ValidateStorageAIReviewerAssignmentIsNotDeleted`, called from `DoModifyAIReviewerAssignmentAsync` alone; `IsSame`, called from the two modify validations alone; the `IsNotSame(Guid first, Guid second, string secondName)` overload, called from `ValidateAgainstStorageAIReviewerAssignmentOnModify` alone, while the add's calls keep the other overloads; and the test data `ModifyDependencyValidationExceptions` (`AIReviewerAssignmentServiceTests.cs`), used by the modify's exception tests alone.
2. **The words it makes untrue go with it**: the comment over `ValidateAgainstStorageAIReviewerAssignmentOnModify`, *"Only the two system-facing bools and the audit trail may change on a modify"*, goes with the method; `IsAIReviewCompleted`'s summary loses *"A human caller may still reach `Modify`, because the flag is a fact about work having happened, not a permission this service arbitrates."*; and `IsAIReviewCommentsPresent`'s *"and — on add and on modify alike — cannot stand `true`"* reads *"and cannot stand `true`"* (`AIReviewerAssignment.cs`). **So do the comments that count the modify among this service's writes, or point at it**: the gate's *"ONE gate for Add, Modify and Remove"* and the review roles' *"may assign, amend, remove and read"* (`AIReviewerAssignmentService.Validations.cs`); the add's *"The pairing invariant below"*, whose *below* is the modify's rule; the return to pending's *"contradicts the public modify's IsDeleted guard"* and *"(see ValidateOnModifyAIReviewerAssignmentAsync's own note)"* (`AIReviewerAssignmentService.Transitions.cs`); and each comment elsewhere that says the system's writes cannot go through *"the public modify"*, because it no longer exists — in `ApprovalOrchestrationService.AIReviewer.cs`, `CoreRegistration.cs` and `AIReviewerAssignmentServiceTests.ReturnToPending.cs`. Each says what the remaining writes are, with no modify among them. **And the words that rest on the modify's not-found guard go with it**: with `ValidateStorageAIReviewerAssignmentIsNotDeleted` gone, no write refuses a withdrawn row — the remove and the return to pending each answer it as stored, as the dismissal does (§2 rule 3) — so the model's `IsDeleted` summary stops calling a withdrawn assignment *"closed to writes and reads alike, both reported as not found"*, and no comment says that a person's write to one would be refused.

## 5. ReturnStaleAIReviewerAssignmentToPendingAsync — every tracking field cleared (#1168)

```csharp
ValueTask<AIReviewerAssignment> ReturnStaleAIReviewerAssignmentToPendingAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

The seam's return to pending stays, as the approval process's step 3 under `RequireReapprovalOnChange = false`, where Berean stays assigned and its review starts again on the changed data (BereanReview.md rule 9, steps 1 and 3; I3.4, I4.4).

1. **It clears all five tracking fields of a live assignment**, under the system identity: `IsAIReviewStarted`, `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` to `false`, and `AIReviewQueuedAt` and `AIReviewCompletedAt` to null. Today it clears the two flags of a completed assignment alone.
2. **It acts on any live assignment with one of them set**: one started and not yet completed as well as one completed. An assignment with none set, or one withdrawn or dismissed, is answered as stored, and nothing is written or published.
3. It publishes `AIReviewerAssignment-Modified`, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §5`). It records no `ProcessedEvent`, as §2 rule 5 has it.
4. **Its callers change**: the reset stops calling it (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1`), and the Modified flow calls it under `RequireReapprovalOnChange = false` alone (§2), where today it calls it under `true` on every dismissing `-Modified`. Neither change is this section's. **Its own summary is rewritten**: it clears every tracking field of a live assignment with one of them set, so that Berean's review starts again while Berean stays assigned, and an assignment with none set, withdrawn or dismissed is answered as stored. It stops saying that *"only the two flags recording what its pass reported go back"*, and that *"a retried reset"* reaches it. The seam's interface summary is not this section's (the opening rule above).
5. **Its failures map as §2 rule 6 maps the dismissal's**, and each is proven by a test of its own: the verb has no exception test today (`AIReviewerAssignmentServiceTests.ReturnToPending.cs` holds logic, validation and the context refusal alone).

## 6. AddAIReviewerAssignmentAsync — the tracking fields refused (#1218)

```csharp
ValueTask<AIReviewerAssignment> AddAIReviewerAssignmentAsync(
    AIReviewerAssignment aiReviewerAssignment,
    CancellationToken cancellationToken = default);
```

1. **An add that carries `IsAIReviewStarted` `true`, or either of `AIReviewQueuedAt` and `AIReviewCompletedAt` set, is refused**, with a validation failure naming each field, as the add already refuses `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` set (`IsSetOnAdd`): an assignment is born pending, and a row claiming a review that has started or finished claims work that never ran (BereanReview.md rule 21).
2. It is asked on the direct path and on the `AIReviewerAssignment-Adding` door alike, because both converge on the add's one validation. The automatic add (`AddAutomaticAIReviewerAssignmentAsync`) mints its row itself, with all five fields empty.
3. **It builds on `Backend/Models/AIReviewerAssignment.md §1`**, which gives the model the three fields, and lands before §2 there maps them.

## 7. RemoveUnofferedAIReviewerAssignmentAsync — Berean removed from a round that no longer offers it (#1233)

```csharp
ValueTask<AIReviewerAssignment> RemoveUnofferedAIReviewerAssignmentAsync(
    Guid aiReviewerAssignmentId,
    CancellationToken cancellationToken = default);
```

The approval process's step 4 removes Berean from a round that no longer offers it, and publishes no review event there (BereanReview.md rule 9, step 4.3; R9). The caller-facing remove cannot serve it: its gate asks for a review-tier role, and the system identity holds none. So the workflow removes Berean through the seam, as it dismisses Berean (§2).

1. **It soft-deletes a live assignment, started or not and completed or not**, under the system identity the seam mints, with the `DeletionReason` *"Removed: Berean is no longer offered on this round."* — so a reader can tell it from a withdrawal, whose `DeletedBy` names the person, and from a dismissal, whose reason names the content's change. `DeletedBy` names the system. A context that is not the system's is refused before any row is read, as the return to pending refuses one.
2. **It leaves the assignment's five tracking fields as they are**: the removal is a reviewer change and no data change, and only a data change clears them (BereanReview.md rule 21).
3. **On an assignment already withdrawn, dismissed or removed, it writes nothing** and answers the stored row. A missing id is not found.
4. It publishes `AIReviewerAssignment-Removed`, as the withdrawal and the dismissal do, which the round's re-test hears (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §6`).
5. **It records no `ProcessedEvent`**, as the seam's other verbs record none (§EVN19).
6. **Its failures map as §2 rule 6 maps the dismissal's**, through the class's one `TryCatch`, and each is proven by a test of its own.
7. **Its own summary** says that it removes Berean's live assignment from a round whose policy no longer offers Berean, under the system identity, and leaves where its review stood as it is.
8. **It lands after §2**, whose words on the seam's verbs it extends, each as §2 leaves it: the class summary over the seam's arm in `AIReviewerAssignmentService.Transitions.cs`, which lists the seam's acts and gains this one; §APR8.6.2.1's count of the seam's members; §ARC16.7.1's note *A seam may grow a second verb*, whose *"The one removal the seam carries"* it makes untrue; §EVN19's list of the seam verbs that publish a fact and write no `ProcessedEvent`, which it joins; and the constructor comment of `AIReviewerOrchestrationService.cs`, the code twin of §ARC16.7.1's note, whose paragraph on the seam's verbs that service does not call names the dismissal once §2 has landed, and gains this one.
