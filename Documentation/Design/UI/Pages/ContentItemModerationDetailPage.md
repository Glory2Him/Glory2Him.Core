# 1. ContentItemModerationDetailPage

One item from the moderation queue, in the admin shell. On the left is what is being judged: the
post, with its tags and Bible references as chips on its card, each showing where its own round
stands, and the post's review thread. On the right is who is judging it: the approval round, then
the settings that govern the item. Moderate on every list leads here. Reviewers place their review
here, and publishers and administrators decide the item, amend it and narrow its settings. Only
administrators reach it today.

**This page is redesigned under #705** (`DesignFeatures/PostModeration.md`, its parent feature),
from the product owner's mocks `Admin Post Detail.dc.html` and `Admin Association Detail.dc.html`
in the design session of 2026-10-11, which settled the questions #698 asked. Every tag and Bible
reference on a post is an approvable association with a round of its own, and each one's round is
moderated on a page of its own, `/Admin/Posts/{contentItemId}/Associations/{associationId}`
(`UI/Pages/AssociationModerationDetailPage.md`), which the post's chips lead to. The rules below
are the design. Section 4 records the page as built at 70dc72e7, and section 6 what is not built
yet. Which server call decides a suggestion is settled: the same approval routes as the post's own
round, addressed by the association's own approval id (#698, #699).

- **Route:** `/Admin/Posts/{contentItemId}` (`src/routes/adminRoutes.tsx` lines 101-115 at
  70dc72e7). It is a child of the `SidebarLayout` layout route (line 23).
- **Source:** `src/pages/admin/contentItemModerationDetailPage.tsx`;
  its tests are `src/pages/admin/contentItemModerationDetailPage.test.tsx` and
  `src/pages/likeControlSurfaces.test.tsx`
- **Section:** admin (moderation)
- **Access:** `Administrators` today. The route is wrapped in `SecuredRoute` with
  `securityPoints.contentItems.view` (`adminRoutes.tsx` line 112; `src/securityMatrix.tsx` line 27),
  the same point as the queue's. By the user's rulings, reviewers and publishers must reach it too
  (item 1).
- **Layout:** three columns — the admin shell's navigation, then the page's main column and its
  right sidebar; two columns — main with right sidebar — when the shell's menu is folded
- **Components:** [ContentItemPanel.md](../Components/ContentItemPanel.md), on a view template
  ([ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md)
  or [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md));
  [ContentItemPanel.Edit.md](../Components/ContentItemPanel.Edit.md), which the page renders
  directly as its editor;
  [ReviewCommentPanel.md](../Components/ReviewCommentPanel.md);
  [ReviewPanel.md](../Components/ReviewPanel.md);
  [ContentItemSettingsPanel.md](../Components/ContentItemSettingsPanel.md). As built it also renders
  [AssociationPanel.TagAssociationPanel.md](../Components/AssociationPanel.TagAssociationPanel.md)
  and [AssociationPanel.BibleReferenceAssociationPanel.md](../Components/AssociationPanel.BibleReferenceAssociationPanel.md),
  each rendering [AssociationPanel.md](../Components/AssociationPanel.md), which the redesign
  removes (rule 2.22, item 17). The undocumented
  building blocks are the core-UI primitives `Breadcrumb`, `Button`, `Spinner` and two
  `ConfirmDialog`s (`src/components/coreUI/`). The breadcrumb is navigation (§UI20.6.4).

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** Reviewers, publishers and administrators reach the page. The item's Moderate leads every one of them here, where the item's moderation tasks are performed. Publishers and administrators define and remove the item's override settings here. *(user, 2026-09-26; user, 2026-09-27)* ≠ item 1

**2.2 [Must]** The review thread belongs to the admin area. General users do not reach the admin area, so no page offers them the thread. *(user, 2026-09-27)*

**2.3 [Must]** Every write to the review thread — adding a comment, amending or withdrawing one's own, and marking a question resolved — is limited to the item's review tier — its `Reviewers`, `Publishers` and `Administrators`, by the scoped names — under the thread's read-only blocks, and the server checks it (§APR8.9 rule 3; §SEC14.7 posture D rule 5; `UI/Components/ReviewCommentPanel.md §5`). Within the tier, a question is marked resolved by its author, a Publisher or an Administrator; an author who has left the tier can no longer act on the thread. The item's owner who holds that tier may comment too: there is no harm in it. The owner is still refused an approval review of their own item (`UI/Components/ReviewPanel.md rule 2.15`), and to do more they would have to be an administrator, whose bypass they would then hold anyway. *(user, 2026-09-27; test: contentItemModerationDetailPage.test.tsx — "should refuse the vote to an administrator who owns the submission")* ≠ `UI/Components/ReviewCommentPanel.md §10 item 19`

**2.4 [Must]** A reviewer sees the item but does not modify it, and sees `ReviewPanel` in the right-hand column, so they can place their review. *(user, 2026-09-26)* ≠ item 1

**2.5 [Must]** Administrators and the publisher tier for the item's content type define and remove the item's override settings here, through `ContentItemSettingsPanel`, which owns the grant set (`UI/Components/ContentItemSettingsPanel.md rule 2.11`). *(user, 2026-09-27)* ≠ item 1

**2.6 [Must]** What is judged stands on the left and who judges it on the right. The item, its tags and Bible references on its card (rule 2.30), and the review thread form one column. The round, then the settings, form the other, so the decision stays readable at a glance while a thread grows without limit. *(code: contentItemModerationDetailPage.tsx — the comments in the layout; test: contentItemModerationDetailPage.test.tsx — "should stand the item in the seven beside a five", "should stand both association surfaces below the item in the seven", "should stand the review round in the five", "should mount the thread under the bible references, not beside the round"; user, 2026-10-11)* ≠ item 17

**2.7 [Must]** The page opens on the item read-only, and the moderator chooses Edit to modify it — save where the association page's Edit sent them here asking for the editor (rule 2.32): here, moderating means editing. The card is on its moderated face with `moderationOpensEditor` on. Its one action, Moderate wearing Edit's pencil and label — in the admin area Moderate may be labelled *Edit*, since there it opens the item for modification — opens `ContentItemEditPanel` in the card's place, and never navigates, because this page is the destination. On a decided item the moderator did not contribute, the action is greyed out as locked. *(user, 2026-09-27; code: contentItemModerationDetailPage.tsx — the comment above `isEditing`; test: contentItemModerationDetailPage.test.tsx — "should open the editor in place when the moderator takes Edit", "should lock the editor on a decided row the moderator did not contribute", "should open the editor on a submitted row the moderator did not contribute")*

**2.8 [Must]** An amendment sends the stored row with the edit laid over it. The editor closes once the save lands. Where the save forks a new version — the moderator's own `Approved` or `Rejected` item — the card shows that new version, so the amendment appears (`UI/Components/ContentItemPanel.md rule 2.11`). A refusal is read back onto the form and toasted, and Cancel puts the item back as it was. *(test: contentItemModerationDetailPage.test.tsx — "should send the stored row with the edit laid over it", "should put the item back as it was when the edit is cancelled"; §APR9.9)* ≠ item 15

**2.9 [Must]** A takedown — the card's *Take down* (rule 2.24) or the editor's *Delete* — returns the moderator to the queue they came from, as they left it, or to the bare queue. The panel confirms before it raises the removal, so the page asks nothing more. A failed takedown keeps the moderator on the item with the reason toasted. *(user, 2026-10-11; code: contentItemModerationDetailPage.tsx — `removeContentItemAsync`; test: contentItemModerationDetailPage.test.tsx — "should take the item down and go back to the queue", "should keep the moderator on the item when the takedown fails")*

**2.10 [Must]** The round is read, not invented. The verdict, the reviews, the outstanding requests, the reviewer candidates, Berean's status, the names and the thread all come off the approval endpoints through `useApprovalRound`. `ReviewPanel`'s owner and status come off the stored item. A caller the verdict refuses gets the round read-only. *(code: contentItemModerationDetailPage.tsx — the comments above `useApprovalRound` and `ReviewPanel`; test: contentItemModerationDetailPage.test.tsx — "should ask for the round of the item in the url", "should show the round the approval endpoints answer with", "should claim nothing about a round the verdict would not answer for")*

**2.11 [Must]** Nothing is optimistic, save the card's reaction. Every write invalidates what it can move, and the panels repaint off the reads. A person's write refreshes the whole round, and Berean's two writes refresh Berean's status and the verdict, because asking Berean holds the round while its review is pending and withdrawing it lifts that block (§APR8.5 rule 8; user ruling 2026-10-13, `DesignFeatures/BereanReview.md` rule 14). Every write to the review thread also refreshes the verdict, because an outstanding comment is a block reason (`UI/Components/ReviewCommentPanel.md rule 2.15`). What Berean's run writes later reaches the page through rule 2.12's refresh. *(code: contentItemModerationDetailPage.tsx — the comments above the writes)* ≠ items 22 and 23 The reaction is the Likes feature's one exception. The engagement hook shows the reader's choice on the card at once, as an overlay that the read following the item's latest write replaces (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §2` rules 5, 6 and 8).

**2.12 [Must]** The page's single round refresh keeps the round and the review thread current: every 15 seconds while the tab is visible, on returning to the tab, and on reconnect — refreshing on reconnect is required, so a change missed while the connection was down never leaves a stale panel. The item is re-read with the round, because `ReviewPanel` takes its open-or-closed status from the stored item. The thread is re-read as part of the same refresh, and the reviewer candidates are left out of it (`UI/Components/ReviewPanel.md §6`). *(user, 2026-09-27; code: useApprovalRoundChanges.ts — `useApprovalRoundChanges`; test: useApprovalRoundChanges.test.ts — "polls refresh on the interval while the tab is visible", "refreshes immediately when the tab becomes visible again", "refreshes on reconnect — a missed message must not leave a stale panel"; test: contentItemModerationDetailPage.test.tsx — "re-fetches the round on an interval so an external change is seen unprompted", "re-fetches the item too, since the panel takes its open-or-closed status from it", "re-fetches the review thread, so another moderator's comment appears unprompted", "leaves the reviewer candidates out of the poll")*

**2.13 [Should]** A refused write is shown with the reason the server gave, and the page falls back to its own sentence only where none was given. *(test: contentItemModerationDetailPage.test.tsx — "should show the reason the server gave when a write is refused", "should show the reason the server gave when a comment is refused")*

**2.14 [Must]** Two removals are confirmed by the page before they are sent. Withdrawing a review comment asks "This comment will be removed from the review thread. This action cannot be undone." Removing a settings override asks *Remove override?*, "This content item will go back to its content type defaults. The override is deleted permanently and cannot be recovered." Nothing is sent on Cancel. *(code: contentItemModerationDetailPage.tsx — the two `ConfirmDialog`s; test: contentItemModerationDetailPage.test.tsx — "should confirm before withdrawing a comment, and soft delete on OK", "should send nothing when the withdrawal is cancelled")*

**2.15 [Must]** The page offers the Like control on the card, and Share and Save deliberately not. The item here may be at any status, `Draft`, `Submitted` and `Rejected` as well as `Approved`, and the address Share copies, `/posts/{id}`, shows an approved item alone (`UI/Pages/PostDetail.md rule 2.1`). *(code: contentItemModerationDetailPage.tsx — the comment above `useContentItemEngagement`; test: contentItemModerationDetailPage.test.tsx — "should add only the like control to the newly wired pages")*

**2.16 [Must]** The card's tag and Bible reference sections show the post's associations as chips (rule 2.30), and the pill beside the type chip is off against the corner ribbon, so the same facts never show twice. The content stands whole, because a moderator rules on what is actually there. *(code: contentItemModerationDetailPage.tsx — the comment above the card; test: contentItemModerationDetailPage.test.tsx — "should not repeat the status as a pill beside the ribbon"; user, 2026-10-11)* ≠ item 17

**2.17 [Must]** Each panel's settings come from one read: the defaults plus this item's own override. The card, the heading and `ContentItemSettingsPanel` resolve against that same collection, so they cannot disagree about which row is in force (§DOM6.4). *(code: contentItemModerationDetailPage.tsx — `useGetEffectiveSettingsFor`, the comment above the settings wiring)* ≠ item 2

**2.18 [Should]** The heading and the tab title follow the effective setting: the item's title where `HasTitle` allows and one exists, and the content type's name otherwise. The breadcrumb reads *Admin* › *Posts* › the heading. *(code: contentItemModerationDetailPage.tsx — `heading`, `crumbs`; test: contentItemModerationDetailPage.test.tsx — "should render the item in the admin chrome with its breadcrumb")*

**2.19 [Should]** The submitter is named on the card from a second read of the contributor, rendered when it arrives rather than waited on. *(code: contentItemModerationDetailPage.tsx — `useGetContributorById`)*

**2.20 [Should]** While the item loads, a spinner stands in for the page. When the item cannot be read, the page says "We could not load this post right now. It may have been removed, or it may not be yours to moderate." and offers *Back to Posts*. *(test: contentItemModerationDetailPage.test.tsx — "should tell the reader honestly when the item cannot be read")*

**2.21 [Could]** *Back to Posts* always leads to the list it names, the queue, `/Admin/Posts`: as the moderator left it when they came from it, and the bare queue otherwise — never to another page an origin in router state names. *(user, 2026-09-27; test: contentItemModerationDetailPage.test.tsx — "should walk back to the bare queue when no origin was carried", "should walk back to the filtered queue a redirect carried in state")* ≠ item 13

**2.22 [Must]** Each tag and Bible reference's round is moderated on its association's page, `/Admin/Posts/{contentItemId}/Associations/{associationId}` (`UI/Pages/AssociationModerationDetailPage.md`), not on this one: the page renders no association panel, and offers a moderator no box for suggesting a tag or a Bible reference (`DesignFeatures/PostModeration.md` rules 1 and 9). *(user, 2026-09-27; user, 2026-10-11)* ≠ item 17

**2.23 [Must]** Every call about the item's round is addressed by the round's approval id, which the item carries: the verdict, `GET api/Approvals/{approvalId}/Verdict`; the decision, `PUT` or `POST api/Approvals/{approvalId}` with a body of the decision, whether a bypass is requested and the bypass reason, and nothing else; the reset, `POST api/Approvals/{approvalId}/Reset`; and the reviewer candidates, the reviewer display names, the review requests and Berean's resource. The reviews and the thread are addressed by it already. The page reads the id off the stored item (§ARC17.5, §ARC16.7). *(user, 2026-09-27)* ≠ item 11

**2.24 [Must]** `Administrators` may take the item down — a soft delete — at any status here, to clean up, for example, duplicates that went through approval. The control is the card's *Take down*, in its moderation footer beside Edit, which asks first and is wired to `onTakeDown` (`DesignFeatures/PostModeration.md` rule 10; `UI/Components/ContentItemPanel.md rule 2.47`); *Delete* stays in the editor as it is. The item's owner is never offered *Delete* on an `Approved` or `Rejected` item. *(user, 2026-09-27; user, 2026-10-11; §APR9.9 rules 2 and 7)* ≠ item 12

**2.25 [Must]** View is switched off: this page is the item's detail view in the admin section, where View on the queue (`UI/Pages/ContentItemModerationPage.md rule 2.3`) and Moderate on the other lists (rule 2.1) lead, so the card does not offer View. *(user, 2026-09-27: View and Edit are each "configurable via the Show* properties"; `UI/Components/ContentItemPanel.md rule 2.40`)* ≠ item 14

**2.26 [Must]** A click on the card's type chip, *Submitted by* or *Author* raises its hook, and the page opens the queue, `/Admin/Posts`, handed the value: the value in its query string, its search bar showing it in the matching box with the advanced section expanded (`UI/Pages/ContentItemModerationPage.md rules 2.12 and 2.13`). A tag or Bible reference chip on the card leads to its association's page instead (rule 2.31). *(user, 2026-09-27; user, 2026-10-11)* ≠ items 5 and 17

**2.27 [Must]** The reviewer candidates are read when `ReviewPanel`'s request picker opens, on the hook it raises then, and not before (`UI/Components/ReviewPanel.md rule 2.32`). *(user, 2026-09-27)* ≠ item 6

**2.28 [Must]** The card and the settings panel are rendered only once the item's setting has loaded: until the effective settings read lands, the page holds them back and shows its spinner, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in their place — never the card or the settings panel without the item's settings. *(user, 2026-09-27)* ≠ item 2

**2.29 [Must]** A change to the item's setting reaches the open page without a reload: an override written elsewhere, say, reshapes the card and the settings panel here (§ARC12.5.2 business rule 12). How the page learns of the change is §UI20.10's: the live connection makes the page's settings read stale, and the page reads it again, with no code of its own (`DesignFeatures/LiveUpdates.md` rule 1, #702). *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 16

**2.30 [Must]** The card shows every tag and Bible reference associated with the post, at every status, as chips: tags, then Bible references, each kind oldest first by the association's `CreatedWhen`, each with its status as `DesignFeatures/PostModeration.md` rule 6 shows it, and under the post's facet switches (rules 4–8 there). An association is the post's where one of its endpoints is a `ContentItem` whose effective id is the post's id or the post's group id, as `UI/Pages/AssociationModerationDetailPage.md rule 2.3` bounds an association to its post: one held across the post's versions is keyed by its group id, and one held to the version on show by its id (§DOM4.5 rule 1, §DOM4.6). The page reads the post's associations (`GET /api/associations`, #857) for both ids, names each tag and each reference from the tag and Bible reference reads (`DesignFeatures/UI/Foundations/TagService.md §2`, `DesignFeatures/UI/Foundations/BibleReferenceService.md §2`), and hands the chips to the card as `associationChipCollection` (`UI/Components/ContentItemPanel.md rule 2.45`). *(user, 2026-10-11)* ≠ item 17

**2.31 [Must]** A chip leads to its association's page, `/Admin/Posts/{contentItemId}/Associations/{associationId}`, from the card's `onAssociationChipClick` (`DesignFeatures/PostModeration.md` rule 7). *(user, 2026-10-11)* ≠ item 17

**2.32 [Must]** A moderator the association page's Edit sends here arrives with the editor open, as if they had taken Edit on this card (rule 2.7; `DesignFeatures/PostModeration.md` rule 11). Where rule 2.7's lock holds on the item, the page opens on the card as it always does, and the editor stays shut. *(user, 2026-10-11)* ≠ item 18

**2.33 [Must]** Until the approval process has created the item's approval record, every control that depends on the round's id is disabled: the page tells `ReviewPanel` and `ReviewCommentPanel` so (`isAwaitingApprovalRecord`; `UI/Components/ReviewPanel.md rule 2.55`, `UI/Components/ReviewCommentPanel.md rule 2.36`). The record is not there while the verdict read answers not-found for the item, and a later read of the round refresh (rule 2.12) that answers with the record enables the controls (`DesignFeatures/PostModeration.md` rule 17). On the route the page reads today, keyed by the item, the verdict read opens a missing round itself for a `Draft` or `Submitted` item, and answers not-found only for an item taken down or a decided item whose round was never recorded, which no read opens (§ARC16.7.2, *The write as built*): there the controls stay disabled. A round the approval process has yet to record reaches the page once it reads the round by its approval id (item 11), a read that opens no round (§ARC16.7.2). *(user, 2026-10-11)* ≠ item 19

**2.34 [Must]** When the read of the post's associations fails, or the tag read or the Bible reference read that names their chips, the page tells the card so (`isAssociationChipReadFailed`; `UI/Components/ContentItemPanel.md rule 2.48`), and the card shows an announced error with *Retry* in the chips' place: never no chips, and never one kind's chips without the other. *Retry* reads again each of those reads that failed, and the chips show once every one answers (`DesignFeatures/PostModeration.md` rule 19). *(user, 2026-10-11)* ≠ item 17

## 3. Layout

The admin shell puts its navigation menu in a left-hand column. Inside the page's own column, the
page has two columns: `col-lg-7` for what is judged and `col-lg-5` for who judges it. So there are
three columns at desktop width, or two when the menu is folded. Below the `lg` breakpoint
everything stacks: the menu, then the main column, then the right-hand column.

```text
+-------------------------------------------------------------------------------+
| site header                                                                   |
+-------------------------------------------------------------------------------+
| +-----------+ +-------------------------------------------------------------+ |
| | admin     | | [=] <heading>                     Admin > Posts > <heading> | |
| | menu      | | ----------------------------------------------------------- | |
| | (NavMenu) | |                                          [<- Back to Posts] | |
| |           | | +------------------------------+ +------------------------+ | |
| |           | | | ContentItemPanel             | | ReviewPanel            | | |
| |           | | |   (its tag and reference     | |   Approval Reviews     | | |
| |           | | |   chips on the card)         | |   Review Outcome       | | |
| |           | | |   or ContentItemEditPanel    | | ContentItemSettings... | | |
| |           | | | ReviewCommentPanel           | |                        | | |
| |           | | +------------------------------+ +------------------------+ | |
| +-----------+ +-------------------------------------------------------------+ |
+-------------------------------------------------------------------------------+
| site footer                                                                   |
+-------------------------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Left sidebar (the shell's) | `col-lg-3`; absent when the menu is folded | the admin navigation menu — not the page's |
| Page head | the shell's `col-lg-9`, or `col-12` folded | the shell's fold toggle; the heading and the `Breadcrumb`; a rule; *Back to Posts*. The spinner, or the error alert with *Back to Posts*, stands in for both columns below until the item is read. |
| Main | `col-lg-7` of the page's row | `ContentItemPanel`, with the post's association chips on it, or `ContentItemEditPanel` while editing; `ReviewCommentPanel`. As built, `TagAssociationPanel` and `BibleReferenceAssociationPanel` stand between the two (item 17). |
| Right sidebar | `col-lg-5` of the page's row | `ReviewPanel`; `ContentItemSettingsPanel` |

The two `ConfirmDialog`s are modal, and belong to the page (rule 2.14).

## 4. Components and their hooks

### 4.1 ContentItemPanel — the moderated card

Rendered while the editor is closed.

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `contentItem` | The item projected with its winning setting and the contributor's name and image, with `withReactions` laying on it its reaction summary, where one has been read, and the overlay of a choice the reader has just made (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §1 and §2`) | Rules 2.17 and 2.19. |
| `contentItemSettingCollection` | The settings read (`?? []`) | Rule 2.17. |
| `showModerationSection`, `moderationOpensEditor` | `true`, `true` | Rule 2.7. |
| `showApprovalStatusRibbon` / `showApprovalStatus` | `true` / `false` | Rule 2.16. |
| `showContentExpanded` | `true` | Rule 2.16. |
| `showTagSection`, `showBibleReferenceSection` | `true`, so the setting decides; `false` as built ≠ item 17 | Rules 2.16 and 2.30. |
| `associationChipCollection` ≠ item 17 | The post's associations, ordered and named (rule 2.30) | Rule 2.30. |
| `isAssociationChipReadFailed` ≠ item 17 | `true` while the associations read, the tag read or the Bible reference read has failed (rule 2.34) | Rule 2.34. |
| `reactionOptions`, `onReactionSelected` | The approved reactions, and the engagement hook's handler | Rule 2.15. |
| `onModerateClick` | Opens the editor | Rule 2.7. |
| `onAssociationChipClick`, `onTakeDown` ≠ items 17 and 12 | The two handlers below | Rules 2.31 and 2.24. |

`showEditSection` stays off on the card, so its one action is the moderation action rather than
the owner's Edit. The editor is `ContentItemEditPanel`, rendered by the page (section 4.2).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onModerateClick` | The moderation action, labelled *Edit*, is pressed while not locked | Swaps the card for `ContentItemEditPanel` in place (rule 2.7). A reviewer is also shown the action, and the editor then refuses them (`UI/Components/ContentItemPanel.Edit.md rule 3.2.2`). | ✅ Yes (`contentItemModerationDetailPage.tsx`, line 727) |
| `onEditClick` | The owner's Edit is pressed | Never offered: the moderated face removes it (`UI/Components/ContentItemPanel.md rule 3.1.6`). | *Not wired — switched off* |
| `onModified`, `onRemoved`, `onCancelled`, `onAdded` | The panel's own writing faces | — | *Never raised*: `showEditSection` is off on the card, and the page renders the editor itself (section 4.2) |
| `onReactionSelected` | The reader chooses a reaction | Records, changes or clears the reader's own reaction. | ✅ Yes (`src/hooks/useContentItemEngagement.ts`, lines 150-208 at 3a824fc6; #739) |
| `onShareClick`, `onSaveClick` | *Share* or *Save* is pressed | Not offered (rule 2.15). | *Not wired — switched off* (the comment above `useContentItemEngagement`) |
| `onTitleClick` | The title is pressed | The title is plain heading text: this page is the detail surface. | *Not wired — switched off* (`UI/Components/ContentItemPanel.md rule 3.1.10`) |
| `onReadMore`, `onExpandCollapse` | *read more* is pressed | Never offered: the content stands whole (rule 2.16). | *Not wired — switched off* |
| `onTagClick`, `onBibleReferenceClick` | A pill on the card is pressed | — | *Never raised*: the page hands the card association chips, which raise `onAssociationChipClick` instead (rule 2.30). As built the sections are off. |
| `onAssociationChipClick` ≠ item 17 | A chip is pressed | Opens the association's page, `/Admin/Posts/{contentItemId}/Associations/{associationId}` (rule 2.31). | ❌ No — item 17 |
| `onAssociationChipRetry` ≠ item 17 | *Retry* is pressed on the chips' error | Reads again each of the chips' reads that failed (rule 2.34). | ❌ No — item 17 |
| `onTakeDown` ≠ item 12 | *Delete* is pressed on *Take down*'s confirmation | `DELETE api/ContentItems/{contentItemId}`, then back to the queue; a failure is toasted and the moderator stays (rules 2.9 and 2.24). | ❌ No — item 12 |
| `onCommentsClick` | The comments control is pressed | — | *Not wired — switched off*: without it the control does not render (`UI/Components/ContentItemPanel.Default.md rule 2.9`); the review thread is `ReviewCommentPanel`, not an item's comments |
| `onContentTypeClick` | The type chip is pressed | Opens `/Admin/Posts` handed the type, its search bar showing it in the Category box with the advanced section expanded (rule 2.26). Today nothing: the chip is a button with no hook behind it. ≠ item 5 | ❌ No — item 5 |
| `onSubmittedByClick` | *Submitted by* is pressed; it renders here, from the contributor read | Opens `/Admin/Posts` handed the submitter, as the type chip (rule 2.26). Today nothing: the segment is a button with no hook behind it. ≠ item 5 | ❌ No — item 5 |
| `onAuthorClick` | *Author* is pressed, where the type has an author and the item carries one | Opens `/Admin/Posts` handed the author, as the type chip (rule 2.26). Today nothing: the segment is a button with no hook behind it. ≠ item 5 | ❌ No — item 5 |
| View's hook ≠ item 14 | View is pressed | — | *Not wired — switched off*: this page is the item's detail view (rule 2.25). The card has no View yet (`UI/Components/ContentItemPanel.md §10 item 20`), and the page is to set View's switch off once it has one; item 14 |

### 4.2 ContentItemEditPanel — the moderator's editor

Rendered in the card's place while editing (rule 2.7).

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `contentItem` | The stored item as a form item | The row the edit is laid over (rule 2.8). |
| `contentItemSettingCollection` | The settings read (`?? []`) | The frozen tiles and the fallback. |
| `showEditSection` | `true` | The face's own surface switch; without it the face refuses (`UI/Components/ContentItemPanel.Edit.md rule 2.3`). |
| `showApprovalStatusRibbon` | `true` | The status stays in view while editing. |
| `validationIssues`, `isSubmitting` | The last refusal's field messages; the modify write's pending state | Rule 2.8. |

The page passes no `submittedByDisplayName`, although it holds the contributor's name (item 4).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onModified` ≠ item 15 | *Save* is pressed | `PUT api/ContentItems` with the stored row and the edit laid over it. It closes the editor on success, showing the new version where the save forked one, and reads a refusal back onto the form and toasts it (rule 2.8). | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 220 and 710), bar the forked version; item 15 |
| `onRemoved` ≠ item 12 | *Delete* is confirmed. It is offered to `Administrators`, and to the owner while the item is `Draft` or `Submitted`, on an item the editor is open for (`UI/Components/ContentItemPanel.Edit.md rules 3.2.4 and 3.2.5`) | `DELETE api/ContentItems/{contentItemId}`, then back to the queue. A failure is toasted and the moderator stays (rule 2.9). An administrator may take the item down at any status (rule 2.24). | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 202 and 711), where the editor opens; item 12 |
| `onCancelled` | *Cancel* is pressed, on the form or on a refusal | Clears the field messages and closes the editor. | ✅ Yes (`contentItemModerationDetailPage.tsx`, line 712) |

### 4.3 TagAssociationPanel and BibleReferenceAssociationPanel — as built, to be removed

The redesign removes both panels (rule 2.22): the post's tags and Bible references are chips on
the card (rule 2.30), each association's round is moderated on its own page, and no moderator is
offered a suggest box. Until item 17 removes them, the page renders both as on a reader's page.

**Properties the page sets:** `associationCollection` `[]`, since the page reads no
associations (§ARC17.4); `onAdd`, a toast saying that suggesting is coming soon;
`showBorder`; and `cssClass` `mt-4`. Everything else keeps the story's defaults.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` | A suggestion is committed | Toasts "Suggesting tags is coming soon." or "Suggesting bible references is coming soon.", and sends nothing. The panel goes with item 17. | *Not wired — the panel is removed (item 17)* |
| `chipOnClick` | A chip's label is pressed | — | *Never raised*: no chip renders while the lists are empty, and the panel is removed (item 17) |
| `onRemove`, `onReject`, `onApprove` | Remove, Reject or Approve is pressed | — | *Not wired — switched off*: `showModerationActions` is off, and an association is decided on its own page (rule 2.22) |
| `loginButtonOnClick` | The login prompt is pressed | — | *Never raised*: the route admits no signed-out reader, so the prompt does not render |

### 4.4 ReviewCommentPanel — the review thread

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `approvalId` ≠ item 11 | The item's own approval id (rule 2.23); today the verdict's, or `''` where there is no verdict | With no round there is no thread to post to, and the panel says so (`UI/Components/ReviewCommentPanel.md rule 2.17`). |
| `reviewComments`, `isLoading` | The round's thread and its loading state, from `useApprovalRound` | Rule 2.10. |
| `entityType`, `contentType` | `ContentItem`, and the content type's enum member name | They compose the resolve tier and the read-only roles; a renamed type keeps its role names. |
| `isSubmitting` | Any of the four comment writes pending | One click is one write. |
| `isAwaitingApprovalRecord` ≠ item 19 | `true` while the verdict read answers not-found (rule 2.33) | Rule 2.33. |
| `showBorder`, `cssClass` | `true`, `mt-4` | Spacing in the column. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSave` | *Save* on a non-blank box | `POST api/ApprovalComments`. On a refusal it toasts the reason and rethrows, so the box keeps the words (`UI/Components/ReviewCommentPanel.md rule 2.29`). Every write refreshes the thread and the verdict (rule 2.11). | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 549 and 781) |
| `onClear` | *Clear* is pressed | Nothing: the box clears itself, and the hook is a notification. | *Not wired — nothing to do* |
| `onModified` | *Save* on the edit face | `PUT api/ApprovalComments` with the stored row, only the words and the type moved. If the row has left the thread, it says the change was not saved. | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 564 and 782) |
| `onRemoveRequested` | *Delete* on the author's own row | Holds the row and asks the confirmation of rule 2.14. On OK it sends `DELETE api/ApprovalComments/{id}` with the reason *Withdrawn by the author*. | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 606 and 783) |
| `onRemoved` | *Delete*, only where nobody listens on `onRemoveRequested` | — | *Never raised*: the page listens on `onRemoveRequested` (`UI/Components/ReviewCommentPanel.md rule 2.27`) |
| `onResolvedChanged` | The settled tick on a question changes, either way | `POST api/ApprovalComments/{id}/Resolve?isResolved=`. | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 626 and 784) |
| `onLoadMore` | The foot of the thread comes into view | — | *Never raised*: the page reads the thread whole and passes no `hasMore` |

### 4.5 ReviewPanel — the approval round

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `entityType`, `contentType` | `ContentItem`, and the content type's enum member name | They compose the vote, decision and read-only tiers. |
| `entityOwnerId`, `approvalStatus` | The stored item's `createdBy` and status | The panel's own gates decide against the real row (rule 2.10). |
| `entityAmenderUserIds` ≠ item 21 | The stored item's `amenderUserIds` (§APR8.6.3) | The panel withholds the vote from whoever amended the post, as from its owner (`UI/Components/ReviewPanel.md rule 2.18`). |
| `approvalVerdict`, `approvalReviewCollection`, `requestedReviewerCollection`, `reviewerCandidateCollection`, `isLoading` | From `useApprovalRound`, which reads the verdict, the candidates, the requests and the names by the item today (item 11) | Rules 2.10 and 2.23. |
| `aiReviewerCandidate`, `aiReviewerAssignment` | Derived from Berean's status read | Offers Berean, and gives it a row once assigned. |
| `isAwaitingApprovalRecord` ≠ item 19 | `true` while the verdict read answers not-found (rule 2.33) | Rule 2.33. |
| `showBorder` | `true` | — |

The page passes no `suggestedReviewerCollection` and no `isCandidatesLoading`, so Berean is the
only suggestion. It reads the candidates when the page loads, not when the picker opens (item 6).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onReviewStatusChanged` | The viewer casts or changes their vote | Files a first review, or amends the viewer's standing review. A dismissed review is not standing, so a new one is filed (`POST` / `PUT api/ApprovalReviews`). | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 439 and 824) |
| `onApprovalStatusChanged` ≠ item 11 | *Submit* on a decision | `PUT` or `POST api/Approvals/{approvalId}` with a body of the decision, whether a bypass is requested and its reason (rule 2.23). On success it toasts "The post has been approved." or "The post has been rejected.". | ✅ Yes, on the route keyed by the item, `POST api/Approvals/ContentItem/{id}/Decision?…` with a query string (`contentItemModerationDetailPage.tsx`, lines 456 and 825; `apiBroker.approvals.ts`, lines 146-147); item 11 |
| `onApprovalReset` ≠ item 11 | An administrator resets a decided round | `POST api/Approvals/{approvalId}/Reset` (rule 2.23), then toasts that the post is back with the reviewers. | ✅ Yes, on the route keyed by the item, `POST api/Approvals/ContentItem/{id}/Reset` (`contentItemModerationDetailPage.tsx`, lines 478 and 827; `apiBroker.approvals.ts`, line 119); item 11 |
| `onReviewerLookupRequested` | The request picker opens | Reads the reviewer candidates (rule 2.27). ≠ item 6 | ❌ No — the page does not wire it, and reads the candidates with the round when the page loads; item 6 |
| `onReviewRequested` ≠ item 11 | A person is picked under Suggestions or Everyone else | Asks them to review, on the review-request route addressed by the approval id (rule 2.23). | ✅ Yes, on the route keyed by the item, `POST api/Approvals/ContentItem/{id}/ReviewRequests?requestedUserId=` (`contentItemModerationDetailPage.tsx`, lines 494 and 828; `apiBroker.approvals.ts`, line 160); item 11 |
| `onReviewRequestWithdrawn` ≠ item 11 | A person is picked under Requested | Withdraws the request, on the review-request route addressed by the approval id (rule 2.23). | ✅ Yes, on the route keyed by the item, `DELETE api/Approvals/ContentItem/{id}/ReviewRequests?requestedUserId=` (`contentItemModerationDetailPage.tsx`, lines 512 and 829; `apiBroker.approvals.ts`, line 175); item 11 |
| `onAIReviewerRequested` ≠ items 11 and 22 | Berean is picked | Asks Berean to review, on Berean's resource addressed by the approval id (rule 2.23). It refreshes Berean's status and the verdict (rule 2.11). | ✅ Yes, on the route keyed by the item, `POST api/AIReviewers/ContentItem/{id}` (`contentItemModerationDetailPage.tsx`, lines 386 and 833; `apiBroker.aiReviewers.ts`, line 41); item 11 |
| `onAIReviewerWithdrawn` ≠ items 11 and 23 | Berean is picked under Requested | Withdraws Berean, on its resource addressed by the approval id (rule 2.23), with no success toast. It refreshes Berean's status and the verdict (rule 2.11). | ✅ Yes, on the route keyed by the item, `DELETE api/AIReviewers/ContentItem/{id}` (`contentItemModerationDetailPage.tsx`, lines 407 and 835; `apiBroker.aiReviewers.ts`, line 59); item 11 |

### 4.6 ContentItemSettingsPanel — the settings in force

**Properties the page sets:** `contentItemId` and `contentType` from the stored item;
`contentItemSettingCollection`, the same settings read the card uses (rule 2.17); `isSubmitting`,
either settings write pending; `showBorder`; and `cssClass` `mt-4`. It passes no `isLoading` (item 2).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onModify` | *Modify* is taken | Nothing: the panel switches to its modify face itself, and the hook is a notification. | *Not wired — nothing to do* |
| `onReset` | *Reset* is taken on the modify face | Nothing: the panel reverts its draft itself. | *Not wired — nothing to do* |
| `onModified` | *Save settings* is taken | Saves the override: `POST api/contentitemsettings` for a new one, `PUT` for an existing one. Toasts "Content settings saved." or the server's reason. | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 260 and 853) |
| `onOverrideRemoved` | *Remove Override* is taken | Holds the row and asks the confirmation of rule 2.14. On OK it sends `DELETE api/contentitemsettings/{id}/Hard` and toasts "Content settings override removed." or the reason. | ✅ Yes (`contentItemModerationDetailPage.tsx`, lines 270 and 855) |

## 5. Security and access

The rows state the design (rules 2.1-2.5). Today the route admits `Administrators` alone, which is
item 1. General users and signed-out visitors never reach the page, so for them every action row is
`➖ n/a`. Owner here is the item's contributor, holding no tier. An owner who holds a tier is
counted in that tier's column, with the row's condition. What each panel offers each persona, and
the read-only roles each action answers to, are the components' own role matrices and security and
access matrices: `UI/Components/ContentItemPanel.md §5`, `UI/Components/ContentItemPanel.Edit.md §5`,
`UI/Components/ReviewCommentPanel.md §5`, `UI/Components/ReviewPanel.md §5` and
`UI/Components/ContentItemSettingsPanel.md §5`. The association chips are a read, offered to
everyone who reaches the page; each association's round is the association page's
(`UI/Pages/AssociationModerationDetailPage.md §5`).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — the item, the round, the thread and the settings, read ≠ item 1 | ❌ No¹ | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Item `Draft` or `Submitted`, no ReadOnly covering its type — **the moderation action opens the editor**; fields, *Save* and *Submit as* | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No² | ✅ Yes³ | ✅ Yes |
| Item `Approved` or `Rejected`, not the viewer's — **the moderation action**, rendered locked | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes⁴ | ✅ Yes | ✅ Yes |
| Item `Draft` or `Submitted` — ***Delete*** in the editor (a takedown) | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ✅ Yes |
| Any status — ***Take down*** on the card ≠ item 12 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ✅ Yes |
| **The association chips**, each leading to its association's page ≠ item 17 | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| **View** on the card ≠ item 14 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| Item `Submitted`, no ReadOnly the round composes — **a vote** in `ReviewPanel` | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Item `Submitted` — **the decision**, and the bypass where the verdict allows it | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes | ✅ Yes |
| Viewer owns the item and holds the column's tier — **a vote** | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| No `ReadOnly` or `ApprovalComment-ReadOnly` held — **the review comment box** ≠ `UI/Components/ReviewCommentPanel.md §10 item 16` | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer owns the item and holds the column's tier, no `ReadOnly` or `ApprovalComment-ReadOnly` held — **the review comment box** ≠ `UI/Components/ReviewCommentPanel.md §10 item 16` | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer also holds `ReadOnly` or `ApprovalComment-ReadOnly` — **the review comment box**, live, and Edit and Delete on their own comment ≠ `UI/Components/ReviewCommentPanel.md §10 item 16` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No⁵ | ❌ No⁵ | ❌ No⁵ |
| As the row above — **the review comment box rendered disabled**, with the block-role tooltip ≠ `UI/Components/ReviewCommentPanel.md §10 items 16 and 21` | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes⁷ | ✅ Yes⁷ | ✅ Yes⁷ |
| Viewer holds their tier only outside the item's review tier, holding neither block of `UI/Components/ReviewCommentPanel.md rule 3.2.2` — **the review comment box**, live, Edit and Delete on their own comment, and the settled tick on a question ≠ `UI/Components/ReviewCommentPanel.md §10 item 19` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No⁶ | ❌ No⁶ | ➖ n/a |
| As the row above — **the review comment box rendered disabled**, with the tooltip saying why ≠ `UI/Components/ReviewCommentPanel.md §10 items 19 and 21` | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes⁸ | ✅ Yes⁸ | ➖ n/a |
| No ReadOnly covering the item's type — **Modify** and **Remove Override** in `ContentItemSettingsPanel` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes³ | ✅ Yes |
| The setting allows reactions, item not `Rejected` — **Like** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, item `Rejected` — **Like** ≠ `UI/Components/ContentItemPanel.md §10 item 29` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |

¹ The route guard shows *Access Restricted* and a *Login* button.
² The design: a reviewer sees the item and does not modify it (rule 2.4). Today the action renders for them, labelled *Edit*, and the editor refuses them (`UI/Components/ContentItemPanel.Edit.md rule 3.2.2`).
³ The publisher tier for the item's content type: `Publishers`, `ContentItem-Publishers` or `ContentItem-{ContentType}-Publishers`.
⁴ Rendered, and disabled with its reason.
⁵ The thread composes these two roles itself; a read-only role of the item under review does not withhold the box (`UI/Components/ReviewCommentPanel.md rules 2.9 and 2.20`).
⁶ A reviewer or a publisher for another content type or entity only, who reaches the page (rule 2.1) but is not in this item's review tier (rule 2.3). `Administrators` are in every item's review tier.
⁷ Rendered, and disabled, not hidden: the text box and Save each carry the block-role tooltip, a property whose default is "User belongs to a block role", and nothing is raised (`UI/Components/ReviewCommentPanel.md rule 3.2.2`; user, 2026-09-27).
⁸ Rendered, and disabled, not hidden: the text box and Save each carry a tooltip saying why, a property whose default is "Only this item's reviewers can comment", and nothing is raised (`UI/Components/ReviewCommentPanel.md rule 3.2.6`; user, 2026-09-27).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — `/Admin/Posts/{id}`: a reviewer or a publisher cannot use it.** Every
   page that wires Moderate — `/`, `/posts`, `/myposts` and `/Admin/Posts` (`home.tsx`, `posts.tsx`,
   `myPosts.tsx`, `admin/contentItemModerationPage.tsx`, each `moderateContentItem`) — routes it to
   this page. The card offers Moderate to the review tier, the publisher tier and `Administrators`
   (`UI/Components/ContentItemPanel.md rules 2.19, 3.1.5 and 3.2.2`). This page admits `Administrators`
   only (`adminRoutes.tsx` — `Admin/Posts/:contentItemId`; `securityMatrix.tsx` — `contentItems.view`),
   so a reviewer or a publisher who is not an administrator is shown *Invalid Access*. The user ruled
   on 2026-09-26 that reviewers reach the page, see the item without modifying it, and see
   `ReviewPanel` on the right-hand side to place their review. The user also ruled that Moderate is the
   way every role but the owner's reaches the item, and this page the place it may lead, where the
   item's other moderation tasks are performed. On 2026-09-27 the user confirmed that publishers define
   an item's override settings here (`UI/Components/ContentItemSettingsPanel.md rule 2.11`).
   `SecuredRoute` takes a fixed list of role names, and the review and publisher tiers are
   suffix-matched per content type (§SEC18.6), which the route comment says such a list cannot
   express. Once they reach the page, a reviewer is still offered the moderation action labelled
   *Edit*, and the editor refuses them (`UI/Components/ContentItemPanel.Edit.md rule 3.2.2`);
   `contentItemPanel.tsx`'s own comment above `moderateButtonLabel` calls that role half "not
   aligned", and not reachable today only because of this page's gate. The route
   comment in `adminRoutes.tsx` (lines 91-94 at 70dc72e7) assigns "widening who reaches this surface"
   to #361, which is closed: the widening is this item's own work, and the route comment goes
   with it. The association page, `/Admin/Posts/{contentItemId}/Associations/{associationId}`,
   takes the same security point and widens with this page (user ruling 2026-10-11;
   `UI/Pages/AssociationModerationDetailPage.md rule 2.2`), so the widening covers its route too.
   Copied from `UI/Components/ContentItemPanel.md §10 item 9` and
   `UI/Components/ContentItemListPanel.md §10 item 5`. `UI/Components/ReviewPanel.md §10 item 7` and
   `UI/Components/ContentItemSettingsPanel.md §10 item 6` point here too.
2. (needs issue) **Page gap — `/Admin/Posts/{id}`: the panels render before the settings read
   lands.** Rule 2.28 (user rulings 2026-09-27): the card and the settings panel are not rendered
   until the item's setting has loaded, and the page shows its spinner meanwhile; if the settings
   read fails, the page shows its error, announced, with a Retry, in their place. The page renders
   the card once the item read has landed, without waiting for its effective-settings read. While that read is in flight, or if it fails, the element carries no
   setting, although a setting always applies (`UI/Components/ContentItemPanel.md rule 2.39`). The
   same read feeds `ContentItemSettingsPanel` without its loading state, so that panel says "No content
   settings apply to this item yet." until the read lands. The page's error answers the item read
   alone (rule 2.20), so a failed settings read still shows the card and the panel. Evidence:
   `admin/contentItemModerationDetailPage.tsx` — `contentItemSettings ?? []` (lines 847-850 at
   70dc72e7 for the settings panel). Copied from `UI/Components/ContentItemPanel.md §10 item 15`, this
   page's share of it. `UI/Components/ContentItemSettingsPanel.md §10 item 5` points to it.
3. Built (#747) **`/Admin/Posts/{id}`: the page's Like control is wired.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share of it. The page hands
   `useContentItemEngagement` the id of its one card and renders the card from what `withReactions`
   projects, so the card, where its item has a reaction summary, shows the reactions it has been
   given and marks the reader's own (`src/pages/admin/contentItemModerationDetailPage.tsx`, the hook
   at lines 131-132, the card at lines 724-736, at 2e532447;
   `DesignFeatures/UI/Hooks/ContentItemEngagement.md §1`; `DesignFeatures/Likes.md` rules 5, 6 and
   11a; #747). A reaction the reader chooses is recorded, changed or cleared by the same hook
   (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §2`; #739), and no signed-out reader chooses
   one here, because `SecuredRoute` admits none.
   A rejected post is the exception: the write refuses a reaction on it (§ARC12.3.1 rule 5a), and
   the card withholds Like from it, under `UI/Components/ContentItemPanel.md` rule 3.1.8, whose
   build its §10 item 29 records (`DesignFeatures/RejectedEndpoints.md` rule 9).
4. (needs issue) **Page gap — `/Admin/Posts/{id}`: the moderator's editor prefills the moderator's
   name.** The editor's owned-basis prefill takes the submitter's name from `submittedByDisplayName`,
   not the editor's (`UI/Components/ContentItemPanel.Edit.md rule 2.15`). Without it, the form falls
   back to the signed-in reader's own name (`contentItemFormPanel.tsx` — `contributorDisplayName`).
   This page renders `ContentItemEditPanel` without `submittedByDisplayName`, although it already
   holds the contributor's name (`contributor?.displayName`, read for the card). So a moderator who
   moves an item with no author to an owned basis has their own name offered as its author.
5. (needs issue) **Page gap — `/Admin/Posts/{id}`: the type chip, *Submitted by* and *Author* do
   not open the queue.** Rule 2.26 (user ruling 2026-09-27): in the
   admin section each click raises its hook, and the page opens `/Admin/Posts` handed the value,
   its search bar showing it with the advanced section expanded. The card renders the type chip,
   *Submitted by* (from the contributor read) and *Author*, where the type has one, as buttons
   raising `onContentTypeClick`, `onSubmittedByClick` and `onAuthorClick`
   (`UI/Components/ContentItemPanel.Default.md rules 3.1.1 and 3.1.4`). This page wires none of
   them, so all three are dead actions (§UI20.6.6 rule 4). The card's tag and Bible reference chips,
   which lead to their association's page instead (rule 2.31), are item 17. The queue's half — its
   query string, and opening its advanced section — is `UI/Pages/ContentItemModerationPage.md §6
   item 6`. The same gap on the association page is
   `UI/Pages/AssociationModerationDetailPage.md §6 item 23`.
6. (needs issue) **Page gap — `/Admin/Posts/{id}`: the reviewer candidates are read when the page
   loads.** Rule 2.27 (user ruling 2026-09-27): the candidates are read when the request picker
   opens (`UI/Components/ReviewPanel.md rule 2.32`), on the `onReviewerLookupRequested` the picker
   raises then. This page does not wire that hook. It reads the candidates when the page loads,
   alongside the round (`src/hooks/useApprovalRound.ts` — `useGetReviewerCandidates`), and leaves
   them out of the refresh. The candidates read is a user-enumeration surface (§ARC16.7.4). The
   page's half is to read them on the hook.
7. (needs issue) **Stale code comments.** The page's header comment says "the review panel, the
   decision controls, the association verdicts — is #350's work; until it lands this page is the
   item in the admin shell". The review panel and the decision controls are on the page. #350, *Add
   The Freshness Channel To The Review Panel*, is closed. The association verdicts are the association page's (`UI/Pages/AssociationModerationDetailPage.md`). The
   comment above `useContentItemEngagement` gives Share's absence as "an item under moderation is
   by definition not approved" (lines 120-124), but the page opens on approved items too: Moderate
   leads here from `/` and `/posts`, whose rows are all approved, and the queue reads every status
   (rule 2.15). The comment above `ContentItemEditPanel`, which says `mode="edit"` is "refused back
   to read", is `UI/Components/ContentItemPanel.Edit.md §10 item 4`.
8. **Note — what belonged to #698, settled.** The redesign's questions were cited here rather
   than asked: where each association's round appears, and what a moderator can do on it
   (`UI/Components/AssociationPanel.md §10 item 10`); whether a moderator is offered the suggest
   box, and in what words (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 3`);
   and what the page puts first. The product owner settled them on 2026-10-11 in the design
   session of #705 (`DesignFeatures/PostModeration.md`): each association's round has a page of
   its own, offering everything `ReviewPanel` offers, its thread and its takedown (rules 1 and
   14 there); no moderator is offered the suggest box (rule 9 there); and the post leads, its
   card first, with its tag chips and then its Bible reference chips on it, in a fixed order, not
   ordered by what still needs a decision (rules 3 and 5 there). Two of #698's questions were
   settled before. Which server call decides a suggestion was settled on 2026-09-27: a suggestion
   is an association with its own approval round, decided through the same approval routes as the
   post, addressed by the association's own approval id (#698, #699;
   `UI/Components/AssociationPanel.md §10 item 9`). Where a submitter sees and answers the review
   thread is settled by the user's ruling of the same day (rules 2.2 and 2.3): the thread belongs
   to the admin area and is offered to no general user. That also answers
   `UI/Components/ReviewCommentPanel.md §10 item 6`.
9. **Retired.** The side panels' facet switches — the redesign under #705 removes both panels (rule 2.22); the card's chips follow the post's facet switches (rule 2.30).
10. **Retired.** The side panels and the post's content type — the redesign under #705 removes both panels (rule 2.22), so neither is handed anything.
11. (needs issue) **Page gap — `/Admin/Posts/{id}`: the round is addressed by the item, not by its
    approval id.** Rule 2.23 (user rulings 2026-09-27; #699): every call about the round is
    addressed by the round's approval id, which every item that implements `IApproval` carries,
    and the decision goes in a body of three values — the decision, whether a bypass is requested, and the bypass
    reason. The page asks for the round by the item, `useApprovalRound('ContentItem',
    contentItemId)` (line 308), and every call it makes about the round but the reviews and the
    thread is keyed by the item: the verdict (`apiBroker.approvals.ts`, line 37), the reviewer
    candidates (line 58), the review requests (lines 68, 160 and 175), the reviewer display names
    (line 81), the reset (line 119), the decision, sent as a query string (lines 146-147), and
    Berean's resource (`apiBroker.aiReviewers.ts`, lines 30, 41 and 59). It takes the approval id
    for the review thread off the verdict (line 772), and two comments say only the verdict knows
    it — the one above `ReviewCommentPanel` (lines 757-758) and `useApprovalRound`'s header
    (`src/hooks/useApprovalRound.ts`, lines 32-33); both go with the change, since the item now
    carries the id. The server half — the id on every item, and the routes — is #699's design and
    is not built (§ARC17.5); the page's half is to read the id off the stored item and address every
    call about the round by it, once the routes are served. The components' halves are
    `UI/Components/ReviewPanel.md §10 item 12` and `UI/Components/ReviewCommentPanel.md §10 item 18`.
    The association page's share of the same move is
    `UI/Pages/AssociationModerationDetailPage.md §6 item 21`.
12. (#1052) **Page gap — `/Admin/Posts/{id}`: an administrator cannot take down a reviewed
    item.** Rule 2.24 (user rulings 2026-09-27 and 2026-10-11; §APR9.9 rule 7): `Administrators`
    take the item down from the card's *Take down*, at any status, after its confirmation, and the
    page sends `DELETE api/ContentItems/{contentItemId}` and returns to the queue (rule 2.9). The
    page offers the takedown only as *Delete* in the editor (section 4.2), and on a decided item
    the administrator did not contribute the moderation action is locked and the editor never
    opens (rule 2.7; `UI/Components/ContentItemPanel.Edit.md rule 3.2.5`), so an administrator can
    take down a `Draft` or `Submitted` item, or their own reviewed one, and no other reviewed
    item. The card has no *Take down* yet (`UI/Components/ContentItemPanel.md §10 item 26`); the
    page's half is to wire `onTakeDown`.
13. (needs issue) **Page gap — `/Admin/Posts/{id}`: *Back to Posts* follows any origin.** Rule
    2.21 (user ruling 2026-09-27): the link always leads to the list it names, the queue,
    `/Admin/Posts`. It leads to whatever origin a redirect carried in router state, and to the bare
    queue only when none was carried (`admin/contentItemModerationDetailPage.tsx`, line 179).
    Moderate leads here from `/`, `/posts` and `/myposts` too, each carrying its own path and query
    as `from` (`UI/Pages/Home.md rule 2.11`, `UI/Pages/Posts.md rule 2.10`, `UI/Pages/MyPosts.md
    rule 2.8`), so *Back to Posts* then leads back to the public page the moderator came from. The
    page's half is to follow an origin only when it is the queue.
14. (needs issue) **Page gap — View is to be switched off here.** Rule 2.25: this page is the item's
    detail view in the admin section, so the page switches View off. The card has no View yet, and
    no switch for it (`UI/Components/ContentItemPanel.md §10 item 20`); once it has, View would
    render here unless the page sets its switch off. The page's half ships with that item: set
    View's switch off. The same gap on `/posts/{id}` is `UI/Pages/PostDetail.md §6 item 13`.
15. (needs issue) **Page gap — `/Admin/Posts/{id}`: an amended reviewed item stays on the old
    version.** Rule 2.8: where the save forks a new version — a moderator amending their own
    `Approved` or `Rejected` item — the card shows that new version, so the amendment appears
    (`UI/Components/ContentItemPanel.md rule 2.11`). The fork is written under a new id
    (`ContentItemProcessingService.cs` — `shouldForkNewVersion`, line 439, and the new version's
    `Id`, line 888), while the page reads no response from the write and does not navigate
    (`saveChangesAsync`, line 220): it re-reads the id in its URL, which is the reviewed version,
    unchanged. The same gap on `/myposts/{id}` is `UI/Pages/MyPostDetail.md §6 item 14`.
16. (#910) **Page gap — `/Admin/Posts/{id}`: a changed setting does not reach the open page.**
    Rule 2.29 (user rulings 2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an
    open page: the page reads its settings through
    `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does, and its own settings writes
    invalidate them, so an override another moderator writes reaches it only on the query library's
    own triggers or on a reload; the page's 15-second round refresh (rule 2.12) does not re-read
    the settings. The live connection was designed under #702 (`DesignFeatures/LiveUpdates.md`,
    user rulings 2026-09-27 and 2026-10-06): this page's share — hearing of a change to the setting
    that governs its item, and updating what it shows — needs no code of its own (§UI20.10 rule 2),
    and is built when `Root` opens the connection (§UI20.10 item 1), the task this item carries.
    An open settings form is not lost to a re-read that leaves the same row in force: the form's
    draft is keyed on the winning row (`contentItemSettingsPanel.tsx`, the `key` on
    `ContentItemSettingsModifyPanel`), so only a different winning row, an override added or
    removed elsewhere, starts the form again from the new row. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
17. (#1051) **Page gap — `/Admin/Posts/{id}`: the post's tags and Bible references are not
    chips on its card.** Rules 2.6, 2.16, 2.22, 2.26, 2.30, 2.31 and 2.34 (user rulings
    2026-10-11; `DesignFeatures/PostModeration.md` rules 1, 3–9 and 19): the card shows every tag
    and Bible reference of the post as a chip showing its association's status, in a fixed order,
    each leading to its association's page, or an announced error with *Retry* in their place
    where they cannot be read, and the page renders no association panel. The page
    renders `TagAssociationPanel` and `BibleReferenceAssociationPanel` below the card, each handed
    an empty collection and a "coming soon" suggest box, and turns the card's tag and reference
    sections off (`admin/contentItemModerationDetailPage.tsx` lines 738-748 at 70dc72e7, the two
    panel elements, and the card's `showTagSection` and `showBibleReferenceSection`). Nothing reads
    the post's associations: the read is #857's (`GET /api/associations`), and the tag and Bible
    reference reads are `DesignFeatures/UI/Foundations/TagService.md §2` and
    `DesignFeatures/UI/Foundations/BibleReferenceService.md §2`. The card's halves are
    `UI/Components/ContentItemPanel.md §10 items 25 and 28`. The tests that stand the two panels in the
    column — "should stand both association surfaces below the item in the seven", "should mount
    the thread under the bible references, not beside the round"
    (`contentItemModerationDetailPage.test.tsx`) — change with it.
18. (#1053) **Page gap — `/Admin/Posts/{id}`: the page cannot open on the editor.** Rule 2.32
    (user rulings 2026-10-11): a moderator the association page's Edit sends here arrives with the
    editor open, unless rule 2.7's lock holds. The page opens on the card whatever brought the
    moderator here: `isEditing` starts `false` and only `onModerateClick` sets it
    (`admin/contentItemModerationDetailPage.tsx` — `isEditing`). The association page's half is
    `UI/Pages/AssociationModerationDetailPage.md §6 item 4`.
19. (#1054) **Page gap — `/Admin/Posts/{id}`: the round's controls are live before its
    record exists.** Rule 2.33 (user ruling 2026-10-11): until the approval process has created the
    item's approval record, `ReviewPanel` and `ReviewCommentPanel` render their controls disabled.
    While the verdict read answers not-found, the page hands `ReviewPanel` no verdict and
    `ReviewCommentPanel` an empty `approvalId` (`admin/contentItemModerationDetailPage.tsx` — the
    `approvalVerdict?.approvalId ?? ''` handed to the thread; `src/hooks/useApprovalRound.ts`), so
    the vote, the request cog and the decision stay live with no round for their writes to land
    on. On today's route the verdict read answers not-found for a decided item whose round was
    never recorded, since it opens a missing round only for a `Draft` or `Submitted` item (rule
    2.33). The components' halves are `UI/Components/ReviewPanel.md §10 item 14` and
    `UI/Components/ReviewCommentPanel.md §10 item 23`.
20. **Retired.** What the card shows when the post's associations or their names cannot be read —
    the user ruled on 2026-10-11 (#705, rulings 18 and 19): rule 2.34.
21. (#1021) **Page gap — `/Admin/Posts/{id}`: `ReviewPanel` is not handed who amended the post.**
    `UI/Components/ReviewPanel.md rule 2.18` withholds the vote from whoever amended the item's
    content, as from its owner, and the panel learns who did from the `entityAmenderUserIds` the
    page hands it, off the stored item's own record (§APR8.6.3 rule 8). The page hands it
    `entityOwnerId` alone (`admin/contentItemModerationDetailPage.tsx` line 823 at b0614925), and
    the item's model carries no record yet (`UI/Models/ContentItem.md §1`). The panel's half is
    `UI/Components/ReviewPanel.md §10 item 2`, and the server refuses such a review whatever the
    panel offers (§APR8.6 HR-5).
22. (#1178) **Page gap — `/Admin/Posts/{id}`: asking Berean leaves the verdict stale.** Rule 2.11
    (user ruling 2026-10-13): once Berean holds a round until its review has completed (§APR8.5
    rule 8), asking Berean moves the verdict, so `onAIReviewerRequested` refreshes the verdict as well
    as Berean's status. Today it refreshes Berean's status alone (`aiReviewerService.ts`,
    `useAssignAIReviewer`'s `onSuccess`), so the decision menu stays live on a round the server now
    refuses, until the 15-second refresh (rule 2.12). The refresh is made in that hook, as the
    thread's writes refresh the verdict in theirs (`approvalCommentService.ts`): this page holds no
    query client. It is the hook's story, `DesignFeatures/UI/Foundations/AIReviewerService.BereanReview.md`
    §1, and is tested at the hook, since this page's tests mock the service whole. The hook is
    shared with the association page, so it also gives that page the refresh its rule 2.19 asks
    (`UI/Pages/AssociationModerationDetailPage.md` §6 item 12, #1065).
    The comment over the request, which says a content item is out of confidence scoring and
    Berean's vote unreachable here, is untrue since the owner's rulings of 2026-10-13 and 2026-10-14
    (`DesignFeatures/BereanReview.md` rules 2 and 5), and is rewritten with this item.
23. (#1179) **Page gap — `/Admin/Posts/{id}`: withdrawing Berean leaves the verdict stale.** Rule
    2.11 (user ruling 2026-10-13): withdrawing Berean lifts the pending block, so
    `onAIReviewerWithdrawn` refreshes the verdict as well as Berean's status. Today it refreshes
    Berean's status alone (`aiReviewerService.ts`, `useWithdrawAIReviewer`'s `onSuccess`). The
    refresh is made in that hook, as item 22's is (`DesignFeatures/UI/Foundations/AIReviewerService.BereanReview.md`
    §2), and gives the association page its refresh too
    (`UI/Pages/AssociationModerationDetailPage.md` §6 item 13, #1066).
24. (needs issue) **Page gap — `/Admin/Posts/{id}`: the post's confidence score and reason are not shown.**
    The owner ruled on 2026-10-13 that every approvable item carries a confidence score and a reason,
    and that where `BlockOnRejectApprovalScore` does not enforce them, *"the score and reason will just
    be visible warning on the UI"* (`DesignFeatures/BereanReview.md`, A4). The association page
    shows them in its confidence block (`DesignFeatures/PostModeration.md` rule 13); this page has no
    place for them, and none is designed. It needs a mockup and a design task before a task can be
    carved.
