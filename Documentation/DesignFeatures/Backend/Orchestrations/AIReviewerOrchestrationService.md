# AI reviewer orchestration service
Parent: [PostModeration.md](../../PostModeration.md)
Level: orchestration — `IAIReviewerOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/AIReviewers/`), existing, §ARC12.5 entry 4
Inherits: §APR8.6.2 (*The confidence suggestion*), §APR8.6.1 rules 4 and 5, §APR7.9 rule 2, §SEC14.5 rule 3, §ARC12.5 (the broker rule), §ARC16.7.1; `PostModeration.md` rule 13

Berean's status on a round is this service's read, and the confidence suggestion rides on it (`PostModeration.md`, *Layer placement*). It spans `Approval` and `AIReviewerAssignment` already, and the suggestion adds no dependency: it is gathered through the `IAccessBroker` the service holds, and a broker costs no exception family and no place in the two-to-three count (§ARC12.5). The read is addressed by the item today, `Association` and the association's id, and moves to the round's approval id with the rest of Berean's routes under #699 (§ARC17.5).

## 1. RetrieveAIReviewerStatusAsync — the confidence suggestion (#1107)

1. **`AIReviewerStatus` gains `ConfidenceSuggestion`**, of a new enum `ConfidenceSuggestion?` in `Glory2Him.Core.Models.Orchestrations.AIReviewers` — `Approval = 1`, `Rejection = 2`, `Unable = 3` — beside `IsOffered`. The host registers no `JsonStringEnumConverter`, so these numbers are what crosses the wire, and the React mirror is numbered to match (`UI/Brokers/AIReviewerBroker.PostModeration.md §1`).
2. **It is the answer of `IAccessBroker.ResolveConfidenceSuggestionByIdAsync`** (`Backend/Brokers/AccessBroker.PostModeration.md §1`) for the resolved round's id, mapped member for member, and `null` where the broker answers `null` or a verdict with no suggestion.
3. **The security context it hands the broker is the caller's**, off the envelope the read already creates to gate its caller (`ResolveAIReviewerApprovalAsync`), never the system identity and never an ambient accessor (§APR8.6.1 rule 5).
4. **It is asked after every gate the read already asks** — the requesting tier, the visibility probe and the round's lookup — so a caller the read refuses today is refused before anything is gathered, and the refusals do not change.
5. **Nothing else on the status changes**: `IsOffered`, `IsRequested` and the two completion flags are read as today, and the suggestion is answered whatever they say (§APR8.6.2 suggestion rules 4 and 5).
