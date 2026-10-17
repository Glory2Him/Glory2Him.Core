# Link service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `ILinkService` (`Glory2Him.Core/Services/Foundations/Links/`). Its other user stories are `Backend/Foundations/LinkService.md` and `LinkService.BereanReview.md`.
Inherits: §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 2, 3, 4, 7 and 8, §ARC12.2.1, ConfidenceReads.md rules 2–9

A link's review tier is the global `Reviewers`, `Publishers` and `Administrators`, `Link-Reviewers` and `Link-Publishers`: `HasReviewRole` in `LinkService.Validations.cs`, the check that admits the review tier to a non-public link (ConfidenceReads.md rule 2). It takes no row, so a caller holds it for every link or for none. Each section answers the four as stored to a caller who holds it, and all four as null to every other caller — the owner, any other signed-in caller, an anonymous caller and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). Where an operation has an event door, the door converges on the body the section changes, so the section covers it (ConfidenceReads.md rule 3).

**Build order** (ConfidenceReads.md, *What ships together*). Every section builds on #1154, which gives `Link` the four properties (`Backend/Models/IApproval.md §1`). Until #1196 maps the columns, the stored row reads them back null, so each answers what it answers today. §1 builds on #1196 too, and follows `ContentItemService.ConfidenceReads.md §1`, whose pattern it takes. The link processing tier's public read is `Backend/Processings/LinkProcessingService.ConfidenceReads.md §1`.

## 1. RetrieveAllLinksAsync — withheld inside the query (#1242)

```csharp
ValueTask<IQueryable<Link>> RetrieveAllLinksAsync(
    CancellationToken cancellationToken = default);
```

1. **Every row it hands up carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rules 2 and 5). Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1), as `ContentItemService.ConfidenceReads.md §1` rule 2 has the content item's read do: the link processing tier's visibility filter, and the `$filter`, `$orderby` or `$select` that `GET api/Links` composes over the processing tier's collection read. The storage broker is unchanged.
3. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates.

## 2. RetrieveLinkByIdAsync — the read by id, its overload and its door (#1243)

```csharp
ValueTask<Link> RetrieveLinkByIdAsync(
    Guid linkId,
    CancellationToken cancellationToken = default);

internal ValueTask<Link> RetrieveLinkByIdAsync<TSource>(
    Guid linkId,
    EventEnvelope<TSource> inboundEnvelope,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Link>?> OnRetrievingLinkByIdAsync(
    EventEnvelope<Link> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not**, once its posture has admitted the caller to the row (ConfidenceReads.md rule 2). Which ids answer the row and which answer not-found is unchanged, and so is every other property of the row.
2. **All three converge on `DoRetrieveLinkByIdAsync`, which applies it**, so each decides it against the security context it decides the row's visibility against: the direct read the ambient caller's, the identity-carrying overload the envelope it is handed, and `Link-RetrievingById` its inbound envelope's. The door's reply carries the answer.

## 3. RetrieveLinksByGroupIdAsync — a group's rows (#1244)

```csharp
ValueTask<IReadOnlyList<Link>> RetrieveLinksByGroupIdAsync(
    Guid groupId,
    CancellationToken cancellationToken = default);
```

1. **Every row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not**, as §1 rule 1 decides it.
2. **It withholds over the rows it has materialised**, after the visibility filter it shares with §1, and answers the same rows in the same order. Where §1's build has that shared filter withhold, this read takes the rule from it, and this section's proof pins it.
3. **The link processing tier's group, latest and published reads compose over it**, served at `GET api/Links/Groups/{groupId}`, `…/Latest` and `…/Published`, and take the rule from it (ConfidenceReads.md rule 3).

## 4. ModifyLinkAsync — the modify's answer, and its door (#1245)

```csharp
ValueTask<Link> ModifyLinkAsync(
    Link link,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Link>?> OnModifyingLinkAsync(
    EventEnvelope<Link> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Link-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/LinkService.BereanReview.md §2`, #1187): the withholding is the answer's last step, after both.
3. **`Link-Modifying` converges on `DoModifyLinkAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 5. RemoveLinkByIdAsync — the remove's answer, and its door (#1246)

```csharp
ValueTask<Link> RemoveLinkByIdAsync(
    Guid linkId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Link>?> OnRemovingLinkByIdAsync(
    EventEnvelope<Link> envelope,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3); `Administrators` is in every tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Link-Removed` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`Link-RemovingById` converges on `DoRemoveLinkByIdAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 6. SubmitLinkByIdAsync — the submit's answer, and its door (#1247)

```csharp
ValueTask<Link> SubmitLinkByIdAsync(
    Guid linkId,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Link>?> OnSubmittingLinkAsync(
    EventEnvelope<Link> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The submit admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too. A `Draft` can hold a score: a round moved back from `Submitted` keeps it, since a status move alone is no data change (BereanReview.md rule 9).
2. **The row it writes and the `Link-Submitted` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`Link-Submitting` converges on `DoSubmitLinkAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.
