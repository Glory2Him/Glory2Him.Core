# Comment service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `ICommentService` (`Glory2Him.Core/Services/Foundations/Comments/`). Its other user stories are `Backend/Foundations/CommentService.md`, `CommentService.BereanReview.md` and `CommentService.ConfidenceReads.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, §ARC12.3.1 shared rule 9, SoftDeletion.md rules 1–3 and *The rulings*

`ModifyCommentAsync` refuses a soft-deleted comment (SoftDeletion.md rule 8). The refusal is asked in `DoModifyCommentAsync` (`CommentService.cs`), the body the direct path and its `Comment-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*).

**Build order** (SoftDeletion.md, *What ships together*). §1 lands after `Backend/Foundations/ContentItemService.SoftDeletion.md §1`, which rewrites the lines every later refusal keeps true, and before `Backend/Foundations/AssociationService.SoftDeletion.md §1`, the last refusal to land. §2 lands after §1.

## 1. ModifyCommentAsync — refuse a soft-deleted comment (#1338)

```csharp
ValueTask<Comment> ModifyCommentAsync(
    Comment comment,
    CancellationToken cancellationToken = default);
```

1. **A stored comment whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `Comment-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing comment is**: `NotFoundCommentException`, with the message `"Comment not found with id: {commentId}."`, wrapped as `CommentValidationException` and logged as an error (SoftDeletion.md rule 2). `PUT api/Comments` answers it `404`, as it answers a missing comment (`CommentsController.cs`).
3. **It is logged before it is thrown**, at information level: `"Comment modify denied. Comment {commentId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageCommentAsync` and before `ValidateStorageCommentIsNotTerminal`**, in `DoModifyCommentAsync` (SoftDeletion.md rule 3). So a caller the gate refuses — anyone who is neither the comment's owner nor in the publisher tier, the system identity among them — is answered unauthorised on a soft-deleted comment as on a live one, and nothing logs the deletion; and a soft-deleted `Approved` or `Rejected` comment is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the comment's owner, its `CreatedBy`, and the publisher tier — `Publishers`, `Administrators` and `Comment-Publishers` (`HasPublisherRole`, `CommentService.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectCommentByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 8, which loses its tag (SoftDeletion.md, *The record*).

## 2. OnModifyingCommentAsync — the `Comment-Modifying` door (#1339)

```csharp
ValueTask<EventEnvelope<Comment>?> OnModifyingCommentAsync(
    EventEnvelope<Comment> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted comment as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `CommentValidationException` over `NotFoundCommentException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing comment today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## Deviations

None.
