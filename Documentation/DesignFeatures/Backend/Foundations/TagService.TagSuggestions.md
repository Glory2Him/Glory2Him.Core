# Tag service — tag suggestions
Parent: [TagSuggestions.md](../../TagSuggestions.md)
Level: foundation — `ITagService` (`Glory2Him.Core/Services/Foundations/Tags/`), existing. Its other user stories are `Backend/Foundations/TagService.md`, `TagService.BereanReview.md`, `TagService.ConfidenceReads.md` and `TagService.SoftDeletion.md`.
Inherits: §APR9.7.1 rules 1 and 2, §ARC12.2.1 rules 1, 3, 5 and 6, §ARC12.3.1 rule 2a, §ARC12.5 (*an orchestration holds brokers*), §EVN13 rule 3, §SEC14.5 rules 1 and 3, §SEC14.6 (rule 1, and *Cross-row rules under visibility filtering*), §SEC14.7 posture A rule 1, `Backend/Foundations/AssociationService.md` §13 and §14, TagSuggestions.md rules 2, 3, 11 and 15

What the tag foundation needs for a tag to be suggested by its name: an add that mints the new tag's `Id`, since the orchestration that adds it cannot (TagSuggestions.md rule 15), and a read that finds the tag a name is held by, whatever its state and whoever owns it (TagSuggestions.md rule 3). §1 and §3 build them on the direct path, and §2 pins the mint on the `Tag-Adding` door. §1 changes an existing member, as `Backend/Foundations/AssociationService.md §13` records another feature's change to the association's add. §3 adds a member.

**Build order.** §1, then §2. §3 after `Backend/Brokers/StorageBroker.TagSuggestions.md §1`, in either order with §1 and §2.

## 1. AddTagAsync — the add mints the row's `Id` (needs issue)

```csharp
ValueTask<Tag> AddTagAsync(
    Tag tag,
    CancellationToken cancellationToken = default);
```

An existing member, changed: it stops taking the new row's `Id` from its caller. Its signature, its gate, its other validations, the write, the `Tag-Added` fact and its `ProcessedEvents` records are unchanged.

A tag's `Id` is an `IKey` member, so it belongs to the identifier broker and is never accepted from a caller (§APR9.7.1 rule 2), on the event path as well as the direct one (§APR9.7.1). Until this change the add took the `Id` from its caller and refused an empty one (`TagService.Validations.cs`, `ValidateOnAddTagAsync`). The suggestion's orchestration adds a tag and cannot mint its `Id`, because an identifier broker is not among the brokers an orchestration holds (§ARC12.5, *an orchestration holds brokers*). So this service mints it, as the association's add does (`Backend/Foundations/AssociationService.md §13`). No deviation is needed.

1. **The add mints the new row's `Id`** through the identifier broker this service already holds (`TagService.cs`), and never takes the caller's. Whatever `Id` the caller's copy carries is overwritten, and is neither compared nor refused. The mint is in `DoAddTagAsync`, the private write body both entry paths share, and not in the public member, so it binds the `Tag-Adding` door too (§2).
2. **It mints after the contribution gate and before validation.** A caller the gate refuses mints nothing.
3. **The add's empty-`Id` rule is not asked**: the `Id` it would ask of is the one this service minted. Every other rule of `ValidateOnAddTagAsync` is asked as it is today.
4. **The added row, and the `Tag-Added` fact, carry the minted `Id`**, and so does what `AddTagAsync` returns. That is how a caller learns which row was written.
5. **`POST /api/Tags` answers the minted `Id`**, through `TagsController.PostTagAsync`, unchanged, which hands the add its body and answers `201` with the row the add returns. A caller's `Id` is ignored there. Tests that post a tag and then read it, or a pairing of it, by the `Id` they posted read it by the `Id` the post answered: the acceptance tests that post a tag (`Websites/Glory2Him.WebApp.Tests.Acceptance/Apis/Tags/`, and `Apis/Approvals/ApprovalTests.AutomaticAIReviewer.cs`), and the foundation's add tests (`Glory2Him.Core.Tests.Unit/Services/Foundations/Tags/TagServiceTests.Add.*.cs`).

## 2. OnAddingTagAsync — the minted `Id` on the `Tag-Adding` door (needs issue)

```csharp
ValueTask<EventEnvelope<Tag>?> OnAddingTagAsync(
    EventEnvelope<Tag> envelope,
    CancellationToken cancellationToken = default);
```

An existing member, unchanged: the handler for `Tag-Adding` (`TagService.Substrate.cs`). It converges on `DoAddTagAsync`, so §1 binds it by construction. This section records what that means on this door, and proves it here, as `Backend/Foundations/AssociationService.md §14` proves the association's mint on its door.

1. **The row is written under the `Id` this service mints, never the one the publisher named.** §APR9.7.1 rule 2 holds on this path too, so no deviation is recorded. The envelope's content, which its signature covers (§SEC14.6 rule 4), is not refused for its `Id`, and the claim is neither compared nor refused, as on the direct path. The reply and the `Tag-Added` fact carry the minted `Id`.
2. **Its verification, its deduplication on `Metadata.EventId` and its contribution gate run before the mint**, so a replayed or tampered envelope, or a caller the gate refuses, mints nothing and writes nothing (§1 rule 2).
3. **Its tests are pins.** §1 puts the mint in the shared body, so they pass as soon as they are written, and they are committed as pins.

## 3. FindTagByNameAsync (needs issue)

```csharp
internal ValueTask<TagNameMatch?> FindTagByNameAsync(
    string name,
    CancellationToken cancellationToken = default);
```

A new member. It finds the tag a name is held by, whatever its state and whoever owns it, so that the suggestion's orchestration can decide what a name names without an identity-filtered read deciding it (TagSuggestions.md rule 3).

1. **It is `internal` to Core**, as the identity-carrying `RetrieveTagByIdAsync` overload is (`ITagService.cs`), so it is not part of the contract `TagsController` is written against (§EVN13 rule 3). Its one caller is `AssociationOrchestrationService.SuggestTagAsync` (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`). It has no event door: nothing publishes a request for it, as nothing publishes one for the association's pair probes.
2. **Its gate is the tag's contribution gate**, asked before anything is read: an authenticated caller holding neither `ReadOnly` nor `Tag-ReadOnly` (§SEC14.7 posture A rule 1; `ValidateUserIsAllowedToContribute`, `TagService.Validations.cs`). It asks it of the envelope it mints, which carries the ambient caller, as `FindAssociationByPairAsync` asks its gate on the method path (`AssociationService.Lookup.cs`): the read exists to serve a contribution, and carries its gate (§SEC14.6, *Cross-row rules under visibility filtering*). A caller it refuses is answered `UnauthorizedTagException`, wrapped as `TagValidationException`, and nothing is read.
3. **Its input is the name as handed**, required and at most 30 characters: a name that is `null`, empty or white space, or longer than 30 characters, is refused, `InvalidTagException` naming `Name`, wrapped as `TagValidationException`, before anything is read. It neither trims the name nor strips anything from it: what names a tag is the suggestion's to settle, and it hands this read the name it settled (TagSuggestions.md rule 2). A name longer than the column could hold names no tag.
4. **Its condition is authored here, as a query-shaping function, and handed to `IStorageBroker.SelectTagsAsync`** (`Backend/Brokers/StorageBroker.TagSuggestions.md §1`; §ARC12.2.1 rules 1 and 3). The function uses `System.Linq` operators only (§ARC12.2.1 rule 3), and selects the rows whose `Name` equals the name handed, by one term and no other: **no `IsDeleted` term, no approval or publication term and no owner term**. So it finds a soft-deleted row, a `Draft`, `Submitted`, `Approved` or `Rejected` row, an unpublished or scheduled row, and another caller's row, all alike. It projects each row to `TagNameMatch` (rule 6) and shapes the query to one row: `IX_Tags_Name` is unique and unfiltered (`StorageBroker.Tag.Configurations.cs`), so a name is held by at most one row, soft-deleted rows included (§ARC12.3.1 rule 2a).
5. **It answers the one match, or `null` where no row holds the name.** It answers the same to every caller its gate admits: no caller term decides what it finds (§SEC14.6, *Cross-row rules under visibility filtering*), and it logs no denial, having none to make.
6. **`TagNameMatch` carries the row's `Id`, its `ApprovalStatus` and its `IsDeleted`, and nothing else** — never its name, its owner, its publication fields, its audit fields or its confidence fields. A new model, `Glory2Him.Core/Models/Foundations/Tags/TagNameMatch.cs`, public as `AssociationPairMatch` is (`Glory2Him.Core/Models/Foundations/Associations/AssociationPairMatch.cs`), which is the projection this one parallels: it carries what its caller branches on, and no row data a caller may not see (§SEC14.5 rules 1 and 3).
7. **Letter case is the column's**, compared in SQL under the column's collation, so a name that differs from the stored one only in letter case finds it (TagSuggestions.md rule 2). An in-memory evaluation compares ordinally and cannot prove it, so an integration test proves it against the real catalogue, with the shaped query's translation and its projection (§ARC12.2.1 rule 6), in `Glory2Him.Core.Tests.Integration/Services/Foundations/Tags/`, as `AssociationNarrowReadTests` proves the association's narrow reads (`Glory2Him.Core.Tests.Integration/Services/Foundations/Associations/`).
8. **Its unit tests execute the condition** (§ARC12.2.1 rule 5): the mocked storage broker applies the function it is handed to an in-memory set holding the row the name names and a row whose name differs, and, for each term the condition must not have, a row that meets the name and differs on that term alone — soft-deleted, at each other approval status, unpublished or scheduled, another caller's.
9. **Its failures are the service's**: a storage failure is `TagDependencyException`, an unexpected one `TagServiceException`, through the service's `TryCatch` (`TagService.Exceptions.cs`); and a cancelled token and a timeout are answered as the service's other members answer them, since the member takes a `CancellationToken` (`the-standard-cancellation-patterns`).
