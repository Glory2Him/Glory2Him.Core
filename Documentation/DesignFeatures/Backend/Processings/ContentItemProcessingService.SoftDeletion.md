# ContentItem processing service — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: processing — `IContentItemProcessingService` (`Glory2Him.Core/Services/Processings/ContentItems/`). Its foundation's work for this feature is `Backend/Foundations/ContentItemService.SoftDeletion.md`.
Inherits: §APR9.7.6 rule 1, §APR9.7.7 rules 6 and 7, §DOM3.4.1, SoftDeletion.md rules 12, 15, 16 and 20

The processing tier already refuses an edit of a soft-deleted content item before it reaches its foundation, and keeps doing so (SoftDeletion.md rule 12). What changes here is code text: the publication swap's comments, and the tests that pin the swap, say that a soft-deleted row holds its group's published slot (SoftDeletion.md rule 20).

## 1. What the code says of the published slot (#1348)

No operation: the code text of `ContentItemProcessingService` and its unit tests that says, or rests on, a soft-deleted content item holding its group's published slot, or the slot index refusing a promote over one. It has a section of its own so that its task has a section to name.

1. **The texts** are the published read's comment on why `FindPublishedSiblingContentItemIdAsync` reads unfiltered (`ContentItemProcessingService.cs`); the swap's demote, its summary and its comment on why the demote's failure is not caught (`ContentItemProcessingService.Substrate.cs`); and, in its unit tests, the swap's summary, its ordering assertion, its tombstone test's comment and its probe stub's summary (`ContentItemProcessingServiceTests.PublicationSwap.cs`). Its task fixes each text's words, before and after.
2. **What they say instead** (SoftDeletion.md rules 15 and 16; §APR9.7.7 rule 7): the slot index excludes deleted rows, so a tombstone holds no slot and cannot block the promote. It can still carry `IsPublished`, the unfiltered probe still finds it, and the unpublish clears its flag, so that it never stands beside the promoted row claiming to be the group's published version (§APR9.7.6 rule 1). Where the incumbent is live, the index refuses a promote that runs before the demote. *"A soft delete never clears IsPublished"* stays wherever a text says it, true until the removal's demotion is built (#1329).
3. **Nothing runs differently.** It changes comments and summaries alone, and the swap's tests run as they do.

## Deviations

None.
