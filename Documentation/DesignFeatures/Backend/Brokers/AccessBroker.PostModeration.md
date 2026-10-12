# Access broker — the confidence suggestion
Parent: [PostModeration.md](../../PostModeration.md)
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`), the policy gatherer of §APR8.6.1 rule 3. The Likes feature's work on the same broker is `Backend/Brokers/AccessBroker.md`, and the amender record's is `Backend/Brokers/AccessBroker.Amenders.md`.
Inherits: §APR8.4, §APR8.6.1 rules 3–5, §APR8.6.2 (*The confidence suggestion*), §SEC14.6 rule 4, §ARC12.2.1 rules 3–6

`AccessBroker` gathers and does not decide. Its gathers are unit-tested, which is where this solution tests them (`AccessBrokerTests.*.Logic.cs`). The decision is the client's (`Backend/Clients/AccessClient.PostModeration.md §1`).

## 1. ResolveConfidenceSuggestionByIdAsync (#1106)

```csharp
ValueTask<ConfidenceSuggestionVerdict?> ResolveConfidenceSuggestionByIdAsync(
    Guid approvalId,
    Models.Events.SecurityContext securityContext,
    CancellationToken cancellationToken = default);
```

1. **It answers `null` when no approval carries the id**, as `ResolveAIReviewerPolicyByIdAsync` does, so a caller cannot read a manufactured *no suggestion* as an answer.
2. **Everything else comes off the stored approval's target**, never a payload: the approval read by id, then the per-`EntityType` read of the row behind it (`ResolveEntityAsync`), which already answers the round's role subjects, the entity's `ConfidenceScore` and, on an association, its personality; then the candidate policy rows (`GatherPoliciesAsync`). The policy key is built as `ResolveAIReviewerPolicyByIdAsync` builds it: the content type for a `ContentItem` round alone, and `IsPersonal` as the row answers it.
3. **The actor is the envelope's.** The `SecurityContext` is the one the caller's envelope carries, turned into an `AccessActor` the way every other `May*` gather turns it (`BuildActorAsync`), never read from an ambient accessor (§APR8.6.1 rule 5).
4. **No review, comment or assignment is gathered.** The suggestion needs none of them.
