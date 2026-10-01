# Association service
Parent: [Likes.md](../../Likes.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`)
Inherits: §DOM4.4 rule 4, §DOM4.5 rule 4, §DOM4.6, §DOM4.10, §SEC14.3 rules 1, 2 and 5, §SEC14.5, §SEC14.6, §SEC14.7 posture A′ rules 1, 2, 4 and 7, §APR9.9 rule 6, §ARC12.2.1 rules 3–6, §ARC16.2.2, §ARC16.8, §EVN2, §EVN17 rule 3, §EVN20 rule 9, Likes.md rules 1–3, 5, 6, 8 and 10

The foundation half of a reader's reaction: the one write that gives, changes and revives it, the lookup that finds it, the two reads the summary counts and marks with, and the one change to the soft remove that lets a reader withdraw their own. Every condition below is authored here as a query-shaping function and handed to `SelectAssociationsAsync` (`Backend/Brokers/StorageBroker.md §1`), which the client awaits with the caller's token (§ARC12.2.1 rule 3); each is unit-tested by executing it (rule 5), and integration proves what an in-memory set cannot (rule 6).

**The personal-key condition is written once.** §1's lookup and §2's private write body resolve the reader's row with one condition — §DOM4.6 rule 2's personal key, `EntityAType`, `EntityAEffectiveId`, `EntityBType`, `UserId` — as §DOM4.6 already requires of the correction it names: *"write the personal-key lookup that `UpsertPersonalAssociationAsync` needs … and the pre-check reuses that condition rather than writing a second one."* It matches withdrawn rows as well as live ones, because a revive needs the withdrawn row. §DOM4.10 rule 6 allows one row per reader for the life of the relationship; where rows written before this feature break it, the live row is taken first and then the most recently updated, the order the pair probe already uses (`StorageBroker.Association.cs:47`). The effective id is computed from the request's scope, group and key as §DOM4.6 defines it, after §DOM4.4 rule 4's normalisation.

## 1. FindPersonalAssociationAsync (#718)

```csharp
ValueTask<PersonalAssociationMatch?> FindPersonalAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

`PersonalAssociationMatch` is `(Guid Id, Guid EntityBKeyId, bool IsDeleted)`: what the withdrawal branches on, and nothing that could leak authorship.

1. **It answers the reader's one row for the association's host and far-end type**, live or withdrawn, or `null` where there is none — over the unfiltered store, through the personal-key condition above.
2. **It normalises canonical order before the lookup** (§DOM4.4 rule 4), so a request naming the endpoints the other way round finds the same row.
3. **It answers only for the signed caller.** The association's `UserId` must be the caller's own, taken from the envelope this member mints. A lookup carrying any other `UserId` answers `null` before storage is asked, as a lookup with no row does, because a denied read answers not found. The true reason is logged server-side as a warning, as `RetrieveAssociationByIdAsync` logs its own (`AssociationService.cs:242-244`; ts-foundations-012). A `null` `UserId`, which means an editorial row, is refused as invalid. A reader never learns anything about another reader's row (§SEC14.7 posture A′ rule 7).
4. **It is a read, and asks no read-only role** (§SEC14.7 posture A′ rule 3: reads stay exempt).

Its caller is the pair-keyed withdrawal (`Backend/Orchestrations/AssociationOrchestrationService.md §3`).

## 2. UpsertPersonalAssociationAsync (#719)

```csharp
ValueTask<PersonalAssociationUpsert> UpsertPersonalAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

`PersonalAssociationUpsert` is `(PersonalAssociationUpsertOutcome Outcome, Association Association)`, where `Association` is the row as it stands after the call and the outcome is one of `Created`, `Restored`, `Repointed`, `Unchanged` and `TakenDown`. The member is §ARC16.2.2's, which owns its field scope, its facts and their subscribers; the rules below are what it does, and cite rather than restate that section.

1. **The write shape is this service's own.** The public member refuses a `null` association and a cancelled token, mints the envelope and delegates to a private `DoUpsertPersonalAssociationAsync`, which normalises canonical order before it resolves the row and before any storage call (§DOM4.4 rule 4, §ARC16.2.2).
2. **It answers only for the signed caller, and never for an editorial row.** Anonymous is refused as unauthorized; a `null` `UserId` is refused as invalid (§ARC16.2.2: the repoint exception is personal-only); a `UserId` that is not the caller's is refused as unauthorized (§SEC14.7 posture A′ rule 2: *acting for that same `UserId`*).
3. **It asks none of the three read-only roles** (§SEC14.7 posture A′ rule 1).
4. **It resolves the reader's row with the personal-key condition, and the row decides the arm** (§DOM4.10):

   | The reader's row | What is written | Fact | Outcome |
   | --- | --- | --- | --- |
   | none | a new row, validated as the add validates one, at the status the caller set | `Association-Added` | `Created` |
   | withdrawn by the reader, same reaction | revived: `IsDeleted`, `DeletedBy` and `DeletedWhen` cleared; the status left as it was (§DOM4.10 rule 8) | `Association-Restored` | `Restored` |
   | withdrawn by the reader, another reaction | revived and repointed in one write (§DOM4.10, *What the reader sees*) | `Association-Repointed` | `Repointed` |
   | live, another reaction | repointed: `EntityBKeyId` and `EntityBGroupId`; the status left as it was (§DOM4.5 rule 4) | `Association-Repointed` | `Repointed` |
   | live, same reaction | nothing | none | `Unchanged` |
   | withdrawn by anybody else | nothing — a takedown is never revived (§DOM4.10 rule 7) | none | `TakenDown` |

   "Withdrawn by the reader" is `DeletedBy` equal to the row's `UserId`, and nothing else (§DOM4.10 rule 7).
5. **A repoint is admitted whatever the row's status**, `Approved` and `Rejected` included. It is the terminal bar's one exception (§SEC14.7 posture A′ rule 2), and `ModifyAssociationAsync` keeps the bar unconditionally.
6. **On an existing row only the enumerated fields change** — `EntityBKeyId`, `EntityBGroupId`, `IsDeleted`, `DeletedBy`, `DeletedWhen` — beside the audit stamp every write carries. Every other field keeps its stored value, whatever the caller's copy says (§ARC16.2.2). The stamp's `UpdatedWhen` is the change's time the approval ear reads (§APR9.7.4).
7. **The facts are published as the add publishes its own**, with the written row as their content, on two new addresses: `Association-Restored` and `Association-Repointed` join `EventBrokerIdentifiers.Association`, each with a stable identifier, and `AssociationEventOperation` gains a member for each, appended.
8. **A second first reaction racing the first is refused by the personal index** (`UX_Associations_PersonalPair`, §DOM4.6 rule 2) and reaches the caller as the add's duplicate does — `AlreadyExistsAssociationException`, as a dependency validation failure.

`Association-Upserting` is not minted, so this member has no event path (§ARC16.2.2).

## 3. RetrieveContentItemReactionCountsAsync (#720)

```csharp
ValueTask<IReadOnlyList<AssociationPairCount>> RetrieveContentItemReactionCountsAsync(
    IReadOnlyList<Guid> contentItemGroupIds,
    IReadOnlyList<Guid> reactionIds,
    CancellationToken cancellationToken = default);
```

`AssociationPairCount` is `(Guid EntityAEffectiveId, Guid EntityBKeyId, int Count)` — §ARC16.8's narrow native row.

1. **It counts the reactions given to each host**, over the rows that satisfy §SEC14.3 rules 1, 2 and 5: not deleted, `Approved`, and a publish date that is null or not after the current moment.
2. **Its predicate is §ARC16.8's**: `EntityAType = ContentItem AND EntityAEffectiveId IN (contentItemGroupIds) AND EntityBType = Reaction AND EntityBKeyId IN (reactionIds)`. It pins the host on endpoint A and the reaction on B, which `CK_Association_CanonicalOrder` guarantees for this pair (§ARC16.8, *The predicate, and what pins it*).
3. **It groups on `(EntityAEffectiveId, EntityBKeyId)` and counts in the projection**, inside the shaping function, so the aggregate runs in SQL (§ARC16.8, *Where the GROUP BY runs*). A pair nobody gave has no row, and so no entry.
4. **It is caller-independent.** It mints no envelope, and every caller receives the same counts (§ARC16.8, *Anonymity*).
5. **The grouped projection is proven to translate** against the real catalogue in `Glory2Him.Core.Tests.Integration` (§ARC12.2.1 rule 6).

Its caller is the summary read (`Backend/Orchestrations/AssociationOrchestrationService.md §4`).

## 4. RetrieveCallerContentItemReactionsAsync (#721)

```csharp
ValueTask<IReadOnlyList<AssociationPairKey>> RetrieveCallerContentItemReactionsAsync(
    IReadOnlyList<Guid> contentItemGroupIds,
    CancellationToken cancellationToken = default);
```

`AssociationPairKey` is `(Guid EntityAEffectiveId, Guid EntityBKeyId)`.

1. **It answers the signed caller's own live reaction on each host**, whatever its approval status and publish date — which is what lets the card show a reaction pressed while its count has not moved (§ARC16.8, *What a reader sees when their own reaction is not yet `Approved`*). An empty list of hosts is valid. It is asked of storage as any other list is, so that rule 4's proof reaches SQL, and it answers an empty list.
2. **The caller is the envelope's, never a parameter**, so it can never return another reader's row. An anonymous caller has no rows, and is answered with an empty list without a query. So is a signed-in caller whose identity carries no user id — null, empty or whitespace. An editorial row's `UserId` is null (§DOM4.6 rule 2), so it is nobody's own. A caller term with no id would answer every editorial reaction row on the hosts asked for as the caller's own. The caller with no user id is logged as a warning, because a signed-in identity without a user id is a misconfiguration. The anonymous answer is ordinary traffic and is not logged.
3. **It decides nothing.** It is an identity-filtered read for display, and no invariant rests on it.
4. **The shaped query is proven against the real catalogue** in `Glory2Him.Core.Tests.Integration` (§ARC12.2.1 rule 6). The proof covers three things the unit tests' in-memory evaluation cannot show:
   - the `IN` over the list and the projection translate;
   - an empty list answers an empty list in SQL;
   - the caller term ignores letter case.

   **`Association.UserId` ignores letter case** — the owner's ruling of 2026-10-01. A caller whose user id differs from a row's only in letter case reads that row as their own. The caller term compares under the column's collation. That collation is case-insensitive, and it is the collation `UX_Associations_PersonalPair` keys on (§DOM4.6 rule 2), so this read and that index treat the same rows as one reader's. The unit tests compare ordinally and cannot show this, so the integration proof pins it. **The ruling's reach beyond this read is #815's, which is open.** That covers three things: whether the ruling becomes a column rule in §DOM4.6; whether a case-insensitive collation is pinned on the column rather than left to the catalogue's default; and the comparisons of `UserId` made in C#, which are ordinal today. Those are the ownership checks of §1 rule 3 and §2 rule 2, and §2 rule 4's revive discriminator, `DeletedBy` equal to the row's `UserId` (§DOM4.10 rule 7). Until #815 lands, §1 and §2 stand as written.

Its caller is the summary read (`Backend/Orchestrations/AssociationOrchestrationService.md §4`), which asks it only for a signed-in caller (§ARC16.8, *What a rendered page costs*, the fifth round trip).

## 5. RemoveAssociationByIdAsync — a reader's own reaction outside the veto (#722)

An existing member, changed. Its signature, its owner test and its facts are unchanged; the order of its gate changes, as §SEC14.7 posture A′ rule 4 rules (user ruling 2026-09-27).

1. **Authentication still runs first**, before any read, so an anonymous caller never reaches the `Associations` table.
2. **The global `ReadOnly` moves below the load**, beside the endpoint half of the veto (§SEC14.7 posture A′ rule 4, *the reaction exemptions meet this split*).
3. **A row whose `UserId` is the signed caller's is asked none of the three read-only roles** — not `ReadOnly`, not either endpoint's `%EntityType%-ReadOnly`, not `ContentItem-%ContentType%-ReadOnly` (§SEC14.7 posture A′ rule 1).
4. **Every other row is asked all three, after the load**: an editorial row, and another reader's reaction, whoever removes it, `Administrators` included (§SEC14.7 posture A′ rule 1, *the exemption is the reader's own*).
5. **The owner test is unchanged** — the row's `CreatedBy`, or `Administrators` — and still runs before the idempotent already-deleted short-circuit (§SEC14.7 posture A rule 3). The status bound of §APR9.9 rule 8 is not built and is not built here; when it is, a reader's own reaction is outside it (§APR9.9 rule 6).
6. **Both entry paths converge on `DoRemoveAssociationByIdAsync`**, so `OnRemovingAssociationByIdAsync` takes the same order against the inbound envelope's caller.

What it gives up is recorded in §SEC14.7 posture A′ rule 4: on this one surface a globally blocked caller reaches the table as any signed-in caller does, and is refused after the load rather than before it.

## 6. OnRemovingAssociationByIdAsync — the remove by id's gate on the event door (#775)

An existing member, unchanged: the handler for `Association-RemovingById`. §5 rule 6 already rules that it converges on `DoRemoveAssociationByIdAsync`, and §5 owns the gate and its order. This section proves them on this door, and restates neither.

1. **The gate is asked of the inbound envelope's caller alone** (§5 rule 6; §SEC14.6 rule 4). An unauthenticated caller, a read-only caller on a row outside the exemption and a caller who fails the owner test are each refused here as §5 rules 1, 4 and 5 refuse them on the direct path. A reader's own reaction is removed here whatever read-only role they hold (§5 rule 3).
2. **No envelope is minted on this path.** The `Association-Removed` fact and the reply are each made next from the inbound envelope, which carries its security context forward (§EVN17 rule 3; §EVN20 rule 9). Neither can carry the ambient caller.
3. **An envelope with no content or no metadata, or one whose signature does not verify for `Association-RemovingById` in the request direction, is refused before anything else is asked** (§SEC14.6 rule 4).
4. **Its tests are pins.** The handler already does all of this, so they pass as soon as they are written, and they are committed as pins. The order of the gate's steps among themselves belongs to the shared body, so its pins belong to §5.
