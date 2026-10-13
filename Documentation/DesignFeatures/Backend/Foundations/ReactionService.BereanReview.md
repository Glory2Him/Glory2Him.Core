# Reaction service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IReactionService` (`Glory2Him.Core/Services/Foundations/Reactions/`). The Likes feature's work on the same service is `Backend/Foundations/ReactionService.md`, and the amender record's `ReactionService.Amenders.md`.
Inherits: §APR8.5, §APR9.7.1 rules 2 and 5, §EVN2, §EVN19, §SEC14.6 rule 4, §SEC14.6.1, BereanReview.md rules 1, 2, 7, 9, 10 and 19

`Reaction` implements `IApproval`, which now extends `IConfidence` (`Backend/Models/IApproval.md §1`), so a reaction carries a confidence score and a reason (BereanReview.md rule 2). Only the system identity writes them, through set-confidence (rule 1). §1 and §2 keep every other write from carrying them, and they land before the columns are mapped (`Backend/Models/IApproval.md §2`): mapped first, a caller's score would be stored on any path not yet refusing it.

**Not yet scheduled.** Every section here waits for the confidence process's design, which carves them (BereanReview.md, *What ships together* step 5): until something can write a score on this item, none of them changes anything.

## 1. AddReactionAsync — the confidence fields refused (needs issue)

1. **An add that carries any of `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` or `ModelVersion` is refused**, with a validation failure naming each field that is set, as the add already refuses `IsPublished` and `PublishDate` (`IsSetOnAdd`). The fields arrive null on every honest add, so nothing that works today changes.
2. It is asked on the direct path and on the `Reaction-Adding` door alike, because both converge on the add's one validation.

## 2. ModifyReactionAsync — a change to the confidence fields refused (needs issue)

1. **A modify whose `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` or `ModelVersion` differs from the stored row's is refused**, with a validation failure naming each field, as the modify already refuses a change to `IsApprovedByBypass` (`IsNotSame`). A modify that echoes the stored values is accepted.
2. It is asked on the direct path and on the `Reaction-Modifying` door alike.

## 3. SetReactionConfidenceAsync (needs issue)

```csharp
ValueTask<Reaction> SetReactionConfidenceAsync(
    Reaction reaction,
    CancellationToken cancellationToken = default);
```

1. **Only the system identity may call it** (BereanReview.md rule 1): the envelope's security context has `IsSystemIdentity = true`, which only this system mints onto a signed envelope (§SEC14.6 rule 4, §SEC14.6.1). Every other caller is refused, whatever its roles, the reaction's owner and `Administrators` included. It asks no role and no owner test.
2. **It writes the four `IConfidence` fields as one unit, and nothing else**; every other field is taken from the stored row (§APR9.7.1 rule 5). It reads the row by the reaction's `Id`, and a row that is missing or soft deleted is refused as not found.
3. **Two shapes, and only two.** *A score:* `ConfidenceScore` between `0.00` and `10.00` with at most two decimal places, a `ConfidenceReason` of 1 to 500 characters, a non-empty `SourceBatchId`, and a `ModelVersion` of 1 to 128 characters. *A clear:* all four null. Anything else — a score without a reason, a reason without a score, a score without its provenance — is refused, naming the fields.
4. **A score lands only on a row with none** (BereanReview.md rule 7). On a row that already holds one it writes nothing, publishes nothing and answers the stored row. A clear writes all four null on a row that holds a score and publishes; on a row that holds none it writes nothing and publishes nothing.
5. **It publishes `Reaction-ConfidenceSet`**, never `Reaction-Modified`, so writing a score never re-enters the approval workflow (§APR9.7.1 rule 5). `UpdatedBy` names the system and `UpdatedWhen` is stamped, as every write is (§SEC14.6.1).
6. Its failures map as the foundation's other transitions do: validation, dependency validation, dependency and service, and its token's cancellation and timeout.

## 4. OnSettingReactionConfidenceAsync (needs issue)

The `Reaction-SettingConfidence` request door, new. Address and subscription are registered under stable identifiers (`EventBrokerIdentifiers.Reaction.cs`, `EventSubscriptionRegistration`). It converges on §3's private body, so §3 proves the gate, the shapes and the write; this door adds what a handler adds (`the-standard-foundations` ts-foundations-011, -014, -015):

1. an envelope that is null or carries no content is refused, and so is one that has been tampered with;
2. §3's gate refuses each non-system context again on this door;
3. a redelivered request is answered from `ProcessedEvents` and writes nothing a second time;
4. it replies with the row as stored;
5. a failure is rethrown to the substrate, after the reply.

## 5. TransitionReactionApprovalAsync — the stored score handed to the decision (needs issue)

1. **The decision query carries the stored row's `ConfidenceScore`** where it carries `null` today (`ReactionService.Transitions.Validations.cs`), so §APR8.5's low-score block reads it on the approve and the bypass (BereanReview.md rule 10). Nothing else in the transition changes.
