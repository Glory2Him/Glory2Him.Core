# Link processing service — confidence reads
Parent: [ConfidenceReads.md](../../ConfidenceReads.md)
Level: processing — `ILinkProcessingService` (`Glory2Him.Core/Services/Processings/Links/`). Its foundation's work for this sub-feature is `Backend/Foundations/LinkService.ConfidenceReads.md`.
Inherits: §SEC14.2 rule 1, §SEC14.6 rules 1–3, §SEC14.7 posture A rule 8, §ARC12.2.1, ConfidenceReads.md rules 3, 6, 7 and 9

The processing tier's reads compose over the foundation's, and take the rule from them with no change of their own, save one: the public collection read, which is the processing tier's own caller-independent read (ConfidenceReads.md rule 7; *Layer placement* item 3). No dependency changes: the service depends on its one foundation service, as today.

**Build order** (ConfidenceReads.md, *What ships together* steps 2 and 3). §1 builds on #1154 and #1196, which give `Link` the four properties and map their columns, and follows `Backend/Foundations/ContentItemService.ConfidenceReads.md §1`, whose pattern it takes: withholding inside the query it hands up, proven against SQL Server. It needs none of that section's code, or of `LinkService.ConfidenceReads.md §1`'s, since it nulls the four whatever the foundation answers.

## 1. RetrieveAllPublicLinksAsync — the public read answers no confidence (#1248)

```csharp
ValueTask<IQueryable<Link>> RetrieveAllPublicLinksAsync(
    CancellationToken cancellationToken = default);
```

1. **Every row it hands up carries all four as null, whoever asks** (§SEC14.7 posture A rule 8; ConfidenceReads.md rule 7). It runs its visibility filter with no security context, so an administrator is answered what an anonymous visitor is (§SEC14.2 rule 1), and it answers them the four that way too.
2. **It withholds them itself.** The foundation read it composes over, `ILinkService.RetrieveAllLinksAsync`, answers the caller's own tier (`Backend/Foundations/LinkService.ConfidenceReads.md §1`), so a caller in the tier of the public rows would otherwise be answered their stored four through the public read.
3. **It withholds inside the query it hands up, still unenumerated** (ConfidenceReads.md rule 6), so the `$filter`, `$orderby` or `$select` that `GET api/Links/Public` composes over it reads null and runs in SQL with everything beneath it (§ARC12.2.1).
4. **Its rows, and every other property of each, are as today.** **Its proof in SQL Server shows that it stays an EF query.** How a read that answers every caller the four as null translates is `ContentItemProcessingService.ConfidenceReads.md §1`'s proof, which this one does not repeat. What none of the other proofs reaches is this read: no other proof composes it, and the endpoint composite splices the foundation's read, not this one. So its proof runs it, composed, through EF Core's asynchronous operator, which a read materialised in memory and nulled there cannot serve.
