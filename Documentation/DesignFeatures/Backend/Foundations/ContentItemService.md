# Content item service
Parent: [Likes.md](../../Likes.md)
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`)
Inherits: §SEC14.1, §SEC14.5 rules 1, 3 and 4, §ARC12.2.1 rules 3–6, §ARC16.8 (*Which rows are counted*, *Anonymity*), §DOM3.4.1, §DOM4.6 rule 1

The reaction summary read counts a content item's reactions at **group** level, because a reaction is written `AllVersions` and belongs to the group, and it answers only for an id whose own version is canonically visible (§ARC16.8, the near-end row of *Which rows are counted*; §DOM4.3). It resolves its hosts through a **caller-independent** read, so the counts are the same whoever asks (§ARC16.8, *Anonymity*). No read on this service answers that: the collection read widens with the caller, and the feed read pages the whole catalogue. This user story adds the one read the summary names in its cost table (§ARC16.8, *What a rendered page costs*, round trip 2).

## 1. RetrievePublicContentItemGroupsAsync (#716)

```csharp
ValueTask<IReadOnlyList<PublicContentItemGroup>> RetrievePublicContentItemGroupsAsync(
    IReadOnlyList<Guid> contentItemIds,
    CancellationToken cancellationToken = default);
```

`PublicContentItemGroup` is `(Guid ContentItemId, Guid GroupId, ContentType ContentType)`. The content type is carried because the summary read keys each host's winning setting on it (`Backend/Brokers/AccessBroker.md §1`).

1. **For each id that names a canonically visible version, it answers that version's group and content type** — the id echoed as supplied. Canonically visible is §SEC14.1, asked of the version the id names: not deleted, `Approved`, published, and a publish date that is null or not after the current moment.
2. **A version that is not itself canonically visible is absent, even where its group has a visible version.** A draft v2 of an item whose v1 is approved and published is absent, and the published v1 answers with the group. Answering the draft would let a caller holding its id tell it apart from an id that names nothing (§SEC14.5 rule 1), and a soft-deleted version is not found for any caller (§SEC14.5 rule 3). This limits what the read discloses, not the endpoint test, which stays at the group (§ARC16.8, the near-end row).
3. **An id that names nothing, and an id whose version is not canonically visible, are absent** from the answer — never an error, and never a count of what was dropped (§SEC14.5 rule 4).
4. **It is caller-independent.** It mints no envelope and resolves no `SecurityContext`, exactly as `RetrieveContentItemFeedAsync` does, so an administrator receives the answer an anonymous visitor would. §SEC14.1 is the strictest posture this service has, so applying it unconditionally is its security, not an absence of it.
5. **The condition is authored here** as a query-shaping function over `ContentItem` — the id match and §SEC14.1 asked of each version together — and handed to `SelectContentItemsAsync` (`Backend/Brokers/StorageBroker.md §2`), which the client awaits with the caller's token (§ARC12.2.1 rule 3). The current moment comes from `IDateTimeBroker`, as the feed read's does.
6. **Duplicate ids answer once each.** Refusing an empty list or bounding its size is the caller's rule (§ARC16.8, *The set, its bounds*), not this read's.
