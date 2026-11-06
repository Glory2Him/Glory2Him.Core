# ContentItem service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`). Its other user stories are `Backend/Foundations/ContentItemService.md`, `ContentItemService.Amenders.md`, `ContentItemService.LiveUpdates.md`, `ContentItemService.BereanReview.md` and `ContentItemService.SoftDeletion.md`.
Inherits: §SEC14.2 rule 1, §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 1, 2, 3, 4, 7 and 8, §SEC18.6 rules 4 and 5, §ARC12.2.1, ConfidenceReads.md rules 2–9 and *The rulings*

A content item's review tier is the global `Reviewers`, `Publishers` and `Administrators`, `ContentItem-Reviewers` and `ContentItem-Publishers`, and the `ContentItem-%ContentType%-Reviewers` and `-Publishers` of the row's own `ContentType` (ConfidenceReads.md rule 2). `HasReviewRole` in `ContentItemService.Validations.cs` asks it of one row, and on a collection `HasBroadReviewRole` answers whether a caller is in every row's tier and `ReviewableContentTypes` which content types' rows a narrow caller's tier covers: the checks that admit the review tier to a non-public content item. Each section but §4 answers the four as stored to a caller in the row's tier, and all four as null to every other caller — the owner, any other signed-in caller, an anonymous caller, a caller with no security context and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). §4, the feed, asks nobody who is reading, so it answers all four as null to every caller (ConfidenceReads.md rule 7). **Each interface method has a section of its own** (ConfidenceReads.md, *The rulings*): a direct path's section changes the body it shares with its overload and its door, and the existing tests that change with it, theirs among them; the overload's section and the door's prove the rule on their own entry, with tests that pass as soon as they are written and are committed as pins.

**Build order** (ConfidenceReads.md, *What ships together* steps 1–4). Every section builds on #1154, which gives `ContentItem` the four properties (`Backend/Models/IApproval.md §1`), and on the content item's add, #1184 (`Backend/Foundations/ContentItemService.BereanReview.md §1`), which has `CreateContentItemFiller` set the four null for every test, so an existing test's row holds a score only where the test seeds one: the tests the content item's modify (#1185) and its door's pin (#1312) add seed them on the row they modify, and change with §5, which builds on both (ConfidenceReads.md, *What ships together* step 1). Until #1196 maps the columns, the stored row reads them back null, so each answers what it answers today. §1 builds on #1196 too, and is the first collection read: the other six foundations' collection reads and the two processing tiers' public reads follow its pattern. Those six rest on its proof of the association orchestration's endpoint composite (rule 4 there), which composes over the association's collection read and splices each item's into it. The two public reads do not: the composite splices no processing read, so each carries its own proof in SQL Server (`Backend/Processings/ContentItemProcessingService.ConfidenceReads.md §1` rule 4, `Backend/Processings/LinkProcessingService.ConfidenceReads.md §1` rule 4). The processing tier's public read is `Backend/Processings/ContentItemProcessingService.ConfidenceReads.md §1`. The overload's section (§8) follows the read by id's (§2), and each door's section follows its direct path's.

## 1. RetrieveAllContentItemsAsync — withheld inside the query (#1234)

```csharp
ValueTask<IQueryable<ContentItem>> RetrieveAllContentItemsAsync(
    CancellationToken cancellationToken = default);
```

1. **Each row it hands up carries the four as stored where the caller is in that row's tier, and all four as null where they are not** (ConfidenceReads.md rules 2 and 5). A caller holding a role `HasBroadReviewRole` names is in every row's tier. A caller holding a narrow `ContentItem-%ContentType%-Reviewers` or `-Publishers` is in the tier of that content type's rows alone, as `ReviewableContentTypes` composes them. Every other caller is in no row's tier. Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), composed over the storage broker's collection read as its visibility filter is composed today, so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1): the processing tier's visibility filter, its public read's (`Backend/Processings/ContentItemProcessingService.ConfidenceReads.md §1`), the association orchestration's endpoint composite, which splices it into the association query as a correlated sub-query (§SEC14.3; ConfidenceReads.md rule 5), and the `$filter`, `$orderby` or `$select` that `GET api/ContentItems` composes over the processing tier's collection read. The storage broker is unchanged.
3. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates, nor that a condition composed over it on any of its properties still does. Each proof against SQL Server runs the composed query through EF Core's asynchronous operator, which a read materialised in memory cannot serve.
4. **It proves the association orchestration's endpoint composite in SQL Server** (ConfidenceReads.md, *What ships together* step 3), for a caller holding `ContentItem-Testimony-Reviewers` alone, over associations whose endpoints span all six endpoint types. The composite's one proof today runs as the global three (`AssociationCompositeReadTests`), for whom no endpoint read withholds; under this caller each of them does, the content item's by a tier that differs row by row and the other five's for every row, so the proof shows each endpoint read's withholding inlining into the association query from the moment that read is built.

## 2. RetrieveContentItemByIdAsync — the read by id (#1235)

```csharp
ValueTask<ContentItem> RetrieveContentItemByIdAsync(
    Guid contentItemId,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier — `HasReviewRole` over its `ContentType` — and all four as null where they do not**, once its posture has admitted the caller to the row, on the public row it answers before it reads any security context as on a row it admits by the context (ConfidenceReads.md rule 2). A missing security context is answered as one that holds no role. Which ids answer the row and which answer not-found is unchanged, and so is every other property of the row.
2. **`DoRetrieveContentItemByIdAsync` applies it**, the body this method shares with its identity-carrying overload (§8) and its door (§9), so each decides it against the security context it decides the row's visibility against: this method the ambient caller's, through the envelope it mints.
3. **A withheld answer is a copy** (ConfidenceReads.md rule 9), so the row storage returned keeps its four. The processing tier's modify reads the row through this read before it hands the foundation's modify a row (`ContentItemProcessingService.cs`), and that modify reads the stored row again for the four (`Backend/Foundations/ContentItemService.BereanReview.md §2`).

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

## 5. ModifyContentItemAsync — the modify's answer (#1238)

```csharp
ValueTask<ContentItem> ModifyContentItemAsync(
    ContentItem contentItem,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `ContentItem-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/ContentItemService.BereanReview.md §2`, #1185): it withholds them from its answer alone, after both.
3. **`DoModifyContentItemAsync` applies it**, the body this method shares with its door (§10).

## 6. RemoveContentItemByIdAsync — the remove's answer (#1239)

```csharp
ValueTask<ContentItem> RemoveContentItemByIdAsync(
    Guid contentItemId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3); `Administrators` is in every tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `ContentItem-Removed` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The already-removed return withholds too** (ConfidenceReads.md rule 8). `DoRemoveContentItemByIdAsync` answers a row that is already soft-deleted as it is stored, after its permission check and before any write or fact, which the processing tier's remove never reaches, because its read by id answers a removed row not-found first, and which the door reaches. That answer carries the four by the same tier, and nothing is written or published.
4. **`DoRemoveContentItemByIdAsync` applies it**, the body this method shares with its door (§11).

## 7. SubmitContentItemByIdAsync — the submit's answer (#1240)

```csharp
ValueTask<ContentItem> SubmitContentItemByIdAsync(
    Guid contentItemId,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The submit admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too. A `Draft` can hold a score: a round moved back from `Submitted` keeps it, since a status move alone is no data change (BereanReview.md rule 9).
2. **The row it writes and the `ContentItem-Submitted` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The withholding is the submit's own** (ConfidenceReads.md rule 8). `DoSubmitContentItemAsync` saves through `SaveTransitionAsync`, the tail it shares with the approval transition, which the system identity calls and which answers the four as stored (ConfidenceReads.md rule 4). So the submit withholds after the tail returns, and the tail answers what it saved.
4. **`DoSubmitContentItemAsync` applies it**, the body this method shares with its door (§12).

## 8. RetrieveContentItemByIdAsync — the identity-carrying overload (#1276)

```csharp
internal ValueTask<ContentItem> RetrieveContentItemByIdAsync<TSource>(
    Guid contentItemId,
    EventEnvelope<TSource> inboundEnvelope,
    CancellationToken cancellationToken = default);
```

1. **It answers the four by the security context of the envelope it is handed, never the ambient caller's.** It chains its read envelope off that one (`CreateNextAsync`), which carries its security context forward, and converges on `DoRetrieveContentItemByIdAsync`, which §2 changes (ConfidenceReads.md rule 2).
2. **It adds no code.** Its tests pass as soon as they are written, once §2 is built, and are committed as pins (ConfidenceReads.md, *The rulings*).

## 9. OnRetrievingContentItemByIdAsync — the `ContentItem-RetrievingById` door (#1277)

```csharp
ValueTask<EventEnvelope<ContentItem>?> OnRetrievingContentItemByIdAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §2**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **A null security context is answered as one that holds no role.** The door can be handed an envelope whose security context is null, and answers a public row before it asks any (ConfidenceReads.md rule 2), so it replies with that row's four as null.
3. **It adds no code.** Its tests pass as soon as they are written, once §2 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals and its failures are unchanged, and their tests stand.

## 10. OnModifyingContentItemAsync — the `ContentItem-Modifying` door (#1278)

```csharp
ValueTask<EventEnvelope<ContentItem>?> OnModifyingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §5**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **It adds no code.** Its tests pass as soon as they are written, once §5 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.

## 11. OnRemovingContentItemByIdAsync — the `ContentItem-RemovingById` door (#1279)

```csharp
ValueTask<EventEnvelope<ContentItem>?> OnRemovingContentItemByIdAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §6**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **The already-removed return replies by the same rule** (§6 rule 3): nothing is written or published, and the reply carries the stored row with the four by the same tier.
3. **It adds no code.** Its tests pass as soon as they are written, once §6 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.

## 12. OnSubmittingContentItemAsync — the `ContentItem-Submitting` door (#1280)

```csharp
ValueTask<EventEnvelope<ContentItem>?> OnSubmittingContentItemAsync(
    EventEnvelope<ContentItem> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §7**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **It adds no code.** Its tests pass as soon as they are written, once §7 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.
