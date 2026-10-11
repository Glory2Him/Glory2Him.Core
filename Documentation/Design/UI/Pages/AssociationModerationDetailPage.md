# 1. AssociationModerationDetailPage

One tag or Bible reference on a post, in the admin shell: the page a moderator reaches from one of
the post's chips on `/Admin/Posts/{contentItemId}`. The post stands across the top, its chips on
its card with this association's ringed. Beneath it, on the left, is what is being judged — the
association, who suggested it and when, the confidence Berean's process recorded for it, and its
review thread — and on the right who is judging it: the association's own approval round. It is a
screen of the post moderation feature, `DesignFeatures/PostModeration.md`, designed under #705
from the product owner's mock `Admin Association Detail.dc.html` (user rulings 2026-10-11). None of
it is built yet: section 6 holds every part.

- **Route:** `/Admin/Posts/{contentItemId}/Associations/{associationId}`, a child of the
  `SidebarLayout` layout route in `src/routes/adminRoutes.tsx`, beside `Admin/Posts/:contentItemId`
  (`DesignFeatures/PostModeration.md` rule 1). Not yet registered (item 1).
- **Source:** `src/pages/admin/associationModerationDetailPage.tsx`, new; its tests
  `src/pages/admin/associationModerationDetailPage.test.tsx`, new
- **Section:** admin (moderation)
- **Access:** `SecuredRoute` with `securityPoints.contentItems.view` (`src/securityMatrix.tsx`),
  the post page's own point: `Administrators` today, widening when the post page's does
  (rule 2.2).
- **Layout:** three columns — the admin shell's navigation, then the page's main column and its
  right sidebar beneath a full-width post card; two columns when the shell's menu is folded
- **Components:** [ContentItemPanel.md](../Components/ContentItemPanel.md), on a view template
  ([ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md)
  or [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md));
  [AssociationDetailPanel.md](../Components/AssociationDetailPanel.md);
  [ReviewCommentPanel.md](../Components/ReviewCommentPanel.md);
  [ReviewPanel.md](../Components/ReviewPanel.md). The undocumented building blocks are the
  core-UI primitives `Breadcrumb`, `Button`, `Spinner` and a `ConfirmDialog` (`src/components/coreUI/`).

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. The post page is `UI/Pages/ContentItemModerationDetailPage.md`, cited below as
*the post page*.

## 2. Business Rules

**2.1 [Must]** The page shows one association of the post in its address: the post, the association, its confidence block, its review thread, and its reviews and *Review Outcome*, with *Back to Posts* and *Back to Post* (`DesignFeatures/PostModeration.md` rules 1 and 12). *(user, 2026-10-11)* ≠ items 1, 2, 6, 14 and 22

**2.2 [Must]** Whoever reaches the post page reaches this one: the same security point, `securityPoints.contentItems.view`, which admits `Administrators` today. When the post page widens to the review and publisher tiers (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`), this page widens with it (`DesignFeatures/PostModeration.md` rule 2). *(user, 2026-10-11)* ≠ item 1

**2.3 [Must]** The page shows an association only under its own post. It reads the post by `contentItemId` and the association by `associationId` (`GET /api/associations/{id}`, #857), and shows the association only where one of its endpoints is a `ContentItem` whose effective id is the post's id or the post's group id (§DOM4.6) and the other is a `Tag` or a `BibleReference`. Otherwise — the association is another post's, is neither a tag nor a Bible reference, or cannot be read — the page shows its could-not-load state, as rule 2.4 has it, and never the association (`DesignFeatures/PostModeration.md` rule 18). *(user, 2026-10-11)* ≠ item 1

**2.4 [Should]** While the post or the association loads, a spinner stands in for the page. When either cannot be read, or the tag or Bible reference the association names cannot be read, or rule 2.3 refuses the pair, the page shows the post page's words, "We could not load this post right now. It may have been removed, or it may not be yours to moderate.", and offers *Back to Posts* (`UI/Pages/ContentItemModerationDetailPage.md rule 2.20`; `DesignFeatures/PostModeration.md` rules 18 and 20). *(user, 2026-10-11)* ≠ item 1

**2.5 [Should]** The heading and the tab title name the association: *Tag: #* and the tag's name, or *Bible reference:* and the reference. The breadcrumb reads *Admin* › *Posts* › the post's heading, as the post page words it (`UI/Pages/ContentItemModerationDetailPage.md rule 2.18`), linking to the post page › the association's chip text. *(user, 2026-10-11: the mock's `assocHeading` and breadcrumb)* ≠ item 22

**2.6 [Could]** *Back to Posts* leads to the queue, `/Admin/Posts`, as the post page's does (`UI/Pages/ContentItemModerationDetailPage.md rule 2.21`): as the moderator left it where the state that brought them here carries the queue's address, the bare queue otherwise. *Back to Post* leads to the post page, `/Admin/Posts/{contentItemId}`. Both always render, *Back to Post* the primary of the two. *(user, 2026-10-11: the mock's two links)* ≠ item 22

**2.7 [Must]** The post leads: its card stands across the page's full width, above both columns (`DesignFeatures/PostModeration.md` rule 3). Beneath it, what is judged stands on the left — the association's card, then its review thread — and who judges it on the right — its round. *(user, 2026-10-11: the mock's `col-12`, `col-lg-7` and `col-lg-5`)* ≠ items 2, 6, 14 and 22

**2.8 [Must]** The post's card is the post page's moderated card (`UI/Pages/ContentItemModerationDetailPage.md rules 2.15–2.17, 2.30 and 2.34`): its chips on it, in the same order and with the same statuses, or the announced error with *Retry* in their place where they cannot be read, this association's chip marked current and ringed, the status ribbon, the content whole, and the Like control, with Share and Save deliberately not (`DesignFeatures/PostModeration.md` rules 3–8, 11 and 19). *(user, 2026-10-11)* ≠ items 2 and 3

**2.9 [Must]** A chip leads to its association's page, `/Admin/Posts/{contentItemId}/Associations/{associationId}`; the current chip leads nowhere (`DesignFeatures/PostModeration.md` rule 7). *(user, 2026-10-11)* ≠ item 2

**2.10 [Must]** The card's moderation action, labelled *Edit*, leads to the post page with the editor open (`UI/Pages/ContentItemModerationDetailPage.md rule 2.32`). On a decided post the moderator did not contribute it is greyed out as locked, under the condition and in the words of the post page (`UI/Pages/ContentItemModerationDetailPage.md rule 2.7`; `DesignFeatures/PostModeration.md` rule 11). *(user, 2026-10-11)* ≠ item 4

**2.11 [Must]** `Administrators` take the post down from the card's *Take down*, at any status, as on the post page (`UI/Pages/ContentItemModerationDetailPage.md rule 2.24`), and the takedown returns them to the queue, or keeps them here with the reason toasted where it fails (`UI/Pages/ContentItemModerationDetailPage.md rule 2.9`; `DesignFeatures/PostModeration.md` rule 10). *(user, 2026-10-11)* ≠ item 5

**2.12 [Must]** The association's card shows whether it is a tag or a Bible reference, the tag's name or the reference, the reference's scripture where its row carries one, who suggested it — the association's `CreatedBy`, named from the contributor read — and when, its `CreatedWhen`, and its status. The page reads the far end through `DesignFeatures/UI/Foundations/TagService.md §1` or `DesignFeatures/UI/Foundations/BibleReferenceService.md §1`, and hands the whole to `AssociationDetailPanel`. *(user, 2026-10-11)* ≠ item 1

**2.13 [Must]** The confidence block applies only where the association records both a confidence score and a confidence reason: the page hands both over as the association carries them, and the panel renders the block only then (`UI/Components/AssociationDetailPanel.md rule 2.4`). Where either is null, the block is absent and nothing else on the page depends on it. The suggestion — an approval, a rejection, or unable to suggest either, against the round's resolved thresholds — is the band the page hands the panel once #863's read exists (`DesignFeatures/PostModeration.md` rule 13). *(user, 2026-10-11)* ≠ item 19

**2.14 [Must]** The page offers everything `ReviewPanel` offers on the association's round — its reviews, a vote, requesting reviewers and Berean, the decision with its bypass for the publisher tier and `Administrators`, and the reset of a decided round for `Administrators` — and the association's own review thread (`DesignFeatures/PostModeration.md` rule 14). *(user, 2026-10-11)* ≠ items 6–18

**2.15 [Must]** Both panels compose their tiers from the association's two endpoints, which the page hands each as `roleSubjectCollection`, each endpoint its entity type and, on a `ContentItem`, its content type's enum member name (`UI/Components/ReviewPanel.md rule 2.54`, `UI/Components/ReviewCommentPanel.md rule 2.35`; §SEC14.7 posture A′ rules 1–3; `DesignFeatures/PostModeration.md` rule 15). *(user, 2026-10-11)* ≠ items 6 and 14

**2.16 [Must]** The round is read, not invented, as the post page reads its own (`UI/Pages/ContentItemModerationDetailPage.md rule 2.10`): the verdict, the reviews, the outstanding requests, the reviewer names, Berean's status and the thread come off the approval endpoints, asked by the association's type and id, `Association` and `associationId`, on the routes served today (§ARC17.5, *As built*). The round's approval id is the one the approval record carries, taken off the verdict the record answers with (`DesignFeatures/PostModeration.md` rule 17). `ReviewPanel`'s owner and status come off the stored association: its `CreatedBy` and its `ApprovalStatus`. *(user, 2026-10-11)* ≠ items 6, 14 and 21

**2.17 [Must]** Until the association's approval record exists — the verdict read answering not-found — every control that depends on the round's id is disabled: the page tells both panels so (`isAwaitingApprovalRecord`; `UI/Components/ReviewPanel.md rule 2.55`, `UI/Components/ReviewCommentPanel.md rule 2.36`), and a later read of the round refresh (rule 2.18) that answers with the record enables them (`DesignFeatures/PostModeration.md` rule 17). On the route the page reads today, keyed by the association, the verdict read opens a missing round itself for a `Draft` or `Submitted` association, and answers not-found only for an association taken down or a decided one whose round was never recorded, which no read opens (§ARC16.7.2, *The write as built*): there the controls stay disabled. A round the approval process has yet to record reaches the page once it reads the round by its approval id (item 21), a read that opens no round (§ARC16.7.2). *(user, 2026-10-11)* ≠ items 6 and 14

**2.18 [Must]** The page's single round refresh keeps the round and the thread current as the post page's does (`UI/Pages/ContentItemModerationDetailPage.md rule 2.12`): every 15 seconds while the tab is visible, on returning to the tab, and on reconnect. The association is re-read with the round, because `ReviewPanel` takes its status from the stored association, and the reviewer candidates are left out of it (`UI/Components/ReviewPanel.md rules 2.8 and 2.32`). *(user, 2026-09-27; user, 2026-10-11)* ≠ item 6

**2.19 [Must]** The round's writes behave as the post page's (`UI/Pages/ContentItemModerationDetailPage.md rules 2.11, 2.13 and 2.27`): nothing is optimistic save the card's reaction, a person's write refreshes the whole round and Berean's two writes refresh Berean's status alone, every write to the thread refreshes the verdict too, a refusal is shown with the server's reason, and the reviewer candidates are read when the request picker opens. A decision's success is toasted in the post page's words with *post* replaced by *tag* or *Bible reference* — "The tag has been approved.", "The Bible reference has been rejected." — and a reset's likewise — "The approval has been reset. The tag is back with the reviewers, and its recorded reviews have been dismissed.". *(user, 2026-10-11; code: contentItemModerationDetailPage.tsx — the decision's and the reset's toasts)* ≠ items 7–18

**2.20 [Must]** Withdrawing a review comment is confirmed in the post page's words before it is sent, and nothing is sent on Cancel (`UI/Pages/ContentItemModerationDetailPage.md rule 2.14`). *(user, 2026-10-11)* ≠ item 17

**2.21 [Must]** `Administrators` take the association down from *Take down* in its card's footer, at any status, after its confirmation (`UI/Components/AssociationDetailPanel.md rule 2.6`). The page sends `DELETE /api/associations/{id}`; on success it returns to the post page, and a failure keeps the moderator here with the reason toasted (`DesignFeatures/PostModeration.md` rule 16). *(user, 2026-10-11)* ≠ item 20

**2.22 [Must]** No box for suggesting a tag or a Bible reference is offered here (`DesignFeatures/PostModeration.md` rule 9). *(user, 2026-10-11)*

**2.23 [Must]** When the read of the association's review thread fails, an error stands in `ReviewCommentPanel`'s place, announced, reading "We could not load the review thread right now.", with *Retry*, which reads the thread again; once a read of the thread answers, the panel stands there again. A thread that could not be read is never shown empty (`DesignFeatures/PostModeration.md` rule 21). *(user, 2026-10-11)* ≠ item 14

**2.24 [Should]** The post's card names the post's submitter, under *Submitted by*, from a second read of the contributor, keyed by the post's `CreatedBy`, rendered when it arrives rather than waited on, as on the post page (`UI/Pages/ContentItemModerationDetailPage.md rule 2.19`). It is the post's submitter, not the association's suggester that rule 2.12 names. *(user, 2026-10-11: the mock's "Submitted by")* ≠ item 2

**2.25 [Must]** What depends on the post's effective settings waits for them, and nothing else does. The post's card, whose sections, chips and Like control follow the settings (rule 2.8; `UI/Pages/ContentItemModerationDetailPage.md rule 2.17`), is not rendered until the settings read lands: a spinner, announced (§UI20.6.6 rule 5), stands in its place meanwhile, as on the post page (`UI/Pages/ContentItemModerationDetailPage.md rule 2.28`). The heading, the tab title, the links back, the association's card, its round and its thread render once their own reads have answered (rule 2.4), without waiting for the settings. What the breadcrumb's crumb for the post shows meanwhile is item 26 (`DesignFeatures/PostModeration.md` rule 22). *(user, 2026-10-11)* ≠ items 1 and 2

**2.26 [Must]** When the post's settings read fails, an error stands in the post's card's place, announced, reading "We could not load this post's settings right now.", with *Retry*, which reads the settings again; once a read of them answers, the card stands there. The page never shows the card without the post's settings, and the failure takes nothing else off the page: the association's card, its round and its thread stay, and the association can still be reviewed (`DesignFeatures/PostModeration.md` rule 23). *(user, 2026-10-11)* ≠ items 1 and 2

## 3. Layout

The admin shell puts its navigation menu in a left-hand column. Inside the page's own column the
post's card spans the width; beneath it the page has two columns, `col-lg-7` for what is judged and
`col-lg-5` for who judges it. Below the `lg` breakpoint everything stacks: the menu, the post, the
main column, then the right-hand column.

```text
+-------------------------------------------------------------------------------+
| site header                                                                   |
+-------------------------------------------------------------------------------+
| +-----------+ +-------------------------------------------------------------+ |
| | admin     | | [=] <Tag: #name>          Admin > Posts > <post> > <#name>   | |
| | menu      | | ----------------------------------------------------------- | |
| | (NavMenu) | |                         [<- Back to Posts] [<- Back to Post] | |
| |           | | +---------------------------------------------------------+ | |
| |           | | | ContentItemPanel — the post, its chips, this one ringed | | |
| |           | | +---------------------------------------------------------+ | |
| |           | | +------------------------------+ +------------------------+ | |
| |           | | | AssociationDetailPanel       | | ReviewPanel            | | |
| |           | | | ReviewCommentPanel           | |   Approval Reviews     | | |
| |           | | |                              | |   Review Outcome       | | |
| |           | | +------------------------------+ +------------------------+ | |
| +-----------+ +-------------------------------------------------------------+ |
+-------------------------------------------------------------------------------+
| site footer                                                                   |
+-------------------------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Left sidebar (the shell's) | `col-lg-3`; absent when the menu is folded | the admin navigation menu — not the page's |
| Page head | the shell's `col-lg-9`, or `col-12` folded | the shell's fold toggle; the heading and the `Breadcrumb`; a rule; *Back to Posts* and *Back to Post*. The spinner, or the error alert with *Back to Posts*, stands in for everything below until the post and the association are read. |
| The post | `col-12` of the page's row | `ContentItemPanel`, or rule 2.25's spinner or rule 2.26's error in its place |
| Main | `col-lg-7` of the page's row | `AssociationDetailPanel`; `ReviewCommentPanel`, or rule 2.23's error in its place |
| Right sidebar | `col-lg-5` of the page's row | `ReviewPanel` |

The `ConfirmDialog` that withdraws a comment is modal and belongs to the page (rule 2.20).

## 4. Components and their hooks

Nothing on this page is built; every *Built today* cell names the section-6 item that builds it.

### 4.1 ContentItemPanel — the post's card

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `contentItem` | The post projected as the post page projects it, its submitter's name and image from the contributor read (rule 2.24), with `withReactions` laying on it its reaction summary and the overlay of a choice the reader has just made (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §1 and §2`) | Rules 2.8 and 2.24. |
| `contentItemSettingCollection` | The post's effective settings read; the card is not rendered until it lands | Rules 2.8, 2.25 and 2.26. |
| `showModerationSection`, `moderationOpensEditor` | `true`, `true`: the action opens the editor — on the post page — so the terminal lock applies as there | Rule 2.10. |
| `showApprovalStatusRibbon` / `showApprovalStatus` | `true` / `false` | Rule 2.8. |
| `showContentExpanded` | `true` | Rule 2.8. |
| `associationChipCollection` | The post's associations as the post page builds them (`UI/Pages/ContentItemModerationDetailPage.md rule 2.30`), this association's chip `isCurrent` | Rules 2.8 and 2.9. |
| `isAssociationChipReadFailed` | `true` while a read of the chips has failed, as on the post page (`UI/Pages/ContentItemModerationDetailPage.md rule 2.34`) | Rule 2.8. |
| `reactionOptions`, `onReactionSelected` | The approved reactions, and the engagement hook's handler | Rule 2.8. |
| `onModerateClick`, `onAssociationChipClick`, `onTakeDown` | The handlers below | Rules 2.9–2.11. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAssociationChipClick` | A chip that is not current is pressed | Opens that association's page, `/Admin/Posts/{contentItemId}/Associations/{associationId}` (rule 2.9). | ❌ No — item 2 |
| `onAssociationChipRetry` | *Retry* is pressed on the chips' error | Reads again each of the chips' reads that failed, as the post page does (rule 2.8; `UI/Pages/ContentItemModerationDetailPage.md rule 2.34`). | ❌ No — item 2 |
| `onReactionSelected` | The reader chooses a reaction | Records, changes or clears the reader's own reaction, through `useContentItemEngagement`. | ❌ No — item 3 |
| `onModerateClick` | The moderation action, labelled *Edit*, is pressed while not locked | Opens the post page, `/Admin/Posts/{contentItemId}`, asking it for the editor (rule 2.10). | ❌ No — item 4 |
| `onTakeDown` | *Delete* is pressed on *Take down*'s confirmation | `DELETE api/ContentItems/{contentItemId}`, then the queue; a failure is toasted and the moderator stays (rule 2.11). | ❌ No — item 5 |
| `onEditClick` | The owner's Edit is pressed | Never offered: the moderated face removes it (`UI/Components/ContentItemPanel.md rule 3.1.6`). | *Not wired — switched off* |
| `onShareClick`, `onSaveClick` | *Share* or *Save* is pressed | Not offered (rule 2.8). | *Not wired — switched off* |
| `onTagClick`, `onBibleReferenceClick` | A pill is pressed | — | *Never raised*: the card shows association chips (rule 2.8) |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` | The type chip, *Submitted by* or *Author* is pressed | Opens the queue handed the value, as the post page is to (`UI/Pages/ContentItemModerationDetailPage.md rule 2.26`). | ❌ No — item 23 |
| `onTitleClick`, `onReadMore`, `onExpandCollapse`, `onCommentsClick` | — | As the post page (`UI/Pages/ContentItemModerationDetailPage.md §4.1`). | *Not wired — switched off* |
| `onModified`, `onRemoved`, `onCancelled`, `onAdded` | The panel's writing faces | — | *Never raised*: `showEditSection` is off, and the editor is the post page's |

### 4.2 AssociationDetailPanel — the association

**Properties the page sets:** `association`, the association projected with its far end's label
and scripture and its suggester's name (rule 2.12), carrying its `ConfidenceScore` and
`ConfidenceReason` as they are (rule 2.13); `confidenceSuggestion`, once #863's read exists
(item 19); `roleSubjectCollection`, the association's two endpoints (rule 2.15); `showBorder`.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onTakeDown` | *Delete* is pressed on the association's *Take down* confirmation | `DELETE /api/associations/{id}`; on success the post page, and on failure a toast and the moderator stays (rule 2.21). | ❌ No — item 20 |

### 4.3 ReviewCommentPanel — the association's thread

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `approvalId` | The round's approval id, off the verdict the record answers with, or `''` | Rule 2.16. |
| `reviewComments`, `isLoading` | The round's thread and its loading state | Rule 2.16. |
| `entityType`, `roleSubjectCollection` | `Association`, and the association's two endpoints | Rule 2.15. |
| `isAwaitingApprovalRecord` | `true` while the verdict read answers not-found | Rule 2.17. |
| `isSubmitting` | Any of the four comment writes pending | One click is one write. |
| `showBorder`, `cssClass` | `true`, `mt-4` | Spacing in the column. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| *(the thread itself)* | — | Reads and shows the thread, refreshed with the round (rules 2.16 and 2.18); where the thread cannot be read, rule 2.23's error stands in the panel's place. | ❌ No — item 14 |
| `onSave` | *Save* on a non-blank box | `POST api/ApprovalComments`, toasting and rethrowing a refusal, as the post page does. | ❌ No — item 15 |
| `onModified` | *Save* on the edit face | `PUT api/ApprovalComments` with the stored row, only the words and the type moved, as the post page does. | ❌ No — item 16 |
| `onRemoveRequested` | *Delete* on the author's own row | Asks the confirmation of rule 2.20, then `DELETE api/ApprovalComments/{id}` with the reason *Withdrawn by the author*. | ❌ No — item 17 |
| `onResolvedChanged` | The settled tick on a question changes | `POST api/ApprovalComments/{id}/Resolve?isResolved=`. | ❌ No — item 18 |
| `onClear` | *Clear* is pressed | Nothing: the box clears itself. | *Not wired — nothing to do* |
| `onRemoved` | *Delete*, only where nobody listens on `onRemoveRequested` | — | *Never raised*: the page listens on `onRemoveRequested` |
| `onLoadMore` | The foot of the thread comes into view | — | *Never raised*: the page reads the thread whole |

### 4.4 ReviewPanel — the association's round

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `entityType`, `roleSubjectCollection` | `Association`, and the association's two endpoints | Rule 2.15. |
| `entityOwnerId`, `approvalStatus` | The stored association's `CreatedBy` and `ApprovalStatus` | Rule 2.16. |
| `approvalVerdict`, `approvalReviewCollection`, `requestedReviewerCollection`, `reviewerCandidateCollection`, `isLoading` | From the round read by the association's type and id | Rule 2.16. |
| `aiReviewerCandidate`, `aiReviewerAssignment` | Derived from Berean's status read | Rule 2.14. |
| `isAwaitingApprovalRecord` | `true` while the verdict read answers not-found | Rule 2.17. |
| `showBorder` | `true` | — |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| *(the round itself)* | — | Reads and shows the round, refreshed every 15 seconds, on returning to the tab and on reconnect (rules 2.16–2.18). | ❌ No — item 6 |
| `onReviewStatusChanged` | The viewer casts or changes their vote | Files a first review, or amends the viewer's standing review (`POST` / `PUT api/ApprovalReviews`), as the post page does. | ❌ No — item 7 |
| `onApprovalStatusChanged` | *Submit* on a decision | The decision on the association's round, on the route the post page calls today, keyed `Association` and `associationId` (rule 2.16), toasting the outcome (rule 2.19). | ❌ No — item 8 |
| `onApprovalReset` | An administrator resets a decided round | The reset on the association's round, keyed as the decision is, then a toast (rule 2.19). | ❌ No — item 9 |
| `onReviewerLookupRequested`, `onReviewRequested` | The request picker opens; a person is picked under Suggestions or Everyone else | Reads the reviewer candidates when the picker opens; asks the person to review on the review-request route, keyed as the decision is. | ❌ No — item 10 |
| `onReviewRequestWithdrawn` | A person is picked under Requested | Withdraws the request on the review-request route, keyed as the decision is. | ❌ No — item 11 |
| `onAIReviewerRequested` | Berean is picked, or re-requested | Asks Berean to review on its resource, keyed `Association` and `associationId`, refreshing Berean's status alone. | ❌ No — item 12 |
| `onAIReviewerWithdrawn` | Berean is picked under Requested | Withdraws Berean on its resource, keyed as above, with no success toast. | ❌ No — item 13 |

## 5. Security and access

The rows state the design (rules 2.2, 2.10, 2.11, 2.14 and 2.21). Today nothing is served on this
route (item 1), and the route is to admit what the post page admits: `Administrators` alone until
the post page widens (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`). General users and
signed-out visitors never reach the page, so for them every action row is `➖ n/a`. Owner here is
the association's suggester, its `CreatedBy`, holding no tier. Reviewer and Publisher hold the tier
at a scope matching either endpoint (§SEC14.7 posture A′ rule 2). What each panel offers each
persona, and the read-only roles each action answers to, are the components' own matrices:
`UI/Components/ContentItemPanel.md §5`, `UI/Components/AssociationDetailPanel.md §5`,
`UI/Components/ReviewCommentPanel.md §5` and `UI/Components/ReviewPanel.md §5`; on this page the
last three compose their read-only roles from the association's two endpoints, a block on either
barring (§SEC14.7 posture A′ rules 1 and 3).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — the post, the association, its round and its thread, read ≠ items 1, 2, 6 and 14 | ❌ No¹ | ❌ No | ❌ No | ✅ Yes² | ✅ Yes² | ✅ Yes |
| Association `Submitted`, no read-only role in scope — **a vote** ≠ item 7 | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Association `Submitted` — **the decision**, and the bypass where the verdict allows it ≠ item 8 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes | ✅ Yes |
| Association `Approved` or `Rejected` — **the reset** ≠ item 9 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ✅ Yes |
| No approval record yet — **every round control**, live ≠ items 6 and 14 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| Any status — **the association's *Take down*** ≠ item 20 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ✅ Yes |
| Any status — **the post's *Take down*** ≠ item 5 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ✅ Yes |
| Post `Approved` or `Rejected`, not the viewer's — **Edit**, rendered locked ≠ item 4 | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| The setting allows reactions — **Like** ≠ item 3 | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |

¹ The route guard shows *Access Restricted* and a *Login* button.
² Once the post page widens (rule 2.2); `Administrators` alone today.

## 6. Open Questions and Gaps

1. (#1050) **Page gap — the association page does not exist.** Rules 2.1–2.4, 2.12, 2.25 and
   2.26 (user rulings 2026-10-11): the route, guarded as the post page's; the reads of the post,
   the association (#857), its far end and its suggester's name; the refusal of an association
   that is not the post's, and the could-not-load state; and the association's card,
   `AssociationDetailPanel` (`UI/Components/AssociationDetailPanel.md §10 items 1 and 2`), which
   waits for none of the post's settings and stays when they cannot be read. The
   heading, the breadcrumb, the two links back and the columns are item 22. No route answers
   `Admin/Posts/:contentItemId/Associations/:associationId` (`src/routes/adminRoutes.tsx`), and
   no page shows an association.
2. (#1055) **Page gap — the post's card shows no chips here.** Rules 2.7–2.9 and 2.24–2.26:
   the post's card, across the page's full width above both columns, naming the post's
   submitter from the contributor read, with its association chips, this association's current
   and ringed, and each other chip leading to its association's page, or the announced error
   with *Retry* in their place where they cannot be read, as on the post page; held back behind
   an announced spinner until the post's settings land, and an announced error with *Retry* in
   its place where they cannot be read. It builds on item 1's
   page and item 22's columns. Where the card's type chip, *Submitted by* and *Author* lead is
   item 23.
3. (#1056) **Page gap — the post's card offers no Like control here.** Rule 2.8
   (`DesignFeatures/Likes.md` rule 11; user ruling 2026-10-11): the card shows the post's
   reaction counts and the reader's own, and records, changes or clears it, through
   `useContentItemEngagement`, as the post page does (`UI/Pages/ContentItemModerationDetailPage.md
   §6 item 3`).
4. (#1057) **Page gap — Edit on the post's card leads nowhere.** Rule 2.10 (user rulings
   2026-10-11): it leads to the post page with the editor open, locked as there. The post page's
   half, opening on the editor, is `UI/Pages/ContentItemModerationDetailPage.md §6 item 18`.
5. (#1058) **Page gap — the post cannot be taken down here.** Rule 2.11 (user rulings
   2026-10-11): `Administrators` take the post down from the card's *Take down*. The card's half
   is `UI/Components/ContentItemPanel.md §10 item 26`.
6. (#1059) **Page gap — the association's round is not shown.** Rules 2.7 and 2.14–2.18: the
   association's round in `ReviewPanel` — its reviews, Berean's row and its *Review Outcome* —
   read by the association's type and id, its tiers composed from the association's endpoints,
   its controls disabled until the approval record exists, refreshed with the association, and
   standing in the right-hand column (rule 2.7).
   The panel's halves are `UI/Components/ReviewPanel.md §10 items 13 and 14`. The React app's
   `EntityTypeName` has no `Association` member yet (`src/models/foundations/approvals/approval.ts`).
7. (#1060) **Page gap — no vote can be cast on the association.** Rules 2.14 and 2.19:
   `onReviewStatusChanged` files or amends the viewer's review.
8. (#1061) **Page gap — the association cannot be decided.** Rules 2.14, 2.16 and 2.19:
   `onApprovalStatusChanged` sends the decision, with its bypass, on the association's round, and
   toasts the outcome.
9. (#1062) **Page gap — a decided association's round cannot be reset.** Rules 2.14 and
   2.19: `onApprovalReset`, `Administrators` only.
10. (#1063) **Page gap — no reviewer can be asked to review the association.** Rules 2.14
    and 2.19: the picker reads the candidates when it opens, and `onReviewRequested` asks the
    person picked.
11. (#1064) **Page gap — a review request on the association cannot be withdrawn.** Rules
    2.14 and 2.19: `onReviewRequestWithdrawn`.
12. (#1065) **Page gap — Berean cannot be asked to review the association.** Rules 2.14 and
    2.19: `onAIReviewerRequested`, for a first request and a re-request alike.
13. (#1066) **Page gap — Berean cannot be withdrawn from the association's round.** Rules
    2.14 and 2.19: `onAIReviewerWithdrawn`.
14. (#1067) **Page gap — the association's review thread is not shown.** Rules 2.7, 2.14–2.18 and 2.23:
    the thread in `ReviewCommentPanel`, beneath the association's card in the left-hand column
    (rule 2.7), its tiers composed from the association's endpoints, its box disabled until the
    approval record exists, and an announced error with *Retry* in its place where the thread
    cannot be read. The panel's halves are
    `UI/Components/ReviewCommentPanel.md §10 items 22 and 23`.
15. (#1068) **Page gap — no comment can be added to the association's thread.** Rule 2.19:
    `onSave`.
16. (#1069) **Page gap — a comment on the association's thread cannot be amended.** Rule
    2.19: `onModified`.
17. (#1070) **Page gap — a comment on the association's thread cannot be withdrawn.** Rules
    2.19 and 2.20: `onRemoveRequested`, confirmed first.
18. (#1071) **Page gap — a question on the association's thread cannot be marked
    resolved.** Rule 2.19: `onResolvedChanged`.
19. (#863) **Page gap — the confidence block shows no suggestion.** Rule 2.13 (user ruling
    2026-10-11): where the block is shown, it carries the suggestion — an approval, a rejection,
    or unable to suggest either — against the round's resolved
    `AIApprovalConfidenceApprovalThreshold` and `AIApprovalConfidenceRejectionThreshold`
    (§APR8.6.2). No read answers those thresholds, or the band they put a score in, for one
    round, and the mock's cut-offs above 7 and below 4 are not the policy. #863 designs the read,
    and carves this item's task or returns it to `(needs issue)` (#863 criterion 9). The panel
    already renders a band it is handed (`UI/Components/AssociationDetailPanel.md rule 3.1.6`).
20. (needs issue) **Page gap — the association cannot be taken down.** Rule 2.21 (user rulings
    2026-10-11): `Administrators` take the association down from its card, and the page sends
    `DELETE /api/associations/{id}`. No route serves it: the remove by id waits on
    `UI/Components/AssociationPanel.md §10 item 22`, which gives it to #700 with the two gates
    beneath it, and #700 has carved no task for it. So this item is not carved before that route
    has a task. The panel's half is `UI/Components/AssociationDetailPanel.md §10 item 3`.
21. (needs issue) **Page gap — the association's round is addressed by the association, not by
    its approval id.** Rule 2.16 reads and writes the round on the routes served today, keyed by
    the association's type and id, as the post page does
    (`UI/Pages/ContentItemModerationDetailPage.md §6 item 11`). Every call about a round is to be
    addressed by its approval id (§ARC17.5; ruled under #699, not built). The routes wait on
    #699's stage-2 build, which has no task (`UI/Components/ReviewPanel.md §10 item 12`); the
    page's half is to address every call about the round by the approval id once they are served.
22. (#1074) **Page gap — the association page has no heading, no way back and no columns.** Rules
    2.5–2.7 (user rulings 2026-10-11): the heading and the tab title naming the association, the
    breadcrumb through the post, *Back to Post* and *Back to Posts*, and §3's regions — a
    full-width row for the post's card above two columns, the association's card in the left-hand
    one. Items 2, 6 and 14 stand the post's card, the round and the thread in their places. It
    builds on item 1's page.
23. (#1075) **Page gap — the post card's type chip, *Submitted by* and *Author* lead nowhere
    here.** `UI/Pages/ContentItemModerationDetailPage.md rule 2.26` (user ruling 2026-09-27), which
    the card here follows (section 4.1): each click opens the queue, `/Admin/Posts`, handed the
    value in its query string. The same gap on the post page is
    `UI/Pages/ContentItemModerationDetailPage.md §6 item 5`, and the queue's half, opening its
    advanced section on the value, is `UI/Pages/ContentItemModerationPage.md §6 item 6`. It builds
    on item 2's card.
24. **Retired.** What the page shows when the post, the association, its tag or Bible reference,
    the post's chips or the review thread cannot be read — the user ruled on 2026-10-11 (#705,
    rulings 18–21): rules 2.4, 2.8 and 2.23. The post's settings read is item 25.
25. **Retired.** What the page shows while the post's settings load, and when they cannot be
    read — the user ruled on 2026-10-11 (#705, rulings 22–23): rules 2.25 and 2.26. What the
    breadcrumb's crumb for the post shows meanwhile is item 26.
26. **Question — what the breadcrumb's crumb for the post shows while the post's settings load,
    and when they cannot be read.** The crumb is the post's heading as the post page words it
    (rule 2.5; `UI/Pages/ContentItemModerationDetailPage.md rule 2.18`): the post's title where
    its effective `HasTitle` allows and one exists, and its content type's name otherwise, both
    resolved against the post's settings. Ruling 22 has what depends on the settings wait for
    them, and ruling 23 says what takes the card's place when they cannot be read, not the
    crumb's. (a) While the settings read loads, does the breadcrumb leave the post's crumb out
    until the settings land, or wait whole? (b) When the settings read fails, does the
    breadcrumb leave the post's crumb out, or name the post some other way — and if so, how? An
    answer adds a rule beside rule 2.25.
