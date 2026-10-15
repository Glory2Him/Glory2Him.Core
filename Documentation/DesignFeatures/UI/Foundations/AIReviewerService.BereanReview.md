# AI reviewer service (React) — the verdict after Berean's two writes
Parent: [BereanReview.md](../../BereanReview.md)
Level: foundation — `aiReviewerService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/aiReviewerService.ts`), existing.
Inherits: §UI20.9, BereanReview.md rules 11, 14 and 18, `UI/Pages/ContentItemModerationDetailPage.md` rule 2.11, `UI/Pages/AssociationModerationDetailPage.md` rule 2.19

Asking Berean holds the round until its review has completed, and withdrawing it lifts that hold (BereanReview.md rules 11 and 18), so each write moves the round's verdict as well as Berean's status (rule 14). Both moderation pages ask and withdraw through this service's two hooks, and neither holds a query client, so the refresh is made in the hooks, as the thread's writes refresh the verdict in theirs (`approvalCommentService.ts`). Each hook is tested at the hook, as the thread's refresh is (`approvalCommentService.test.tsx`, *what a write refreshes*): a page test mocks this service whole, and cannot see what a hook invalidates.

## 1. useAssignAIReviewer — the verdict refreshed (#1178)

1. **On success it invalidates the round's verdict, keyed by the entity the write names**, `['ApprovalVerdict', entityType, entityId]`, beside Berean's status, `['AIReviewerStatus', entityType, entityId]`, as it does today. The verdict's key is the entity-keyed one `approvalService.useGetApprovalVerdict` reads (`approvalService.ts`), and the write knows the entity, so it invalidates that key and not the bare `['ApprovalVerdict']` prefix, which would refetch every round's verdict the client holds — the reason this file already gives for keying Berean's status by the entity.
2. **A refused write invalidates neither**: the refusal is an answer, and the page shows it with the server's reason (`UI/Pages/ContentItemModerationDetailPage.md` rule 2.13, `UI/Pages/AssociationModerationDetailPage.md` rule 2.19).
3. **Nothing else of the round is invalidated**: an assignment moves no person's vote, candidate or request.
4. **The words it makes untrue are rewritten**: the comment over the one read that can answer differently after either write, which names Berean's status alone, and the comment over the hooks that says neither invalidates the round, each saying that the verdict moves too; and the comment over this hook, which still describes the re-request.

## 2. useWithdrawAIReviewer — the verdict refreshed (#1179)

1. **On success it invalidates the round's verdict as §1 rule 1 does**, beside Berean's status, because the withdrawal lifts the pending block (BereanReview.md rule 18).
2. **A refused write invalidates neither**, as §1 rule 2 has it: the withdrawal is refused once Berean's review has started, or where the round's policy asks for Berean automatically (rule 18).
3. **The comment over this hook** stops calling its invalidation the same as the assign's narrow one alone, and says that it refreshes the verdict too.
