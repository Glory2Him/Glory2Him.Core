# Post moderation
Epic: [INTENT.md](../../INTENT.md)
Inherits: §SEC14.7 postures A, A′ and D, §SEC18.6, §APR7.8, §APR7.9, §APR8.5, §APR8.6, §APR9.9, §ARC16.7, §ARC17.4, §ARC17.5, §UI20.5.1, §UI20.6.4, §UI20.6.6, §UI20.9.3
Mockups: [G2H Mockup](../Mockups/G2H%20Mockup/README.md) — `Admin Post Detail.dc.html` and `Admin Association Detail.dc.html`, at 8868c321
Design task: #705, which closes #698; the confidence suggestion, #863

A moderator reviews a post, and each tag and Bible reference suggested for it, on two admin
screens: the post's page, and a page for one of its associations. The post's chips lead from the
first to the second, and the second leads back.

## Problem

Every tag and Bible reference on a post is an association with an approval round of its own
(§APR7.4 item 6, §SEC14.7 posture A′), but the admin post page shows only the post's round. Its
tag and Bible reference panels are a reader's panels, with a reader's suggest box, and offer a
moderator no way to see an association's reviews, vote on it, decide it or take it down. The
product owner's mocks move the post's tags and Bible references onto its card as chips that show
where each one's round stands, and give each association a page of its own for its round.

## Business rules

The product owner's rulings are on #705: the design session of 2026-10-11 (rulings 1–11,
https://github.com/Glory2Him/Glory2Him.Core/issues/705#issuecomment-6109616859) and its
continuation the same day (rulings 12–17,
https://github.com/Glory2Him/Glory2Him.Core/issues/705#issuecomment-6109747616), and the rulings on
failed reads the same day (rulings 18–21,
https://github.com/Glory2Him/Glory2Him.Core/issues/705#issuecomment-6112986595), and the rulings on
the association page's read of the post's settings the same day (rulings 22–23,
https://github.com/Glory2Him/Glory2Him.Core/issues/705#issuecomment-6113417041), and the rulings on
the association page's breadcrumb the same day (rulings 24–26,
https://github.com/Glory2Him/Glory2Him.Core/issues/705#issuecomment-6113739849). The two rulings
on the confidence suggestion were made on 2026-10-13, under #863, and §APR8.6.2, *The confidence
suggestion*, rules 3–5 record them. Each rule below names its source.

1. **Two screens.** The post page, `/Admin/Posts/{contentItemId}`
   (`Design/UI/Pages/ContentItemModerationDetailPage.md`), and the association page, nested under
   it, `/Admin/Posts/{contentItemId}/Associations/{associationId}`
   (`Design/UI/Pages/AssociationModerationDetailPage.md`). Each tag and Bible reference's round is
   moderated on its association's page, not on the post page. *(ruling 1; #698 point 1)*
2. **Who reaches the association page.** Whoever reaches the post page: the same security point,
   `securityPoints.contentItems.view`, which admits `Administrators` today, and which widens when
   the post page's does (`Design/UI/Pages/ContentItemModerationDetailPage.md §6 item 1`).
   *(ruling 2)*
3. **The post leads.** On both pages the post's card comes first, with its tag chips and then its
   Bible reference chips on it. *(ruling 10; #698 point 5)*
4. **Which chips show.** Every tag and every Bible reference associated with the post, at every
   status — `Draft`, `Submitted`, `Approved` and `Rejected` — each as one chip. An association that
   has been taken down is not shown, because the association read does not answer it (§SEC14.5
   rule 3). *(ruling 11)*
5. **Chip order.** Tags, then Bible references. Within each kind, the oldest association first, by
   its `CreatedWhen`. *(ruling 10)*
6. **What a chip shows.** Its tag (`#` and the tag's name) or its Bible reference (the reference
   as stored), its status icon, a dashed border on a `Draft` association, its label struck
   through on a `Rejected` one, and a tooltip naming its status followed by "click to review".
   The four status icons are the mock's: `Approved` `bi-check-circle-fill`, `Submitted`
   `bi-hourglass-split`, `Draft` `bi-pencil-fill`, `Rejected` `bi-x-circle-fill`. On the
   association page the chip of the association on show is ringed. *(ruling 11; the mocks'
   `STATUS` table and `chips` function)*
7. **Where a chip leads.** Every chip leads to its association's page. On the association page a
   chip leads to that association's page, and the ringed chip, the association already on show,
   leads nowhere. *(ruling 1; the mocks: every chip's `href` is `Admin Association Detail.dc.html`,
   and `switchTo` returns on the current chip)*
8. **The chips follow the post's facet switches.** The tag chips show only where the post's
   effective `ShowTags` is on, and the Bible reference chips only where its `ShowBibleReferences`
   is on, as the card's tag and reference sections already do (`Design/UI/Components/ContentItemPanel.md
   rule 2.29`).
9. **No suggest box for moderators.** Neither page offers a box for suggesting a tag or a Bible
   reference. *(#698 point 4: 8868c321 removed both boxes from the post page, and neither mock
   binds an element to its suggest state)*
10. **The post's takedown.** `Administrators` take the post down — a soft delete — from a *Take
    down* action in the card's moderation footer, beside Edit, at any status, on both pages. It
    asks first: title *Are you sure?*, message "This removes the contribution. It cannot be undone
    from here.", button *Delete*; nothing is sent on Cancel. *Delete* stays in the editor as it is.
    *(rulings 6, 14 and 17; §APR9.9 rule 7)*
11. **The post card on the association page.** It offers the Like control, as Likes rule 11 has
    every page that renders the card (`DesignFeatures/Likes.md` rule 11), and Share and Save
    deliberately not, for the post page's reason
    (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.15`). Its Edit leads to the post
    page with the editor open, and is greyed out as locked under the condition, and in the words,
    it is on the post page (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.7`).
    *(rulings 4, 7 and 16)*
12. **What the association page shows.** The post's card; the association's card — whether it is a
    tag or a Bible reference, the tag or the reference, the reference's scripture where its row
    carries one, who suggested it and when, and its status; the association's confidence block
    (rule 13); the association's review thread; its reviews and its *Review Outcome*; and *Back to
    Posts* and *Back to Post*. *(#705 criterion 2; the mock)*
13. **The confidence block.** It applies only where the association records both a confidence score
    and a confidence reason (§APR9.7.1 rule 5). It then shows Berean as the reviewer, the score, the
    reason, and the suggestion — an approval, a rejection, or that it is unable to suggest either —
    which §APR8.6.2 decides against the round's resolved `AIApprovalConfidenceApprovalThreshold`
    and `AIApprovalConfidenceRejectionThreshold`. Where either the score or the reason is null the
    block is not rendered, and nothing about it affects the round: the confidence feature may be
    switched off, and the rest of the process must not depend on it. The suggestion is
    §APR8.6.2's confidence suggestion, carried on Berean's status read, and when it is answered is
    §APR8.6.2's (*The confidence suggestion* rules 3–5). *(ruling 5; §APR8.6.2 records the user
    rulings of 2026-10-13, under #863)*
14. **What a moderator can do on an association.** Everything `ReviewPanel` offers for its round —
    see the reviews, cast a vote, request reviewers and Berean, decide it with its bypass (the
    publisher tier and `Administrators`), and reset a decided round (`Administrators`) — the
    association's own review thread, and the association's takedown (rule 16). *(ruling 8)*
15. **An association's round composes its tiers from its two endpoints.** On the association page,
    `ReviewPanel` and `ReviewCommentPanel` compose every tier from the association's two endpoints,
    not from `Association`: a scoped role matching either endpoint admits, at either tier, and a
    block on either endpoint bars (§SEC14.7 posture A′ rules 1–3). An association has no roles of
    its own (§SEC18.6). *(ruling 2)*
16. **The association's takedown.** `Administrators` take the association down from a *Take down*
    action in the association card's footer, at any status. It asks first: title *Are you sure?*,
    a message naming the tag or the reference — "This takes #grace down from this post. It cannot
    be undone from here." — and button *Delete*. On success the moderator returns to the post page;
    a failure keeps them on the association page with the reason toasted. *(rulings 9 and 17)*
17. **The round's id comes from the approval record.** The approval process creates an item's
    approval record through eventing, and the page takes the round's approval id from that record.
    Until the record exists, every control that depends on the id is disabled. In the product
    owner's words: *"Via eventing the approval process will create an item where we can get the id
    for the approval. For all components that depend on this Id, we can disable the controls till
    the process has created the approval record."* This holds on both pages. *(ruling 12)*
18. **An association that is not the post's.** Where the association in the address is not one of
    that post's, or is neither a tag nor a Bible reference, the association page shows its
    could-not-load state with *Back to Posts*, as the post page does for a post it cannot read
    (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.20`). It never shows an association
    under another post. *(ruling 15)*
19. **The chips cannot be read.** Where the read of the post's associations fails, or the read of
    their tags or of their Bible references, an announced error with a Retry takes the chips' place
    on the card, on both pages, as a failed settings read does on the post page
    (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.28`). The card never shows no chips
    in their place, nor one kind's chips without the other. *(rulings 18 and 19)*
20. **The association's tag or Bible reference cannot be read.** The association page shows its
    could-not-load state with *Back to Posts*, as for an association it cannot read (rule 18).
    *(ruling 20)*
21. **The association's review thread cannot be read.** An announced error with a Retry takes the
    thread's place on the association page. It is never shown as an empty thread. *(ruling 21)*
22. **The post's settings are loading (association page).** What depends on the post's settings
    waits until they have loaded, and the rest renders once it has what it needs. In the product
    owner's words: *"anything that is dependent on the settings should wait till the settings are
    loaded. other things may render as long as it has what's needed to render properly."*
    *(ruling 22)*
23. **The post's settings cannot be read (association page).** An announced error with a Retry
    takes the post card's place, as on the post page
    (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.28`), reading "We could not load
    this post's settings right now.". The association can still be reviewed. *(rulings 23 and 26)*
24. **The breadcrumb's crumb for the post (association page).** The breadcrumb renders at once,
    without the post's crumb while the post's settings load; the crumb appears once they arrive.
    If the settings read fails, the crumb reads *Post* and still links to the post page.
    *(rulings 24 and 25)*

## User stories

Each screen is a user story, recorded in its page document (§UI20.5.1):

- [Design/UI/Pages/ContentItemModerationDetailPage.md](../Design/UI/Pages/ContentItemModerationDetailPage.md) — page, `/Admin/Posts/{contentItemId}`, redesigned
- [Design/UI/Pages/AssociationModerationDetailPage.md](../Design/UI/Pages/AssociationModerationDetailPage.md) — page, `/Admin/Posts/{contentItemId}/Associations/{associationId}`, new

The reads of a tag and a Bible reference that the chips and the association card need (ruling
13):

- [UI/Brokers/TagBroker.md](UI/Brokers/TagBroker.md) — broker, new
- [UI/Foundations/TagService.md](UI/Foundations/TagService.md) — foundation service, new
- [UI/Brokers/BibleReferenceBroker.md](UI/Brokers/BibleReferenceBroker.md) — broker, new
- [UI/Foundations/BibleReferenceService.md](UI/Foundations/BibleReferenceService.md) — foundation service, new

The confidence suggestion (rule 13; §APR8.6.2, *The confidence suggestion*), designed under #863,
from where the policy is resolved up to the read the association page makes:

- [Backend/Clients/AccessClient.PostModeration.md](Backend/Clients/AccessClient.PostModeration.md) — client, the decision; a second document beside the amender record's
- [Backend/Brokers/AccessBroker.PostModeration.md](Backend/Brokers/AccessBroker.PostModeration.md) — broker, the gather; a third document beside the Likes and amender ones
- [Backend/Orchestrations/AIReviewerOrchestrationService.md](Backend/Orchestrations/AIReviewerOrchestrationService.md) — orchestration, Berean's status read, existing
- [Backend/Controllers/AIReviewersController.md](Backend/Controllers/AIReviewersController.md) — exposer, existing, unchanged in code
- [UI/Brokers/AIReviewerBroker.PostModeration.md](UI/Brokers/AIReviewerBroker.PostModeration.md) — broker, existing; a second document beside `BrokersHoldNoLogic.md`'s
- [UI/Foundations/AIReviewerService.md](UI/Foundations/AIReviewerService.md) — foundation service, existing

The presentation components the two pages render are features of their own under §UI20.6.4, and
this feature's work on each is a gap of its section 10:

| Component | What this feature changes |
| --- | --- |
| [ContentItemPanel.md](../Design/UI/Components/ContentItemPanel.md), with [ContentItemPanel.Default.md](../Design/UI/Components/ContentItemPanel.Default.md) | The association chips on the card (rules 4–8), the error that takes their place when they cannot be read (rule 19), and the post's *Take down* in its moderation footer (rule 10) |
| [ReviewPanel.md](../Design/UI/Components/ReviewPanel.md) | Its tiers composed from an association's two endpoints (rule 15), and its controls disabled until the round's record exists (rule 17) |
| [ReviewCommentPanel.md](../Design/UI/Components/ReviewCommentPanel.md) | The same two changes |
| [AssociationDetailPanel.md](../Design/UI/Components/AssociationDetailPanel.md), new | The association's card, its confidence block and its takedown (rules 12, 13 and 16) |
| [AssociationPanel.md](../Design/UI/Components/AssociationPanel.md) and its two stories | No change to the components: the post page stops rendering them (rule 1) |

**Layer placement.** Nearly all of it is presentation and the React app's reads: two pages, the
components they render, and a broker and a foundation service each for tags and Bible
references, which call routes the server already serves (`TagsController`,
`BibleReferencesController`). Nothing among them sits above a foundation service.

The confidence suggestion is the one part on the server, and it adds no service, no layer and no
route:

- **The page is handed the band, and compares nothing.** The panel never works out the suggestion
  (`Design/UI/Components/AssociationDetailPanel.md` rule 2.5), and neither does the page: the
  thresholds of every tier are already readable by every signed-in caller through
  `GET api/ApprovalSettings` (§SEC14.7 posture C rule 2), but resolving §APR8.4's tiers over them
  in the React app would put most-specific-wins in a second place, which §APR8.6.1 rule 4 exists
  to prevent (*"The broker returns a verdict, not settings"*). Handing the page the two resolved
  thresholds would leave the comparison of rules 1–3 to it all the same.
- **The decision is the security client's**, `IAccessClient`, which decides every policy question
  (§APR8.6.1 rules 2 and 4), and `IAccessBroker` gathers for it. It is a decision of its own,
  not a widening of `AIReviewerPolicyVerdict`: who is answered it depends on the caller
  (§APR8.6.2 suggestion rule 6), and that verdict is resolved under the roleless system identity
  by the automatic assignment (§APR8.6.2.1 gate 4).
- **Berean's status read carries it**, `AIReviewerOrchestrationService.RetrieveAIReviewerStatusAsync`
  on `GET api/AIReviewers/{entityType}/{entityId}`, an orchestration because it already spans
  `Approval` and `AIReviewerAssignment` (§ARC12.5 entry 4). The association page already makes
  that read for its round (`Design/UI/Pages/AssociationModerationDetailPage.md` rule 2.16), keyed
  by `Association` and the association's id, so the suggestion waits on no approval id. Neither
  `ApprovalVerdict` nor `GET api/ApprovalSettings` is widened: the verdict is the publisher tier's
  read of the round's conditions, and the settings read answers rows, not a resolved policy. The
  rulings of 2026-10-13 change none of this placement: the suggestion asks none of Berean's
  switches, but the block presents it as Berean's, and the read that carries it is the one the
  page makes for Berean anyway.

## Entity count, events and storage

- **Entities read.** The post (`ContentItem`), its associations (`Association`), each
  association's far end (`Tag` or `BibleReference`), the round (`Approval`, with its reviews,
  requests and comments) and the contributor's name. Each is read on its own route by the page;
  no service composes two of them.
- **The confidence suggestion** reads `Approval`, the association (its score, its personality and
  its endpoints, through `IAccessBroker`) and `ApprovalSetting`, inside an orchestration that
  already spans two entity types. It adds no service dependency: the broker it is gathered through costs
  none (§ARC12.5).
- **Events.** None new. The approval record is created by the approval process's existing
  subscriptions (§APR9.7.2, §APR9.7.3; rule 17).
- **Storage.** None.

## Reads and writes this feature builds on

- **The association reads, #857.** The chips need every association of the post — its id, its far
  end's type and id, its status and its `CreatedWhen` — from `GET /api/associations`, and the
  association page needs one association by its id, with its endpoints, its `CreatedBy`, its
  `CreatedWhen`, its status, and its `ConfidenceScore` and `ConfidenceReason`, from
  `GET /api/associations/{id}` (§ARC17.4). Both are designed under #857 and not built. Every page
  task that needs one names #857 as the task it is blocked on, and #857's design PR moves it to the
  task #857 carves for that read (#857, *Next steps* 2).
- **The confidence suggestion, #863.** The confidence block's suggestion (rule 13) is carried on
  Berean's status read, built by the user stories listed above for it, and handed to the panel by
  the association page (`Design/UI/Pages/AssociationModerationDetailPage.md §6 item 19`).
- **The remove by id, #700.** The association's takedown (rule 16) calls
  `DELETE /api/associations/{id}`, which `Design/UI/Components/AssociationPanel.md §10 item 22`
  gives to #700, with the two gates beneath it. #700 has carved no task for it yet, so the
  takedown is a gap tagged `(needs issue)`.
- **The round's routes.** Both pages read and write a round on the routes the server serves today,
  keyed by the item's type and id, and take the approval id off the record the verdict read
  answers with (rule 17). The move to routes keyed by the approval id (§ARC17.5) is the post
  page's `§6 item 11` and the association page's share of it, and waits on #699's stage-2 build,
  which has no task (`Design/UI/Components/ReviewPanel.md §10 item 12`).
- **The tiers rule 15 waits on, `Design/UI/Components/ReviewPanel.md §10 items 1 and 9` and
  `Design/UI/Components/ReviewCommentPanel.md §10 item 19`.** Rule 15's block on either endpoint
  bars in `ReviewPanel` once the panel composes read-only roles: it composes none today, on any
  round, and those two items, which have no task, compose them from an association's endpoints
  with the view. In `ReviewCommentPanel` the tick's publisher tier and read-only roles compose
  from the endpoints in this feature (`Design/UI/Components/ReviewCommentPanel.md §10 item 22`),
  but the box, Edit, Delete and the author's tick ask no review tier today, on any round
  (`Design/UI/Components/ReviewCommentPanel.md rule 2.30`). They answer to rule 15's
  tier, a scoped role matching either endpoint, only once item 19 there, which has no task,
  builds the review tier, and item 22 composes it from the endpoints
  (`Design/UI/Components/ReviewCommentPanel.md rule 2.35`).

## Risks

- **Reversible throughout.** No schema, no event and no route changes. Berean's status gains one
  member, which no existing reader reads. The side panels the post page drops are still rendered
  by the reader pages.
- **Berean's status read gathers the policy twice.** It already resolves the offer through
  `IAccessBroker`, and the suggestion is a second gather of the round, the row behind it and the
  policy rows, with one call the read never made: building the caller's actor asks the security
  client for their user id (`BuildActorAsync`). The read is refreshed with the round on both
  moderation pages, every 15 seconds while the tab is visible
  (`Design/UI/Pages/AssociationModerationDetailPage.md` rule 2.18,
  `Design/UI/Pages/ContentItemModerationDetailPage.md` rule 2.12), so the post page pays the
  second gather too, for content-item rounds whose suggestion is always none (§APR8.6.2 suggestion
  rule 2). The policy table holds a handful of rows, and the cost is accepted on both pages rather
  than folding the suggestion into the actor-independent verdict (*Layer placement*).
- **A failed suggestion gather fails Berean's whole status.** The read answers nothing where the
  gather fails (`Backend/Orchestrations/AIReviewerOrchestrationService.md §1` rule 6), so
  `IsOffered` and `IsRequested` are lost with the suggestion, and on both moderation pages
  `ReviewPanel` is handed neither Berean's row nor its place in the picker, as for any failed
  status read today (`Design/UI/Pages/ContentItemModerationDetailPage.md` §4,
  `aiReviewerCandidate` and `aiReviewerAssignment`). The status read already fails whole when the
  offer's gather fails; the suggestion adds a second gather that can.
- **A round with no record.** While the verdict read answers not-found, a round's controls stay
  disabled (rule 17), and a refresh that finds the record enables them. On the routes the pages
  read today, keyed by the item, that read opens a missing round itself for a `Draft` or
  `Submitted` item, so the page's own read creates the record. It answers not-found only for an
  item taken down, or for a decided item whose round was never recorded, which no read opens
  (§ARC16.7.2, *The write as built*): there the controls stay disabled, which is the safe side.
  The window rule 17 names, a round the approval process has yet to record, reaches the pages once
  they read the round by its approval id, a read that opens no round (§ARC16.7.2, §ARC17.5; *The
  round's routes* above).

## Out of scope

- The queue, `/Admin/Posts` (`Design/UI/Pages/ContentItemModerationPage.md`), and the mock folder's
  other 15 screens.
- §ARC17.4's two association reads and §SEC14.7 posture A′ rule 7 (#857).
- Building §APR8.6.2 rules 1–3, Berean's comment and review, and the confidence process
  (§APR13.4).
- The remove by id, `DELETE /api/associations/{id}`, and the two gates beneath it (#700).
- Moving the round's routes to the approval id (§ARC17.5; #699's stage-2 build).
- Suggesting a tag or a Bible reference from either page (rule 9).
