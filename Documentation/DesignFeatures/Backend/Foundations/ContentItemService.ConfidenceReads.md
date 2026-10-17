# ContentItem service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`). Its other user stories are `Backend/Foundations/ContentItemService.md`, `ContentItemService.Amenders.md`, `ContentItemService.LiveUpdates.md` and `ContentItemService.BereanReview.md`.
Inherits: §SEC14.2 rule 1, §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 1, 2, 3, 4, 7 and 8, §SEC18.6 rules 4 and 5, §ARC12.2.1, ConfidenceReads.md rules 2–9

A content item's review tier is the global `Reviewers`, `Publishers` and `Administrators`, `ContentItem-Reviewers` and `ContentItem-Publishers`, and the `ContentItem-%ContentType%-Reviewers` and `-Publishers` of the row's own `ContentType` (ConfidenceReads.md rule 2). `HasReviewRole` in `ContentItemService.Validations.cs` asks it of one row, and on a collection `HasBroadReviewRole` answers whether a caller is in every row's tier and `ReviewableContentTypes` which content types' rows a narrow caller's tier covers: the checks that admit the review tier to a non-public content item. Each section answers the four as stored to a caller in the row's tier, and all four as null to every other caller — the owner, any other signed-in caller, an anonymous caller and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). Where an operation has an event door, the door converges on the body the section changes, so the section covers it (ConfidenceReads.md rule 3).

**Build order** (ConfidenceReads.md, *What ships together*). Every section builds on #1154, which gives `ContentItem` the four properties (`Backend/Models/IApproval.md §1`). Until #1196 maps the columns, the stored row reads them back null, so each answers what it answers today. §1 builds on #1196 too, and is the first collection read: the other six foundations' collection reads and the two processing tiers' public reads follow its pattern. The processing tier's public read is `Backend/Processings/ContentItemProcessingService.ConfidenceReads.md §1`.

## 1. RetrieveAllContentItemsAsync — withheld inside the query (#1234)

```csharp
ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
    CancellationToken cancellationToken = default);
```

1. **Each row it hands up carries the four as stored where the caller is in that row's tier, and all four as null where they are not** (ConfidenceReads.md rules 2 and 5). A caller holding a role `HasBroadReviewRole` names is in every row's tier. A caller holding a narrow `ContentItem-%ContentType%-Reviewers` or `-Publishers` is in the tier of that content type's rows alone, as `ReviewableContentTypes` composes them. Every other caller is in no row's tier. Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), composed over the storage broker's collection read as its visibility filter is composed today, so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1): the processing tier's visibility filter, its public read's (`Backend/Processings/ContentItemProcessingService.ConfidenceReads.md §1`), and the `$filter`, `$orderby` or `$select` that `GET api/ContentItems` composes over the processing tier's collection read. The storage broker is unchanged.
3. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates, nor that a condition composed over it on any of its properties still does.

## 2. RetrieveContentItemByIdAsync — the read by id, its overload and its door (#1235)

```csharp
ValueTask<ContentItem> RetrieveContentItemByIdAsync(
    Guid contentItemId,
    CancellationToken cancellationToken = default);

internal ValueTask<ContentItem> RetrieveContentItemByIdAsync<TSource>(
    Guid contentItemId,
    EventEnvelope<TSource> inboundEnvelope,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<ContentItem>?> OnRetrievingContentItemByIdAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier — `HasReviewRole` over its `ContentType` — and all four as null where they do not**, once its posture has admitted the caller to the row (ConfidenceReads.md rule 2). Which ids answer the row and which answer not-found is unchanged, and so is every other property of the row.
2. **All three converge on `DoRetrieveContentItemByIdAsync`, which applies it**, so each decides it against the security context it decides the row's visibility against: the direct read the ambient caller's, the identity-carrying overload the envelope it is handed, and `ContentItem-RetrievingById` its inbound envelope's. The door's reply carries the answer.

## 3. RetrieveContentItemsByGroupIdAsync — a group's rows (#1236)

```csharp
ValueTask<IReadOnlyList<ContentItem>> RetrieveContentItemsByGroupIdAsync(
    Guid groupId,
    CancellationToken cancellationToken = default);
```

1. **Each row it answers carries the four by its own tier**, as §1 rule 1 decides it. A group's rows share one content type, which is create-only and which a fork carries unchanged (§SEC14.7 posture A rule 1), so a narrow caller is in the tier of every row of a group or of none.
2. **It withholds over the rows it has materialised**, after the visibility filter it shares with §1 and before it orders them, and answers the same rows in the same order. Where §1's build has that shared filter withhold, this read takes the rule from it, and this section's proof pins it.
3. **The processing tier's group, latest and published reads compose over it**, served at `GET api/ContentItems/Groups/{groupId}`, `…/Latest` and `…/Published`, and take the rule from it (ConfidenceReads.md rule 3).

## 4. RetrieveContentItemFeedAsync — the feed answers no confidence (#1237)

```csharp
ValueTask<IReadOnlyList<ContentItem>> RetrieveContentItemFeedAsync(
    int skip,
    int take,
    CancellationToken cancellationToken = default);
```

1. **Every row of the page carries all four as null, whoever asks** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 7). The feed mints no envelope and resolves no caller, and goes on doing neither: an administrator is answered what an anonymous visitor is (§SEC14.2 rule 1).
2. **Its rows, their order and every other property of each are as today.** The processing tier's feed, served at `GET api/ContentItems/Feed`, passes it through and takes the rule from it.

## 5. ModifyContentItemAsync — the modify's answer, and its door (#1238)

```csharp
ValueTask<ContentItem> ModifyContentItemAsync(
    ContentItem contentItem,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<ContentItem>?> OnModifyingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `ContentItem-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/ContentItemService.BereanReview.md §2`, #1185): the withholding is the answer's last step, after both.
3. **`ContentItem-Modifying` converges on `DoModifyContentItemAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 6. RemoveContentItemByIdAsync — the remove's answer, and its door (#1239)

```csharp
ValueTask<ContentItem> RemoveContentItemByIdAsync(
    Guid contentItemId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<ContentItem>?> OnRemovingContentItemByIdAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3); `Administrators` is in every tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `ContentItem-Removed` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`ContentItem-RemovingById` converges on `DoRemoveContentItemByIdAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 7. SubmitContentItemByIdAsync — the submit's answer, and its door (#1240)

```csharp
ValueTask<ContentItem> SubmitContentItemByIdAsync(
    Guid contentItemId,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<ContentItem>?> OnSubmittingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The submit admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too. A `Draft` can hold a score: a round moved back from `Submitted` keeps it, since a status move alone is no data change (BereanReview.md rule 9).
2. **The row it writes and the `ContentItem-Submitted` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`ContentItem-Submitting` converges on `DoSubmitContentItemAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.
