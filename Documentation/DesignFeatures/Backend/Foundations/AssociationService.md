# Association service
Parent: [Likes.md](../../Likes.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`)
Inherits: §DOM4.3, §DOM4.4 rule 4, §DOM4.5 rules 1, 2 and 4, §DOM4.6, §DOM4.10, §SEC14.3 rules 1, 2 and 5, §SEC14.5, §SEC14.6, §SEC14.7 posture A′ rules 1, 2, 4 and 7, §APR7.5.1, §APR9.7.1 rule 2, §APR9.9 rule 6, §ARC12.2.1 rules 3–6, §ARC12.5, §ARC16.2.2, §ARC16.8, §ARC16.8.1, §EVN2, §EVN17 rule 3, §EVN20 rule 9, §EVN23 rules 1–3, Likes.md rules 1–3, 5, 6, 8 and 10

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
   | none | a new row under an `Id` this member mints (rule 10), validated as the add validates one, at the status the caller set | `Association-Added` | `Created` |
   | withdrawn by the reader, same reaction | revived: `IsDeleted`, `DeletedBy` and `DeletedWhen` cleared; the status left as it was (§DOM4.10 rule 8) | `Association-Restored` | `Restored` |
   | withdrawn by the reader, another reaction | revived and repointed in one write (§DOM4.10, *What the reader sees*) | `Association-Repointed` | `Repointed` |
   | live, another reaction | repointed: `EntityBKeyId` and `EntityBGroupId`; the status left as it was (§DOM4.5 rule 4) | `Association-Repointed` | `Repointed` |
   | live, same reaction | nothing | none | `Unchanged` |
   | withdrawn by anybody else | nothing — a takedown is never revived (§DOM4.10 rule 7) | none | `TakenDown` |

   "Withdrawn by the reader" is `DeletedBy` equal to the row's `UserId`, and nothing else (§DOM4.10 rule 7).
5. **A repoint is admitted whatever the row's status**, `Approved` and `Rejected` included. It is the terminal bar's one exception (§SEC14.7 posture A′ rule 2), and `ModifyAssociationAsync` keeps the bar unconditionally.
6. **On an existing row only the enumerated fields change** — `EntityBKeyId`, `EntityBGroupId`, `IsDeleted`, `DeletedBy`, `DeletedWhen` — beside the audit stamp every write carries. Every other field keeps its stored value, whatever the caller's copy says (§ARC16.2.2). The stamp's `UpdatedWhen` is the change's time the approval ear reads (§APR9.7.4).
7. **The facts are published with the written row as their content, and the result of every publish is inspected** (§EVN23 rules 1–3), the create arm's `Association-Added` included. The facts are on two new addresses: `Association-Restored` and `Association-Repointed` join `EventBrokerIdentifiers.Association`, each with a stable identifier, and `AssociationEventOperation` gains a member for each, appended.
8. **A second first reaction racing the first is refused by the personal index** (`UX_Associations_PersonalPair`, §DOM4.6 rule 2) and reaches the caller as the add's duplicate does — `AlreadyExistsAssociationException`, as a dependency validation failure.
9. **It checks the endpoint fields it is handed, and derives none of them.** Its caller derives each endpoint's scope and group (`Backend/Orchestrations/AssociationOrchestrationService.md` §1 rule 1 step 3), and this member takes the personal key and the repoint from them. So before the row is resolved it refuses as invalid:
   - a scope other than the one §DOM4.5 rule 1 derives for the endpoint's type — `AllVersions` on a versioned type and `ThisVersionOnly` on any other, as `EntityTypeVersioning` answers it (§APR7.5.1) — a value outside the `Scope` enum (§DOM4.3) included;
   - an empty group id;
   - a group other than the key on an endpoint whose type is not versioned (§DOM4.5 rule 2).

   Both endpoints are checked, because either may be A once canonical order is restored (rule 1), and each refusal names the field the request carries. **A versioned endpoint's group is not checked against its row**: the foundation may not read that row, so it keeps the group it is handed, as the add does (§DOM4.5 rule 2), and the caller's derivation from the resolved row is what makes it right. The refusals do not breach §DOM4.10 rule 2's overwrite-never-refuse house rule, which the caller applies where it derives these fields. This member checks what it is handed, as rule 2 does for `UserId`.
10. **The create arm mints the new row's `Id`**, through the identifier broker this service already holds, and never takes the caller's. Its caller sends two endpoints and nothing else (§ARC16.8.1), and nothing between the request and this member sets an `Id`, so whatever `Id` the request carries is overwritten. The add's empty-`Id` rule is therefore not asked of the new row. An existing row keeps its own `Id` (rule 6).

`Association-Upserting` is not minted, so this member has no event path (§ARC16.2.2). It records no `ProcessedEvents` row, because nothing deduplicates against one, so no bookkeeping follows its publish.

## 3. RetrieveContentItemReactionCountsAsync (#720)

```csharp
ValueTask<IReadOnlyList<AssociationPairCount>> RetrieveContentItemReactionCountsAsync(
    IReadOnlyList<Guid> contentItemGroupIds,
    IReadOnlyList<Guid> reactionIds,
    CancellationToken cancellationToken = default);
```

`AssociationPairCount` is `(Guid EntityAEffectiveId, Guid EntityBKeyId, int Count)` — §ARC16.8's narrow native row.

1. **It counts the reactions given to each host**, over the rows that satisfy §SEC14.3 rules 1, 2 and 5: not deleted, `Approved`, and a publish date that is null or not after the current moment. An empty list of hosts, of reactions or of both is valid, and answers an empty list.
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

   **`Association.UserId` ignores letter case** (§DOM4.6 rule 3), so a caller whose user id differs from a row's only in letter case reads that row as their own. The caller term compares under the column's collation, which `UX_Associations_PersonalPair` keys under too (§DOM4.6 rule 2), so this read and that index treat the same rows as one reader's. The unit tests compare ordinally and cannot show this, so the integration proof pins it. It pins the code as well: a read that compared `UserId` in C#, once the rows were read, would compare ordinally and fail it.

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

## 7. FindPersonalAssociationAsync — a pair with no host on A (#878)

An existing member, changed: §1's lookup gains one refusal, §DOM4.10 rule 9's. Its signature, its answer and its other rules are unchanged.

1. **A lookup whose endpoint A is of a personal type, once canonical order is restored (§1 rule 2), is refused as invalid**, naming `EntityAType`, the field the stored row would hold it in, and storage is never asked. Personal is `EntityTypePersonalisation`'s answer (§DOM4.10 rule 4). Today that is a `Reaction` paired with a `Tag`, named in either order, or with another `Reaction`.
2. **It is asked after §1 rule 3's caller check.** An anonymous caller and a lookup carrying another reader's `UserId` still answer `null`, whatever pair they name, as every denied read does, and a `null` `UserId` is still refused as invalid naming `UserId`.
3. **Its caller is unchanged.** The pair-keyed withdrawal (`Backend/Orchestrations/AssociationOrchestrationService.md §3`) answers the refusal as its dependency validation exception, through the arm every `IAssociationService` refusal reaches (that document's §4 rule 9).

## 8. UpsertPersonalAssociationAsync — a pair with no host on A (#879)

An existing member, changed: §2's write gains one refusal, §DOM4.10 rule 9's. Its signature, its outcomes, its facts and its other rules are unchanged.

1. **A write whose endpoint A is of a personal type, once canonical order is restored (§2 rule 1), is refused as invalid**, naming `EntityAType`, and before the reader's row is resolved: storage is never asked, nothing is written and no fact is published. Personal is `EntityTypePersonalisation`'s answer (§DOM4.10 rule 4). Today that is a `Reaction` paired with a `Tag`, named in either order, or with another `Reaction`.
2. **It is asked after §2 rule 2's and rule 9's refusals**: an anonymous caller, a `null` `UserId`, another reader's `UserId` and an endpoint field §2 rule 9 refuses are refused as they are today, whatever pair they name.
3. **Its caller is unchanged.** The upsert's personal arm (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 4) answers the refusal as its dependency validation exception, through the arm every `IAssociationService` refusal reaches (that document's §4 rule 9).

## 9. FindPersonalAssociationAsync — a user id in another letter case (#1024)

An existing member, changed: §1 rule 3's caller check follows §DOM4.6 rule 3. Its signature, its answer, the place of the check in the member and its other rules are unchanged.

1. **A lookup whose `UserId` differs from the signed caller's only in letter case is the caller's own**, and is answered as §1 rule 1 answers it. Any other `UserId` still answers `null` before storage is asked, and the denial is still logged as a warning (§1 rule 3).
2. **The personal-key condition is still asked with the caller's user id**, and the column's collation matches it to the reader's row in whatever letter case the row holds it (§DOM4.6 rule 3). An in-memory set compares ordinally, so this is proven against the real catalogue, through this member, from the request to the answer (§ARC12.2.1 rule 6).

## 10. UpsertPersonalAssociationAsync — a user id in another letter case (#1025)

An existing member, changed: §2 rule 2's caller check follows §DOM4.6 rule 3. Its signature, its outcomes, its facts and its other rules are unchanged. Its revive discriminator follows the same rule under §12.

1. **A request whose `UserId` differs from the signed caller's only in letter case is the caller's own** (§2 rule 2), and the row is resolved and written as §2 rule 4 has it. Any other `UserId` is still refused as unauthorized. The reader's row is resolved with the personal-key condition, which §9 rule 2 proves against the real catalogue.
2. **The row it creates keeps the `UserId` it is handed**, as it does today, in the request's letter case. Rule 1 and §DOM4.6 rule 3 make that the caller's own.

## 11. RemoveAssociationByIdAsync — a user id in another letter case (#1026)

An existing member, changed: §5 rule 3's test of whose row it is follows §DOM4.6 rule 3. Its signature, its facts and its other rules are unchanged.

1. **A personal row whose `UserId` differs from the signed caller's only in letter case is the caller's own**, and is asked none of the three read-only roles (§5 rule 3). Every other row is still asked all three (§5 rule 4).
2. **The owner test is unchanged** (§5 rule 5). It compares the row's `CreatedBy` with the caller exactly, which §DOM4.6 rule 3 leaves open, so a row whose `CreatedBy` differs from the caller's user id, in letter case alone or in more, is still refused to a caller outside `Administrators`, whatever its `UserId`.
3. **Both entry paths take it**, because both converge on `DoRemoveAssociationByIdAsync` (§5 rule 6). It is proven on the direct path, where the shared body's gate is proven (§6 rule 4).

## 12. UpsertPersonalAssociationAsync — a withdrawal recorded in another letter case (#1033)

An existing member, changed: §2 rule 4's revive discriminator follows §DOM4.6 rule 3. Its signature, its outcomes, its facts, its caller check (§10) and its other rules are unchanged.

1. **"Withdrawn by the reader" is a `DeletedBy` that differs from the row's `UserId` in nothing but letter case** (§2 rule 4, §DOM4.10 rule 7). Such a row is revived, `Restored` or `Repointed` as §2 rule 4's table has it. A withdrawn row whose `DeletedBy` names anybody else is still `TakenDown`.

## 13. AddAssociationAsync — the add mints the row's `Id` (#1088)

```csharp
ValueTask<Association> AddAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

An existing member, changed: it stops taking the new row's `Id` from its caller. Its signature, its gate, its derivations, its other validations, canonical order, the write, the `Association-Added` fact and its `ProcessedEvents` records are unchanged.

An association's `Id` is an `IKey` member, so it belongs to the identifier broker and is never accepted from a caller (§APR9.7.1 rule 2). That rule bounds every write to an approvable entity on the event path as well as the direct one (§APR9.7.1). Until #1088 the add took the `Id` from its caller and refused an empty one. Its only caller is the upsert's editorial arm, which hands it the caller's two-endpoint request unchanged (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 3), so every editorial suggestion was refused. The orchestration cannot mint the `Id` instead: an identifier broker is not among the brokers an orchestration holds (§ARC12.5, *an orchestration holds brokers*). So this service mints it, as §2 rule 10 already does for a reader's reaction. No deviation is needed.

1. **The add mints the new row's `Id`** through the identifier broker this service already holds, and never takes the caller's. Whatever `Id` the caller's copy carries is overwritten, and is neither compared nor refused. The mint is in `DoAddAssociationAsync`, the private write body both entry paths share, and not in the public member, so it binds the `Association-Adding` door too (§14).
2. **It mints after the contribution gate and before validation.** A caller the gate refuses mints nothing.
3. **The add's empty-`Id` rule is not asked**, as §2 rule 10 does not ask it of a reaction's new row: the `Id` it would ask of is the one this service minted.
4. **The inserted row, the `Association-Added` fact and the returned row carry the minted `Id`.** The upsert's editorial arm answers `Created` with it (§ARC16.8.1), and that answer is how a caller learns the new row's `Id`.
5. **What stops a second row is unchanged, and it is never the `Id`.** It is the occupancy check the orchestration runs ahead of the add on both doors (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 3 and §2), and `UX_Associations_EditorialPair` beneath it (§DOM4.6 rule 2). A write that fails after the insert leaves the row as it does today.

## 14. OnAddingAssociationAsync — the minted `Id` on the event door (#1089)

An existing member, unchanged: the handler for `Association-Adding`, which since #631 the orchestration's handler reaches with the same envelope (`Backend/Orchestrations/AssociationOrchestrationService.md §2`). It converges on `DoAddAssociationAsync`, so §13 binds it by construction. This section records what that means on this door, and proves it here.

1. **The row is written under the `Id` this service mints, never the one the publisher named.** §APR9.7.1 rule 2 holds on this path too, so no deviation is recorded. The envelope's content, which its signature covers (§SEC14.6 rule 4), is not refused for its `Id`, and the claim is neither compared nor refused, as on the direct path. The reply and the `Association-Added` fact carry the minted `Id`, and they are how the publisher learns which row was written.
2. **Deduplication on `Metadata.EventId` and the contribution gate run before the mint**, so a replay, or a caller the gate refuses, mints nothing and writes nothing (§13 rule 2).
3. **Its tests are pins.** §13 puts the mint in the shared body, so they pass as soon as they are written, and they are committed as pins.
