# 1. ReviewCommentPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** none as story files. Its four faces — `ReviewCommentAddPanel`, `ReviewCommentResultsPanel`, `ReviewCommentViewPanel`, `ReviewCommentEditPanel` — and the `CommentTypeRadioGroup` two of them share are documented inside this file, in sections 3.5 and 6, by the user's ruling (2026-09-26): the file set mirrors the sample pages, and a face is split into a file of its own only if it grows.
- **Composes:** none
- **Used by:** no other component document renders it. Pages: `/Admin/Posts/{contentItemId}` — `src/pages/admin/contentItemModerationDetailPage.tsx`, in the left-hand column beneath the bible references, with [ReviewPanel.md](ReviewPanel.md) in the column beside it.
- **Inherits:** §SEC14.6, §SEC14.7 posture D rule 5, §SEC18.6 rule 2, §APR7.8, §APR8.9 rule 3, §APR8.5, §ARC16.7.2, §ARC16.7.4, §UI20.6.4, §UI20.6.5, §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/approvals/reviewCommentPanel.tsx`, with its faces in the same folder — `reviewCommentAddPanel.tsx`, `reviewCommentResultsPanel.tsx`, `reviewCommentViewPanel.tsx`, `reviewCommentEditPanel.tsx`, `commentTypeRadioGroup.tsx`
- **Sample page:** `/SamplePages/Components/Review-Comment-Panel` — `src/pages/samplePages/components/reviewCommentPanelDoc.tsx`
- **Relocated from:** §UI20.6.3

The round's conversation: the box and its Comment/Question choice, the thread newest-first, the settled tick on asks, and the author's Edit and Delete *(§UI20.6)*. It is the conversation the round is made of — the thing [ReviewPanel.md](ReviewPanel.md) can only report as a count of unresolved comments *(§UI20.6.3)*. There a moderator reads that an approval is held by two outstanding questions; here those questions are read, answered and settled.

A comment is one of two kinds (§APR7.8). A **remark** asks for nothing and never blocks. An **ask** holds the approval shut until it is settled. Most of this panel exists to keep that distinction honest: the writer states which one they are writing, and only an ask can be settled.

## 2. Business Rules

*The contract*

**2.1 [Must]** `ReviewCommentPanel` is a pure presentation component: props in, events out, no fetching, no mutation, no sockets. *(§UI20.6.3)*

**2.2 [Must]** The dispatcher owns everything the faces share — the ordering, the ownership gate, the resolve tier and the `ReadOnly` veto — and the templates render what it decides, the way `ContentItemPanel` is built (`UI/Components/ContentItemPanel.md §6`). *(§UI20.6.3)*

**2.3 [Won't]** No separate add-only comment form. `ApprovalCommentForm` is retired, superseded by this panel, which is the whole thread rather than the box alone: a separate add-only form would have had to re-decide the same three gates. *(§UI20.6)*

*The type and the settled flag*

**2.4 [Must]** The type is stated, not inferred. The add face's radio pair writes `ApprovalCommentType`, and the birth value of `IsResolved` is derived from it — a `Question` is created outstanding and holds the approval shut, a `Comment` is created settled and never blocks. That is §APR7.8's own sentence rather than a rule the client invents: the flag carries no SHAPE rule, so a caller who said nothing would make every remark a blocker. *(§UI20.6.3; user, 2026-09-27)*

**2.5 [Must]** The client is agreeing with the gate rather than being it — the server refuses the settled ask on both the add and the amend path. *(§UI20.6.3)*

**2.6 [Must]** Retyping moves the flag with the type, and only then. The edit face offers the same pair, so an amendment that changes `ApprovalCommentType` re-derives `IsResolved` exactly as birth does — a remark retyped as a question is *outstanding*, a question retyped as a remark is *settled*. *(§UI20.6.3)*

**2.7 [Won't]** An amendment does not send the new type over the stored flag. That was the earlier shape and it could not work: a remark is born settled, so retyping one produced a question already resolved, which is the single pairing the amend gate refuses outright — the save came back a flat refusal and only the words could ever be edited. *(§UI20.6.3)*

**2.8 [Must]** Where the type does not move, the flag is left alone, because a settled ask may be edited by its author and re-deriving would silently re-open it; resolving and re-opening answer to their own operation and its tier (§SEC14.7 posture D rule 5). *(§UI20.6.3)*

*Three gates, and the sanction reaches them differently* *(§UI20.6.3)*

**2.9 [Must]** Adding is the item's review tier — `Reviewers`, `Publishers` and `Administrators`, and the scoped names the entity composes: `{entityType}-Reviewers` and `{entityType}-Publishers`, and `ContentItem-{contentType}-Reviewers` and `ContentItem-{contentType}-Publishers` when the entity is a `ContentItem` and a content type is given (§SEC18.6 rule 5) — and the server checks it (§SEC14.7 posture D rule 5, §APR8.9 rule 3). It is stopped by the global `ReadOnly` and by `ApprovalComment-ReadOnly`, the read-only role scoped to review comments; the read-only roles of the entity under review do not stop it (§SEC14.7 posture D rule 5, §SEC18.6 rule 2). *(§UI20.6.3; user, 2026-09-27)* ≠ items 16 and 19

**2.10 [Must]** Amending and withdrawing are the **author alone**, and only while the author holds the item's review tier (rule 2.9); no role widens them. *(§UI20.6.3; user, 2026-09-27)* ≠ item 19

**2.11 [Must]** The settled tick renders on an **ask only**, to the author, while they hold the item's review tier (rule 2.9), or to the publisher tier for the entity, and is stopped by a `ReadOnly` at any scope the entity composes — because that one control clears a §APR8.5 gate (§SEC18.6 rule 3). *(§UI20.6.3; user, 2026-09-27)* — on the citation, see section 10. ≠ item 19

**2.12 [Must]** Every one of these gates decides rendering only; the foundation re-decides each write against the stored row (§SEC14.6). *(§UI20.6.3)*

*What the consumer owns*

**2.13 [Must]** The consumer owns freshness, and here that is a requirement rather than a note. Two moderators working the same submission is the case this surface exists for, so the collection must be kept moving — by the page's single round refresh, every 15 seconds, on returning to the tab, and on reconnect, which keeps the thread current with the round. Every page that shows the thread refreshes it when the connection comes back. *(§UI20.6.3; user, 2026-09-27)*

**2.14 [Must]** The consumer owns the confirmation: the panel raises which row the reader wants gone, and the page asks "Are you sure?", the same split `ContentItemSettingsPanel` makes for Remove Override (`UI/Components/ContentItemSettingsPanel.md rule 2.17`). *(§UI20.6.3)*

**2.15 [Must]** Every write invalidates the thread **and** the verdict: an outstanding comment is one of the block reasons `ReviewPanel` prints in the column beside it (`UI/Components/ReviewPanel.md rule 2.21`). *(§UI20.6.3)*

**2.16 [Could]** It lands beneath the bible references on `/Admin/Posts/{id}` — in the column with the thing being discussed, not in the decision column, which has to stay readable at a glance while a thread grows without limit. *(§UI20.6.3)*

*Existing behaviour the design relies on*

**2.17 [Must]** An empty `approvalId` means there is no thread — an item with no approval round, or, on today's wiring, a caller the verdict refused (section 10, item 18). The box is withheld from everyone and the panel says why, rather than offering a box that cannot post. *(code: reviewCommentPanel.tsx — `mayComment`; test: reviewCommentPanel.test.tsx — "should offer no box when there is no approval round to speak on")*

**2.18 [Should]** The thread renders newest first, sorted by the panel rather than trusted in the order it arrives. Rows are ordered on the parsed instant, not the text, so two different offsets compare correctly; an unparseable date sorts last. The panel sorts a copy, never the consumer's array. *(code: reviewCommentPanel.tsx — `orderedComments`; test: "should render newest first whatever order the consumer hands it over in")*

**2.19 [Must]** Who wrote a row is an account-id comparison — the row's `authorId` against the signed-in account — never a display name, which two accounts can share. *(code: reviewCommentPanel.tsx — `viewerOwns`)*

**2.20 [Must]** The global `ReadOnly` and `ApprovalComment-ReadOnly` withhold the author's Edit and Delete, and disable the box (rule 3.2.2), because the thread's words are stopped by those two alone (§SEC18.6 rule 2, §SEC14.7 posture D rule 5); a read-only role of the entity under review does not reach them. *(code: reviewCommentPanel.tsx — `mayAmend`, `isBlockedFromCommenting`; user, 2026-09-27)* ≠ items 16 and 21

**2.21 [Must]** The publisher tier the tick admits is `Administrators`, `Publishers`, `{entityType}-Publishers`, and `ContentItem-{contentType}-Publishers` when the entity is a `ContentItem` and a content type is given (§SEC14.7 posture D rule 5, note 2; §SEC18.6 rule 5). With no content type the narrow name is not composed at all, so a half-empty name such as `ContentItem--Publishers` neither grants nor blocks. *(code: reviewCommentPanel.tsx — `scopedNames`, `holdsPublisherTier`; test: "should render on somebody else's question for %s", "should not grant the tick on a name composed from a missing content type", "should not block the tick on a name composed from a missing content type")* ≠ item 7

**2.22 [Must]** A holder of `Reviewers` alone, at any scope, never settles somebody else's ask, and nor does a publisher of another content type: a reviewer who wants to respond writes a comment of their own. *(code: reviewCommentPanel.tsx — `holdsPublisherTier`; test: "should not render on somebody else's question for %s")*

**2.23 [Must]** Settling runs both ways off one control (§APR7.8 rule 3), and the panel does not flip the tick itself: the row is the consumer's, so an unpersisted click must not look settled. *(test: "should raise both directions off one control", "should raise the unsettling direction off a settled question")*

**2.24 [Should]** A blank comment is refused on both writing faces, and the text is capped at 1000 characters — the limit §APR7.8 sets and the foundation enforces. The browser only spares the round trip. *(code: reviewCommentAddPanel.tsx, reviewCommentEditPanel.tsx — `canSave`, `maxLength`; test: "should refuse a blank comment")*

**2.25 [Must]** The box opens on `Comment` by default: most of what is written on a round is an observation, and opening on `Question` would make every absent-minded save block the approval. *(code: reviewCommentPanel.tsx — `defaultType` note)*

**2.26 [Must]** An amendment is raised as the whole row with only the words and the type moved, because the foundation pins `CreatedBy`, `CreatedWhen`, `ApprovalId` and `UpdatedWhen` against storage. The edit face never moves `IsResolved`. *(code: reviewCommentEditPanel.tsx — `save`; test: "should swap the row for the edit template and commit the changed row")*

**2.27 [Must]** Delete raises `onRemoveRequested` when the consumer listens on it, and `onRemoved` only when it does not — never both, so one click is one event. *(code: reviewCommentViewPanel.tsx — `requestRemoval`; test: "should raise the removal as an INTENT so the consumer can ask first", "should remove directly for a surface with nothing to ask")*

**2.28 [Must]** A comment's words are rendered as text, never as markup: the thread is written by anybody who can contribute. *(code: reviewCommentViewPanel.tsx — the comment paragraph)*

**2.29 [Must]** The box clears on a committed save and only then. When `onSave` returns a promise the add face waits on it; a rejected save leaves the words where the reader typed them, because the draft is the payload and nothing else could recover it. *(code: reviewCommentAddPanel.tsx — `save`; test: "should keep the draft when the consumer rejects the save", "should clear the box only after an awaited save resolves")*

*Questions, and the rules every presentation component follows*

**2.30 [Must]** A Question starts unresolved — outstanding (rule 2.4). Other reviewers answer it with follow-on comments of their own, which are further rows in the same thread (rule 2.22). Only the question's author, the publisher tier and `Administrators` may mark it resolved, through the tick (rules 2.11, 2.21 and 3.2.4) — the author only while they hold the item's review tier, since that tier gates every write to the thread, marking a Question resolved included (rule 2.9; user ruling 2026-09-27). The resolve operation is §SEC14.7 posture D rule 5's; the code's is `mayResolve`, whose author branch asks no tier (item 19). The design adds what the ruling does not state: a read-only role at any scope the entity composes withholds the tick from all three (rule 3.3.2). Re-opening a question runs off the same control, for the same people (rule 2.23). *(user, 2026-09-27; §SEC14.7 posture D rule 5; code: reviewCommentPanel.tsx — `mayResolve`)* ≠ item 19

**2.31 [Must]** Every visible string the family renders, and every string it renders for a screen reader alone, is a property, whose default is today's text, so a page need set none and may override any (§UI20.6.6 rule 1). *(§UI20.6.6 rule 1)* ≠ item 12

**2.32 [Should]** Both of the thread's loading states are announced as a status (§UI20.6.6 rule 5): the first page's spinner (rule 3.5.6) and the further page's (rule 3.5.8). *(§UI20.6.6 rule 5; code: reviewCommentResultsPanel.tsx — the `isLoading` branch, `renderPagingFoot`; code: spinner.tsx — `role="status"`)*

**2.33 [Should]** While `isSubmitting` is on, the write in flight is announced as a status, as a loading state is (§UI20.6.6 rule 5). *(§UI20.6.6 rule 5; user, 2026-09-27)* ≠ item 17

**2.34 [Won't]** No sign-in hook is added for a signed-out reader. The thread lives in the admin area, behind the sign-in guard, so an anonymous reader never reaches it, and the signed-out line of rule 3.2.1 is not a way in. *(user, 2026-09-27)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `approvalId=""` replaces the box with "This submission has no approval round, so there is nothing to comment on yet." (rule 2.17). The thread below still renders. *(code: reviewCommentPanel.tsx — the `mayComment === false` message)*

**3.1.2** `reviewComments` empty shows `emptyText` (default "Nothing has been said about this submission yet."). *(test: "should say so rather than render an empty list when nothing has been said")*

**3.1.3** `isLoading=true` holds the thread back behind a spinner rather than emptying it; `isLoadingMore`, `hasMore` and `onLoadMore` drive the scroll (section 3.5). *(test: "should hold the thread back behind a spinner rather than emptying it")*

**3.1.4** `isSubmitting=true` disables every button, both text boxes, the radios and the tick, so one click is one write. Nothing is hidden. *(code: every face — `disabled={isSubmitting}`; test: "should freeze every control while the consumer is persisting")*

**3.1.5** `showBorder=true`, the default, draws the panel as a bordered card, because it stands under other panels in a column. *(code: reviewCommentPanel.tsx — `showBorder` note, `panelCssClass`)*

**3.1.6** `titleText` is the heading and the panel's accessible name. *(test: "should name the region from its title, and take that name from titleText")*

**3.1.7** A row's `commentType` decides its chip and whether the tick can appear at all: never on a `Comment` (rule 2.11). *(test: "should never render on a comment, whoever is looking")*

### 3.2 Driven by roles

**3.2.1** Signed out, the box is replaced by "Sign in to comment on this submission.", which raises no hook (rule 2.34). *(code: reviewCommentPanel.tsx; test: "should offer no box to a signed-out reader, and say why"; user, 2026-09-27)*

**3.2.2** Holding the global `ReadOnly` or `ApprovalComment-ReadOnly`, whatever tier they hold, the reader is shown the box disabled, not hidden: the text box and Save each carry the block-role tooltip, a property whose default is "User belongs to a block role" — the pattern of the review panel's disabled controls (`UI/Components/ReviewPanel.md rule 2.53`). Their own Edit and Delete disappear (rule 2.20). *(user, 2026-09-27)* ≠ items 16 and 21

**3.2.3** Edit and Delete render on the reader's own rows only (rule 2.10), including for an administrator looking at somebody else's, and only while the reader holds the item's review tier (rule 2.9). *(test: "should offer Edit and Delete to the author alone", "should offer neither to an administrator on somebody else's row"; user, 2026-09-27)* ≠ item 19

**3.2.4** The tick renders on an ask to its author, while they hold the item's review tier (rule 2.9), or to the publisher tier of rule 2.21; a holder of `Reviewers` alone, at any scope, never gets it on somebody else's ask (rule 2.22). *(test: "should render on a question for its author"; user, 2026-09-27)* ≠ item 19

**3.2.5** Any `ReadOnly` the entity composes — `ReadOnly`, `{entityType}-ReadOnly`, and `ContentItem-{contentType}-ReadOnly` on a `ContentItem` (§SEC18.6 rule 5) — withholds the tick, the author's own included. *(test: "should withhold the tick from a publisher sanctioned by %s")* ≠ item 7

**3.2.6** A signed-in reader outside the item's review tier who holds neither block of rule 3.2.2 is shown the box disabled, not hidden, whatever else they hold (rule 2.9): the text box and Save each carry a tooltip saying why, a property whose default is "Only this item's reviewers can comment". *(user, 2026-09-27)* ≠ items 19 and 21

### 3.3 Combinations

**3.3.1** A missing round outranks everything for the box: with no `approvalId` nobody gets one, whatever they hold. *(code: reviewCommentPanel.tsx — `mayComment`)*

**3.3.2** The block is asked before the grant on the tick: a publisher, an administrator or the author holding a `ReadOnly` at any scope the entity composes gets no tick (§SEC14.7 posture D rule 5, note 2). *(code: reviewCommentPanel.tsx — `mayResolve`)*

**3.3.3** A read-only role of the entity under review — `{entityType}-ReadOnly`, or `ContentItem-{contentType}-ReadOnly` on a `ContentItem` — reaches the tick and nothing else. Its holder keeps what the review tier gives them — the box, and Edit and Delete on their own rows (rules 2.9, 2.10 and 2.20). `ApprovalComment-ReadOnly` disables the box and withholds Edit and Delete (rules 2.9, 2.20 and 3.2.2); the tick's block set is rule 3.2.5's, unchanged. *(code: reviewCommentPanel.tsx — `isBlockedFromCommenting`, `isBlockedFromResolving`; user, 2026-09-27)* ≠ items 16, 19 and 21

**3.3.4** Ownership is a property of the row, so one reader can see Edit and Delete on one row and not on the next. The gates are decided once by the dispatcher and asked per row by the faces. *(code: reviewCommentResultsPanel.tsx — `mayAmend`, `mayResolve` note)*

**3.3.5** `isSubmitting` disables what the roles grant; it never hides it (rule 3.1.4). *(code: every face — `disabled={isSubmitting}`)*

### 3.4 Role matrix

Owner here is the author of the row on screen — the account whose id matches its `authorId` — holding no tier. Publisher holds the publisher tier of rule 2.21; Reviewer holds `Reviewers` at any scope the entity composes (rule 2.9). Every row assumes an `approvalId` unless it says otherwise.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| **The thread's rows** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ |
| **The box** (Comment/Question, Clear, Save), live ≠ item 19 | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer outside the item's review tier, holding neither block of rule 3.2.2 — **the box rendered disabled**, with the tooltip of rule 3.2.6 ≠ items 19 and 21 | ❌ No | ✅ Yes⁴ | ✅ Yes⁴ | ➖ n/a | ➖ n/a | ➖ n/a |
| `approvalId=""` — **the box** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Global `ReadOnly` or `ApprovalComment-ReadOnly` — **the box**, live ≠ item 16 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| As the row above — **the box rendered disabled**, with the block-role tooltip ≠ items 16 and 21 | ➖ n/a | ✅ Yes⁴ | ✅ Yes⁴ | ✅ Yes⁴ | ✅ Yes⁴ | ✅ Yes⁴ |
| A read-only role of the entity under review only — **the box**, live ≠ item 19 | ➖ n/a | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| **Edit / Delete** on the row ≠ item 19 | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Viewer wrote the row and holds the column's tier — **Edit / Delete** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Global `ReadOnly` or `ApprovalComment-ReadOnly` — **Edit / Delete** ≠ item 16 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| A read-only role of the entity under review only, viewer wrote the row and holds the column's tier — **Edit / Delete** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Row is a `Question` — **settled tick** ≠ item 19 | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes² | ✅ Yes |
| Row is a `Question` the viewer wrote, and they hold the column's tier — **settled tick** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| Row is a `Comment` — **settled tick** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Row is a `Question`, viewer holds a `ReadOnly` at any scope the entity composes — **settled tick** | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `isSubmitting=true` — **every control** | ❌ No | ❌ No³ | ❌ No³ | ❌ No³ | ❌ No³ | ❌ No³ |

¹ The panel renders whatever thread it is handed; what a caller may read is the foundation's (§SEC14.7 posture D rule 1).
² At `Publishers`, `ContentItem-Publishers` or the item's own `ContentItem-{ContentType}-Publishers`; a publisher of another content type gets no tick.
³ Rendered, and disabled.
⁴ Rendered, and disabled: the text box and Save each carry the tooltip, and nothing is raised (rules 3.2.2 and 3.2.6).

### 3.5 The faces

The faces decide nothing: every gate reaches them as a boolean or a per-row predicate from the dispatcher (rule 2.2), so no face contains a role name or an ownership comparison. *(code: reviewCommentViewPanel.tsx — header note; reviewCommentResultsPanel.tsx — `mayAmend` note)*

*`ReviewCommentAddPanel` — the box*

**3.5.1** Renders live only when the dispatcher allows a comment (rules 2.9, 2.17). It renders disabled, its text box and Save each carrying a tooltip, for a holder of a block role and for a signed-in reader outside the item's review tier (rules 3.2.2 and 3.2.6). Otherwise — no round, or signed out — the dispatcher says why in its place (rules 3.1.1 and 3.2.1). *(code: reviewCommentPanel.tsx — `mayComment`; user, 2026-09-27)* ≠ items 16, 19 and 21

**3.5.2** A three-line text box, the Comment/Question radios opening on `defaultType`, and Clear and Save. Save is disabled while the box is blank or `isSubmitting` is on. *(code: reviewCommentAddPanel.tsx — `canSave`; test: "should refuse a blank comment")*

**3.5.3** Clear empties the box, returns the radios to `defaultType` and raises `onClear`. *(test: "should open on defaultType and put Clear back to it")*

**3.5.4** Save raises `onSave` with the approval id, the trimmed words and the chosen type, then clears the box and returns the radios to `defaultType` once the save is committed (rule 2.29). *(test: "should save the words, the chosen type and the approval id", "should clear the box once a save has been raised")*

**3.5.5** A changed `defaultType` resets a radio choice the reader has not saved, and never the words. *(code: reviewCommentAddPanel.tsx — the `defaultType` effect)*

*`ReviewCommentResultsPanel` — the rows*

**3.5.6** While `isLoading` is on, a spinner and `loadingText` replace the rows. *(code: reviewCommentResultsPanel.tsx — the `isLoading` branch)*

**3.5.7** An empty collection shows `emptyText` and still renders the paging foot beneath it, so a thread whose first page came back empty can still load. *(code: reviewCommentResultsPanel.tsx — the empty branch, `renderPagingFoot`)*

**3.5.8** With `hasMore` on, a one-pixel sentinel below the rows asks for the next page when it scrolls into view; while `isLoadingMore` is on, a spinner and `loadingMoreText` show beneath the rows and the sentinel waits; where `IntersectionObserver` is unavailable a `loadMoreButtonText` button asks instead. The sentinel is shared with `ContentItemResultsPanel` through `useInfiniteScrollSentinel`. *(code: reviewCommentResultsPanel.tsx — `renderPagingFoot`; test: "should offer a load-more button where IntersectionObserver is unavailable")*

**3.5.9** Each row renders the view face, except the one row being edited, which renders the edit face. One row is open at a time, and which one is local state the consumer never persists. *(code: reviewCommentResultsPanel.tsx — `editingCommentId`)*

**3.5.10** A committed edit closes the editor before `onModified` is raised; Cancel closes it; a row that leaves the collection closes its editor. What the row then shows is the consumer's item. *(code: reviewCommentResultsPanel.tsx — `onModified` wrapper, the `stillPresent` effect; test: "should restore the row exactly as it was when the edit is cancelled")*

*`ReviewCommentViewPanel` — a row, read*

**3.5.11** The author's display name, then their username in brackets only when there is one, then the chip — "Question" or "Comment" — and on the right the date and time written; an unparseable date shows nothing. *(test: "should name the author, chip the type and print the date AND the time", "should draw no brackets for an author whose account no longer resolves")*

**3.5.12** The words, as typed (rule 2.28).

**3.5.13** The tick, when `showsResolveControl` is on: checked from the row's `IsResolved`, raising `onResolvedChanged` with the new value (rule 2.23).

**3.5.14** Edit and Delete, when `showsAmendActions` is on. Edit opens the edit face; Delete raises the removal (rule 2.27). *(test: "should raise the removal as an INTENT so the consumer can ask first")*

*`ReviewCommentEditPanel` — a row, edited*

**3.5.15** A labelled text box ("Edit your comment") seeded with the row's words, the radios seeded with the row's type, and Cancel and Save. Save is disabled while the box is blank or `isSubmitting` is on. Nothing is persisted until Save. *(code: reviewCommentEditPanel.tsx; test: "should swap the row for the edit template and commit the changed row")*

**3.5.16** Cancel restores the row exactly as it was: the view face re-renders from an item the edit face never touched. *(test: "should restore the row exactly as it was when the edit is cancelled")*

*`CommentTypeRadioGroup` — the choice both writing faces share*

**3.5.17** Two options, Comment and Question, disabled with its face. *(code: commentTypeRadioGroup.tsx)*

## 4. Properties and Events

### 4.1 Properties

The dispatcher's own:

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `approvalId` | `string` | `''` | The round every comment is tied to; empty means no thread (rule 2.17). | add face `approvalId` |
| `reviewComments` | `ReviewCommentItem[]` | `[]` | The thread, already projected with author names. | results face `reviewCommentCollection`, after sorting (rule 2.18) |
| `defaultType` | `ApprovalCommentType` | `Comment` | Which radio the box opens on and what Clear returns it to (rule 2.25). | add face `defaultType` |
| `entityType` | `string` | `'ContentItem'` | Composes the resolve tier and the scoped `ReadOnly` names. Fetches nothing. | — (decided here) |
| `contentType` | `string` | `''` | The narrow tier; the enum member name, never the editable type name. | — (decided here) |
| `isLoading`, `isLoadingMore`, `hasMore` | `boolean` | `false` | The collection's states. | results face, same names |
| `onLoadMore` | `() => void` | — | The next page. | results face `onLoadMore` |
| `isSubmitting` | `boolean` | `false` | Freezes every control (rule 3.1.4). | add face and results face; view and edit faces |
| `emptyText` | `string` | `'Nothing has been said about this submission yet.'` | The empty thread. | results face `emptyText` |
| `showBorder` | `boolean` | `true` | Bordered card (rule 3.1.5). | — |
| `cssClass` | `string` | `''` | Appended to the panel's class list. | — |
| `titleText` | `string` | `'Review Comments'` | The heading and the accessible name. There is deliberately no `ariaLabel` (section 9). | — |
| The block-role tooltip — its name is fixed by the task that builds it ≠ item 21 | `string` | `'User belongs to a block role'` | The tooltip on the disabled box's text box and Save for a holder of a block role (rule 3.2.2). | add face |
| The review-tier tooltip — its name is fixed by the task that builds it ≠ item 21 | `string` | `"Only this item's reviewers can comment"` | The tooltip on the disabled box's text box and Save for a signed-in reader outside the item's review tier (rule 3.2.6). | add face |

The faces' own, for section 4.3:

| Face | Property | Type | Default |
| --- | --- | --- | --- |
| `ReviewCommentAddPanel` | `approvalId` | `string` | required |
| | `defaultType` | `ApprovalCommentType` | `Comment` |
| | `isSubmitting` | `boolean` | `false` |
| | `placeholderText` | `string` | `'Write a comment or ask a question…'` |
| | `maxLength` | `number` | `1000` |
| `ReviewCommentResultsPanel` | `reviewCommentCollection` | `ReviewCommentItem[]` | `[]` |
| | `mayAmend`, `mayResolve` | `(item) => boolean` | `() => false` |
| | `isLoading`, `isLoadingMore`, `hasMore`, `isSubmitting` | `boolean` | `false` |
| | `onLoadMore` | `() => void` | — |
| | `loadingText`, `loadingMoreText`, `loadMoreButtonText` | `string` | `'Loading…'`, `'Loading more…'`, `'Load more'` |
| | `emptyText` | `string` | required, no default |
| `ReviewCommentViewPanel` | `reviewComment` | `ReviewCommentItem` | required |
| | `showsAmendActions`, `showsResolveControl`, `isSubmitting` | `boolean` | `false` |
| | `onEditClick` | `() => void` | — |
| `ReviewCommentEditPanel` | `reviewComment` | `ReviewCommentItem` | required |
| | `isSubmitting` | `boolean` | `false` |
| | `maxLength` | `number` | `1000` |
| | `onCancelled` | `() => void` | — |
| `CommentTypeRadioGroup` | `value`, `onChange` | `ApprovalCommentType`, `(type) => void` | required |
| | `disabled` | `boolean` | `false` |
| | `groupLabel` | `string` | `'Comment type'` |

### 4.2 Events

The family's events are declared once (`ReviewCommentEvents`) so every face declares the same ones and a consumer wires them once. Each is a notification; nothing persists anything.

| Event | Payload | Raised when |
| --- | --- | --- |
| `onSave` | `ReviewCommentDraft` — `approvalId`, trimmed `comment`, `commentType` | Save on a non-blank box. May return a promise, which the box waits on (rule 2.29). |
| `onClear` | none | Clear; the box empties and the radio returns to `defaultType` internally. |
| `onModified` | the whole `ReviewCommentItem`, words and type moved | Save on the edit face; the editor has already closed (rule 2.26). |
| `onRemoveRequested` | `ReviewCommentItem` | Delete, when the consumer listens on this (rule 2.27). |
| `onRemoved` | `ReviewCommentItem` | Delete, only when nobody listens on `onRemoveRequested` (rule 2.27). |
| `onResolvedChanged` | `ReviewCommentItem`, `isResolved: boolean` | The tick is changed, in either direction (rule 2.23). |

### 4.3 Pass-through properties

Per §UI20.6.5. The dispatcher is the parent of the four faces; the results face is the parent of the view and edit faces; the add and edit faces each render a `CommentTypeRadioGroup`.

| Reaches | Face property | From | Unchanged? |
| --- | --- | --- | --- |
| add face | `approvalId`, `defaultType`, `isSubmitting`, `onSave`, `onClear` | the dispatcher's same-named properties | yes |
| results face | `isLoading`, `isLoadingMore`, `hasMore`, `onLoadMore`, `isSubmitting`, `emptyText`, `onModified`, `onRemoved`, `onRemoveRequested`, `onResolvedChanged` | the dispatcher's same-named properties | yes |
| results face | `reviewCommentCollection` | `reviewComments` | sorted newest first (rule 2.18) |
| results face | `mayAmend`, `mayResolve` | decided by the dispatcher from the roles, `entityType` and `contentType` | decided, not forwarded |
| view face | `reviewComment`, `isSubmitting`, `onRemoved`, `onRemoveRequested`, `onResolvedChanged` | the results face | yes |
| view face | `showsAmendActions`, `showsResolveControl` | `mayAmend(row)`, `mayResolve(row)` | decided per row |
| view face | `onEditClick` | the results face's own editor state | internal |
| edit face | `reviewComment`, `isSubmitting` | the results face | yes |
| edit face | `onModified` | the dispatcher's `onModified`, wrapped to close the editor first | wrapped |
| edit face | `onCancelled` | the results face's own editor state | internal |
| `CommentTypeRadioGroup` | `value`, `onChange`, `disabled` | the face's own draft state and `isSubmitting` | internal |

What the page cannot reach today — each a gap in section 10:

- add face `placeholderText`, `maxLength`
- results face `loadingText`, `loadingMoreText`, `loadMoreButtonText`
- edit face `maxLength`
- `CommentTypeRadioGroup` `groupLabel`, on both writing faces

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

The panel composes two sets of read-only roles (§SEC18.6 rule 2, §SEC14.7 posture D rule 5). Every write to the thread — the box, Edit and Delete, and the tick — is the item's review tier's alone (rules 2.9–2.11; user rulings 2026-09-27), and a signed-in reader outside the tier is shown the box disabled (rule 3.2.6). The thread's words — the box, Edit and Delete — are withheld by the global `ReadOnly` and by `ApprovalComment-ReadOnly`, the box shown disabled (rule 3.2.2), and by no read-only role of the entity under review. The tick is withheld by `ReadOnly`, `{entityType}-ReadOnly`, and `ContentItem-{contentType}-ReadOnly` on a `ContentItem`. A comment is not an approvable entity (§APR7.5) but an approval workflow record (§SEC14.7 posture D), so the lock of §APR9.9 does not reach Delete.

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The thread** (a view) | Everyone the consumer renders the panel for (section 3.4, note ¹) | None: a read | ✅ Allowed — the same thread | ✅ Shown, if the consumer hands them one | §SEC14.7 posture D rule 1: the records are never public |
| **The box** — the Comment/Question choice, Clear and Save, raising `onSave` and `onClear` ≠ items 16, 19 and 21 | The item's review tier, where the consumer hands an `approvalId` (rules 2.9, 2.17); a signed-in reader outside the tier is shown it disabled, with the tooltip of rule 3.2.6 | The global `ReadOnly` and `ApprovalComment-ReadOnly` (rule 2.9) | ❌ Refused under either — the box shown disabled, its text box and Save each carrying the block-role tooltip (rule 3.2.2); ✅ Allowed under a read-only role of the entity under review (rule 3.3.3) | Not offered — "Sign in to comment on this submission." in its place, raising no hook (rules 3.2.1 and 2.34) | §SEC14.7 posture D rule 5, the item's review tier; §SEC18.6 rule 2, the thread's words |
| **Edit** on one's own row, opening the edit face, whose Save raises `onModified` ≠ items 16 and 19 | The row's author alone, while they hold the item's review tier (rules 2.10, 3.2.3) | The global `ReadOnly` and `ApprovalComment-ReadOnly` (rule 2.20) | ❌ Refused under either — no Edit (rule 3.2.2); ✅ Allowed under a read-only role of the entity under review (rule 3.3.3) | Not offered — a signed-out reader owns no row (rule 2.19) | §SEC14.7 posture D rule 5: the item's review tier, and the owner alone |
| **Delete** on one's own row, raising `onRemoveRequested` or `onRemoved` ≠ items 16 and 19 | The row's author alone, while they hold the item's review tier (rules 2.10, 2.27) | The global `ReadOnly` and `ApprovalComment-ReadOnly` (rule 2.20) | ❌ Refused under either — no Delete (rule 3.2.2); ✅ Allowed under a read-only role of the entity under review (rule 3.3.3) | Not offered (rule 2.19) | §SEC14.7 posture D rule 5: the item's review tier, and the owner alone |
| **The settled tick**, raising `onResolvedChanged` ≠ items 7 and 19 | On a Question only: its author, while they hold the item's review tier, and the publisher tier with `Administrators` (rules 2.11, 2.21, 2.30) | `ReadOnly`, `{entityType}-ReadOnly`, and `ContentItem-{contentType}-ReadOnly` on a `ContentItem` (rule 3.2.5) | ❌ Refused — no tick, the author's own included (rule 3.3.2) | Not offered | §SEC14.7 posture D rule 5 and its note 2: the resolve operation, with the veto asked first |
| **Load more** — the button offered where `IntersectionObserver` is unavailable, raising `onLoadMore` | Everyone who sees the thread, while `hasMore` is on (rule 3.5.8) | None: a read | ✅ Allowed | ✅ Offered, if the consumer hands them a thread | §SEC14.7 posture D rule 1 |

1. Every gate decides rendering only; the foundation re-decides add, modify, remove and resolve against the stored row (rule 2.12, §SEC14.6). A hidden control is a courtesy, never an authorization boundary.
2. Ownership is matched on account id (rule 2.19), from the signed-in account in the auth context — never a property the page could set.
3. Every write is the item's review tier's; the words answer to the global `ReadOnly` and `ApprovalComment-ReadOnly`; the tick answers to a `ReadOnly` at every scope the entity composes, asked first (§SEC18.6 rule 2, §SEC14.7 posture D rule 5). Rules 2.9–2.11 and 2.20 apply that split.
4. The tick is a Question's author's, while they hold the item's review tier, or the publisher tier's with `Administrators` (rules 3.2.4 and 2.21); a holder of `Reviewers` alone, at any scope, never gets it on somebody else's ask (rule 2.22). The publisher tier holds it because an outstanding comment holds the approval shut, and the people that block stops are the people who decide the approval (§SEC14.7 posture D rule 5, note 2).
5. The words are rendered as text (rule 2.28).
6. The thread and its author names reach the page only through reads that are never public (§SEC14.7 posture D rule 1) and the round-keyed name resolver (§ARC16.7.4).

## 6. Composition and Usage

**The family** *(§UI20.6.3)*:

```
ReviewCommentPanel                 the thread, and who may do what to it
├── ReviewCommentAddPanel          the box, the Comment/Question radios, Clear / Save
└── ReviewCommentResultsPanel      the rows, newest first, scrolled rather than paged
    ├── ReviewCommentViewPanel     READ:  author, chip, timestamp, resolve tick, Edit / Delete
    └── ReviewCommentEditPanel     EDIT:  the words and the type, Save / Cancel
```

`CommentTypeRadioGroup` is the one control the two writing faces share, so that it means the same thing on each. *(code: commentTypeRadioGroup.tsx)*

- **Where it lands.** Rule 2.16. On `/Admin/Posts/{contentItemId}` it follows the item, `TagAssociationPanel` and `BibleReferenceAssociationPanel` in the left-hand column; [ReviewPanel.md](ReviewPanel.md) holds the right-hand column. *(code: contentItemModerationDetailPage.tsx)*
- **Beside `ReviewPanel`.** The two are one round: the thread here is the outstanding-comment block reason there, and every write here moves that reason (rule 2.15).
- **Wiring on the one consumer.** `approvalId` is the id the item stores for its round (§APR7.4 item 6; ruled 2026-09-27 under #699, not yet built) — today it comes off the verdict, as the example below shows (section 10, item 18); `reviewComments` is `useApprovalRound`'s `reviewCommentCollection`; `isSubmitting` is any of the four comment writes pending; `onSave` is passed by reference so the box can wait on it, and rethrows after its toast; `onRemoveRequested` opens the page's `ConfirmDialog`. *(code: contentItemModerationDetailPage.tsx, src/hooks/useApprovalRound.ts)*

```tsx
<ReviewCommentPanel
    approvalId={approvalVerdict?.approvalId ?? ''}
    reviewComments={reviewCommentCollection}
    entityType="ContentItem"
    contentType={ContentType[contentItem.contentType] ?? ''}
    isLoading={areReviewCommentsLoading}
    isSubmitting={addReviewComment.isPending || modifyReviewComment.isPending
        || removeReviewComment.isPending || resolveReviewComment.isPending}
    onSave={saveReviewCommentAsync}
    onModified={(item) => void modifyReviewCommentAsync(item)}
    onRemoveRequested={setCommentToRemove}
    onResolvedChanged={(item, isResolved) => void resolveReviewCommentAsync(item, isResolved)}
    showBorder />
```

## 7. Dependencies

**Direct API dependencies** (called by the consumer, never the component) *(§UI20.6.3)*:

| Concern | Endpoint |
| --- | --- |
| The thread | `GET api/ApprovalComments` filtered by `ApprovalId` and `IsDeleted eq false` |
| A new comment | `POST api/ApprovalComments` — the id minted client-side, the audit values stamped server-side |
| An amend | `PUT api/ApprovalComments` — the whole row that was read, since four fields are pinned against storage |
| A withdrawal | `DELETE api/ApprovalComments/{id}?deletionReason=` — the SOFT delete, which is what leaves the §APR8.5 block |
| The settled flag | `POST api/ApprovalComments/{id}/Resolve?isResolved=` — always sent, since the endpoint binds it required |
| The approval id, and the author names ≠ item 18 | The id the item stores for its round (§APR7.4 item 6), and the §ARC16.7.4 resolver, `GET api/Approvals/{approvalId}/ReviewerDisplayNames` (§ARC17.5), already read for [ReviewPanel.md](ReviewPanel.md) |

Every write invalidates the thread and the verdict (rule 2.15). On the one consumer it also invalidates the name resolver, because a first comment can bring an author into the round whose name the resolver has not yet carried. *(code: approvalCommentService.ts — `invalidateThread`)*

**Indirect dependencies:** the signed-in identity and roles through the auth context, for every gate.

## 8. States, Validation and Feedback

- **Loading.** Two states, both announced (rule 2.32). The first page shows a spinner in place of the thread (rule 3.1.3), and the spinner is a status region; a further page shows a spinner beneath the rows, inside a status region (rules 3.5.6 and 3.5.8).
- **Empty.** `emptyText` as a status line; a pending first page can still load (rule 3.5.7).
- **No box.** One of two reasons is said in its place — no round, signed out (rules 3.1.1 and 3.2.1).
- **Disabled box.** A holder of a block role, and a signed-in reader outside the item's review tier, see the box disabled, the reason in the tooltip on its text box and Save (rules 3.2.2 and 3.2.6); today neither is built (section 10, item 21).
- **Submitting.** `isSubmitting` freezes every control (rule 3.1.4). The write in flight must be announced (rule 2.33); today nothing announces it (section 10, item 17).
- **Validation.** Blank is refused and 1000 characters is the ceiling (rule 2.24); Save stays disabled until the box holds text. Every other refusal is the server's, reported by the consumer — the one consumer toasts the API's own reason.
- **Failed save.** The add face keeps the words (rule 2.29); the edit face has already closed, and the consumer reports the failure.
- **Confirmation.** The page's (rule 2.14): the one consumer asks "Are you sure?" with "This comment will be removed from the review thread. This action cannot be undone." What rule 2.14 shares with `ContentItemSettingsPanel` is the split, not the wording: that page's dialog is titled "Remove override?" (`UI/Components/ContentItemSettingsPanel.md §8`).
- **Freshness.** Rule 2.13.

## 9. Styling and Accessibility

- **Class hooks** (`approvals.css`): `g2h-review-comment-panel`, `g2h-review-comment-add`, `g2h-review-comment-row` (a rule between rows, none under the last), `g2h-review-comment-text` (`pre-wrap`, so typed paragraphs survive), `g2h-review-comment-sentinel`.
- **Chips** use `text-bg-primary` for a question and `text-bg-secondary` for a remark, so the foreground follows the theme.
- **Region.** The panel is a `section` named by its own heading. There is deliberately no `ariaLabel` property: `aria-labelledby` outranks `aria-label`, so one would be settable and change nothing; a page telling two threads apart sets `titleText`. *(code: reviewCommentItem.ts — `ReviewCommentText` note)*
- **Radios.** `CommentTypeRadioGroup` is a `radiogroup` named "Comment type", so it announces "Comment, 1 of 2" and the arrow keys move within it; every instance mints its own name, so the box and an open editor are two groups, not one of four.
- **Row actions.** Edit and Delete are named for their row — "Edit comment by {author}, {date and time}", the name alone when the date will not parse — so two rows by one author are told apart. The fixed words of those names are not properties yet (section 10, item 12). *(code: reviewCommentViewPanel.tsx — `rowName`; test: "should give each row a distinct label when one author wrote two")*
- **The disabled box.** A disabled control takes no keyboard focus, and a tooltip is not announced, so a tooltip alone reaches neither a keyboard user nor a screen reader. Each of the disabled box's text box and Save therefore carries its reason twice (rules 3.2.2 and 3.2.6): as its tooltip, restored on hover over the disabled control, and in what a screen reader announces for the control, which lists it with the reason although it takes no focus — the pattern of the card's locked action (`UI/Components/ContentItemPanel.Default.md §9`). Both are the one tooltip property's text (§UI20.6.6 rule 1). Not built (section 10, item 21).
- **The tick's label** carries both halves, "Is resolved — I got my answer", so the explanation is announced and clickable.
- **Dates** render in a `time` element carrying the stored value; an unparseable one renders nothing rather than "Invalid Date".
- **Live text.** The empty line and the further-page spinner are `role="status"`; the scroll sentinel is `aria-hidden` and a pixel tall, because an observer over a zero-area target is unreliable.

## 10. Open Questions and Gaps

1. **Note — where the flag is derived, ruled.** Rule 2.4 used to word the birth value of `IsResolved` as derived by the add face's radio pair, and rule 2.6 has an amendment that changes the type re-derive it. The faces never derived it, and both rules were written beside code that put the derivation in the consumer — for the birth value the service, for the retype the page, until `33313e8a` moved it into the service. Rule 2.4 arrived in `1aad51fb` (2026-09-06 20:58:01 +01:00), ten seconds after `4c434e80` (20:57:51) put the birth mapping in `approvalCommentService.ts` with the comment "this is the one place that mapping lives". Rule 2.6 arrived in `7db5462f` (2026-09-07), the commit that put the retype derivation in the page, whose comment there says "The add face derives IsResolved". At both `4c434e80` and `7db5462f` the add face never reads `isResolved` and the edit face says it is "NOT touched here", and rule 2.6's subject is an amendment, not the edit face. `33313e8a` (2026-09-07) then removed the page's second copy, leaving `approvalCommentService.ts` — `isSettledForType`, called by `useAddApprovalComment` at birth and by `useModifyApprovalComment` when the type moves — as the one home of the mapping. Its comment (lines 23-31 at 70dc72e7) calls it "the ONE place that mapping lives". Asked whether the design should say that the consumer's service derives the flag, the user ruled on 2026-09-27 on the behaviour: a submitted Question starts unresolved (rule 2.30). Rule 2.4 now states that behaviour without naming a face as what derives the flag. Rule 2.6 stands as written, because its subject is the amendment. The faces send the type and never move `IsResolved` (rules 2.26 and 3.5.4), and the consumer's service derives the flag today.
2. **Note — where the polling runs, ruled.** Rule 2.13, as relocated, named `approvalCommentService.useGetApprovalComments` as what polls and refetches on focus. It does not poll. The thread is kept moving by the round's freshness channel instead — `useApprovalRound`'s `refresh`, driven every 15 seconds by `useApprovalRoundChanges`, and on returning to the tab and on reconnect. The code gives its reason (`approvalCommentService.ts` lines 14-20 at 70dc72e7): "THE POLLING IS NOT HERE", because the round's one freshness channel "does not poll a hidden tab, it refreshes on reconnect, and its refetches join an in-flight read instead of cancelling and reissuing it", and "One channel is also one thing to replace when SignalR lands." This item asked whether rule 2.13 should name the round's freshness channel, or `useGetApprovalComments` should poll as the rule said. The user ruled on 2026-09-27 that the design is reworded: the page's single round refresh — every 15 seconds, on returning to the tab, and on reconnect — keeps the review thread current, and every page that shows the thread refreshes it when the connection comes back. Rule 2.13 now says so (§UI20.6.4 case 1), and the one consumer already behaves so. The doc page's copy of the old wording is item 5.
3. (needs issue) **Citation — "§SEC18.6 rule 3" in rule 2.11.** Rule 3 of §SEC18.6 is the publisher bypass. The text rule 2.11 relies on — the thread's words outside the scoped block, and `IsResolved` the one place that strains — sits in rule 2's paragraphs. Security.md itself cites it both ways (§SEC14.7 posture A′ rule 3 says rule 2; the note under posture D rule 5 says rule 3). The fix is a citation correction in both places: rule 2.11 here, and the rule-3 citation under posture D rule 5.
4. (needs issue) **The doc page and the model misdescribe `onRemoved`.** `reviewCommentPanelDoc.tsx` says "Wire this, and onRemoved fires only after the answer", and `reviewCommentItem.ts` says it "fires only once the consumer has said yes". Nothing in the family raises `onRemoved` after a confirmation: it is raised instead of `onRemoveRequested` when that is not wired (rule 2.27), and the one consumer does not wire it.
5. (needs issue) **The doc page's polling line is stale.** `reviewCommentPanelDoc.tsx` "What the consumer owns" repeats "useGetApprovalComments polls and refetches on focus". Rule 2.13 now names the page's single round refresh instead (user ruling 2026-09-27; item 2), so the line is to name that. Its family sample's "infinite scroll" is accurate: the results face loads further pages through `useInfiniteScrollSentinel`, scrolled rather than paged (`reviewCommentResultsPanel.tsx`).
6. **Note — a submitter's box, ruled.** This item asked whether a submitter should see and answer the thread, and on which page: rule 2.9, as relocated, opened the box to any authenticated reader, submitters included, while the one consumer takes `approvalId` off the verdict today, which only the moderation tier is handed (§ARC16.7.2), so a submitter with no tier would be told there is no round; the page is to read the id off the item instead (item 18). The user ruled on 2026-09-27 that the review thread belongs to the admin area: general users do not reach it, so the thread is offered to no general user on any page, and no submitter-facing consumer is needed. Whoever reaches the admin page for the item may add review comments, the item's owner included when they are a moderator; the owner is still refused an approval review of their own item (`UI/Components/ReviewPanel.md rule 2.15`), and to do more they would have to be an `Administrator`, whose bypass they would hold anyway. The page states it (`UI/Pages/ContentItemModerationDetailPage.md rules 2.2 and 2.3`). That page admits `Administrators` alone today (`src/securityMatrix.tsx` — `contentItems.view`); widening it is `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`. The user ruled later the same day that only the item's review tier — its `Reviewers`, `Publishers` and `Administrators`, by the scoped names — may add a review comment, and that the server checks it; asked next whether that tier gates the thread's other writes too, the user ruled that it gates every one — adding, amending, withdrawing, and marking a Question resolved, where within the tier the question's author, `Publishers` and `Administrators` still resolve (§SEC14.7 posture D rule 5). That retires "submitters converse in review threads" as the add gate's reason, which rule 2.9, as relocated, gave; rule 2.9 now states the review tier instead (§UI20.6.4 case 1), and the code's half is item 19.
7. (needs issue) **Gap — the narrow tier on other entity types.** `scopedNames` composes `{entityType}-{contentType}-…` for any entity type once a content type is given, while §SEC18.6 rule 5 confines content-type roles to `ContentItem`. `ReviewPanel` guards this (`contentTypedRole`); this panel does not. Evidence: `reviewCommentPanel.tsx` — `scopedNames`.
8. (needs issue) **Pass-through gap — add face.** `placeholderText` and `maxLength` cannot be set from the page. Evidence: `reviewCommentPanel.tsx` — the `<ReviewCommentAddPanel>` element forwards neither.
9. (needs issue) **Pass-through gap — results face.** `loadingText`, `loadingMoreText` and `loadMoreButtonText` cannot be set from the page. Evidence: `reviewCommentPanel.tsx` — the `<ReviewCommentResultsPanel>` element forwards none.
10. (needs issue) **Pass-through gap — edit face.** `maxLength` cannot be set from the page. Evidence: `reviewCommentResultsPanel.tsx` — the `<ReviewCommentEditPanel>` element does not forward it, and the dispatcher has no property for it.
11. (needs issue) **Pass-through gap — `CommentTypeRadioGroup`.** `groupLabel` cannot be set from the page. Evidence: `reviewCommentAddPanel.tsx` and `reviewCommentEditPanel.tsx` — neither `<CommentTypeRadioGroup>` element passes it.
12. (needs issue) **Gap — visible strings that are not properties.** Rule 2.31 applies §UI20.6.6 rule 1 (user ruling 2026-09-27). These are fixed English with no property on any face: Clear and Save on the add face; the label "Edit your comment", Cancel and Save on the edit face; the chip labels "Question" and "Comment", the tick's label "Is resolved — I got my answer", and Edit and Delete on the view face; the radio labels "Comment" and "Question" in `CommentTypeRadioGroup`; and the dispatcher's two no-box messages (rules 3.1.1 and 3.2.1). §UI20.6.6 rule 1 covers the strings written for a screen reader alone too (user ruling 2026-09-27), and these have no property either: the fixed words of the row actions' names, "Edit comment", "Delete comment" and the "by" before the author (`reviewCommentViewPanel.tsx` — `rowName` and the two buttons' `aria-label`, lines 70-72, 155 and 164 at 70dc72e7); and the results face's two spinners, each named *Loading...* by the `Spinner`'s own default label, which the face passes no `label` to replace (`reviewCommentResultsPanel.tsx`, lines 114 and 137; `coreUI/spinner.tsx`). The faces' own text properties that the page cannot reach are items 8, 9 and 11. Evidence: `reviewCommentAddPanel.tsx` — the Clear and Save buttons; `reviewCommentEditPanel.tsx` — the label and the Cancel and Save buttons; `reviewCommentViewPanel.tsx` — `chip`, the tick's label, the Edit and Delete buttons; `commentTypeRadioGroup.tsx` — `options`; `reviewCommentPanel.tsx` — the `mayComment === false` message.
13. **Note — rule 2.16 describes the page.** Rule 2.16, a relocated rule, places the thread on `/Admin/Posts/{id}`. Under the user's ruling of 2026-09-26 a page specifies its own behaviours for the presentation components it renders, and a component document specifies the component alone (§UI20.6.4), so where the thread lands is that page's to specify. The rule is kept as relocated. It does not contradict the user's description of the page — reviewers see `ReviewPanel` on the right-hand side to place their review (`UI/Pages/ContentItemModerationDetailPage.md rule 2.4`) — because it places the thread in the other column.
14. **Note — the read-only roles that stop a reader writing in the review thread, ruled.** This
    item asked whether the user's ruling that a holder of a blocked role may not add or submit
    comments reaches the review thread, where the design stopped the thread's words — new
    comments, and edits and deletes of one's own — at the global `ReadOnly` alone, and a holder
    of a read-only role of the entity under review, such as `ContentItem-Quote-ReadOnly`, kept
    writing. The user ruled on 2026-09-27 that review comments are restricted by `ReadOnly` and
    by a new role scoped to them, `ApprovalComment-ReadOnly`; the read-only roles of the entity
    under review still do not stop them. Adding a comment is what the ruling names; amending and
    withdrawing one's own are the other two writes the global `ReadOnly` already stops on the
    thread, and the new role stops them with it. Rules 2.9, 2.20, 3.2.2 and 3.3.3 now say so, and
    §SEC14.7 posture D rule 5 and §SEC18.6 rule 2 carry the role. The code's half is item 16.
15. **Note — the signed-out reader's message, ruled.** This item asked whether the thread's
    signed-out line, "Sign in to comment on this submission.", should become a way in that raises
    a sign-in hook for the page, or stay a statement. The user ruled on 2026-09-27 that the review
    thread is in the admin area, which an anonymous reader never reaches, so there is no
    signed-out state to design for. No sign-in hook is added (rule 2.34); the line stays as it is
    (rule 3.2.1).
16. (needs issue) **Gap — `ApprovalComment-ReadOnly` is not composed.** Rules 2.9, 2.20, 3.2.2 and
    3.3.3 (user rulings 2026-09-27) withhold the live box, showing it disabled, and the author's Edit and Delete, from a
    holder of `ApprovalComment-ReadOnly` as well as the global `ReadOnly`, and leave the tick's
    block set as it is (rule 3.2.5). The panel asks the global `ReadOnly` alone
    (`reviewCommentPanel.tsx` — `isBlockedFromCommenting`, line 116 at 70dc72e7), and its comment
    above that line says the global sanction "is the only one that reaches the WORDS". The role
    does not exist yet: `Glory2Him.Core/Models/Securities/Roles.cs` declares no
    `ApprovalComment-ReadOnly`. `isBlockedFromResolving` folds `isBlockedFromCommenting` into the
    tick's block (line 133), so widening the one must not widen the other.
17. (needs issue) **Gap — the write in flight is not announced.** Rule 2.33 applies §UI20.6.6 rule 5
    (user ruling 2026-09-27). While `isSubmitting` is on, every face disables its controls (rule
    3.1.4) and nothing announces the write: none of the four faces renders a `role="status"` or
    other live region for it. Evidence: `reviewCommentAddPanel.tsx`, `reviewCommentEditPanel.tsx`
    and `reviewCommentViewPanel.tsx` — `disabled={isSubmitting}`; the one live region beyond the
    spinners is the empty line (`reviewCommentResultsPanel.tsx`, line 150 at 70dc72e7).
18. (needs issue) **Gap — the thread's round id comes off the verdict, not off the item (#699).**
    Every item that implements `IApproval` stores the id of its approval round (§APR7.4 item 6),
    and every approval-round route is addressed by it, the reviewer display names included
    (§ARC17.5; user rulings of 2026-09-27, designed under #699,
    https://github.com/Glory2Him/Glory2Him.Core/issues/699, which holds the design alone; the
    build has no task yet). The panel's
    `approvalId` is therefore the id the item stores, and the author names come from the resolver
    asked by that id (section 7). Today the one consumer asks the verdict by the item's type and
    id and hands the panel `approvalVerdict?.approvalId ?? ''`
    (`contentItemModerationDetailPage.tsx`, line 772 at 70dc72e7; `src/hooks/useApprovalRound.ts`,
    lines 51 and 53), and the page's comment above it says the verdict "is the only read that
    turns this post into the round its comments hang off" (lines 757-758), which the ruling
    retires. So rule 2.17's second case, "a caller the verdict refused", is today's wiring and not
    the ruled one. The resolver is asked by the item (`src/hooks/useApprovalRound.ts`, line 80),
    which is `UI/Components/ReviewPanel.md §10 item 12`. No model that implements `IApproval`
    carries an approval id yet (§APR7.4 item 6), so the page has none to read until the server work lands. The page's
    half is `UI/Pages/ContentItemModerationDetailPage.md §6`.
19. (needs issue) **Gap — the box, Edit, Delete and the author's tick are offered outside the
    item's review tier.** Rules 2.9–2.11, 2.30, 3.2.3, 3.2.4 and 3.2.6 (user rulings 2026-09-27)
    limit every write to the review thread — adding a review comment, amending and withdrawing
    one's own, and marking a Question resolved — to the item's review tier, `Reviewers`,
    `Publishers` and `Administrators`, by the scoped names the entity composes; within the tier
    the question's author, the publisher tier and `Administrators` resolve. The server is to
    check it (§SEC14.7 posture D rule 5, §APR8.9 rule 3; not yet built). The panel asks no tier
    for any of them: it offers the box live to any signed-in reader holding neither block
    wherever an `approvalId` is handed, Edit and Delete to a row's author holding neither, and the
    tick to a Question's author holding no read-only role the entity composes
    (`reviewCommentPanel.tsx` — `mayComment`, lines 154-155 at 70dc72e7, whose comment still gives
    "submitters converse in review threads" as its reason, lines 151-153; `mayAmend`, lines
    160-161; `mayResolve`, whose author branch is `viewerOwns(item)`, lines 165-169). The tests
    sign in with no role and expect the box, the author's Edit and the author's tick
    (`reviewCommentPanel.test.tsx` — "should save the words, the chosen type and the approval
    id", "should offer Edit and Delete to the author alone", "should render on a question for its
    author"). The tier's content-type names are to be confined to `ContentItem`, as item 7 asks
    of the tick's. What a reader outside the tier sees instead of the live box is item 21.
20. **Note — what stands in the box's place, ruled.** This item asked whether the panel should
    say why it withholds the box from a signed-in reader outside the item's review tier, as it
    does for its other refusals, and in what words. The user ruled on 2026-09-27 that the box is
    not withheld: the review-comment box and its button are shown disabled, each with a tooltip,
    as the review panel's disabled controls are (`UI/Components/ReviewPanel.md rule 2.53`). In the
    user's words: *"the review comment box and button must be disabled. It can have a tooltip on
    each as per the other controls stating user belongs to a block role"*. A
    holder of a block role is given "User belongs to a block role", in place of the line rule
    3.2.2 put in the box's place for them before, "Your account is currently read-only, so you
    cannot add comments."; a reader outside the tier who holds neither block of rule 3.2.2 is given "Only this
    item's reviewers can comment". Each is a property whose default is that text.
    Rules 3.2.2, 3.2.6 and 3.5.1 now say so. The code's half is item 21.
21. (needs issue) **Gap — the box is never shown disabled with a tooltip.** Rules 3.2.2 and 3.2.6
    (user rulings 2026-09-27) show the box disabled, not hidden, to a holder of a block role and
    to a signed-in reader outside the item's review tier, its text box and Save each carrying a
    tooltip — "User belongs to a block role" or "Only this item's reviewers can comment", each a
    property — whose reason a screen reader announces too (section 9). The panel withholds the
    box from a holder of the global `ReadOnly` and renders "Your account is currently read-only,
    so you cannot add comments." in its place (`reviewCommentPanel.tsx` — the `mayComment ===
    false` message, lines 221-227 at 70dc72e7), and offers it live outside the tier (item 19).
    A test pins today's line and must change with it: "should offer no box against a global
    ReadOnly sanction" (`reviewCommentPanel.test.tsx`) expects no box and the read-only line.
    The add face disables its controls only while `isSubmitting` is on
    (`reviewCommentAddPanel.tsx`, lines 109-130), and neither the dispatcher nor the add face
    has a tooltip, or a property for one. `ApprovalComment-ReadOnly` is not composed at all (item
    16).
