# Tag service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `ITagService` (`Glory2Him.Core/Services/Foundations/Tags/`). Its other user stories are `Backend/Foundations/TagService.md` and `TagService.BereanReview.md`.
Inherits: §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 2, 3, 4, 7 and 8, §ARC12.2.1, ConfidenceReads.md rules 2–9 and *The rulings*

A tag's review tier is the global `Reviewers`, `Publishers` and `Administrators`, `Tag-Reviewers` and `Tag-Publishers`: `HasReviewRole` in `TagService.Validations.cs`, the check that admits the review tier to a non-public tag (ConfidenceReads.md rule 2). It takes no row, so a caller holds it for every tag or for none. Each section answers the four as stored to a caller who holds it, and all four as null to every other caller — the owner, any other signed-in caller, an anonymous caller, a caller with no security context and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). **Each interface method has a section of its own** (ConfidenceReads.md, *The rulings*): a direct path's section changes the body it shares with its overload and its door, and the existing tests that change with it, theirs among them; the overload's section and the door's prove the rule on their own entry, with tests that pass as soon as they are written and are committed as pins.

**Build order** (ConfidenceReads.md, *What ships together* steps 1–4). Every section builds on #1154, which gives `Tag` the four properties (`Backend/Models/IApproval.md §1`), and on the tag's add, #1188 (`Backend/Foundations/TagService.BereanReview.md §1`), which has `CreateTagFiller` leave the four null, so no existing test's row holds a score. Until #1196 maps the columns, the stored row reads them back null, so each answers what it answers today. §1 builds on #1196 too, and follows `ContentItemService.ConfidenceReads.md §1`, whose pattern it takes and whose proof of the association orchestration's endpoint composite it rests on. The overload's section (§6) follows the read by id's (§2), and each door's section follows its direct path's.

## 1. RetrieveAllTagsAsync — withheld inside the query (#1249)

```csharp
ValueTask<IQueryable<Tag>> RetrieveAllTagsAsync(
    CancellationToken cancellationToken = default);
```

1. **Every row it hands up carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rules 2 and 5). Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), as `ContentItemService.ConfidenceReads.md §1` rule 2 has the content item's read do, so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1): the association orchestration's endpoint composite, which splices it into the association query as a correlated sub-query (§SEC14.3; ConfidenceReads.md rule 5), and the `$filter`, `$orderby` or `$select` that `GET api/Tags` composes over it. The storage broker is unchanged.
3. **Its proof in SQL Server is the endpoint composite's** (`ContentItemService.ConfidenceReads.md §1` rule 4). Its tier takes no row, so for a caller outside it the read withholds every row alike, a shape whose translation that section's own proof shows, and no proof here repeats it. What is this read's own is that it stays an EF query: the composite's proof splices it into the association query for a caller outside this tier, and runs that query through EF Core's asynchronous operator, which a read materialised in memory cannot serve, so it shows this read translating from the moment it withholds.

## 2. RetrieveTagByIdAsync — the read by id (#1250)

```csharp
ValueTask<Tag> RetrieveTagByIdAsync(
    Guid tagId,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier — `HasReviewRole` — and all four as null where they do not**, once its posture has admitted the caller to the row, on the public row it answers before it reads any security context as on a row it admits by the context (ConfidenceReads.md rule 2). A missing security context is answered as one that holds no role. Which ids answer the row and which answer not-found is unchanged, and so is every other property of the row.
2. **`DoRetrieveTagByIdAsync` applies it**, the body this method shares with its identity-carrying overload (§6) and its door (§7), so each decides it against the security context it decides the row's visibility against: this method the ambient caller's, through the envelope it mints.
3. **A withheld answer is a copy** (ConfidenceReads.md rule 9), so the row storage returned keeps its four.

## 3. ModifyTagAsync — the modify's answer (#1251)

```csharp
ValueTask<Tag> ModifyTagAsync(
    Tag tag,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Tag-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/TagService.BereanReview.md §2`, #1189): it withholds them from its answer alone, after both.
3. **`DoModifyTagAsync` applies it**, the body this method shares with its door (§8).

## 4. RemoveTagByIdAsync — the remove's answer (#1252)

```csharp
ValueTask<Tag> RemoveTagByIdAsync(
    Guid tagId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3); `Administrators` is in every tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Tag-Removed` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The already-removed return withholds too** (ConfidenceReads.md rule 8). `DoRemoveTagByIdAsync` answers a row that is already soft-deleted as it is stored, after its permission check and before any write or fact, which a second `DELETE api/Tags/{tagId}` reaches, since the controller binds this foundation. That answer carries the four by the same tier, and nothing is written or published.
4. **`DoRemoveTagByIdAsync` applies it**, the body this method shares with its door (§9).

## 5. SubmitTagByIdAsync — the submit's answer (#1253)

```csharp
ValueTask<Tag> SubmitTagByIdAsync(
    Guid tagId,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The submit admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too. A `Draft` can hold a score: a round moved back from `Submitted` keeps it, since a status move alone is no data change (BereanReview.md rule 9).
2. **The row it writes and the `Tag-Submitted` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The withholding is the submit's own** (ConfidenceReads.md rule 8). `DoSubmitTagAsync` saves through `SaveTransitionAsync`, the tail it shares with the approval transition, which the system identity calls and which answers the four as stored (ConfidenceReads.md rule 4). So the submit withholds after the tail returns, and the tail answers what it saved.
4. **`DoSubmitTagAsync` applies it**, the body this method shares with its door (§10).

## 6. RetrieveTagByIdAsync — the identity-carrying overload (#1286)

```csharp
internal ValueTask<Tag> RetrieveTagByIdAsync<TSource>(
    Guid tagId,
    EventEnvelope<TSource> inboundEnvelope,
    CancellationToken cancellationToken = default);
```

1. **It answers the four by the security context of the envelope it is handed, never the ambient caller's.** It chains its read envelope off that one (`CreateNextAsync`), which carries its security context forward, and converges on `DoRetrieveTagByIdAsync`, which §2 changes (ConfidenceReads.md rule 2).
2. **It adds no code.** Its tests pass as soon as they are written, once §2 is built, and are committed as pins (ConfidenceReads.md, *The rulings*).

## 7. OnRetrievingTagByIdAsync — the `Tag-RetrievingById` door (#1287)

```csharp
ValueTask<EventEnvelope<Tag>?> OnRetrievingTagByIdAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §2**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **A null security context is answered as one that holds no role.** The door can be handed an envelope whose security context is null, and answers a public row before it asks any (ConfidenceReads.md rule 2), so it replies with that row's four as null.
3. **It adds no code.** Its tests pass as soon as they are written, once §2 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals and its failures are unchanged, and their tests stand.

## 8. OnModifyingTagAsync — the `Tag-Modifying` door (#1288)

```csharp
ValueTask<EventEnvelope<Tag>?> OnModifyingTagAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §3**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **It adds no code.** Its tests pass as soon as they are written, once §3 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.

## 9. OnRemovingTagByIdAsync — the `Tag-RemovingById` door (#1289)

```csharp
ValueTask<EventEnvelope<Tag>?> OnRemovingTagByIdAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §4**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **The already-removed return replies by the same rule** (§4 rule 3): nothing is written or published, and the reply carries the stored row with the four by the same tier.
3. **It adds no code.** Its tests pass as soon as they are written, once §4 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.

## 10. OnSubmittingTagAsync — the `Tag-Submitting` door (#1290)

```csharp
ValueTask<EventEnvelope<Tag>?> OnSubmittingTagAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §5**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **It adds no code.** Its tests pass as soon as they are written, once §5 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.
