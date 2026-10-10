# Approval comment broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ApprovalCommentBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.approvalComments.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/ApprovalComments`, the review thread. One member holds logic today (`BrokersHoldNoLogic.md` rule 2): `GetApprovalCommentsAsync` writes the thread's `$filter` from the approval id it is handed. It gains a replacement that sends the query it is handed, `approvalCommentService` writes the query (`UI/Foundations/ApprovalCommentService.md §1`), and the old member goes (§UI20.9.3 rule 6). Its other members hold none, and their broker tests go (§3).

## 1. GetApprovalCommentsByQueryAsync (#940)

```ts
GetApprovalCommentsByQueryAsync(query: ODataQuery): Promise<ApprovalComment[]>
```

1. **It sends `GET /api/approvalcomments` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the comments as they came**, typed as `ApprovalComment[]`.
3. **Its name carries `ByQuery`** because §2's member holds `GetApprovalCommentsAsync` (§UI20.9.3 rule 3).

## 2. GetApprovalCommentsAsync — deleted (#970)

Deleted once `approvalCommentService.useGetApprovalComments` calls §1 instead (`UI/Foundations/ApprovalCommentService.md §1`) and nothing calls it. Its broker test case, *should ask for the thread by approval, and leave the withdrawn ones behind*, goes with it, having moved to `approvalCommentService.test.tsx`.

## 3. Its unit tests of format conversion — deleted (#978)

The other four cases in `apiBroker.approvalComments.test.ts` pin members that hold no logic, and a broker has no unit tests (§UI20.9.3 rule 8): *should post a comment as a body, with the id the caller minted*, *should put the whole row, since four fields are pinned against storage*, *should soft delete with the reason on the query string*, and the `it.each` case *should always send the resolution flag, including %s*, run for `true` and `false`. They are deleted, and no member changes. Whichever of this task and §2's removes the file's last case deletes the file (`BrokersHoldNoLogic.md` rule 5).
