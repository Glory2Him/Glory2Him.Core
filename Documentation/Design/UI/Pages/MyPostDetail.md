# 1. MyPostDetail

One of the contributor's own posts, read on its own surface. The contribution page's thanks leads
the contributor to `/myposts`, and every way into an item from `/myposts` lands here. The page is
the contributor's view of their own item. It keeps the public detail page's two-column split, and
adds the way back to their list and editing in place. It is used by any signed-in reader, and is
meant for the item's owner.

- **Route:** `/myposts/{contentItemId}` (`src/routes/publicPostRoutes.tsx` lines 50-60 at 70dc72e7)
- **Source:** `src/pages/myPostDetail.tsx`; its tests are
  `src/pages/myPostDetail.test.tsx` and `src/pages/likeControlSurfaces.test.tsx`
- **Section:** user
- **Access:** any signed-in reader. The route is wrapped in `SecuredRoute` with no role list
  (`publicPostRoutes.tsx` lines 56-59 at 70dc72e7). The guard renders nothing while the sign-in
  state is being read, and gives a signed-out visitor its *Access Restricted* alert and *Login*
  button. The page does not ask whether the reader owns the item (item 10).
- **Layout:** two columns — main with right sidebar
- **Components:** [ContentItemPanel.md](../Components/ContentItemPanel.md), on a view template
  ([ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md)
  or [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md))
  and, once Edit is taken, on its edit face
  ([ContentItemPanel.Edit.md](../Components/ContentItemPanel.Edit.md));
  [AssociationPanel.TagAssociationPanel.md](../Components/AssociationPanel.TagAssociationPanel.md)
  and [AssociationPanel.BibleReferenceAssociationPanel.md](../Components/AssociationPanel.BibleReferenceAssociationPanel.md),
  each rendering [AssociationPanel.md](../Components/AssociationPanel.md);
  [SharingPanel.md](../Components/SharingPanel.md). The one undocumented building block is
  `Spinner` (`src/components/coreUI/spinner.tsx`), a core-UI primitive. The *Back to my posts* link
  is the page's own.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** Only a signed-in reader reaches the page. *(code: publicPostRoutes.tsx — the `myposts/:contentItemId` route; code: securedRoutes.tsx — `SecuredRoute`)*

**2.2 [Must]** The item comes from the route. The page reads it with the caller-scoped item read, which shows an owner their own row at any status. It reads the item's effective settings alongside: the content type defaults plus this item's own override. *(code: myPostDetail.tsx — `useGetContentItemById`, `useGetEffectiveSettingsFor`; code: publicPostRoutes.tsx — the route comment)* ≠ item 3

**2.3 [Must]** The owner edits in place. The card has `showEditSection` on and the page listens on `onModified`, so the owner's Edit swaps the card for the edit template (`UI/Components/ContentItemPanel.md rule 2.9`). A save sends the stored row with the amendments laid over it, and the card then re-renders from what storage holds, its status included. Where the save forks a new version — the item was `Approved` or `Rejected` (rule 2.6) — the card shows that new version, so the amendment appears (`UI/Components/ContentItemPanel.md rule 2.11`). *(code: myPostDetail.tsx — `saveChangesAsync`, the comment above `useModifyContentItem`; test: myPostDetail.test.tsx — "should send the whole row with the amendment over it"; §APR9.9)* ≠ item 14

**2.4 [Must]** A refused save is read back onto the form the contributor is looking at, and its reason is toasted. The API is the authority on what an item must carry, so the page pre-judges nothing. *(code: myPostDetail.tsx — `saveChangesAsync`, `toContentItemApiFailure`)*

**2.5 [Must]** Edit is the owner's action, and the edit from `/myposts` MUST open this page straight in edit mode, while View opens it read-only. *(user, 2026-09-27)* ≠ item 1

**2.6 [Must]** The owner may withdraw their item while it is `Draft` or `Submitted`. Once it is `Approved` or `Rejected` it is locked to them, and an amendment of it forks a new version (§APR9.9 rules 2-4; `UI/Components/ContentItemPanel.md rule 2.17`). The owner is never offered *Delete* on an `Approved` or `Rejected` item. *(§APR9.9; user, 2026-09-27)* ≠ item 2

**2.7 [Must]** The page offers the Like control on the card, and Share and Save deliberately not. Leaving Like off would lose a control the reader had on `/myposts`, and a page that passes no handler would be a second switch no `ShowReactions` setting can reach (§DOM6.5). Share and Save are left off because this page reads items that may be Drafts, and the address Share copies, `/posts/{id}`, answers nothing for one. *(code: myPostDetail.tsx — the comment above `useContentItemEngagement`; test: myPostDetail.test.tsx — "should add only the like control to the newly wired pages", "should offer the like control on my own post's detail page")* ≠ item 4

**2.8 [Must]** The card's tag and Bible reference sections are off, and the two association panels stand in the right-hand column, so the same facts never show twice on one screen. *(code: myPostDetail.tsx — the comment above `ContentItemPanel`; `UI/Components/ContentItemPanel.md §6.3`)*

**2.9 [Must]** The content stands whole (`showContentExpanded`), because a cut with a *read more* that led here would point at itself. *(code: myPostDetail.tsx — the comment above `readItem`)*

**2.10 [Should]** The card wears its status as the corner ribbon, and the pill is off, as on `/myposts`. *(code: myPostDetail.tsx — the `showApprovalStatus={false}` comment)*

**2.11 [Should]** The page's heading is visually hidden, because the card carries the visible title. The heading and the tab title follow the effective setting: the item's title where `HasTitle` allows and one exists, and the content type's name otherwise. *(code: myPostDetail.tsx — `pageHeading`, `useDocumentTitle`)*

**2.12 [Could]** *Back to my posts* always leads to the list it names, `/myposts`: as the reader left it when they came from it, and the bare list otherwise — never to another page an origin in router state names. *(user, 2026-09-27; test: myPostDetail.test.tsx — "should offer the way back to my posts")* ≠ item 15

**2.13 [Should]** While the item loads, a spinner stands in for the page. When the item cannot be read, the page says "We could not load this contribution right now. It may have been removed, or it may not be yours to read." *(code: myPostDetail.tsx — the `isLoading` and `isError` branches)*

**2.14 [Must]** The association panels take their collections from this page, keyed on the item id in the URL. Associations have no HTTP exposer yet (§ARC17.4, not yet built; item 5), so until then the collections are empty rather than invented. *(code: myPostDetail.tsx — the comments above the two panels)* ≠ item 5

**2.15 [Must]** A click on the card's type chip, *Submitted by* or *Author*, or on a tag chip in the side panel, raises its hook, and the page opens the journal's search, `/posts`, handed the value: its search bar shows it in the matching box — Category, Submitted by, Author or Tags, each one of the bar's advanced boxes — with the advanced section expanded (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). *(user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.42`)* ≠ items 7 and 12

**2.16 [Must]** The page hands both association panels the item's content type, so that each composes the post's own read-only roles — `ContentItem-ReadOnly`, and `ContentItem-{ContentType}-ReadOnly` for the item's type — and withholds its suggest box from a holder; the chips stay visible to them (`UI/Components/AssociationPanel.md rule 2.30`). *(user, 2026-09-27)* ≠ item 13

**2.17 [Must]** The invitation to contribute, `SharingPanel`, leads a signed-in reader to the contribution page, `/posts/contribute`, carrying this page's path and query as `from`. The route admits no signed-out reader (rule 2.1), so no sign-in redirect arises here. *(user, 2026-09-27; code: myPostDetail.tsx — the `SharingPanel` element; `UI/Components/SharingPanel.md rules 2.9–2.11`)*

**2.18 [Must]** The page shows the reader their own items alone. An item they did not contribute is answered as not found, whoever they are — the message of rule 2.13, which the page shows for any item it cannot read — so the page never reveals that the item exists. Another's approved item is read on `/posts/{id}`, and a moderator reads any item on `/Admin/Posts/{id}`. *(user, 2026-09-27)* ≠ item 10

**2.19 [Must]** View is switched off: the reader is already on the item's detail view, where View leads from `/myposts` (`UI/Pages/MyPosts.md rule 2.15`), so the card does not offer it here. *(user, 2026-09-27: View and Edit are each "configurable via the Show* properties"; `UI/Components/ContentItemPanel.md rule 2.40`)* ≠ item 9

**2.20 [Could]** The page may offer Moderate to an owner who also holds the moderation tier, leading to the item's admin address, `/Admin/Posts/{id}`, as `/myposts` does (`UI/Pages/MyPosts.md rule 2.8`): it is role-based functionality they hold. *(user, 2026-09-27)* ≠ item 11

**2.21 [Must]** The card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the card back, the spinner of rule 2.13 standing in its place, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the card's place — never the card without its setting. *(user, 2026-09-27)* ≠ item 3

**2.22 [Must]** A change to the item's setting reaches the open page without a reload: comments switched off for the item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is designed under #702, *Push Live Updates To Open Pages*; until it is designed and built, nothing pushes a change to the page. *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 16

**2.23 [Could]** A Bible reference in the side panel that cannot be read as a passage leads to the Bible reference page all the same, which says it could not be found and offers the search (`UI/Pages/BibleReference.md rule 2.18`). *(user, 2026-09-28)* ≠ item 17

## 3. Layout

Two columns inside the public chrome: the item on the left, and on the right the surfaces that
belong beside it. The *Back to my posts* link stands above both. Below the `lg` breakpoint the
right-hand column stacks under the item.

```text
+----------------------------------------------------------------------+
| site header                                                          |
+----------------------------------------------------------------------+
| [<- Back to my posts]                                                |
| +-------------------------------------+ +--------------------------+ |
| | ContentItemPanel                    | | TagAssociationPanel      | |
| |   the card, or the editor in place  | | BibleReferenceAssoc...   | |
| |                                     | | SharingPanel             | |
| +-------------------------------------+ +--------------------------+ |
+----------------------------------------------------------------------+
| site footer                                                          |
+----------------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Above the columns | full width of the page `container` | *Back to my posts* |
| Main | `col-lg-7` | the visually hidden `h1`; `ContentItemPanel` — the card, or its edit face while Edit is taken. The spinner or the error alert stands in for both columns until the item is read. |
| Right sidebar | `col-lg-5` | `TagAssociationPanel`; `BibleReferenceAssociationPanel`; `SharingPanel` |

*(test: myPostDetail.test.tsx — "should stand the item in the seven beside a five", "should stand the association and sharing surfaces in the five", "should render the item itself on the left")*

## 4. Components and their hooks

### 4.1 ContentItemPanel — the card, and its editor in place

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `contentItem` | The item projected with its winning setting, and the visit's chosen reaction folded in | Rule 2.2. |
| `contentItemSettingCollection` | The settings read (`?? []`) | The editor's frozen tiles and fallback. |
| `showEditSection` | `true` | Rule 2.3. |
| `onModified`, `validationIssues`, `isSubmitting` | `saveChangesAsync`; the last refusal's field messages; the modify write's pending state | Rules 2.3 and 2.4. |
| `showApprovalStatusRibbon` / `showApprovalStatus` | `true` / `false` | Rule 2.10. |
| `showContentExpanded` | `true` | Rule 2.9. |
| `showTagSection`, `showBibleReferenceSection` | `false` | Rule 2.8. |
| `reactionOptions`, `onReactionSelected` | The approved reactions, and the engagement hook's handler | Rule 2.7. |

The page does not set `allowTitleClick`, which stays off, so the title is plain text. It wires no
`onShareClick` or `onSaveClick` (rule 2.7), no `onCommentsClick`, and no `onEditClick` or
`onModerateClick`. It passes no `submittedByDisplayName`. The owner's own name is what the
owned-basis prefill falls back to, and only the owner reaches the editor here.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onModified` ≠ item 14 | The editor's *Save* is pressed. The panel closes the editor first. | `PUT api/ContentItems` with the stored row and the amendments over it; shows the new version where the save forked one. A refusal is read back onto the form and toasted (rules 2.3 and 2.4). | ✅ Yes (`myPostDetail.tsx`, lines 79-96 and 190), bar the forked version; item 14 |
| `onRemoved` | The editor's *Delete* is confirmed | Nothing: the page does not listen, so a confirmed Delete sends nothing. ≠ item 2 | ❌ No — item 2 |
| `onCancelled` | The editor's *Cancel* is pressed | Nothing. The panel closes the editor and discards the draft itself (`UI/Components/ContentItemPanel.md rule 2.11`). | *Not wired — nothing to do* |
| `onAdded` | The add face submits | — | *Never raised*: the panel is always handed an item (`UI/Components/ContentItemPanel.md rule 2.6`) |
| `onEditClick` | The owner's Edit is pressed and the editor does not open in place | The edit intent `/myposts` sends is not acted on. ≠ item 1 | *Never raised*: the editor opens in place (`UI/Components/ContentItemPanel.md rule 3.1.4`) |
| `onModerateClick` | Moderate is pressed by an owner who also holds the moderation tier | Navigates to `/Admin/Posts/{id}` (rule 2.20). ≠ item 11 | ❌ No — the page wires no `onModerateClick`, so Moderate does not render here; item 11 |
| `onReactionSelected` | The reader chooses a reaction | Records, changes or clears the reader's own reaction. ≠ item 4 | ❌ No — the choice is held in page state for the visit, and nothing is recorded (`useContentItemEngagement.ts`, lines 34-42); item 4 |
| `onShareClick`, `onSaveClick` | *Share* or *Save* is pressed | Not offered (rule 2.7). | *Not wired — switched off* (`myPostDetail.tsx`, the comment above `useContentItemEngagement`) |
| `onTitleClick` | The title is pressed | The title is plain heading text: this page is the detail surface. | *Not wired — switched off* (`UI/Components/ContentItemPanel.md rule 3.1.10`) |
| `onReadMore`, `onExpandCollapse` | *read more* is pressed | Never offered: the content stands whole (rule 2.9). | *Not wired — switched off* |
| `onTagClick`, `onBibleReferenceClick` | A pill on the card is pressed | The card's tag and reference sections are off (rule 2.8), so they never render. The panels beside the card carry them (section 4.2). | *Not wired — switched off* |
| `onCommentsClick` | The comments control is pressed | — | *Not wired — switched off*: without it the control does not render (`UI/Components/ContentItemPanel.Default.md rule 2.9`) |
| `onContentTypeClick` | The type chip is pressed | Opens `/posts` handed the type, its search bar showing it in the Category box with the advanced section expanded (rule 2.15). Today nothing: the chip renders as a button with no hook behind it. ≠ item 12 | ❌ No — item 12 |
| `onAuthorClick` | *Author* is pressed, where the type has an author and the item carries one | Opens `/posts` handed the author, as the type chip (rule 2.15). Today nothing: the segment renders as a button with no hook behind it. ≠ item 12 | ❌ No — item 12 |
| `onSubmittedByClick` | *Submitted by* is pressed | Were it to render, the page would open `/posts` handed the submitter (rule 2.15). | *Never raised*: the projection carries no submitter name, so the segment does not render (`toContentItemSearchItem.ts`, lines 62-80) |
| View's hook ≠ item 9 | View is pressed | — | *Not wired — switched off*: the reader is on the item's detail view already (rule 2.19). The card has no View yet (`UI/Components/ContentItemPanel.md §10 item 20`), and the page is to set View's switch off once it has one; item 9 |

### 4.2 TagAssociationPanel and BibleReferenceAssociationPanel

The page renders both stories the same way and sets the same properties on each.

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `associationCollection` | `[]` | Rule 2.14. |
| `onAdd` | a toast, "Suggesting tags is coming soon." or "Suggesting bible references is coming soon." | The association writes are not yet built (rule 2.14; item 5). |
| `showBorder`, `cssClass` | `true`, `mb-4` | Spacing in the column. |
| The post's content type (`UI/Components/AssociationPanel.md rule 2.30`) | Not passed; the panel has no such property yet (`UI/Components/AssociationPanel.md §10 item 20`) | Rule 2.16; item 13. |

Everything else keeps the story's defaults (`UI/Components/AssociationPanel.TagAssociationPanel.md
rule 3.1.1`, `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 3.1.1`). So
`showAdd` is on, `showModerationActions` is off, and the chip's link is the story's own default.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` | A suggestion is committed in the add box | Toasts that suggesting is coming soon, and sends nothing. ≠ item 5 | ❌ No — item 5 |
| `chipOnClick` or `chipHrefFor` | A chip's label is pressed | The page supplies both links: a reference leads to the page showing the verse, `/BibleReferences/{USFM}` (`UI/Pages/BibleReference.md`), or, where it cannot be read as a passage, to that page all the same, which says it could not be found (rule 2.23); a tag opens `/posts` handed the tag, its search bar showing it in the Tags box with the advanced section expanded (rule 2.15). ≠ items 7 and 17 | ❌ No — item 7, and item 17 for a reference it cannot read. The page supplies neither a link nor a hook, so a tag leads to `/Search?q=<tag>` and a reference to `/BibleReferences/{USFM}`, or `/Search?q=<reference>` for one it cannot read, both composed by the story; `chipOnClick` is unreachable while each story's default `chipHrefFor` wins (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 1`, `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 2`). No chip renders today (rule 2.14). |
| `onRemove` | Remove is pressed on the owner's own `Draft` or `Submitted` chip, which the panel offers with its moderation actions off (`UI/Components/AssociationPanel.md rule 2.18`) | Nothing: the page does not listen. No chip renders today. ≠ item 8 | ❌ No — item 8 |
| `onReject`, `onApprove` | Reject or Approve is pressed | Never offered: `showModerationActions` stays off, as on every surface but a moderation one (`UI/Components/AssociationPanel.md rule 2.15`). | *Not wired — switched off* |
| `loginButtonOnClick` | The login prompt is pressed | — | *Never raised*: the route admits no signed-out reader, and mounts the page only once the sign-in state is read (`securedRoutes.tsx` — `isLoading`), so the prompt does not render |

### 4.3 SharingPanel

**Properties the page sets:** none but `onSubmit`. Every string, the icon and the spacing keep
their defaults (`UI/Components/SharingPanel.md rule 2.2`).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSubmit` | *Submit a contribution* is pressed, by a reader the panel shows itself to (`UI/Components/SharingPanel.md rule 3.2.1`) | Navigates to `/posts/contribute`, carrying this page's path and query as `from` in router state (rule 2.17). | ✅ Yes (`myPostDetail.tsx`, lines 215-217) |

## 5. Security and access

Owner here is the item's contributor. The page asks no ownership question of its own (item 10).
Which card action each persona is offered is `UI/Components/ContentItemPanel.md §3.4` and
`UI/Components/ContentItemPanel.Edit.md §3.4`. The association panels' gates are
`UI/Components/AssociationPanel.md §3.4`. The read-only roles each action answers to are those
components' security and access matrices, `UI/Components/ContentItemPanel.md §5`,
`UI/Components/ContentItemPanel.Edit.md §5`, `UI/Components/AssociationPanel.TagAssociationPanel.md §5`,
`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §5` and `UI/Components/SharingPanel.md §5`.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page and the card ≠ item 10 | ❌ No¹ | ❌ No² | ✅ Yes | ❌ No² | ❌ No² | ❌ No² |
| No ReadOnly covering the item's type — **Edit**, the editor in place | ❌ No¹ | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Editor open, item `Draft` or `Submitted` — ***Delete*** ≠ item 2 | ❌ No¹ | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Editor open, item `Approved` or `Rejected` — ***Delete*** ≠ `UI/Components/ContentItemPanel.Edit.md §10 item 5` | ❌ No¹ | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| **View** on the card ≠ item 9 | ❌ No¹ | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| The viewer owns the item and holds the column's tier, no ReadOnly covering the item's type — **Moderate** ≠ item 11 | ❌ No¹ | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| The viewer owns the item, holding the column's tier where the column names one; the setting allows reactions — **Like** | ❌ No¹ | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The viewer owns the item, holding the column's tier where the column names one; no ReadOnly the panel composes, the post's own included — **the tag and Bible reference add boxes** ≠ item 13 | ❌ No¹ | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The viewer owns the item, holding the column's tier where the column names one; no `ReadOnly` or `ContentItem-ReadOnly` held — `SharingPanel`, **Submit a contribution** | ❌ No¹ | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `ReadOnly` or `ContentItem-ReadOnly` — `SharingPanel` ≠ `UI/Components/SharingPanel.md §10 item 5` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ The route guard shows *Access Restricted* and a *Login* button (rule 2.1).
² Shown today, although the item is not theirs, wherever the caller-scoped item read returns it: to a review role any non-deleted item, at any status, and to any other signed-in reader an `Approved` one (item 10).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — `/myposts/{id}`: the edit from `/myposts` lands on the read-only
   view.** The user ruled on 2026-09-27 that View and Edit are two actions: View opens the item's
   detail view read-only, and Edit MUST open the detail view straight in edit mode. Where each leads
   is the page's (§UI20.6.4; `UI/Components/ContentItemPanel.md rule 2.40`). `/myposts` routes the
   owner's Edit here with `edit: true` in router state (`myPosts.tsx` — `editContentItem`). This page
   reads only `from` from the state (`myPostDetail.tsx` — `backHref`), so the item opens read-only
   and the owner must press Edit again. The `/myposts` comment calls the detail's edit mode "its own
   work". The card half, where Edit is labelled *View* and there is no View action, is
   `UI/Components/ContentItemListPanel.md §10 item 9` and `UI/Components/ContentItemPanel.md §10 item 20`.
2. (needs issue) **Page gap — `/myposts/{id}`: *Delete* does nothing.** The editor offers the owner
   *Delete* and asks *Are you sure?*, and on confirmation raises `onRemoved`
   (`UI/Components/ContentItemPanel.Edit.md rule 2.11`). This page does not listen on `onRemoved`, so
   a confirmed Delete sends nothing: a dead action (§UI20.6.6 rule 4). Evidence: `myPostDetail.tsx`,
   the `ContentItemPanel` element, which passes no `onRemoved`; `contentItemFormPanel.tsx` — `mayDelete`
   asks no listener. The owner is never offered *Delete* on an `Approved` or `Rejected` item
   (rule 2.6; user ruling 2026-09-27), and the server refuses it (§APR9.9 rule 2). The design lets the owner withdraw while the item is `Draft` or `Submitted`
   (§APR9.9 rule 2; `UI/Components/ContentItemPanel.md rule 2.17`) through the soft delete
   `DELETE api/ContentItems/{contentItemId}` (`UI/Components/ContentItemPanel.Edit.md §7`). What the
   page shows once the item is gone is not settled. The moderation detail page, which does wire
   removal, returns to the list it came from (`admin/contentItemModerationDetailPage.tsx` —
   `removeContentItemAsync`). The editor also offers the owner Delete on a reviewed item, which is the
   component's gap, `UI/Components/ContentItemPanel.Edit.md §10 item 5`.
3. (needs issue) **Page gap — `/myposts/{id}`: the card renders before the settings read lands.**
   Rule 2.21 (user rulings 2026-09-27): the card is not rendered until its setting has loaded, and
   the page shows its spinner meanwhile; if the settings read fails, the page shows its error,
   announced, with a Retry, in the card's place. The page renders the card once the item read has landed,
   without waiting for its effective-settings read. While that read is in flight, or if it fails, the element carries no
   setting, although a setting always applies (`UI/Components/ContentItemPanel.md rule 2.39`).
   The page's error answers the item read alone (rule 2.13), so a failed settings read still shows
   the card. Evidence: `myPostDetail.tsx` — `contentItemSettings ?? []`. Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share of it.
4. (#745) **Page gap — `/myposts/{id}`: a chosen reaction is not persisted.** The page takes
   `onReactionSelected` from `useContentItemEngagement`, which toggles the choice in page state for
   the visit only. Recording and withdrawing the reader's own reaction (§ARC16.8.1, designed and not yet built) are this item's work. The sign-in half of the
   same page gap does not arise, because `SecuredRoute` admits no signed-out reader. Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share of it.
   **The card's counts are this item's work too** (the Likes feature, `DesignFeatures/Likes.md`).
   The page reads no reaction summary, so the card shows none of the reactions its item has been
   given, and the reader's own reaction is the visit's page state rather than the one they hold
   (`toContentItemSearchItem.ts` leaves `reactionSummary` unset, lines 71-83). The page hands
   `useContentItemEngagement` the id of its one card and renders what `withReactions` projects
   (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §1`; §ARC16.8).
5. (needs issue) **Page gap — `/myposts/{id}`: suggesting a tag or a Bible reference sends
   nothing.** The page answers each panel's `onAdd` with a "coming soon" toast and reads no
   associations, so both lists are always empty (`myPostDetail.tsx` — `suggestTag`,
   `suggestBibleReference`, `associationCollection={[]}`). The server work is partly built: of the association HTTP exposer (§ARC17.4), the suggestion
   `POST /api/associations` is served (#728) and the association read `GET /api/associations` is
   not (`UI/Components/AssociationPanel.md §7`). The page's half is to read the item's associations, project
   them to the panels, and send each suggestion with the viewer as its owner
   (`UI/Components/AssociationPanel.md §7`, the note on `asSuggestedAssociation`).
6. (needs issue) **Page gap — `/myposts/{id}`: the tag and Bible reference facet switches are not
   wired.** Each panel renders only where the item's effective `ShowTags` or `ShowBibleReferences` is
   on, and its add box shows only where `TagsAllowed` or `BibleReferenceAllowed` is on, each ANDed
   with the page's own switch (`UI/Components/AssociationPanel.TagAssociationPanel.md rule 2.8`,
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 2.11`). This page renders both
   panels unconditionally and leaves `showAdd` at its default, on, although it already resolves the
   item's effective setting for its heading (`myPostDetail.tsx` — `pageHeadingSetting`). Copied from
   `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 2` and
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 4`, this page's share of
   them.
7. (needs issue) **Page gap — `/myposts/{id}`: the page supplies no Bible reference link.** The page
   supplies every link and redirect, and the component supplies the hook (§UI20.6.4; user ruling
   2026-09-27), and in the user section a Bible reference leads to a page showing the verse (user
   ruling 2026-09-27): the existing verse route, `/BibleReferences/{USFM}`, which the list pages
   already lead to (`UI/Pages/MyPosts.md §4.4`). This page passes neither `chipHrefFor` nor
   `chipOnClick` to either panel (`myPostDetail.tsx`, the two panel elements), so a reference chip
   follows the reference story's own `/BibleReferences/{USFM}`, or `/Search?q=<reference>` for a
   reference it cannot read, and a tag chip the tag story's own
   `/Search?q=<tag>`, the coupling each story's document records
   (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`,
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`). The page's half is
   to supply the reference's link itself; the link for a reference it cannot read is item 17, held
   for #700. A chip cannot raise `chipOnClick` until the story's default
   goes (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 2`). The tag
   chip's link is the page's too: in the user section a tag opens `/posts` handed the tag, its
   search bar showing it in the Tags box with the advanced section expanded (rule 2.15; user
   rulings 2026-09-27). No chip renders until the item's associations are read (item 5).
8. (needs issue) **Page gap — `/myposts/{id}`: Remove on the owner's own pending chip is not
   wired.** With its moderation actions off, the panel still offers the owner Remove on their own
   `Draft` or `Submitted` chip (`UI/Components/AssociationPanel.md rules 2.18 and 3.3.3`). This page
   supplies no `onRemove`, so once associations are read (item 5) that Remove would be a dead action
   (§UI20.6.6 rule 4). The ruling on dead actions leaves an unwired button for the page to record
   (`UI/Components/AssociationPanel.md §10 item 8`). The removal is `DELETE /api/associations/{id}`
   (§ARC17.4, not yet built).
9. (needs issue) **Page gap — View is to be switched off here.** Rule 2.19: the reader is already
   on the item's detail view, so the page switches View off. The card has no View yet, and no
   switch for it (`UI/Components/ContentItemPanel.md §10 item 20`); once it has, View would render
   here unless the page sets its switch off. The page's half ships with that item: set View's
   switch off. The same gap on `/posts/{id}` is `UI/Pages/PostDetail.md §6 item 13`.
10. (needs issue) **Page gap — `/myposts/{id}` shows an item the reader does not own.** Rule 2.18
    (user ruling 2026-09-27): `/myposts` shows an owner their own content, and this page shows the
    reader their own items alone. The page renders whatever the caller-scoped item read returns for
    the id in the URL, and asks nothing about ownership (`myPostDetail.tsx` reads no identity;
    `useGetContentItemById`, line 47). So a signed-in reader who opens `/myposts/{id}` for somebody
    else's `Approved` item reads it here, under *Back to my posts*, and a review role reads any
    non-deleted item, at any status, the same way (`ContentItemService.cs` — the by-id read's owner
    and review-role branch, lines 327-341; §SEC14.7 posture A rule 4). Neither is offered Edit,
    because the card's Edit is the owner's (`UI/Components/ContentItemPanel.md rule 3.2.3`). The
    page's half is to answer such an item as not found, with the message rule 2.13 gives for any item
    it cannot read, so it never reveals that the item exists (rule 2.18; user ruling 2026-09-27).
11. (needs issue) **Page gap — `/myposts/{id}`: no Moderate on the owner's own detail page.** Rule
    2.20 (user ruling 2026-09-27): the page may offer Moderate to an owner who holds the moderation
    tier, as `/myposts` does (`UI/Pages/MyPosts.md rule 2.8`), because it is role-based
    functionality they hold. This page wires no `onModerateClick` (`myPostDetail.tsx`, the
    `ContentItemPanel` element), so the same moderator loses Moderate by opening the item. The
    page's half is to wire it to `/Admin/Posts/{id}`, carrying `from`.
12. (needs issue) **Page gap — `/myposts/{id}`: the card's type chip and *Author* do nothing.**
    Rule 2.15 (user rulings 2026-09-27): each click raises its hook, and the page opens `/posts`
    handed the value, its search bar showing it in the matching box with the advanced section
    expanded. The page wires neither `onContentTypeClick` nor `onAuthorClick` (`myPostDetail.tsx`
    — the `ContentItemPanel` element), yet the card renders both as buttons, so each is a dead
    action (§UI20.6.6 rule 4); *Submitted by* does not render here (section 4.1). The page's half
    is to wire the hooks — `onSubmittedByClick` with them — to open `/posts` handed the value; that
    the bar there opens with its advanced section expanded is
    `UI/Components/ContentItemListPanel.md §10 item 12`. The tag chips' half is item 7. The same
    gap on `/posts/{id}` is `UI/Pages/PostDetail.md §6 item 9`.
13. (needs issue) **Page gap — `/myposts/{id}`: the association panels are not handed the post's
    content type.** Rule 2.16 (user ruling 2026-09-27): the page hands both panels the item's
    content type, as data, so that each composes the post's `ContentItem-ReadOnly` and its
    `ContentItem-{ContentType}-ReadOnly` and withholds its suggest box from a holder, the chips
    staying visible (`UI/Components/AssociationPanel.md rule 2.30`). The page passes neither panel
    the type (`myPostDetail.tsx`, the two panel elements), although it holds the item and reads its
    type for its heading (`pageHeadingSetting`). The panel has no property to receive it yet
    (`UI/Components/AssociationPanel.md §10 item 20`). The same gap on `/posts/{id}` is
    `UI/Pages/PostDetail.md §6 item 11`.
14. (needs issue) **Page gap — `/myposts/{id}`: an amended reviewed item stays on the old version.**
    Rule 2.3: where the owner's save forks a new version — the item was `Approved` or `Rejected` —
    the card shows that new version, so the amendment appears (`UI/Components/ContentItemPanel.md
    rule 2.11`). The fork is written under a new id (`ContentItemProcessingService.cs` —
    `shouldForkNewVersion`, line 439, and the new version's `Id`, line 888), while the page reads
    no response from the write and does not navigate (`myPostDetail.tsx` — `saveChangesAsync`,
    lines 79-96): it re-reads the id in its URL, which is the reviewed version, unchanged. So the
    owner saves and sees their amendment vanish. The same gap on `/Admin/Posts/{id}` is
    `UI/Pages/ContentItemModerationDetailPage.md §6 item 15`.
15. (needs issue) **Page gap — `/myposts/{id}`: *Back to my posts* follows any origin.** Rule 2.12
    (user ruling 2026-09-27): the link always leads to the list it names, `/myposts`. It leads to
    whatever origin a redirect carried in router state, and to `/myposts` only when none was
    carried (`myPostDetail.tsx` — `backHref`, line 138; test: `myPostDetail.test.tsx` — "should
    walk back to the origin a redirect carried in state"). Today only `/myposts` sends a reader
    here. Once `/` and `/posts` route the owner's Edit to `/myposts/{id}` (`UI/Pages/Home.md rule
    2.10`, `UI/Pages/Posts.md rule 2.9`), each carrying its own path as `from`, the link would lead
    to `/` or `/posts`. The page's half is to follow an origin only when it is `/myposts`, and the
    test goes with it.
16. (#702) **Page gap — `/myposts/{id}`: a changed setting does not reach the open page.** Rule
    2.22 (user rulings 2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open
    page: the page reads its settings through
    `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does, so a setting another
    person changes reaches it only on the query library's own triggers or on a reload. The live
    connection is designed under #702, *Push Live Updates To Open Pages* (user ruling 2026-09-27);
    this page's share — hearing of a change to the setting that governs its item, and updating
    what it shows — is carved from that design. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
17. (#700) **Page gap — `/myposts/{id}`: an unreadable Bible reference in the side panel leads to
    the demo search page.** Held for #700, which decides how the Bible reference page is addressed
    for such a reference (`UI/Pages/BibleReference.md rule 2.18`). Rule 2.23 (user ruling
    2026-09-28): a reference in the side panel that cannot be read as a passage leads to the Bible
    reference page all the same, which says it could not be found. With no link supplied (item 7),
    its chip follows the reference story's own `/Search?q=<reference>`
    (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`). The same gap on
    the cards is `UI/Pages/Home.md §6 item 14`.
