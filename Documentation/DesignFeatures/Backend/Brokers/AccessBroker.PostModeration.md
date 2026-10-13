# Access broker — the confidence suggestion
Parent: [PostModeration.md](../../PostModeration.md)
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`), the policy gatherer of §APR8.6.1 rule 3. The Likes feature's work on the same broker is `Backend/Brokers/AccessBroker.md`, and the amender record's is `Backend/Brokers/AccessBroker.Amenders.md`.
Inherits: §APR8.6.1 rules 3–5, §APR8.6.2 (*The confidence suggestion*), §SEC14.6 rule 4, §ARC12.2.1 rules 3–6

`AccessBroker` gathers and does not decide. Its gathers are unit-tested, which is where this solution tests them (`AccessBrokerTests.*.Logic.cs`). The decision is the client's (`Backend/Clients/AccessClient.PostModeration.md §1`).

## 1. ResolveConfidenceSuggestionByIdAsync (#1106)

```csharp
ValueTask<ConfidenceSuggestionVerdict?> ResolveConfidenceSuggestionByIdAsync(
    Guid approvalId,
    Models.Events.SecurityContext securityContext,
    CancellationToken cancellationToken = default);
```

1. **It answers `null` when no approval carries the id**, as `ResolveAIReviewerPolicyByIdAsync` does, so a caller cannot read a manufactured *no suggestion* as an answer.
2. **The score and the role subjects come off the stored approval's target**, never a payload: the approval read by id, then the per-`EntityType` read of the row behind it (`ResolveEntityAsync`), which already answers the round's role subjects and the entity's `ConfidenceScore`.
3. **The thresholds are the confidence process's configured pair** (§APR8.6.2 suggestion rule 1), read as #1117's design has the server read them. No approval-setting row is gathered for them, and no policy key is built. **This member waits on #1117's design**: until it is on `main`, nothing says where the pair is read from.
4. **The actor is the envelope's.** The `SecurityContext` is the one the caller's envelope carries, turned into an `AccessActor` the way every other `May*` gather turns it (`BuildActorAsync`), never read from an ambient accessor (§APR8.6.1 rule 5).
5. **No review, comment, assignment or policy row is gathered.** The suggestion needs none of them.
