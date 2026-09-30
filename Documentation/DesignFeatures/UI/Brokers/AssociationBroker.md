# Association broker
Parent: [Likes.md](../../Likes.md)
Level: broker — `AssociationBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.associations.ts`), new
Inherits: §ARC16.8 (*The route*), §ARC16.8.1, §ARC17.4, `Backend/Controllers/AssociationsController.md`, `the-standard-reacttypescript-brokers`

The React app's door to the three association routes the Likes feature serves. It follows the app's broker shape — a default-exported class in `src/brokers/apiBroker.<resource>.ts`, holding an `ApiBroker` and one `<Verb><Entity>Async` member per call (`apiBroker.reactions.ts`, `apiBroker.contentItems.ts`) — and holds no logic: it forms the request, sends it and hands back what came.

**What crosses the wire is the server's shape.** An enum travels as its number, because the host registers no `JsonStringEnumConverter` (`src/models/foundations/contentItemSettings/contentType.ts`); `EntityType` is the app's existing mirror of it (`src/models/foundations/approvalSettings/approvalSetting.ts`). The wire models are new, under `src/models/foundations/associations/`: the request — the two endpoints, `entityAType`, `entityAKeyId`, `entityBType`, `entityBKeyId`, and nothing else (§ARC16.8.1); the suggestion result — `status` and `associationId`, the status a numeric mirror of `AssociationSuggestionStatus` in the server's order, `Repointed` last; and the summary — §ARC16.8's projection, `contentItemId`, `reactions` (each `reactionId`, `name`, `unicodeEmoji`, `count`), `viewerReactionId` and `viewerReactionName`, carrying the entity's names and not the view's (§ARC16.8, *The projection*). Each model is added by the first member that sends or receives it.

## 1. PostAssociationAsync (#731)

Sends the request as the body of `POST /api/associations` and returns the suggestion result.

## 2. DeleteAssociationPairAsync (#732)

Sends `DELETE /api/associations/pair`, the request's four fields on the query string by those names, the two types as their numbers and the two ids as they are (§ARC16.8.1; `Backend/Controllers/AssociationsController.md §2`). `ApiBroker.DeleteAsync` sends no body, which is why the pair travels on the query string. Returns nothing.

## 3. GetReactionSummariesAsync (#733)

Sends `GET /api/associations/reactionsummaries` with one `contentItemIds` parameter per id it is handed, in the order handed, and returns the summaries. It sends exactly the ids it is given: bounding and chunking the set is its caller's (§ARC16.8, *The set, its bounds*; `UI/Foundations/AssociationService.md §3`).

## Deviations

One, from `the-standard-reacttypescript-brokers`. It is awaiting the owner's approval.

1. **tsr-brokers-003 and tsr-brokers-015 — *"Brokers MUST catch external exceptions and wrap them in broker-specific exceptions."*** No member catches or wraps: a failed call rejects with the `AxiosError` that `ApiBroker` raised, unchanged. **Why:** the app's callers read that error as it is. `toContentItemApiFailure.ts` and `apiErrorMessage.ts` read the server's problem body off `error.response.data`, and `contributorService.ts` reads a `404` off `error.response.status`. Each tests the error with `axios.isAxiosError` first. A broker-specific exception would hide all three unless it copied the response through, which is the same error under another name. No broker in the app wraps (`src/brokers/apiBroker.*.ts`), and this one follows them. **Instead:** the error reaches the service unchanged, and the app's global handler announces it (`apiBroker.globals.ts`) unless the caller opts out with `meta.suppressGlobalErrorToast`.
