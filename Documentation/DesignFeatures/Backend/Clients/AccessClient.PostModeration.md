# Access client — the confidence suggestion
Parent: [PostModeration.md](../../PostModeration.md)
Level: client — `IAccessClient` (`Clients/SecurityClients/G2H.Security.Client/Clients/Access/IAccessClient.cs`), `securityClient.Access`, the pure decision function of §APR8.6.1 rule 2, served by `AccessService`. The amender record's work on the same client is `Backend/Clients/AccessClient.md`.
Inherits: §APR8.6.1 rules 2, 4 and 5, §APR8.6.2 (*The confidence suggestion* rules 1, 2 and 4–6), §SEC14.7 posture A′ rules 2 and 3, posture D rule 2; `PostModeration.md` rule 13

The client decides the confidence suggestion and gathers nothing. `AccessBroker` hands it the entity's score, the round's role subjects, the actor and the confidence process's two configured thresholds (`Backend/Brokers/AccessBroker.PostModeration.md §1`). It reads no configuration of its own: where the thresholds are configured is #1117's design, and the client is handed the pair. Every member of the request is `required`, so a forgotten gather is a compile error rather than an empty list read as permission (§APR8.6.1 rule 2).

**A new operation, not a widening of `ResolveAIReviewerPolicyAsync`.** `AIReviewerPolicyVerdict` leaves the thresholds out because *"nothing that resolves this verdict casts a vote or reads a score"*, and that stays true: its two callers, the offer gate and the automatic assignment (§APR8.6.2.1 gate 4), still read no score. The suggestion also depends on who asks (§APR8.6.2 suggestion rule 6), and the automatic assignment resolves that verdict under the roleless system identity, so carrying the suggestion there would give an actor-independent verdict an actor.

**It stays one operation.** Its logic is five criteria, with the band's three outcomes and its comparisons proven as one rule (§1 rule 5) with cases. The band (suggestion rule 1) and who is answered it (rule 6) are one answer: splitting them would hand the band to a caller before anything decided they may see it. This is the shape `MayRecordApprovalReviewAsync` takes, deciding the veto, the tier, HR-1 and the window in one call.

## 1. ResolveConfidenceSuggestionAsync (#1105)

```csharp
ValueTask<ConfidenceSuggestionVerdict> ResolveConfidenceSuggestionAsync(
    ResolveConfidenceSuggestionRequest resolveConfidenceSuggestionRequest);
```

1. **`ResolveConfidenceSuggestionRequest`** carries, each `required`: `Actor` (`AccessActor`), `RoleSubjects` (`IReadOnlyList<RoleSubject>`), `ConfidenceScore` (`decimal?`), `RejectionThreshold` (`decimal`) and `ApprovalThreshold` (`decimal`). The two thresholds are the confidence process's configured pair (§APR8.6.2 suggestion rule 1), never an approval-setting row's.
2. **`ConfidenceSuggestionVerdict`** carries one member, `Suggestion`, of the new enum `ConfidenceSuggestion?` — `Approval = 1`, `Rejection = 2`, `Unable = 3`, with no zero member, so a default value is never read as a band. `null` is *no suggestion*.
3. **The actor is asked first.** An actor outside the review tier composed from `RoleSubjects` is answered no suggestion, whatever the score (§APR8.6.2 suggestion rule 6). The tier is the one `MayRecordApprovalReviewAsync` asks, one matching subject admitting. A read-only role does not withhold it (§SEC14.7 posture A′ rule 3), so the veto is not asked.
4. **A null `ConfidenceScore` answers no suggestion** (suggestion rule 2).
5. **The band is suggestion rule 1's**, against the pair it is handed: `Rejection` at or below the rejection threshold, `Approval` strictly above the approval threshold, `Unable` otherwise (user ruling 2026-10-13).
6. **No switch is read.** The request carries none, so neither `IsAIReviewerOffered` nor `IsAIAllowedToVote` can govern the answer (suggestion rule 4), and no approval-setting row is resolved.
7. **Validation:** a null request, and a null `Actor` or `RoleSubjects`, is refused with the validation exception the client's other requests are refused with, naming the member. The client does not check the configured pair's scale or order: whether anything does is #1117's design.
