# Storage broker — soft deletion
Parent: [SoftDeletion.md](../../SoftDeletion.md)
Level: broker — `IStorageBroker` (`Glory2Him.Core/Brokers/Storages/Sql/`), the SQL storage broker, one partial per entity. The Likes feature's work on the same broker is `Backend/Brokers/StorageBroker.md`, and the amender record's is `StorageBroker.Amenders.md`.
Inherits: §DOM3.4.1, §ARC12.2.1, SoftDeletion.md rules 16 and 17

The broker's published-row reads say that a soft-deleted row holds its group's published slot, and the integration tests that prove those reads repeat it. This story rewrites that text, and changes no member (SoftDeletion.md rule 17).

## 1. What the published-row reads say of the published slot (#1344)

No operation: the code text that says, or rests on, a soft-deleted content item or link holding its group's published slot. It has a section of its own so that its task has a section to name.

1. **The texts** are the summaries of `SelectPublishedContentItemInGroupAsync` and `SelectPublishedLinkInGroupAsync` (`IStorageBroker.ContentItem.cs`, `IStorageBroker.Link.cs`), and, in the integration tests that run those reads against SQL Server, a test's summary and two test names (`Glory2Him.Core.Tests.Integration/Services/Foundations/ContentItems/ContentItemNarrowReadTests.cs`, `…/Links/LinkNarrowReadTests.cs`). Its task fixes each text's words, before and after.
2. **What they say instead** (SoftDeletion.md rule 16; §DOM3.4.1): each read is unfiltered on `IsDeleted`, so it finds a tombstone that kept `IsPublished`. That tombstone holds no slot, because the slot index excludes deleted rows. *"A soft delete never clears IsPublished"* stays wherever a text says it, true until the removal's demotion is built (#1329).
3. **Nothing runs differently.** It changes summaries and test names alone. Each renamed test passes under its new name, and no read's predicate moves: where a read's condition belongs is §ARC12.2.1's, and its conversion is not part of this.

## Deviations

None.
