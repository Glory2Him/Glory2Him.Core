# Storage client
Parent: [Likes.md](../Likes.md)
Level: client — `IEFCoreClient` (`Clients/StorageClients/G2H.StorageClient/Clients/IEFCoreClient.cs`), the client every storage broker in the solution delegates to
Inherits: §ARC12.2.1 rules 3–6

§ARC12.2.1 rule 3 gives the client three terminal shapes, each additive to its surface, so that a query condition is authored by the storage broker's caller, handed down as a query-shaping function, applied by the client to its own source and awaited there with the caller's token. The Likes feature needs one of the three, and this user story builds that one alone. *The first matching row or none* and *whether any row matches* are not built here; every read this feature adds asks for at most a handful of rows, and a lookup that wants one row shapes its query to one.

The client's public surface is `IEFCoreClient`; its operation service and its own storage broker are internal to the library, so an operation here is one public member of `IEFCoreClient` with what it needs inside the library.

## 1. SelectListAsync — the matching rows, optionally projected (#707)

```csharp
ValueTask<IReadOnlyList<TResult>> SelectListAsync<T, TResult>(
    Func<IQueryable<T>, IQueryable<TResult>> query,
    CancellationToken cancellationToken = default)
    where T : class;
```

1. **The query is the caller's; the source and the await are the client's.** The client applies `query` to its own source for `T` — the `DbContext` set it already answers `SelectAllAsync<T>` from — and awaits the terminal operator with `cancellationToken`, so the shaped query runs in SQL and nothing is materialised to be filtered in memory (§ARC12.2.1 rules 3 and 4).
2. **What comes back is the query's result, as it shaped it**: every row the function selects, in the order it orders them, projected to `TResult` when it projects. An empty result is an empty list, never `null`.
3. **The rows come back untracked**, so a caller that reads a row this way and then writes it through `UpdateAsync` or `DeleteAsync` meets no tracked copy of it.
4. **A `null` function is refused** with `ArgumentNullException`, and **a cancelled token** with `OperationCanceledException`, both before the source is queried — the same posture `SelectAsync<T>(object[])` takes for its own arguments.
5. **Exceptions are not wrapped**, as the client's README states for every member: a failure of the underlying query reaches the caller unchanged.
6. **The query really runs in SQL**, and that is proven in `G2H.StorageClient.Tests.Integrations` against the real database, because the unit tests can only prove it over an in-memory set (§ARC12.2.1 rule 6).
