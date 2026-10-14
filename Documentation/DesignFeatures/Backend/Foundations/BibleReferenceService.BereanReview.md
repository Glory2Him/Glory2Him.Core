# BibleReference service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IBibleReferenceService` (`Glory2Him.Core/Services/Foundations/BibleReferences/`). The amender record's work on the same service is `Backend/Foundations/BibleReferenceService.md`.
Inherits: §APR8.5, §APR9.7.1 rules 2 and 5, §EVN2, §EVN19, §SEC14.6 rule 4, §SEC14.6.1, BereanReview.md rules 1, 2, 7, 9, 10 and 19

`BibleReference` implements `IApproval`, which now extends `IConfidence` (`Backend/Models/IApproval.md §1`), so a Bible reference carries a confidence score and a reason (BereanReview.md rule 2). Only the system identity writes them, through set-confidence (rule 1). §1 and §2 keep every other write from carrying them, and they land before the columns are mapped (`Backend/Models/IApproval.md §2`): mapped first, a caller's score would be stored on any path not yet keeping it out.

**Build order** (BereanReview.md, *What ships together* step 5). §1, §2, §3 and §5 build on `Backend/Models/IApproval.md §1`, and §4 on §3. Until `Backend/Models/IApproval.md §2` maps the columns, which waits for §1 and §2 here and on the other five foundations, each runs and stores nothing. The content-change flow and the write-back publish to §4's door (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`, `Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §3`).

## 1. AddBibleReferenceAsync — the confidence fields refused (#1194)

1. **An add that carries any of `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` or `ModelVersion` is refused**, with a validation failure naming each field that is set, as the add already refuses `IsPublished` and `PublishDate` (`IsSetOnAdd`). The fields arrive null on every honest add, so nothing that works today changes.
2. It is asked on the direct path and on the `BibleReference-Adding` door alike, because both converge on the add's one validation.

## 2. ModifyBibleReferenceAsync — the confidence fields kept as stored (#1195)

1. **A modify takes `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` and `ModelVersion` from the stored row, and ignores the caller's copy**, neither comparing nor refusing it, as every write but the insert does with `ApprovalId` (§APR9.7.1) and as the general modify does with `AmenderUserIds` (§APR8.6.3 rule 4). So the general modify never sets or clears a score (BereanReview.md rule 1). Taking rather than comparing is what lets a caller holding a copy read before a run's score landed save at all: the author editing while Berean runs, or a moderator whose editor was opened before the score was written. Refusing a difference, as the modify refuses a change to `IsApprovedByBypass`, was rejected for that reason: the bypass pair changes only when the round closes, and the score changes while the item is open to edits.
2. It is asked on the direct path and on the `BibleReference-Modifying` door alike.

## 3. SetBibleReferenceConfidenceAsync (#1207)

```csharp
ValueTask<BibleReference> SetBibleReferenceConfidenceAsync(
    BibleReference bibleReference,
    CancellationToken cancellationToken = default);
```

1. **Only the system identity may call it** (BereanReview.md rule 1): the envelope's security context has `IsSystemIdentity = true`, which only this system mints onto a signed envelope (§SEC14.6 rule 4, §SEC14.6.1). Every other caller is refused, whatever its roles, the Bible reference's owner and `Administrators` included. It asks no role and no owner test.
2. **It writes the four `IConfidence` fields as one unit, and nothing else**; every other field is taken from the stored row (§APR9.7.1 rule 5). It reads the row by the Bible reference's `Id`, and a row that is missing or soft deleted is refused as not found.
3. **Two shapes, and only two.** *A score:* `ConfidenceScore` between `0.00` and `10.00` with at most two decimal places, a `ConfidenceReason` of 1 to 500 characters, a `SourceBatchId`, and a `ModelVersion` of 1 to 128 characters, all four present. *A clear:* all four absent, and it writes all four null. A field is absent when it is null; a reason or a model version is absent too when it is empty or white space alone, and a `SourceBatchId` when it is `Guid.Empty`, as the association's provenance rule already reads a blank model version (`AssociationService.Transitions.Validations.cs`, `IsProvenanceIncomplete`). Anything else is refused, naming each field at fault: a score without a reason, a reason without a score, a score without its provenance, a score off the scale or past two decimal places, a reason over 500 characters, a model version over 128.
4. **A score lands only on a row with none** (BereanReview.md rule 7). On a row that already holds one it writes nothing, publishes nothing and answers the stored row. A clear writes all four null on a row that holds a score and publishes; on a row that holds none it writes nothing and publishes nothing.
5. **It publishes `BibleReference-ConfidenceSet`**, never `BibleReference-Modified`, so writing a score never re-enters the approval workflow (§APR9.7.1 rule 5). `UpdatedBy` names the system and `UpdatedWhen` is stamped, as every write is (§SEC14.6.1).
6. Its failures map as the foundation's other transitions do: validation, dependency validation, dependency and service, and its token's cancellation and timeout.

## 4. OnSettingBibleReferenceConfidenceAsync (#1208)

The `BibleReference-SettingConfidence` request door, new. Address and subscription are registered under stable identifiers (`EventBrokerIdentifiers.BibleReference.cs`, `EventSubscriptionRegistration`). It converges on §3's private body, so §3 proves the gate, the shapes and the write; this door adds what a handler adds (`the-standard-foundations` ts-foundations-011, -014, -015):

1. an envelope that is null or carries no content is refused, and so is one that has been tampered with;
2. §3's gate refuses each non-system context again on this door;
3. a redelivered request is answered from `ProcessedEvents` and writes nothing a second time;
4. it replies with the row as stored;
5. a failure is rethrown to the substrate, after the reply.

## 5. TransitionBibleReferenceApprovalAsync — the stored score handed to the decision (#1214)

1. **The decision query carries the stored row's `ConfidenceScore`** where it carries `null` today (`BibleReferenceService.Transitions.Validations.cs`), so §APR8.5's low-score block reads it on the approve and the bypass (BereanReview.md rule 10). Nothing else in the transition changes.
