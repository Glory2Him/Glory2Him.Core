# Association service — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `IAssociationService` (`Glory2Him.Core/Services/Foundations/Associations/`). The Likes feature's work on the same service is `Backend/Foundations/AssociationService.md`, and the association reads' `AssociationService.AssociationReads.md`.
Inherits: §APR9.7.1 rule 5, §SEC14.6 rule 4, §SEC14.6.1, §SEC14.7 posture A′, BereanReview.md rules 1, 7 and 19

`Association` already carries the four confidence fields, and from `Backend/Models/IApproval.md §1` it carries them through `IApproval`, not through a declaration of its own. Its add already refuses them (`Backend/Foundations/AssociationService.md §15`), and its general modify pins every non-audit property against storage (§APR8.6.1, *the last-editor question*). What changes is who may set them: set-confidence admitted `Publishers` and `Administrators` and refused the owner; it now admits the system identity alone (BereanReview.md rule 1). Its door, `Association-SettingConfidence`, and its fact, `Association-ConfidenceSet`, stay.

## 1. SetAssociationConfidenceAsync — the system identity alone (#1155)

```csharp
ValueTask<Association> SetAssociationConfidenceAsync(
    Association association,
    CancellationToken cancellationToken = default);
```

1. **Only the system identity may call it**: the envelope's security context has `IsSystemIdentity = true` (§SEC14.6 rule 4, §SEC14.6.1). Every other caller is refused, `Publishers` and `Administrators` included. This replaces both of `ValidateUserCanSetStorageAssociationConfidenceAsync`'s checks — the owner check and the role check — which refused the roleless system identity, and the owner check, which refused the system on any association it authored (`CreatedBy` is `SystemIdentity.UserId`). With no person admitted, the owner exclusion has nothing left to exclude.
2. **The shapes, and the landing on an unscored row, are those of every item's set-confidence** (`Backend/Foundations/TagService.BereanReview.md §3` rules 3 and 4). A score lands only on an association with none. A clear writes all four null. What the operation validates today — the range, the length, and provenance written with a score — keeps its rules, and a score with no provenance is now refused: a person's score, whose provenance was null, no longer exists (§APR13.4 rule 5).
3. **The read-only veto still covers it**, as it covers every posture A′ transition (§SEC14.7 posture A′ rule 1). The system identity holds no role, so the veto never refuses it.
4. **Its comments and its refusal logs stop describing a person's score**: `AssociationService.Transitions.cs`, the summary over the operation, and `AssociationService.Transitions.Validations.cs`, the summary over the gate and its *"publisher typed"* and *"correct"* sentences.
5. It publishes `Association-ConfidenceSet`, as today.

## 2. OnSettingAssociationConfidenceAsync — the system identity alone (#1156)

The `Association-SettingConfidence` door. It converges on §1's private body, so §1 proves the gate and the write; this section proves the gate refuses again on the door. An envelope carrying a `Publishers` context, an `Administrators` context and the association's owner's context is each refused, and one carrying the system identity writes. Its envelope refusals, deduplication, reply and rethrow are unchanged (`the-standard-foundations` ts-foundations-011, -014, -015).
