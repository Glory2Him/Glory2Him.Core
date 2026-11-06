# Approval setting service (React) — the block's two names
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation service — `approvalSettingService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/approvalSettingService.ts`). The services-and-brokers work on the same service is `UI/Foundations/ApprovalSettingService.md`.
Inherits: §UI20.9.2, `UI/Models/ApprovalSetting.md §1`, BereanReview.md *What ships together* step 1

While the server is renamed, the hooks read either name and send both (`UI/Models/ApprovalSetting.md §1`). Each hook below applies one of that section's mappers, and each lands before the server's rename (`Backend/Models/ApprovalSetting.md §1`). `useAddApprovalSetting` already sends `toApprovalSettingAddRequest`, so it needs nothing.

## 1. useGetApprovalSettings (#1146)

The list read hands the page each row through `fromApprovalSettingResponse`, so the list's *Blocks on low score* pill reads the block from either name.

## 2. useGetApprovalSettingById (#1147)

The read by id hands the page the row through `fromApprovalSettingResponse`, so the detail page's switch reads the block from either name, and the object it edits carries the block under one name only.

## 3. useUpdateApprovalSetting (#1148)

The update sends `toApprovalSettingUpdateRequest(approvalSetting)` in place of the object the page edited. So the block's edited value goes under both names, and nothing from the read rides along beside it.
