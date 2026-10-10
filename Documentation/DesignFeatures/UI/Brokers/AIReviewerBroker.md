# AI reviewer broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `AIReviewerBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.aiReviewers.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/AIReviewers`, Berean's assignment (§APR8.6.2). None of its members holds logic (`BrokersHoldNoLogic.md` rule 3): `DeleteAIReviewerAsync` reads the empty answer to a `204` as `null`, which is format conversion (§UI20.9.3 rule 4). It has unit tests all the same, and a broker has none.

## 1. Its unit tests of format conversion — deleted (#976)

`apiBroker.aiReviewers.test.ts` and its five cases are deleted, since each pins format conversion and a broker has no unit tests (§UI20.9.3 rule 8): *should read, assign and withdraw the AI reviewer at one entity-keyed address*, *should not address the AI reviewer through the approval round*, *should assign the AI reviewer with an empty body*, *should answer with the assignment the withdrawal removed*, and the `it.each` case *should answer nothing when the withdrawal removed nothing (%j)*, run for an empty string, `undefined` and `null`. No member changes.
