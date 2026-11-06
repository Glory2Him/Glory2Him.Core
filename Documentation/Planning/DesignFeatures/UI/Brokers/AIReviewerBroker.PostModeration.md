# AI reviewer broker — the confidence suggestion
Parent: [PostModeration.md](../../PostModeration.md)
Level: broker — `AIReviewerBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.aiReviewers.ts`), existing. The broker work of `BrokersHoldNoLogic.md` on the same broker is `UI/Brokers/AIReviewerBroker.md`.
Inherits: §UI20.9.1, §UI20.9.3, §APR8.6.2 (*The confidence suggestion*)

The association page reads Berean's status through this broker, and the confidence suggestion arrives on that answer (`Backend/Controllers/AIReviewersController.md §1`). The broker's code does not change: it hands back the body as the server answers it, and holds no logic (`BrokersHoldNoLogic.md` rule 3). What changes is the type it answers with, so this story is groundwork, with no test paths: a broker has no unit tests (§UI20.9.3 rule 8), and its proof is the type check `npm run build` runs.

## 1. GetAIReviewerStatusAsync — the confidence suggestion on its answer (#1109)

1. **`AIReviewerStatus`** (`src/models/foundations/approvals/approval.ts`) gains `confidenceSuggestion: ConfidenceSuggestion | null`, beside `isOffered`.
2. **`ConfidenceSuggestion`** is a numeric enum in the same file mirroring the server's — `Approval = 1`, `Rejection = 2`, `Unable = 3` — because the host registers no `JsonStringEnumConverter` and the numbers are what crosses the wire (`Backend/Orchestrations/AIReviewerOrchestrationService.md §1`).
3. **Nothing sends it.** It is a read's answer; no request the app builds carries it.
