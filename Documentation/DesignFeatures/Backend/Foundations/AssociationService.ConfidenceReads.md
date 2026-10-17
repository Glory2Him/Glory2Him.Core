# Association service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`). Its other user stories are `Backend/Foundations/AssociationService.md`, `AssociationService.AssociationReads.md` and `AssociationService.BereanReview.md`.
Inherits: §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rule 8, posture A′ rules 1, 2, 3, 5, 6 and 7, §ARC12.2.1, ConfidenceReads.md rules 2–9 and 11

An association's review tier is the global `Reviewers`, `Publishers` and `Administrators`, or a coarse or narrow review or publisher role matching either endpoint, the endpoint's type tested beside its content type (§SEC14.7 posture A′ rules 2, 5 and 6; ConfidenceReads.md rule 2). `HasReviewRoleForAssociation` in `AssociationService.Validations.cs` asks it of one row, and on a collection `HasGlobalReviewRole` answers whether a caller is in every row's tier and `ResolveReviewableEntityTypes` and `ResolveReviewableContentTypes` in `AssociationService.cs` which rows a scoped caller's tier covers: the checks that admit the review tier to a non-public association. Each section answers the four as stored to a caller in the row's tier, and all four as null to every other caller — the owner, its `CreatedBy` or on a personal row its `UserId` (posture A′ rule 7), any other signed-in caller, an anonymous caller and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). Where an operation has an event door, the door converges on the body the section changes, so the section covers it (ConfidenceReads.md rule 3).

**Build order** (ConfidenceReads.md, *What ships together* steps 4 and 5). An association's four are stored already. Every section builds on #1229, which has the modify take them from the stored row in place of comparing them (`Backend/Foundations/AssociationService.BereanReview.md §3`): before it, a caller outside the tier who read a withheld association and sent it back on a modify would be refused for four values they never touched (ConfidenceReads.md rule 9). §1 and §2 change the bodies `AssociationService.AssociationReads.md §1–§3` change (#1121, #1122, #1123), and whichever lands second rebases on the first. **§1 lands last of this sub-feature's tasks**, and records the rule as built.

## 1. RetrieveAllAssociationsAsync — withheld inside the query, and the rule recorded as built (#1273)

```csharp
ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
    CancellationToken cancellationToken = default);
```

1. **Each row it hands up carries the four as stored where the caller is in that row's tier, and all four as null where they are not** (ConfidenceReads.md rules 2 and 5). A caller holding a role `HasGlobalReviewRole` names is in every row's tier. A caller holding a scoped role is in the tier of the rows one of whose endpoints it matches: by the endpoint's type, for a role in `ResolveReviewableEntityTypes`, or by a `ContentItem` endpoint's content type, for a role in `ResolveReviewableContentTypes`, the endpoint's type tested beside its content type on both endpoints (posture A′ rule 6). Every other caller is in no row's tier. Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), as `ContentItemService.ConfidenceReads.md §1` rule 2 has the content item's read do, so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1): the orchestration's endpoint composite (§SEC14.3), and the `$filter`, `$orderby` or `$select` that `GET api/associations` will compose over the orchestration's read (#1124). The storage broker is unchanged.
3. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates.
4. **It lands after every other section of `ConfidenceReads.md`'s user stories**, and records the rule as built in its own pull request (`Design/design.md`, *A gap that is built stays in its list*, point 5): it is the read whose build leaves no operation of ConfidenceReads.md rule 3 answering the four to a caller outside the tier. Its task fixes the words.

## 2. RetrieveAssociationByIdAsync — the read by id and its door (#1269)

```csharp
ValueTask<Association> RetrieveAssociationByIdAsync(
    Guid associationId,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Association>?> OnRetrievingAssociationByIdAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier — `HasReviewRoleForAssociation` — and all four as null where they do not**, once its posture has admitted the caller to the row (ConfidenceReads.md rule 2). Which ids answer the row and which answer not-found is unchanged, posture A′ rule 7's owner of a personal row included (#1122), and so is every other property of the row.
2. **Both converge on `DoRetrieveAssociationByIdAsync`, which applies it**, so each decides it against the security context it decides the row's visibility against: the direct read the ambient caller's, and `Association-RetrievingById` its inbound envelope's. The door's reply carries the answer.

## 3. ModifyAssociationAsync — the modify's answer, and its door (#1270)

```csharp
ValueTask<Association> ModifyAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Association>?> OnModifyingAssociationAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the endpoint-derived publisher tier (§SEC14.7 posture A′ rule 2); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Association-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/AssociationService.BereanReview.md §3`, #1229): the withholding is the answer's last step, after both.
3. **`Association-Modifying` converges on `DoModifyAssociationAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 4. RemoveAssociationByIdAsync — the remove's answer, and its door (#1271)

```csharp
ValueTask<Association> RemoveAssociationByIdAsync(
    Guid associationId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);

ValueTask<EventEnvelope<Association>?> OnRemovingAssociationByIdAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3), and a reader withdrawing their own reaction (posture A′ rule 1); `Administrators` is in every tier, and the owner and the reader are in it only where they hold it too.
2. **The row it writes and the `Association-Removed` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **`Association-RemovingById` converges on `DoRemoveAssociationByIdAsync`**, so its reply carries the answer, decided against its inbound envelope's security context.

## 5. SortAssociationAsync — the sort's answer (#1272)

```csharp
ValueTask<Association> SortAssociationAsync(
    Association association,
    Association anchorAssociation,
    SortPosition position,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The sort admits the owner and `Administrators` (`ValidateUserCanSortStorageAssociationAsync`); `Administrators` is in every tier, and the owner is in it only where they hold it too. No caller reaches it today, and the rule holds for the first that does.
2. **The row it writes and the `Association-Sorted` it publishes carry the four as stored**: the withholding is the answer's last step, after both.
3. **The other transitions answer as they do today.** The sort shares `SaveTransitionAsync` with the approval transition, set-confidence and set-scope, whose every caller is the review tier or the system, and which answer the four as stored (ConfidenceReads.md rule 4). It has no door.
