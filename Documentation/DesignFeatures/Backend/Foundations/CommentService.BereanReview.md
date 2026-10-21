# Comment service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `ICommentService` (`Glory2Him.Core/Services/Foundations/Comments/`). The amender record's work on the same service is `Backend/Foundations/CommentService.md`.
Inherits: §APR8.5, §APR9.7.1 rules 2 and 5, §EVN2, §EVN19, §SEC14.6 rule 4, §SEC14.6.1, BereanReview.md rules 1, 2, 7, 9, 10 and 19

`Comment` implements `IApproval`, which now extends `IConfidence` (`Backend/Models/IApproval.md §1`), so a comment carries a confidence score and a reason (BereanReview.md rule 2). Only the system identity writes them, through set-confidence (rule 1). §1 and §2 keep every other write from carrying them, and they land before the columns are mapped (`Backend/Models/IApproval.md §2`): mapped first, a caller's score would be stored on any path not yet keeping it out.

**Build order** (BereanReview.md, *What ships together* step 5). §1, §2, §3 and §5 build on `Backend/Models/IApproval.md §1`, §4 on §3, and §6 and §7 on §1 and §2, whose doors they pin, adding tests and no code (`README.md`, *One section per operation*; ConfidenceReads.md, *The rulings*). Until `Backend/Models/IApproval.md §2` maps the columns, which waits for §1 and §2 here and on the other five foundations, and for neither pin, each runs and stores nothing. The data change publishes to §4's door to clear a score (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`), and the review process will to write one (BereanReview.md rule 6, designed under #1216).

## 1. AddCommentAsync — the confidence fields refused (#1192)

1. **An add that carries any of `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` or `ModelVersion` is refused**, with a validation failure naming each field that is set, as the add already refuses `IsPublished` and `PublishDate` (`IsSetOnAdd`). The fields arrive null on every honest add, so nothing that works today changes.
2. **It is asked in the add's one validation**, which the `Comment-Adding` door converges on, so it reaches the door as this section builds it, and §6 proves it there.

## 2. ModifyCommentAsync — the confidence fields kept as stored (#1193)

1. **A modify takes `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` and `ModelVersion` from the stored row, and ignores the caller's copy**, neither comparing nor refusing it, as every write but the insert does with `ApprovalId` (§APR9.7.1) and as the general modify does with `AmenderUserIds` (§APR8.6.3 rule 4). So the general modify never sets or clears a score (BereanReview.md rule 1). Taking rather than comparing is what lets a caller holding a copy read before a run's score landed save at all: the author editing while Berean runs, or a moderator whose editor was opened before the score was written. Refusing a difference, as the modify refuses a change to `IsApprovedByBypass`, was rejected for that reason: the bypass pair changes only when the round closes, and the score changes while the item is open to edits.
2. **It is asked in `DoModifyCommentAsync`**, which the `Comment-Modifying` door converges on, so it reaches the door as this section builds it, and §7 proves it there.

## 3. SetCommentConfidenceAsync (#1205)

```csharp
ValueTask<Comment> SetCommentConfidenceAsync(
    Comment comment,
    CancellationToken cancellationToken = default);
```

1. **Only the system identity may call it** (BereanReview.md rule 1): the envelope's security context has `IsSystemIdentity = true` and `AuthenticationType = System` — the system acting on its own account, which `IEventEnvelopeBroker.CreateSystemAsync` alone mints onto a signed envelope (§SEC14.6 rule 4, §SEC14.6.1). An elevated context is refused: `CreateElevatedAsync` mints `IsSystemIdentity = true` with `AuthenticationType = Delegated` for a person's manual approve or reject, and records the person (§SEC14.6.1), and no person sets a score (BereanReview.md rules 1 and 19). The gate reads the envelope alone, so it is asked first, before the row is read: a refused caller learns nothing of whether the row exists. Every other caller is refused, whatever its roles, the comment's owner and `Administrators` included. It asks no role and no owner test.
2. **It writes the four `IConfidence` fields as one unit, and nothing else**; every other field is taken from the stored row (§APR9.7.1 rule 5). It reads the row by the comment's `Id`, and a row that is missing or soft deleted is refused as not found.
3. **Two shapes, and only two.** *A score:* `ConfidenceScore` between `0.00` and `10.00` with at most two decimal places, a `ConfidenceReason` of 1 to 500 characters, a `SourceBatchId`, and a `ModelVersion` of 1 to 128 characters, all four present. *A clear:* all four absent, and it writes all four null. A field is absent when it is null; a reason or a model version is absent too when it is empty or white space alone, and a `SourceBatchId` when it is `Guid.Empty`, as the association's provenance rule already reads a blank model version (`AssociationService.Transitions.Validations.cs`, `IsProvenanceIncomplete`). Anything else is refused, naming each field at fault: a score without a reason, a reason without a score, a score without its provenance, provenance without a score (a `SourceBatchId` or a `ModelVersion` with no score and no reason), a score off the scale or past two decimal places, a reason over 500 characters, a model version over 128.
4. **A score lands only on a row with none** (BereanReview.md rule 7). On a row that already holds one it writes nothing, publishes nothing and answers the stored row. A clear writes all four null on a row that holds a score and publishes; on a row that holds none it writes nothing and publishes nothing.
5. **It publishes `Comment-ConfidenceSet`**, never `Comment-Modified`, so writing a score never re-enters the approval workflow (§APR9.7.1 rule 5). `UpdatedBy` names the system and `UpdatedWhen` is stamped, as every write is (§SEC14.6.1).
6. **Its failures map as the foundation's modify does**, with the modify's exception tests as the pattern (`CommentServiceTests.Modify.Exceptions.cs`), since the transitions' own exception files hold cancellation alone: a SQL error on the read or the update, a critical dependency failure; a database-update error, a dependency failure; a concurrency conflict, a dependency validation failure; a dependency's `OperationCanceledException` under a token that was not cancelled, a dependency failure; a token cancelled before the call, rethrown; and an unexpected failure, a service failure. A failure before the write publishes nothing.
7. **It records the request it serves under its door's receiver name**, `CommentOnSettingCommentConfidenceSubscriptionName`, whose value is `"CommentService.OnSettingCommentConfidence"`. The inbound envelope's `ProcessedEvent` and the outbound one are keyed on the request handler's name (§EVN19 rule 1), so this body and §4's door must name the same one. This section mints it, in `EventBrokerIdentifiers.Comment.cs` beside the fact address, because the body lands before the door (BereanReview.md, *What ships together* step 5). §4 registers its subscription under it, and mints no name of its own. The association's body passes its door's name the same way (`AssociationService.Transitions.cs`, `DoSetAssociationConfidenceAsync`).

## 4. OnSettingCommentConfidenceAsync (#1206)

The `Comment-SettingConfidence` request door, new. Its address and its subscription's id are minted under stable identifiers (`EventBrokerIdentifiers.Comment.cs`), and its subscription is registered (`EventSubscriptionRegistration`) under the receiver name §3 minted, which its redelivery check reads (§3 rule 7). It converges on §3's private body, so §3 proves the gate, the shapes and the write; this door adds what a handler adds (`the-standard-foundations` ts-foundations-011, -014, -015):

1. an envelope that is null or carries no content is refused, and so is one that has been tampered with;
2. §3's gate refuses each non-system context again on this door;
3. a redelivered request is answered from `ProcessedEvents` and writes nothing a second time;
4. it replies with the row as stored;
5. a failure is rethrown to the substrate, after the reply.

## 5. TransitionCommentApprovalAsync — the stored score handed to the decision (#1213)

1. **The decision query carries the stored row's `ConfidenceScore`** where it carries `null` today (`CommentService.Transitions.Validations.cs`), so §APR8.5's low-score block reads it on the approve and the bypass (BereanReview.md rule 10). Nothing else in the transition changes.

## 6. OnAddingCommentAsync — the confidence fields refused on the `Comment-Adding` door (#1319)

```csharp
ValueTask<EventEnvelope<Comment>?> OnAddingCommentAsync(
    EventEnvelope<Comment> envelope,
    CancellationToken cancellationToken = default);
```

1. **An add that arrives on the door carrying any of the four is refused**, naming each field it carries, as §1 refuses it on the direct path: the door converges on the add's one validation, which §1 changes.
2. **It adds no code.** Its test passes as soon as it is written, once §1 is built, and is committed as a pin (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its deduplication and its rethrow are unchanged, and their tests stand.

## 7. OnModifyingCommentAsync — the confidence fields kept as stored on the `Comment-Modifying` door (#1320)

```csharp
ValueTask<EventEnvelope<Comment>?> OnModifyingCommentAsync(
    EventEnvelope<Comment> envelope,
    CancellationToken cancellationToken = default);
```

1. **A modify that arrives on the door keeps the stored four**, whatever its copy carries, as §2 keeps them on the direct path: the door converges on `DoModifyCommentAsync`, which §2 changes.
2. **It adds no code.** Its test passes as soon as it is written, once §2 is built, and is committed as a pin (ConfidenceReads.md, *The rulings*). Its envelope's refusals, its deduplication and its rethrow are unchanged, and their tests stand.
3. **The withholding of the modify's answer changes its test**, where it answers a caller outside the review tier, and builds on this section (`Backend/Foundations/CommentService.ConfidenceReads.md §3`; ConfidenceReads.md, *What ships together* step 1).
