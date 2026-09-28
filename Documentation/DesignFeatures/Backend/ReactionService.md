# Reaction service
Parent: [Likes.md](../Likes.md)
Level: foundation — `IReactionService` (`Glory2Him.Core/Services/Foundations/Reactions/`)
Inherits: §DOM5.2, §SEC14.3 rules 3 and 4, §ARC12.2.1 rules 3–6, §ARC16.8 (*Which rows are counted*, *Anonymity*)

The reaction summary read counts only reactions of the **canonically visible** vocabulary, resolved through this service and closed over as an `IN (...)` set (§ARC16.8, the far-end row of *Which rows are counted*), and the summary carries each reaction's `Name` and `UnicodeEmoji` so the client never joins them (§ARC16.8, *The projection*). The vocabulary read that exists, `RetrieveAllReactionsAsync`, widens with the caller and hands back a live queryable, which the orchestration may compose but may not run a terminal operator over to answer a question (§ARC12.2.1 rule 7). This user story adds the caller-independent read the summary names (§ARC16.8, *What a rendered page costs*, round trip 1).

## 1. RetrievePublicReactionsAsync (#717)

```csharp
ValueTask<IReadOnlyList<Reaction>> RetrievePublicReactionsAsync(
    CancellationToken cancellationToken = default);
```

1. **It returns every reaction that is visible to anybody**: not deleted, `Approved`, published, and a publish date that is null or not after the current moment — an endpoint entity's visibility under §SEC14.3 rule 4.
2. **It is caller-independent.** It mints no envelope and resolves no `SecurityContext`, so a publisher holding a draft reaction receives the same vocabulary an anonymous visitor does. A count must not move when somebody signs in (§ARC16.8, *Anonymity*).
3. **The condition is authored here** as a query-shaping function over `Reaction` and handed to `SelectReactionsAsync` (`Backend/StorageBroker.md §4`), which the client awaits with the caller's token (§ARC12.2.1 rule 3). The current moment comes from `IDateTimeBroker`.
4. **Its order carries no meaning**, and no caller relies on one.
