# Association service — association reads
Parent: [AssociationReads.md](../../AssociationReads.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`), existing
Inherits: §ARC12.2.1 rules 5–7, §DOM4.6 rules 2 and 3, §SEC14.3 rules 1, 2 and 5, §SEC14.5, §SEC14.6 rules 1 and 4, §SEC14.7 posture A′ rules 2, 5, 6 and 7, §EVN17 rule 3, AssociationReads.md rules 2–4

§SEC14.7 posture A′ rule 7 on the foundation's two reads, which is where that rule says it is closed:
the collection filter and the read by id (*How it is closed*). The read by id's event handler
takes it through the body both paths share. Each section below changes one existing member, or
proves one, and leaves its signature, its envelope, its validation and its exception handling as
they are.

**Personal and editorial.** A personal row is one whose `UserId` is set, and an editorial row is
one whose `UserId` is null (§DOM4.2, §DOM4.10). **The owner of a personal row** is the caller
whose user id is its `UserId`, compared as §DOM4.6 rule 3 compares the column. **The review
tier** is posture A′ rule 2's: a global `Reviewers`, `Publishers` or `Administrators`, or a
`%EntityType%-Reviewers` / `-Publishers`, or a `ContentItem-%ContentType%-Reviewers` /
`-Publishers` on a `ContentItem` endpoint, matching either endpoint. Both reads already compose
that tier, the collection read from closed-over sets (posture A′ rules 5 and 6) and the read by
id from the row (`HasReviewRoleForAssociation`), and neither changes it.

## 1. RetrieveAllAssociationsAsync — a personal association to its owner and the review tier alone (#1121)

```csharp
ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
    CancellationToken cancellationToken = default);
```

An existing member, changed: the collection filter (`ApplyCollectionReadVisibilityFilterAsync`)
gains rule 7. The rest of the filter is unchanged: the soft-delete term, the global review
tier's shortcut to every live row, and the scoped review terms with their endpoint-type test.

1. **A personal row is in the set only for its owner and the review tier**, whatever its
   approval status, its `IsPublished` and its `PublishDate`. To every other caller it is not in
   the set, an anonymous caller included, and that holds for a row that is `Approved`,
   published and past its publish date.
2. **The owner term compares `UserId` with the caller's user id inside the query.** It runs in
   SQL, under the column's collation, so a `UserId` that differs from the caller's id in letter
   case alone is the caller's (§DOM4.6 rule 3). Nothing is compared after the read: the
   queryable stays unenumerated (§ARC12.2.1 rule 7).
3. **`CreatedBy` does not make a personal row the caller's.** A personal row whose `CreatedBy`
   is the caller and whose `UserId` is not is not in their set (rule 7 moves the owner from
   `CreatedBy` to `UserId`).
4. **An editorial row is in the set exactly as today**: public, or created by the caller, or
   matching the caller's scoped review tier.
5. **A caller with no user id owns no personal row.** For an anonymous caller, and for a signed-in
   caller whose identity carries a null, empty or whitespace user id, the owner term is not
   composed, so no row is theirs by it (AssociationReads.md rule 4).
6. **A row the caller may not see drops out silently** (§SEC14.5 rule 4). Nothing is logged for
   it.
7. **The filter is proven twice.** The unit tests apply it to an in-memory set (§ARC12.2.1 rule
   5). Against the real catalogue, the integration suite proves that the owner term translates,
   and that it matches a `UserId` stored in another letter case than the caller's id (rule 6):
   an in-memory set compares ordinally and cannot show it.

## 2. RetrieveAssociationByIdAsync — a personal association to its owner and the review tier alone (#1122)

```csharp
ValueTask<Association> RetrieveAssociationByIdAsync(
    Guid associationId,
    CancellationToken cancellationToken = default);
```

An existing member, changed: the shared body, `DoRetrieveAssociationByIdAsync`, gains rule 7.
Its validation of the id, its storage read, and its answer for no row and for a soft-deleted row
are unchanged.

1. **A personal row is answered to its owner and to the review tier**, whatever its approval
   status, its `IsPublished` and its `PublishDate`.
2. **To every other caller a personal row is not found**, an anonymous caller included, and that
   holds for a row that is `Approved`, published and past its publish date. The answer is
   `NotFoundAssociationException` with the message every other miss of this read carries,
   *"Content item association not found with id: {associationId}."*, which names nothing about
   the row (§SEC14.5 rules 1 and 2).
3. **The owner comparison ignores letter case.** The row is in hand, so the comparison is made
   in C#: the caller's user id with the row's `UserId`, ordinally and ignoring case (§DOM4.6
   rule 3).
4. **`CreatedBy` does not make a personal row the caller's** (§1 rule 3).
5. **A caller with no user id owns no personal row** (§1 rule 5).
6. **The denial is logged as a warning before the not-found is thrown** (§SEC14.5 rules 5–7). The
   message names the association's id, says that the row is a personal association, names the
   caller's user id where it was resolved or says that the caller is not authenticated, and says
   that the caller was answered with not-found.
7. **An editorial row is answered exactly as today**, and a soft-deleted row is not found to
   every caller as today, logged as information (§SEC14.5 rules 3 and 6).

## 3. OnRetrievingAssociationByIdAsync — rule 7 on the event door (#1123)

```csharp
ValueTask<EventEnvelope<Association>?> OnRetrievingAssociationByIdAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

An existing member, unchanged: the handler for `Association-RetrievingById`. It converges on
`DoRetrieveAssociationByIdAsync` (`AssociationService.Substrate.cs`), so §2 binds it by
construction, and every rule is enforced on both entry paths (§SEC14.6 rule 4). This section
proves it on this door.

1. **Rule 7 is asked of the inbound envelope's caller.** A personal row is answered to the owner
   and the review tier that the envelope's security context names, and is refused as not found
   to every other caller it names, an anonymous one included, as §2 rules 1 and 2 have it.
2. **No envelope is minted on this path.** The reply is made next from the inbound envelope
   (§EVN17 rule 3), and a not-found is logged and rethrown as the handler rethrows every refusal.
3. **An envelope with no content or no metadata, or one whose signature does not verify for
   `Association-RetrievingById` in the request direction, is refused before anything else is
   asked** (§SEC14.6 rule 4).
4. **Its tests are pins.** §2 puts the rule in the shared body, so they pass as soon as they are
   written, and they are committed as pins.
