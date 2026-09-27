# 1. Home

The public front page: the verse of the day, an invitation to contribute, and what has been
contributed and published, searched and scrolled. Everyone reads it, signed in or not; it is a
reading surface, and moderation is reached from it but not done on it.

- **Route:** `/` (`src/routes/publicPostRoutes.tsx` line 20 at 70dc72e7 — the index route)
- **Source:** `src/pages/home.tsx`
- **Section:** user — every hook leads to a user route, except Moderate, which leaves for the admin area (rule 2.11)
- **Access:** no guard — every reader, signed in or not
- **Layout:** single column
- **Components:** [ContentItemListPanel.md](../Components/ContentItemListPanel.md), with its
  children [ContentItemListPanel.ContentItemSearchBarPanel.md](../Components/ContentItemListPanel.ContentItemSearchBarPanel.md)
  and [ContentItemListPanel.ContentItemResultsPanel.md](../Components/ContentItemListPanel.ContentItemResultsPanel.md)
  and a [ContentItemPanel.md](../Components/ContentItemPanel.md) card per element;
  [SharingPanel.md](../Components/SharingPanel.md). Undocumented building block:
  `VerseOfTheDay` (`src/components/coreUI/verseOfTheDay.tsx`).

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `/` is public: its route has no guard, so every reader reaches it, signed in or not. *(code: publicPostRoutes.tsx — the index route)*

**2.2 [Must]** The page lists contributions through `ContentItemListPanel`. With nothing narrowed it reads the feed (§DOM11.3, §SEC14.2); once the reader narrows anything it reads the public set (§SEC14.1) with a filter. Which read answers is the page's decision. *(code: home.tsx — header comment, `resolveContentItemFeedScope`; test: contentItemFeedPages.test.tsx — "should feed the panel from the feed read when no criteria are supplied", "should feed the panel from the public read when the reader supplied %s")*

**2.3 [Must]** Both reads are caller-independent: a privileged visitor sees the rows an anonymous one sees, so no role change anywhere can put a draft on the front page. Unreviewed content is never shown on a public page. *(code: home.tsx — header comment; user, 2026-09-27)*

**2.4 [Should]** A tag or a Bible reference alone keeps the feed read, because no read narrows on either until associations are exposed over HTTP (§ARC17.4, not yet built). *(test: contentItemFeedPages.test.tsx — "should stay on the feed when the reader supplied only %s")*

**2.5 [Must]** The committed criteria live in the URL, so a shared link and the back button land with the results showing. The header's *Search* leads to `/posts`, not to this page (§UI20.7 rule 4). *(code: home.tsx — header comment, `search`; user, 2026-09-27)* ≠ item 12

**2.6 [Must]** Every element is handed to the list self-contained, carrying its winning setting, resolved from the effective settings read: the content type defaults plus the override rows of the items on screen (`UI/Components/ContentItemPanel.md rules 2.5 and 2.39`). *(code: home.tsx — `useGetEffectiveSettingsFor`, `toContentItemSearchItem`)* ≠ item 1

**2.7 [Must]** The page decides where every hook leads, and each redirect carries the page's own path and query as `from` in router state, so the destination can offer a true way back (`UI/Components/ContentItemListPanel.md rule 2.15`; §UI20.6.4). *(code: contentItemFeedNavigation.ts — `buildContentItemFeedNavigation`; code: home.tsx — `editContentItem`, `moderateContentItem`)*

**2.8 [Must]** A title — or the content of a quote or verse image, which carries none — and *read more....* lead to the item's read-only detail view, `/posts/{id}`. *(code: contentItemFeedNavigation.ts — `onTitleClick`, `onReadMore`)*

**2.9 [Must]** View leads to the item's detail view, read-only, on the template its type renders through: `/posts/{id}`. *(user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.40`)* ≠ item 4

**2.10 [Must]** The owner's Edit leads to the item's detail view straight in edit mode, on the contributor's own surface, `/myposts/{id}`: a listed card's Edit is never an in-place swap (`UI/Components/ContentItemListPanel.md rule 2.19`). *(user, 2026-09-26; user, 2026-09-27; `UI/Components/ContentItemPanel.md rules 2.38 and 2.40`)* ≠ item 4

**2.11 [Must]** Moderate leaves the public side for the item's admin address, `/Admin/Posts/{id}`, where the item's moderation tasks are performed, carrying `from` and `moderate: true`. *(user, 2026-09-26; code: home.tsx — `moderateContentItem`; test: contentItemFeedPages.test.tsx — "should send a moderator from the home feed to the admin address", "should send a moderator to the admin address on a row they did not contribute")* ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`

**2.12 [Must]** A Bible reference click leads to a page showing the passage, `/BibleReferences/{reference}`, addressed as that route parses it (`UI/Pages/BibleReference.md`). A reference that cannot be read as a passage is rule 2.25. *(user, 2026-09-27; code: contentItemFeedNavigation.ts — `onBibleReferenceClick`; code: toUsfmReference.ts — `bibleReferenceHref`)* ≠ `UI/Components/ContentItemListPanel.md §10 item 8`

**2.13 [Must]** A click on a card's tag, type chip, *Submitted by* or *Author* raises its hook, and the page opens the journal's search, `/posts`, handed the value: its search bar shows the value in the matching box — Tags, Category, Submitted by or Author, each one of the bar's advanced boxes — with the advanced section expanded (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). A tag narrows the list there once associations are exposed over HTTP (§ARC17.4, not yet built). *(user, 2026-09-27; `UI/Components/ContentItemListPanel.md rules 2.8–2.12 and 2.24`)* ≠ item 9

**2.14 [Must]** The comments control leads to the item's comments on its detail view, `/posts/{id}#comments`. *(code: contentItemFeedNavigation.ts — `onCommentsClick`)* ≠ item 6

**2.15 [Must]** Choosing a reaction is the page's to act on. A signed-out reader is sent to sign in through the one reusable sign-in action, carrying where they came from, and returned there afterwards — but not while their sign-in state is still being read. For a signed-in reader the page records, changes or clears their own reaction. *(§UI20.6.6 rule 2; user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 3.2.4`)* ≠ items 2 and 3

**2.16 [Must]** Share copies the item's address, `/posts/{id}`, to the clipboard and says *Link copied.* *(code: useContentItemEngagement.ts — `onShareClick`)*

**2.17 [Must]** Save is offered on every card, so it must have something behind it (§UI20.6.6 rule 4). A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and returned there afterwards — but not while their sign-in state is still being read (`UI/Components/ContentItemPanel.md §5`). *(§UI20.6.6 rules 2 and 4; code: home.tsx — `onSaveClick`)* ≠ item 5

**2.18 [Must]** The invitation to contribute, `SharingPanel`, stands full width above the list. Its button leads a signed-in reader to the contribution page, `/posts/contribute`, carrying `from`. A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and then on to the contribution form, `/posts/contribute`, the place their press was heading for, the origin surviving the sign-in step (`UI/Pages/Contribute.md rule 2.9`) — but not while their sign-in state is still being read. *(user, 2026-09-27; §UI20.6.6 rule 2; code: home.tsx — the `SharingPanel` element; `UI/Components/SharingPanel.md rule 2.9`)* ≠ item 10

**2.19 [Should]** The search bar offers no approval-status boxes: every row the page reaches is approved, so a status box would be a control whose every setting says the same thing. *(test: contentItemFeedPages.test.tsx — "should leave the approval statuses out of the search options")*

**2.20 [Should]** A failed read is said, in an alert standing in the list's place, never shown as an empty journal. *(code: home.tsx — the `isError` branch)*

**2.21 [Could]** The verse of the day stands above the feed and links to the Bible reference page, `/BibleReferences`. *(test: contentItemFeedPages.test.tsx — "should keep the verse of the day above the feed"; code: home.tsx — the `VerseOfTheDay` element)*

**2.22 [Won't]** No card on this page opens an editor in place: the list carries none of the writing faces' properties (`UI/Components/ContentItemListPanel.md rule 2.19`). *(code: home.tsx — the `ContentItemListPanel` element)*

**2.23 [Must]** A card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the cards back and shows the list's loading state, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the cards' place — never the cards without their settings. *(user, 2026-09-27)* ≠ item 1

**2.24 [Must]** A change to an item's setting reaches the open page without a reload: comments switched off for an item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is designed under #702, *Push Live Updates To Open Pages*; until it is designed and built, nothing pushes a change to the page. *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 11

**2.25 [Could]** A Bible reference that cannot be read as a passage leads to the Bible reference page all the same, which says it could not be found and offers the search (`UI/Pages/BibleReference.md rule 2.18`). *(user, 2026-09-28)* ≠ item 14

## 3. Layout

One column at every width, inside the persistent chrome `Root` renders around every page — the
header above and the footer below (`src/components/root.tsx`). There is no shell sidebar, so
nothing stacks on a narrow screen: the column is already one.

```text
+------------------------------------------------------------+
| header (Root)                                              |
+------------------------------------------------------------+
| VerseOfTheDay                                              |
+------------------------------------------------------------+
| SharingPanel (banner face)                                 |
| ContentItemListPanel                                       |
|   ContentItemSearchBarPanel                                |
|   ContentItemResultsPanel -> ContentItemPanel x n          |
+------------------------------------------------------------+
| footer (Root)                                              |
+------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Verse strip | the container, full width | `VerseOfTheDay` |
| Main | the container, `col-12` | `SharingPanel` — wide enough for its banner face (`UI/Components/SharingPanel.md rule 2.3`); then `ContentItemListPanel`, rendered bare, or the error alert in its place |

## 4. Components and their hooks

### 4.1 ContentItemListPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `ariaLabel` | `The journal` | Names the list; the page gives it no heading (`UI/Components/ContentItemListPanel.md rule 3.1.1`). |
| `contentItemCollection` | The accumulated rows of the infinite read, each projected with its winning setting, with the reaction this visitor chose this visit folded in | Rules 2.2 and 2.6; `useContentItemEngagement` — `withViewerReactions`. |
| `categorySettingCollection` | The effective settings read — defaults plus the overrides of the items on screen | The bar's Category box lists the defaults alone (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.5`). |
| `criteria` | Read off the URL | Rule 2.5. |
| `isLoading`, `isLoadingMore`, `hasMore` | The infinite read's state | Paging (section 4.3). |
| `reactionOptions` | The approved reactions, `GET api/Reactions` | The choices behind Like (`UI/Components/ContentItemListPanel.md §7`). |
| `emptyText` | `Nothing matched that search. Try clearing the advanced options.` | The page's own wording for an empty result. |
| Everything else | Left at the list's defaults: the bar shown, no approval-status boxes, no ribbon or status pill, `showModerationSection` off, every section switch on, titles as links, `editButtonText` *View* | Rule 2.19; the *View* default is `UI/Components/ContentItemListPanel.md §10 item 9`. |

**Hooks** — the ones the list raises itself

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | The bar commits, or a type chip or *Author* on a card is clicked. *Submitted by* and the tag and reference pills do not render on a listed card today: the projection leaves the submitter's name, the tags and the references unset (`toContentItemSearchItem.ts`, lines 62-80) | Writes the criteria into the URL; the read follows the URL (rules 2.2 and 2.5) | ✅ Yes (`home.tsx`, lines 95-96 and 147) |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` ≠ item 9 | A type chip, *Submitted by* or *Author* on a card is clicked — today after the list has rewritten the criteria and raised `onSearch` (`UI/Components/ContentItemListPanel.md §10 item 11`). *Submitted by* does not render on a listed card today (section 4.1, `onSearch`) | Opens `/posts` handed the value, its search bar showing it in the matching box with the advanced section expanded (rule 2.13) | ❌ No — the page wires none of the three; the list's rewrite narrows this page's own list instead, so the reader stays on `/`; item 9 |
| `onTagClick` ≠ item 9 | A tag pill on a card is clicked, after the list's rewrite. No pill renders today: the projection carries no tags until associations are exposed over HTTP (§ARC17.4, not yet built) | Opens `/posts` handed the tag, as the three above (rule 2.13) | ❌ No — the page wires none; the list toggles the tag criterion itself (`UI/Components/ContentItemListPanel.md §10 item 8`), and no read narrows on a tag until associations are exposed over HTTP (rule 2.4), so the list does not change; item 9 |
| `onBibleReferenceClick` ≠ item 14 | A reference pill on a card is clicked, after the list's rewrite. No pill renders today: the projection carries no references until associations are exposed over HTTP (§ARC17.4, not yet built) | Navigates to `/BibleReferences/{reference}`, or, for a reference it cannot read, to that page all the same, which says it could not be found, carrying `from` (rules 2.12 and 2.25) | For a readable reference ✅ Yes (`contentItemFeedNavigation.ts`, lines 45-46; `toUsfmReference.ts`, lines 67-73); for one it cannot read ❌ No — it goes to `/Search?q=<reference>` (`toUsfmReference.ts`, line 72); item 14. The list rewrites the criteria first (`UI/Components/ContentItemListPanel.md §10 item 8`) |

### 4.2 ContentItemSearchBarPanel, through the list

**Properties the page sets** — through the list: `criteria` and `categorySettingCollection`
(section 4.1). The approval-status boxes stay off (rule 2.19); every text is the bar's default.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | Search is pressed or Enter is hit | As section 4.1 | ✅ Yes (`home.tsx`, line 147) |

### 4.3 ContentItemResultsPanel, through the list

**Properties the page sets** — through the list: `isLoading`, `isLoadingMore`, `hasMore` and
`emptyText` (section 4.1). The loading texts are the panel's defaults.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onLoadMore` | The foot of the list comes into view, or *Load more* is pressed | Fetches the next page of the same read | ✅ Yes (`home.tsx`, line 151) |

### 4.4 The card — ContentItemPanel, inside the results

**Properties the page sets** — through the list: the element, `reactionOptions` and the
list's per-surface defaults (section 4.1). The card's face is the view template of the item's
type (`UI/Components/ContentItemPanel.md rule 2.3`).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onTitleClick` | The title — or a quote's or verse's content — is clicked | Navigates to `/posts/{id}`, carrying `from` (rule 2.8) | ✅ Yes (`contentItemFeedNavigation.ts`, line 35) |
| `onReadMore` | *read more....* is clicked on a cut card | As `onTitleClick` (rule 2.8) | ✅ Yes (`contentItemFeedNavigation.ts`, line 36) |
| `onExpandCollapse` | Only where `allowInPlaceExpansion` is on | — | *Not wired — switched off*: the page leaves in-place expansion off, so a cut card raises `onReadMore` (`UI/Components/ContentItemPanel.md rule 3.1.9`) |
| `onCommentsClick` ≠ item 6 | The comments control is clicked | Navigates to `/posts/{id}#comments`, carrying `from` (rule 2.14) | ❌ No — it navigates, but the detail view has no comments (item 6) |
| View's hook ≠ item 4 | View is clicked | Navigates to `/posts/{id}` (rule 2.9) | ❌ No — the card has no View yet (`UI/Components/ContentItemPanel.md §10 item 20`); item 4 |
| `onEditClick` ≠ item 4 | The owner's Edit is clicked — labelled *View* today | Navigates to `/myposts/{id}`, opened in edit mode (rule 2.10) | ❌ No — it navigates to `/posts/{id}` with `edit: true`, which that page never reads (`home.tsx`, lines 110-113); item 4 |
| `onModerateClick` ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1` | Moderate is clicked | Navigates to `/Admin/Posts/{id}`, carrying `from` and `moderate: true` (rule 2.11) | ✅ Yes (`home.tsx`, lines 115-118) |
| `onReactionSelected` ≠ items 2 and 3 | A reaction is chosen, whatever the sign-in state | Sends a signed-out reader to sign in; records, changes or clears a signed-in reader's own reaction (rule 2.15) | ❌ No — the choice is held in page state for the visit; the card redirects a signed-out reader itself (`UI/Components/ContentItemPanel.md §10 item 12`); item 2 |
| `onShareClick` | Share is clicked | Copies `/posts/{id}` and says so (rule 2.16) | ✅ Yes (`useContentItemEngagement.ts`, lines 44-49) |
| `onSaveClick` ≠ item 5 | Save is clicked | Nothing designed yet (rule 2.17) | ❌ No — it says *Saving posts is coming soon.* (`useContentItemEngagement.ts`, line 51); item 5 |
| `onTagClick`, `onBibleReferenceClick`, `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` | A pill or a meta segment is clicked | The list wraps them (section 4.1) | As section 4.1 |
| `onAdded`, `onModified`, `onRemoved`, `onCancelled` | Never on a listed card | — | *Not wired — switched off*: the list carries none of the writing faces' properties (rule 2.22) |

### 4.5 SharingPanel

**Properties the page sets** — none but `onSubmit`: the icon, the texts and the spacing are the
panel's defaults (`UI/Components/SharingPanel.md rule 2.2`).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSubmit` ≠ item 10 | The button is pressed, by any reader the panel shows itself to (`UI/Components/SharingPanel.md rule 3.2.1`) | Sends a signed-out reader to sign in, and then on to `/posts/contribute`; navigates a signed-in reader to `/posts/contribute`, carrying `from` (rule 2.18) | For a signed-in reader ✅ Yes (`home.tsx`, lines 131-134); for a signed-out reader ❌ No — they are sent to `/posts/contribute` unsigned, and sign in from its login link; item 10 |

### 4.6 VerseOfTheDay — building block

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `verse` | The sample verse in `src/pages/sampleContent.ts` | The strip's text. |
| `href` | `/BibleReferences` | The page supplies the link, so the block's `'#'` default does not apply (§UI20.6.4, its list of building blocks, item 17). |

**Hooks** — none. The verse is a link to the `href` the page supplies. ✅ Yes (`home.tsx`, line 122).

## 5. Security and access

Every reader reaches the page (rule 2.1), and every reader is shown the same rows (rule 2.3).
What differs by persona is the card's actions, as `ContentItemListPanel` and `ContentItemPanel`
decide them (`UI/Components/ContentItemListPanel.md §3.4`, `UI/Components/ContentItemPanel.md §5`).
The page asks no role of its own. Owner means the item's contributor.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — the verse strip, the search bar and the list | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The invitation, no `ReadOnly` or `ContentItem-ReadOnly` held ≠ item 10 | ✅ Yes³ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `ReadOnly` or `ContentItem-ReadOnly` — **the invitation** ≠ `UI/Components/SharingPanel.md §10 item 5` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| The rows listed — the feed or the public set, the same for every persona | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The title, *read more....*, the pills, the comments control and Share | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| View ≠ item 4 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The owner's Edit, no read-only role covering the item's type ≠ item 4 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Moderate, no read-only role covering the item's type ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1` | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes |
| Like, where the item's setting allows reactions ≠ items 2 and 3 | ✅ Yes² | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Save ≠ item 5 | ✅ Yes⁴ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Offered, and the page sends them to `/Admin/Posts/{id}`, which admits `Administrators` alone today (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`).
² Offered; choosing raises the hook, and the page is to send them to sign in (rule 2.15).
³ Offered; pressing it raises the hook, and the page is to send them to sign in and then on to the contribution form (rule 2.18).
⁴ Offered; pressing it raises the hook, and the page is to send them to sign in (rule 2.17).

**The read-only roles.** The page composes none. What a holder of a read-only role may do on a
card — every read, Like, Save and Share, and neither Edit nor Moderate — is
`UI/Components/ContentItemPanel.md §5`: sharing adds no content, so no read-only role withholds
it (user ruling 2026-09-27). The invitation composes its own: it hides itself from a signed-in
holder of `ReadOnly` or `ContentItem-ReadOnly`, and still invites a reader whose only read-only
roles are per content type (`UI/Components/SharingPanel.md §5`).

**The server decides.** Both reads are §SEC14.1's canonical set, the feed through §SEC14.2; every
write a card leads to is decided again by the service (§SEC14.6).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — the card renders before the settings read lands.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share. Rule 2.23 (user rulings
   2026-09-27): a card is not rendered until its setting has loaded, and the page shows the
   list's loading state meanwhile; if the settings read fails, the page shows its error, announced,
   with a Retry, in the cards' place. The page projects its
   elements through `toContentItemSearchItem` from its own effective-settings read
   (`contentItemSettingService.useGetEffectiveSettingsFor`), but renders the cards once the list
   read has landed, without waiting for that read. While it is in flight, or if it fails, the
   element carries no setting, although a setting always applies
   (`UI/Components/ContentItemPanel.md rule 2.39`), and the card shapes itself by the
   `UI/Components/ContentItemPanel.md rule 2.26` fallback. Evidence: `home.tsx` —
   `contentItemSettings ?? []` (lines 92 and 145). The page's `isError` branch answers the list
   read alone (rule 2.20), so a failed settings read still shows the cards. The same gap stands on
   `/posts` (`UI/Pages/Posts.md §6 item 1`) and `/posts/{id}`
   (`UI/Pages/PostDetail.md §6 item 1`). The component's half is
   `UI/Components/ContentItemPanel.md §10 item 5`.
2. (needs issue) **Page gap — the page does not act on a chosen reaction.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share. What follows
   `onReactionSelected` is the page's (`UI/Components/ContentItemPanel.md rule 3.2.4`): a
   signed-out reader is sent to sign in, with return information that brings them back
   afterwards, and a signed-in reader's reaction is recorded or cleared. The page takes
   `onReactionSelected` from the shared `useContentItemEngagement` hook, which only toggles the
   chosen reaction in page state for the visit and reads no sign-in state
   (`src/hooks/useContentItemEngagement.ts`, lines 34-42). No page sends a signed-out reader to
   sign in — the card does it itself (`UI/Components/ContentItemPanel.md §10 item 12`) — and no
   page records or clears a reaction: the reaction write surface is designed and not yet built
   (§ARC16.8.1). Recording and withdrawing the reader's own reaction are this item's work, and so
   is the redirect; it uses the one reusable sign-in action (item 3). The page that takes over the
   redirect must not send a reader whose sign-in state has not been read back:
   `isAuthenticated` reports false both for a reader with no session and for one whose session is
   still being read, and every full page load passes through the second with the cards already on
   screen. The card holds that guard today (`contentItemPanel.tsx` — the `onReactionSelected`
   handler, lines 463-468; test: `contentItemPanel.test.tsx` — "should not send a reader to sign
   in while the sign-in state is still unknown"). This item ships with
   `UI/Components/ContentItemPanel.md §10 item 12`. A signed-out reader meets the card on `/`,
   `/posts` and `/posts/{id}`; the other two are `UI/Pages/Posts.md §6 item 2` and
   `UI/Pages/PostDetail.md §6 item 2`.
3. (needs issue) **No reusable sign-in action.** §UI20.6.6 rule 2 makes sign-in a global action:
   one reusable way every page uses, so the sign-in route is defined once, returning the reader to
   exactly the place they left. It makes one exception: a reader who accepts the invitation to
   contribute is returned to the contribution form, `/posts/contribute`, where their press was
   heading, and the origin the invitation carried survives the sign-in step, so that the form's
   Cancel still returns them to where they pressed it (`UI/Pages/Contribute.md rule 2.9`; rule
   2.18). The action is asked for both. Router state does not survive sign-in: the sign-in page
   ends by navigating to its return address with no state (`src/pages/account/login.tsx`, line
   70), hands that address on as `ReturnUrl` when a second factor is asked (line 62), and external
   sign-in posts it to the server as `ReturnUrl` (`src/pages/account/externalLoginPicker.tsx`, line
   38). So the origin has to reach the form by a way that survives each of the three. No such
   action exists. The route `/Account/Login?returnUrl=…` is composed in
   five places, each from the path alone, so a reader on `/posts?q=grace` would return to `/posts`
   with the search gone: `src/components/securitys/securedRoutes.tsx` — `goToLogin` (line 28);
   `contentItemPanel.tsx` — the reaction redirect (line 472); `associationPanel.tsx` —
   `resolvedLoginHref` (line 219); `contentItemFormPanel.tsx` — `resolvedLoginHref` (line 396);
   and `src/pages/postSingle.tsx` — `loginHref` (line 44). The three component defaults are gaps
   of their own (`UI/Components/ContentItemPanel.md §10 item 12`,
   `UI/Components/AssociationPanel.md §10 item 15`, `UI/Components/ContentItemPanel.Add.md §10
   item 4`). This item is the action every page needs before it can take those over: this page for
   Like (item 2) and the invitation (item 10), and the pages that cite this item for theirs. The route guard's own loss of the
   query is `UI/Pages/MyPosts.md §6 item 7`.
4. (needs issue) **View and Edit lead to the wrong places.** Rules 2.9 and 2.10 (user rulings
   2026-09-26 and 2026-09-27) send View to the read-only detail view, `/posts/{id}`, and the
   owner's Edit to the detail view in edit mode, `/myposts/{id}`. The card has no View
   (`UI/Components/ContentItemPanel.md §10 item 20`), and the list relabels the owner's Edit as
   *View* (`UI/Components/ContentItemListPanel.md §10 item 9`). The page's half: it routes that
   Edit to `/posts/{id}` with `edit: true` in router state (`home.tsx` — `editContentItem`, lines
   110-113), and `/posts/{id}` reads no such state and offers no editing
   (`UI/Pages/PostDetail.md rule 2.9`), so the owner lands on the read-only view. Once the card
   offers both, the page wires View to `/posts/{id}` and Edit to `/myposts/{id}`; that
   `/myposts/{id}` opens straight in edit mode is that page's to build. The same gap stands on
   `/posts` (`UI/Pages/Posts.md §6 item 3`).
5. (needs issue) **Save has nothing behind it.** Every card offers Save (rule 2.17), and the
   page's handler only says *Saving posts is coming soon.* (`useContentItemEngagement.ts`, line
   51), to a signed-out reader too: nobody is sent to sign in (rule 2.17). Save has no design yet:
   `UI/Components/ContentItemPanel.md §5` records it, and the user's ruling of 2026-09-27 exempts a
   reader's own Save from the lock after review and from the read-only veto once it is designed
   (§APR9.9 rule 6). §UI20.6.6 rule 4 plans it end to end, from the page down to the API, in the
   user's order: Likes first, then Save. The same handler serves `/posts` and
   `/posts/{id}` (`UI/Pages/Posts.md §6 item 4`, `UI/Pages/PostDetail.md §6 item 3`).
6. (needs issue) **The comments control leads to a detail view with no comments.** The page
   sends the comments control to `/posts/{id}#comments` (rule 2.14;
   `contentItemFeedNavigation.ts`, lines 40-41). `/posts/{id}` renders no comments and no element
   with the id `comments` (`src/pages/postDetail.tsx`), so the reader lands at the top of the item
   with no comments to read. Comments are the last feature in the user's order under §UI20.6.6
   rule 4. The same gap stands on `/posts` (`UI/Pages/Posts.md §6 item 5`).
7. **Note — where a tag click leads in the user section, ruled.** This item asked which page is
   the search a tag click leads to on `/`, `/posts`, `/posts/{id}`, `/Post-Single`, `/myposts`
   and `/myposts/{id}`: the list page's own filter, which is what `ContentItemListPanel` does in
   place today; `/posts` filtered by the tag; `/Search`, the ported Blogzine search page, a demo
   that answers every query with the same sample posts (`src/pages/search.tsx`, header comment),
   which `TagAssociationPanel`'s default link reaches as `/Search?q=<tag>`; or another page. The
   user ruled on 2026-09-27 that the search page is `/posts`: in the user section a click on a
   card's tag, content type, *Submitted by* or *Author* raises its hook, and the page opens
   `/posts` handed the value, its search bar showing it in the matching box with the advanced
   section expanded where the box is an advanced one; on `/posts` itself the page applies the
   value to its own list the same way. Tags narrow the list once associations are exposed over HTTP (§ARC17.4, not yet built). Rule 2.13 says so
   here; the pages' halves are item 9 and the items it names. In the admin section the same
   clicks open the queue, `/Admin/Posts`, handed the value, as the user ruled the same day
   (`UI/Pages/ContentItemModerationPage.md rule 2.13`).
8. **Note — a signed-out reader who accepts the invitation to contribute, ruled.** This item
   asked whether `/` and `/posts/{id}`, whose `SharingPanel` sends every reader, signed in or
   not, to `/posts/contribute`, and `/posts`, whose own link does the same, should send a
   signed-out reader straight to sign in, or leave it to the contribution page's own *Login to
   contribute* link, which returns them to the contribution page. The user ruled on 2026-09-27:
   a signed-out reader sees `SharingPanel` as it is; choosing it raises its hook, and the page
   sends them to sign in and back to the same place they were. The user refined it the same day for
   the invitation: the page sends them to sign in and then on to the contribution form,
   `/posts/contribute`, which saves them a step. A signed-in holder of `ReadOnly`
   or `ContentItem-ReadOnly` is not shown the panel, which composes those roles itself; a reader
   whose only read-only roles are per content type is shown it, and the page sends them to
   `/posts/contribute`, whose tiles omit the types they are blocked from, or whose restricted face
   shows where none is left (`UI/Components/SharingPanel.md rules 2.9–2.11`). Rule 2.18 says so
   here; the pages' halves are item 10 and the items it names.
9. (needs issue) **Page gap — a card's tag, type chip, *Submitted by* and *Author* clicks do not
   open `/posts`.** Rule 2.13 (user rulings 2026-09-27): each click raises its hook, and the page
   opens `/posts` handed the value, its search bar showing it in the matching box with the
   advanced section expanded. The page wires none of `onTagClick`, `onContentTypeClick`,
   `onSubmittedByClick` and `onAuthorClick` (`home.tsx` — the `ContentItemListPanel` element,
   lines 142-161). The list turns the type chip and *Author* clicks into a search of this page's
   own list instead — it rewrites the criteria and raises `onSearch`, which the page writes into its
   own URL (`UI/Components/ContentItemListPanel.md §10 items 8 and 11`) — so the reader stays on
   `/`, the value in this page's search bar and the advanced section folded. *Submitted by* and the
   tag pills do not render on a listed card today: the projection leaves the submitter's name and
   the tags unset (`toContentItemSearchItem.ts`, lines 62-80), so those two hooks wait on the name
   and on associations being exposed over HTTP (§ARC17.4, not yet built). The page's half is to wire the
   four hooks to open `/posts` handed the value; `/posts` reads its criteria off its URL
   (`UI/Pages/Posts.md rule 2.3`), and opens its advanced section whenever the address carries one
   of its criteria (`UI/Pages/Posts.md rule 2.22`), which needs the list and the bar to accept it
   (`UI/Components/ContentItemListPanel.md §10 item 12`,
   `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 7`). The same gap
   stands on `/posts` (`UI/Pages/Posts.md §6 item 7`), `/posts/{id}` (`UI/Pages/PostDetail.md §6
   item 9`), `/myposts` (`UI/Pages/MyPosts.md §6 item 8`) and `/myposts/{id}`
   (`UI/Pages/MyPostDetail.md §6 item 12`); the side panels' tag chips are
   `UI/Pages/PostDetail.md §6 item 5` and `UI/Pages/MyPostDetail.md §6 item 7`.
10. (needs issue) **Page gap — the invitation sends a signed-out reader to the contribution
    page unsigned.** Rule 2.18 (user rulings 2026-09-27; §UI20.6.6 rule 2): a signed-out reader
    who presses `SharingPanel`'s button is sent straight to sign in through the one reusable sign-in
    action, and then on to the contribution form, `/posts/contribute`, the origin surviving the
    sign-in step (`UI/Pages/Contribute.md rule 2.9`); a signed-in reader goes to
    `/posts/contribute`. The page sends every reader to `/posts/contribute` and reads no sign-in
    state (`home.tsx`, lines 131-134), so a signed-out reader lands on the contribution page
    unsigned and has to press its *Login to contribute* link, which signs them in and returns them
    there (`UI/Pages/Contribute.md rule 2.10`): one step more than the ruled route.
    The redirect uses the one reusable sign-in action (item 3), and must not fire while the
    reader's sign-in state is still being read. That the panel hides itself from a holder of
    `ReadOnly` or `ContentItem-ReadOnly` is the component's half,
    `UI/Components/SharingPanel.md §10 item 5`. The same gap stands on `/posts/{id}`
    (`UI/Pages/PostDetail.md §6 item 10`), and on `/posts` for its own contribution link
    (`UI/Pages/Posts.md §6 item 8`).
11. (#702) **Page gap — a changed setting does not reach the open page.** Rule 2.24 (user rulings
    2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open page. The page
    reads its settings through `contentItemSettingService.useGetEffectiveSettingsFor`, a read that
    goes stale after 60 seconds (`staleTime`) and is read again only on the query library's own
    triggers — the tab regaining focus, the connection returning, the page remounting — or when a
    settings write in the same browser invalidates it (`invalidateContentItemSettingReads`). So a
    setting another person changes reaches the page only then, or on a reload. The live connection
    that tells an open page of a change is designed under #702, *Push Live Updates To Open Pages*
    (user ruling 2026-09-27). The page's share — hearing of a change to a setting that governs an
    item it shows, and updating what it shows — is carved from that design. The card's half is
    `UI/Components/ContentItemPanel.md rule 2.44`. The same share stands on `/posts`
    (`UI/Pages/Posts.md §6 item 11`), `/posts/{id}` (`UI/Pages/PostDetail.md §6 item 14`),
    `/myposts` (`UI/Pages/MyPosts.md §6 item 10`), `/myposts/{id}`
    (`UI/Pages/MyPostDetail.md §6 item 16`), `/Admin/Posts`
    (`UI/Pages/ContentItemModerationPage.md §6 item 10`) and
    `/Admin/Posts/{id}` (`UI/Pages/ContentItemModerationDetailPage.md §6 item 16`).
12. (needs issue) **The layout's links lead to the sample pages.** The header, the footer and the
    off-canvas menu are layout: they exist to route, and are outside *Hooks, not routes*
    (§UI20.6.4), but where their links lead is ruled: §UI20.7 rule 4 (user ruling 2026-09-27)
    sends the header's *Search*, the footer's *Journal* and *Authors*, and the off-canvas menu's
    *Our Journal* to the journal's search, `/posts`, and the Bible reader's tags to `/posts` with
    the tag. The pages they lead to today are sample material, moving under `/SamplePages`
    (§UI20.5.1, its items 1 and 2). The header's *Search*, on every page, is a bare link to
    `/Search` (`src/components/layouts/header.tsx`, line 240). The footer, on every page, links
    *Journal* to `/Categories` and *Authors* to `/Author` (`src/components/layouts/footer.tsx`,
    lines 53-54). The off-canvas menu, which `Root` renders on every page
    (`src/components/root.tsx`, line 27), links *Our Journal* to `/Categories`
    (`src/components/layouts/offcanvasMenu.tsx`, line 33). The Bible reader,
    `/BibleReferences/BibleReader`, a product page with no document of its own yet (§UI20.5.1),
    links each of its passage's tags to `/Search?q=<tag>` (`src/pages/bibleReader.tsx`, line 106).
    The work is to point each of these links where the rule sends it. The footer's topic links are
    item 15's. The product's other links to the sample
    pages are gaps of their own: an unreadable Bible reference on a card is item 14, which names the
    other pages', and the tag panel's default link, which sends a tag to `/Search?q=<tag>`, is
    `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`. `TagPillList` sends a tag to
    `/Search?q=<tag>` too (§UI20.6.4, its list of building blocks, item 16), but it is not a product
    surface: only `ArticleCard` renders it (`src/components/coreUI/articleCard.tsx`), and only a
    sample page renders that (`src/pages/samplePages/home/homeDefaultSample.tsx`).
13. (needs issue) **Four code comments say the header's search lands on the results.** The
    header comment of `home.tsx` (line 51) and of `posts.tsx` (line 38), and the one at the head of
    `src/services/views/contentItems/contentItemSearchCriteriaUrl.ts` (line 19, "the header's
    search, a shared link and the back button all land with the results already showing, exactly
    as /Search does with ?q="), say that the header's search lands on the results of this page, or of
    `/posts`. The header has no search form: its *Search* is a bare link to `/Search`, which
    replaced the magnifier's form (`src/components/layouts/header.tsx`, lines 235-242). The fourth,
    at the head of `src/pages/search.tsx` (lines 17-19, "The query lives in the URL (?q=) so the
    header's /Search link and deep links land with the results already showing"), is wrong about
    that link too: the demo page shows its results only once the address carries `q`
    (`hasSearched`, line 87), and the header's link carries none. Rule 2.5 and
    `UI/Pages/Posts.md rule 2.3` say what the URL carries, and §UI20.7 rule 4 where the header's
    *Search* leads. The four comments are to say what the code does.
14. (needs issue) **Page gap — an unreadable Bible reference leads to the demo search page.**
    Rule 2.25 (user ruling 2026-09-28): a reference that cannot be read as a passage leads to the
    Bible reference page all the same, which says it could not be found. The page's
    `onBibleReferenceClick` navigates to what `bibleReferenceHref` builds
    (`contentItemFeedNavigation.ts`, lines 45-46), which is `/Search?q=<reference>` for a reference
    it cannot read (`toUsfmReference.ts`, line 72): the demo search page, sample material
    (§UI20.5.1). No pill renders on a listed card today (section 4.1), so a reader meets it only
    once associations are exposed over HTTP (§ARC17.4, not yet built). The page's half is to send
    an unreadable reference to the Bible reference page. The same helper is the
    reference panel's default link, the component's half
    (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`). The same gap
    stands on `/posts` (`UI/Pages/Posts.md §6 item 13`) and `/myposts`
    (`UI/Pages/MyPosts.md §6 item 12`), whose navigation is built by the same
    `buildContentItemFeedNavigation`. The admin queue, `/Admin/Posts`, builds its navigation with
    that function too (`src/pages/admin/contentItemModerationPage.tsx`, line 97), but in the admin
    section a reference leads to the queue itself, not to the Bible reference page
    (`UI/Pages/ContentItemModerationPage.md rule 2.13`, its gap
    `UI/Pages/ContentItemModerationPage.md §6 item 6`), so a change made in the shared function
    reaches both destinations. The side panels' reference chips are
    `UI/Pages/PostDetail.md §6 item 5`, `UI/Pages/MyPostDetail.md §6 item 7` and
    `UI/Pages/BibleReference.md §6 item 4`.
15. (needs issue) **The footer's topic links lead to a sample page.** The footer, on every page,
    has a *Hot topics* block of eight links — *Faith*, *Hope*, *Prayer*, *Scripture*, *Testimony*,
    *Worship*, *Community* and *Missions* — each to `/Categories`, sample material moving under
    `/SamplePages` (`src/components/layouts/footer.tsx`, lines 95-102; §UI20.5.1, its item 2).
    §UI20.7 rule 4 (user rulings 2026-09-27) sends each to `/posts` with its word as a tag, in the
    search bar's Tags box with the advanced section expanded (`UI/Pages/Posts.md rule 2.22`). A tag
    narrows nothing until associations are exposed over HTTP (§ARC17.4, not yet built;
    `UI/Pages/Posts.md rule 2.12`), so until then a topic link shows the whole journal, its tag in
    the Tags box. The links are to move before `/Categories` does, or with it: once §UI20.5.1 item 2
    has moved `/Categories` under `/SamplePages`, a link still pointing there reaches the Not Found
    page (`src/routes/staticRoutes.tsx`, line 31).
