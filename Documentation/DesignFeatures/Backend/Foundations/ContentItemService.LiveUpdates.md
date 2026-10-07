# Content item service — live updates
Parent: [LiveUpdates.md](../../LiveUpdates.md)
Level: foundation — `IContentItemService` (`Glory2Him.Core/Services/Foundations/ContentItems/`). The Likes feature's work on the same service is `Backend/Foundations/ContentItemService.md` (`design.md`, *Conventions*).
Inherits: §SEC14.1, §SEC14.5 rules 1, 3 and 4, §ARC12.2.1 rules 3–6, §ARC16.8 (*Which rows are counted*, the near-end row), LiveUpdates.md rule 8

A count message names the canonically visible version of the reaction's host (LiveUpdates.md rule 8). The forwarder holds the host's group, from the association row, and no read on this service tells it which of the group's versions everyone may see. `RetrievePublicContentItemGroupsAsync` is keyed on version ids (`Backend/Foundations/ContentItemService.md §1`), and the group read, `RetrieveContentItemsByGroupIdAsync`, widens with the caller. This user story adds that read.

## 1. FindPublicContentItemGroupAsync (#896)

```csharp
ValueTask<PublicContentItemGroup?> FindPublicContentItemGroupAsync(
    Guid groupId,
    CancellationToken cancellationToken = default);
```

`PublicContentItemGroup` is `(Guid ContentItemId, Guid GroupId, ContentType ContentType)`, the projection `Backend/Foundations/ContentItemService.md §1` answers with. The content type is carried because the forwarder keys the version's winning setting on it, as the summary does.

1. **It answers the group's canonically visible version, or nothing.** Canonically visible is §SEC14.1, asked of each version of the group: not deleted, `Approved`, published, and a publish date that is null or not after the current moment. A group has at most one such version, because it has at most one published, non-deleted version (`IX_ContentItem_IsPublished`).
2. **A group with no such version answers nothing, and so does an id that names no group.** Neither is an error, and the two are not told apart (§SEC14.5 rules 1 and 4). A group whose published version is soft deleted has no such version (§SEC14.5 rule 3).
3. **It is caller-independent.** It mints no envelope and resolves no `SecurityContext`, as `RetrievePublicContentItemGroupsAsync` does, so every caller receives the same answer.
4. **The condition is authored here**, as a query-shaping function over `ContentItem` that asks the group match and §SEC14.1 together, and is handed to `SelectContentItemsAsync` (`Backend/Brokers/StorageBroker.md §2`). The storage client awaits it with the caller's token (§ARC12.2.1 rule 3). The current moment comes from `IDateTimeBroker`.
5. **An empty group id is refused** as a validation failure, and storage is never asked.
