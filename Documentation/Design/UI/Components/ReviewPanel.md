# 1. ReviewPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** none
- **Composes:** none. It renders two core UI components, `Avatar` and `ConfirmDialog`, which have no component documents of their own (see section 4.3).
- **Used by:** no other component document renders it. Pages: `/Admin/Posts/{contentItemId}` — `src/pages/admin/contentItemModerationDetailPage.tsx`, in the right-hand column beside [ReviewCommentPanel.md](ReviewCommentPanel.md). `UI/Components/ContentItemPanel.md rule 2.36` leaves approval controls to this panel.
- **Inherits:** §SEC14.6, §SEC14.7 posture D, §SEC18.6, §APR7.9, §APR8.5, §APR8.6, §APR8.6.2, §ARC16.7.2, §ARC16.7.4, §ARC16.7.5, §EVN18, §UI20.6.4, §UI20.6.5, §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/approvals/reviewPanel.tsx`
- **Sample page:** `/SamplePages/Components/Review-Panel` — `src/pages/samplePages/components/reviewPanelDoc.tsx`
- **Relocated from:** §UI20.6.1

The approval round, rendered: the reviews, the viewer's own vote, the block reasons, the bypass, the publisher-tier decision, and the review requests *(§UI20.6)*. A moderator reads it to answer one question — where does this round stand, and what may happen to it now — and acts on it through the vote, the decision, the reset and the request picker.

It is a pure presentation component. It fetches nothing and writes nothing: the page reads the round and hands it over, and every click leaves as an event the page persists. The conversation behind the "unresolved comments" block reason is not here — it is [ReviewCommentPanel.md](ReviewCommentPanel.md), which sits beside this panel and whose every write moves this panel's verdict (`UI/Components/ReviewCommentPanel.md rule 2.15`).

The rules below are grouped by the panel's parts: the contract, freshness, the reviews and the viewer's vote, the outcome, the decision and bypass, the reset, the review requests, the AI reviewer, the identity block every name is drawn with, and the read-only-role view — then the rules every presentation component follows.

## 2. Business Rules

*The contract*

**2.1 [Must]** `ReviewPanel` is a pure presentation component: props in, events out, no fetching, no sockets. *(§UI20.6.1)*

**2.2 [Must]** Every gate it renders is a courtesy — the orchestration re-decides votes, decisions, bypass and requests against the stored rows (§SEC14.6). *(§UI20.6.1)*

**2.3 [Must]** Wherever the server has already answered a question per caller (`CanApprove`, `IsBypassAllowedForCurrentUser`), the verdict's answer is used verbatim rather than re-derived from role names. *(§UI20.6.1)*

**2.4 [Must]** The remaining render gates compose roles per §SEC18.6, capability-last and plural. *(§UI20.6.1)*

**2.5 [Must]** The approval's status drives the frozen/live switch, and it is deliberately a prop of its own, because the read-only view has a status to show and no verdict to read it from. *(§UI20.6.1)*

**2.6 [Could]** Styling is expressed as CSS classes, never as literal colours, so every control follows the light and dark theme. *(code: reviewPanel.tsx — `ReviewPanelProps` THEMING note)*

**2.7 [Must]** The viewer is read from the auth context, never from a property. `entityType` and `contentType` compose role names and nothing else; no identifier is used to fetch anything. *(code: reviewPanel.tsx — `useAuth`, `ReviewPanelProps` Subject note)*

*Freshness*

**2.8 [Must]** The consumer owns freshness. The panel shows the world as of the last props it was handed, so its consumer must re-fetch and re-render when the round changes underneath it — another vote cast, a comment added or resolved, a decision or auto-approval, a request made or answered. It must also refresh the round when the connection comes back: the page's single round refresh — every 15 seconds, on returning to the tab, and on reconnect — keeps the round and its review thread current. *(§UI20.6.1; user, 2026-09-27)*

**2.9 [Could]** SignalR, polling, or a refetch after each event callback are all acceptable; without one of them the panel is simply stale. *(§UI20.6.1)*

**2.10 [Won't]** A push channel adds no new facts. Server side, the EventHighway facts the approval workflow already publishes (§EVN18) are the signal a push channel would forward — a future SignalR hub subscribes to those; it does not add new facts. *(§UI20.6.1)*

*The reviews and the viewer's vote*

**2.11 [Must]** Only three states of a review are in scope — approved, rejected and pending. A dismissed review is excluded outright rather than styled, because it judged superseded content and must not be counted (§APR8.5 rule 3); a soft-deleted review is excluded too, because a withdrawn opinion is no opinion. *(code: reviewPanel.tsx — `visibleReviews`; test: reviewPanel.test.tsx — "should exclude a dismissed review entirely", "should exclude a soft-deleted review entirely")*

**2.12 [Must]** The viewer's own row is matched on `reviewerUserId`, never on a name, and leads the list; every other row is sorted alphabetically by display name, case-insensitively. *(code: reviewPanel.tsx — `viewerReview`, `otherRows`; test: "should list the viewer first and everyone else alphabetically")*

**2.13 [Should]** Cast votes and outstanding review requests share one alphabetical list, a request wearing a "Requested" chip where a vote would be, because a reader asks "where does this round stand?" per person. A vote supersedes the request for the same person (§APR7.9 rule 6), and the viewer never appears among the requested rows. *(code: reviewPanel.tsx — `pendingRequests`; test: "should sort requests and votes into one alphabetical list", "should drop a request once its target has voted", "should not render the viewer among the requested rows")*

**2.14 [Must]** An eligible viewer with no review gets a synthesised "Vote…" placeholder row, because an uncast vote has no `ApprovalReview` row to project. *(code: reviewPanel.tsx — `showPlaceholderRow`; test: "should synthesize a placeholder vote row for an eligible viewer with no vote")*

**2.15 [Must]** The entity's owner is offered no vote: nobody reviews their own content (§APR8.6 HR-1), and the server refuses it regardless. *(code: reviewPanel.tsx — `mayVote`; test: "should not offer a vote to the entity owner")*

**2.16 [Must]** A vote is `Approved` or `Rejected`; "Vote…" is a placeholder, not a castable state. Re-picking the vote already cast raises nothing. *(code: reviewPanel.tsx — `castVote`; test: "should not raise onReviewStatusChanged when the viewer re-picks their current vote")*

**2.17 [Should]** A cast vote stays visible after the round closes or the viewer's roles change, as a badge rather than a control. *(code: reviewPanel.tsx — `renderViewerVoteControl`; test: "should freeze the viewer's vote into a badge once the approval is decided")*

**2.18 [Must]** The UI must not offer the review action to a user who has just edited the entity. "Just edited" means the entity's owner (`CreatedBy`) or whoever amended the item's content (the owner, or a Publisher or Administrator who amended it): neither is offered a review, and neither is offered as a review-request candidate, because a review from either would be a self-approval. What records who amended the content is not yet designed; #701 holds that design (item 10). An Administrator's bypass is not a review — it bypasses the review process and assigns the outcome directly — so it stays available to them (rule 2.24). *(§APR8.6 residual 2; user, 2026-09-26)* ≠ item 2

*The outcome*

**2.19 [Must]** The verdict is read at the moderation tier only (§ARC16.7.2), so a viewer outside it is handed no verdict and the outcome section shows the status pill without block reasons. *(§UI20.6.1)*

**2.20 [Should]** Block reasons render only while the round is open — `Draft` or `Submitted`. A terminal status paints none, even beside a verdict fetched a moment before the decision landed. *(code: reviewPanel.tsx — `isRoundOpen`; test: "should not render block reasons once the round is no longer submitted", "should still paint no reasons over a %s round")*

**2.21 [Should]** Every block reason renders, not only the first, and a `Draft` round states its draft reason rather than swallowing it (§ARC16.7.2). *(test: "should show the blocked panel with every reason message when blocked", "should state the draft block reason rather than swallowing it")*

*The decision and the bypass*

**2.22 [Must]** Deciding belongs to the publisher tier and `Administrators` alone. No `-Reviewers` role at any scope reaches the decision, because a reviewer may never set an `ApprovalStatus` (§APR8.6 HR-3). *(code: reviewPanel.tsx — `defaultDecisionRoles`; test: "should hide the set-approval-status dropdown from %s")*

**2.23 [Must]** Approve is enabled when the verdict says `canApprove`, or when the bypass is ticked on a blocked round. Reject is never gated by the conditions: a rejection withholds approval rather than granting it (§APR8.6 HR-4 route 1). *(code: reviewPanel.tsx — `mayApproveNow`; test: "should disable approve while blocked without a bypass, and keep reject enabled")*

**2.24 [Must]** The bypass is offered only to the decision tier, only on a `Submitted` round, only while the round is blocked, and only when the verdict says `isBypassAllowedForCurrentUser` (§ARC16.7.2). A draft has no round to waive, and a bypass cannot rescue an approval that was never submitted (§ARC16.7.3). *(code: reviewPanel.tsx — `showBypassCheckbox`; test: "should refuse the bypass on a draft even where the verdict allows one", "should hide the bypass checkbox from the reviewer tier even when the verdict allows it")*

**2.25 [Must]** A bypass reason is mandatory when bypassing: Submit holds until the reason is non-blank. A rejection never records a bypass and is always raised as `(Reject, false, "")`. *(§UI20.6.1; code: reviewPanel.tsx — `isBypassReasonMissing`, `submitDecision`; test: "should submit a plain rejection with no bypass recorded", "should hold submit on a bypass approve until a reason is given, then send it")*

**2.26 [Must]** The bypass tick, its reason and a pending decision reset whenever the verdict changes underneath them — a different approval, a reason added, removed or reworded, or a changed `canApprove` or `isBypassAllowedForCurrentUser`. Consent given against one set of reasons is not consent to the next, and a justification typed for one item must never be written onto another's record. *(code: reviewPanel.tsx — `verdictSignature`; test: "should reset the bypass tick when the verdict changes underneath it", "should reset the bypass tick when a block reason is reworded", "should reset the bypass tick when the panel moves to another approval")*

**2.27 [Must]** Unticking the bypass clears its reason, and clears an Approve selection the caller could only make under the bypass. *(code: reviewPanel.tsx — `onBypassToggled`; test: "should clear a bypass approve selection when the bypass is unticked")*

*The reset*

**2.28 [Must]** Resetting a decided round is offered to `Administrators` alone, only on an `Approved` or `Rejected` round, and only when the consumer listens on `onApprovalReset` — and never to an administrator the read-only-role view of rule 2.50 covers. Deciding an open round is the publisher tier's; undeciding a closed one is the §APR8.6 HR-4 override (§ARC16.7.5). *(code: reviewPanel.tsx — `mayResetApproval`; test: "should offer the reset on a decided round to an administrator", "should hide the reset from %s", "should render no reset when nobody is listening"; user, 2026-09-26)* ≠ item 1

**2.29 [Must]** Resetting an `Approved` round asks first, because it takes a live item off the public site; a `Rejected` round resets without asking, because it was never public (§ARC16.7.5). *(test: "should ask before resetting an approved round, and not act until confirmed", "should reset a rejected round without asking")*

*The review requests*

**2.30 [Must]** Requesting a review is coordination, not decision, so it is open to the whole review tier — reviewers included (§APR7.9 rule 2) — and, unlike voting, the entity's owner is not excluded. *(code: reviewPanel.tsx — `mayRequest`; test: "should show the request cog to %s")* ≠ item 3

**2.31 [Must]** Requests are offered only while the round is `Submitted`. *(code: reviewPanel.tsx — `mayRequest`; test: "should hide the request cog once the approval is decided")* ≠ item 3

**2.32 [Must]** The candidates are the consumer's read (§ARC16.7.4), fetched when the picker opens, never by the panel. The picker filters the supplied list client-side by display name and username; it never searches the server. *(code: reviewPanel.tsx — `onReviewerLookupRequested`, `matchesFilter`; test: "should raise onReviewerLookupRequested and list the candidates when opened", "should filter the candidates by display name and by username")*

**2.33 [Should]** Nobody is filtered out of the picker. A person already asked or already answered stays listed and ticked, so a search finds them and answers "why is this person not here?" before it is asked. *(code: reviewPanel.tsx — picker sections note; test: "should list a voter as ticked and refuse to act on a click")*

**2.34 [Must]** The picker has three sections, and they differ in what a click means: **Suggestions** requests, **Requested** withdraws, **Everyone else** requests — with the people who have already voted at its top, ticked and inert. The Requested section is the only route to unassigning anybody (§APR7.9 rule 5). *(code: reviewPanel.tsx — `renderPickerRow`, `everyoneElseRows`; test: "should withdraw a request when its Requested row is picked")*

**2.35 [Must]** A person whose vote is cast is inert wherever they are listed, Requested included: withdrawing an answered invitation is refused (§APR7.9 rule 5). *(code: reviewPanel.tsx — `isInert`; test: "should list a voter as ticked and refuse to act on a click", "should render a requested candidate who has already voted as inert")*

**2.36 [Must]** Requested wins the tie. A person handed in both the suggestions and the outstanding requests renders once, under Requested, where a click withdraws; nobody appears in two sections of one open picker. *(code: reviewPanel.tsx — `requestedPickerRows`, `everyoneElseRows`; test: "should keep a suggested person who is also requested under requested", "should not repeat a suggested person under everyone else")*

**2.37 [Must]** The panel does no ranking of its own. Who is suggested, in what order and for what reason is the consumer's, because it depends on history the panel cannot see. *(code: reviewPanel.tsx — `suggestedReviewerCollection` note)*

**2.38 [Could]** The picker stays open after a pick, because assigning several reviewers is one task. *(code: reviewPanel.tsx — `requestReview`; test: "should raise onReviewRequested and keep the picker open")*

**2.39 [Must]** `maxReviewerRequests` (default 15) caps how many people are waited on at once, counted on outstanding invitations only. At the cap new requests stop; withdrawal never does. *(code: reviewPanel.tsx — `outstandingRequests`, `isBlockedByCap`; test: "should not count an answered request against the cap", "should stop new requests at the cap while leaving withdrawal available")*

**2.40 [Must]** A duplicate request is the server's to dissolve (§APR7.9 rule 4), so the consumer needs no existence check of its own. *(code: reviewPanel.tsx — `onReviewRequested` note)*

*The AI reviewer (Berean)*

**2.41 [Must]** Supplying `aiReviewerCandidate` is the whole of what offers Berean; a panel handed nothing offers nothing — the fail-closed posture of §APR8.6.2. *(code: reviewPanel.tsx — `aiReviewerCandidate` note; test: "should not offer the AI reviewer when no candidate is supplied")*

**2.42 [Should]** Berean leads the Suggestions, ahead of every human suggestion (§APR8.6.2), and answers the filter box like any other row. *(test: "should render the AI reviewer as the first suggestion", "should filter the AI reviewer out with everybody else")*

**2.43 [Must]** Picking Berean raises `onAIReviewerRequested` instead of `onReviewRequested`, and withdrawing it raises `onAIReviewerWithdrawn` instead of `onReviewRequestWithdrawn` — never both. Berean is not a role-bearing identity and has no `ApprovalReviewRequest` (§APR8.6.2). *(code: reviewPanel.tsx — `requestReview`, `withdrawRequest`; test: "should raise onAIReviewerRequested rather than onReviewRequested", "should raise onAIReviewerWithdrawn for Berean and the human one for a person")*

**2.44 [Must]** A live `aiReviewerAssignment` gives Berean a row in the round's list and moves it out of Suggestions into Requested. It is keyed off the assignment rather than the candidate, so a Berean assigned before the offer was switched off stays withdrawable. *(code: reviewPanel.tsx — `requestedPickerRows`; test: "should move an assigned AI reviewer out of suggestions and into requested", "should keep an assigned AI reviewer withdrawable after it stops being offered")*

**2.45 [Must]** Berean's row shows a pending dot until the review is completed, then a re-request control and a comments glyph. Re-requesting raises `onAIReviewerRequested`, because assigning and re-requesting are one upsert. Neither control withdraws. *(code: reviewPanel.tsx — `renderAIReviewerControl`; test: "should render a pending AI reviewer as a dot rather than a Requested badge", "should raise onAIReviewerRequested when the recycle control is clicked")*

**2.46 [Must]** Berean spends no request slot, and keeps its withdrawal at the cap. *(test: "should neither spend a request slot nor lose its withdrawal at the cap")*

**2.47 [Won't]** The panel knows nothing of `IsAIAllowedToVote`: whether Berean casts a review happens on the round, not in this picker. *(code: reviewPanel.tsx — `aiReviewerCandidate` note)*

*The identity block*

**2.48 [Could]** Everywhere a person is named — the round's list and the picker alike — one identity block is drawn: an avatar, the display name over the username. The second line is dropped when there is no username, or when it equals the display name once trimmed and compared case-insensitively. Berean wears its tagline in the username's place and a glyph instead of initials. *(code: reviewPanel.tsx — `renderIdentity`; test: "should render the username under the display name", "should not print a username that is the display name", "should render the AI reviewer tagline where a username goes")*

**2.49 [Should]** The viewer's own row is labelled from the review it carries, and falls back to the signed-in account when the name resolver names nobody. *(code: reviewPanel.tsx — `viewerRow`; test: "should label the viewer own row from the review it carries", "should fall back to the auth context for the viewer own username")*

*The read-only-role view*

**2.50 [Must]** A viewer holding a read-only role at a scope the round composes has the read-only-role view of the panel: no action of any kind. The read-only roles are §SEC18.6's three tiers, composed the way section 3.2 composes the vote and decision tiers: the global `ReadOnly`, `{entityType}-ReadOnly`, and — only when `entityType` is `ContentItem` and a `contentType` is given — `ContentItem-{contentType}-ReadOnly`. Such a viewer is offered no review request — the cog, the picker and Berean's re-request control are hidden — and no reset. The controls their tier would otherwise give them are shown disabled, each carrying the block-role tooltip (rule 2.53): a viewer the vote tier admits sees their vote row with its dropdown disabled (rule 3.3.7) — whether they have yet to vote or have already voted, so they can neither cast nor change a vote before the round's final outcome — and a viewer the decision tier admits sees the decision menu and the bypass disabled (rule 3.3.5). None of them raises anything. The block is still stated to a holder the consumer hands a verdict: on a `Submitted` round the verdict carries it as a block reason (§ARC16.7.2), and the panel renders it like any other (rule 2.21). A holder outside the moderation tier is handed no verdict (rule 2.19), so no block reason can be stated to them. *(user, 2026-09-26; user, 2026-09-27; §SEC18.6 rule 2; code: reviewPanel.tsx — `contentTypedRole`)* ≠ items 1 and 9

*The rules every presentation component follows*

**2.51 [Must]** Every visible string the panel renders, and every string it renders for a screen reader alone, is a property, whose default is today's text, so a page need set none and may override any (§UI20.6.6 rule 1). *(§UI20.6.6 rule 1)* ≠ item 6

**2.52 [Should]** Both of the panel's loading states — the rows' loading line (rule 3.1.4) and the picker's (rule 3.1.5) — are announced as a status (§UI20.6.6 rule 5). *(§UI20.6.6 rule 5)* ≠ item 11

**2.53 [Could]** Every control the read-only-role view shows disabled — the vote dropdown, the decision menu and the bypass (rule 2.50) — carries the block-role tooltip, a property whose default is "User belongs to a block role" (§UI20.6.6 rule 1). How its reason reaches a keyboard and a screen-reader user is section 9. *(user, 2026-09-27; §UI20.6.6 rule 1)* ≠ item 1

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `approvalStatus` other than `Submitted` removes the vote dropdown, the request cog, the decision menu and the bypass; a cast vote stays as a badge (rule 2.17). *(code: reviewPanel.tsx — `isSubmitted`; test: "should hide the request cog once the approval is decided", "should hide the set-approval-status dropdown once the approval is decided")*

**3.1.2** The status pill reads "Awaiting approval" for `Draft` and `Submitted`, and "Approved", "Rejected" or "Dismissed" for those statuses, each with its own class property. *(code: reviewPanel.tsx — `statusPill`; test: "should render the status pill for status %s as "%s"")*

**3.1.3** With `approvalVerdict` absent the outcome section shows the status pill alone — no block reasons, no bypass, and an Approve option that stays disabled (rule 2.19). *(code: reviewPanel.tsx — `mayApproveNow`)*

**3.1.4** `isLoading=true` replaces the review rows with a loading line and holds back the whole outcome section — reasons, pill, reset, bypass and decision — because every part of it is read off rows that have not arrived. *(code: reviewPanel.tsx — `isLoading === false` guard; test: "should hold back the whole outcome while loading, not only the rows", "should show the outcome again once the load is done")*

**3.1.5** `isCandidatesLoading=true` shows a loading line inside the open picker in place of its sections. *(test: "should show the loading text in the picker while candidates load")*

**3.1.6** `emptyText` renders only when there is no viewer row and no other row, and only when it is non-empty; the default is empty. *(code: reviewPanel.tsx — empty branch)*

**3.1.7** `onApprovalReset` absent removes the reset link for everyone (rule 2.28). *(test: "should render no reset when nobody is listening")*

**3.1.8** `voteRoles` and `decisionRoles`, when passed, replace the composed defaults entirely; `""` admits nobody. They change what is rendered and nothing else. *(code: reviewPanel.tsx — `voteRoleList`, `decisionRoleList`, Roles note)*

**3.1.9** `maxReviewerRequests` names the picker heading through `{max}` in `pickerTitleText`; at the cap `requestCapReachedText` shows under the filter box. *(test: "should name the cap in the picker heading")*

**3.1.10** `showBorder=true` draws the panel as a bordered card; the default is `false`. *(code: reviewPanel.tsx — `panelCssClass`)*

**3.1.11** Choosing a decision replaces the menu trigger's text and class with the chosen option's, and reveals a Submit button in the same class. *(code: reviewPanel.tsx — `decisionSelection`, the Submit button)*

**3.1.12** The bypass reason box appears only once the bypass is ticked; the "give a reason" message appears only while the tick, an Approve selection and an empty box together hold Submit shut. *(code: reviewPanel.tsx — `isBypassReasonMissing`; test: "should mark the reason box as the thing holding submit shut, then clear it")*

### 3.2 Driven by roles

**3.2.1** The vote tier, by default: `Reviewers`, `Publishers`, `Administrators`, `{entityType}-Reviewers`, `{entityType}-Publishers`, and — only when `entityType` is `ContentItem` and a `contentType` is given — `ContentItem-{contentType}-Reviewers` and `-Publishers` (§SEC18.6 rule 5). A scoped role for another entity type is not eligible. *(code: reviewPanel.tsx — `defaultVoteRoles`, `contentTypedRole`; test: "should offer a vote to the scoped role %s", "should not treat another entity's scoped role as eligible here")*

**3.2.2** The decision tier, by default: `Publishers`, `Administrators`, `{entityType}-Publishers`, and `ContentItem-{contentType}-Publishers` under the same condition (rule 2.22). *(code: reviewPanel.tsx — `defaultDecisionRoles`; test: "should show the set-approval-status dropdown to %s")*

**3.2.3** The request cog asks the vote tier (rule 2.30). *(code: reviewPanel.tsx — `mayRequest`)*

**3.2.4** The reset asks `Administrators` directly, not through `decisionRoles`, so no override widens it to publishers. *(code: reviewPanel.tsx — `mayResetApproval`)*

**3.2.5** An anonymous viewer is offered no control at all; every gate requires a signed-in identity. *(code: reviewPanel.tsx — `isAuthenticated` in every gate; test: "should not offer a vote to an anonymous reader")* ≠ item 3

**3.2.6** A signed-in reader holding no tier sees the reviews and the status pill and nothing else. *(test: "should show reviews and the status pill, and no controls, to a roleless reader")*

### 3.3 Combinations

**3.3.1** Status first, roles second, and a read-only role at a scope the round composes last, withholding every action the first two leave — the request and the reset hidden, the vote, the decision and the bypass shown disabled (rule 2.50). A round that is not `Submitted` offers no vote, no request and no decision whatever the viewer holds; a decided round offers the reset to an administrator who holds no such read-only role (rules 2.28 and 3.3.7), and nothing else. *(code: reviewPanel.tsx — `mayVote`, `mayDecide`, `mayRequest`, `mayResetApproval`; user, 2026-09-26; user, 2026-09-27)* ≠ items 1 and 9

**3.3.2** Ownership subtracts the vote only. An owner who also holds a vote-tier role keeps the request cog; an owner who also holds the decision tier keeps the decision menu, where Approve follows the verdict's `canApprove` — which folds §APR8.6 HR-2 and its administrator bypass exception — and Reject stays enabled. *(code: reviewPanel.tsx — `mayVote`, `mayRequest`, `mayDecide`)*

**3.3.3** The bypass needs all four: the decision tier, a `Submitted` round, a blocked verdict, and the verdict's `isBypassAllowedForCurrentUser`. The verdict saying yes does not admit the reviewer tier. *(code: reviewPanel.tsx — `showBypassCheckbox`)*

**3.3.4** `isLoading` outranks every role: while it is true no outcome control renders for anybody. *(code: reviewPanel.tsx — outcome guard)*

**3.3.5** A `ReadOnly` reaches the decision and the bypass through the verdict, taken verbatim (rule 2.3). A viewer holding one at a scope the entity composes is answered `canApprove` false and `isBypassAllowedForCurrentUser` false, and on a submitted round the verdict carries the block as a reason. The server refuses such a viewer every decision, Reject as well as Approve, and every bypass, so none is live: the decision menu and the bypass checkbox render disabled beside the stated block, each carrying the block-role tooltip (rule 2.53). The menu does not open, so neither Approve nor Reject can be chosen, and the bypass cannot be ticked. The veto is not one of the §APR8.5 conditions rule 2.23 leaves Reject free of. *(§ARC16.7.2; code: AccessService.cs — `DecideMayRecordApprovalOutcome`; user, 2026-09-27)* ≠ item 9

**3.3.6** A read-only role at a scope the round composes (rule 2.50) outranks the request tier: its holder is offered no request cog, no picker and no Berean re-request control, whatever tier they hold: the three are hidden, not disabled. The panel withholds the request under a scoped block as well as the global one, and the server refuses a review request under all three read-only roles in scope, as it refuses the round's other writes, and the same block refuses withdrawing one (§APR7.9 rule 2; user rulings 2026-09-27; not yet built — today it refuses both under the global `ReadOnly` alone, section 10, item 1). The same block refuses requesting or withdrawing Berean on the server, as it refuses a human request (§APR8.6.2; user ruling 2026-09-27; not yet built — today Berean's assignment refuses the global `ReadOnly` alone, section 10, item 1). *(user, 2026-09-26; user, 2026-09-27; code: ApprovalReviewRequestService.Validations.cs — `ValidateUserIsAllowedToContribute`; code: AIReviewerAssignmentService.Validations.cs — `ValidateUserIsAllowedToContribute`)* ≠ items 1 and 3

**3.3.7** The same role outranks the vote and the reset. Its holder's vote row, where the vote tier gives them one (rule 2.14) or they have already voted, renders its dropdown disabled — showing their vote, or the "Vote…" placeholder, both of which the user named on 2026-09-27, so that they can neither cast nor change a vote before the round's final outcome — and carrying the block-role tooltip (rule 2.53); an administrator holding it is offered no reset on a decided round. The server refuses both — the vote under §SEC18.6 rule 2, and the reset, which it asks as an amendment of the approval record. *(user, 2026-09-26; user, 2026-09-27; code: ApprovalOrchestrationService.Resets.cs — `MayAmendApprovalAsync`; test: ApprovalOrchestrationServiceTests.Reset.cs — "ShouldRefuseAResetToASanctionedAdministratorAsync")* ≠ item 1

**3.3.8** The read-only-role view is not the frozen view. The frozen view is keyed on the round's status (rule 3.1.1): a cast vote stays visible as a badge (rule 2.17), no dropdown, cog, menu or bypass renders, and an administrator is offered the reset on a decided round (rule 2.28). The read-only-role view withholds the reset on every status, and on a `Submitted` round hides the cog and renders the vote dropdown, the decision menu and the bypass disabled, each carrying the block-role tooltip (rules 3.3.5–3.3.7), where the frozen view renders none of them. On a round that is not `Submitted` the holder sees what the frozen view shows anyone, the reset apart. *(code: reviewPanel.tsx — `isSubmitted`, `renderViewerVoteControl`; user, 2026-09-26; user, 2026-09-27)* ≠ item 1

### 3.4 Role matrix

Owner here is the account whose id matches `entityOwnerId` — the entity's contributor. Reviewer and Publisher hold the tier at a scope section 3.2 composes. Every row is on a `Submitted` round unless it says otherwise. The amender rows are `➖ n/a` for the Reviewer column, because the review tier may not amend another's content (§SEC14.7 posture A rule 3), and for the Owner column, as in the owner rows, because that column holds no tier.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Any status — **review rows, Requested chips, Berean's row, status pill** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `approvalVerdict` supplied and blocked, `Draft` or `Submitted` — **block reasons** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes |
| `Approved`, `Rejected` or `Dismissed` — **block reasons** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| **Vote dropdown** (or the "Vote…" placeholder row) | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer owns the entity and holds the column's tier — **vote dropdown** | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| Any status but `Submitted` — **vote dropdown** (a cast vote shows as a badge) | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| **Request cog and picker** | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer owns the entity and holds the column's tier — **request cog** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Any status but `Submitted` — **request cog** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| **Decision menu** ("Set approval status") and its **Reject** option | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| **Approve** option enabled | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes² | ✅ Yes² |
| Viewer owns the entity and holds the column's tier — **decision menu** | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes³ | ✅ Yes³ |
| Verdict blocked and `isBypassAllowedForCurrentUser` — **bypass checkbox** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| `Draft`, verdict allows a bypass — **bypass checkbox** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `Approved` or `Rejected`, `onApprovalReset` wired — **Reset approval** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| `Draft`, `Submitted` or `Dismissed`, or `onApprovalReset` absent — **Reset approval** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `aiReviewerAssignment` completed — **Berean's re-request control** ≠ item 3 | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer holds a read-only role at a scope the round composes (rule 2.50) — **vote dropdown**, live ≠ item 1 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| As the row above — **vote dropdown rendered disabled**, with the block-role tooltip ≠ item 1 | ➖ n/a | ❌ No | ❌ No | ✅ Yes⁴ | ✅ Yes⁴ | ✅ Yes⁴ |
| As the row above — **request cog and picker**, and **Berean's re-request control** ≠ items 1 and 3 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| As the row above — **decision menu and bypass checkbox**, live ≠ item 9 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| As the row above — **decision menu and bypass checkbox rendered disabled** beside the stated block, with the block-role tooltip ≠ item 9 | ➖ n/a | ❌ No | ❌ No | ❌ No | ✅ Yes⁴ | ✅ Yes⁴ |
| As the row above, `Approved` or `Rejected`, `onApprovalReset` wired — **Reset approval** ≠ item 1 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Viewer amended the entity's content (rule 2.18) and holds the column's tier — **vote dropdown** ≠ item 2 | ➖ n/a | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No |
| As the row above, verdict blocked and `isBypassAllowedForCurrentUser` — **bypass checkbox** | ➖ n/a | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes |
| `voteRoles=""` — **vote dropdown and request cog** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `decisionRoles=""` — **decision menu and bypass** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ The panel renders the reasons for anyone it is handed a blocked verdict for. The consumer is handed one only at the moderation tier (§ARC16.7.2, rule 2.19), so on a page these viewers see none.
² Only when the verdict says `canApprove`, or the bypass is ticked on a blocked round.
³ The menu renders; Approve follows `canApprove`, which folds §APR8.6 HR-2 and its administrator bypass exception.
⁴ Rendered, and disabled: it does not open and raises nothing (rules 3.3.5 and 3.3.7).

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `entityType` | `string` | required | Names the entity under approval so the vote and decision tiers compose (section 3.2). Fetches nothing. | — |
| `contentType` | `string?` | — | Adds the narrow `ContentItem-{contentType}-` tier; `ContentItem` only. The enum member name, never the editable type name. | — |
| `entityOwnerId` | `string?` | — | The entity owner's account id; suppresses their vote (rule 2.15). | — |
| `approvalStatus` | `ApprovalStatus` | required | The frozen/live switch and the status pill (rule 2.5). | — |
| `approvalVerdict` | `ApprovalVerdictItem?` | — | The per-caller verdict; absent outside the moderation tier (rule 2.19). | — |
| `approvalReviewCollection` | `ApprovalReviewItem[]` | `[]` | Every recorded review; dismissed and soft-deleted rows are dropped (rule 2.11). | — |
| `requestedReviewerCollection` | `ReviewerCandidateItem[]` | `[]` | The pending review requests (§APR7.9). | — |
| `reviewerCandidateCollection` | `ReviewerCandidateItem[]` | `[]` | Everyone the picker may offer (§ARC16.7.4). | — |
| `suggestedReviewerCollection` | `ReviewerCandidateItem[]` | `[]` | Worth asking first, each with its own `suggestionReason`. | — |
| `aiReviewerCandidate` | `ReviewerCandidateItem?` | — | Offers Berean (rule 2.41). | `Avatar` glyph, via `aiReviewerIconCssClass` |
| `aiReviewerAssignment` | `{ candidate, isAIReviewCompleted, isAIReviewCommentsPresent }?` | — | A live Berean assignment (rule 2.44). | — |
| `maxReviewerRequests` | `number` | `15` | The request cap (rule 2.39). | — |
| `isLoading` | `boolean` | `false` | Holds back the rows and the outcome (rule 3.1.4). | — |
| `isCandidatesLoading` | `boolean` | `false` | The picker's own loading line (rule 3.1.5). | — |
| `voteRoles`, `decisionRoles` | `string?` | composed (section 3.2) | Comma-separated overrides of the render gates (rule 3.1.8). | — |
| `showBorder` | `boolean` | `false` | Bordered card. | — |
| `cssClass` | `string` | `''` | Appended to the panel's class list. | — |
| `titleText`, `outcomeTitleText` | `string` | `'Approval Reviews'`, `'Review Outcome'` | The two headings. | — |
| `resetApprovalText` | `string` | `'Reset approval'` | The reset link. | — |
| `resetConfirmTitleText`, `resetConfirmMessageText`, `resetConfirmButtonText`, `resetCancelButtonText` | `string` | `'Reset this approval?'`, the unpublish warning, `'Reset approval'`, `'Cancel'` | The approved-round reset confirmation. | `ConfirmDialog` `title`, `message`, `confirmText`, `cancelText` |
| Vote text: `votePlaceholderText`, `approvedText`, `rejectedText`, `approveVoteDescription`, `rejectVoteDescription` | `string` | `'Vote...'`, `'Approved'`, `'Rejected'`, … | The vote dropdown and badges. | — |
| Outcome text: `blockedTitleText`, `awaitingApprovalText`, `approvedStatusText`, `rejectedStatusText`, `dismissedStatusText`, `requestedVoteText` | `string` | `'Approval is blocked'`, `'Awaiting approval'`, … , `'Requested'` | The block heading, the pill and the Requested chip. | — |
| Berean text: `aiReviewerTaglineText`, `aiReviewPendingTooltip`, `aiReviewReRequestTooltip`, `aiReviewCommentsPresentTooltip`, `aiReviewCommentsAbsentTooltip`, `withdrawAIReviewerTooltip` | `string` | `'Your AI Pair Reviewer'`, … | Berean's tagline and the accessible names of its glyphs. | — |
| Picker text: `pickerTitleText`, `suggestionsSectionText`, `requestedSectionText`, `everyoneElseSectionText`, `requestCapReachedText`, `requestReviewTooltip`, `candidateFilterPlaceholderText`, `noCandidatesText`, `withdrawRequestTooltip` | `string` | `'Request up to {max} reviewers'`, … | The cog, the picker and its hints. | — |
| Decision text: `bypassLabelText`, `bypassReasonPlaceholderText`, `bypassReasonRequiredText`, `setStatusText`, `approveOptionText`, `approveOptionDescription`, `rejectOptionText`, `rejectOptionDescription`, `submitButtonText` | `string` | `'Set approval status'`, `'Submit'`, … | The bypass and the decision menu. | — |
| `emptyText` | `string` | `''` | Shown when the list is empty (rule 3.1.6). | — |
| The block-role tooltip — its name is fixed by the task that builds it ≠ item 1 | `string` | `'User belongs to a block role'` | The tooltip on every control the read-only-role view shows disabled (rule 2.53). | — |
| Class properties: `approvedVoteCssClass`, `rejectedVoteCssClass`, `uncastVoteCssClass`, `requestedVoteCssClass`, `awaitingPillCssClass`, `approvedPillCssClass`, `rejectedPillCssClass`, `dismissedPillCssClass`, `blockedIconCssClass`, `bypassCssClass`, `setStatusCssClass`, `approveSelectionCssClass`, `rejectSelectionCssClass`, `aiReviewerIconCssClass` | `string` | theme classes — `btn-success`, `btn-danger`, `btn-secondary`, `btn-warning`, `btn-dark`, `g2h-review-status-select`, `bi-person-fill`, … | Theme classes only, never literal colours (rule 2.6). | `aiReviewerIconCssClass` → `Avatar` `iconCssClass` |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onReviewStatusChanged` | `vote: ApprovalStatus` (`Approved` or `Rejected`) | The viewer casts or changes their vote (rule 2.16). |
| `onApprovalStatusChanged` | `decision, isBypassRequested, bypassReason` | Submit is pressed; maps 1:1 onto the decision endpoint (rule 2.25). |
| `onApprovalReset` | none | An administrator resets a decided round — after the confirmation on an `Approved` one (rule 2.29). |
| `onReviewerLookupRequested` | none | The picker opens. |
| `onReviewRequested` | `candidate: ReviewerCandidateItem` | A person is picked under Suggestions or Everyone else. |
| `onReviewRequestWithdrawn` | `candidate: ReviewerCandidateItem` | A person is picked under Requested. |
| `onAIReviewerRequested` | `candidate: ReviewerCandidateItem` | Berean is picked, or its re-request control is pressed. |
| `onAIReviewerWithdrawn` | `candidate: ReviewerCandidateItem` | Berean is picked under Requested. |

### 4.3 Pass-through properties

Per §UI20.6.5. `ReviewPanel` renders no child that has a component document, so the check has nothing to cover. The two core-UI primitives it renders are styled through its own text and CSS-class properties, and the settings it fixes on them — the dialog's confirm colour, the avatar's size and image — are not gaps (§UI20.6.5). What of its properties reaches them:

| Child | Child property | Driven by |
| --- | --- | --- |
| `ConfirmDialog` | `title`, `message`, `confirmText`, `cancelText` | `resetConfirmTitleText`, `resetConfirmMessageText`, `resetConfirmButtonText`, `resetCancelButtonText`, unchanged |
| `Avatar` | `name` | the row's display name |
| `Avatar` | `iconCssClass` | `aiReviewerIconCssClass`, on Berean's rows only |

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

The read-only roles the panel composes are rule 2.50's three: `ReadOnly`, `{entityType}-ReadOnly`, and `ContentItem-{contentType}-ReadOnly` on a `ContentItem` with a content type. A holder has the read-only-role view: no action of any kind (user ruling 2026-09-26). The request and the reset are hidden from them, and the vote, the decision and the bypass are shown disabled, carrying the block-role tooltip (rule 2.50; user ruling 2026-09-27). No action is offered to a signed-out reader (rule 3.2.5), and the one page that renders the panel admits `Administrators` alone (section 10, item 7).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The review rows, the Requested chips, Berean's row and the status pill** (a view) | Everyone the consumer renders the panel for | None: a read | ✅ Allowed — the same view | ✅ Shown, if the consumer renders the panel for them | §SEC14.7 posture D rule 1: the records are never public |
| **The block reasons** (a gated view) | Whoever the consumer hands a blocked verdict, on a `Draft` or `Submitted` round (rules 2.19, 2.20) | None | ✅ Allowed — on a `Submitted` round the block is stated among the reasons (rule 2.50) | Not handed a verdict (rule 2.19) | §ARC16.7.2: the verdict is read at the moderation tier only |
| **The vote**, raising `onReviewStatusChanged` ≠ item 1 | The vote tier (rule 3.2.1), on a `Submitted` round, never the owner (rule 2.15) or whoever amended the content (rule 2.18) | Rule 2.50's three | ❌ Refused — the dropdown renders disabled, with the block-role tooltip (rule 3.3.7) | Not offered (rule 3.2.5) | §SEC14.7 posture D rule 4; §APR8.6 HR-1; §SEC18.6 rule 2 |
| **The request cog and picker** — requesting and withdrawing, raising `onReviewRequested` and `onReviewRequestWithdrawn` ≠ item 1 | The vote tier, on a `Submitted` round; the owner is not excluded (rules 2.30, 2.31) | Rule 2.50's three | ❌ Refused — no cog and no picker (rule 3.3.6) | Not offered (rule 3.2.5) | §APR7.9 rules 2–5; the server is to refuse a request, and its withdrawal, under all three read-only roles in scope (§APR7.9 rule 2; not yet built, rule 3.3.6) |
| **Berean in the picker**, raising `onAIReviewerRequested` and `onAIReviewerWithdrawn` ≠ item 1 | As the picker, when the consumer supplies `aiReviewerCandidate` or `aiReviewerAssignment` (rules 2.41, 2.44) | Rule 2.50's three | ❌ Refused — no picker (rule 3.3.6) | Not offered (rule 3.2.5) | §APR8.6.2; the server is to refuse requesting Berean, and withdrawing it, under all three read-only roles in scope, as it refuses a human request (user ruling 2026-09-27; not yet built, rule 3.3.6) |
| **Berean's re-request control**, raising `onAIReviewerRequested` ≠ items 1 and 3 | The request tier, on a `Submitted` round, once Berean's review is completed (rules 2.30, 2.31, 2.45) | Rule 2.50's three | ❌ Refused — no control (rule 3.3.6) | Not offered (rule 3.2.5) | §APR8.6.2; the server is to refuse the re-request under all three read-only roles in scope, as it refuses a human request (user ruling 2026-09-27; not yet built, rule 3.3.6) |
| **The decision** — Approve, Reject and Submit, raising `onApprovalStatusChanged` ≠ item 9 | The decision tier (rule 3.2.2), on a `Submitted` round; Approve only when the verdict says `canApprove` or the bypass is ticked (rule 2.23) | Rule 2.50's three | ❌ Refused — the menu renders disabled beside the stated block, with the block-role tooltip (rule 3.3.5) | Not offered (rule 3.2.5) | §SEC14.7 posture D rule 3; §APR8.6 HR-2 and HR-3; the verdict's per-caller answer (§ARC16.7.2) |
| **The bypass** — its tick and its reason ≠ item 9 | The decision tier, on a blocked `Submitted` round the verdict allows a bypass on (rule 2.24) | Rule 2.50's three, through the verdict | ❌ Refused — the checkbox renders disabled, with the block-role tooltip, and cannot be ticked (rule 3.3.5) | Not offered (rule 3.2.5) | §ARC16.7.2; §APR8.6.1 |
| **Reset approval**, raising `onApprovalReset` ≠ item 1 | `Administrators`, on an `Approved` or `Rejected` round, when the consumer listens (rule 2.28) | Rule 2.50's three | ❌ Refused — no reset link (rule 3.3.7) | Not offered (rule 3.2.5) | §ARC16.7.5; §APR8.6 HR-4; the reset is asked as an amendment of the approval record (rule 3.3.7) |

1. Every gate decides rendering only. The orchestration re-decides votes, decisions, bypass and requests against the stored rows (rule 2.2, §SEC14.6); a hidden control is a courtesy, never an authorization boundary.
2. Identity is the signed-in account from the auth context, matched on account id — `user.userId` against `entityOwnerId` and `reviewerUserId` — never on a display name. *(code: reviewPanel.tsx — `isOwner`, `viewerReview`)*
3. The per-caller answers — `canApprove`, `isBypassAllowedForCurrentUser` — are the server's and are used verbatim (rule 2.3). The browser cannot compute them: they fold the §APR8.5 conditions, §APR8.6 HR-2 and the regardless-rule on reviewers who decide, and `DoNotAllowBypassingSettings`.
4. The verdict is never public (§SEC14.7 posture D, §ARC16.7.2). The panel shows block reasons only when it is handed a verdict, and the consumer is handed one only at the moderation tier.
5. Vote, decision and reset follow the hard rules: no self-review (§APR8.6 HR-1), no reviewer deciding (HR-3), the reset is the administrator's override (HR-4, §ARC16.7.5).
6. Reviewer candidates are a user-enumeration surface (§ARC16.7.4). The panel shows only what the consumer's read carries — account id, display name, username — and adds nothing.
7. A read-only role at a scope the round composes gives its holder the read-only-role view (rule 2.50). The block reaches the decision and the bypass through the verdict's per-caller answers, and the server refuses a blocked viewer every decision, Reject included, so neither may be live (rule 3.3.5). The vote, the request and the reset must ask the block themselves (rules 3.3.6 and 3.3.7); the server refuses the vote and the reset where the block covers them, and is to refuse the request and its withdrawal under all three read-only roles in scope, where today it asks the global `ReadOnly` alone (§APR7.9 rule 2; not yet built); the same block is to refuse requesting or withdrawing Berean, where today Berean's assignment asks the global `ReadOnly` alone (§APR8.6.2; not yet built). Today the render gates ask no `ReadOnly` of their own (section 10, items 1 and 9).
8. No self-review (rule 2.18, §APR8.6 HR-1). The owner is matched on the account id, as item 2 above says; whoever amended the content is not matched today (section 10, items 2 and 10).

## 6. Composition and Usage

- **Where it lands.** On `/Admin/Posts/{contentItemId}` it stands in the right-hand column — "what is being judged on the left, who is judging it on the right" — above `ContentItemSettingsPanel`, with [ReviewCommentPanel.md](ReviewCommentPanel.md) in the left column beneath the item and its associations (`UI/Components/ReviewCommentPanel.md rule 2.16`). *(code: contentItemModerationDetailPage.tsx)*
- **The two panels are one round.** The unresolved-comment count this panel can only report as a block reason is the thread `ReviewCommentPanel` renders, and every write to that thread invalidates this panel's verdict (`UI/Components/ReviewCommentPanel.md rule 2.15`).
- **Assembling a round is a page's job.** Every item that implements `IApproval` stores the id of its round (§APR7.4 item 6), and every round route is addressed by it (§ARC17.5; ruled 2026-09-27 under #699, not yet built). So the page reads the id off the item, reads the verdict and the reviews by it, and alongside them the candidates, the outstanding requests, the reviewer names and Berean's status, and projects them into this panel's props. Today `useApprovalRound` asks the verdict by the item's type and id and takes the approval id off it before reading the reviews (section 10, item 12). *(code: src/hooks/useApprovalRound.ts)*
- **Freshness on the one consumer.** `useApprovalRoundChanges` polls the round every 15 seconds while the tab is visible and refreshes it on returning to the tab and on reconnect, and the page refreshes the item with it, because `approvalStatus` comes off the stored item rather than off the verdict. *(code: src/hooks/useApprovalRoundChanges.ts, contentItemModerationDetailPage.tsx — `refreshModerationView`)*
- **Minimal use.**

  ```tsx
  <ReviewPanel
      entityType="ContentItem"
      contentType={ContentType[contentItem.contentType] ?? ''}
      entityOwnerId={contentItem.createdBy}
      approvalStatus={contentItem.approvalStatus}
      approvalVerdict={approvalVerdict}
      approvalReviewCollection={approvalReviewCollection}
      onReviewStatusChanged={(vote) => void castVoteAsync(vote)}
      onApprovalStatusChanged={(decision, isBypassRequested, bypassReason) =>
          void decideAsync(decision, isBypassRequested, bypassReason)} />
  ```

## 7. Dependencies

**Direct API dependencies** (called by the consumer, never the component) *(§UI20.6.1)*:

| Concern | Endpoint |
| --- | --- |
| The outcome section ≠ item 12 | `GET api/Approvals/{approvalId}/Verdict` (§ARC16.7.2, §ARC17.5 — moderation tier only, so the read-only view gets the status pill without block reasons) |
| The decision ≠ item 12 | `PUT` or `POST api/Approvals/{approvalId}`, its body carrying the decision, whether a bypass is requested, and the bypass reason — mandatory when bypassing — and nothing else (§ARC17.5) |
| The viewer's vote | `POST` / `PUT api/ApprovalReviews` |
| The review rows | `GET api/ApprovalReviews` filtered by `ApprovalId` |
| The request rows and picker ≠ item 12 | The §ARC16.7.4 candidates and review-request endpoints, under `api/Approvals/{approvalId}/…` (§ARC17.5) |
| The names on its reviewers and its invitations ≠ item 12 | `GET api/Approvals/{approvalId}/ReviewerDisplayNames` — the §ARC16.7.4 resolver, asked once for the round. Candidates are NOT in it: the candidates read above already carries a display name for every person it offers, and both are composed by the same method, so the two never disagree |

Two further endpoints the one consumer calls, not in the relocated table:

| Concern | Endpoint |
| --- | --- |
| The reset ≠ item 12 | `POST api/Approvals/{approvalId}/Reset` (§ARC16.7.5, §ARC17.5) *(code: apiBroker.approvals.ts — `PostApprovalResetAsync`)* |
| Berean's status, assignment and withdrawal ≠ item 12 | `GET` / `POST` / `DELETE` on Berean's own resource, addressed by the approval id (§APR8.6.2, §ARC17.5) *(code: apiBroker.aiReviewers.ts)* |

**Indirect dependencies:** the signed-in identity and roles (`/api/accounts/me` via the auth context) for the render gates, and the approval's status for the frozen/live switch — deliberately a prop of its own, because the read-only view has a status to show and no verdict to read it from. *(§UI20.6.1)*

## 8. States, Validation and Feedback

- **Loading.** Two states. `isLoading` shows "Loading…" in place of the rows and withholds the outcome (rule 3.1.4); `isCandidatesLoading` does the same inside the picker (rule 3.1.5). Both must be announced (rule 2.52). Today neither is: both are plain paragraphs, the panel's only status region being the block reasons, and neither text is a property (section 10, items 6 and 11).
- **Empty.** No rows renders `emptyText` if one is given, and nothing otherwise (rule 3.1.6). An open picker with nothing in any section shows `noCandidatesText`.
- **Frozen.** A round that is not `Submitted` renders read-only (rule 3.1.1).
- **Read-only-role view.** A viewer holding a read-only role at a scope the round composes is offered no action on any status (rules 2.50 and 3.3.8): the request and the reset are hidden, and on a `Submitted` round the vote, the decision and the bypass render disabled, each carrying the block-role tooltip (rule 2.53). On a `Submitted` round the block is stated among the reasons to a holder the consumer hands a verdict; a holder outside the moderation tier is handed no verdict (rule 2.19), so no block reason is stated to them.
- **Blocked.** The block reasons render under `blockedTitleText` as a live status region (rule 2.21).
- **Validation.** The only input is the bypass reason: Submit holds while it is blank, and `bypassReasonRequiredText` says why (rules 2.25, 3.1.12). Every other refusal is the server's; the consumer reports it (the one consumer raises a toast carrying the API's reason).
- **Confirmation.** Resetting an `Approved` round confirms through `ConfirmDialog`; a `Rejected` one does not (rule 2.29). The decision itself is not confirmed: choosing an option and pressing Submit is the two-step.
- **Stale consent.** The bypass tick, reason and selection reset when the verdict changes (rule 2.26).
- **Freshness.** The panel is only as fresh as its props (rule 2.8).

## 9. Styling and Accessibility

- **Class hooks** (`approvals.css`): `g2h-review-panel`, `g2h-review-row`, `g2h-review-identity-name`, `g2h-review-identity-username`, `g2h-review-identity-reason`, `g2h-review-vote-badge`, `g2h-review-status-pill`, `g2h-review-request-cog`, `g2h-review-candidate-picker`, `g2h-review-picker-head`, `g2h-review-picker-list`, `g2h-review-picker-section`, `g2h-review-picker-section-title`, `g2h-review-picker-row`, `g2h-review-picker-tick`, `g2h-review-status-select`, `g2h-review-blocked`, `g2h-review-block-reason`, `g2h-review-reset`, and the Berean hooks `g2h-ai-reviewer-pending-dot`, `g2h-ai-reviewer-rerequest`, `g2h-ai-reviewer-comments-present`, `g2h-ai-reviewer-comments-absent`. The secondary identity lines use their own colour rather than `.text-muted`, which the theme paints at 1.49:1 on white. *(code: approvals.css)*
- **Layout.** Each human row is stacked — the identity on top, the vote across the full width beneath — so a long name never pushes the answer off a narrow column and a column of equal bars reads as a tally. Berean's row is one line, its controls being glyph-sized. *(code: reviewPanel.tsx — `renderReviewRow`, `renderAIReviewerRow`; test: "should render every vote across the full width of the row")*
- **Region.** The panel is a `section` named by its heading (`aria-labelledby`). The block reasons are `role="status"`.
- **Menus.** The vote menu, the picker and the decision menu are disclosures: `aria-expanded`, `aria-controls` only while open, labelled by their trigger, and deliberately no `aria-haspopup`. Each dismisses on Escape (focus back to its trigger), on a click outside, on its own trigger and on tabbing out; only one is open at a time; focus returns to the trigger after a vote or a decision; the picker focuses its first control when it opens. *(code: reviewPanel.tsx, src/hooks/useDismissableMenu.ts; test: reviewPanel.test.tsx — "menu dismissal and labelling")*
- **Picker rows.** A row is a button with `aria-pressed` for its tick, and says what a click will do in a visually-hidden hint rather than an `aria-label`, so the visible name stays in the accessible name (WCAG 2.5.3). *(code: reviewPanel.tsx — `actionHint`)*
- **Glyphs.** Every Berean state carries its own accessible name: the pending dot and the comments glyph through visually-hidden text, the re-request button through `aria-label`. The request cog carries `requestReviewTooltip` as its name.
- **Disabled controls.** A disabled control takes no keyboard focus, and a tooltip is not announced, so a tooltip alone reaches neither a keyboard user nor a screen reader. Each control the read-only-role view shows disabled — the vote dropdown, the decision menu and the bypass (rule 2.50) — therefore carries the block-role reason twice (rule 2.53): as its tooltip, restored on hover over the disabled control, and in what a screen reader announces for the control, which lists it with the reason although it takes no focus — the pattern of the card's locked action (`UI/Components/ContentItemPanel.Default.md §9`). Both are the one property's text (§UI20.6.6 rule 1). Not built (section 10, items 1 and 9).
- **Avatars** are decorative, because the name is printed beside them.
- **Bypass reason.** `aria-required`, and `aria-describedby` pointing at the required-reason message while it shows.

## 10. Open Questions and Gaps

1. (needs issue) **Gap — the read-only-role view is not built for the vote, the request and the reset.** Rule 2.50 (user rulings 2026-09-26 and 2026-09-27) gives a viewer holding a read-only role at a scope the round composes the read-only-role view, and rules 3.3.6–3.3.8 say how the vote, the request and the reset render in it: the request cog, the picker and Berean's re-request control hidden, the reset hidden, and the viewer's vote row rendered with its dropdown disabled, carrying the block-role tooltip, a property whose default is "User belongs to a block role" (rule 2.53). The panel composes no read-only role name, and its gates for these three ask no block (`reviewPanel.tsx` — `mayVote`, `mayRequest`, `mayResetApproval`; Berean's re-request control asks no gate at all, item 3). Such a holder is offered a live vote dropdown, the request cog and picker, and — to an administrator on a decided round — the reset. `renderViewerVoteControl` has no disabled state: it renders a live dropdown where `mayVote` holds, and otherwise a badge or nothing; and the panel has no block-role tooltip and no property for one (`ReviewPanelProps`). The user's ruling names the vote dropdown of a reader "previously assigned as a reviewer" and was given over the "Vote…" placeholder row; asked whether that meant the placeholder or a vote already cast, the user answered on 2026-09-27 "yes to both, it disables the drop down so they can't cast or change their vote prior to the final outcome" — the vote row the panel draws for the viewer, the vote tier's placeholder (rule 2.14) or a vote already cast (rules 2.50 and 3.3.7). The server refuses the vote (§SEC18.6 rule 2), the request and its withdrawal under the global `ReadOnly` alone today — the user ruled on 2026-09-27 that it is to refuse both under every read-only role in scope, as the panel does (§APR7.9 rule 2), and that server work is not yet built (`ApprovalReviewRequestService.Validations.cs` — `ValidateUserIsAllowedToContribute`) — and the reset, which it asks as an amendment of the approval record (`ApprovalOrchestrationService.Resets.cs` — `MayAmendApprovalAsync`; test: "ShouldRefuseAResetToASanctionedAdministratorAsync"). Berean is not refused that way yet either: the user ruled on 2026-09-27 that the same block refuses requesting or withdrawing Berean on the server, as it refuses a human request (§APR8.6.2), and that server work is not yet built — Berean's assignment refuses the global `ReadOnly` alone (`AIReviewerAssignmentService.Validations.cs` — `ValidateUserIsAllowedToContribute`, line 52 at 70dc72e7), and the orchestration's request gate asks no read-only role (`AIReviewerOrchestrationService.Validations.cs` — `ValidateUserMayRequestAIReviewer`, lines 40-59 at 70dc72e7). The decision's part of the read-only-role view — the decision menu and the bypass, left live — is item 9. The panel already composes the content-type tier through a helper that confines it to `ContentItem`, as rule 2.50's third name requires (`reviewPanel.tsx` — `contentTypedRole`); `ReviewCommentPanel`'s `scopedNames` does not confine it (`UI/Components/ReviewCommentPanel.md §10 item 7`).
2. (needs issue) **Gap — rule 2.18 is not built for whoever amended the content.** Rule 2.18, defined by the user's ruling of 2026-09-26, withholds the review from the entity's owner and from whoever amended the item's content, and keeps both out of the review-request candidates. For the owner the panel and the server already comply: the panel offers the owner no vote (rule 2.15, `entityOwnerId`); the server refuses the owner's review (§APR8.6 HR-1) and an invitation aimed at them (§APR7.9 rule 3); and the candidates read omits them (§ARC16.7.4), so the picker, which filters nobody out of what it is handed (rule 2.33), never lists them from that read. For whoever amended the content nothing does: no property carries the amender, `mayVote` asks ownership alone (`reviewPanel.tsx` — `mayVote`, `isOwner`), the verdict answers nothing about reviewing, and the candidates read does not exclude them. Neither does anything keep either person out of `suggestedReviewerCollection`, which is the consumer's (rule 2.37). The server half, which decides how the amender becomes known, is item 10.
3. (needs issue) **Gap — Berean's re-request control is ungated.** `renderAIReviewerControl` renders the re-request button to anybody the consumer hands an assignment to, on any status, while the picker it mirrors requires the request tier and a `Submitted` round (rules 2.30, 2.31). Evidence: `reviewPanel.tsx` — `renderAIReviewerControl` checks neither `mayRequest` nor `isSubmitted`.
4. **Note — refetch on reconnect, ruled.** `useApprovalRoundChanges` and the sample page's freshness sample both state "REFETCH ON RECONNECT" and attribute it to this contract (they cite its old number, now §UI20.6.1), but the relocated text (rules 2.8–2.10) did not say it. This item asked whether it should be a rule here. The user ruled on 2026-09-27 that the page's single round refresh — every 15 seconds, on returning to the tab, and on reconnect — keeps the round's review thread current, and that every page showing the thread refreshes it when the connection comes back. Rule 2.8 now requires the consumer to refresh the round on reconnect. The one consumer already does (`useApprovalRoundChanges`, section 6).
5. (needs issue) **The doc page needs updating.** `reviewPanelDoc.tsx` disagrees with the component in five places:
   - "Reviewer who has already voted" says a cast vote "replaces the dropdown with a badge" and "is not a control". While the round is `Submitted` and the viewer is eligible, the dropdown stays live and a change raises `onReviewStatusChanged` (test: "should raise onReviewStatusChanged when the viewer changes their vote"); the badge is only the frozen state (rule 2.17).
   - The "text overrides" row says "Nothing is hard-coded". Four strings are: the two "Loading…" lines and the picker hints "has already reviewed" and "request limit reached" (item 6).
   - The props table has no row for `onApprovalReset`, `resetApprovalText`, the four `resetConfirm…`/`resetCancel…` texts or `bypassReasonRequiredText`, and lists `voteRoles`/`decisionRoles` twice.
   - The `approvalStatus` row says the panel "freezes every control once the round is no longer Submitted"; the reset link appears only then (rule 2.28).
   - The playground's `approvalStatus` radio offers no `Draft`, so the draft block reason (rule 2.21) cannot be shown there.
6. (needs issue) **Gap — four strings are not properties.** Rule 2.51 applies §UI20.6.6 rule 1 (user ruling 2026-09-27). The two "Loading…" lines (rows and picker) have no property. Every other visible string is a property. Two more strings have none, the picker hints "has already reviewed" and "request limit reached": they are visually hidden, and assistive technology reads them as part of a picker row's name (section 9). The user ruled on 2026-09-27 that a string written for a screen reader alone is a property too (§UI20.6.6 rule 1), so they are part of this gap. Evidence: `reviewPanel.tsx` — the `isLoading` branch (line 1338 at 70dc72e7), the `isCandidatesLoading` branch (line 1310), `actionHint` (lines 1082-1085).
7. **Note — reachability.** The only consumer page, `/Admin/Posts/{contentItemId}`, is guarded by `securityPoints.contentItems.view` = `Administrators` (`src/securityMatrix.tsx`), so the Reviewer and Publisher columns of section 3.4 describe the component, not a page anyone in those tiers can reach today. The user expects reviewers to reach that page and place their review in this panel there, and publishers to reach it through Moderate, where the item's other moderation tasks are performed (user rulings 2026-09-26); the gap is the page's, recorded as `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`.
8. **Note — the code cites the old number.** Comments in `useApprovalRound.ts`, `useApprovalRoundChanges.ts` and `reviewPanelDoc.tsx` cite the old number of §UI20.6.1; that heading keeps its *(formerly …)* annotation and becomes a pointer stub to this file, so the citations still resolve.
9. (needs issue) **Gap — the decision and the bypass stay live under the `ReadOnly` veto.** Rule 3.3.5 (user rulings 2026-09-26 and 2026-09-27) requires the decision menu and the bypass checkbox to render disabled beside the stated block for a blocked viewer, each carrying the block-role tooltip of rule 2.53 (§ARC16.7.2) — the decision's part of the read-only-role view of rule 2.50, whose other parts are item 1. The server refuses such a viewer every decision: the veto is asked before a rejection is permitted (`AccessService.cs` — `DecideMayRecordApprovalOutcome`). The panel renders the menu live: it disables Approve through the verdict's `canApprove`, but leaves the Reject option enabled and lets Submit send it (`reviewPanel.tsx` — the Reject option, `maySubmitDecision`). The menu's trigger has no disabled state and no tooltip (`reviewPanel.tsx` — the `mayDecide` block), and the bypass checkbox is not rendered at all, since the verdict answers `isBypassAllowedForCurrentUser` false for such a viewer (`showBypassCheckbox`).
10. (#701) **Server design — nothing records who amended the content, and `UpdatedBy` cannot.** Rule 2.18 keeps whoever amended the item's content from reviewing it and from the review-request candidates (user ruling 2026-09-26). No data records the amender today (§APR8.6 residual 2), and `UpdatedBy` cannot be what does: §APR8.6 ("Why this is not written against `UpdatedBy`") holds that it is audit, not authorization — a single slot restamped by every write, narrow transitions included, so it answers neither "who last changed the content" nor "who has vouched for this text" — and §APR8.6.1 records that the column cannot carry such a bar at any point in the future either. §ARC16.7.4's candidates read subtracts only the owner and the blocked, and §APR7.9 rule 3 refuses an invitation only to those. Excluding whoever amended the content from reviewing and from candidacy is therefore a server-side design change of its own, including what records the amender; the panel renders what the server answers per caller (rule 2.3). Component work does not edit `Approval.md` or `Architecture.md` (§UI20.6.4). That design is #701, `DESIGN: Record Who Amended An Item's Content` (https://github.com/Glory2Him/Glory2Him.Core/issues/701).
11. (needs issue) **Gap — neither loading line is announced.** Rule 2.52 applies §UI20.6.6 rule 5 (user ruling 2026-09-27). The rows' loading line and the picker's are plain paragraphs, with no `role="status"` or other live region; the panel's only status region is the block reasons (section 9). Evidence: `reviewPanel.tsx` — `<p className="small text-muted mb-3">Loading…</p>` (line 1338 at 70dc72e7) and `<p className="small text-muted mb-0 px-3 py-2">Loading…</p>` (line 1310).
12. (needs issue) **Gap — the round is found by the item, not by its approval id (#699).** Every item that implements `IApproval` stores the id of its approval round (§APR7.4 item 6), and every approval-round route is addressed by it: the verdict, the decision, the reset, the reviewer candidates, the reviewer display names, the review requests and Berean's resource (§ARC16.7, §ARC17.5; user rulings of 2026-09-27, designed under #699, https://github.com/Glory2Him/Glory2Him.Core/issues/699, which holds the design alone; the build has no task yet). The decision's body carries three values — the decision, whether a bypass is requested, and the bypass reason — and nothing else; `onApprovalStatusChanged` already raises exactly those three (section 4.2). Section 6 and the section 7 rows state the ruled routes. As relocated, those rows named the routes keyed on the item; the later ruling on the same routes settles them (§UI20.6.4 case 1). Today the one consumer finds the round by the item. `useApprovalRound` asks the verdict by `entityType` and `entityId` and takes `approvalId` off it (`src/hooks/useApprovalRound.ts`, lines 51 and 53 at 70dc72e7), and asks the candidates, the review requests, Berean's status and the display names by the item (lines 64, 67, 74 and 80). The brokers compose the item-keyed routes, the decision's three values in its query string (`src/brokers/apiBroker.approvals.ts` — `PostApprovalDecisionAsync`, lines 146-147, which `PostApprovalDecisionByQueryAsync` replaces under #939 and #969 without changing the query string; `src/brokers/apiBroker.aiReviewers.ts`, line 30). No model that implements `IApproval` carries an approval id yet (§APR7.4 item 6), so the page has none to read until the server work lands. The page's half is `UI/Pages/ContentItemModerationDetailPage.md §6`.
