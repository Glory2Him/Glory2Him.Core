# BibleReference service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `IBibleReferenceService` (`Glory2Him.Core/Services/Foundations/BibleReferences/`). Its other user stories are `Backend/Foundations/BibleReferenceService.md` and `BibleReferenceService.BereanReview.md`.
Inherits: §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 2, 3, 4, 7 and 8, §ARC12.2.1, ConfidenceReads.md rules 2–9

A Bible reference's review tier is the global `Reviewers`, `Publishers` and `Administrators`, `BibleReference-Reviewers` and `BibleReference-Publishers`: `HasReviewRole` in `BibleReferenceService.Validations.cs`, the check that admits the review tier to a non-public Bible reference (ConfidenceReads.md rule 2). It takes no row, so a caller holds it for every Bible reference or for none. Each section answers the four as stored to a caller who holds it, and all four as null to every other caller — the owner, any other signed-in caller, an anonymous caller and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). Where an operation has an event door, the door converges on the body the section changes, so the section covers it (ConfidenceReads.md rule 3).

**Build order** (ConfidenceReads.md, *What ships together*). Every section builds on #1154, which gives `BibleReference` the four properties (`Backend/Models/IApproval.md §1`). Until #1196 maps the columns, the stored row reads them back null, so each answers what it answers today. §1 builds on #1196 too, and follows `ContentItemService.ConfidenceReads.md §1`, whose pattern it takes.

## 1. RetrieveAllBibleReferencesAsync — withheld inside the query (#1264)

```csharp
ValueTask<IQueryable<BibleReference>> RetrieveAllBibleReferencesAsync(
    CancellationToken cancellationToken = default);
```

1. **Every row it hands up carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rules 2 and 5). Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), so a `$filter`, an `$orderby` or a `$select` that `GET api/BibleReferences` composes over it reads the withheld value, and runs in SQL with everything beneath it (§ARC12.2.1), as `ContentItemService.ConfidenceReads.md §1` rule 2 has the content item's read do. The storage broker is unchanged.
3. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates.

## 2. RetrieveBibleReferenceByIdAsync — the read by id, its overload and its door (#1265)

```csharp
ValueTask<BibleReference> RetrieveBibleReferenceByIdAsync(
    Guid bibleReferenceId,
    CancellationToken cancellationToken = default);

internal ValueTask<BibleReference> RetrieveBibleReferenceByIdAsync<TSource>(
    Guid bibleReferenceId,
    EventEnvelope<TSource> inboundEnvelope,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<BibleReference>?> OnRetrievingBibleReferenceByIdAsync(
    EventEnvelope<BibleReference> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not**, once its posture has admitted the caller to the row (ConfidenceReads.md rule 2). Which ids answer the row and which answer not-found is unchanged, and so is every other property of the row.
2. **All three converge on `DoRetrieveBibleReferenceByIdAsync`, which applies it**, so each decides it against the security context it decides the row's visibility against: the direct read the ambient caller's, the identity-carrying overload the envelope it is handed, and `BibleReference-RetrievingById` its inbound envelope's. The door's reply carries the answer.

## 3. ModifyBibleReferenceAsync — the modify's answer, and its door (#1266)

```csharp
ValueTask<BibleReference> ModifyBibleReferenceAsync(
    BibleReference bibleReference,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<BibleReference>?> OnModifyingBibleReferenceAsync(
    EventEnvelope<BibleReference> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `BibleReference-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/BibleReferenceService.BereanReview.md §2`, #1195): the withholding is the answer's last step, after both.
3. **`BibleReference-Modifying` converges on `DoModifyBibleReferenceAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 4. RemoveBibleReferenceByIdAsync — the remove's answer, and its door (#1267)

```csharp
ValueTask<BibleReference> RemoveBibleReferenceByIdAsync(
    Guid bibleReferenceId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<BibleReference>?> OnRemovingBibleReferenceByIdAsync(
    EventEnvelope<BibleReference> envelope,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3); `Administrators` is in every tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `BibleReference-Removed` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`BibleReference-RemovingById` converges on `DoRemoveBibleReferenceByIdAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 5. SubmitBibleReferenceByIdAsync — the submit's answer, and its door (#1268)

```csharp
ValueTask<BibleReference> SubmitBibleReferenceByIdAsync(
    Guid bibleReferenceId,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<BibleReference>?> OnSubmittingBibleReferenceAsync(
    EventEnvelope<BibleReference> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The submit admits the owner and the publisher tier (§SEC14.7 posture A rule 3); the publisher tier is in the review tier, and the owner is in it only where they hold it too. A `Draft` can hold a score: a round moved back from `Submitted` keeps it, since a status move alone is no data change (BereanReview.md rule 9).
2. **The row it writes and the `BibleReference-Submitted` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`BibleReference-Submitting` converges on `DoSubmitBibleReferenceAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.
