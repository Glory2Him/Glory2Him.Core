# AI reviewer service (React)
Parent: [PostModeration.md](../../PostModeration.md)
Level: foundation — `aiReviewerService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/aiReviewerService.ts`), existing
Inherits: §UI20.9, §APR8.6.2 (*The confidence suggestion*)

The association page reads Berean's status through `useGetAIReviewerStatus`, inside `useApprovalRound` (`src/hooks/useApprovalRound.ts`), and the confidence suggestion is one member of what it answers. The hook passes the broker's answer through unchanged. Nothing tests it today, so this story pins that it hands the suggestion on as the broker returns it.

## 1. useGetAIReviewerStatus — answers the confidence suggestion (#1110)

1. **It answers `confidenceSuggestion` as the broker returns it**: each of the three members, and `null`, untouched (`UI/Brokers/AIReviewerBroker.PostModeration.md §1`).
2. **Its key, its staleness, its retry and its error handling are unchanged**: `['AIReviewerStatus', entityType, entityId]`, 15 seconds, no retry, and no global error toast.
