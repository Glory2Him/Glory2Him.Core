# Association service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`). The Likes feature's work on the same service is `Backend/Foundations/AssociationService.md`, and the association reads' `AssociationService.AssociationReads.md`.
Inherits: §APR9.7.1 rule 5, §SEC14.6 rule 4, §SEC14.6.1, §SEC14.7 posture A′, BereanReview.md rules 1, 7 and 19

`Association` already carries the four confidence fields, and from `Backend/Models/IApproval.md §1` it carries them through `IApproval`, not through a declaration of its own. Its add already refuses them (`Backend/Foundations/AssociationService.md §15`), and its general modify pins every non-audit property against storage (§APR8.6.1, *the last-editor question*), the four among them. Two things change. Set-confidence admitted `Publishers` and `Administrators` and refused the owner; it now admits the system identity alone (BereanReview.md rule 1). And the general modify stops comparing the four and takes them from the stored row, as the six items' modify does (§3). Its door, `Association-SettingConfidence`, and its fact, `Association-ConfidenceSet`, stay.

## 1. SetAssociationConfidenceAsync — the system identity alone (#1155)

```csharp
ValueTask<Association> SetAssociationConfidenceAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **Only the system identity may call it**: the envelope's security context has `IsSystemIdentity = true` and `AuthenticationType = System` (§SEC14.6 rule 4, §SEC14.6.1), as every item's set-confidence asks (`Backend/Foundations/TagService.BereanReview.md §3` rule 1), so an elevated context, which records a person, is refused too. Every other caller is refused, `Publishers` and `Administrators` included. **The gate is asked first**, before `LoadTransitionTargetAsync` reads the row, as every item's is: it reads the envelope alone, and a refused caller learns nothing of whether the row exists. This replaces both of `ValidateUserCanSetStorageAssociationConfidenceAsync`'s checks — the owner check and the role check — which refused the roleless system identity, and the owner check, which refused the system on any association it authored (`CreatedBy` is `SystemIdentity.UserId`). With no person admitted, the owner exclusion has nothing left to exclude.
2. **The shapes, and the landing on an unscored row, are those of every item's set-confidence** (`Backend/Foundations/TagService.BereanReview.md §3` rules 3 and 4). A score lands only on an association with none. A clear writes all four null. What the operation validates today — the range, the length, and provenance written with a score — keeps its rules, and a score with no provenance is now refused: a person's score, whose provenance was null, no longer exists (§APR13.4 rule 5).
3. **The read-only veto still covers it**, as it covers every posture A′ transition (§SEC14.7 posture A′ rule 1). The system identity holds no role, so the veto never refuses it.
4. **Its comments and its refusal messages stop describing a person's score**: in `AssociationService.Transitions.cs`, the comment inside `DoSetAssociationConfidenceAsync` that begins *"All four IConfidence fields move together. A human correcting a machine score"*, with its *"a publisher typed"* and *"the human's correction"*, which says instead that all four move together because a score always carries its provenance; and in `AssociationService.Transitions.Validations.cs`, the owner-exclusion comment over the gate, which begins *"The owner is excluded ON PURPOSE"*, and the gate's two refusal messages, *"The owner of a content item association may not set its confidence."* and *"The current user is not allowed to set the confidence of this content item association."*, which give way to one refusal of every context but the system's, worded as the six items' refusal is.
5. It publishes `Association-ConfidenceSet`, as today.

## 2. OnSettingAssociationConfidenceAsync — the system identity alone (#1156)

The `Association-SettingConfidence` door. It converges on §1's private body, so §1 proves the gate and the write; this section proves the gate refuses again on the door. An envelope carrying a `Publishers` context, an `Administrators` context and the association's owner's context is each refused, and one carrying the system identity writes. Its envelope refusals, deduplication, reply and rethrow are unchanged (`the-standard-foundations` ts-foundations-011, -014, -015).

## 3. ModifyAssociationAsync — the confidence fields kept as stored (#1229)

```csharp
ValueTask<Association> ModifyAssociationAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **A modify takes `ConfidenceScore`, `ConfidenceReason`, `SourceBatchId` and `ModelVersion` from the stored row, and ignores the caller's copy**, neither comparing nor refusing it, as the six items' general modify does and for the same reason (`Backend/Foundations/ContentItemService.BereanReview.md §2` rule 1): once the review process writes a score while the round is open, a caller holding a copy read before it — the owner moving the association between `Draft` and `Submitted`, or a moderator whose editor was opened earlier — would be refused for a value they never touched. Today `ValidateAgainstStorageAssociationOnModify` compares the four and refuses a difference (`AssociationService.Validations.cs`); those four comparisons go, and so do the range and length rules the modify asks of the caller's score and reason, since nothing of the caller's is written. Every other field the modify pins — sorting, the two scopes, the reacting user and the bypass pair among them — stays pinned. The comment over the four comparisons, which explains why confidence is pinned with sorting and scope, is rewritten to say why the four are taken instead.
2. It is asked on the direct path and on the `Association-Modifying` door alike, because both converge on `DoModifyAssociationAsync`.
3. It builds on nothing of this design's: `Association` already carries the four, stored. It is no prerequisite of `Backend/Models/IApproval.md §2`, which maps the other seven tables' columns.
