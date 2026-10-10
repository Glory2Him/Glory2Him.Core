# 1. ContentItemPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** [ContentItemPanel.Add.md](ContentItemPanel.Add.md), [ContentItemPanel.Restricted.md](ContentItemPanel.Restricted.md), [ContentItemPanel.Edit.md](ContentItemPanel.Edit.md), [ContentItemPanel.Default.md](ContentItemPanel.Default.md), [ContentItemPanel.ContentItemQuotesPanel.md](ContentItemPanel.ContentItemQuotesPanel.md), [ContentItemPanel.ContentItemVerseImagePanel.md](ContentItemPanel.ContentItemVerseImagePanel.md). The form engine the add and edit faces share, `ContentItemFormPanel`, has no file of its own, by the user's ruling (2026-09-26); the rules it holds for both faces are `UI/Components/ContentItemPanel.Add.md rules 2.13–2.19`.
- **Composes:** none
- **Used by:**
  - [ContentItemListPanel.ContentItemResultsPanel.md](ContentItemListPanel.ContentItemResultsPanel.md) — one `ContentItemPanel` per element (`src/components/contentItems/contentItemResultsPanel.tsx`), and through it [ContentItemListPanel.md](ContentItemListPanel.md)
  - `/posts/contribute` — `src/pages/contribute.tsx` (the add face, or the restricted face in its place — rule 2.43)
  - `/posts/{contentItemId}` — `src/pages/postDetail.tsx` (the view face, `showEditSection` off)
  - `/myposts/{contentItemId}` — `src/pages/myPostDetail.tsx` (the view face with editing in place)
  - `/Admin/Posts/{contentItemId}` — `src/pages/admin/contentItemModerationDetailPage.tsx` (the moderated view face; that page renders `ContentItemEditPanel` directly for its editor)
  - `/`, `/posts`, `/myposts`, `/Admin/Posts` — `src/pages/home.tsx`, `posts.tsx`, `myPosts.tsx`, `admin/contentItemModerationPage.tsx`, each through `ContentItemListPanel`
  - The sample pages listed under **Sample page** below
- **Inherits:** §SEC14.6, §SEC14.7 posture A, §SEC18.6, §DOM3.4 rule 16, §DOM6.4, §DOM6.5, §DOM6.6, §ARC12.4.1 rule 7a, §ARC12.5.2 business rules 1–2, §APR9.7.1 rule 1, §APR9.9, §UI20.6.4, §UI20.6.5, §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemPanel.tsx`; the contract with its view templates is `src/models/components/contentItems/contentItemTemplate.ts`; the form engine both writing faces share is `src/components/contentItems/contentItemFormPanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Panel` — `src/pages/samplePages/components/contentItemPanelDoc.tsx`, with the view-face playground `shared/contentItemPanelPlayground.tsx` and the persona board `shared/securityContextDemo.tsx`
- **Relocated from:** §UI20.6.2

`ContentItemPanel` shows one content item on whichever face the moment asks for. Handed no item,
it is the contribution form. Handed an item, it is the card every feed and detail page shows,
through the view template registered for the item's content type — and, where the page allows
it, the in-place editor for that item.

It is the dispatcher of its family. It owns what is the same on every face — the effective-setting
reads, the ownership and role gates, the reaction gating and the per-card state — and hands a
fully decided bundle to the face it dispatches to. The faces render what it decides. Each face
has a story of its own; the rules that belong to one face live there, and section 6.2 lists where.

## 2. Business Rules

**2.1 [Must]** `ContentItemPanel` is a pure presentation component: props in, events out, no fetching, no mutation, no sockets. *(§UI20.6.2)*

**2.2 [Must]** It is the one dispatcher for a content item's every face. Handed a settings collection and no item, it renders the add template ([ContentItemPanel.Add.md](ContentItemPanel.Add.md)), or the restricted template in its place where the reader has no content type left to contribute (rule 2.43). With Edit taken in place — or `mode="edit"` passed — it renders the edit template ([ContentItemPanel.Edit.md](ContentItemPanel.Edit.md)). Otherwise the item renders through the view template registered for its content type: `ContentItemDefaultPanel` ([ContentItemPanel.Default.md](ContentItemPanel.Default.md)), or an override such as `ContentItemQuotesPanel` ([ContentItemPanel.ContentItemQuotesPanel.md](ContentItemPanel.ContentItemQuotesPanel.md)). *(§UI20.6.2; user, 2026-09-27)*

**2.3 [Must]** An override is registered against one content type, and adding one is one entry in the panel's template registry. Today `Quote` renders through `ContentItemQuotesPanel` and `VerseImage` through `ContentItemVerseImagePanel`; every other type renders through `ContentItemDefaultPanel`. *(code: contentItemPanel.tsx — templateOverrides)*

**2.4 [Must]** `ContentItemListPanel` composes the search bar and the scrolled results, rendering this same panel for every element — one family, one tree, no second detail component to keep in sync. *(§UI20.6.2)*

**2.5 [Must]** Every face runs on the family's one projection: a self-contained element carrying the item and its §DOM6.4 winning setting, so a list element hands to a detail surface — and seeds its editor — with no further read, and an update is one element swapped by the consumer. *(§UI20.6.2)*

**2.6 [Must]** An item always renders its card. A panel handed both an element and a settings collection never falls into the add face. *(test: contentItemPanel.test.tsx — "should never fall into add from a list — an item always renders its card")*

**2.7 [Must]** Every gate the panel renders decides what to SHOW and nothing more. The foundation and processing services re-decide add, modify and remove against the stored row (§SEC14.6, §SEC14.7 posture A), and must: a hidden button is a courtesy to the reader, never an authorization boundary. *(§UI20.6.2)*

**2.8 [Must]** Where Edit goes is the page's wiring. A page listening on `onEditClick` alone gets the event and routes to its own edit surface, carrying its back context. *(§UI20.6.2)*

**2.9 [Must]** A page that switches `showEditSection` on and listens on `onModified`/`onRemoved` gets the editor **in place**: the owner's Edit swaps the card for the edit template, and both a committed Save and Cancel swap the card back. *(§UI20.6.2)*

**2.10 [Could]** `mode="edit"` lands straight on the editor, still subject to the same gates. *(§UI20.6.2)*

**2.11 [Must]** What the card shows after the editor closes is the consumer's element: the page persists and swaps it, so the amendments appear. Cancel discards the draft, and reopening seeds from the original. *(§UI20.6.2)*

**2.12 [Must]** `showEditSection` is the surface switch, ahead of every role check, and it is off by default — the safe posture `AssociationPanel` takes with `showModerationActions`. *(§UI20.6.2)*

**2.13 [Must]** While `showEditSection` is off the panel renders no in-place editor and no `Delete`, however the roles fall. Moderate (`onModerateClick`) and a page-routed Edit (`onEditClick`, rule 2.8) are routes the page chooses to wire, outside this switch. What the edit template does meanwhile is `UI/Components/ContentItemPanel.Edit.md rule 2.3`. *(§UI20.6.2; user, 2026-09-26)*

**2.14 [Must]** A public page renders the panel without `showEditSection` and gets a view surface that cannot accidentally become an edit one; a profile or admin area switches it on, and the role gates then decide, per action, what is actually shown. It only ever subtracts. *(§UI20.6.2)*

**2.15 [Must]** Role composition follows §SEC18.6 — capability last and plural — resolved against the content type IN PLAY: the selected type while adding, the item's own type when reading or editing. *(§UI20.6.2)*

**2.16 [Must]** Every role set but the block set is an overridable comma-separated prop in which `{ContentType}` resolves to the enum member name, and `[OWNER]` names the item's contributor, matched on the account id and never on a display name. The block set is not a property: the panel composes its read-only roles itself from what it represents, and no page supplies, adds to or removes from them (§UI20.6.6 rule 3). *(§UI20.6.2; user, 2026-09-27)* ≠ items 2 and 24

**2.17 [Must]** The role sets default to: *(§UI20.6.2; user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Edit.md §10 item 5`

| Gate | Default |
| --- | --- |
| blocked by ≠ item 24 | `ReadOnly`, `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` — composed by the panel, never a property (rule 2.16) |
| add | empty — any authenticated reader, since there is no `Contributor` role |
| edit | `[OWNER]`, `Publishers`, `ContentItem-Publishers`, `ContentItem-{ContentType}-Publishers`, `Administrators` — the non-owner half further confined to `Draft` / `Submitted`; the owner's amendment of an `Approved` or `Rejected` item forks a new version and never deletes the reviewed one (§APR9.9) |
| delete | `[OWNER]`, `Administrators` — the owner only while the item is `Draft` or `Submitted`, since an `Approved` or `Rejected` item is locked to its owner (§APR9.9); `Administrators` at any status, since removal is a takedown, not a moderation step (§SEC14.7 posture A.3) |

**2.18 [Must]** The block set is asked first and outranks every grant, `[OWNER]` included (§SEC18.6 rule 2). A contributor holding `ContentItem-Devotional-ReadOnly` sees no `Edit` and no `Delete` on their own devotional, and no add surface for that type, while stories and quotes stay open to them. Where the narrow block lands on the add face is `UI/Components/ContentItemPanel.Add.md rule 2.6`. *(§UI20.6.2)*

**2.19 [Must]** The `Reviewers` tier appears in none of the add, edit and delete sets — a reviewer reviews. The view face's moderation tier includes it (rule 3.2.2), because a reviewer needs a way to the round they review. *(§UI20.6.2; user, 2026-09-26)*

**2.20 [Must]** The panel's block set is still a render courtesy (§SEC14.6), but it is no longer courtesy alone: `ContentItem-{ContentType}-ReadOnly` is a real role now — seeded, and refused by the foundation, the processing layer and the approval surface alike (§SEC18.6 rule 2). The two answers agree by construction rather than by coincidence, because both compose the same name from the row's own content type. *(§UI20.6.2)*

**2.21 [Must]** Which fields exist is per content type and is passed in, never fetched — and the panel resolves the EFFECTIVE row itself. The consumer hands over the `ContentItemSetting` rows it already holds and the most specific one wins, exactly as §DOM6.4 and §ARC12.5.2 business rules 1–2 require: an item-level override takes **full precedence** over the content type default, and a soft-deleted row is excluded from resolution entirely (§DOM6.6). *(§UI20.6.2)* ≠ item 5

**2.22 [Must]** The override is matched on the **item** as well as the type, so a mixed collection is safe — one item's override is never applied to another's. *(§UI20.6.2)*

**2.23 [Must]** What the panel reads off the resolved row is the field shaping and the type's presentation: `HasTitle`, `HasAuthor`, `ContentTypeName`, `ContentTypeDescription`, `ContentTypeIconCssClass`. `HasTitle` and `HasAuthor` govern every face — the inputs on the two writing faces, and the title and author on the view templates (`UI/Components/ContentItemPanel.Default.md rule 2.4`). *(§UI20.6.2)*

**2.24 [Must]** The `Max*Length` ceilings cap the fields client-side on both writing faces: the input refuses further typing, and a stored value already over a lowered ceiling is refused at submit with the limit named. *(§UI20.6.2)*

**2.25 [Must]** A field the reader cannot see contributes nothing, and the row keeps whatever it already had. One rule settles both halves: the amendment half is `UI/Components/ContentItemPanel.Edit.md rule 2.10`, the contribution half `UI/Components/ContentItemPanel.Add.md rule 2.12`. *(§UI20.6.2)*

**2.26 [Must]** Where no setting row resolves at all there is no flag to obey, and the panel shows whichever of the title and author the item carries. In the product a type default always exists (§ARC12.5.2 business rule 5), so this is only the component's fallback for a page that fails to supply a setting. A page never renders the card before its setting has loaded: it holds the card, showing its announced loading state (§UI20.6.6 rule 5), until the setting arrives, and if its settings read fails it shows its announced error with a Retry in place of the cards — never the cards without their settings. So what the card shows when handed none is not a state the design relies on. *(§UI20.6.2; user, 2026-09-26; user, 2026-09-27)*

**2.27 [Must]** `SharePermission` is the exception, and drops rather than persisting. It is hidden by the contributor's own answer to a question in front of them — not by a setting they never chose — so "the row keeps what it had" does not apply: a note reading *permission granted by the author* stored against an item its contributor has just declared `Owned` is a provenance claim they withdrew. Nothing server-side correlates the two (the foundation length-checks `SharePermission` and no more), and no read surface renders it once the basis has moved, so preserving it would file a contradiction nobody can see or clear. *(§UI20.6.2)*

**2.28 [Must]** The `SharePermission` field, the placement of its validation messages and what is submitted all read the same flag, so the three cannot disagree. *(§UI20.6.2)*

**2.29 [Must]** The facet pairs (§DOM6.5 — `TagsAllowed`/`ShowTags` and the same for comments, reactions, links, attachments and bible references) govern surfaces this panel does not own; the panels rendering beside it read those, against this same effective row. They also govern the card's own features, each together with the page's switch for it: the card's display of a feature — tags, bible references, reactions, comments — needs the page's switch (its `show…Section` property) AND the setting's `Show<Feature>`, and a contribution control on the card — the reaction picker — needs the page's switch AND `<Feature>Allowed`. If either is false, the part is not shown. *(§UI20.6.2; user, 2026-09-26)*

**2.30 [Must]** The consumer owns persistence and freshness. The panel raises `onAdded`, `onModified`, `onRemoved` and `onCancelled`, and does nothing else: the page decides whether `onModified` is a `PUT` or a fork of a new version on a terminal item (§DOM3.4 rule 16), swaps the amended element so the closed editor's card shows it, and re-fetches whenever the item changes underneath it. The panel shows the world as of the last props it was handed. *(§UI20.6.2)*

**2.31 [Must]** Validation comes back from the API, not from the browser — with two ruled exceptions the panel is the right surface for. A permission basis makes the `SharePermission` box mandatory (a claim of permission with no permission named is not a submission the product accepts), and the effective setting's `Max*Length` ceilings are enforced as rule 2.24 says; both speak through the same field-issue channel the server's messages use. *(§UI20.6.2)*

**2.32 [Must]** Everything else the panel leaves to the server — a second opinion in the browser would drift from it. *(§UI20.6.2)*

**2.33 [Should]** The consumer submits, and hands the `errors` dictionary of the returned `ValidationProblemDetails` back to the panel as `validationIssues`. The panel matches those keys onto its fields case-insensitively (they are the server's parameter names) and summarises anything it cannot place rather than dropping it. *(§UI20.6.2)*

**2.34 [Should]** The failure also raises a timed notification through the existing toast framework, carrying the API's own reason rather than a generic one. *(§UI20.6.2)*

**2.35 [Won't]** Associations render beside the panel, never within it. Tags and bible references belong to `AssociationPanel` and its two wrappers, which have their own approval and role rules and need an item to associate to — so they cannot render on an add surface at all. *(§UI20.6.2)*

**2.36 [Won't]** Approval controls are not part of the panel; they belong to `ReviewPanel` (`UI/Components/ReviewPanel.md §6`). *(§UI20.6.2)*

**2.37 [Won't]** Paste-to-upload for inline images (§DOM5.6.6) is not part of the panel yet. *(§UI20.6)*

**2.38 [Must]** Edit and Moderate split by role. The card's Edit is the owner's hook (rules 3.1.4 and 3.2.3); every other role — Reviewer, Publisher and Administrator — gets Moderate (rules 3.1.5 and 3.2.2), which on a moderated view wears Edit's pencil and label (rule 3.1.6). They are two buttons with two purposes, and the page sets each one's label; no label depends on the viewer's role (rule 3.1.6). Rule 2.17's edit set, which holds the publisher tier and `Administrators`, governs the edit template, which those roles reach on a page through Moderate, and not the card's Edit. Where each hook leads is the page's (§UI20.6.4). *(user, 2026-09-26; user, 2026-09-27)* ≠ item 21

**2.39 [Must]** A setting always applies, and the card always renders under the winning row: the item's own override where one exists, the content type default otherwise (§DOM6.4, §ARC12.5.2 business rules 1, 2 and 5). The element carries that row (rule 2.5); where it does not, the card resolves it from the collection it was handed, taking the item's override first and the type default otherwise (rule 2.21). *(user, 2026-09-26)* ≠ item 5

**2.40 [Must]** View and Edit are two actions with two hooks, and neither stands in for the other. View opens the item's detail view read-only, on the view template registered for its type (rule 2.3); Edit opens the detail view straight in edit mode. Each is switched by a `show…` property of its own, and the page's properties decide which the card offers; the names of the two switches and of View's hook are fixed by the task that builds them. They sit beside Moderate without merging into it: Edit is the owner's and Moderate the moderation tier's (rule 2.38), and the ruling puts no role condition on View, whose switch alone decides it. Where each hook leads is the page's (§UI20.6.4). *(user, 2026-09-27)* ≠ item 20

**2.41 [Must]** A tag click and a Bible reference click raise `onTagClick` and `onBibleReferenceClick` and do nothing else: the card performs no navigation and no filtering of its own, and where the reader goes is the page's (§UI20.6.4). *(user, 2026-09-27; code: contentItemDefaultPanel.tsx — the tag and reference pills)*

**2.42 [Must]** A click on the type chip, on *Submitted by* or on *Author* raises `onContentTypeClick`, `onSubmittedByClick` or `onAuthorClick` with the element, and does nothing else: the card runs no search and performs no navigation of its own, and where the reader goes is the page's (§UI20.6.4). The element carries the value the page hands on — the item's content type, the submitter's account id and name together, the author. *(user, 2026-09-27; code: contentItemDefaultPanel.tsx — the type chip and the meta row)*

**2.43 [Must]** Handed no item, the panel shows the restricted face, `ContentItemRestrictedPanel` ([ContentItemPanel.Restricted.md](ContentItemPanel.Restricted.md)), instead of the add face when the reader has no content type left to contribute, whatever the reason none remains: no content type is on offer, or the reader's read-only roles leave no tile (`UI/Components/ContentItemPanel.Add.md rules 2.6 and 3.2.4`). It takes the place of the add face's two refusals and is asked where they were — after the loading line and after sign-in — so a signed-out reader still gets the add face's login link (`UI/Components/ContentItemPanel.Add.md rule 3.3.1`). *(user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1`

**2.44 [Must]** A setting may change while an item is shown — comments switched off for one item, for example — and the card is to reflect the change without the reader reloading (§ARC12.5.2 business rule 12). The card takes the change when the page hands it the new setting, as it takes any change (rule 2.30): the parts that setting governs then show or go under it (rules 2.29, 3.1.8 and 3.1.12). How the page learns of the change is the page's: the user ruled on 2026-09-27 that live updates across the site — a live connection pushing changes to open pages, potentially driven by the `-Added`, `-Modified` and `-Removed` facts — get their own design, #702 (`DESIGN: Push Live Updates To Open Pages`). That design is `DesignFeatures/LiveUpdates.md`, and §UI20.10 rules how every page hears a change: a setting message makes the page's settings read stale, and the page hands the card the setting it reads again (§ARC12.5.2 business rule 12). *(user, 2026-09-27; §ARC12.5.2 business rule 12)*

## 3. Presentation / Behaviour rules

The rules below are the dispatcher's own decisions. How each face renders what it is handed is
in that face's story.

### 3.1 Driven by properties

**3.1.1** No `contentItem` renders the add face, whatever else is passed — or, where rule 2.43 says so, the restricted face in its place; none of the view-face props reach either. *(code: contentItemPanel.tsx — ContentItemPanel, the `contentItem == null` branch; user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1`

**3.1.2** With a `contentItem`, the edit face renders only when the editor is asked for (`mode="edit"`, or Edit taken) **and** `showEditSection` is on **and** the page listens on `onModified` or `onRemoved`. In every other case the view template renders. `mode="add"` and `mode="read"` change nothing when an item is present. *(code: contentItemPanel.tsx — opensEditorInPlace)*

**3.1.3** A different item id, or a changed `mode`, closes an editor the reader opened. *(code: contentItemPanel.tsx — the `[contentItemId, mode]` effect)*

**3.1.4** The owner's **Edit** renders on the view face when the page's Edit switch is on (rule 2.40), `showModerationSection` is off, the viewer owns the item, no block applies, and either the editor opens in place (rule 3.1.2) or `onEditClick` is wired. Pressed, it opens the editor in place where it can, and otherwise raises `onEditClick`. *(test: contentItemPanel.test.tsx — "should keep routing Edit to the page when only onEditClick is wired"; user, 2026-09-27)* ≠ item 20

**3.1.5** **Moderate** renders when the viewer holds the moderation tier (rule 3.2.2) and `onModerateClick` is wired. It is a hook: what Moderate does for each viewer is the page's (§UI20.6.4). *(code: contentItemPanel.tsx — showsModerateButton; user, 2026-09-26)*

**3.1.6** `showModerationSection` off: Edit and Moderate stand side by side, Moderate wearing the shield (`bi bi-shield`) and the label *Moderate*. On: the owner's Edit never renders, and Moderate stands alone wearing Edit's pencil (`bi bi-pencil`) and the label *Edit*, still raising `onModerateClick`. Each label is a property the page sets, whose default is the one named here (§UI20.6.6 rule 1): a page may relabel Edit or Moderate to suit where the card stands, and in the admin area may label Moderate *Edit*, since there it opens the item for modification. No label depends on the viewer's role. *(test: contentItemPanel.test.tsx — "should dress Moderate as Edit and stand it alone on a moderated view"; user, 2026-09-27)* ≠ item 21

**3.1.7** The terminal lock. With `moderationOpensEditor` on, Moderate renders **disabled** for a viewer who does not own the item when the item's status is not `Draft` or `Submitted` (an absent status counts as amendable). The locked action raises nothing and carries its reason, worded from its own label: *Locked for moderation*, or *Locked for editing* where it wears Edit's label. With `moderationOpensEditor` off — a surface whose action routes rather than edits — it is never locked. *(test: contentItemPanel.test.tsx — "should lock the moderation action on an approved item for a moderator who is not its contributor", "should leave the moderation action live where the surface routes rather than edits", "should word the lock from the action's own label")*

**3.1.8** The Like control is offered only when `showReactionSection` is on, `onReactionSelected` is wired, `reactionOptions` is not empty, and the winning setting (rule 2.39) neither refuses reactions (`ReactionsAllowed`) nor hides them (`ShowReactions`). Where the setting carries `LimitReactionsToLoveOnly`, only the options marked `isLove` are offered. *(code: contentItemPanel.tsx — offeredReactions; user, 2026-09-26)* ≠ item 5

**3.1.9** The content length. `showContentExpanded` off (the default) cuts the content at `truncateAt` (default 400) characters; on, the whole content stands. With `allowInPlaceExpansion` on, the panel owns the expansion, seeded from `showContentExpanded` and reset when the item or `showContentExpanded` changes, and toggles it on `onExpandCollapse`; off, `showContentExpanded` decides alone. *(code: contentItemPanel.tsx — isContentToggledOpen)*

**3.1.10** `allowTitleClick` is off by default: a panel standing on its own is the detail surface, so its title is plain heading text even with `onTitleClick` wired. *(test: contentItemPanel.test.tsx — "should stand the title as plain text by default even with onTitleClick wired")*

**3.1.11** `showApprovalStatusRibbon` and `showApprovalStatus` are off by default. On, the card wears its status on every status, `Approved` included. *(test: contentItemPanel.test.tsx — "should ribbon the ordinary approved case too — that is what opting in means")*

**3.1.12** The six section switches (`showTagSection`, `showBibleReferenceSection`, `showReactionSection`, `showCommentsSection`, `showShareSection`, `showSaveSection`) default to on and only subtract: a section renders only where the switch and the winning setting (rule 2.39) both agree. *(test: contentItemPanel.test.tsx — "should still let the setting decide when the surface says nothing"; user, 2026-09-26)* ≠ item 5

**3.1.13** The type is named by the `ContentTypeName` (rule 2.23) of the winning setting (rules 2.5 and 2.39), falling back to the fixed enum label when no setting resolves (rule 2.26). *(test: contentItemPanel.test.tsx — "should fall back to the enum label when the element carries no setting"; user, 2026-09-26)* ≠ item 5

**3.1.14** `isLoading`, `isSubmitting`, `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`, `ariaLabel`, `titleText` and `showBorder` reach the writing faces only; the view templates never receive them. *(code: contentItemPanel.tsx — the ContentItemAddPanel and ContentItemEditPanel renders)*

**3.1.15** Every visible string the panel composes itself, and every string it composes for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). The card's other strings are its templates' (`UI/Components/ContentItemPanel.Default.md rule 3.1.12`). *(user, 2026-09-27)* ≠ item 21

### 3.2 Driven by roles

**3.2.1** A viewer holding `ReadOnly`, `ContentItem-ReadOnly` or `ContentItem-{ContentType}-ReadOnly` for the item's own type sees neither Edit nor Moderate. The action is **hidden**, not locked. *(test: contentItemPanel.test.tsx — "should show no action to a ReadOnly holder rather than a locked one")*

**3.2.2** The moderation tier on the view face is `Administrators`, `Reviewers`, `Publishers`, `ContentItem-Reviewers`, `ContentItem-Publishers`, and `ContentItem-{ContentType}-Reviewers` / `-Publishers` for the item's own type. A narrow tier scoped to another type does not count. *(test: contentItemPanel.test.tsx — "should offer Moderate to the %s tier with the shield", "should offer no Moderate to a tier scoped to another type")*

**3.2.3** The owner's Edit renders for the item's own contributor alone — the element's `submittedById` equal to the signed-in reader's account id — and never for an anonymous visitor. The publisher tier and `Administrators` in rule 2.17's edit set do not make this button theirs: that set governs the edit template, and they reach it through Moderate (rule 2.38). *(test: contentItemPanel.test.tsx — "should offer Edit to the person who submitted it and to nobody else", "should offer Edit to no anonymous visitor"; user, 2026-09-26)*

**3.2.4** A signed-out reader is offered Like on the same terms as any reader (rule 3.1.8), and choosing a reaction raises `onReactionSelected` whatever the sign-in state. The panel never redirects and composes no route of its own (§UI20.6.4); no other hook is needed. What follows the hook is the page's (§UI20.6.6 rule 2; `useContentItemEngagement`, which the seven pages `DesignFeatures/UI/Hooks/ContentItemEngagement.md` names take `onReactionSelected` from, its §2 rules 1 to 4; #739): it sends a signed-out reader to sign in, with return information that brings them back afterwards, and persists or clears the reaction for a signed-in reader. A reader whose sign-in state has not been read back yet is not sent to sign in. *(user, 2026-09-27; test: contentItemPanel.test.tsx — "should raise the reaction hook for a signed-out reader and navigate nowhere", "should raise the reaction hook for a reader whose sign-in state failed to read", "should not send a reader to sign in while the sign-in state is still unknown")*

**3.2.5** A signed-out reader still sees the reaction counts. *(test: contentItemPanel.test.tsx — "should show the reaction counts to a signed-out reader")*

**3.2.6** The add gates are `UI/Components/ContentItemPanel.Add.md §3.2`; the edit and delete gates are `UI/Components/ContentItemPanel.Edit.md §3.2`. *(code: contentItemFormPanel.tsx — mayAdd, mayEdit, mayDelete)*

### 3.3 Combinations

**3.3.1** The order the view face asks in: the surface switches first (`showEditSection` only subtracts, rules 2.12–2.14; `showModerationSection` removes the owner's Edit whatever the ownership), then the block veto (hides both actions, the owner included, rule 3.2.1), then ownership and the moderation tier, and last the terminal lock, which greys the moderation action rather than hiding it (rule 3.1.7). *(code: contentItemPanel.tsx — showsEditButton, showsModerateButton, isModerateButtonLocked)*

**3.3.2** An owner who also holds the moderation tier sees Edit and Moderate side by side off the moderated view, and Moderate alone, dressed as Edit, on it. Neither is locked on their own terminal item, because their amendment forks a new version (§DOM3.4 rule 16, §APR9.9). *(test: contentItemPanel.test.tsx — "should offer both, side by side, to an owning moderator off the moderated view", "should leave the contributor's own edit live on a terminal item")*

### 3.4 Role matrix

Owner means the item's contributor: the element's `submittedById` (the editor's `createdBy`) is the
viewer's account id. Rows 1–7 and 12 are the view face; rows 8–11 summarise the writing faces,
whose stories hold the detail; row 13 is the restricted face.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| 1. An item — the view card renders | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| 2. The page's Edit switch on, `showModerationSection=false`, `onEditClick` wired (or `showEditSection=true` + `onModified`), no ReadOnly — **Edit** ≠ item 20 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| 3. `onModerateClick` wired, no ReadOnly — **Moderate** (shield; pencil and *Edit* when `showModerationSection=true`) | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| 4. As row 3, `moderationOpensEditor=true`, item `Approved` or `Rejected` — Moderate rendered **locked** | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| 5. Any ReadOnly covering the item's type, whatever else is held — **Edit** or **Moderate** | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| 6. Owner also holding `Publishers` or `Administrators`, `showModerationSection=false`, both wired — **Edit and Moderate side by side, neither locked** | ➖ n/a | ➖ n/a | ✅ Yes | ➖ n/a | ➖ n/a | ➖ n/a |
| 7. `reactionOptions` given, `onReactionSelected` wired, setting allows — **Like** | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| 8. `showEditSection=true`, `onModified` wired, Edit taken — **editor in place** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| 9. `mode="edit"`, `showEditSection=true`, `onModified` wired, item `Draft` or `Submitted` — **editor fields and Save** | ❌ No | ❌ No | ✅ Yes | ❌ No | ✅ Yes² | ✅ Yes |
| 10. As row 9, item `Approved` or `Rejected` — **editor fields and Save**, the owner's save forking a new version (§APR9.9) | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| 11. No item, a content type left to the reader — **the add form** | ❌ No³ | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| 12. The page's View switch on and View's hook wired, any read-only role or none — **View** ≠ item 20 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| 13. No item, no content type left to the reader — **the restricted face**, in place of the add form ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1` | ❌ No³ | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Shown; choosing raises `onReactionSelected`, and the page sends the reader to sign in (rule 3.2.4).
² At the global `Publishers`, `ContentItem-Publishers` or `ContentItem-{ContentType}-Publishers` scope for the item's type.
³ The login link renders instead (`UI/Components/ContentItemPanel.Add.md rule 3.2.1`).

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItem` | `ContentItemSearchItem?` | — | The self-contained element (rule 2.5). Absent, the panel is the add face. | View templates as `contentItem`; the edit face as a `ContentItemFormItem` projection of it |
| `contentItemSettingCollection` | `ContentItemSetting[]` | `[]` | The content type defaults the consumer holds: the add face's tiles, and the editor's frozen tiles and fallback. | Add face, edit face |
| `mode` | `'add' \| 'read' \| 'edit'?` | — | `'edit'` lands on the editor (rule 3.1.2). | Dispatcher only |
| `reactionOptions` | `ContentItemReactionOption[]` | `[]` | The choices behind Like. Empty means no Like. | View templates, gated and narrowed, as `offeredReactions` |
| `showModerationSection` | `boolean` | `false` | The card sits on a moderated surface (rule 3.1.6). | View templates, resolved into `moderateButtonIconCss` / `moderateButtonLabel` |
| `moderationOpensEditor` | `boolean` | `false` | This surface's moderation action opens the editor here; the terminal lock reads it (rule 3.1.7). | View templates, resolved into `moderateButtonLockReason` |
| `allowTitleClick` | `boolean` | `false` | Whether the title is a way into the detail surface. | View templates |
| `showApprovalStatusRibbon` | `boolean` | `false` | The status corner ribbon. | View templates; edit face |
| `showApprovalStatus` | `boolean` | `false` | The status pill beside the type chip. | View templates |
| `showContentExpanded` | `boolean` | `false` | The whole content, uncut. | View templates, resolved into `isContentExpanded` |
| `truncateAt` | `number` | `400` | Where the content is cut. | View templates |
| `allowInPlaceExpansion` | `boolean` | `false` | Read-more toggles in place rather than raising `onReadMore`. | View templates |
| `showEditSection` | `boolean` | `false` | The surface switch (rules 2.12–2.14). | Edit face |
| `showTagSection`, `showBibleReferenceSection`, `showReactionSection`, `showCommentsSection`, `showShareSection`, `showSaveSection` | `boolean` | `true` | The section switches (rule 3.1.12). | View templates |
| `isLoading` | `boolean` | `false` | A loading line instead of a half-built form. | Add face, edit face |
| `isSubmitting` | `boolean` | `false` | Freezes the form buttons while the consumer persists. | Add face, edit face |
| `validationIssues` | `Record<string, string[]>?` | — | The API's field messages (rule 2.33). | Add face, edit face |
| `submittedByDisplayName` | `string?` | — | Whose name an owned basis prefills into Author. | Add face, edit face |
| `approvalStatusDefault` | `ApprovalStatus?` | — (the form holds `Submitted`) | What *Submit as* opens on where the model names no status. | Add face, edit face |
| `ariaLabel`, `titleText`, `showBorder` | `string?`, `string?`, `boolean?` | — | The writing faces' own frame and name. | Add face, edit face |
| `submittedByLabelText`, `authorLabelText`, `shareabilityLabelText`, `dateLabelText`, `likeButtonText`, `commentsText`, `commentsNoCountText`, `shareButtonText`, `saveButtonText`, `editButtonText`, `readMoreText`, `expandLinkText`, `showLessText`, `allReactionsText`, `shareabilityBasisLabels` | `string` / label map | per `UI/Components/ContentItemPanel.Default.md §4.1` | The card's wording (`ContentItemText`). | View templates only |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onAdded` | `ContentItemFormItem` | The add face submits. |
| `onModified` | `ContentItemFormItem` | The edit face saves. The panel closes the editor first, then raises it. |
| `onRemoved` | `ContentItemFormItem` | The edit face's Delete is confirmed. |
| `onCancelled` | none | The add or edit face is cancelled. On the edit face the panel closes the editor first. |
| `onEditClick` | `ContentItemSearchItem` | The owner's Edit is pressed and the editor does not open in place (rule 3.1.4). |
| `onModerateClick` | `ContentItemSearchItem` | Moderate is pressed and is not locked. |
| `onReactionSelected` | `ContentItemSearchItem`, `ContentItemReactionOption` | A reader chooses a reaction, whatever the sign-in state (rule 3.2.4). |
| `onExpandCollapse` | `ContentItemSearchItem` | The in-place toggle is pressed; the panel toggles the expansion and raises the event. |
| `onContentTypeClick`, `onTitleClick`, `onSubmittedByClick`, `onAuthorClick`, `onTagClick`, `onBibleReferenceClick`, `onCommentsClick`, `onReadMore`, `onShareClick`, `onSaveClick` | the element (and the tag or reference) | Passed through unchanged; `UI/Components/ContentItemPanel.Default.md §4.2` says when each fires. |

### 4.3 Pass-through properties

Per §UI20.6.5. The panel hands each face the properties in the table below, unchanged except
where the **Passes through to** column of section 4.1 says it resolves them first.

| Face | Receives from `ContentItemPanel` |
| --- | --- |
| Restricted face | Its wording property, unchanged, once the face is built (`UI/Components/ContentItemPanel.Restricted.md §4.3`) |
| Add face | `contentItemSettingCollection`, `isLoading`, `isSubmitting`, `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`, `ariaLabel`, `titleText`, `showBorder`, `onAdded`, `onCancelled` |
| Edit face | `contentItem` (as a form item), `contentItemSettingCollection`, `showEditSection`, `isLoading`, `isSubmitting`, `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`, `showApprovalStatusRibbon`, `ariaLabel`, `titleText`, `showBorder`, `onModified` and `onCancelled` (each wrapped to close the editor), `onRemoved` |
| View templates | Every `ContentItemEvents`, `ContentItemText` and `ContentItemSectionToggles` member, plus `allowTitleClick`, `showApprovalStatusRibbon`, `showApprovalStatus`, `truncateAt`, `allowInPlaceExpansion`, and the decided values listed in section 4.1 |

The view templates' every property is reachable from the panel (`contentSlot` is the overrides'
derivation point, not a consumer's). The writing faces' properties are not: the face properties
the panel does not forward, `typeBlockedText` apart (below), are the section 10 gaps 6 and 7. Until
they are closed, a page that needs them renders the add or edit template directly *(code:
contentItemAddPanel.tsx — the component's header comment)*.

**One add-face property is withheld.** `typeBlockedText` is not forwarded to the add face. It
labels only a blocked type's tile, and `UI/Components/ContentItemPanel.Add.md rules 2.6 and
3.2.3` (user ruling 2026-09-27) remove that tile from the picker, so the property is retired with
it (`UI/Components/ContentItemPanel.Add.md §10 item 5`). The edit face's picker is frozen and
blocks no tile, so it never renders the property *(code: contentItemFormPanel.tsx —
renderTypePicker, `isTypeBlocked`)*.

**Three names collide; two are renamed when forwarded.** `authorLabelText`,
`shareabilityLabelText` and `saveButtonText` are `ContentItemText` members the panel already
hands the view templates, and the writing faces declare the same three names — `saveButtonText`
is the card's bookmark *Save* and the editor's *Save*. One panel feeds both faces, the card and
then the editor in place, so a property forwarded under one of those names would set both. The
user ruled on 2026-09-27 how they are forwarded when gaps 6 and 7 close:

| Form property | Forwarded as | Why |
| --- | --- | --- |
| `authorLabelText` | `authorLabelText`, one property shared with the card | The same text, with the same meaning, on the card and the form. |
| `saveButtonText` | a name of its own, distinct from the card's `saveButtonText` (for example `formSaveButtonText`) | The form's *Save* submits the amendment; the card's is its bookmark. |
| `shareabilityLabelText` | a name of its own, distinct from the card's `shareabilityLabelText` (for example `formShareabilityLabelText`) | The form asks *How are you permitted to share this?*; the card labels its *Shareability* badge. |

The ruling chose which property is shared, not the two new names: the names above are examples.
The task that builds the forwarding of section 10, items 6 and 7, fixes the two names and records
them in this table, per §UI20.6.5. Each of the two is a rename recorded under that rule.

## 5. Security Requirements

**Security and access matrix**

The view face's actions, and the restricted face shown in the add face's place. The writing
faces' are `UI/Components/ContentItemPanel.Add.md §5` and
`UI/Components/ContentItemPanel.Edit.md §5`. A read-only role blocks contributions, not reads
(§SEC18.6), so an action that only reads is blocked by none. Every read-only role in the
**Blocked by** column is composed by the panel itself, from what it represents: no page hands it
a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3). The
writing faces still accept one today, `blockRoles`, which is retired (section 10, item 24).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The title, or the quote or verse, as a way in (`onTitleClick`) | Every persona, where `allowTitleClick` is on and the hook is wired (rule 3.1.10) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onTitleClick` | Nothing of its own: the read it leads to answers under §SEC14.7 posture A rule 4 |
| *read more* (`onReadMore`) | Every persona, where the content is cut and expansion is not in place (rule 3.1.9) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onReadMore` | Nothing of its own, as the title |
| *read more…* / *show less* in place (`onExpandCollapse`) | Every persona, where `allowInPlaceExpansion` is on and the content overruns the cut (rule 3.1.9) | None — no request | ✅ Allowed | Offered; toggles in place and raises `onExpandCollapse` | Nothing — no request |
| The type chip, *Submitted by* and *Author* (`onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick`) | Every persona (`UI/Components/ContentItemPanel.Default.md rules 3.1.1 and 3.1.4`) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises the hook (rule 2.42) | Nothing of its own, as the title |
| A tag or Bible reference pill (`onTagClick`, `onBibleReferenceClick`) | Every persona, under the section switches and the setting (rule 3.1.12) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises the hook (rule 2.41) | Nothing of its own, as the title |
| The reaction counts | Every persona, where the summary is shown (rule 3.1.12) | None — no request | ✅ Allowed | Offered; toggles in place | Nothing — no request |
| **Like**: giving, changing or clearing the reader's own reaction (`onReactionSelected`) | Every persona, where the page's switch and the setting allow it (rule 3.1.8) | None: a reaction is not a contribution (user rulings 2026-09-27) | ✅ Allowed (user rulings 2026-09-27) | Offered; raises `onReactionSelected`, and the page sends them to sign in (rule 3.2.4; §UI20.6.6 rule 2) | Settled (user rulings 2026-09-27): the exemption from the association block roles is personal — only a reader giving, changing or clearing their own reaction is exempt — so giving or changing the reaction is a `POST`, and clearing it a `DELETE` whose gate exempts the row only when the caller is clearing their own (§SEC14.7 posture A′ rules 1 and 4); any other write to a reaction row, an Administrator's deletion of another reader's reaction included, stays under the read-only veto; clearing one's own reaction at any status, §APR9.9 rule 6 |
| The comments control (`onCommentsClick`) | Every persona, where comments are shown and the hook is wired (`UI/Components/ContentItemPanel.Default.md rule 2.9`) | None: it opens the comments and adds none. Adding a comment is refused to a blocked-role holder where comments are added (user ruling 2026-09-27). | ✅ Allowed | Offered; raises `onCommentsClick` | Nothing of its own, as the title |
| **Share** (`onShareClick`) | Every persona, where `showShareSection` is on and the hook is wired (rule 3.1.12) | None: sharing adds no content, so it is not a contribution (user ruling 2026-09-27) | ✅ Allowed (user ruling 2026-09-27) | Offered; raises `onShareClick` | Nothing of its own: the short links it would share are designed, not built (§DOM19.7) |
| **Save** (`onSaveClick`) | Every persona, where `showSaveSection` is on and the hook is wired (rule 3.1.12) | None (user rulings 2026-09-27) | ✅ Allowed (user rulings 2026-09-27) | Offered; raises `onSaveClick`, and the page sends them to sign in (§UI20.6.6 rule 2) | Save has no design yet. The user ruled on 2026-09-27 that the read-only veto does not reach a reader's own Save. |
| **Edit**, the owner's (`onEditClick`, or the editor in place) ≠ item 20 | The owner, where the page's Edit switch is on (rules 2.40, 3.1.4 and 3.2.3) | `ReadOnly`, `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` for the item's type (rule 3.2.1) | ❌ Refused: hidden, not locked (rule 3.2.1) | Not offered | §SEC14.7 posture A rule 3; the owner's amendment of a reviewed item forks (§APR9.9) |
| **Moderate** (`onModerateClick`) | The moderation tier (rule 3.2.2) | The same three (rule 3.2.1) | ❌ Refused: hidden (rule 3.2.1) | Not offered | Nothing of its own: it leads to the page's moderation surface, whose writes are decided there |
| **View** ≠ item 20 | Every persona, where the page's switch is on (rule 2.40) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises View's hook | Nothing of its own, as the title |
| **The restricted face** (a gated view, in the add face's place) ≠ `UI/Components/ContentItemPanel.Restricted.md §10 item 1` | A signed-in reader with no content type left to contribute (rule 2.43) | None — it is what a reader left with no type is shown | ✅ Shown in the add face's place, whether read-only roles or the settings left no type (rule 2.43) | Not offered: the add face's login link comes first (rule 2.43) | Nothing — it offers no action and sends no request (`UI/Components/ContentItemPanel.Restricted.md §5`) |

- **Render gates only** (rule 2.7). The server-side rules the gates mirror are §SEC14.6 (every
  service enforces its own), §SEC14.7 posture A (the contribution gate, modify, remove) and
  §SEC18.6 (role names and the block veto). The role sets are rule 2.17; the block veto is
  rules 2.18 and 2.20.
- **Identity** comes from the auth context — `userId` and the role names that
  `/api/accounts/me` returns — and ownership is an account-id comparison, never a display name
  (rules 2.16 and 3.2.3).
- **The view face's gates are hard-coded, not the rule 2.17 props.** See section 10, item 2.
- **No action is authorized by being shown.** A page that switches on `showEditSection` or wires
  `onModerateClick` is choosing what to offer; the write it leads to is refused by the service
  if the caller may not make it.

## 6. Composition and Usage

### 6.1 The family

```text
ContentItemListPanel                   composes the two below
├── ContentItemSearchBarPanel
└── ContentItemResultsPanel            one ContentItemPanel per element
    └── ContentItemPanel               this document — one item, on whichever face
        ├── ContentItemAddPanel        the add template
        ├── ContentItemRestrictedPanel the add template's replacement when no type is left (not built)
        ├── ContentItemEditPanel       the edit template
        ├── ContentItemDefaultPanel    the view template most types use
        └── overrides by ContentType   ContentItemQuotesPanel, ContentItemVerseImagePanel
```

*(code: contentItemPanel.tsx — the dispatch comment and templateOverrides; contentItemResultsPanel.tsx; contentItemListPanel.tsx)*

### 6.2 The faces, and the relocated rules that live in them

| Face | Document | What of §UI20.6.2 it holds |
| --- | --- | --- |
| Restricted | [ContentItemPanel.Restricted.md](ContentItemPanel.Restricted.md) | Nothing relocated: the face was designed by the user's ruling of 2026-09-27 |
| Add | [ContentItemPanel.Add.md](ContentItemPanel.Add.md) | The type picker, the narrow block on the picker, the content type offered only on add, the default-only resolution, the contribution half of the hidden-field rule, the `POST` row |
| Edit | [ContentItemPanel.Edit.md](ContentItemPanel.Edit.md) | The refusal while `showEditSection` is off, the frozen type tiles and chip, the amendment half of the hidden-field rule, the amendment row |
| Default view | [ContentItemPanel.Default.md](ContentItemPanel.Default.md) | The view templates' title and author shaping |
| Quotes view | [ContentItemPanel.ContentItemQuotesPanel.md](ContentItemPanel.ContentItemQuotesPanel.md) | Derivation from the default via `contentSlot` |
| Verse image view | [ContentItemPanel.ContentItemVerseImagePanel.md](ContentItemPanel.ContentItemVerseImagePanel.md) | Nothing — §UI20.6.2 does not name it |

### 6.3 What stands beside it

- Tags and bible references render in `AssociationPanel` and its wrappers, beside the panel
  (rule 2.35): `UI/Components/AssociationPanel.md §6`,
  `UI/Components/AssociationPanel.TagAssociationPanel.md §6` and
  `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §6`.
  A page that stands them beside the card turns the card's own tag and reference sections off
  so the same facts never show twice (`postDetail.tsx`, `myPostDetail.tsx`,
  `contentItemModerationDetailPage.tsx`).
- Approval controls render in `ReviewPanel` (rule 2.36): `UI/Components/ReviewPanel.md §6`.

### 6.4 Consumers

`/posts/contribute` renders the add face and owns the `POST`, the redirect to `/myposts` — the
contributor's own posts, never the acknowledgement's `Id` (§DOM3.4.2 rule 6) — the notification
and the validation readback. `/posts/{contentItemId}`
renders the view face with `showEditSection` left off; `/myposts/{contentItemId}` renders it with
editing in place; and the feeds (`/`, `/posts`, `/myposts`, `/Admin/Posts`) render every element
through this same panel via `ContentItemListPanel`. *(§UI20.6.2; §DOM3.4.2 rule 6)*

The invitation that leads a reader to `/posts/contribute` is `SharingPanel`: it shows to a
signed-out reader, is hidden from a holder of `ReadOnly` or `ContentItem-ReadOnly`, and shows to a
reader whose read-only roles are per content type alone
(`UI/Components/SharingPanel.md rules 2.9–2.11`). Those rules and rule 2.43 are the two halves of
one ruling of the user's (2026-09-27): the invitation, and the restricted face a reader with no
content type left meets in the form's place.

The page above the panel obeys rule 2.25 too: a heading that named a title the panel
deliberately hides would make the suppressed value the loudest thing on the screen, so
`/posts/{id}` resolves the effective row through the same shared projection and falls back to
the type's name. *(§UI20.6.2)*

`/Admin/Posts/{contentItemId}` also renders the panel, with `showModerationSection` and
`moderationOpensEditor` on, and opens `ContentItemEditPanel` directly from `onModerateClick`
*(code: contentItemModerationDetailPage.tsx)*. §UI20.6.2's consumer list does not name it; see
section 10, item 4.

## 7. Dependencies

**Data the consumer supplies:** the element (`ContentItemSearchItem`, carrying its winning
setting — rule 2.5), the `ContentItemSetting` rows for the writing faces, and the reaction
options (approved rows from `GET api/Reactions`).

**Direct API dependencies** (called by the consumer, never the component): *(§UI20.6.2)*

| Concern | Endpoint |
| --- | --- |
| The type picker and field shaping | `GET api/ContentItemSettings` (`[AllowAnonymous]`; `$filter=contentItemId eq null` for the defaults, plus `isAvailableAsGeneralUserContribution eq true` for the contribution surface). A page rendering one item may also pass that item's override row alongside the defaults — the panel resolves which wins. |
| The contribution | See `UI/Components/ContentItemPanel.Add.md §7`. |
| The item | `GET api/ContentItems/{contentItemId}` (`[AllowAnonymous]` — the service's own visibility filter decides what a caller may see) |
| An amendment | See `UI/Components/ContentItemPanel.Edit.md §7`. |

**Indirect dependencies:** the signed-in identity and roles (`/api/accounts/me` via the auth
context) for the render gates, and the item's `ApprovalStatus` for the non-owner edit gate.
*(§UI20.6.2)*

## 8. States, Validation and Feedback

- **Loading and submitting** — the writing faces' (`isLoading`, `isSubmitting`); see
  `UI/Components/ContentItemPanel.Add.md §8`. The view face has no loading state of its own. The
  writing faces' one loading state, their *Loading…* line, is not announced today
  (`UI/Components/ContentItemPanel.Add.md §10 item 9`), and nor is their submitting state
  (`UI/Components/ContentItemPanel.Add.md §10 item 11`). The restricted face has neither
  (`UI/Components/ContentItemPanel.Restricted.md §8`).
- **Validation** — rules 2.31–2.34. The notification of rule 2.34 is raised by the consumer:
  `contribute.tsx` calls `toastError` with the API's message *(code: contribute.tsx —
  addContentItemAsync)*.
- **Freshness** — rule 2.30. The panel keeps three pieces of per-card state of its own — which
  face shows, whether the reaction counts are expanded, whether the Like picker is open — none
  of which the consumer persists *(code: contentItemPanel.tsx — isEditorTaken,
  areReactionCountsExpanded, isReactionPickerOpen)*.
- **Confirmation** — removal is confirmed on the edit face (`UI/Components/ContentItemPanel.Edit.md §8`).
- **Empty and error** — the consumer's: the panel is not rendered until the page has an element
  or its settings (`contribute.tsx`, `postDetail.tsx`).

## 9. Styling and Accessibility

- The family's stylesheet is `src/components/contentItems/contentItems.css`. The type colour
  lives there and nowhere else: the chip and the selected tile carry the enum member name in
  `data-content-type`, and the palette is measured by
  `contentItemChipPalette.test.ts` *(test: contentItemChipPalette.test.ts — "should give every
  content type a colour of its own", "should keep every pair of types clearly tellable apart",
  "should keep every chip label readable on its own fill")*.
- Each face's own hooks are in its story's section 9.

## 10. Open Questions and Gaps

1. **Note — the owner-only Edit on the card, ruled.** Rule 2.17's edit set holds the publisher
   tier and `Administrators`, while the card's Edit renders for the owner alone and a non-owning
   publisher reaches the editor through Moderate or `mode="edit"` (`contentItemPanel.tsx` —
   showsEditButton). The user ruled on 2026-09-26 that this is the split: Edit is the owner's
   hook, and Reviewer, Publisher and Administrator get Moderate (rule 2.38). Where each hook
   leads is the page's (§UI20.6.4). The user's expectation for the pages: the owner's Edit may
   lead to `/myposts/{id}` where it is not an in-place swap to the edit template, and Moderate to
   `/Admin/Posts/{id}`, where the item's other moderation tasks are performed; that page's gap is
   `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`. The panel already behaves so.
2. (needs issue) **The role sets are not props of `ContentItemPanel`.** Rule 2.16 says every set but the
   block set is an overridable prop. `addRoles`, `editRoles`, `deleteRoles` and `entityType` exist
   on the writing faces only; the view face's moderation list is hard-coded
   (`contentItemPanel.tsx` — viewerModerates) and the panel forwards none of the faces' sets.
   The block set is not part of this gap: rule 2.16 keeps it composed by the panel, never a
   property (§UI20.6.6 rule 3), as the view face composes it today (`contentItemPanel.tsx` —
   isBlocked), and the writing faces' `blockRoles` is retired (item 24).
3. **Note — the view templates read facet pairs, ruled.** Rule 2.29, as relocated, said only
   that the facet pairs govern surfaces the panel does not own, while the view templates render
   tags, bible references, reactions and the comments control gated on `ShowTags`,
   `ShowBibleReferences`, `ShowReactions`, `ShowComments` and `ReactionsAllowed`
   (`contentItemDefaultPanel.tsx` — showsTags, showsBibleReferences, showsAssignedReactions,
   showsComments; `contentItemPanel.tsx` — offeredReactions). The user ruled on 2026-09-26 that
   the page's switch and the setting work together as an AND: display needs the page's switch
   AND `Show<Feature>`, a contribution control needs the page's switch AND `<Feature>Allowed`.
   Rule 2.29 now says so. The switches' meaning is §DOM6.1's, unchanged. The panel already
   behaves so (rules 3.1.8 and 3.1.12).
4. **Note — where a contribution redirects, corrected.** Section 6.4, as relocated, said
   `/posts/contribute` redirects to `/myposts/{contentItemId}` (commit `29cf444c`,
   2026-09-01). §DOM3.4.2 rule 6, settled in #412 (commit `93d1b6bd`, 2026-09-07), superseded
   it: the client must not act on the acknowledgement's `Id`, because following a duplicate's
   lands on a 404 that names the duplicate, so the contribution page thanks the contributor and
   returns them to their own posts. Section 6.4 now says so and cites the rule, and the code
   follows it (`contribute.tsx` — addContentItemAsync; test: `contribute.test.tsx` — "should
   thank the contributor and land on their posts once it is submitted"). The relocated list
   also omits `/Admin/Posts/{contentItemId}`; section 6.4's last paragraph names it.
5. (needs issue) **Gap — the card does not always render under the winning row.** The user ruled
   on 2026-09-26 that a setting always applies and the most specific one wins — the item's own
   override over its type default (§DOM6.4) — and that the card must always render under the
   winning row, resolving it from the collection it was handed when the element does not carry
   it (rule 2.39). That a type default always exists is §ARC12.5.2 business rule 5 (#387: the
   default refuses removal and the seed restores a missing one); the domain design states the two
   tiers and the precedence (§DOM6.2–§DOM6.4) but not that invariant. The component departs
   from it: the view face reads only the element's embedded setting and never consults
   `contentItemSettingCollection` (`contentItemPanel.tsx` — contentItemSetting, line 288 at
   70dc72e7). Resolving from the collection would not reach every card alone.
   `ContentItemListPanel` deliberately withholds the settings collection from its cards, so a
   listed card can never fall into the add face (`UI/Components/ContentItemListPanel.md rule
   2.7`, tagged [Won't]), and `/posts/{id}` hands the card none either (`postDetail.tsx`). What
   those cards rely on instead is the element carrying its winning setting (rule 2.5), which
   each page projects into it; that the pages render the card before that setting has arrived
   is the pages' half, recorded in each page's document: `UI/Pages/Home.md §6 item 1`,
   `UI/Pages/Posts.md §6 item 1`, `UI/Pages/PostDetail.md §6 item 1`, `UI/Pages/MyPosts.md §6
   item 1`, `UI/Pages/MyPostDetail.md §6 item 3`, `UI/Pages/ContentItemModerationPage.md §6
   item 1` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 2`.
6. (needs issue) **Add-face properties the panel cannot reach.** `ContentItemPanel` does not forward
   `cssClass`, `entityType`, `addRoles`, `loginHref`, `loginButtonText`,
   `loginButtonCssClass`, `submitButtonCssClass`, or any of the form's text props
   (`typePickerTitleText`, `titleLabelText`, `titlePlaceholderText`, `authorLabelText`,
   `authorPlaceholderText`, `authorPrefilledHintText`, `contentLabelText`,
   `shareabilityLabelText`, `sharePermissionLabelText`, `sharePermissionPlaceholderText`,
   `sharePermissionRequiredText`, `submitAsLabelText`, `maxLengthExceededText`,
   `submitButtonText`, `cancelButtonText`, `validationSummaryText`, `blockedText`,
   `noTypesText`, `loadingText`) to `ContentItemAddPanel`
   (`contentItemPanel.tsx`, the `ContentItemAddPanel` render). Forwarded, `authorLabelText` is
   the card's own property and `shareabilityLabelText` takes a name of its own (section 4.3).
   `typeBlockedText` is not part of this gap: the panel withholds it (section 4.3). Nor, once the
   restricted face is built, is `noTypesText`: it words one of the two refusals that face replaces
   (rule 2.43; `UI/Components/ContentItemPanel.Add.md §10 item 10`). Nor is `blockRoles`, which is
   retired rather than forwarded (item 24).
7. (needs issue) **Edit-face properties the panel cannot reach.** It does not forward `cssClass`,
   `entityType`, `editRoles`, `deleteRoles`, `submitButtonCssClass`,
   `deleteButtonCssClass`, or the form's text props (those in item 6 that the edit face
   renders, plus `saveButtonText`, `deleteButtonText`, `deleteConfirmTitleText`,
   `deleteConfirmMessageText`, `deleteConfirmButtonText`, `typeLabelText`) to
   `ContentItemEditPanel` (`contentItemPanel.tsx`, the `ContentItemEditPanel` render).
   Forwarded, `authorLabelText` is the card's own property, and `shareabilityLabelText` and
   `saveButtonText` take names of their own (section 4.3). `blockRoles` is not part of this gap:
   it is retired rather than forwarded (item 24).
8. (needs issue) **The doc page needs updating.** `contentItemPanelDoc.tsx`'s props table has no rows for
   `isLoading`, `approvalStatusDefault`, `ariaLabel`, `titleText`, `showBorder` or the
   `ContentItemText` members, all of which `ContentItemPanelProps` declares; and its `mode`
   row says the editor is "still subject to showEditSection" without the second condition the
   code adds — the page must listen on `onModified` or `onRemoved` (rule 3.1.2).
9. **Moved to the page documents.** Page gap — `/Admin/Posts/{id}`: a reviewer or publisher
   cannot use it — now `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`.
10. **Moved to the page documents.** Stale comment — `postDetail.tsx` — now
    `UI/Pages/PostDetail.md §6 item 7`.
11. **Note — how the three colliding properties are forwarded, ruled.** `authorLabelText`,
    `shareabilityLabelText` and `saveButtonText` are `ContentItemText` members for the card and,
    under the same names, form properties (section 4.3). The user ruled on 2026-09-27 that
    `authorLabelText` is one property the card and the form share, and that the form's Save and
    shareability texts are forwarded under names of their own, each a rename recorded under
    §UI20.6.5. The ruling chose which property is shared, not the two new names: section 4.3
    gives example names, and the task that builds the forwarding of items 6 and 7 fixes the two
    names and records them in section 4.3, per §UI20.6.5.
12. Built (#740) **The panel raises the reaction hook for every reader.** Rule 3.2.4 (user
    ruling 2026-09-27) has the panel raise `onReactionSelected` whatever the sign-in state and
    never redirect. Sending a signed-out reader to sign in, with return information, and
    persisting or clearing a signed-in reader's reaction are the page's (§UI20.6.4). The panel
    raises the hook for every reader and composes no route
    (`src/components/contentItems/contentItemPanel.tsx`, the `onReactionSelected` handler,
    lines 454-457 at f376e6b0; #740), and every page that renders the view face inherits it,
    the list pages through `ContentItemListPanel`. The pages' half is `useContentItemEngagement`,
    which the seven pages `DesignFeatures/UI/Hooks/ContentItemEngagement.md` names take
    `onReactionSelected` from: it sends a signed-out reader to sign in (its §2 rule 2; #739), so
    no signed-out reader's choice is dropped — the silent failure #622 moved the redirect into the
    card to prevent.
13. **Note — the lock after review reaches content items, ruled.** This item asked whether the
    user's ruling of 2026-09-26 — the owner may withdraw their submission only while it is Draft
    or Submitted, and once reviewed it is locked — reaches content items, and whether "locked"
    withholds the owner's deletion alone or their amendment too. The user ruled on 2026-09-27
    that it holds for everything subject to approval (§APR9.9): the owner may soft-delete only
    while the row is `Draft` or `Submitted`; an `Approved` or `Rejected` row is locked to its
    owner, with no in-place edit and no delete; the owner's edit of a reviewed versioned item
    forks a new version and never deletes the reviewed one; and an `Administrators` takedown at
    any status stays. `ContentItem` is versioned (§APR7.5.1), so the owner still amends a
    reviewed item, the amendment forking (§DOM3.4 rule 16): the owner's Edit and the edit face's
    editor stand as they were. The owner's delete does not. Rule 2.17's delete set, as relocated,
    admitted the owner at any status; it now confines the owner to `Draft` and `Submitted` and
    cites §APR9.9 (§UI20.6.4 case 1). The edit face still offers the owner *Delete* on a reviewed
    item (`UI/Components/ContentItemPanel.Edit.md §10 item 5`); the server's side is item 19. A
    reader's own reaction is not a contribution, and the lock does not
    reach it (§APR9.9).
14. **Note — the moderation button's label, ruled.** This item asked whether the moderation
    button's label should depend on what the viewer may do on the page it leads to, or be a label
    the page sets. Rule 3.1.6 dresses Moderate in Edit's pencil and the label *Edit* wherever
    `showModerationSection` is on, for every moderation tier (`contentItemPanel.tsx` —
    moderateButtonLabel), so a reviewer, who may see the item there but not modify it
    (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`), was shown a button reading *Edit*.
    The user ruled on 2026-09-27 that Edit and Moderate are two buttons with two purposes: in the
    user area a card may show both, Edit — the owner's — leading to `/myposts/{id}`, and Moderate —
    the moderation tier's — to `/Admin/Posts/{id}`; the page may override either label to suit
    where the card stands, and in the admin area may set Moderate's to *Edit*, since there it
    opens the item for modification. No label depends on the viewer's role. Rules 2.38 and 3.1.6
    now say so. The user's word **Manage** for the link, in the ruling of 2026-09-27 on who may
    change an item's settings, names the same button and does not set its text. That the two
    labels are not yet properties is item 21.
15. **Moved to the page documents.** Page gap — the card renders before the settings read lands —
    now `UI/Pages/Home.md §6 item 1`, `UI/Pages/Posts.md §6 item 1`, `UI/Pages/PostDetail.md §6
    item 1`, `UI/Pages/MyPosts.md §6 item 1`, `UI/Pages/MyPostDetail.md §6 item 3`,
    `UI/Pages/ContentItemModerationPage.md §6 item 1` and
    `UI/Pages/ContentItemModerationDetailPage.md §6 item 2`.
16. **Note — the settings-gated parts while a card has no setting, ruled.** This item asked
    whether, while a card has no setting — a page's settings read still in flight, or failed —
    its settings-gated parts should show, as today, or stay hidden until the setting arrives.
    The code shows them: it withholds a part only where its flag is `false`, so a card with no
    setting shows every section and offers Like (`contentItemDefaultPanel.tsx` — showsTags,
    showsBibleReferences, showsAssignedReactions, showsComments; `contentItemPanel.tsx` —
    offeredReactions). The user ruled on 2026-09-27 that items are hidden until their settings
    load, and confirmed that the whole card waits: a card is rendered only with its setting, and
    the page holds it, showing its announced loading state (§UI20.6.6 rule 5), until the setting
    has loaded. A failed read is a separate case, which the user ruled on later the same day: the
    page shows its announced error with a Retry in place of the cards — never the cards without
    their settings. Rule 2.26 now says both. What the card shows when handed no setting is therefore not
    a state the design relies on, and rules 2.26 and 2.29 and
    `UI/Components/ContentItemPanel.Default.md rules 2.5 and 3.3.1` stand as written. Holding the
    card is each page's: the pages' gaps are those item 5 lists.
17. **Moved to the page documents.** Page gap — the cards show no counts — now
    `UI/Pages/Home.md §6 item 2`, `UI/Pages/Posts.md §6 item 2`, `UI/Pages/PostDetail.md §6 item
    2`, `UI/Pages/MyPosts.md §6 item 2`, `UI/Pages/MyPostDetail.md §6 item 4`,
    `UI/Pages/ContentItemModerationPage.md §6 item 2` and
    `UI/Pages/ContentItemModerationDetailPage.md §6 item 3`.
18. **Note — which setting fields may change once a row exists, and `HasTitle` on quotes and
    verse images, ruled.** This item asked whether §ARC12.5.2 business rule 9's list of
    amendable setting fields — then the six facet pairs and `LimitReactionsToLoveOnly` — stood,
    or widened to what the settings admin page edits; and, if it widened, whether
    `HasTitle = true` should be refused for `Quote` and `VerseImage`, on a row that exists and on
    an override added for one item alike. The code enforced no list: the foundation compares only
    the control fields on modify (`ContentItemSettingService.Validations.cs` —
    `ValidateAgainstStorageContentItemSettingOnModify`), and the add path admits any `HasTitle`
    on an override (`ContentItemSettingOrchestrationService.cs` — `AddContentItemSettingAsync`),
    all at 70dc72e7. The user ruled on 2026-09-27 that settings can change on rows: rule 9's list
    widens to every field the settings admin page edits (§ARC12.5.2 business rule 9). The server
    refuses `HasTitle = true` for `Quote` and `VerseImage`, on a type default and on an item
    override alike, for every caller (§ARC12.5.2 business rule 11; not yet built). So a quote or a
    verse image never carries a title, which
    `UI/Components/ContentItemPanel.ContentItemQuotesPanel.md rule 3.1.5`,
    `UI/Components/ContentItemPanel.ContentItemVerseImagePanel.md rule 3.1.5` and
    `UI/Components/ContentItemPanel.Default.md rule 2.4` now cite; and the two relocated sentences
    that assume a setting changes after items exist, `UI/Components/ContentItemPanel.Edit.md
    rule 2.10` and rule 2.24, describe a real case. `ContentItemSettingsPanel` still edits only
    its own fields (`UI/Components/ContentItemSettingsPanel.md rule 2.9`). The user also ruled
    that a changed setting reaches a page already open without a reload, by a mechanism like the
    one that updates the reaction counts after a vote. Rule 2.44 is the card's half: it takes the
    change when the page hands it the new setting. How the page learns of the change was then
    ruled too: live updates across the site get their own design, #702 (`DESIGN: Push Live
    Updates To Open Pages`), which §ARC12.5.2 business rule 12 points at, and which was written as
    `DesignFeatures/LiveUpdates.md` on 2026-10-06.
19. **Note — the server's removal rule agrees; its gate is not yet built to it.** §SEC14.7
    posture A rule 3 confines the owner's removal to a stored row that is `Draft` or `Submitted`
    and keeps `Administrators` at any status (§APR9.9), as rule 2.17 now does. The foundation's
    removal gate still admits the owner at any status (`ContentItemService.Validations.cs` —
    `ValidateUserCanRemoveStorageContentItemAsync`, lines 276-299 at 70dc72e7). §APR9.9 records
    that server work as not yet built, so this document carries no marker for it; the
    component's own side is `UI/Components/ContentItemPanel.Edit.md §10 item 5`.
20. (needs issue) **The card has no View action, and no switch for View or Edit.** Rule 2.40
    (user ruling 2026-09-27) gives the card two actions with two hooks, each switched by a
    `show…` property of its own. The card offers Edit alone, rendered for the owner wherever
    `onEditClick` is wired or the editor opens in place, with no switch of its own, and it has no
    View control, hook or switch (`contentItemPanel.tsx` — `showsEditButton`, lines 390-394 at
    70dc72e7; `contentItemTemplate.ts` — `ContentItemEvents`). `ContentItemListPanel` stands in
    for the missing View by relabelling Edit (`UI/Components/ContentItemListPanel.md §10 item
    9`).
21. (needs issue) **Visible strings that are not properties.** Rule 3.1.15 (§UI20.6.6 rule 1).
    The panel composes the moderation action's two labels, *Moderate* and *Edit*
    (`contentItemPanel.tsx` — `moderateButtonLabel`, line 412 at 70dc72e7), and the locked
    action's reasons, *Locked for moderation* and *Locked for editing*, which show as its tooltip
    and are read with its name through a visually hidden copy, a string for a screen reader
    alone that §UI20.6.6 rule 1 covers too (`contentItemTemplate.ts` — `lockReasonForActionLabel`, lines
    180-186 at 70dc72e7). No property sets any of them. The user ruled on 2026-09-27 that each label is
    a property the page sets (rule 3.1.6; item 14).
22. **Note — Share for a holder of a read-only role, ruled.** This item asked whether a holder of
    `ReadOnly`, `ContentItem-ReadOnly` or `ContentItem-{ContentType}-ReadOnly` for the item's
    type should be offered *Share*, which the card offers to every persona where the page switches
    `showShareSection` on and wires `onShareClick`, asking no role (rule 3.1.12;
    `contentItemDefaultPanel.tsx` — `showsShare`). The user ruled on 2026-09-27 that sharing adds no
    content: like Like and Save, it is not contributing, so no read-only role restricts it. The
    security and access matrix in section 5 says so, and the card already behaves so.
23. **Note — a page-set block list, ruled.** Asked on 2026-09-27 whether `AssociationPanel`
    should take a property listing its blocking roles, which a page could extend, like the add
    form's `blockRoles`, the user answered that each component knows what it represents, and so
    which read-only roles apply to it, and that each component document spells them out in its
    security matrix; §UI20.6.6 rule 3 says so for every component. This family was designed
    earlier the other way: rule 2.16, a relocated rule, made every role set an overridable
    property, the block set included, and the writing faces declare `blockRoles`, a property
    whose default is the composed set (`contentItemFormPanel.tsx` — `blockRoles`, lines 193 and
    407 at 70dc72e7), while the view face composes its block list with no property
    (`contentItemPanel.tsx` — `isBlocked`, lines 328-332 at 70dc72e7). This item asked which stood. The user
    ruled on 2026-09-27 that no component takes a blocking-role list from its page: `blockRoles`
    is retired, and each component composes its read-only roles itself, which a page can neither
    add to nor remove from (§UI20.6.6 rule 3). Rule 2.16 now excludes the block set (§UI20.6.4
    case 1), and rule 2.17's block set is composed, never a property. Items 2, 6 and 7 here and
    `UI/Components/ContentItemListPanel.md §10 item 3` no longer ask for it to be exposed or
    forwarded. Removing the property from the code is item 24.
24. (needs issue) **`blockRoles` is still a property of the writing faces.** Rule 2.16 (user
    ruling 2026-09-27; §UI20.6.6 rule 3) retires it: the block set is composed by the component,
    and no page supplies it. The form engine the add and edit faces share declares `blockRoles`
    and uses it in place of the composed set whenever a page passes one
    (`contentItemFormPanel.tsx` — `ContentItemFormPanelProps.blockRoles`, line 193 at 70dc72e7,
    and `parseRoles(blockRoles ?? defaultBlockRoles)`, line 407), and both faces take it through
    `Omit<ContentItemFormPanelProps, 'contentItem'>` (`contentItemAddPanel.tsx`, line 7;
    `contentItemEditPanel.tsx`, line 9). The add face's sample page sets it through its persona
    board (`contentItemAddPanelDoc.tsx` — `blockRoles`, lines 51, 157, 263 and 334). The rows
    are `UI/Components/ContentItemPanel.Add.md §4.1` and
    `UI/Components/ContentItemPanel.Edit.md §4.1`.
