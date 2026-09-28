# Association orchestration service
Parent: [Likes.md](../../Likes.md)
Level: orchestration — `IAssociationOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/Associations/`)
Inherits: §ARC12.5 (entry 1, and *an orchestration holds brokers*), §ARC12.5.2 business rule 6, §ARC16.2.1, §ARC16.2.2, §ARC16.8, §ARC16.8.1, §DOM4.2, §DOM4.4 rule 4, §DOM4.5, §DOM4.10, §SEC14.3, §SEC14.5, §SEC14.6, §SEC14.7 posture A′ rules 1, 4 and 7, §EVN13 rule 3, Likes.md rules 1–10

The caller-facing surface of a reader's reaction, and the layer an exposer binds to (§EVN13 rule 3). §ARC16.8.1 designs the write surface and is its single home — the upsert's shape, the withdrawal's signature, route and response codes — and §ARC16.8 designs the read; §ARC16.2.1 designs the facet gate. This document gives each of their operations its section and settles what those sections leave to the build, citing them rather than restating them.

**It gains one dependency, and it is a broker.** `IAccessBroker` answers the facet gate and the summary's rule 6 through one arm (`Backend/Brokers/AccessBroker.md §1`); a broker sits outside the Florance count (§ARC12.5), and §ARC16.2.1 and §ARC16.8 name this service as its fourth holder. No entity service is added. The host registers this service scoped (`CoreRegistration.cs`) and supplies the broker there.

**Personal or editorial is the lookup's answer** (`Backend/Models/EntityTypePersonalisation.md §1`). An association is personal where either endpoint's type is personal — today a `Reaction` endpoint (§DOM4.2) — and editorial otherwise, and every member below takes the answer from the lookup and never from a test of its own.

## 1. UpsertAssociationAsync (#724)

```csharp
ValueTask<AssociationSuggestionResult> UpsertAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

§ARC16.8.1 owns the member: it **replaces** `AddAssociationAsync`, which leaves the interface; it takes two endpoints and nothing else; one flow runs up to the branch, and the two personalities branch after it.

1. **The flow is the add's, with the reaction exemption and the gate in it** — one flow, still shared with the event door (§2), in this order:
   1. authentication; and, for an **editorial** pair only, the global `ReadOnly` — a **personal** pair asks none of the read-only roles (§SEC14.7 posture A′ rule 1);
   2. the add's structural validation of the raw endpoints;
   3. both endpoints resolved and their scope, group and content type derived, as today (§DOM4.5);
   4. for an editorial pair only, the endpoint half of the veto, as today (§SEC14.7 posture A′ rule 4);
   5. `UserId`: the caller's own, from the envelope, on a personal pair; `null` on an editorial one. Whatever the caller sent is overwritten without comment on both (§DOM4.10 rules 1 and 2);
   6. the facet gate (§ARC16.2.1), then the branch.
2. **The facet gate asks each `ContentItem` endpoint's winning setting**, through `IAccessBroker.RetrieveEffectiveContentItemSettingsAsync` keyed on that endpoint's content type and key id, for the switch the far end's type maps to — `TagsAllowed`, `ReactionsAllowed`, `CommentsAllowed`, `BibleReferenceAllowed`, `LinksAllowed` or `AttachmentsAllowed` (§ARC16.2.1's table). It refuses in exactly two ways, each naming the switch: the switch is `false`, or the setting carries `LimitReactionsToLoveOnly` and the far-end `Reaction`'s `Name`, trimmed and compared without case, is not `Love` (§ARC16.2.1). A `ContentItem` endpoint whose setting resolves no row is refused too: the gate never falls open. A `BibleReference` host is not gated — its settings entity is not built (§ARC16.2.1, *the `BibleReference` host is a gap*) — and a far end with no mapped switch asks nothing. Each refusal is `InvalidAssociationOrchestrationException`, which this service maps to `AssociationOrchestrationValidationException`. **The gate sits in the flow both doors share**, so it is on the `Association-Adding` door too, by construction (§ARC16.2.1: *the gate is reached on both entry paths*). This section's task proves it on the direct path, and §5 proves it on the event door. A settings read that fails reaches this service's closing catch as its service exception, and nothing is written (§ARC16.2.1, *the gate never falls open*).
3. **The editorial arm is today's `AddAssociationAsync`, unchanged**: the two probes, the insert of a free pair and the same statuses (§ARC16.8.1). It never repoints.
4. **The personal arm sets `ApprovalStatus` to `Submitted`** (§ARC16.8.1: a reaction row is created at `Submitted`, never at `Draft`) **and hands the row to `IAssociationService.UpsertPersonalAssociationAsync`**, which resolves the reader's row itself (`Backend/Foundations/AssociationService.md §2`). This service runs no probe of its own on this arm (§ARC16.8.1).
5. **The foundation's outcome becomes the result's status**, and the result carries the status and the row's id and nothing else (§ARC16.8.1):

   | Foundation outcome | `AssociationSuggestionStatus` |
   | --- | --- |
   | `Created` | `Created` |
   | `Restored` | `Restored` |
   | `Repointed` | `Repointed` *(new, appended)* |
   | `Unchanged`, the row `Approved` | `AlreadyApproved` |
   | `Unchanged`, any other status | `AlreadyPending` |
   | `TakenDown` | `AlreadyPending` — a takedown tells the reader nothing (§DOM4.10 rule 7) |

   `Restored` has never been produced (`AssociationOrchestrationService.cs`, the soft-deleted branch), and its documentation, which describes an editorial revive to `Draft`, is to describe the personal revive: back at the status it was withdrawn at (§DOM4.10 rule 8).

## 2. OnAddingAssociationAsync (#723)

The `Association-Adding` handler #631 moved here. Its verification, its deduplication, its refusal set and its occupancy check are unchanged. It gains the facet gate through the flow it shares with §1, and §1's task proves it on this door. What this section adds is one refusal, and it is built **before** §1, so that the flow's personal derivation never reaches this door.

1. **This door refuses a personal pair**, after the envelope is verified and the deduplication check, and before any endpoint is read. A reader's reaction has no event path: `Association-Upserting` is not minted (§ARC16.2.2), and the foundation's add behind this door can neither revive nor repoint, so it would write a second row where §DOM4.10 rule 6 allows one — the withdrawn row left behind, a new one inserted beside it. Today this door derives `UserId` as `null` and refuses any other claim (#631's refusal set, rule 1); once §1 derives the caller's `UserId` for a personal pair, that refusal would no longer catch an honest claim, and this one is what keeps the add behind this door writing editorial rows only.
2. **The refusal is one message for every personal pair**, naming no row, no reader and no status, and a refusal writes nothing, publishes no fact and records no `ProcessedEvents` row.

## 3. RemoveAssociationByPairAsync (#725)

```csharp
ValueTask<AssociationRemovalResult> RemoveAssociationByPairAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

`AssociationRemovalResult` is `(AssociationRemovalStatus Status, Guid? AssociationId)`, the status `Removed` or `NothingToRemove`. §ARC16.8.1 owns the member's signature, route and response codes.

1. **It takes the upsert's caller shape and runs its flow up to the pair** (§ARC16.8.1): authentication, the same structural validation, the same endpoint resolution, and the caller's `UserId` from the envelope, whatever the caller sent. It asks none of the read-only roles — the far-end type says the row is personal before any read, so the exemption is decidable here (§SEC14.7 posture A′ rule 4) — and runs no facet gate: withdrawing is never gated (§ARC16.2.1).
2. **It withdraws only a personal pair.** An editorial pair is refused as invalid: a withdrawal is keyed on (content item, reaction, caller) (§ARC16.8).
3. **It finds the reader's row through `IAssociationService.FindPersonalAssociationAsync`** (`Backend/Foundations/AssociationService.md §1`) and, where that row is live and names the reaction the caller named, **soft-deletes it through `IAssociationService.RemoveAssociationByIdAsync`** and answers `Removed` with the row's id. The foundation's soft delete publishes `Association-Removed`; this service mints no address (§ARC16.8).
4. **Anything else is `NothingToRemove`, with no id**: no row, a withdrawn row, or a row holding a different reaction — which stays as it is. It is idempotent, and never answers not-found (§ARC16.8.1).

## 4. RetrieveContentItemReactionSummariesAsync (#726)

```csharp
ValueTask<IReadOnlyList<ContentItemReactionSummary>> RetrieveContentItemReactionSummariesAsync(
    IReadOnlyList<Guid> contentItemIds,
    CancellationToken cancellationToken = default);
```

§ARC16.8 owns the read: its projection, which rows are counted, its anonymity, its bounds and what a page costs. What follows is how its steps are reached.

1. **The set is the distinct ids supplied, 1 to 25 of them.** An empty set, a set of more than 25 distinct ids and a `Guid.Empty` member are refused before any read (§ARC16.8, *The set, its bounds*); duplicates are answered once.
2. **The hosts come from `IContentItemService.RetrievePublicContentItemGroupsAsync`** (`Backend/Foundations/ContentItemService.md §1`), caller-independent and at group level. An id it does not answer is absent from the response (§ARC16.8, *Anonymity*).
3. **The vocabulary comes from `IReactionService.RetrievePublicReactionsAsync`** (`Backend/Foundations/ReactionService.md §1`), and only its reactions are counted (§ARC16.8, the far-end row). Each count carries its reaction's `Name` and `UnicodeEmoji` from it.
4. **Rule 6 comes from `IAccessBroker.RetrieveEffectiveContentItemSettingsAsync`**, keyed on each host's content type and supplied id: a host whose winning setting carries `ShowReactions = false`, or resolves no row, is answered with `Reactions` empty, and is not counted (§ARC16.8, the rule 6 row). The collection read's evaluator keeps its rule 6 vacancy; this is §SEC14.3's second resolver, for a host the read already holds (§SEC14.3, *one rule, two resolvers*).
5. **The counts come from `IAssociationService.RetrieveContentItemReactionCountsAsync`** over the counted hosts' group ids and the vocabulary's ids, in one call (`Backend/Foundations/AssociationService.md §3`).
6. **A signed-in caller's own reaction comes from `IAssociationService.RetrieveCallerContentItemReactionsAsync`** over every answered host's group (`Backend/Foundations/AssociationService.md §4`), and fills `ViewerReactionId` and `ViewerReactionName` where it names a reaction of the vocabulary. Where it names one that is not, such as a reaction withdrawn from the vocabulary since the reader gave it, both are `null`: a departure from §ARC16.8's projection, recorded under *Deviations*. Both are `null` for an anonymous caller too, for whom that read is not asked (§ARC16.8, *What a signed-out caller receives*).
7. **Each summary echoes the id it was asked for** (§ARC16.8, *The projection*), so two versions of one item each receive their own id and the group's one count.
8. **Each summary lists its reactions in the vocabulary's order**, the order `RetrievePublicReactionsAsync` answers in (`Backend/Foundations/ReactionService.md §1` rule 4), whatever order the counts arrive in. The orchestration keeps that order and does not sort again (§ARC16.8, *The projection*).
9. **The three foundations' refusals are this service's dependency validation exception.** A validation or dependency validation exception from `IContentItemService` or `IReactionService` needs catch arms of its own in this service's `TryCatch`. Today only the `Association*` families have arms, and any other `Xeption` reaches the dependency arm (`AssociationOrchestrationService.Exceptions.cs:103-107`). The access broker's failure reaches the closing catch, as in §1 rule 2.

## 5. OnAddingAssociationAsync — the facet gate on the event door (#753)

#723 builds this door's refusal of a personal pair before the upsert exists. §1 then puts the facet gate into the flow this door already runs, and §ARC16.2.1 requires the gate on both entry paths, so this section proves it on this door once §1 is built.

1. **An editorial pair the host's setting refuses is refused here as §1 rule 2 refuses it**: nothing written, no fact published, no `ProcessedEvents` row recorded. A host whose setting resolves no row is refused the same way.
2. **A settings read that fails reaches this service's closing catch** as its service exception, which the handler logs and rethrows.
3. **Its tests are pins.** §1 puts the gate into this door's flow, so they pass as soon as they are written, and they are committed as pins. A personal pair never reaches the gate on this door, because #723 refuses it first.

## Deviations

1. **The dependency count** — §ARC12.5, the two-to-three rule for an orchestration. This service holds seven entity services: `IAssociationService`, `IContentItemService`, `ITagService`, `IReactionService`, `IBibleReferenceService`, `ICommentService` and `ILinkService`. This user story adds four operations to it without adding a dependency. **Why:** §ARC16.8, §ARC16.8.1 and §ARC16.2.1 place the reaction write, the withdrawal and the summary read on the one service that resolves both endpoints of any pair. Splitting them into a service of their own would copy that resolution and the facet gate. §ARC12.5 entry 1's provisional note records the count, and the revisit of the endpoint resolution that could earn a processing service beneath this one. **Approved** by the owner, by name, on 2026-09-28 (QA round 1 on #748, BLOCKING 1: *"Approve a named deviation"*).
2. **The viewer members** — §ARC16.8, *The projection*. §4 rule 6 answers both viewer members `null` where the caller's own row names a reaction outside the public vocabulary. §ARC16.8 would carry the row's `EntityBKeyId` and its reaction's name. **Why:** the card marks a reaction by matching it to an option it offers, and it offers only the public vocabulary, so it could not mark that reaction anyway. Naming it would take a read of a reaction nobody else may see. **Approved** by the owner on 2026-09-28 (QA round 1 on #748, BLOCKING 6: *"Record it as a deviation"*).
