# Approval broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ApprovalBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.approvals.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to the approval round: the orchestration routes under `api/Approvals` and the plain collection `api/ApprovalReviews`. Two of its members hold logic today (`BrokersHoldNoLogic.md` rule 2). `GetApprovalReviewsAsync` writes the reviews' `$filter` from the approval id it is handed, and `PostApprovalDecisionAsync` trims the bypass reason and leaves a blank one out. Each gains a replacement that sends what it is handed, `approvalService` writes what they send (`UI/Foundations/ApprovalService.md`), and the old members go (§UI20.9.3 rule 6). Its other members hold none, and their broker tests go (§5).

## 1. GetApprovalReviewsByQueryAsync (#938)

```ts
GetApprovalReviewsByQueryAsync(query: ODataQuery): Promise<ApprovalReview[]>
```

1. **It sends `GET /api/approvalreviews` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the reviews as they came**, typed as `ApprovalReview[]`.
3. **Its name carries `ByQuery`** because §3's member holds `GetApprovalReviewsAsync` (§UI20.9.3 rule 3).

## 2. PostApprovalDecisionByQueryAsync (#939)

```ts
PostApprovalDecisionByQueryAsync(
    entityType: EntityTypeName,
    entityId: string,
    decision: ApprovalDecision,
    isBypassRequested: boolean,
    bypassReason?: string): Promise<ApprovalOutcome>
```

1. **It sends `POST /api/approvals/<entityType>/<entityId>/Decision` with an empty body**, and on the query string `decision` as its number, `isBypassRequested` as `true` or `false`, and `bypassReason`, encoded, when it is handed one. A reason that is `undefined` is left out (§UI20.9.3 rule 4). It sends the reason it is handed as it is: deciding that a blank reason is no reason is the service's (`UI/Foundations/ApprovalService.md §2`).
2. **It returns the outcome the server answers with**, typed as `ApprovalOutcome`.
3. **Its name carries `ByQuery`** because §4's member holds `PostApprovalDecisionAsync`, and everything the decision carries rides the query string (§UI20.9.3 rule 3).

## 3. GetApprovalReviewsAsync — deleted (#968)

Deleted once `approvalService.useGetApprovalReviews` calls §1 instead (`UI/Foundations/ApprovalService.md §1`) and nothing calls it. Its broker test case, *should ask for the reviews by approval, and leave the withdrawn ones behind*, goes with it, having moved to `approvalService.test.tsx`.

## 4. PostApprovalDecisionAsync — deleted (#969)

Deleted once `approvalService.useDecideApproval` calls §2 instead (`UI/Foundations/ApprovalService.md §2`) and nothing calls it. Its two broker test cases, *should post the decision by entity with the bypass and its reason on the query* and *should leave the reason off a plain decision*, go with it, having moved to `approvalService.test.tsx`.

## 5. Its unit tests of format conversion — deleted (#977)

The other six cases in `apiBroker.approvals.test.ts` pin members that hold no logic, and a broker has no unit tests (§UI20.9.3 rule 8): *should ask for the verdict by entity, naming the type rather than numbering it*, *should ask for the candidates and the requests by entity*, *should ask for every reviewer name in one request keyed on the round*, *should post a vote as a review row carrying no audit fields*, *should put a changed vote to the collection with the row it was read as*, and *should post and delete a review request by entity, naming who was asked*. They are deleted, and no member changes. Whichever of this task, §3's and §4's removes the file's last case deletes the file (`BrokersHoldNoLogic.md` rule 5).
