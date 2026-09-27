# 1. Posts

The journal: every approved contribution, searched and scrolled. It is the collection
`/posts/{id}` and `/posts/contribute` are members of. Everyone reads it, and everyone is shown the
same approved rows.

- **Route:** `/posts` (`src/routes/publicPostRoutes.tsx` line 37 at 70dc72e7)
- **Source:** `src/pages/posts.tsx`
- **Section:** user — every hook leads to a user route, except Moderate, which leaves for the admin area (rule 2.10)
- **Access:** no guard — every reader, signed in or not
- **Layout:** single column
- **Components:** [ContentItemListPanel.md](../Components/ContentItemListPanel.md), with its
  children [ContentItemListPanel.ContentItemSearchBarPanel.md](../Components/ContentItemListPanel.ContentItemSearchBarPanel.md)
  and [ContentItemListPanel.ContentItemResultsPanel.md](../Components/ContentItemListPanel.ContentItemResultsPanel.md)
  and a [ContentItemPanel.md](../Components/ContentItemPanel.md) card per element. No
  undocumented building block: the heading and the contribution link are the page's own markup.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `/posts` is public: its route has no guard, so every reader reaches it, signed in or not. *(code: publicPostRoutes.tsx — the `posts` route)*

**2.2 [Must]** The page lists contributions through `ContentItemListPanel`, and only approved ones — the canonical public set (§SEC14.1) — to every reader, anonymous or signed in, the item's owner and the moderators included. Unreviewed content is never shown here: an owner sees their own on `/myposts`, and the moderation tier in the admin area, `/Admin/Posts`. *(user, 2026-09-27)* ≠ item 6

**2.3 [Must]** The committed criteria live in the URL, so a shared link and the back button land with the results showing. The header's *Search*, on every page, leads here (§UI20.7 rule 4). *(code: posts.tsx — header comment, `search`; test: posts.test.tsx — "should read the criteria off the url so a shared link lands on the results", "should put what was searched for back into the url", "should read a clicked submitted-by filter back off the url"; user, 2026-09-27)* ≠ `UI/Pages/Home.md §6 item 12`

**2.4 [Should]** A content type the URL does not name is ignored rather than searched for. *(test: posts.test.tsx — "should ignore a content type the url does not actually name")*

**2.5 [Must]** Every element is handed to the list self-contained, carrying its winning setting, resolved from the effective settings read: the content type defaults plus the override rows of the items on screen (`UI/Components/ContentItemPanel.md rules 2.5 and 2.39`). *(code: posts.tsx — `useGetEffectiveSettingsFor`, `toContentItemSearchItem`; test: posts.test.tsx — "should project each row onto the card the panel renders")* ≠ item 1

**2.6 [Should]** The list holds every page read so far, and asking for the next page goes straight to the read. *(test: posts.test.tsx — "should accumulate the pages rather than showing only the last", "should hand the next page request straight to the query")*

**2.7 [Must]** The page decides where every hook leads, and each redirect carries the page's own path and query as `from` in router state (`UI/Components/ContentItemListPanel.md rule 2.15`; §UI20.6.4). A title — or a quote's or verse's content — and *read more....* lead to the item's read-only detail view, `/posts/{id}`. *(code: contentItemFeedNavigation.ts — `buildContentItemFeedNavigation`)*

**2.8 [Must]** View leads to the item's detail view, read-only: `/posts/{id}`. *(user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.40`)* ≠ item 3

**2.9 [Must]** The owner's Edit leads to the item's detail view straight in edit mode, on the contributor's own surface, `/myposts/{id}`. *(user, 2026-09-26; user, 2026-09-27; `UI/Components/ContentItemPanel.md rules 2.38 and 2.40`)* ≠ item 3

**2.10 [Must]** Moderate leaves the public side for the item's admin address, `/Admin/Posts/{id}`, carrying `from` and `moderate: true`. *(user, 2026-09-26; code: posts.tsx — `moderateContentItem`; test: contentItemFeedPages.test.tsx — "should send a moderator from the public list to the admin address")* ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`

**2.11 [Must]** A Bible reference click leads to a page showing the passage, `/BibleReferences/{reference}` (`UI/Pages/BibleReference.md`). A reference that cannot be read as a passage is rule 2.26. *(user, 2026-09-27; code: contentItemFeedNavigation.ts — `onBibleReferenceClick`; code: toUsfmReference.ts — `bibleReferenceHref`)* ≠ `UI/Components/ContentItemListPanel.md §10 item 8`

**2.12 [Must]** This page is the journal's search. A click on a card's tag, type chip, *Submitted by* or *Author* raises its hook, and the page applies the value to its own list: it puts the value in its committed criteria, in its URL (rule 2.3), and its search bar shows it in the matching box — Tags, Category, Submitted by or Author, each one of the bar's advanced boxes — with the advanced section expanded (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). A tag narrows the list once associations are exposed over HTTP (§ARC17.4, not yet built). *(user, 2026-09-27; `UI/Components/ContentItemListPanel.md rules 2.8–2.12 and 2.24`)* ≠ item 7

**2.13 [Must]** The page's own link beside its heading, *Share what He has done*, leads a signed-in reader to the contribution page, `/posts/contribute`, carrying this page's own address as `from`, so the contribution page's Cancel can return them here (`UI/Pages/Contribute.md rule 2.9`). A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and then on to the contribution form, `/posts/contribute`, the place their press was heading for, the origin surviving the sign-in step — but not while their sign-in state is still being read. *(code: posts.tsx — the `Link` beside the heading; test: posts.test.tsx — "should render the journal and a way into the contribution form"; user, 2026-09-27; §UI20.6.6 rule 2)* ≠ items 8 and 12

**2.14 [Must]** The comments control leads to the item's comments on its detail view, `/posts/{id}#comments`, and reads uncounted where no count is known. *(code: contentItemFeedNavigation.ts — `onCommentsClick`; test: posts.test.tsx — "should offer the comments control uncounted rather than with an invented figure")* ≠ item 5

**2.15 [Must]** Like is offered from the approved reaction vocabulary. A signed-out reader who chooses is sent to sign in through the one reusable sign-in action, carrying where they came from, and returned there afterwards — but not while their sign-in state is still being read; for a signed-in reader the page records, changes or clears their own reaction. *(§UI20.6.6 rule 2; user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 3.2.4`; test: posts.test.tsx — "should offer the Like control fed by the approved vocabulary")* ≠ item 2

**2.16 [Must]** Share copies the item's address, `/posts/{id}`, to the clipboard and says *Link copied.* *(code: useContentItemEngagement.ts — `onShareClick`)*

**2.17 [Must]** Save is offered on every card, so it must have something behind it (§UI20.6.6 rule 4). A signed-out reader who presses it is sent to sign in through the one reusable sign-in action, and returned there afterwards — but not while their sign-in state is still being read (`UI/Components/ContentItemPanel.md §5`). *(§UI20.6.6 rules 2 and 4; code: posts.tsx — `onSaveClick`)* ≠ item 4

**2.18 [Should]** A failed read is said, in an alert standing in the list's place, never shown as an empty journal. *(test: posts.test.tsx — "should say so rather than showing an empty journal when the read fails")*

**2.19 [Should]** The page shows no approval status: every row it lists is approved (rule 2.2), so the search bar offers no status boxes, and no card wears the ribbon or the status pill. *(code: posts.tsx — the `ContentItemListPanel` element sets none of `showApprovalStatusSearchOptions`, `showApprovalStatusRibbon`, `showApprovalStatus`; user, 2026-09-27)*

**2.20 [Won't]** No card on this page opens an editor in place: the list carries none of the writing faces' properties (`UI/Components/ContentItemListPanel.md rule 2.19`). *(code: posts.tsx — the `ContentItemListPanel` element)*

**2.21 [Must]** The search lives in the query string, so the address is always a bookmark or a shareable link of what the reader sees: as the reader changes the search form, the query string follows without a reload, not only when the search is committed (rule 2.3). *(user, 2026-09-27)* ≠ item 10

**2.22 [Must]** The page opens its search bar's advanced section whenever the query string carries a criterion that lives there — every criterion but the query itself: Category, Author, Submitted by, Shareability, Tags and Bible references — whether the reader typed it, a shared link carried it, or another page handed it over (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). A value a click or a link hands over always lands in its own box there — a tag in Tags, a Bible reference in Bible references, a type in Category, a *Submitted by* or an *Author* in its own — and never in the free-text query, which is the reader's own, for narrowing further within the advanced criteria already set. *(user, 2026-09-27)* ≠ item 10

**2.23 [Must]** The page's own contribution link, *Share what He has done*, behaves as `SharingPanel` does (`UI/Components/SharingPanel.md rules 2.9–2.11`): it is hidden from a signed-in holder of `ReadOnly` or `ContentItem-ReadOnly`, still shown to a reader whose only read-only roles are per content type, and a signed-out reader who presses it goes to sign in and then on to the contribution form (rule 2.13). *(user, 2026-09-27)* ≠ item 9

**2.24 [Must]** A card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the cards back and shows the list's loading state, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the cards' place — never the cards without their settings. *(user, 2026-09-27)* ≠ item 1

**2.25 [Must]** A change to an item's setting reaches the open page without a reload: comments switched off for an item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is designed under #702, *Push Live Updates To Open Pages*; until it is designed and built, nothing pushes a change to the page. *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 11

**2.26 [Could]** A Bible reference that cannot be read as a passage leads to the Bible reference page all the same, which says it could not be found and offers the search (`UI/Pages/BibleReference.md rule 2.18`). *(user, 2026-09-28)* ≠ item 13

## 3. Layout

One column at every width, inside `Root`'s header and footer (`src/components/root.tsx`). There is
no shell sidebar. On a narrow screen the heading row wraps: the contribution link drops beneath
the heading.

```text
+------------------------------------------------------------+
| header (Root)                                              |
+------------------------------------------------------------+
| "The journal"                 [Share what He has done]     |
| ContentItemListPanel                                       |
|   ContentItemSearchBarPanel                                |
|   ContentItemResultsPanel -> ContentItemPanel x n          |
+------------------------------------------------------------+
| footer (Root)                                              |
+------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Main | the container, `col-12` | The page's heading row — the `h1` *The journal* and its contribution link; then `ContentItemListPanel`, rendered bare, or the error alert in its place |

## 4. Components and their hooks

### 4.1 ContentItemListPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `ariaLabel` | `The journal` | Names the list for assistive technology; the page's `h1` stands outside it. |
| `contentItemCollection` | The accumulated rows of the read, each projected with its winning setting, with the reaction this visitor chose this visit folded in; the read is the caller-scoped one today (item 6) | Rules 2.2, 2.5 and 2.6. |
| `categorySettingCollection` | The effective settings read — defaults plus the overrides of the items on screen | The bar's Category box lists the defaults alone (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.5`). |
| `criteria` | Read off the URL | Rules 2.3, 2.21 and 2.22. |
| `isLoading`, `isLoadingMore`, `hasMore` | The infinite read's state | Rule 2.6. |
| `reactionOptions` | The approved reactions, `GET api/Reactions` | Rule 2.15. |
| `emptyText` | `Nothing matched that search. Try clearing the advanced options.` | The page's own wording for an empty result. |
| Everything else | Left at the list's defaults: the bar shown, no approval-status boxes, no ribbon or status pill, `showModerationSection` off, every section switch on, titles as links, `editButtonText` *View* | Rule 2.19; the *View* default is `UI/Components/ContentItemListPanel.md §10 item 9`. |

**Hooks** — the ones the list raises itself

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | The bar commits, or a type chip or *Author* on a card is clicked. *Submitted by* and the tag and reference pills do not render on a listed card today: the projection leaves the submitter's name, the tags and the references unset (`toContentItemSearchItem.ts`, lines 62-80) | Writes the criteria into the URL; the read follows the URL (rule 2.3) | ✅ Yes (`posts.tsx`, lines 84-85 and 133) |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` ≠ item 7 | A type chip, *Submitted by* or *Author* on a card is clicked — today after the list has rewritten the criteria and raised `onSearch` (`UI/Components/ContentItemListPanel.md §10 item 11`). *Submitted by* does not render on a listed card today (section 4.1, `onSearch`) | Puts the value in its criteria, its search bar showing it in the matching box with the advanced section expanded (rule 2.12) | ❌ No — the page wires none of the three; the list's rewrite puts the value in the URL and the box, and the advanced section stays folded; item 7 |
| `onTagClick` ≠ item 7 | A tag pill on a card is clicked, after the list's rewrite. No pill renders today: the projection carries no tags until associations are exposed over HTTP (§ARC17.4, not yet built) | Puts the tag in its criteria, as the three above (rule 2.12) | ❌ No — the page wires none; the list toggles the tag criterion itself (`UI/Components/ContentItemListPanel.md §10 item 8`), and the caller-scoped read does not narrow on a tag until associations are exposed over HTTP (§ARC17.4, not yet built), so the list does not change; item 7 |
| `onBibleReferenceClick` ≠ item 13 | A reference pill on a card is clicked, after the list's rewrite. No pill renders today: the projection carries no references until associations are exposed over HTTP (§ARC17.4, not yet built) | Navigates to `/BibleReferences/{reference}`, or, for a reference it cannot read, to that page all the same, which says it could not be found, carrying `from` (rules 2.11 and 2.26) | For a readable reference ✅ Yes (`contentItemFeedNavigation.ts`, lines 45-46; `toUsfmReference.ts`, lines 67-73); for one it cannot read ❌ No — it goes to `/Search?q=<reference>` (`toUsfmReference.ts`, line 72); item 13. The list rewrites the criteria first (`UI/Components/ContentItemListPanel.md §10 item 8`) |

### 4.2 ContentItemSearchBarPanel, through the list

**Properties the page sets** — through the list: `criteria` and `categorySettingCollection`
(section 4.1). The approval-status boxes stay off (rule 2.19); every text is the bar's default.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | Search is pressed or Enter is hit | As section 4.1 | ✅ Yes (`posts.tsx`, line 133) |

### 4.3 ContentItemResultsPanel, through the list

**Properties the page sets** — through the list: `isLoading`, `isLoadingMore`, `hasMore` and
`emptyText` (section 4.1). The loading texts are the panel's defaults.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onLoadMore` | The foot of the list comes into view, or *Load more* is pressed | Fetches the next page of the same read (rule 2.6) | ✅ Yes (`posts.tsx`, line 137) |

### 4.4 The card — ContentItemPanel, inside the results

**Properties the page sets** — through the list: the element, `reactionOptions` and the
list's per-surface defaults (section 4.1).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onTitleClick` | The title — or a quote's or verse's content — is clicked | Navigates to `/posts/{id}`, carrying `from` (rule 2.7) | ✅ Yes (`contentItemFeedNavigation.ts`, line 35) |
| `onReadMore` | *read more....* is clicked on a cut card | As `onTitleClick` (rule 2.7) | ✅ Yes (`contentItemFeedNavigation.ts`, line 36) |
| `onExpandCollapse` | Only where `allowInPlaceExpansion` is on | — | *Not wired — switched off*: the page leaves in-place expansion off, so a cut card raises `onReadMore` (`UI/Components/ContentItemPanel.md rule 3.1.9`) |
| `onCommentsClick` ≠ item 5 | The comments control is clicked | Navigates to `/posts/{id}#comments`, carrying `from` (rule 2.14) | ❌ No — it navigates, but the detail view has no comments (item 5) |
| View's hook ≠ item 3 | View is clicked | Navigates to `/posts/{id}` (rule 2.8) | ❌ No — the card has no View yet (`UI/Components/ContentItemPanel.md §10 item 20`); item 3 |
| `onEditClick` ≠ item 3 | The owner's Edit is clicked — labelled *View* today | Navigates to `/myposts/{id}`, opened in edit mode (rule 2.9) | ❌ No — it navigates to `/posts/{id}` with `edit: true`, which that page never reads (`posts.tsx`, lines 99-102); item 3 |
| `onModerateClick` ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1` | Moderate is clicked | Navigates to `/Admin/Posts/{id}`, carrying `from` and `moderate: true` (rule 2.10) | ✅ Yes (`posts.tsx`, lines 104-107) |
| `onReactionSelected` ≠ item 2 | A reaction is chosen, whatever the sign-in state | Sends a signed-out reader to sign in; records, changes or clears a signed-in reader's own reaction (rule 2.15) | ❌ No — the choice is held in page state for the visit; the card redirects a signed-out reader itself (`UI/Components/ContentItemPanel.md §10 item 12`); item 2 |
| `onShareClick` | Share is clicked | Copies `/posts/{id}` and says so (rule 2.16) | ✅ Yes (`useContentItemEngagement.ts`, lines 44-49) |
| `onSaveClick` ≠ item 4 | Save is clicked | Nothing designed yet (rule 2.17) | ❌ No — it says *Saving posts is coming soon.* (`useContentItemEngagement.ts`, line 51); item 4 |
| `onTagClick`, `onBibleReferenceClick`, `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` | A pill or a meta segment is clicked | The list wraps them (section 4.1) | As section 4.1 |
| `onAdded`, `onModified`, `onRemoved`, `onCancelled` | Never on a listed card | — | *Not wired — switched off*: the list carries none of the writing faces' properties (rule 2.20) |

## 5. Security and access

Every reader reaches the page (rule 2.1), and every reader is to be shown the same approved rows
(rule 2.2) — today the caller-scoped read shows more (item 6). The card's actions are decided as
`ContentItemListPanel` and
`ContentItemPanel` decide them (`UI/Components/ContentItemListPanel.md §3.4`,
`UI/Components/ContentItemPanel.md §5`). The page asks no role of its own. Owner means the item's
contributor.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — its heading, the search bar and the list | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The contribution link ≠ item 8 | ✅ Yes⁴ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `ReadOnly` or `ContentItem-ReadOnly` — **the contribution link** ≠ item 9 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Public rows listed | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Rows that are not public — drafts, submissions and refusals — listed ≠ item 6 | ❌ No | ❌ No | ❌ No¹ | ❌ No¹ | ❌ No¹ | ❌ No¹ |
| The title, *read more....*, the pills, the comments control and Share | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| View ≠ item 3 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The owner's Edit, no read-only role covering the item's type ≠ item 3 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Moderate, no read-only role covering the item's type ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1` | ❌ No | ❌ No | ❌ No | ✅ Yes² | ✅ Yes² | ✅ Yes |
| Like, where the item's setting allows reactions ≠ item 2 | ✅ Yes³ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Save ≠ item 4 | ✅ Yes⁵ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Listed today, with no status shown: the owner's own rows, and every non-deleted row for a review role (§SEC14.7 posture A rule 4; item 6).
² Offered, and the page sends them to `/Admin/Posts/{id}`, which admits `Administrators` alone today (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`).
³ Offered; choosing raises the hook, and the page is to send them to sign in (rule 2.15).
⁴ Offered; the page is to send them to sign in and then on to the contribution form (rule 2.13).
⁵ Offered; pressing it raises the hook, and the page is to send them to sign in (rule 2.17).

**The read-only roles.** The page composes none. What a holder of a read-only role may do on a
card, Share included, is `UI/Components/ContentItemPanel.md §5`: sharing adds no content, so no
read-only role withholds it (user ruling 2026-09-27).

**The server decides.** The rows are §SEC14.1's canonical set, the same for every caller; the
caller-scoped read the page uses today answers under §SEC14.7 posture A rule 4 instead (item 6).
Every write a card leads to is decided again by the service (§SEC14.6).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — the card renders before the settings read lands.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share. Rule 2.24 (user rulings
   2026-09-27): a card is not rendered until its setting has loaded, and the page shows the
   list's loading state meanwhile; if the settings read fails, the page shows its error, announced,
   with a Retry, in the cards' place. The page projects its
   elements through `toContentItemSearchItem` from its own effective-settings read
   (`contentItemSettingService.useGetEffectiveSettingsFor`), but renders the cards once the list
   read has landed, without waiting for that read. While it is in flight, or if it fails, the
   element carries no setting, although a setting always applies
   (`UI/Components/ContentItemPanel.md rule 2.39`), and the card shapes itself by the
   `UI/Components/ContentItemPanel.md rule 2.26` fallback. Evidence: `posts.tsx` —
   `contentItemSettings ?? []` (lines 81 and 131). The page's alert answers the list read alone
   (rule 2.18), so a failed settings read still shows the cards. The same gap on `/` is
   `UI/Pages/Home.md §6 item 1`.
2. (needs issue) **Page gap — the page does not act on a chosen reaction.** Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share. A signed-out reader who
   chooses a reaction is to be sent to sign in, with return information that brings them back
   afterwards, and a signed-in reader's reaction recorded or cleared
   (`UI/Components/ContentItemPanel.md rule 3.2.4`). The page takes `onReactionSelected` from
   `useContentItemEngagement`, which only toggles the choice in page state for the visit and reads
   no sign-in state (`src/hooks/useContentItemEngagement.ts`, lines 34-42); the card redirects a
   signed-out reader itself (`UI/Components/ContentItemPanel.md §10 item 12`, which this item ships
   with). Recording and withdrawing the reader's own reaction (§ARC16.8.1, designed and not yet built) are this item's work, and so is the redirect, which uses
   the one reusable sign-in action (`UI/Pages/Home.md §6 item 3`), and must not fire while the
   reader's sign-in state is still being read — the guard the card holds today. The same gap on
   `/` is `UI/Pages/Home.md §6 item 2`, whose evidence stands for this page too.
3. (needs issue) **View and Edit lead to the wrong places.** Rules 2.8 and 2.9 send View to
   `/posts/{id}` and the owner's Edit to `/myposts/{id}` in edit mode. The card has no View
   (`UI/Components/ContentItemPanel.md §10 item 20`), and the list relabels the owner's Edit as
   *View* (`UI/Components/ContentItemListPanel.md §10 item 9`). The page routes that Edit to
   `/posts/{id}` with `edit: true` in router state (`posts.tsx` — `editContentItem`, lines 99-102),
   which `/posts/{id}` never reads (`UI/Pages/PostDetail.md rule 2.9`). The same gap on `/` is
   `UI/Pages/Home.md §6 item 4`.
4. (needs issue) **Save has nothing behind it.** Every card offers Save (rule 2.17), and the
   handler only says *Saving posts is coming soon.* (`useContentItemEngagement.ts`, line 51), to a
   signed-out reader too. Save has no design yet. The same gap, and its plan under §UI20.6.6 rule 4, is
   `UI/Pages/Home.md §6 item 5`: one handler serves both pages.
5. (needs issue) **The comments control leads to a detail view with no comments.** The page
   sends it to `/posts/{id}#comments` (rule 2.14), and `/posts/{id}` renders no comments and no
   element with that id. The same gap on `/` is `UI/Pages/Home.md §6 item 6`.
6. (needs issue) **Page gap — `/posts` lists rows that are not public.** Rule 2.2 (user ruling
   2026-09-27): `/posts` shows only approved content, to every reader, the owner and the
   moderators included; unreviewed content is seen only on `/myposts`, an owner's own, and in the
   admin area. The page reads the caller-scoped `GET api/ContentItems` (`posts.tsx` —
   `useSearchContentItems(criteria, { scope: 'caller' })`, line 60), which lists, besides the
   public set, the signed-in reader's own rows at every status and, for a review role, every
   non-deleted row — drafts, submissions and refusals included (§SEC14.7 posture A rule 4) — with
   no status shown (rule 2.19), so such a row reads here as a published one does. The test "should
   feed the panel from the caller-scoped read" (`posts.test.tsx`) holds that read in place. `/`
   already reads a caller-independent set: the feed, or, once the reader narrows,
   `GET api/ContentItems/Public` over §SEC14.1's canonical set (`UI/Pages/Home.md rules 2.2 and
   2.3`; `apiBroker.contentItems.ts`, lines 94-99 and 201-204). A public list reads one of those
   two caller-independent reads, both built, and never the caller-widening collection (§SEC14.2
   rules 1 and 2); the page's half is to read them.
7. (needs issue) **Page gap — a card's tag, type chip, *Submitted by* and *Author* clicks are not
   the page's.** Rule 2.12 (user rulings 2026-09-27): each click raises its hook, and this page,
   the journal's search, puts the value in its own criteria, its search bar showing it in the
   matching box with the advanced section expanded. The page wires none of `onTagClick`,
   `onContentTypeClick`, `onSubmittedByClick` and `onAuthorClick` (`posts.tsx` — the
   `ContentItemListPanel` element, lines 128-147). Today the list does the criteria half itself —
   it rewrites the criteria and raises `onSearch`, which the page writes into its URL — and that
   rewrite is to go (`UI/Components/ContentItemListPanel.md §10 items 8 and 11`), after which
   the clicks would do nothing here. The advanced section stays folded: neither the list nor the
   bar can yet be asked to open it (`UI/Components/ContentItemListPanel.md §10 item 12`,
   `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 7`). The page's half
   is to wire the four hooks, putting the value in its criteria and opening the advanced section.
   A tag narrows nothing until associations are exposed over HTTP (§ARC17.4, not yet built). The same gap on `/` is `UI/Pages/Home.md §6 item 9`, which
   names the others.
8. (needs issue) **Page gap — the contribution link sends a signed-out reader to the
   contribution page unsigned.** Rule 2.13 (user rulings 2026-09-27; §UI20.6.6 rule 2): a
   signed-out reader who presses *Share what He has done* is sent straight to sign in through the
   one reusable sign-in action, and then on to the contribution form, `/posts/contribute`, the
   origin surviving the sign-in step (`UI/Pages/Contribute.md rule 2.9`). The link
   sends every reader to `/posts/contribute` and the page reads no sign-in state (`posts.tsx`,
   lines 117-120), so a signed-out reader lands on the contribution page unsigned and has to press
   its *Login to contribute* link, which signs them in and returns them there
   (`UI/Pages/Contribute.md rule 2.10`): one step more than the ruled route.
   The redirect uses the one reusable sign-in action (`UI/Pages/Home.md §6 item 3`), and must not
   fire while the reader's sign-in state is still being read. The same gap for `SharingPanel` on
   `/` is `UI/Pages/Home.md §6 item 10`.
9. (needs issue) **Page gap — the page's own contribution link invites a reader who cannot
   contribute.** Rule 2.23 (user ruling 2026-09-27): *Share what He has done* behaves as
   `SharingPanel` does — hidden from a signed-in holder of `ReadOnly` or `ContentItem-ReadOnly`,
   and still shown to a reader whose only read-only roles are per content type
   (`UI/Components/SharingPanel.md rules 2.10 and 2.11`). The link is the page's own markup, shown
   to every reader, and the page reads no role for it (`posts.tsx`, lines 117-120). So a holder of
   the global `ReadOnly` is invited to a page that refuses them at its door
   (`UI/Pages/Contribute.md rule 2.2`), and a holder of `ContentItem-ReadOnly` to one that shows
   them the restricted face alone (`UI/Pages/Contribute.md rule 2.14`). The signed-out half is
   item 8. The same gap on `/myposts` is `UI/Pages/MyPosts.md §6 item 9`.
10. (needs issue) **Page gap — the query string follows only a committed search, and never opens
   the advanced section.** Rules 2.21 and 2.22 (user ruling 2026-09-27). The page reads its
   criteria off the query string and writes them back only when the list raises `onSearch`
   (`posts.tsx` — `search`, lines 84-85): on Search or Enter in the bar, or on a card's type chip
   or *Author* (section 4.1). What the reader types or picks in the form reaches the address only
   once they search. The bar raises no hook as its boxes change — it holds the drafts itself and
   raises `onSearch` with the committed criteria alone (`contentItemSearchBarPanel.tsx`, the
   comment above `ContentItemSearchBarPanelProps`) — so the page has nothing to follow until the
   bar and the list offer one: the bar's half is
   `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 8`, and the list's,
   forwarding it, is `UI/Components/ContentItemListPanel.md §10 item 13`. The advanced section
   opens folded whatever the query string carries: neither the list nor the bar can yet be handed
   an expanded state (`UI/Components/ContentItemListPanel.md §10 item 12`,
   `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 7`). The page's half
   is to write the query string as the form changes, without a reload, and to open the advanced
   section whenever the query string carries one of its criteria. The user-section pages that hand
   this page a value put it in the address they build (`UI/Pages/Home.md rule 2.13`,
   `UI/Pages/PostDetail.md rules 2.13 and 2.18`, `UI/Pages/MyPosts.md rule 2.14`,
   `UI/Pages/MyPostDetail.md rule 2.15`). The admin
   section's queue follows the same two rules (`UI/Pages/ContentItemModerationPage.md §6 item 6`).
11. (#702) **Page gap — a changed setting does not reach the open page.** Rule 2.25 (user rulings
    2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open page: the page
    reads its settings through `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does,
    so a setting another person changes reaches it only on the query library's own triggers or on a
    reload. The live connection is designed under #702, *Push Live Updates To Open Pages* (user
    ruling 2026-09-27); this page's share — hearing of a change to a setting that governs an item
    it shows, and updating what it shows — is carved from that design. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
12. (needs issue) **Page gap — the contribution link carries no origin.** Rule 2.13 (user ruling
    2026-09-27): every link to `/posts/contribute` passes its own address as the origin, this
    page's included, and the origin survives the sign-in step, so the contribution page's Cancel
    returns the reader here (`UI/Pages/Contribute.md rule 2.9`). The link is a bare
    `<Link to="/posts/contribute">` with no router state (`posts.tsx`, lines 117-120), so the
    contribution page is told no origin, and its Cancel cannot return the reader here. The
    contribution page's half, returning the reader to the origin it is told, is
    `UI/Pages/Contribute.md §6 item 3`; the signed-out half of this link is item 8. The same gap on
    `/myposts` is `UI/Pages/MyPosts.md §6 item 11`.
13. (needs issue) **Page gap — an unreadable Bible reference leads to the demo search page.** Rule
    2.26 (user ruling 2026-09-28): a reference that cannot be read as a passage leads to the Bible
    reference page all the same, which says it could not be found. The page's `onBibleReferenceClick` sends it
    to `/Search?q=<reference>` instead (`contentItemFeedNavigation.ts`, lines 45-46;
    `toUsfmReference.ts`, line 72). No pill renders on a listed card today (section 4.1). The same
    gap on `/` is `UI/Pages/Home.md §6 item 14`, whose evidence stands for this page too.
14. **Note — the header comment's gap is recorded on `/`.** The header comment of `posts.tsx`
    (line 38) says the header's search lands on this page's results. That comment is one of the
    four stale comments of `UI/Pages/Home.md §6 item 13`, which records the gap once for both
    pages.
