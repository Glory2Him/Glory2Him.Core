# Tag service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `ITagService` (`Glory2Him.Core/Services/Foundations/Tags/`). Its other user stories are `Backend/Foundations/TagService.md`, `TagService.BereanReview.md` and `TagService.ConfidenceReads.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, §ARC12.3.1 shared rule 9, SoftDeletion.md rules 1–3 and *The rulings*

`ModifyTagAsync` refuses a soft-deleted tag (SoftDeletion.md rule 6). The refusal is asked in `DoModifyTagAsync` (`TagService.cs`), the body the direct path and its `Tag-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*).

**Build order** (SoftDeletion.md, *What ships together*). §1 lands after `Backend/Foundations/ContentItemService.SoftDeletion.md §1`, which rewrites the lines every later refusal keeps true, and before `Backend/Foundations/AssociationService.SoftDeletion.md §1`, the last refusal to land. §2 lands after §1.

## 1. ModifyTagAsync — refuse a soft-deleted tag (#1334)

```csharp
ValueTask<Tag> ModifyTagAsync(
    Tag tag,
    CancellationToken cancellationToken = default);
```

1. **A stored tag whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `Tag-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing tag is**: `NotFoundTagException`, with the message `"Tag not found with id: {tagId}."`, wrapped as `TagValidationException` and logged as an error (SoftDeletion.md rule 2). `PUT api/Tags` answers it `404`, as it answers a missing tag (`TagsController.cs`).
3. **It is logged before it is thrown**, at information level: `"Tag modify denied. Tag {tagId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageTagAsync` and before `ValidateStorageTagIsNotTerminal`**, in `DoModifyTagAsync` (SoftDeletion.md rule 3). So a caller the gate refuses — anyone who is neither the tag's owner nor in the publisher tier, the system identity among them — is answered unauthorised on a soft-deleted tag as on a live one, and nothing logs the deletion; and a soft-deleted `Approved` or `Rejected` tag is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the tag's owner, its `CreatedBy`, and the publisher tier — `Publishers`, `Administrators` and `Tag-Publishers` (`HasPublisherRole`, `TagService.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectTagByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 6, which loses its tag (SoftDeletion.md, *The record*).

## 2. OnModifyingTagAsync — the `Tag-Modifying` door (#1335)

```csharp
ValueTask<EventEnvelope<Tag>?> OnModifyingTagAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted tag as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `TagValidationException` over `NotFoundTagException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing tag today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## Deviations

None.
