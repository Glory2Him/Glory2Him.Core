# Content item reaction option
Parent: [Likes.md](../../Likes.md)
Level: view — `toContentItemReactionOption` (`Websites/Glory2Him.WebApp.React/src/services/views/contentItems/toContentItemReactionOption.ts`), existing
Inherits: `UI/Components/ContentItemPanel.md rule 3.1.8`

The card's Like control offers `ContentItemReactionOption`s, and this view service projects each one from a row of the reaction vocabulary. An option is identified by its `label` today, which was all the page-state choice needed. Recording a choice on the server needs the reaction's id (`UI/Hooks/ContentItemEngagement.md §2`).

## 1. toContentItemReactionOption (#755)

1. **Each option carries the reaction's `id`**, mapped from the vocabulary row's `id`, beside its `label`, `glyph` and `isLove`.
2. **The `label` stays the option's identity everywhere else**, in the pressed mark and the `isLove` narrowing (`contentItemSearchItem.ts`), and nothing else about the projection changes.
