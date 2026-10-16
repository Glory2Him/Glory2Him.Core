# Approval setting (React) — the low-score block
Parent: [BereanReview.md](../../BereanReview.md)
Level: UI model — `ApprovalSetting` and its mappers (`Websites/Glory2Him.WebApp.React/src/models/foundations/approvalSettings/approvalSetting.ts`)
Inherits: §UI20.9, BereanReview.md rules 10 and 15, *What ships together* step 1

The server renames `blockOnZeroApprovalScore` to `blockOnRejectApprovalScore` (`Backend/Models/ApprovalSetting.md §1`). Until it does, it binds only the old name, and after it does only the new one. So the React app first reads either name and sends both (§1, with `UI/Foundations/ApprovalSettingService.BereanReview.md`), and drops the old name only once the server is renamed (§2). Every request is built from the model's own fields, never from the object a read returned. Built from that object, a stale member from the read would ride along beside the edited one, and the server would bind whichever it knows.

## 1. Read either name, send both (#1145)

1. **`fromApprovalSettingResponse`, new**, maps a response onto the model: the block's value is taken from `blockOnRejectApprovalScore` where the response carries it, and from `blockOnZeroApprovalScore` otherwise, onto the model's `blockOnZeroApprovalScore`. The result carries no other name for it. Every other field is copied as it is.
2. **`toApprovalSettingAddRequest` sends the block's value under both names.**
3. **`toApprovalSettingUpdateRequest`, new**, builds the update's body from the model's fields alone — the fields the add sends, plus `id` and the audit fields the update sends today — with the block's value under both names.
4. The pages and the hooks are unchanged here. `UI/Foundations/ApprovalSettingService.BereanReview.md` applies the two new mappers.

## 2. The new name alone (#1153)

1. **The model's field is renamed `blockOnRejectApprovalScore`**, and both mappers read and send that name alone. `fromApprovalSettingResponse` stops consulting `blockOnZeroApprovalScore`.
2. **`isAIAllowedToVote` stays**, in the model, its mappers and its comments: the owner's rulings of 2026-10-14 keep Berean's vote (BereanReview.md rule 5). `newApprovalSetting` keeps the block `true`. *(This rule used to take the vote out of the model.)*
3. **Retired.** It rewrote the automatic switch's comment, which names the vote (*"A SIBLING of isAIAllowedToVote, not its child"*), and the vote stays, so the comment is true. The comment on the two thresholds is rewritten earlier, by the low-score block that starts reading the rejection threshold (`Backend/Clients/AccessClient.BereanReview.md §1` rule 7). The number is kept so that rules 4 and 5 keep theirs.
4. The pages' and the hooks' references follow by name, with no change in what they do.
5. **It waits for `Backend/Models/ApprovalSetting.md §1` on `main`.** Before it, the deployed server still sends the old name.
