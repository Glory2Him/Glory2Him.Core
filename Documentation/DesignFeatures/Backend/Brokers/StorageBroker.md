# Storage broker
Parent: [Likes.md](../../Likes.md)
Level: broker — `IStorageBroker` (`Glory2Him.Core/Brokers/Storages/Sql/`), the SQL storage broker, one partial per entity
Inherits: §ARC12.2.1 rules 1–3, 7 and 8; `the-standard-brokers`

Every read the Likes feature adds asks its question through a query-shaping function its caller authors, and the storage broker hands that function to the storage client unchanged (§ARC12.2.1 rule 3). The broker authors no condition, composes no operator and queries nothing through its `DbContext` (§ARC12.2.1 rules 1 and 2); it gains one pass-through per entity this feature reads, each forwarding to the client's `SelectListAsync` (`Backend/Clients/StorageClient.md §1`) with the caller's `CancellationToken`. The first pass-through built adds the private `SelectListAsync<T, TResult>` helper beside the partial's existing `efCoreClient` helpers (`StorageBroker.cs`), and the rest reuse it.

Each section is one member, and a broker member has no test paths: brokers hold no logic and get no unit tests. Each member's behaviour is proven by the tests of the service that authors its condition — unit tests that execute the function (§ARC12.2.1 rule 5), and integration tests against the real catalogue where rule 6 requires them.

The signature is the same shape in every section, for its own entity:

```csharp
ValueTask<IReadOnlyList<TResult>> Select<Entity>sAsync<TResult>(
    Func<IQueryable<<Entity>>, IQueryable<TResult>> query,
    CancellationToken cancellationToken = default);
```

The unfiltered `SelectAll<Entity>sAsync` stays beside each of them, for exposure (§ARC12.2.1 rule 7).

## 1. SelectAssociationsAsync (#708)

`IStorageBroker.Association.cs`. Read by `AssociationService`'s personal lookup, its reaction counts and the caller's own reactions (`Backend/Foundations/AssociationService.md §1–§4`).

## 2. SelectContentItemsAsync (#709)

`IStorageBroker.ContentItem.cs`. Read by `ContentItemService`'s publicly visible groups (`Backend/Foundations/ContentItemService.md §1`).

## 3. SelectContentItemSettingsAsync (#710)

`IStorageBroker.ContentItemSetting.cs`. Read by `AccessBroker`'s effective-setting gather (`Backend/Brokers/AccessBroker.md §1`), which §ARC12.2.1 rule 3 names as a condition author for its gathers.

## 4. SelectReactionsAsync (#711)

`IStorageBroker.Reaction.cs`. Read by `ReactionService`'s publicly visible vocabulary (`Backend/Foundations/ReactionService.md §1`).

## 5. SelectApprovalReviewsAsync (#712)

`IStorageBroker.ApprovalReview.cs`. Read by `AccessBroker`'s gather of a round's active reviews (`Backend/Brokers/AccessBroker.md §2`).
