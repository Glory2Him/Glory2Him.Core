# Approval setting — the low-score block and the vote
Parent: [BereanReview.md](../../BereanReview.md)
Level: model — `ApprovalSetting` (`Glory2Him.Core/Models/Foundations/ApprovalSettings/`), its storage configuration (`StorageBroker.ApprovalSetting.Configurations.cs`), `ApprovalSettingService`'s validations, the seed (`Websites/Glory2Him.WebApp/Data/ApprovalSettingSeedData.cs`) and two migrations
Inherits: §APR8.2, §APR8.4 rule 2, §APR8.6.2, §ARC12.5.1, BereanReview.md rules 5, 10, 12 and 13, *Storage and migration* items 2–4, *What ships together* steps 1 and 2

`BlockOnZeroApprovalScore` is renamed `BlockOnRejectApprovalScore` (A3), and `IsAIAllowedToVote` is retired (ruling 1). The thresholds stay, with their constraints and seed constants (A1). The rename reaches `main` in steps so that the React app and the server never name the field differently in one deployment (BereanReview.md, *What ships together* step 1). The vote's drop comes after everything that reads it.

## 1. The rename, in the model (#1150)

1. **The property is renamed `BlockOnRejectApprovalScore`**, mapped to the existing column with `HasColumnName("BlockOnZeroApprovalScore")`, so the schema does not change and no migration is added. Its summary says what it now blocks: a score at or below the resolved rejection threshold, past which only a bypass goes (BereanReview.md rule 10).
2. **Everything that names it follows**: `AccessBroker`'s mapping onto the security client's policy, the seed's constant (`BlockOnRejectApprovalScore`, `true` on the house policy and `false` on the personal-association row, as today) and its drift comparison's entry.
3. **The JSON member becomes `blockOnRejectApprovalScore`.** It waits for `UI/Models/ApprovalSetting.md §1` on `main`, so that the deployed React app already reads either name and sends both. The ASP.NET Core binder ignores the old member it no longer knows.

## 2. The rename, in the schema (#1151)

1. **One migration renames the column** `BlockOnZeroApprovalScore` to `BlockOnRejectApprovalScore` on `ApprovalSettings`, keeping every row's value, and `HasColumnName` goes from the configuration. One statement, so one batch with nothing to `EXEC`. `Down` renames it back.
2. It follows §1 on `main`.

## 3. The vote, retired (#1152)

1. **`IsAIAllowedToVote` leaves the model**, with its configuration, `CK_ApprovalSetting_AIVoteRequiresAIReviewer`, and the seed's constant and drift entry. `ApprovalSettingService` has no rule pairing the vote to the offer, so it loses nothing: the pairing was the constraint's alone (`StorageBroker.ApprovalSetting.Configurations.cs`).
2. **One migration drops the constraint, then the column.** Two statements, neither naming anything the batch creates, so one batch with nothing to `EXEC`. `Down` adds both back with the column `false` on every row, for the developer's rollback alone: what each row held is gone (BereanReview.md, *Risks*).
3. **The order's words are rewritten**: the comment over `CK_ApprovalSetting_AIThresholdOrder` (*"so both fire"*, beside a fail-closed pair that *"casts no vote to threshold"*), and the comment in `ApprovalSettingService.IsApprovalThresholdBelowRejectionThreshold` (*"so both verdicts fire"*). Each says instead that the order holds the confidence suggestion's band (BereanReview.md rule 12). The model's summaries of the two thresholds and the seed's comment on their constants are rewritten earlier, by the low-score block that starts reading the rejection threshold (`Backend/Clients/AccessClient.BereanReview.md §1` rule 6).
4. **It waits for every reader to have stopped**: both approval-setting pages (BereanReview.md rule 15) and the security client's policy with the broker's mapping (`Backend/Clients/AccessClient.BereanReview.md §4`). The React model may still send `isAIAllowedToVote` until `UI/Models/ApprovalSetting.md §2`; the ASP.NET Core binder ignores a member the model no longer has.
5. **The vote's other words go with it**, each a claim the drop makes untrue: the model's summary of `IsAIReviewerAutomaticallyRequested` (*"unlike `IsAIAllowedToVote` … that pairing carries NO CHECK constraint"*), and `AIReviewerPolicyVerdict.IsAutomaticallyRequested`'s (*"unlike `IsAIAllowedToVote`, which a CHECK constraint pairs to the offer"*), each of which says instead that no constraint pairs the automatic switch to the offer; and the acceptance project's copy of the model (`Websites/Glory2Him.WebApp.Tests.Acceptance/Models/ApprovalSettings/ApprovalSetting.cs`), with every acceptance test that sets the vote or explains its constraint (`ApprovalSettingTests.cs`, `ApprovalSettingTests.Post.cs`, `ApprovalTests.AIReviewer.cs`, `ApprovalTests.AutomaticAIReviewer.cs`).
