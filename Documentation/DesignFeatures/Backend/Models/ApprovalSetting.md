# Approval setting — the low-score block and the vote
Parent: [BereanReview.md](../../BereanReview.md)
Level: model — `ApprovalSetting` (`Glory2Him.Core/Models/Foundations/ApprovalSettings/`), its storage configuration (`StorageBroker.ApprovalSetting.Configurations.cs`), `ApprovalSettingService`'s validations, the seed (`Websites/Glory2Him.WebApp/Data/ApprovalSettingSeedData.cs`) and two migrations
Inherits: §APR8.2, §APR8.4 rule 2, §APR8.6.2, §ARC12.5.1, BereanReview.md rules 5, 10, 12 and 13, *Storage and migration* items 2–4, *What ships together* steps 1 and 2

`BlockOnZeroApprovalScore` is renamed `BlockOnRejectApprovalScore` (A3), and `IsAIAllowedToVote` is retired (ruling 1). The thresholds stay, with their constraints and seed constants (A1). The rename reaches `main` in steps so that the React app and the server never name the field differently in one deployment (BereanReview.md, *What ships together* step 1). The vote's drop comes after everything that reads it.

## 1. The rename, in the model (needs issue)

1. **The property is renamed `BlockOnRejectApprovalScore`**, mapped to the existing column with `HasColumnName("BlockOnZeroApprovalScore")`, so the schema does not change and no migration is added. Its summary says what it now blocks: a score at or below the resolved rejection threshold, past which only a bypass goes (BereanReview.md rule 10).
2. **Everything that names it follows**: `AccessBroker`'s mapping onto the security client's policy, the seed's constant (`BlockOnRejectApprovalScore`, `true` on the house policy and `false` on the personal-association row, as today) and its drift comparison's entry.
3. **The JSON member becomes `blockOnRejectApprovalScore`.** It waits for `UI/Models/ApprovalSetting.md §1` on `main`, so that the deployed React app already reads either name and sends both. The ASP.NET Core binder ignores the old member it no longer knows.

## 2. The rename, in the schema (needs issue)

1. **One migration renames the column** `BlockOnZeroApprovalScore` to `BlockOnRejectApprovalScore` on `ApprovalSettings`, keeping every row's value, and `HasColumnName` goes from the configuration. One statement, so one batch with nothing to `EXEC`. `Down` renames it back.
2. It follows §1 on `main`.

## 3. The vote, retired (needs issue)

1. **`IsAIAllowedToVote` leaves the model**, with its configuration, `CK_ApprovalSetting_AIVoteRequiresAIReviewer`, `ApprovalSettingService`'s validation that pairs it to the offer, and the seed's constant and drift entry.
2. **One migration drops the constraint, then the column.** Two statements, neither naming anything the batch creates, so one batch with nothing to `EXEC`. `Down` adds both back with the column `false` on every row, for the developer's rollback alone: what each row held is gone (BereanReview.md, *Risks*).
3. **The thresholds' words are rewritten where this section touches them**: the model's summaries of the two threshold properties (*"Read only when `IsAIAllowedToVote` is true"*), the seed's comment on their constants (*"inert while IsAIAllowedToVote is false"*), the comment over `CK_ApprovalSetting_AIThresholdOrder` (*"so both fire"*, beside a fail-closed pair that *"casts no vote to threshold"*), and the comment in `ApprovalSettingService.IsApprovalThresholdBelowRejectionThreshold` (*"so both verdicts fire"*). Each says instead that the rejection threshold decides the low-score block and that the order holds the confidence suggestion's band (BereanReview.md rule 12).
4. **It waits for every reader to have stopped**: both approval-setting pages and the React model (`UI/Models/ApprovalSetting.md §1`, BereanReview.md rule 15), and the security client's policy (`Backend/Clients/AccessClient.BereanReview.md §4`).
