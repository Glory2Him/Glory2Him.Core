# Approval comment service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `approvalCommentService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/approvalCommentService.ts`), existing
Inherits: §UI20.9.2, §UI20.9.3 rule 2, `UI/Brokers/ApprovalCommentBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `ApprovalCommentBroker`. One of them hands its broker a read whose filter `GetApprovalCommentsAsync` writes (`BrokersHoldNoLogic.md` rule 2). It takes the filter over and asks `GetApprovalCommentsByQueryAsync` (`UI/Brokers/ApprovalCommentBroker.md §1`). What the pages see does not change.

## 1. useGetApprovalComments (#954)

```ts
useGetApprovalComments: (approvalId: string, enabled?: boolean) => UseQueryResult<ApprovalComment[]>
```

1. **It asks for the thread of the approval it is handed, and leaves the withdrawn comments behind**: the filter `approvalId eq <approvalId> and isDeleted eq false`, the id as it is handed. A withdrawn comment is not part of the conversation, so the server drops it.
2. **Its query key, `['ApprovalComments', approvalId]`, its `enabled` rule, its `retry`, its `meta` and its stale time are unchanged.**
