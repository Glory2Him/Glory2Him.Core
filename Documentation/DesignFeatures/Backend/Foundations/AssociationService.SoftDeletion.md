# Association service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`). Its other user stories are `Backend/Foundations/AssociationService.md`, `AssociationService.AssociationReads.md`, `AssociationService.BereanReview.md` and `AssociationService.ConfidenceReads.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, posture A′ rule 4, §ARC12.3.1 shared rule 9, §DOM4.10, SoftDeletion.md rules 1–3, 15 and *The rulings*

`ModifyAssociationAsync` refuses a soft-deleted association (SoftDeletion.md rule 10). The refusal is asked in `DoModifyAssociationAsync` (`AssociationService.cs`), the body the direct path and its `Association-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*).

A reader's own withdrawn reaction is a soft-deleted personal association, and the general modify refuses it like any other. Re-giving it goes through the personal upsert, whose restored and repointed arms revive it, and which the owner's ruling leaves as it is (SoftDeletion.md rule 15; §DOM4.10 rules 6–8).

**Build order** (SoftDeletion.md, *What ships together*). §1 is the last refusal to land, after each of the six other items' (`Backend/Foundations/ContentItemService.SoftDeletion.md §1`, and §1 of `LinkService.SoftDeletion.md`, `TagService.SoftDeletion.md`, `ReactionService.SoftDeletion.md`, `CommentService.SoftDeletion.md` and `BibleReferenceService.SoftDeletion.md`): its build completes the refusal, and it rewrites the record to say so (§1 rule 8). §2 lands after §1.

## 1. ModifyAssociationAsync — refuse a soft-deleted association (#1342)

```csharp
ValueTask<Association> ModifyAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **A stored association whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `Association-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing association is**: `NotFoundAssociationException`, with the message `"Content item association not found with id: {associationId}."`, wrapped as `AssociationValidationException` and logged as an error (SoftDeletion.md rule 2). No route serves the modify: the association orchestration's `ModifyAssociationAsync` reaches it, after authentication and the global block, and answers the refusal as its dependency validation, as it answers a missing association (`AssociationOrchestrationService.Writes.cs`, `AssociationOrchestrationService.Exceptions.cs`).
3. **It is logged before it is thrown**, at information level: `"Content item association modify denied. Content item association {associationId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageAssociationAsync` and before `ValidateStorageAssociationIsNotTerminal`**, in `DoModifyAssociationAsync` (SoftDeletion.md rule 3). So a caller the gate refuses — anyone who is neither the association's owner nor in its publisher tier, the system identity among them — is answered unauthorised on a soft-deleted association as on a live one, and nothing logs the deletion; and a soft-deleted `Approved` or `Rejected` association is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the association's owner, its `CreatedBy`, and its publisher tier — `Publishers`, `Administrators`, and a publisher role matching either endpoint, `<EndpointType>-Publishers` or, on a content item endpoint, `ContentItem-%ContentType%-Publishers` of its content type (`HasPublisherRoleForAssociation`, `AssociationService.Transitions.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectAssociationByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 10, which loses its tag (SoftDeletion.md, *The record*).
8. **It rewrites the lines its build makes untrue**, in the words its task fixes (SoftDeletion.md, *The record*): `ConfidenceReads.md` *The rulings*' and *Risks*' sentences on the refusal, as `ContentItemService.SoftDeletion.md §1` left them, and §APR9.7.2 rule 1's record of where the refusal is not yet built. Each is rewritten to say that the refusal is built on all seven items.

## 2. OnModifyingAssociationAsync — the `Association-Modifying` door (#1343)

```csharp
ValueTask<EventEnvelope<Association>?> OnModifyingAssociationAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted association as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `AssociationValidationException` over `NotFoundAssociationException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing association today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## Deviations

None.
