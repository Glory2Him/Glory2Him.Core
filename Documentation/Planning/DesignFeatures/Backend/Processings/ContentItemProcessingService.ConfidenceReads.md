# ContentItem processing service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: processing — `IContentItemProcessingService` (`Glory2Him.Core/Services/Processings/ContentItems/`). Its foundation's work for this sub-feature is `Backend/Foundations/ContentItemService.ConfidenceReads.md`.
Inherits: §SEC14.2 rule 1, §SEC14.6 rules 1–3, §SEC14.7 posture A rule 8, §ARC12.2.1, ConfidenceReads.md rules 3, 6, 7 and 9

The processing tier's reads compose over the foundation's, and take the rule from them with no change of their own, save one: the public collection read, which is the processing tier's own caller-independent read (ConfidenceReads.md rule 7; *Layer placement* item 3). No dependency changes: the service depends on its one foundation service, as today.

**Build order** (ConfidenceReads.md, *What ships together* steps 2 and 3). §1 builds on #1154 and #1196, which give `ContentItem` the four properties and map their columns, and follows `Backend/Foundations/ContentItemService.ConfidenceReads.md §1`, whose pattern it takes: withholding inside the query it hands up, proven against SQL Server. It needs none of that section's code, since it nulls the four whatever the foundation answers.

## 1. RetrieveAllPublicContentItemsAsync — the public read answers no confidence (#1241)

```csharp
ValueTask<IQueryable<ContentItem>> RetrieveAllPublicContentItemsAsync(
    CancellationToken cancellationToken = default);
```

1. **Every row it hands up carries all four as null, whoever asks** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 7). It runs its visibility filter with no security context, so an administrator is answered what an anonymous visitor is (§SEC14.2 rule 1), and it answers them the four that way too.
2. **It withholds them itself.** The foundation read it composes over, `IContentItemService.RetrieveAllContentItemsAsync`, answers the caller's own tier (`Backend/Foundations/ContentItemService.ConfidenceReads.md §1`), so a caller in the tier of the public rows would otherwise be answered their stored four through the public read.
3. **It withholds inside the query it hands up, still unenumerated** (ConfidenceReads.md rule 6), so the `$filter`, `$orderby` or `$select` that `GET api/ContentItems/Public` composes over it reads null and runs in SQL with everything beneath it (§ARC12.2.1).
4. **Its rows, and every other property of each, are as today.** Its proof includes SQL Server, over the real foundation beneath it, because the foundation answers a caller in the tier the stored four and only this read's own withholding nulls them; the proof runs the composed query through EF Core's asynchronous operator, which a read materialised in memory cannot serve.
