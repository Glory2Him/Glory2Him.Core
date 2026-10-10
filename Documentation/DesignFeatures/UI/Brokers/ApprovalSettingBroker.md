# Approval setting broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ApprovalSettingBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.approvalSettings.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/ApprovalSettings`, the §APR8.4 policy rows. Two members hold logic today (`BrokersHoldNoLogic.md` rule 2). `GetApprovalSettingsAsync` writes a fixed `$filter` and a fixed `$orderby`, and `RemoveApprovalSettingByIdAsync` trims the deletion reason and leaves a blank one out. Each gains a replacement that sends what it is handed, `approvalSettingService` writes what they send (`UI/Foundations/ApprovalSettingService.md`), and the old members go (§UI20.9.3 rule 6). Its other members hold none, and their broker tests go (§5).

## 1. GetApprovalSettingsByQueryAsync (#941)

```ts
GetApprovalSettingsByQueryAsync(query: ODataQuery): Promise<ApprovalSetting[]>
```

1. **It sends `GET /api/approvalsettings` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the rows as they came**, typed as `ApprovalSetting[]`, in the order they came.
3. **Its name carries `ByQuery`** because §3's member holds `GetApprovalSettingsAsync` (§UI20.9.3 rule 3).

## 2. DeleteApprovalSettingAsync (#942)

```ts
DeleteApprovalSettingAsync(approvalSettingId: string, deletionReason?: string): Promise<ApprovalSetting>
```

1. **It sends `DELETE /api/approvalsettings/<approvalSettingId>`, with `deletionReason` on the query string, encoded, when it is handed one**, and without it when the reason is `undefined` (§UI20.9.3 rule 4). It sends the reason it is handed as it is: deciding that a blank reason is no reason is the service's (`UI/Foundations/ApprovalSettingService.md §2`).
2. **It returns the row the server answers with**, typed as `ApprovalSetting`. The delete is soft, and the `/Hard` route stays unoffered, as today.
3. **It is named for the verb it sends**, as departure 2 names every new member, so it does not collide with `RemoveApprovalSettingByIdAsync` (§UI20.9.3 rule 3).

## 3. GetApprovalSettingsAsync — deleted (#971)

Deleted once `approvalSettingService.useGetApprovalSettings` calls §1 instead (`UI/Foundations/ApprovalSettingService.md §1`) and nothing calls it. Its two broker test cases under *reading the set*, *should leave closed rows behind* and *should order the defaults above the rows that override them*, go with it, having moved to `approvalSettingService.test.tsx`.

## 4. RemoveApprovalSettingByIdAsync — deleted (#972)

Deleted once `approvalSettingService.useRemoveApprovalSetting` calls §2 instead (`UI/Foundations/ApprovalSettingService.md §2`) and nothing calls it. Its two broker test cases under *closing one*, *should carry a stated reason into the audit trail* and *should ask plainly when no reason was given*, go with it, having moved to `approvalSettingService.test.tsx`.

## 5. Its unit tests of format conversion — deleted (#979)

The other three cases in `apiBroker.approvalSettings.test.ts` pin members that hold no logic, and a broker has no unit tests (§UI20.9.3 rule 8): *should post the scope and the policy and nothing about who or when* under *creating one*, and *should put to the collection rather than to the row* and *should send the audit fields the foundation checks against storage* under *amending one*. They are deleted, and no member changes. Whichever of this task, §3's and §4's removes the file's last case deletes the file (`BrokersHoldNoLogic.md` rule 5).
