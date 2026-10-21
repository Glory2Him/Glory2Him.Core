# Association service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`). Its other user stories are `Backend/Foundations/AssociationService.md`, `AssociationService.AssociationReads.md` and `AssociationService.BereanReview.md`.
Inherits: §SEC14.5, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rule 8, posture A′ rules 1, 2, 3, 5, 6 and 7, §ARC12.2.1, AssociationReads.md rule 10, ConfidenceReads.md rules 2–9 and 11 and *The rulings*

An association's review tier is the global `Reviewers`, `Publishers` and `Administrators`, or a coarse or narrow review or publisher role matching either endpoint, the endpoint's type tested beside its content type (§SEC14.7 posture A′ rules 2, 5 and 6; ConfidenceReads.md rule 2). `HasReviewRoleForAssociation` in `AssociationService.Validations.cs` asks it of one row, and on a collection `HasGlobalReviewRole` answers whether a caller is in every row's tier and `ResolveReviewableEntityTypes` and `ResolveReviewableContentTypes` in `AssociationService.cs` which rows a scoped caller's tier covers: the checks that admit the review tier to a non-public association. Each section answers the four as stored to a caller in the row's tier, and all four as null to every other caller — the owner, its `CreatedBy` or on a personal row its `UserId` (posture A′ rule 7), any other signed-in caller, an anonymous caller, a caller with no security context and the system identity — and changes nothing else its operation does (§SEC14.7 posture A rule 8). **Each interface method has a section of its own** (ConfidenceReads.md, *The rulings*): a direct path's section changes the body it shares with its door, and the existing tests that change with it, the door's among them; the door's section proves the rule on its own entry, with tests that pass as soon as they are written and are committed as pins. **A withheld answer carries both computed effective ids as stored** (ConfidenceReads.md rule 9), which `Backend/Models/Association.ConfidenceReads.md §1` makes possible.

**Build order** (ConfidenceReads.md, *What ships together* steps 4–6). An association's four are stored already. Every section builds on #1229, which has the modify take them from the stored row in place of comparing them (`Backend/Foundations/AssociationService.BereanReview.md §3`): before it, a caller outside the tier who read a withheld association and sent it back on a modify would be refused for four values they never touched (ConfidenceReads.md rule 9). Every section builds on `Backend/Models/Association.ConfidenceReads.md §1` too, which makes the two computed effective ids' setters `internal`. **§2 is the first of them**, and every other section builds on it: it is the first build that can answer a caller null for an association whose score is stored, so it rewrites `IConfidence`'s summary of what a null means (§2 rule 4). Each door's section follows its direct path's. §1 and §2 change the bodies `AssociationService.AssociationReads.md §1–§3` change (#1121, #1122, #1123), and whichever lands second rebases on the first. **§1 lands last of this sub-feature's tasks**, and records the rule as built.

## 1. RetrieveAllAssociationsAsync — withheld inside the query, and the rule recorded as built (#1273)

```csharp
ValueTask<IQueryable<Association>> RetrieveAllAssociationsAsync(
    CancellationToken cancellationToken = default);
```

1. **Each row it hands up carries the four as stored where the caller is in that row's tier, and all four as null where they are not** (ConfidenceReads.md rules 2 and 5). A caller holding a role `HasGlobalReviewRole` names is in every row's tier. A caller holding a scoped role is in the tier of the rows one of whose endpoints it matches: by the endpoint's type, for a role in `ResolveReviewableEntityTypes`, or by a `ContentItem` endpoint's content type, for a role in `ResolveReviewableContentTypes`, the endpoint's type tested beside its content type on both endpoints (posture A′ rule 6). Every other caller is in no row's tier. Which rows it hands up is unchanged, and so is every other property of each.
2. **It withholds inside the query it hands up, still unenumerated** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 6), as `ContentItemService.ConfidenceReads.md §1` rule 2 has the content item's read do, so whatever is composed over it reads the withheld value and runs in SQL with everything beneath it (§ARC12.2.1): the orchestration's endpoint composite (§SEC14.3), which composes conditions on both endpoints' types, key ids, group ids and scopes, and the `$filter`, `$orderby` or `$select` that `GET api/associations` will compose over the orchestration's read (#1124). The storage broker is unchanged.
3. **It carries both computed effective ids as stored.** The association panels' read composes `entityAEffectiveId eq <id>`, or the same of endpoint B, over `GET api/associations` (AssociationReads.md rule 10), so a projection that dropped either would answer it no row for every caller the read projects for, anonymous readers among them.
4. **Its proof includes SQL Server.** A unit test runs the query over rows in memory, and cannot show that it translates. Its own proof is what the association turns on that no other read does — a tier derived from either endpoint, and the two computed effective ids — and it proves the endpoint composite over its withheld rows, through the composite proof `ContentItemService.ConfidenceReads.md §1` rule 4 builds. Each runs the composed query through EF Core's asynchronous operator.
5. **It lands after every other section of `ConfidenceReads.md`'s user stories**, and records the rule as built in its own pull request (`Design/design.md`, *A gap that is built stays in its list*, point 5): it is the read whose build leaves no operation of ConfidenceReads.md rule 3 answering the four to a caller outside the tier. Its task fixes the words.

## 2. RetrieveAssociationByIdAsync — the read by id (#1269)

```csharp
ValueTask<Association> RetrieveAssociationByIdAsync(
    Guid associationId,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier — `HasReviewRoleForAssociation` — and all four as null where they do not**, once its posture has admitted the caller to the row, on the public row it answers before it reads any security context as on a row it admits by the context (ConfidenceReads.md rule 2). A missing security context is answered as one that holds no role. Which ids answer the row and which answer not-found is unchanged, posture A′ rule 7's owner of a personal row included (#1122), and so is every other property of the row.
2. **`DoRetrieveAssociationByIdAsync` applies it**, the body this method shares with its door (§7), so each decides it against the security context it decides the row's visibility against: this method the ambient caller's, through the envelope it mints.
3. **A withheld answer is a copy** (ConfidenceReads.md rule 9), so the row storage returned keeps its four, and the copy carries both computed effective ids as stored.
4. **Its build rewrites `IConfidence`'s summary of what a null means** (`Glory2Him.Core/Models/Bases/IConfidence.cs`; `Design/design.md`, *A gap that is built stays in its list*, point 4). From this build, an association whose score is stored can be answered null, so the summary's words on a null — *"Null means not yet scored"* on `ConfidenceScore`, *"Null when a human set it by hand"* on the provenance, and, once `Backend/Foundations/AssociationService.BereanReview.md §1` rule 6 is built, *"a null score means none has been recorded, or one was cleared because the content changed"* — are said of the stored row and no longer of every answer. The summary gains a paragraph that says so, in words its task fixes, and that holds whichever of that rule's build and this one lands first.

## 3. ModifyAssociationAsync — the modify's answer (#1270)

```csharp
ValueTask<Association> ModifyAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The modify admits the owner and the endpoint-derived publisher tier (§SEC14.7 posture A′ rule 2); the publisher tier is in the review tier, and the owner is in it only where they hold it too.
2. **The row it writes and the `Association-Modified` it publishes carry the four as stored**, taken from the stored row (`Backend/Foundations/AssociationService.BereanReview.md §3`, #1229): it withholds them from its answer alone, after both.
3. **`DoModifyAssociationAsync` applies it**, the body this method shares with its door (§8).

## 4. RemoveAssociationByIdAsync — the remove's answer (#1271)

```csharp
ValueTask<Association> RemoveAssociationByIdAsync(
    Guid associationId,
    string? deletionReason = null,
    CancellationToken cancellationToken = default);
```

1. **The removed row it answers carries the four as stored where the caller holds the tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The remove admits the owner and `Administrators` (§SEC14.7 posture A rule 3), and a reader withdrawing their own reaction (posture A′ rule 1); `Administrators` is in every tier, and the owner and the reader are in it only where they hold it too.
2. **The row it writes and the `Association-Removed` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The already-removed return withholds too** (ConfidenceReads.md rule 8). `DoRemoveAssociationByIdAsync` answers a row that is already soft-deleted as it is stored, after its permission check and before any write or fact. That answer carries the four by the same tier, and nothing is written or published.
4. **`DoRemoveAssociationByIdAsync` applies it**, the body this method shares with its door (§9).

## 5. SortAssociationAsync — the sort's answer (#1272)

```csharp
ValueTask<Association> SortAssociationAsync(
    Association association,
    Association anchorAssociation,
    SortPosition position,
    CancellationToken cancellationToken = default);
```

1. **The row it answers carries the four as stored where the caller holds the row's tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The sort admits the owner and `Administrators` (`ValidateUserCanSortStorageAssociationAsync`); `Administrators` is in every tier, and the owner is in it only where they hold it too. No caller reaches it today, and the rule holds for the first that does.
2. **The row it writes and the `Association-Sorted` it publishes carry the four as stored**: it withholds them from its answer alone, after both.
3. **The withholding is the sort's own** (ConfidenceReads.md rule 8). The sort saves through `SaveTransitionAsync`, the tail it shares with the approval transition, set-confidence and set-scope, which answer the four as stored (ConfidenceReads.md rule 4). The approval transition admits the system identity, which holds no role (`AssociationService.Transitions.Validations.cs`), and set-confidence admits it alone once #1155 is built, so the sort withholds after the tail returns, and the tail answers what it saved. It has no door.

## 6. UpsertPersonalAssociationAsync — the personal upsert's answer (#1307)

```csharp
ValueTask<PersonalAssociationUpsert> UpsertPersonalAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **The row it answers, `PersonalAssociationUpsert.Association`, carries the four as stored where the reader holds the row's tier, and all four as null where they do not** (ConfidenceReads.md rule 8). The upsert admits the signed-in reader whose row it is alone (`ValidateUserIsTheReader`), who is in the row's tier only where they hold it. A reader's row holds a score only where its round was scored before it was approved as it opened (BereanReview.md rules 3 and 22).
2. **Every arm that answers a stored or a written row withholds.** The unchanged and taken-down arms answer the reader's stored row and write nothing; the restored and repointed arms write the row and publish `Association-Restored` or `Association-Repointed` with the four as stored, and withhold them from the answer alone, after both. The created arm answers the row it has just added, which holds none of the four (ConfidenceReads.md rule 4, *An add*). The outcome each arm answers is unchanged.
3. **Its caller reads the answer's id and status alone** (`UpsertPersonalPairAsync`, `AssociationOrchestrationService.cs`), so nothing above it changes. It has no door.

## 7. OnRetrievingAssociationByIdAsync — the `Association-RetrievingById` door (#1308)

```csharp
ValueTask<EventEnvelope<Association>?> OnRetrievingAssociationByIdAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §2**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **A null security context is answered as one that holds no role.** The door can be handed an envelope whose security context is null, and answers a public row before it asks any (ConfidenceReads.md rule 2), so it replies with that row's four as null.
3. **It adds no code.** Its tests pass as soon as they are written, once §2 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals and its failures are unchanged, and their tests stand.

## 8. OnModifyingAssociationAsync — the `Association-Modifying` door (#1309)

```csharp
ValueTask<EventEnvelope<Association>?> OnModifyingAssociationAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §3**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **It adds no code.** Its tests pass as soon as they are written, once §3 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.

## 9. OnRemovingAssociationByIdAsync — the `Association-RemovingById` door (#1310)

```csharp
ValueTask<EventEnvelope<Association>?> OnRemovingAssociationByIdAsync(
    EventEnvelope<Association> envelope,
    CancellationToken cancellationToken = default);
```

1. **Its reply carries the answer of the body it shares with §4**, decided against its inbound envelope's security context (ConfidenceReads.md rules 2 and 8): the four as stored where that context holds the row's tier, and all four as null where it does not.
2. **The already-removed return replies by the same rule** (§4 rule 3): nothing is written or published, and the reply carries the stored row with the four by the same tier.
3. **It adds no code.** Its tests pass as soon as they are written, once §4 is built, and are committed as pins (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its failures and its deduplication are unchanged, and their tests stand.
