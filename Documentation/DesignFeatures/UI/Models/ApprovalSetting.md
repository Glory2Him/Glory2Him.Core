# Approval setting (React) — the low-score block and the vote
Parent: [BereanReview.md](../../BereanReview.md)
Level: UI model — `ApprovalSetting` and its mappers (`Websites/Glory2Him.WebApp.React/src/models/foundations/approvalSettings/approvalSetting.ts`)
Inherits: §UI20.9, BereanReview.md rules 5, 10 and 15, *What ships together* step 1

The server renames `blockOnZeroApprovalScore` to `blockOnRejectApprovalScore` (`Backend/Models/ApprovalSetting.md §1`). Until it does, it binds only the old name, and after it does only the new one. So the React app first reads either name and sends both (§1, with `UI/Foundations/ApprovalSettingService.BereanReview.md`), and drops the old name only once the server is renamed (§2). Every request is built from the model's own fields, never from the object a read returned. Built from that object, a stale member from the read would ride along beside the edited one, and the server would bind whichever it knows.

## 1. Read either name, send both (#1145)

1. **`fromApprovalSettingResponse`, new**, maps a response onto the model: the block's value is taken from `blockOnRejectApprovalScore` where the response carries it, and from `blockOnZeroApprovalScore` otherwise, onto the model's `blockOnZeroApprovalScore`. The result carries no other name for it. Every other field is copied as it is.
2. **`toApprovalSettingAddRequest` sends the block's value under both names.**
3. **`toApprovalSettingUpdateRequest`, new**, builds the update's body from the model's fields alone — the fields the add sends, plus `id` and the audit fields the update sends today — with the block's value under both names.
4. The pages and the hooks are unchanged here. `UI/Foundations/ApprovalSettingService.BereanReview.md` applies the two new mappers.

## 2. The new name alone, and no vote (#1153)

1. **The model's field is renamed `blockOnRejectApprovalScore`**, and both mappers read and send that name alone. `fromApprovalSettingResponse` stops consulting `blockOnZeroApprovalScore`.
2. **`isAIAllowedToVote` leaves the model** and its mappers, with its comments. `newApprovalSetting` keeps the block `true`.
3. **The comments on the two thresholds** stop saying they are *"read only when isAIAllowedToVote is true"*, and say what reads them (BereanReview.md rule 12).
4. The pages' and the hooks' references follow by name, with no change in what they do.
5. **It waits for `Backend/Models/ApprovalSetting.md §1` on `main`.** Before it, the deployed server still sends the old name.
