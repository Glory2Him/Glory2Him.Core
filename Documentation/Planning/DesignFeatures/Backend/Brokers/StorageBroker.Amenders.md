# Storage broker — the amender record
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: broker — `IStorageBroker` (`Glory2Him.Core/Brokers/Storages/Sql/`), the SQL storage broker, one partial per entity. The Likes feature's work on the same broker is `Backend/Brokers/StorageBroker.md`, whose pass-through shape this follows.
Inherits: §ARC12.2.1 rules 1–3, 7 and 8; `the-standard-brokers`

A fork of a `Link` inherits the records of its group's rows (§APR8.6.3 rule 5), and `LinkService` reads them through a query-shaping function it authors (§ARC12.2.1 rule 3). The broker gains the one pass-through that read needs, in the shape `Backend/Brokers/StorageBroker.md` gives every other, forwarding to the storage client's `SelectListAsync` (`Backend/Clients/StorageClient.md §1`) through the existing private `SelectListAsync<T, TResult>` helper (`StorageBroker.cs`). It authors no condition. A broker member has no test paths; its behaviour is proven by the tests of the service that authors its condition (`Backend/Foundations/LinkService.md §1`).

## 1. SelectLinksAsync (#1010)

```csharp
ValueTask<IReadOnlyList<TResult>> SelectLinksAsync<TResult>(
    Func<IQueryable<Link>, IQueryable<TResult>> query,
    CancellationToken cancellationToken = default);
```

`IStorageBroker.Link.cs` and `StorageBroker.Link.cs`. Read by `LinkService`'s add, for the rows of the group a fork joins (`Backend/Foundations/LinkService.md §1`). The unfiltered `SelectAllLinksAsync` stays beside it (§ARC12.2.1 rule 7).
