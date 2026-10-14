# Approval setting — the low-score block
Parent: [BereanReview.md](../../BereanReview.md)
Level: model — `ApprovalSetting` (`Glory2Him.Core/Models/Foundations/ApprovalSettings/`), its storage configuration (`StorageBroker.ApprovalSetting.Configurations.cs`), the seed (`Websites/Glory2Him.WebApp/Data/ApprovalSettingSeedData.cs`) and one migration
Inherits: §APR8.2, §APR8.4 rule 2, §APR8.6.2, §ARC12.5.1, BereanReview.md rules 5, 10, 12 and 13, *Storage and migration* items 2–4, *What ships together* step 1

`BlockOnZeroApprovalScore` is renamed `BlockOnRejectApprovalScore` (A3). The thresholds stay, with their constraints and seed constants (A1), and so does the vote, `IsAIAllowedToVote`, with its constraint (I2.A). The rename reaches `main` in steps so that the React app and the server never name the field differently in one deployment (BereanReview.md, *What ships together* step 1).

## 1. The rename, in the model (#1150)

1. **The property is renamed `BlockOnRejectApprovalScore`**, mapped to the existing column with `HasColumnName("BlockOnZeroApprovalScore")`, so the schema does not change and no migration is added. Its summary says what it now blocks: a score below the resolved rejection threshold on a round Berean is assigned to, a null score reading as `0.00`, past which only a bypass goes (BereanReview.md rule 10).
2. **Everything that names it follows**: `AccessBroker`'s mapping onto the security client's policy, the seed's constant (`BlockOnRejectApprovalScore`, `true` on the house policy and `false` on the personal-association row, as today) and its drift comparison's entry.
3. **The JSON member becomes `blockOnRejectApprovalScore`.** It waits for `UI/Models/ApprovalSetting.md §1` on `main`, so that the deployed React app already reads either name and sends both. The ASP.NET Core binder ignores the old member it no longer knows.

## 2. The rename, in the schema (#1151)

1. **One migration renames the column** `BlockOnZeroApprovalScore` to `BlockOnRejectApprovalScore` on `ApprovalSettings`, keeping every row's value, and `HasColumnName` goes from the configuration. One statement, so one batch with nothing to `EXEC`. `Down` renames it back.
2. It follows §1 on `main`.

## 3. The vote, kept (#1152, closed)

Retired from this design. It dropped `IsAIAllowedToVote` and `CK_ApprovalSetting_AIVoteRequiresAIReviewer` (#1152, closed), and the owner's rulings of 2026-10-14 keep Berean's vote (BereanReview.md rule 5; I2.A). The column, the constraint, the seed's `false` and its drift entry stay, and so do the comments over `CK_ApprovalSetting_AIThresholdOrder` and in `ApprovalSettingService.IsApprovalThresholdBelowRejectionThreshold`, which give the vote's two verdicts as the order's reason and are true again. The number is kept so that a citation of this section still finds it.
