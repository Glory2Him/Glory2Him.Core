# Access client — the confidence suggestion
Parent: [PostModeration.md](../../PostModeration.md)
Level: client — `IAccessClient` (`Clients/SecurityClients/G2H.Security.Client/Clients/Access/IAccessClient.cs`), `securityClient.Access`, the pure decision function of §APR8.6.1 rule 2, served by `AccessService`. The amender record's work on the same client is `Backend/Clients/AccessClient.md`.
Inherits: §APR8.4 (the tiers, and rule 2's system default), §APR8.6.1 rules 2, 4 and 5, §APR8.6.2 (*The confidence suggestion* rules 1–6), §SEC14.7 posture A′ rules 2 and 3, posture D rule 2; `PostModeration.md` rule 13

The client decides the confidence suggestion and gathers nothing: `AccessBroker` hands it the candidate policy rows, the policy key, the entity's score, the round's role subjects and the actor (`Backend/Brokers/AccessBroker.PostModeration.md §1`). Every section of the request is `required`, so a forgotten gather is a compile error rather than an empty list read as permission (§APR8.6.1 rule 2).

**A new operation, not a widening of `ResolveAIReviewerPolicyAsync`.** `AIReviewerPolicyVerdict` leaves the thresholds out because *"nothing that resolves this verdict casts a vote or reads a score"*, and that stays true: its two callers, the offer gate and the automatic assignment (§APR8.6.2.1 gate 4), still read no score. The suggestion also depends on who asks (§APR8.6.2 suggestion rule 6), and the automatic assignment resolves that verdict under the roleless system identity, so carrying the suggestion there would give an actor-independent verdict an actor.

**Its logic runs past eight criteria, and it stays one operation.** The band (suggestion rules 1–3) and who is answered it (rule 6) are one answer: splitting them would hand the band to a caller before anything decided they may see it. This is the shape `MayRecordApprovalReviewAsync` takes, deciding the veto, the tier, HR-1 and the window in one call.

## 1. ResolveConfidenceSuggestionAsync (#1105)

```csharp
ValueTask<ConfidenceSuggestionVerdict> ResolveConfidenceSuggestionAsync(
    ResolveConfidenceSuggestionRequest resolveConfidenceSuggestionRequest);
```

1. **`ResolveConfidenceSuggestionRequest`** carries, each `required`: `Actor` (`AccessActor`), `RoleSubjects` (`IReadOnlyList<RoleSubject>`), `CandidatePolicies` (`IReadOnlyList<ApprovalPolicy>`), `EntityType` (`string`), `ContentType` (`string?`), `IsPersonal` (`bool?`) and `ConfidenceScore` (`decimal?`). The last four mean what they mean on `ApprovalConditionsRequest`.
2. **`ConfidenceSuggestionVerdict`** carries one member, `Suggestion`, of the new enum `ConfidenceSuggestion?` — `Approval = 1`, `Rejection = 2`, `Unable = 3`, with no zero member, so a default value is never read as a band. `null` is *no suggestion*.
3. **The actor is asked first.** An actor outside the review tier composed from `RoleSubjects` is answered no suggestion, whatever the score and the policy (§APR8.6.2 suggestion rule 6). The tier is the one `MayRecordApprovalReviewAsync` asks, one matching subject admitting. A read-only role does not withhold it (§SEC14.7 posture A′ rule 3), so the veto is not asked.
4. **A null `ConfidenceScore` answers no suggestion** (suggestion rule 2).
5. **The policy resolves by §APR8.4's tiers**, as `EvaluateApprovalConditionsAsync` resolves it, the first matching row supplying both thresholds. **Where no row matches, no suggestion is answered**, and the system default's pair is never compared (suggestion rule 3).
6. **The band is rules 1–3's** (suggestion rule 1): `Rejection` below the rejection threshold, `Approval` above the approval threshold, `Unable` otherwise, both comparisons strict.
7. **`IsAIReviewerOffered` and `IsAIAllowedToVote` are not read** (suggestion rule 4).
8. **Validation:** a null request, and a null `Actor`, `RoleSubjects` or `CandidatePolicies`, or an invalid `EntityType`, is refused with the validation exception the client's other requests are refused with, naming the member.
