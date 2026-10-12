# Content item model (React)
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: model — `ContentItem` (`Websites/Glory2Him.WebApp.React/src/models/foundations/contentItems/contentItem.ts`), the React app's shape of the item the API returns
Inherits: §APR8.6.3 rule 8, §UI20.9

The page hands `ReviewPanel` the item's amender record off the stored item (§APR8.6.3 rule 8), so the React model of the item names it. A model change alone has no test paths; its proof is the type check `npm run build` runs.

## 1. amenderUserIds (#1019)

1. **`ContentItem` gains `amenderUserIds: string[]`**, the item's `AmenderUserIds` as the API returns it (§APR8.6.3 rule 1), beside `createdBy`.
2. **Nothing writes it.** The server derives the record and ignores whatever a caller sends (§SEC14.7 posture A rule 6), so no form or broker sets it.
