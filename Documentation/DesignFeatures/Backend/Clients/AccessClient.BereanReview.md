# Access client — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: client — `IAccessClient` (`Clients/SecurityClients/G2H.Security.Client/Clients/Access/`), served by `AccessService` (`Clients/SecurityClients/G2H.Security.Client/Services/Foundations/Access/`). The amender record's work on the same client is `Backend/Clients/AccessClient.md`.
Inherits: §APR8.4 rule 2, §APR8.5, §APR8.6 HR-4, §APR8.6.1 rules 2 and 4, §APR8.6.2, §APR8.6.2.1 gate 4, BereanReview.md rules 3, 5 and 10–13

The client decides both blocks and the offer, from what `IAccessBroker` hands it, as it decides every §APR8.5 condition (§APR8.6.1 rule 2). It is a pure function: a section that adds a request member makes it `required`, so a forgotten gather does not compile, and `AccessBroker` — the one place Core builds each request — passes a neutral value until its own task gathers the real one, so Core keeps compiling and nothing changes in production until then (the precedent of `Backend/Clients/AccessClient.md §1`).

## 1. EvaluateApprovalConditionsAsync — the low-score block (#1140)

```csharp
ValueTask<ApprovalConditionsVerdict> EvaluateApprovalConditionsAsync(
    ApprovalConditionsRequest approvalConditionsRequest);
```

1. **`ApprovalPolicy.BlockOnZeroApprovalScore` is renamed `BlockOnRejectApprovalScore`**, and `AccessDenialReason.BlockedByZeroConfidenceScore` is renamed `BlockedByRejectConfidenceScore`, keeping its value `16` (BereanReview.md rule 10). `AccessBroker`'s mapping of the setting onto the policy, and `ApprovalOrchestrationService.DescribeBlockReason`'s arm, are renamed with it, so Core keeps compiling. The arm's words are §3's of `Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md`.
2. **Where the resolved `BlockOnRejectApprovalScore` is `true`, a `ConfidenceScore` at or below the resolved `AIApprovalConfidenceRejectionThreshold` blocks**, with `BlockedByRejectConfidenceScore` and the explanation *"Berean's confidence score is at or below the rejection threshold."* A score above it, or a null score, does not. Where the setting is `false`, no score blocks.
3. **The block is collected with every other**, in the precedence the conditions keep today, so a single-valued consumer still reads the first.
4. **`ApprovalPolicyDefaults` holds `BlockOnRejectApprovalScore = true`** with the two thresholds at `0.00` (§APR8.4 rule 2, BereanReview.md rule 13), so a score of exactly `0.00` blocks where no row resolves and no other does.
5. **The words that call the block zero-only are rewritten**: the comment over it in `AccessService` (*"Only an explicit zero blocks"*), `IAccessClient`'s *"the zero-score block"*, `AccessDenialReason`'s *"exactly zero"*, and `ApprovalPolicy`'s and `ApprovalPolicyDefaults`' own comments on the field.

## 2. MayDecideApprovalAsync — the low-score block on a decision (#1141)

```csharp
ValueTask<AccessVerdict> MayDecideApprovalAsync(DecideApprovalRequest decideApprovalRequest);
```

1. **An approval is refused with `BlockedByRejectConfidenceScore`** where §1's block holds, on route 1 (§APR8.6 HR-4).
2. **A bypass waives it** where `DoNotAllowBypassingSettings` is `false`, and the verdict reports `BypassedBlockReason = BlockedByRejectConfidenceScore` (§APR8.6.1, §APR9.7.5 bypass rule 3). Where it is `true`, the bypass is refused as it is for any block.
3. **A rejection is never refused by it** (§APR8.6 HR-4 route 1).
4. The code is §1's, shared by both operations, so this section pins it on the decision path.

## 3. ResolveAIReviewerPolicyAsync — Berean offered only where the round requires review (#1142)

```csharp
ValueTask<AIReviewerPolicyVerdict> ResolveAIReviewerPolicyAsync(
    ResolveAIReviewerPolicyRequest resolveAIReviewerPolicyRequest);
```

1. **`IsOffered` is `true` only where the resolved policy has `IsAIReviewerOffered = true`, `RequireApprovals = true`, and an `IsPersonal` that is not `true`** (BereanReview.md rule 3). `IsAutomaticallyRequested` is `IsOffered && IsAIReviewerAutomaticallyRequested`, so it carries the same two terms.
2. Every reader of the verdict follows by construction: the caller-facing request's gate (`AIReviewerOrchestrationService.ValidateAIReviewerIsOffered`), the reviewer suggestion, the automatic assignment's gate 4 (§APR8.6.2.1), and the Modified flow's fresh assignment (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`).
3. **`AIReviewerPolicyVerdict`'s summary** stops describing itself as *"narrower than `ApprovalSetting.IsAIAllowedToVote`"* (§4).

## 4. The vote retired from the policy (#1149)

Groundwork, with no operation: `ApprovalPolicy.IsAIAllowedToVote` is removed (BereanReview.md rule 5).

1. `ApprovalPolicy` loses the member, and `ApprovalPolicyDefaults` its line. Their comments that read the thresholds *"only where `IsAIAllowedToVote` is true"* say instead that the rejection threshold is read by §1's block, and the pair by the confidence suggestion (BereanReview.md rule 12).
2. `AccessBroker`'s mapping loses the line that copies the setting, so Core keeps compiling. The stored column stays until `Backend/Models/ApprovalSetting.md §3` drops it.

## 5. EvaluateApprovalConditionsAsync — the pending block (#1170)

1. **`ApprovalConditionsRequest` gains two members**: `public required bool IsAIReviewerAssigned { get; init; }`, whether a live Berean assignment stands on the round, and `public required bool IsAIReviewCompleted { get; init; }`, whether that assignment's review has completed, `false` where none stands. `AccessBroker.EvaluateApprovalConditionsByIdAsync` passes `false` for both until `Backend/Brokers/AccessBroker.BereanReview.md §4` gathers them, so nothing blocks in production until then.
2. **`AccessDenialReason` gains `AwaitingAIReviewerConfidenceScore = 25`**, documented as BereanReview.md rule 11. `24` is #1011's.
3. **Where `IsAIReviewerAssigned` is `true` and either `ConfidenceScore` is null or `IsAIReviewCompleted` is `false`, the round is blocked** with it and the explanation *"Berean's review is pending."*, whatever `BlockOnRejectApprovalScore` says (BereanReview.md rule 11). A completed review always leaves a score (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §3`), so this holds a round exactly while the confidence process has not finished, as A6 asks. Where `IsAIReviewerAssigned` is `false`, a null score blocks nothing.
4. **It is collected after §1's**, in the conditions' precedence.

## 6. MayDecideApprovalAsync — the pending block on a decision (#1171)

1. **`DecideApprovalRequest` gains the same two members**; `AccessBroker.MayDecideApprovalAsync` and `MayDecideApprovalByIdAsync` pass `false` for both until `Backend/Brokers/AccessBroker.BereanReview.md §5` and `§6` gather them.
2. **An approval is refused with `AwaitingAIReviewerConfidenceScore`** where §5's block holds. A bypass waives it as §2 rule 2 waives the low-score block, and a rejection is never refused by it.
3. The code is §5's, shared by both operations, so this section pins it on the decision path.
