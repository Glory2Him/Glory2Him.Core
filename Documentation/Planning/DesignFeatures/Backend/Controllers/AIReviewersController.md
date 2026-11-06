# AI reviewers controller
Parent: [PostModeration.md](../../PostModeration.md)
Level: exposer — `AIReviewersController` (`Websites/Glory2Him.WebApp/Controllers/AIReviewers/AIReviewersController.cs`), existing, §ARC12.6 row 13, bound to `IAIReviewerOrchestrationService` alone
Inherits: §APR8.6.2 (*The confidence suggestion*), §ARC12.6 row 13, §ARC17.5

The exposer is thin and its code does not change: `GetAIReviewerAsync` returns the `AIReviewerStatus` the orchestration answers, so the suggestion reaches the wire with it. What this story adds is the proof over HTTP, which acceptance tests give against an exposer.

## 1. GetAIReviewerAsync — the confidence suggestion over HTTP (#1108)

1. **Route and audience are unchanged:** `GET api/AIReviewers/{entityType}/{entityId}`, `[Authorize]`, the orchestration deciding the rest — the requesting tier (§APR7.9 rule 2), and the confidence suggestion only for the review tier of the entity behind the round (§APR8.6.2 suggestion rule 6). A caller outside the requesting tier is answered `401`, as today; an entity taken down, or a round that cannot be found, `404`, as today.
2. **The body carries `confidenceSuggestion`**, the number of the orchestration's `ConfidenceSuggestion` member, or `null` (`Backend/Orchestrations/AIReviewerOrchestrationService.md §1`).
3. **It moves with Berean's other routes** to `api/AIReviewers/{approvalId}` under #699 (§ARC17.5), and the field moves with it.
