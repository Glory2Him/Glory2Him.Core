# BibleReference service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `IBibleReferenceService` (`Glory2Him.Core/Services/Foundations/BibleReferences/`). Its other user stories are `Backend/Foundations/BibleReferenceService.md`, `BibleReferenceService.BereanReview.md` and `BibleReferenceService.ConfidenceReads.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, §ARC12.3.1 shared rule 9, SoftDeletion.md rules 1–3 and *The rulings*

`ModifyBibleReferenceAsync` refuses a soft-deleted Bible reference (SoftDeletion.md rule 9). The refusal is asked in `DoModifyBibleReferenceAsync` (`BibleReferenceService.cs`), the body the direct path and its `BibleReference-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*).

**Build order** (SoftDeletion.md, *What ships together*). §1 lands after `Backend/Foundations/ContentItemService.SoftDeletion.md §1`, which rewrites the lines every later refusal keeps true, and before `Backend/Foundations/AssociationService.SoftDeletion.md §1`, the last refusal to land. §2 lands after §1.

## 1. ModifyBibleReferenceAsync — refuse a soft-deleted Bible reference (#1340)

```csharp
ValueTask<BibleReference> ModifyBibleReferenceAsync(
    BibleReference bibleReference,
    CancellationToken cancellationToken = default);
```

1. **A stored Bible reference whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `BibleReference-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing Bible reference is**: `NotFoundBibleReferenceException`, with the message `"Bible reference not found with id: {bibleReferenceId}."`, wrapped as `BibleReferenceValidationException` and logged as an error (SoftDeletion.md rule 2). `PUT api/BibleReferences` answers it `404`, as it answers a missing Bible reference (`BibleReferencesController.cs`).
3. **It is logged before it is thrown**, at information level: `"Bible reference modify denied. Bible reference {bibleReferenceId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageBibleReferenceAsync` and before `ValidateStorageBibleReferenceIsNotTerminal`**, in `DoModifyBibleReferenceAsync` (SoftDeletion.md rule 3). So a caller the gate refuses — anyone who is neither the Bible reference's owner nor in the publisher tier, the system identity among them — is answered unauthorised on a soft-deleted Bible reference as on a live one, and nothing logs the deletion; and a soft-deleted `Approved` or `Rejected` Bible reference is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the Bible reference's owner, its `CreatedBy`, and the publisher tier — `Publishers`, `Administrators` and `BibleReference-Publishers` (`HasPublisherRole`, `BibleReferenceService.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectBibleReferenceByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 9, which loses its tag (SoftDeletion.md, *The record*).

## 2. OnModifyingBibleReferenceAsync — the `BibleReference-Modifying` door (#1341)

```csharp
ValueTask<EventEnvelope<BibleReference>?> OnModifyingBibleReferenceAsync(
    EventEnvelope<BibleReference> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted Bible reference as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `BibleReferenceValidationException` over `NotFoundBibleReferenceException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing Bible reference today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## Deviations

None.
