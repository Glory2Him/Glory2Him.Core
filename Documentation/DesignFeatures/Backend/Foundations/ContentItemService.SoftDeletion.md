# ContentItem service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`). Its other user stories are `Backend/Foundations/ContentItemService.md`, `ContentItemService.Amenders.md`, `ContentItemService.BereanReview.md`, `ContentItemService.ConfidenceReads.md` and `ContentItemService.LiveUpdates.md`.
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.7 posture A rules 1 and 3, §SEC18.6 rule 2, §ARC12.3.1 shared rule 9, SoftDeletion.md rules 1–3, 16 and 18, and *The rulings*

`ModifyContentItemAsync` refuses a soft-deleted content item (SoftDeletion.md rule 4). The refusal is asked in `DoModifyContentItemAsync` (`ContentItemService.cs`), the body the direct path and its `ContentItem-Modifying` door share, so §1 builds it and §2 proves it on the door, with tests that pass as soon as they are written and are committed as pins (`FOUNDATIONS: Pin That …`). Each interface method has a section and a task of its own, as the owner ruled on 2026-10-17 (`ConfidenceReads.md`, *The rulings*). §3 rewrites what the service's code says of its group's published slot (SoftDeletion.md rule 18).

**Build order** (SoftDeletion.md, *What ships together*). §1 is the first refusal to land: its build makes two lines of `ConfidenceReads.md` untrue, and it rewrites them into the words every later refusal keeps true (§1 rule 8). The other six items' refusals land after it. §2 lands after §1. §3 waits for neither.

## 1. ModifyContentItemAsync — refuse a soft-deleted content item (#1330)

```csharp
ValueTask<ContentItem> ModifyContentItemAsync(
    ContentItem contentItem,
    CancellationToken cancellationToken = default);
```

1. **A stored content item whose `IsDeleted` is `true` is refused**, whoever the caller (SoftDeletion.md rule 1). Nothing is written, `ContentItem-Modified` is not published, and no processed event is recorded.
2. **It is answered as a missing content item is**: `NotFoundContentItemException`, with the message `"Content item not found with id: {contentItemId}."`, wrapped as `ContentItemValidationException` and logged as an error (SoftDeletion.md rule 2). `PUT api/ContentItems` binds the content item processing tier, which refuses a soft-deleted content item before it reaches this modify, and keeps doing so (SoftDeletion.md rule 12). This refusal holds the rule at the foundation itself, for its `ContentItem-Modifying` door and for any caller that binds it (§SEC14.6 rules 1 and 2).
3. **It is logged before it is thrown**, at information level: `"Content item modify denied. Content item {contentItemId} is soft-deleted; reported to the caller as not found."` (SoftDeletion.md rule 2).
4. **It is asked after `ValidateUserCanModifyStorageContentItemAsync` and before `ValidateStorageContentItemIsNotTerminal`**, in `DoModifyContentItemAsync` (SoftDeletion.md rule 3). So a caller the gate refuses is answered unauthorised on a soft-deleted content item as on a live one, and nothing logs the deletion: one blocked from the stored content type, which the gate asks first (§SEC18.6 rule 2), and anyone who is neither the item's owner nor in the publisher tier, the system identity among them. A soft-deleted `Approved` or `Rejected` content item is answered not found, not with the terminal refusal.
5. **Every caller the gate admits is refused**: the item's owner, its `CreatedBy`, and the publisher tier — `Publishers`, `Administrators`, `ContentItem-Publishers` and `ContentItem-%ContentType%-Publishers` of the stored `ContentType` (`HasPublisherRole`, `ContentItemService.Validations.cs`).
6. **It reads `IsDeleted` off the stored row** that `SelectContentItemByIdAsync` has already returned, and asks storage nothing more.
7. **It records its build** on SoftDeletion.md rule 4, which loses its tag (SoftDeletion.md, *The record*).
8. **It rewrites the lines its build makes untrue**, in the words its task fixes (SoftDeletion.md, *The record*): in `ConfidenceReads.md`, *The rulings*' *"as each item's general modify writes one today"* and *"That refusal is not yet built."*, and *Risks*' sentence that each `DoModify<Entity>Async` refuses only a missing row today, with the sentence after it. Each is rewritten to words that stay true as each later item's refusal lands, pointing at SoftDeletion.md rules 4–10.

## 2. OnModifyingContentItemAsync — the `ContentItem-Modifying` door (#1331)

```csharp
ValueTask<EventEnvelope<ContentItem>?> OnModifyingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **It refuses a soft-deleted content item as §1 does**, decided against its inbound envelope's security context, since it runs the body §1 changes: it throws `ContentItemValidationException` over `NotFoundContentItemException`, logs as §1 logs, writes nothing, publishes nothing and records no processed event, as it does for a missing content item today.
2. **It adds no code.** Its tests pass as soon as they are written, once §1 is built, and are committed as pins. Its envelope's refusals, its deduplication and its failures are unchanged, and their tests stand.

## 3. What the code says of the published slot (#1346)

No operation: the code text of `ContentItemService` and its unit tests that says, or rests on, a soft-deleted content item holding its group's published slot (SoftDeletion.md rules 16 and 18). It has a section of its own so that its task has a section to name.

1. **The texts** are the unpublish's comment (`ContentItemService.Transitions.cs`) and its summary (`IContentItemService.cs`); the published-sibling probe's comments (`ContentItemService.Lookup.cs`) and its summary (`IContentItemService.cs`); the version read's comment, where it likens a lineage to the published slot (`ContentItemService.Lookup.cs`); the collection read's summary of the questions a tombstone answers (`IContentItemService.cs`); and, in its unit tests, the probe's tests (`ContentItemServiceTests.Lookup.cs`, two test names among them) and the unpublish's (`ContentItemServiceTests.Unpublish.cs`). Its task fixes each text's words, before and after.
2. **What they say instead** (SoftDeletion.md rule 16; §APR9.7.7 rule 7): a tombstone holds no slot, since the slot index excludes deleted rows. It can still carry `IsPublished`, the probe still finds it, and the publication swap's unpublish clears its flag. *"A soft delete never clears IsPublished"* stays wherever a text says it, true until the removal's demotion is built (#1329).
3. **Nothing runs differently.** It changes comments, summaries and test names alone, and each renamed test passes under its new name.

## Deviations

None.
