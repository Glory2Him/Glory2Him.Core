# Reaction
Parent: [Likes.md](../Likes.md)
Level: model — `Reaction` (`Glory2Him.Core/Models/Foundations/Reactions/Reaction.cs`), with its storage configuration, one migration, and the vocabulary's seed (`Websites/Glory2Him.WebApp/Data/ReactionSeedData.cs`)
Inherits: §DOM5.2 (`SortOrder`), §DOM6.6 (`ContentItemSetting.SortOrder`, the shape this copies), §ARC12.3.1 shared rules 1 and 9

§DOM5.2 gives a reaction a `SortOrder`, and both the Like control's choices and every card's counts are presented in that order (§ARC16.8, *The projection*). This user story adds the column and gives the five seeded reactions their order. It copies `ContentItemSetting.SortOrder` (§DOM6.6), including the fixes for the two faults that column hit when it was added: the backfill the seed cannot do, and the column default that overwrote a zero.

## 1. SortOrder (#749)

```csharp
public int SortOrder { get; set; } = 1000;
```

1. **The entity defaults `SortOrder` to `1000`**, the column's default, so a reaction built without an order sorts after the curated ones rather than first.
2. **The column is required, carries a store default of `1000`, and is `ValueGeneratedNever()`.** The store default serves a raw-SQL insert that names no column. `ValueGeneratedNever()` makes EF always send what the entity holds, so a `0` the caller set is stored as `0` and not replaced by the default (#395). `StorageBrokerStoreDefaultTests` fails without it.
3. **A new migration adds the column and backfills the five seeded reactions by id** — `10`, `20`, `30`, `40` and `50` for Amen, Love, Joy, Moved and Praying — leaving any other row at `1000`. The seed only inserts a reaction that is missing, so without the backfill every database that has already booted would keep its five rows on `1000`, in an order nobody chose. The `UPDATE` is wrapped in `EXEC`: the deploy path runs the idempotent script as one batch, and SQL Server compiles the whole batch before the new column exists (the comment above the `UPDATE` in `20260830160539_AddSortOrderToContentItemSettings`).
4. **The seed writes the same five values**, so a fresh database and a backfilled one agree. The values are §DOM5.2's; the seed and the backfill change together.
5. **No index.** The vocabulary is a handful of rows, read whole.
