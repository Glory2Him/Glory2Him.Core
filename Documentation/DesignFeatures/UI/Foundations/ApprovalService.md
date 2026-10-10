# Approval service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `approvalService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/approvalService.ts`), existing
Inherits: §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/ApprovalBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `ApprovalBroker`. Two of them hand their broker work it should not hold (`BrokersHoldNoLogic.md` rule 2): the reviews read, whose filter `GetApprovalReviewsAsync` writes, and the decision, whose bypass reason `PostApprovalDecisionAsync` trims. Each takes that over and calls the broker's new member (`UI/Brokers/ApprovalBroker.md` §1 and §2). What the pages see does not change.

## 1. useGetApprovalReviews (#952)

```ts
useGetApprovalReviews: (approvalId: string, enabled?: boolean) => UseQueryResult<ApprovalReview[]>
```

1. **It asks for the reviews of the approval it is handed, and leaves the withdrawn ones behind**: the filter `approvalId eq <approvalId> and isDeleted eq false`, the id as it is handed. A withdrawn review is no opinion, so the server drops it.
2. **Its query key, `['ApprovalReviews', approvalId]`, its `enabled` rule, its `retry`, its `meta` and its stale time are unchanged.**

## 2. useDecideApproval (#953)

```ts
useDecideApproval: () => UseMutationResult<ApprovalOutcome, unknown, {
    entityType: EntityTypeName; entityId: string; decision: ApprovalDecision;
    isBypassRequested: boolean; bypassReason: string }>
```

1. **It sends the bypass reason the reader typed, trimmed**, through `PostApprovalDecisionByQueryAsync` (`UI/Brokers/ApprovalBroker.md §2`), with the entity, the decision and whether a bypass is asked for as it is handed them.
2. **A reason that is blank once trimmed is no reason**, and it hands the broker none, so the request carries no `bypassReason` rather than an empty one. The orchestration reads a missing reason as none supplied, and validates a bypass against that.
3. **What it invalidates, and its `meta`, are unchanged.**
