# Storage broker — tag suggestions
Parent: [TagSuggestions.md](../../TagSuggestions.md)
Level: broker — `IStorageBroker` (`Glory2Him.Core/Brokers/Storages/Sql/`), existing, its `Tag` partial. Its other user stories are `Backend/Brokers/StorageBroker.md`, the Likes feature's, `StorageBroker.Amenders.md` and `StorageBroker.SoftDeletion.md`.
Inherits: §ARC12.2.1 rules 1–3, 7 and 8; `Backend/Brokers/StorageBroker.md` (the pass-through's shape); `the-standard-brokers`

The read by name asks its question through a query-shaping function that `TagService` authors (`Backend/Foundations/TagService.TagSuggestions.md §3`), and the storage broker hands that function to the storage client unchanged (§ARC12.2.1 rule 3). The broker's `Tag` partial has no such member, so this user story adds the one it needs.

A broker member has no test paths: brokers hold no logic and get no unit tests. The member's behaviour is proven by the tests of the service that authors its condition — unit tests that execute the function (§ARC12.2.1 rule 5) and an integration test against the real catalogue (rule 6).

## 1. SelectTagsAsync (#1364)

```csharp
ValueTask<IReadOnlyList<TResult>> SelectTagsAsync<TResult>(
    Func<IQueryable<Tag>, IQueryable<TResult>> query,
    CancellationToken cancellationToken = default);
```

`IStorageBroker.Tag.cs` and `StorageBroker.Tag.cs`, in the shape `Backend/Brokers/StorageBroker.md` gives every pass-through, for `Tag`.

1. **It hands the function to the client unchanged**, through the partial's existing private `SelectListAsync<T, TResult>` helper (`StorageBroker.cs`), with the caller's `CancellationToken`, as `SelectReactionsAsync` does (`StorageBroker.Reaction.cs`). It composes no operator and authors no condition (§ARC12.2.1 rules 1 and 2).
2. **`SelectAllTagsAsync` stays beside it**, for exposure (§ARC12.2.1 rule 7).
3. **Its one caller is `TagService`'s read by name** (`Backend/Foundations/TagService.TagSuggestions.md §3`).
