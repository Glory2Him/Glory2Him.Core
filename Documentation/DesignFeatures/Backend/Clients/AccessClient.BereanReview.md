# Access client — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: client — `IAccessClient` (`Clients/SecurityClients/G2H.Security.Client/Clients/Access/`), served by `AccessService` (`Clients/SecurityClients/G2H.Security.Client/Services/Foundations/Access/`). The amender record's work on the same client is `Backend/Clients/AccessClient.md`.
Inherits: §APR8.4 rule 2, §APR8.5, §APR8.6 HR-4, §APR8.6.1 rules 2 and 4, §APR8.6.2, §APR8.6.2.1 gate 4, BereanReview.md rules 3, 5, 10–13 and 22

The client decides Berean's blocks, the offer, and a round that requires no review, from what `IAccessBroker` hands it, as it decides every §APR8.5 condition (§APR8.6.1 rule 2). It is a pure function: a section that adds a request member makes it `required`, so a forgotten gather does not compile, and `AccessBroker` — the one place Core builds each request — passes a neutral value until its own task gathers the real one, so Core keeps compiling and nothing changes in production until then (the precedent of `Backend/Clients/AccessClient.md §1`).

## 1. EvaluateApprovalConditionsAsync — the low-score block (#1140)

```csharp
ValueTask<ApprovalConditionsVerdict> EvaluateApprovalConditionsAsync(
    ApprovalConditionsRequest approvalConditionsRequest);
```

1. **`ApprovalPolicy.BlockOnZeroApprovalScore` is renamed `BlockOnRejectApprovalScore`**, and `AccessDenialReason.BlockedByZeroConfidenceScore` is renamed `BlockedByRejectConfidenceScore`, keeping its value `16` (BereanReview.md rule 10). `AccessBroker`'s mapping of the setting onto the policy, and `ApprovalOrchestrationService.DescribeBlockReason`'s arm, are renamed with it, so Core keeps compiling. The arm's words are §3's of `Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md`.
2. **`ApprovalConditionsRequest` gains `public required bool IsAIReviewerAssigned { get; init; }`**, whether a live Berean assignment stands on the round. `AccessBroker.EvaluateApprovalConditionsByIdAsync` passes `false` until `Backend/Brokers/AccessBroker.BereanReview.md §4` gathers it, so the block refuses nothing in production until then.
3. **Where the resolved `BlockOnRejectApprovalScore` is `true` and `IsAIReviewerAssigned` is `true`, a `ConfidenceScore` below the resolved `AIApprovalConfidenceRejectionThreshold` blocks**, with `BlockedByRejectConfidenceScore` and the explanation *"Berean's confidence score is below the rejection threshold."* A null score reads as `0.00` (BereanReview.md rule 10). A score on the threshold or above it does not block. Where the setting is `false`, or Berean is not assigned, no score blocks.
4. **The block is collected with every other**, in the precedence the conditions keep today, so a single-valued consumer still reads the first.
5. **`ApprovalPolicyDefaults` holds `BlockOnRejectApprovalScore = true`** with the two thresholds at `0.00` (§APR8.4 rule 2, BereanReview.md rule 13), so where no row resolves no score is below the threshold, and the block refuses nothing.
6. **The words that call the block zero-only are rewritten**: the comment over it in `AccessService` (*"Only an explicit zero blocks"*), the explanation *"The entity's confidence score is zero."*, `IAccessClient`'s *"the zero-score block"*, `AccessDenialReason`'s *"exactly zero"*, `ApprovalConditionsRequest.ConfidenceScore`'s summary (*"Neither blocks. Only an explicit `0` blocks"*), and `ApprovalPolicy`'s and `ApprovalPolicyDefaults`' own comments on the field.
7. **The thresholds' words are rewritten wherever they say the rejection threshold is read only under the vote**, because from this section on the low-score block reads it with the vote off: `ApprovalPolicy`'s summaries of the two thresholds (*"Meaningful only where `IsAIAllowedToVote` is true"*); `ApprovalPolicyDefaults`' comment on their `0.00` (*"Never read while IsAIAllowedToVote is false"*); `ApprovalSetting`'s summaries of the two (*"Read only when `IsAIAllowedToVote` is true"*); the seed's comment on their constants (*"The two thresholds are inert while IsAIAllowedToVote is false"*, `ApprovalSettingSeedData.cs`); and the React model's comment on them (*"read only when isAIAllowedToVote is"*, `approvalSetting.ts`). Each says instead that the rejection threshold decides this block, that both decide the vote Berean casts where `IsAIAllowedToVote` is on (BereanReview.md rule 5), and that the pair's order holds the confidence suggestion's band (rule 12). What each says about the vote's two reviews stays: rule 5 keeps them. `ApprovalPolicyDefaults`' says that at `0.00` the block refuses nothing (rule 5 of this section).

## 2. MayDecideApprovalAsync — the low-score block on a decision (#1141)

```csharp
ValueTask<AccessVerdict> MayDecideApprovalAsync(DecideApprovalRequest decideApprovalRequest);
```

1. **`DecideApprovalRequest` gains the same `IsAIReviewerAssigned`**; `AccessBroker.MayDecideApprovalAsync` and `MayDecideApprovalByIdAsync` pass `false` until `Backend/Brokers/AccessBroker.BereanReview.md §5` and `§6` gather it.
2. **An approval is refused with `BlockedByRejectConfidenceScore`** where §1's block holds, on route 1 (§APR8.6 HR-4).
3. **A bypass waives it** where `DoNotAllowBypassingSettings` is `false`, and the verdict reports `BypassedBlockReason = BlockedByRejectConfidenceScore` (§APR8.6.1, §APR9.7.5 bypass rule 3). Where it is `true`, the bypass is refused as it is for any block.
4. **A rejection is never refused by it** (§APR8.6 HR-4 route 1).
5. The code is §1's, shared by both operations, so this section pins it on the decision path.

## 3. ResolveAIReviewerPolicyAsync — Berean offered only where the round requires review (#1142)

```csharp
ValueTask<AIReviewerPolicyVerdict> ResolveAIReviewerPolicyAsync(
    ResolveAIReviewerPolicyRequest resolveAIReviewerPolicyRequest);
```

1. **`IsOffered` is `true` only where the resolved policy has `IsAIReviewerOffered = true` and `RequireApprovals = true`, and the request's `IsPersonal` is not `true`** — the item is not a personal association (BereanReview.md rule 3). `IsAutomaticallyRequested` is `IsOffered && IsAIReviewerAutomaticallyRequested`, so it carries the same two terms.
2. Every reader of the verdict follows by construction: the caller-facing request's gate (`AIReviewerOrchestrationService.ValidateAIReviewerIsOffered`), the reviewer suggestion, the automatic assignment's gate 4 while it stands (§APR8.6.2.1), the approval process's step 4.2 (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §7–§8`), and the withdrawal's gate (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §4`).

## 4. The vote, kept

Retired from this design. It removed `ApprovalPolicy.IsAIAllowedToVote` (#1149, closed), and the owner's rulings of 2026-10-14 keep Berean's vote (BereanReview.md rule 5). The member and its mapping stay. The number is kept so that §5–§8 keep theirs.

## 5. EvaluateApprovalConditionsAsync — Berean's pending and in-flight blocks (#1170)

1. **`ApprovalConditionsRequest` gains two members** beside §1's `IsAIReviewerAssigned`: `public required bool IsAIReviewStarted { get; init; }` and `public required bool IsAIReviewCompleted { get; init; }`, the live assignment's two flags, each `false` where none stands. `AccessBroker.EvaluateApprovalConditionsByIdAsync` passes `false` for both until `Backend/Brokers/AccessBroker.BereanReview.md §4` gathers them.
2. **`AccessDenialReason` gains `BlockedByPendingAIReview = 25` and `BlockedByAIReviewInFlight = 26`**, each documented as BereanReview.md rule 11. `24` is #1011's.
3. **The evaluation computes BereanReview.md rule 11's two values**, which nothing stores: `IsAIReviewPending` is `IsAIReviewerAssigned && IsAIReviewStarted is false`, and `IsAIReviewInFlight` is `IsAIReviewerAssigned && IsAIReviewStarted && IsAIReviewCompleted is false`.
4. **Where `IsAIReviewPending` holds, the round is blocked** with `BlockedByPendingAIReview` and the explanation *"The Berean review has not started."*; **where `IsAIReviewInFlight` holds**, with `BlockedByAIReviewInFlight` and *"The Berean review has not completed."* Each blocks whatever `BlockOnRejectApprovalScore` says (BereanReview.md rule 11). Where Berean is not assigned, or its review has completed, neither does.
5. **Both are collected immediately before §1's low-score block**, in the conditions' precedence, so that where a null score also trips the low-score block, a single-valued consumer reads why the score is still null.

## 6. MayDecideApprovalAsync — Berean's pending and in-flight blocks on a decision (#1171)

1. **`DecideApprovalRequest` gains the same two members**; `AccessBroker.MayDecideApprovalAsync` and `MayDecideApprovalByIdAsync` pass `false` for both until `Backend/Brokers/AccessBroker.BereanReview.md §5` and `§6` gather them.
2. **An approval is refused with `BlockedByPendingAIReview` or `BlockedByAIReviewInFlight`** where §5's blocks hold. A bypass waives either as §2 rule 3 waives the low-score block, and a rejection is never refused by them.
3. The code is §5's, shared by both operations, so this section pins it on the decision path.

## 7. EvaluateApprovalConditionsAsync — a round that requires no review (needs issue)

1. **A round requires no review where the resolved `RequireApprovals` is `false`, or the request's `IsPersonal` is `true`** — the item is a personal association (BereanReview.md rule 22).
2. **Such a round's conditions are met and it is approved automatically**: `AreConditionsMet` and `ShouldAutoApprove` are `true` whatever `AutoApproveIfAllApprovalRequirementsMet` says, and the count, the rejection and the comment conditions are not asked. The explanation reads *"No review is required: the round is approved as it opens."*
3. **Berean's three blocks are still asked**, §1's and §5's, so a round Berean was assigned to before this rule shipped is still held by them (BereanReview.md rule 22, I1.5b). Where one holds, neither member is `true`.
4. Every flow that evaluates follows by construction: the Added and Modified flows apply `Approved` wherever `ShouldAutoApprove` is `true` (§APR9.7.7).

## 8. MayDecideApprovalAsync — a round that requires no review, on a decision (needs issue)

1. **A publisher's approve of a round §7 rule 1 names is permitted on route 1 without its count, its rejection or its comments** — a personal association under a tier that asks approvals, or a round with an outstanding comment under `RequireApprovals = false` — while Berean's three blocks still refuse it as §2 and §6 do. Such a round is approved as it opens (§7), so the decision meets one only where a round open before §7 shipped is decided before its next evaluation. Every other term the decision asks — the tier, the veto, the open round, HR-2 and §APR8.6 regardless-rule 1 — is unchanged.
2. **The automatic approval is not this gate's.** The flows apply it under the system identity, which holds no publisher-tier role, through the item's transition, which admits that identity in process (§SEC14.7 posture A′, its transitions table), so §7 alone governs it.
3. The code is §7's, shared by both operations, so this section pins it on the decision path.
