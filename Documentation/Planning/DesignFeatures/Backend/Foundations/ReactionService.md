# Reaction service
Parent: [Likes.md](../../Likes.md)
Level: foundation — `IReactionService` (`Glory2Him.Core/Services/Foundations/Reactions/`)
Inherits: §DOM5.2, §SEC14.3 rules 3 and 4, §ARC12.2.1 rules 3–6, §ARC12.3.1 shared rule 9, §ARC16.8 (*The projection*, *Which rows are counted*, *Anonymity*)

The reaction summary read counts only reactions of the **canonically visible** vocabulary, resolved through this service and closed over as an `IN (...)` set (§ARC16.8, the far-end row of *Which rows are counted*), and the summary carries each reaction's `Name` and `UnicodeEmoji` so the client never joins them (§ARC16.8, *The projection*). The vocabulary read that exists, `RetrieveAllReactionsAsync`, widens with the caller and hands back a live queryable, which the orchestration may compose but may not run a terminal operator over to answer a question (§ARC12.2.1 rule 7). This user story adds the caller-independent read the summary names (§ARC16.8, *What a rendered page costs*, round trip 1), which answers in the vocabulary's order, and it puts §DOM5.2's bound on a reaction's `SortOrder` onto the two writes that carry it.

## 1. RetrievePublicReactionsAsync (#717)

```csharp
ValueTask<IReadOnlyList<Reaction>> RetrievePublicReactionsAsync(
    CancellationToken cancellationToken = default);
```

1. **It returns every reaction that is visible to anybody**: not deleted, `Approved`, published, and a publish date that is null or not after the current moment — an endpoint entity's visibility under §SEC14.3 rule 4.
2. **It is caller-independent.** It mints no envelope and resolves no `SecurityContext`, so a publisher holding a draft reaction receives the same vocabulary an anonymous visitor does. A count must not move when somebody signs in (§ARC16.8, *Anonymity*).
3. **The condition is authored here** as a query-shaping function over `Reaction` and handed to `SelectReactionsAsync` (`Backend/Brokers/StorageBroker.md §4`), which the client awaits with the caller's token (§ARC12.2.1 rule 3). The current moment comes from `IDateTimeBroker`.
4. **It answers in the vocabulary's order** — `SortOrder`, lower first, with `Name` breaking a tie (§DOM5.2). The order is part of the query-shaping function, so SQL does the ordering. The summary keeps it (§ARC16.8, *The projection*).

## 2. AddReactionAsync (#750)

```csharp
ValueTask<Reaction> AddReactionAsync(
    Reaction reaction,
    CancellationToken cancellationToken = default);
```

1. **A negative `SortOrder` is refused** with the add's validation exception, which names `SortOrder`, and nothing is written (§DOM5.2). `0` and every positive value are admitted.
2. **The event door is out of scope.** `OnAddingReactionAsync` runs the same `DoAddReactionAsync` and its validation, so the rule reaches that door by construction. The task proves it on the direct path, as `planner.md` proves what a shared body owns.

## 3. ModifyReactionAsync (#751)

```csharp
ValueTask<Reaction> ModifyReactionAsync(
    Reaction reaction,
    CancellationToken cancellationToken = default);
```

1. **A negative `SortOrder` is refused** with the modify's validation exception, which names `SortOrder`, before storage is read or written (§DOM5.2). `0` and every positive value are admitted.
2. **The event door is out of scope**, as in §2: `OnModifyingReactionAsync` runs the same `DoModifyReactionAsync`.
3. **Nothing else about modify changes.** `SortOrder` is content under §ARC12.3.1 shared rule 1, so it is not pinned against storage, and an approved or rejected reaction's order is as fixed as its name: shared rule 9 refuses the amendment already.
