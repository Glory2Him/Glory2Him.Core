# Approval setting service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `approvalSettingService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/approvalSettingService.ts`), existing
Inherits: §APR8.4, §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/ApprovalSettingBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `ApprovalSettingBroker`. Two of them hand their broker work it should not hold (`BrokersHoldNoLogic.md` rule 2): the list read, whose filter and order `GetApprovalSettingsAsync` writes, and the removal, whose reason `RemoveApprovalSettingByIdAsync` trims. Each takes that over and calls the broker's new member (`UI/Brokers/ApprovalSettingBroker.md` §1 and §2). What the pages see does not change.

## 1. useGetApprovalSettings (#955)

```ts
useGetApprovalSettings: () => UseQueryResult<ApprovalSetting[]>
```

1. **It asks for the live set and leaves the closed rows behind**: the filter `isDeleted eq false`. A closed setting is not a policy.
2. **It asks for the defaults above the rows that override them**: `orderBy` `entityType,contentType`.
3. **It asks for no page.** The set is bounded by entity types times content types, small enough to read whole, and the list pages it in the browser.
4. **Its query key, `['ApprovalSettingsGetAll']`, and its stale time are unchanged.**

## 2. useRemoveApprovalSetting (#956)

```ts
useRemoveApprovalSetting: () => UseMutationResult<ApprovalSetting, unknown, { approvalSettingId: string; deletionReason?: string }>
```

1. **It sends the reason the administrator typed, trimmed**, through `DeleteApprovalSettingAsync` (`UI/Brokers/ApprovalSettingBroker.md §2`), so it reaches the audit trail.
2. **A reason that is missing, or blank once trimmed, is no reason**, and it hands the broker none, so the request carries no `deletionReason` rather than an empty one: sending `deletionReason=` would say a reason was given and lost.
3. **What it invalidates, and its `meta`, are unchanged.**
