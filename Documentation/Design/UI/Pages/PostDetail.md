# 1. PostDetail

One content item, read: the item's permanent address, and where a card's title, *read more....*
and View lead. The item stands on the left; its tags, its Bible references and the invitation to
contribute stand beside it on the right. Everyone reads it; it is a reading surface, and nothing on
it edits or moderates the item.

- **Route:** `/posts/{contentItemId}` (`src/routes/publicPostRoutes.tsx` line 61 at 70dc72e7)
- **Source:** `src/pages/postDetail.tsx`
- **Section:** user — every hook leads to a user route
- **Access:** no guard — every reader, signed in or not; the item shows only when it is approved (rule 2.1)
- **Layout:** two columns — main with right sidebar
- **Components:** [ContentItemPanel.md](../Components/ContentItemPanel.md) on its view face — the
  template of the item's type: [ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md)
  or [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md);
  [AssociationPanel.TagAssociationPanel.md](../Components/AssociationPanel.TagAssociationPanel.md)
  and [AssociationPanel.BibleReferenceAssociationPanel.md](../Components/AssociationPanel.BibleReferenceAssociationPanel.md),
  each rendering [AssociationPanel.md](../Components/AssociationPanel.md);
  [SharingPanel.md](../Components/SharingPanel.md). Core-UI primitive: `Spinner`
  (`src/components/coreUI/spinner.tsx`), while the item loads.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `/posts/{id}` is public: its route has no guard, and the page reads the item the route names. It shows the item only when it is approved — §SEC14.1's canonical set — to every reader, the item's owner and the moderators included. Unreviewed content is never shown on a public page: an owner reads their own unreviewed item on `/myposts/{id}`, and a moderator on `/Admin/Posts/{id}`. *(code: publicPostRoutes.tsx — the `posts/:contentItemId` route; test: postDetail.test.tsx — "should read the item named by the route"; user, 2026-09-27)* ≠ item 12

**2.2 [Must]** The item renders through `ContentItemPanel`'s view face, whole — no cut, no *read more*. *(code: postDetail.tsx — `showContentExpanded`; test: postDetail.test.tsx — "should render the item through the view face, full and unclamped")*

**2.3 [Must]** The card is handed the same self-contained element the feeds carry, with its winning setting resolved from the content type defaults plus this item's own override (`UI/Components/ContentItemPanel.md rules 2.5 and 2.39`), and with the contributor's name and picture from the contributor read. The card does not wait on the byline. *(code: postDetail.tsx — `useGetEffectiveSettingsFor`, `readItem`, `useGetContributorById`; test: postDetail.test.tsx — "should name the contributor the item records, not the reader looking at it")* ≠ item 1

**2.4 [Must]** The page's heading — out of sight, since the card carries the visible title — and the browser tab's title are resolved from the same winning setting as the card: the item's title where the setting has a title and the item carries one, otherwise the type's name, so a title the card hides is never shouted above it (`UI/Components/ContentItemPanel.md §6.4`). *(test: postDetail.test.tsx — "should head the document once, out of sight, and let the card carry the title", "should fall back to the content type name for a type that carries no title", "should not shout a title the panel deliberately hides", "should prefer an item override when naming the page", "should name the type rather than a literal before the settings arrive")*

**2.5 [Must]** The item's tags and Bible references stand beside the card, in the two association panels on the right, and the card's own tag and reference sections are switched off, so one fact is said once (`UI/Components/ContentItemPanel.md §6.3`). *(code: postDetail.tsx — `showTagSection={false}`, `showBibleReferenceSection={false}`; test: postDetail.test.tsx — "should stand the association surfaces in the five", "should say the same association fact once, beside the card and not within it")*

**2.6 [Must]** The two panels are handed the item's associations, read for this item from the association read (§ARC17.4, keyed per §DOM4.6 rule 1). *(code: postDetail.tsx — the comment above the two panels)* ≠ item 4

**2.7 [Must]** A signed-in reader may suggest a tag or a Bible reference, and the page writes the suggestion (§ARC17.4). *(code: postDetail.tsx — `suggestTag`, `suggestBibleReference`; test: postDetail.test.tsx — "should offer a signed-in reader the box itself, not only its heading")* ≠ item 4

**2.8 [Must]** The two panels' moderation actions stay off: a reader may suggest, and withdraw their own suggestion while it is `Draft` or `Submitted` (§APR9.9), and nothing on the page decides anything (`UI/Components/AssociationPanel.md rule 2.15`). *(code: postDetail.tsx — the comment above the two panels)* ≠ item 4

**2.9 [Must]** Editing is off. The page leaves `showEditSection` off and wires neither `onEditClick` nor `onModerateClick`, so no reader — the item's owner and an administrator included — is offered Edit, Delete or Moderate here (`UI/Components/ContentItemPanel.md rules 2.13 and 2.14`). It reads no `edit` or `moderate` flag a list page hands it in router state. *(code: postDetail.tsx — the `ContentItemPanel` element; test: postDetail.test.tsx — "should offer no editing to the reader who contributed it", "should offer no editing to an administrator either", "should offer no moderation control however the reader is trusted")*

**2.10 [Must]** The card carries the engagement row the feeds carry — Like, Share and Save. A signed-out reader who chooses a reaction is sent to sign in through the one reusable sign-in action, carrying where they came from, and returned there afterwards — but not while their sign-in state is still being read; for a signed-in reader the page records, changes or clears their own reaction. *(§UI20.6.6 rule 2; user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 3.2.4`; test: postDetail.test.tsx — "should carry the engagement row the feeds carry")* ≠ item 2

**2.11 [Must]** Share copies this post's own address to the clipboard and says *Link copied.* *(code: useContentItemEngagement.ts — `onShareClick`; test: postDetail.test.tsx — "should copy THIS post’s address when the reader shares it")*

**2.12 [Must]** Save must have something behind it (§UI20.6.6 rule 4). A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and returned there afterwards — but not while their sign-in state is still being read (`UI/Components/ContentItemPanel.md §5`). *(§UI20.6.6 rules 2 and 4; test: postDetail.test.tsx — "should answer Save honestly, so the control is not merely present")* ≠ item 3

**2.13 [Must]** The page supplies where a tag and a Bible reference in the side panels lead — a tag to the journal's search, `/posts`, handed the tag, its search bar showing it in the Tags box with the advanced section expanded, and a reference to a page showing the passage — and the sign-in action their login prompts raise (§UI20.6.4; §UI20.6.6 rule 2). A reference that cannot be read as a passage is rule 2.23. *(user, 2026-09-27)* ≠ item 5

**2.14 [Must]** The invitation to contribute, `SharingPanel`, stands beneath the two association panels. Its button leads a signed-in reader to the contribution page, `/posts/contribute`, carrying this page's address as `from`. A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and then on to the contribution form, `/posts/contribute`, the place their press was heading for, the origin surviving the sign-in step (`UI/Pages/Contribute.md rule 2.9`) — but not while their sign-in state is still being read. *(user, 2026-09-27; §UI20.6.6 rule 2; code: postDetail.tsx — the `SharingPanel` element; test: postDetail.test.tsx — "should invite the reader to share something of their own from the five", "should carry the reader to the contribution surface and back here"; `UI/Components/SharingPanel.md rule 2.9`)* ≠ item 10

**2.15 [Should]** While the item loads, a spinner stands in the column an error would stand in. *(test: postDetail.test.tsx — "should stand the spinner in the same column the refusal stands in")*

**2.16 [Should]** An item that cannot be read is said — it may have been removed, or may not be the reader's to read — with a way back to the journal, `/`. *(test: postDetail.test.tsx — "should say so rather than render an empty page when the item cannot be read")*

**2.17 [Should]** The page claims no engagement figure it has no source for: no reaction, comment or view count. *(test: postDetail.test.tsx — "should claim no engagement figures it has no source for")*

**2.18 [Must]** A click on the card's type chip, *Submitted by* or *Author* raises its hook, and the page opens the journal's search, `/posts`, handed the value: its search bar shows it in the matching box — Category, Submitted by or Author, each one of the bar's advanced boxes — with the advanced section expanded (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). *(user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.42`)* ≠ item 9

**2.19 [Must]** The page hands both association panels the item's content type, so that each composes the post's own read-only roles — `ContentItem-ReadOnly`, and `ContentItem-{ContentType}-ReadOnly` for the item's type — and withholds its suggest box from a holder; the chips stay visible to them (`UI/Components/AssociationPanel.md rule 2.30`). *(user, 2026-09-27)* ≠ item 11

**2.20 [Must]** View is switched off: the reader is already on the item's detail view, where View leads (`UI/Pages/Home.md rule 2.9`), so the card does not offer it here. *(user, 2026-09-27: View and Edit are each "configurable via the Show* properties"; `UI/Components/ContentItemPanel.md rule 2.40`)* ≠ item 13

**2.21 [Must]** The card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the card back, the spinner of rule 2.15 standing in its place, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the card's place — never the card without its setting. *(user, 2026-09-27)* ≠ item 1

**2.22 [Must]** A change to the item's setting reaches the open page without a reload: comments switched off for the item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is designed under #702, *Push Live Updates To Open Pages*; until it is designed and built, nothing pushes a change to the page. *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 14

**2.23 [Could]** A Bible reference in the side panel that cannot be read as a passage leads to the Bible reference page all the same, which says it could not be found and offers the search (`UI/Pages/BibleReference.md rule 2.18`). *(user, 2026-09-28)* ≠ item 15

## 3. Layout

Two columns at the `lg` breakpoint and wider — the item on the left in seven twelfths, its side
surfaces on the right in five — inside `Root`'s header and footer (`src/components/root.tsx`).
There is no shell sidebar. Narrower, the columns stack: the item first, then the side surfaces
beneath it. While the item loads, or when it cannot be read, a single centred column
(`col-xl-9`) holds the spinner or the error instead.

```text
+------------------------------------------------------------+
| header (Root)                                              |
+-----------------------------------+------------------------+
| main (col-lg-7)                   | sidebar (col-lg-5)     |
|   h1 (visually hidden)            |   TagAssociationPanel  |
|   ContentItemPanel (view face)    |   BibleReference...    |
|                                   |   SharingPanel         |
+-----------------------------------+------------------------+
| footer (Root)                                              |
+------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Main | `col-lg-7` | The page's `h1`, visually hidden (rule 2.4); `ContentItemPanel` |
| Right sidebar | `col-lg-5` | `TagAssociationPanel`; `BibleReferenceAssociationPanel`; `SharingPanel` — narrow enough for its stacked face (`UI/Components/SharingPanel.md rule 2.3`) |
| Loading or error | `col-xl-9`, centred | `Spinner`, or the error alert and its link back to `/` |

## 4. Components and their hooks

### 4.1 ContentItemPanel — the view face

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `contentItem` | The item, projected with its winning setting and the contributor's name and picture, with the reaction this visitor chose this visit folded in | Rule 2.3. |
| `showContentExpanded` | `true` | Rule 2.2. |
| `showTagSection`, `showBibleReferenceSection` | `false` | Rule 2.5. |
| `reactionOptions` | The approved reactions, `GET api/Reactions` | Rule 2.10. |
| Everything else | Left at the panel's defaults: `showEditSection` off, `allowTitleClick` off, no ribbon or status pill, the other four section switches on | Rules 2.9 and 2.17; `UI/Components/ContentItemPanel.md rule 3.1.10`. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onReactionSelected` ≠ item 2 | A reaction is chosen, whatever the sign-in state | Sends a signed-out reader to sign in; records, changes or clears a signed-in reader's own reaction (rule 2.10) | ❌ No — the choice is held in page state for the visit; the card redirects a signed-out reader itself (`UI/Components/ContentItemPanel.md §10 item 12`); item 2 |
| `onShareClick` | Share is clicked | Copies this post's address and says so (rule 2.11) | ✅ Yes (`postDetail.tsx`, line 182; `useContentItemEngagement.ts`, lines 44-49) |
| `onSaveClick` ≠ item 3 | Save is clicked | Nothing designed yet (rule 2.12) | ❌ No — it says *Saving posts is coming soon.*; item 3 |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` ≠ item 9 | The type chip, *Submitted by* or *Author* is clicked | Opens `/posts` handed the value, its search bar showing it in the matching box with the advanced section expanded (rule 2.18) | ❌ No — the page wires none, yet the three render as buttons, so pressing one does nothing (`contentItemDefaultPanel.tsx`, lines 194-198, 305-308 and 323-326); item 9 |
| `onTagClick`, `onBibleReferenceClick` | A tag or reference pill on the card is clicked | — | *Not wired — switched off*: the card's two sections are off, because the side panels carry them (rule 2.5) |
| `onTitleClick` | Only where `allowTitleClick` is on | — | *Not wired — switched off*: the panel on its own is the detail surface, so its title is plain heading text (`UI/Components/ContentItemPanel.md rule 3.1.10`) |
| `onReadMore`, `onExpandCollapse` | Only on a cut card | — | *Not wired — switched off*: the content stands whole (rule 2.2) |
| `onCommentsClick` | Only where the page listens | — | *Not wired — switched off*: without it the comments control does not render (`UI/Components/ContentItemPanel.Default.md rule 2.9`), and the page has no comments to show (`UI/Pages/Home.md §6 item 6`) |
| View's hook ≠ item 13 | View is clicked | — | *Not wired — switched off*: the reader is on the item's detail view already (rule 2.20). The card has no View yet (`UI/Components/ContentItemPanel.md §10 item 20`), and the page is to set View's switch off once it has one; item 13 |
| `onEditClick`, `onModerateClick` | Edit or Moderate is clicked | — | *Not wired — switched off*: editing is off on this page (rule 2.9) |
| `onAdded`, `onModified`, `onRemoved`, `onCancelled` | Only on a writing face | — | *Not wired — switched off*: `showEditSection` is off (rule 2.9) |

### 4.2 TagAssociationPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `associationCollection` | `[]` | The association read is not yet served over HTTP (§ARC17.4, not yet built); the page hands an honest empty set rather than an invented one. Item 4. |
| `onAdd` | `suggestTag` | Rule 2.7. |
| `showBorder`, `cssClass` | `true`, `mb-4` | Frames the panel in the sidebar. |
| The post's content type (`UI/Components/AssociationPanel.md rule 2.30`) | Not passed; the panel has no such property yet (`UI/Components/AssociationPanel.md §10 item 20`) | Rule 2.19; item 11. |
| Everything else | Left at the story's defaults: the add box on, the moderation actions off, the story's texts, its moderation tier, its own chip link and the panel's own sign-in link | Rule 2.8; the chip link and the sign-in link are item 5. That the panel and its box do not follow the item's `ShowTags` and `TagsAllowed` is item 8. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` ≠ item 4 | A signed-in reader commits a tag in the box, once per tag | Writes the suggestion, `POST /api/associations` (§ARC17.4), and refreshes the panel's collection (rule 2.7) | ❌ No — it says *Suggesting tags is coming soon.* (`postDetail.tsx`, line 135); item 4 |
| `onRemove` ≠ item 4 | The reader withdraws their own `Draft` or `Submitted` tag | Removes it, `DELETE /api/associations/{id}` (§ARC17.4), and refreshes (rule 2.8) | ❌ No — not wired; no chip renders while the collection is empty; item 4 |
| `chipOnClick` or `chipHrefFor` ≠ item 5 | A tag is clicked | Opens `/posts` handed the tag, its search bar showing it in the Tags box with the advanced section expanded (rule 2.13) | ❌ No — the page supplies neither; the story's own default links the tag to `/Search?q=<tag>` (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`); item 5 |
| `loginButtonOnClick` or `loginHref` ≠ item 5 | A signed-out reader presses *Login to suggest a tag* | Sends them to sign in through the one reusable sign-in action (rule 2.13) | ❌ No — the page supplies neither; the panel composes its own sign-in route (`UI/Components/AssociationPanel.md §10 item 15`); item 5 |
| `onApprove`, `onReject` | Only with `showModerationActions` on | — | *Not wired — switched off*: a reading surface decides nothing (rule 2.8) |

### 4.3 BibleReferenceAssociationPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `associationCollection` | `[]` | As section 4.2. Item 4. |
| `onAdd` | `suggestBibleReference` | Rule 2.7. |
| `showBorder`, `cssClass` | `true`, `mb-4` | As section 4.2. |
| The post's content type | Not passed, as section 4.2 | Rule 2.19; item 11. |
| Everything else | Left at the story's defaults, as section 4.2 | That the panel and its box do not follow the item's `ShowBibleReferences` and `BibleReferenceAllowed` is item 8. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` ≠ item 4 | A signed-in reader commits a reference in the box, once per reference | Writes the suggestion, `POST /api/associations` (§ARC17.4), and refreshes (rule 2.7) | ❌ No — it says *Suggesting bible references is coming soon.* (`postDetail.tsx`, lines 137-138); item 4 |
| `onRemove` ≠ item 4 | The reader withdraws their own `Draft` or `Submitted` reference | Removes it, `DELETE /api/associations/{id}` (§ARC17.4), and refreshes (rule 2.8) | ❌ No — not wired; item 4 |
| `chipOnClick` or `chipHrefFor` ≠ items 5 and 15 | A reference is clicked | Leads to a page showing the passage, `/BibleReferences/{reference}` (rule 2.13), or, for a reference it cannot read, to that page all the same, which says it could not be found (rule 2.23) | ❌ No — the page supplies neither; the story's own default builds `/BibleReferences/<USFM>`, or `/Search?q=<reference>` for a reference it cannot read (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`); item 5, and item 15 for a reference it cannot read |
| `loginButtonOnClick` or `loginHref` ≠ item 5 | A signed-out reader presses *Login to suggest a bible reference* | As section 4.2 | ❌ No — as section 4.2; item 5 |
| `onApprove`, `onReject` | Only with `showModerationActions` on | — | *Not wired — switched off*, as section 4.2 |

### 4.4 SharingPanel

**Properties the page sets** — none but `onSubmit`: the icon, the texts and the spacing are the
panel's defaults (`UI/Components/SharingPanel.md rule 2.2`).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSubmit` ≠ item 10 | The button is pressed, by any reader the panel shows itself to (`UI/Components/SharingPanel.md rule 3.2.1`) | Sends a signed-out reader to sign in, and then on to `/posts/contribute`; navigates a signed-in reader to `/posts/contribute`, carrying `from` (rule 2.14) | For a signed-in reader ✅ Yes (`postDetail.tsx`, lines 211-213); for a signed-out reader ❌ No — they are sent to `/posts/contribute` unsigned, and sign in from its login link; item 10 |

## 5. Security and access

Every reader reaches the page; whether the item answers is the service's decision (rule 2.1). The
card offers no editing or moderation to anyone (rule 2.9). What differs by persona is Like's
sign-in, the association panels' boxes and chips, and which item answers. Owner means the item's
contributor in the rows about the item, and a suggestion's contributor in the rows about a chip
(`UI/Components/AssociationPanel.md §3.4`).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The item, when it is public | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The item, when it is not public — a draft, a submission or a refusal ≠ item 12 | ❌ No | ❌ No | ❌ No³ | ❌ No³ | ❌ No³ | ❌ No³ |
| Edit, Delete or Moderate on the card | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| View on the card ≠ item 13 | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Like, where the item's setting allows reactions ≠ item 2 | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Share | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Save ≠ item 3 | ✅ Yes⁴ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The type chip, *Submitted by* and *Author*, as buttons ≠ item 9 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The suggest boxes, where the item's setting allows suggestions, no read-only role the panel composes, the post's own included ≠ items 4, 8 and 11 | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The panels' login prompts ≠ item 5 | ✅ Yes | ❌ No⁵ | ❌ No⁵ | ❌ No⁵ | ❌ No⁵ | ❌ No⁵ |
| Withdrawing one's own `Draft` or `Submitted` suggestion ≠ item 4 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Approve, Reject, or Remove of another reader's suggestion | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| The invitation to contribute, no `ReadOnly` or `ContentItem-ReadOnly` held ≠ item 10 | ✅ Yes² | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `ReadOnly` or `ContentItem-ReadOnly` — **the invitation** ≠ `UI/Components/SharingPanel.md §10 item 5` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ Offered; choosing raises the hook, and the page is to send them to sign in (rule 2.10).
² Offered; pressing it raises the hook, and the page is to send them to sign in and then on to the contribution form (rule 2.14).
³ Shown today: the item read answers the owner and the review roles with a row that is not public (§SEC14.7 posture A rule 4; item 12).
⁴ Offered; pressing it raises the hook, and the page is to send them to sign in (rule 2.12).
⁵ Shown to a signed-in reader, too, while their sign-in state is still being read (item 5).

**The read-only roles.** The page composes none. On the card, what a holder of a read-only role
may do is `UI/Components/ContentItemPanel.md §5`. In the side panels, `ReadOnly` or `Tag-ReadOnly`
withholds the tag box and the reader's own withdrawal, and `ReadOnly` or `BibleReference-ReadOnly`
the reference box and withdrawal (`UI/Components/AssociationPanel.TagAssociationPanel.md §5`,
`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §5`); the panels do not yet
render that veto (`UI/Components/AssociationPanel.md §10 item 1`). The post's own
`ContentItem-ReadOnly`, and `ContentItem-{ContentType}-ReadOnly` for its type, withhold both too,
once the page hands the panels the post's type (rule 2.19; item 11). A holder of any of them still
sees the chips. The invitation hides itself from a signed-in holder of `ReadOnly` or
`ContentItem-ReadOnly`, and still invites a reader whose only read-only roles are per content type
(`UI/Components/SharingPanel.md §5`).

**The server decides.** The item shown is §SEC14.1's canonical set, the same for every caller;
the item read the page uses today answers under §SEC14.7 posture A rule 4 and §SEC14.5 instead
(item 12). A suggestion and its withdrawal are decided under §SEC14.7 posture A′; every gate on the
page is a courtesy (§SEC14.6).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — the card renders before the settings read lands.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share. Rule 2.21 (user rulings
   2026-09-27): the card is not rendered until its setting has loaded, and the page shows its
   spinner meanwhile; if the settings read fails, the page shows its error, announced, with a
   Retry, in the card's place. The page projects its
   element through `toContentItemSearchItem` from its own effective-settings read
   (`contentItemSettingService.useGetEffectiveSettingsFor`), but renders the card once the item
   read has landed, without waiting for that read. While it is in flight, or if it fails, the
   element carries no setting, although a setting always applies
   (`UI/Components/ContentItemPanel.md rule 2.39`), and the card shapes itself by the
   `UI/Components/ContentItemPanel.md rule 2.26` fallback. Evidence: `postDetail.tsx` —
   `contentItemSettings ?? []` (line 86). The page's error answers the item read alone (rule
   2.16), so a failed settings read still shows the card. The page's heading already names the
   type while the settings are on their way (rule 2.4). The same gap on `/` is `UI/Pages/Home.md §6 item 1`.
2. (#743) **Page gap — the page does not act on a chosen reaction.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share. A signed-out reader who
   chooses a reaction is to be sent to sign in, with return information that brings them back
   afterwards, and a signed-in reader's reaction recorded or cleared
   (`UI/Components/ContentItemPanel.md rule 3.2.4`). The page takes `onReactionSelected` from
   `useContentItemEngagement`, which only toggles the choice in page state for the visit and reads
   no sign-in state (`src/hooks/useContentItemEngagement.ts`, lines 34-42; test:
   `postDetail.test.tsx` — "should mark the reaction the reader chose for this visit", "should
   withdraw the reaction when the reader chooses it again"); the card redirects a signed-out
   reader itself (`UI/Components/ContentItemPanel.md §10 item 12`, which this item ships with).
   Recording the reader's own reaction (§ARC16.8.1, served by #728) and withdrawing it (designed and not yet built) are this item's work, and so is the redirect, which uses the one
   reusable sign-in action (`UI/Pages/Home.md §6 item 3`), and must not fire while the reader's
   sign-in state is still being read. The same gap on `/` is `UI/Pages/Home.md §6 item 2`.
   **The card's counts are this item's work too** (the Likes feature, `DesignFeatures/Likes.md`).
   The page reads no reaction summary, so the card shows none of the reactions its item has been
   given, and the reader's own reaction is the visit's page state rather than the one they hold
   (`toContentItemSearchItem.ts` leaves `reactionSummary` unset, lines 71-83). The page hands
   `useContentItemEngagement` the id of its one card and renders what `withReactions` projects
   (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §1`; §ARC16.8).
3. (needs issue) **Save has nothing behind it.** The card offers Save (rule 2.12), and the
   handler only says *Saving posts is coming soon.* (`useContentItemEngagement.ts`, line 51), to a
   signed-out reader too. Save has no design yet. The same gap, and its plan under §UI20.6.6 rule 4, is
   `UI/Pages/Home.md §6 item 5`: one handler serves every page that renders the card.
4. (needs issue) **The association panels are inert.** Rules 2.6–2.8 have the page read the
   item's tags and Bible references, write a reader's suggestion and remove their own withdrawn
   one. The page hands both panels an empty collection, answers every suggestion with a *coming
   soon* notice, and wires no `onRemove` (`postDetail.tsx`, lines 135-138 and 195-205; test:
   `postDetail.test.tsx` — "should answer a suggested tag honestly rather than dropping it",
   "should answer a suggested bible reference honestly rather than dropping it"). The read and the
   two writes are designed (§ARC17.4) and not yet served: `AssociationsController` serves a reader's reaction alone (#728), and an editorial suggestion waits on #871; the page's
   wiring is its own work on top of it. With the collection empty, no chip renders, so the missing
   `onRemove` would surface as a dead withdrawal only once the read is wired
   (`UI/Components/AssociationPanel.md §10 item 8`).
5. (needs issue) **The page supplies no chip destination and no sign-in action to the two
   panels.** Rule 2.13: where a tag or a reference leads, and where a login prompt sends a
   signed-out reader, are the page's (§UI20.6.4; §UI20.6.6 rule 2). The page passes neither
   `chipHrefFor` nor `chipOnClick`, and neither `loginHref` nor `loginButtonOnClick`, to either
   panel (`postDetail.tsx`, lines 195-205), so each renders the route its component composes
   itself: `/Search?q=<tag>` for a tag; for a reference, `/BibleReferences/<USFM>`, or
   `/Search?q=<reference>` for one it cannot read; and `/Account/Login?returnUrl=<path>` for the
   prompt. The prompts read `isAuthenticated` alone
   (`associationPanel.tsx`, line 213), which is false while a signed-in reader's sign-in state is
   still being read, so such a reader arriving on a full page load is shown *Login to suggest a
   tag* until it is (`UI/Components/AssociationPanel.md §10 item 15`). The components' halves are
   `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`,
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5` and
   `UI/Components/AssociationPanel.md §10 item 15`; a tag chip cannot raise `chipOnClick` until the
   story's default goes (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 1`). The
   sign-in half needs the one reusable sign-in action (`UI/Pages/Home.md §6 item 3`); the tag's
   destination is `/posts`, handed the tag (rule 2.13; user rulings 2026-09-27). The link for a
   reference it cannot read is item 15, held for #700.
6. **Note — the type chip, *Submitted by* and *Author* on a detail view in the user section,
   ruled.** This item asked where each of the three leads from `/posts/{id}` and `/myposts/{id}`,
   where the card renders them as buttons that raise their hooks
   (`UI/Components/ContentItemPanel.Default.md rules 3.1.1 and 3.1.4`) and neither page wires them,
   so a reader pressing one got nothing — for example to `/posts` narrowed to that type, submitter
   or author — or whether the card should offer them as plain text where the page wires no hook.
   The user ruled on 2026-09-27 that each raises its hook and the page decides; that in the user
   section the page opens the search page, `/posts`, handed the value, its search bar showing it in
   the matching box with the advanced section expanded; and that the type chip works the same way
   as *Submitted by* and *Author*. The list no longer turns these clicks into its own search
   (`UI/Components/ContentItemListPanel.md rules 2.8–2.11`). The user confirmed it for `/posts/{id}`
   and `/myposts/{id}` the same day: each raises its hook, and the page sends the reader to
   `/posts` with the matching advanced fields filled in. Rule 2.18 says so here; the page's
   half is item 9, and `/myposts/{id}`'s is `UI/Pages/MyPostDetail.md §6 item 12`.
7. (needs issue) **Stale comment — `postDetail.tsx`.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 10`. The page's comment (lines 34-38) says leaving
   `showEditSection` off guarantees "no Edit, no route into the editor, however the reader's roles
   fall". `UI/Components/ContentItemPanel.md rule 2.13` puts a page-routed Edit (`onEditClick`) and
   Moderate outside that switch; the page shows neither because it wires neither (rule 2.9).
8. (needs issue) **Page gap — the tag and Bible reference facet switches are not wired.** Copied
   from `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 2` and
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 4`, this page's share
   of them. Each panel renders only where the item's effective `ShowTags` or `ShowBibleReferences`
   is on, and its add box shows only where `TagsAllowed` or `BibleReferenceAllowed` is on, each
   ANDed with the page's own switch (`UI/Components/AssociationPanel.TagAssociationPanel.md rule 2.8`,
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 2.11`). The page renders
   both panels unconditionally and leaves `showAdd` at its default, on (`postDetail.tsx`, lines
   195-205), although it already resolves the item's winning setting for its heading
   (`postDetail.tsx` — `pageHeadingSetting`, lines 106-111; rule 2.4). The same gap on
   `/myposts/{id}` is `UI/Pages/MyPostDetail.md §6 item 6`.
9. (needs issue) **Page gap — the card's type chip, *Submitted by* and *Author* do nothing.** Rule
   2.18 (user rulings 2026-09-27): each click raises its hook, and the page opens `/posts` handed
   the value, its search bar showing it in the matching box with the advanced section expanded.
   The page wires none of `onContentTypeClick`, `onSubmittedByClick` and `onAuthorClick`
   (`postDetail.tsx` — the `ContentItemPanel` element, lines 175-183), yet the card renders the
   three as buttons (`contentItemDefaultPanel.tsx`, lines 194-198, 305-308 and 323-326), so each
   is a dead action (§UI20.6.6 rule 4). The page's half is to wire the three to open `/posts`
   handed the value; that the bar there opens with its advanced section expanded is
   `UI/Components/ContentItemListPanel.md §10 item 12`. The same gap on `/` is
   `UI/Pages/Home.md §6 item 9`, which names the others.
10. (needs issue) **Page gap — the invitation sends a signed-out reader to the contribution
    page unsigned.** Rule 2.14 (user rulings 2026-09-27; §UI20.6.6 rule 2): a signed-out reader
    who presses `SharingPanel`'s button is sent straight to sign in through the one reusable
    sign-in action, and then on to the contribution form, `/posts/contribute`, the origin surviving
    the sign-in step (`UI/Pages/Contribute.md rule 2.9`); a signed-in reader
    goes to `/posts/contribute`. The page sends every reader to `/posts/contribute` and reads no
    sign-in state for it (`postDetail.tsx`, lines 211-213), so a signed-out reader lands on the
    contribution page unsigned and has to press its *Login to contribute* link, which signs them in
    and returns them there (`UI/Pages/Contribute.md rule 2.10`): one step more than the ruled
    route. The redirect uses the one reusable sign-in action
    (`UI/Pages/Home.md §6 item 3`), and must not fire while the reader's sign-in state is still
    being read. The panel's own half is `UI/Components/SharingPanel.md §10 item 5`. The same gap
    on `/` is `UI/Pages/Home.md §6 item 10`.
11. (needs issue) **Page gap — the association panels are not handed the post's content type.**
    Rule 2.19 (user ruling 2026-09-27): the page hands both panels the item's content type, as
    data, so that each composes the post's `ContentItem-ReadOnly` and its
    `ContentItem-{ContentType}-ReadOnly` and withholds its suggest box from a holder, the chips
    staying visible (`UI/Components/AssociationPanel.md rule 2.30`). The page passes neither panel
    the type (`postDetail.tsx`, lines 195-205), although it holds the item and its type. The panel
    has no property to receive it yet, and composes no role for the post's end
    (`UI/Components/AssociationPanel.md §10 item 20`); the page's half is to pass the type once it
    can. The same gap stands on `/myposts/{id}` (`UI/Pages/MyPostDetail.md §6 item 13`) and
    `/Admin/Posts/{id}` (`UI/Pages/ContentItemModerationDetailPage.md §6 item 10`).
12. (needs issue) **Page gap — `/posts/{id}` shows an item that is not public.** Rule 2.1 (user
    ruling 2026-09-27): a public page shows approved content only, to every reader, the owner and
    the moderators included; an owner reads their own unreviewed item on `/myposts/{id}`, and a
    moderator on `/Admin/Posts/{id}`. The page reads the item with the caller-scoped
    `GET api/ContentItems/{id}` (`postDetail.tsx` — `useGetContentItemById`), which answers a row
    that is not public — a draft, a submission or a refusal — to its owner and to the review roles,
    and not-found to everyone else (§SEC14.7 posture A rule 4, §SEC14.5), so an owner or a
    moderator reads such an item here as if it were published. The read that answers an approved
    item alone, whoever asks, is designed and not yet built: `GET api/ContentItems/Public/{contentItemId}`,
    which answers not-found for every other id (§SEC14.2 rule 3; §ARC17.1). The
    page's half is to read it, and to show a row it refuses as the item that cannot be read (rule
    2.16). The same gap on `/posts` is `UI/Pages/Posts.md §6 item 6`.
13. (needs issue) **Page gap — View is to be switched off here.** Rule 2.20: the reader is already
    on the item's detail view, so the page switches View off. The card has no View yet, and no
    switch for it (`UI/Components/ContentItemPanel.md §10 item 20`); once it has, View would
    render here unless the page sets its switch off, and a View that leads to the page the reader
    is on is no way anywhere. The page's half ships with that item: set View's switch off. The
    same gap stands on `/myposts/{id}` (`UI/Pages/MyPostDetail.md §6 item 9`) and
    `/Admin/Posts/{id}` (`UI/Pages/ContentItemModerationDetailPage.md §6 item 14`).
14. (#702) **Page gap — a changed setting does not reach the open page.** Rule 2.22 (user rulings
    2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open page: the page
    reads its settings through `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does,
    so a setting another person changes reaches it only on the query library's own triggers or on a
    reload. The live connection is designed under #702, *Push Live Updates To Open Pages* (user
    ruling 2026-09-27); this page's share — hearing of a change to the setting that governs its
    item, and updating what it shows — is carved from that design. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
15. (#700) **Page gap — `/posts/{id}`: an unreadable Bible reference in the side panel leads to the
    demo search page.** Held for #700, which decides how the Bible reference page is addressed for
    such a reference (`UI/Pages/BibleReference.md rule 2.18`). Rule 2.23 (user ruling 2026-09-28): a
    reference in the side panel that cannot be read as a passage leads to the Bible reference page
    all the same, which says it could not be found. With no link supplied (item 5), its chip follows
    the reference story's own `/Search?q=<reference>`
    (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`). The same gap on
    the cards is `UI/Pages/Home.md §6 item 14`.
