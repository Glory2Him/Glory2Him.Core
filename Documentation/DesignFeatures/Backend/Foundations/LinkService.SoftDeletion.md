# Link service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `ILinkService` (`Glory2Him.Core/Services/Foundations/Links/`). Its other user stories are `Backend/Foundations/LinkService.md`, `LinkService.BereanReview.md` and `LinkService.ConfidenceReads.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, §ARC12.3.1 shared rule 9, SoftDeletion.md rules 1–3 and *The rulings*

`ModifyLinkAsync` refuses a soft-deleted link (SoftDeletion.md rule 5). The refusal is asked in `DoModifyLinkAsync` (`LinkService.cs`), the body the direct path and its `Link-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*).

**Build order** (SoftDeletion.md, *What ships together*). §1 lands after `Backend/Foundations/ContentItemService.SoftDeletion.md §1`, which rewrites the lines every later refusal keeps true, and before `Backend/Foundations/AssociationService.SoftDeletion.md §1`, the last refusal to land. §2 lands after §1.

## 1. ModifyLinkAsync — refuse a soft-deleted link (#1332)

```csharp
ValueTask<Link> ModifyLinkAsync(
    Link link,
    CancellationToken cancellationToken = default);
```

1. **A stored link whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `Link-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing link is**: `NotFoundLinkException`, with the message `"Link not found with id: {linkId}."`, wrapped as `LinkValidationException` and logged as an error (SoftDeletion.md rule 2). `PUT api/Links` binds the link processing tier, which refuses a soft-deleted link before it reaches this modify, and keeps doing so (SoftDeletion.md rule 12). This refusal holds the rule at the foundation itself, for its `Link-Modifying` door and for any caller that binds it (§SEC14.6 rules 1 and 2).
3. **It is logged before it is thrown**, at information level: `"Link modify denied. Link {linkId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageLinkAsync` and before `ValidateStorageLinkIsNotTerminal`**, in `DoModifyLinkAsync` (SoftDeletion.md rule 3). So a caller the gate refuses — anyone who is neither the link's owner nor in the publisher tier, the system identity among them — is answered unauthorised on a soft-deleted link as on a live one, and nothing logs the deletion; and a soft-deleted `Approved` or `Rejected` link is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the link's owner, its `CreatedBy`, and the publisher tier — `Publishers`, `Administrators` and `Link-Publishers` (`HasPublisherRole`, `LinkService.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectLinkByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 5, which loses its tag (SoftDeletion.md, *The record*).

## 2. OnModifyingLinkAsync — the `Link-Modifying` door (#1333)

```csharp
ValueTask<EventEnvelope<Link>?> OnModifyingLinkAsync(
    EventEnvelope<Link> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted link as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `LinkValidationException` over `NotFoundLinkException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing link today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## Deviations

None.
