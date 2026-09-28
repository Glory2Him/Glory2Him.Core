# 1. MyPosts

The contributor's own shelf. It lists every content item the signed-in reader has contributed, at
every approval status, searched and scrolled. It uses the same list family as the home feed,
narrowed to one person. Any signed-in reader uses it, and the contribution page returns a
contributor here after a submission (`UI/Components/ContentItemPanel.md §6.4`).

- **Route:** `/myposts` (`src/routes/publicPostRoutes.tsx` lines 43-49 at 70dc72e7)
- **Source:** `src/pages/myPosts.tsx`; its tests are in
  `src/pages/contentItemFeedPages.test.tsx` (the `MyPosts` and "the moderate destination" blocks)
  and `src/pages/likeControlSurfaces.test.tsx`
- **Section:** user
- **Access:** any signed-in reader. The route is wrapped in `SecuredRoute` with no role list
  (`publicPostRoutes.tsx` lines 45-48 at 70dc72e7; `src/components/securitys/securedRoutes.tsx`).
  While the sign-in state is still being read, the guard renders nothing. A signed-out visitor gets
  the guard's *Access Restricted* alert and its *Login* button.
- **Layout:** single column
- **Components:** [ContentItemListPanel.md](../Components/ContentItemListPanel.md), and through it
  [ContentItemListPanel.ContentItemSearchBarPanel.md](../Components/ContentItemListPanel.ContentItemSearchBarPanel.md),
  [ContentItemListPanel.ContentItemResultsPanel.md](../Components/ContentItemListPanel.ContentItemResultsPanel.md)
  and one [ContentItemPanel.md](../Components/ContentItemPanel.md) card per item. Each card is drawn
  by one of the view templates:
  [ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md)
  or [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md).
  There is no undocumented building block. The heading and the *Share what He has done* link are
  the page's own markup.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** Only a signed-in reader reaches the page, because a visitor has no "my". *(code: publicPostRoutes.tsx — the comment above the `myposts` route; code: securedRoutes.tsx — `SecuredRoute`)*

**2.2 [Must]** The page pins the read to the signed-in account. It asks the caller-scoped read for the items whose submitter is that account, so the list is the reader's own contributions at every status. A *Submitted by* click cannot widen it to anybody else. *(code: myPosts.tsx — the header comment, `useSearchContentItems` with `scope: 'caller'` and `submittedById`; test: contentItemFeedPages.test.tsx — "should pin the read to the signed-in account")*

**2.3 [Must]** The read waits for the account id. Until the id has resolved, the page asks for nothing and the list shows its loading state, so it never asks for everybody's rows while the identity is still arriving. *(code: myPosts.tsx — `enabled: userId.length > 0`, `isLoading || userId.length === 0`; test: contentItemFeedPages.test.tsx — "should hold the read while the account id has not resolved")*

**2.4 [Must]** By default the page shows the whole shelf. The read asks for Draft, Submitted, Approved and Rejected, and the search bar offers the four approval-status boxes with all four ticked, so the boxes describe the read (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.21`). When the reader commits a selection, it replaces the default. *(code: myPosts.tsx — `defaultApprovalStatuses`, the four `searchApproval*Selected`; test: contentItemFeedPages.test.tsx — "should offer the approval statuses in the search options")*

**2.5 [Should]** Every card wears its status as the corner ribbon, and the status pill is off, because a card that says *Draft* twice reads as two different facts about the row. *(code: myPosts.tsx — the `showApprovalStatus={false}` comment; test: contentItemFeedPages.test.tsx — "should show the caller their own draft wearing its ribbon")*

**2.6 [Must]** The committed search criteria live in the URL. `onSearch` writes them to the query string, and the page reads them back from it. *(code: myPosts.tsx — `search`, `criteria`)*

**2.7 [Must]** Every way into an item from this page stays in the user section. The title, *read more*, the comments control and the owner's Edit lead to `/myposts/{id}`, and each carries the page's path and query in router state as `from`. The owner's Edit may lead to `/myposts/{id}` when it is not an in-place swap to the edit template. *(code: myPosts.tsx — `feedNavigation`, `editContentItem`; user, 2026-09-26)* ≠ `UI/Pages/MyPostDetail.md §6 item 1`

**2.8 [Must]** Moderate leads to the item's admin address, `/Admin/Posts/{id}`, where the item's other moderation tasks are performed. It carries `from` in router state. A moderator reading their own posts is still moderating. *(user, 2026-09-26; code: myPosts.tsx — `moderateContentItem`; test: contentItemFeedPages.test.tsx — "should send a moderator from my posts to the admin address")* ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`

**2.9 [Must]** Each element carries its winning setting. The page reads the content type defaults plus the overrides of exactly the items on screen, and projects each item together with its winner, so a card renders under the item's own override where one exists (§DOM6.4; `UI/Components/ContentItemPanel.md rule 2.39`). *(code: myPosts.tsx — `useGetEffectiveSettingsFor`, `toContentItemSearchItem`)* ≠ item 1

**2.10 [Must]** Like, Share and Save come from the shared engagement hook, `useContentItemEngagement`. Section 4.4 gives what each does. *(code: myPosts.tsx — `useContentItemEngagement`)*

**2.11 [Should]** A failed read replaces the list with an alert: "We could not load your posts right now. Please try again later." *(code: myPosts.tsx — the `isError` branch)*

**2.12 [Should]** An empty result reads "You have not contributed anything that matches. Share what He has done!" *(code: myPosts.tsx — `emptyText`)*

**2.13 [Could]** Beside its heading the page offers *Share what He has done*, a link to `/posts/contribute`. *(code: myPosts.tsx — the `Link` beside the `h1`)*

**2.14 [Must]** A click on a card's tag, type chip, *Submitted by* or *Author* raises its hook, and the page opens the journal's search, `/posts`, handed the value: its search bar shows it in the matching box — Tags, Category, Submitted by or Author, each one of the bar's advanced boxes — with the advanced section expanded (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). A tag narrows the list there once associations are exposed over HTTP (§ARC17.4, not yet built). *(user, 2026-09-27; `UI/Components/ContentItemListPanel.md rules 2.8–2.12 and 2.24`)* ≠ item 8

**2.15 [Must]** View leads to the item's detail view, read-only, on the contributor's own surface: `/myposts/{id}`, carrying `from`. *(user, 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.40`; `UI/Pages/MyPostDetail.md rule 2.5`)* ≠ item 6

**2.16 [Must]** Share is offered only on an `Approved` item. The address it copies, `/posts/{id}`, shows an approved item alone (`UI/Pages/PostDetail.md rule 2.1`), and this page lists the reader's items at every status (rule 2.4). *(user, 2026-09-27)* ≠ item 5

**2.17 [Must]** The page's own contribution link, *Share what He has done*, behaves as `SharingPanel` does (`UI/Components/SharingPanel.md rules 2.9–2.11`): it is hidden from a holder of `ReadOnly` or `ContentItem-ReadOnly`, and still shown to a reader whose only read-only roles are per content type. The route admits no signed-out reader (rule 2.1), so no sign-in redirect arises here. *(user, 2026-09-27)* ≠ item 9

**2.18 [Must]** A card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the cards back and shows the list's loading state, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the cards' place — never the cards without their settings. *(user, 2026-09-27)* ≠ item 1

**2.19 [Must]** A change to an item's setting reaches the open page without a reload: comments switched off for an item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is designed under #702, *Push Live Updates To Open Pages*; until it is designed and built, nothing pushes a change to the page. *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 10

**2.20 [Must]** The page's contribution link, *Share what He has done*, carries this page's own address as `from`, so the contribution page's Cancel returns the reader here (`UI/Pages/Contribute.md rule 2.9`). *(user, 2026-09-27)* ≠ item 11

**2.21 [Could]** A Bible reference that cannot be read as a passage leads to the Bible reference page all the same, which says it could not be found and offers the search (`UI/Pages/BibleReference.md rule 2.18`). *(user, 2026-09-28)* ≠ item 12

## 3. Layout

One column in the public chrome, which has the site header above and the footer below. There is
no shell sidebar. On a narrow screen the heading row wraps: the *Share what He has done* link drops
beneath the heading (`myPosts.tsx`, line 126).

```text
+---------------------------------------------------------------+
| site header                                                   |
+---------------------------------------------------------------+
| My posts                           [Share what He has done]   |
| ContentItemListPanel                                          |
|   search bar  (query, Search, advanced options, 4 statuses)   |
|   results     (one card per item, scrolled)                   |
+---------------------------------------------------------------+
| site footer                                                   |
+---------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Main | `col-12` in the page `container` | the heading and the *Share what He has done* link; then `ContentItemListPanel` (its search bar, then its results), or the error alert in its place |

## 4. Components and their hooks

### 4.1 ContentItemListPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `ariaLabel` | `My posts` | Names the list's section. |
| `contentItemCollection` | The loaded pages, projected with each item's winning setting, with the visit's chosen reactions folded in (`withViewerReactions`) | Rules 2.2 and 2.9. |
| `categorySettingCollection` | The settings read (`contentItemSettings ?? []`) | Feeds the Category box. |
| `criteria`, `onSearch` | Read from, and written to, the URL | Rule 2.6. |
| `isLoading` | The read's first-page load, or no account id yet | Rule 2.3. |
| `isLoadingMore`, `hasMore`, `onLoadMore` | The infinite read's own state and `fetchNextPage` | The scroll. |
| `showApprovalStatusSearchOptions` and the four `searchApproval*Selected` | `true` | Rule 2.4. |
| `showApprovalStatusRibbon` / `showApprovalStatus` | `true` / `false` | Rule 2.5. |
| `reactionOptions` | The approved reactions (`GET api/Reactions`) | The choices behind Like. |
| `emptyText` | Rule 2.12's text | Rule 2.12. |

The page leaves every other property at its default. So `showModerationSection` is off,
`allowTitleClick` is on, the six section switches are on, the content is cut at 400 characters
with a *read more* that raises `onReadMore`, and `editButtonText` keeps the list's default,
*View*, which labels the owner's Edit (`UI/Components/ContentItemListPanel.md §10 item 9`).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | The bar commits, a status box changes, or a card's type chip or *Author* is clicked; *Submitted by* does not render here (section 4.4) | Writes the criteria to the URL, and the read follows them (rule 2.6). | ✅ Yes (`myPosts.tsx`, lines 91-92 and 145) |
| `onLoadMore` | The foot of the results comes into view, or *Load more* is pressed | Fetches the next page of the pinned read. | ✅ Yes (`myPosts.tsx`, line 165) |

### 4.2 ContentItemSearchBarPanel, through the list

**Properties the page sets:** those the list forwards (`UI/Components/ContentItemListPanel.md §4.3`):
`criteria`, the Category rows and the four status flags. The page sets no text.

**Hooks:** the bar has one hook, `onSearch`, and it reaches the page as the list's `onSearch`
(section 4.1).

### 4.3 ContentItemResultsPanel, through the list

**Properties the page sets:** the list's paging state and texts (section 4.1).

**Hooks:** the panel has one hook, `onLoadMore`, and it reaches the page as the list's
`onLoadMore` (section 4.1).

### 4.4 ContentItemPanel — each card, through the results

**Properties the page sets:** the card properties section 4.1 lists, through the list. The page
wires the navigation hooks by spreading `buildContentItemFeedNavigation(navigate, location,
(item) => '/myposts/' + item.id)`, which supplies `onTitleClick`, `onReadMore`, `onCommentsClick` and
`onBibleReferenceClick` (`src/services/views/contentItems/contentItemFeedNavigation.ts`).

**Hooks** — every hook the card raises. `onAssignedReactionsClick` and `onReactionClick` are
handled inside `ContentItemPanel` (the counts toggle and the Like picker) and never reach the page.

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onTitleClick` | The title is pressed | Navigates to `/myposts/{id}` with `from` (rule 2.7). | ✅ Yes (`myPosts.tsx`, lines 97-98; `contentItemFeedNavigation.ts`, line 35) |
| `onReadMore` | *read more....* is pressed on a cut card | Navigates to `/myposts/{id}` with `from`. | ✅ Yes (`myPosts.tsx`, lines 97-98; `contentItemFeedNavigation.ts`, line 36) |
| `onExpandCollapse` | The in-place *read more…* / *show less* is pressed | Nothing, because the control is not offered: `allowInPlaceExpansion` is left off, so *read more* navigates instead. | *Not wired — switched off* (the list's default, `UI/Components/ContentItemPanel.md rule 3.1.9`) |
| `onContentTypeClick` | The type chip is pressed | Opens `/posts` handed the type, its search bar showing it in the Category box with the advanced section expanded (rule 2.14). Today the page wires no hook of its own: the list toggles the Category criterion on this page and raises `onSearch` (section 4.1; `UI/Components/ContentItemListPanel.md §10 item 11`), so the reader stays on their own shelf. ≠ item 8 | ❌ No — item 8 |
| `onSubmittedByClick` | *Submitted by* is pressed | Were it to render, the page would open `/posts` handed the submitter (rule 2.14). | *Never raised*: the segment never renders here, because the projection carries no submitter name (`toContentItemSearchItem.ts`, lines 62-80) |
| `onAuthorClick` | *Author* is pressed | Opens `/posts` handed the author, as the type chip (rule 2.14). Today the list sets the author criterion on this page and raises `onSearch`, and the page wires no hook of its own. ≠ item 8 | ❌ No — item 8 |
| `onTagClick` | A tag pill is pressed | No pill renders today, because the projection carries no tags until associations are exposed over HTTP (§ARC17.4, not yet built). When one does, the page is to open `/posts` handed the tag (rule 2.14); the list toggles the tag criterion instead (`UI/Components/ContentItemListPanel.md §10 item 8`), and the page supplies no destination of its own. ≠ item 8 | ❌ No — item 8 |
| `onBibleReferenceClick` ≠ item 12 | A Bible reference pill is pressed | No pill renders today: associations are not yet exposed over HTTP (§ARC17.4). When one does, the page navigates to the passage at `/BibleReferences/{USFM}`, or, for a reference it cannot read, to that page all the same, which says it could not be found (rule 2.21), with `from`. The list also toggles the reference criterion first (`UI/Components/ContentItemListPanel.md §10 item 8`). | For a readable reference ✅ Yes (`contentItemFeedNavigation.ts`, lines 45-46; `toUsfmReference.ts`, lines 67-73); for one it cannot read ❌ No — it goes to `/Search?q=<reference>` (`toUsfmReference.ts`, line 72); item 12 |
| `onCommentsClick` | The comments control is pressed | Navigates to `/myposts/{id}#comments` with `from`. That page shows no comments. ≠ item 4 | ✅ Yes (`contentItemFeedNavigation.ts`, lines 40-41); the destination is item 4 |
| `onReactionSelected` | The reader chooses a reaction | Records, changes or clears the reader's own reaction. A signed-out reader never reaches the page (rule 2.1). ≠ item 2 | ❌ No — the choice is held in page state for the visit, and nothing is recorded (`useContentItemEngagement.ts`, lines 34-42); item 2 |
| `onShareClick` | *Share* is pressed, on an `Approved` item alone (rule 2.16) | Copies `{origin}/posts/{id}` to the clipboard and toasts "Link copied." ≠ item 5 | ✅ Yes (`useContentItemEngagement.ts`, lines 44-49), but Share is offered on every card, whatever its status; item 5 |
| `onSaveClick` | *Save* is pressed | Saves the post for the reader; Save has no design yet. ≠ item 3 | ❌ No — it toasts "Saving posts is coming soon." and saves nothing (`useContentItemEngagement.ts`, line 51); item 3 |
| `onEditClick` | The owner's Edit is pressed. It reads *View* today (`UI/Components/ContentItemListPanel.md §10 item 9`) | Navigates to `/myposts/{id}`, carrying `from` and `edit: true` in router state (rule 2.7). The detail page does not act on `edit` today. ≠ `UI/Pages/MyPostDetail.md §6 item 1` | ✅ Yes (`myPosts.tsx`, lines 111-114 and 170) |
| `onModerateClick` | Moderate is pressed (the shield) by an owner who also holds the moderation tier | Navigates to `/Admin/Posts/{id}`, carrying `from` and `moderate: true` in router state (rule 2.8). That page admits `Administrators` alone today. ≠ `UI/Pages/ContentItemModerationDetailPage.md §6 item 1` | ✅ Yes (`myPosts.tsx`, lines 116-119 and 171) |
| View's hook ≠ item 6 | View is pressed | Navigates to `/myposts/{id}`, read-only, carrying `from` (rule 2.15). | ❌ No — the card has no View action, hook or switch yet (`UI/Components/ContentItemPanel.md §10 item 20`); item 6 |

## 5. Security and access

Every card on this page is the viewer's own, because the read is pinned to their account (rule
2.2). So the Signed-in reader column, which by definition does not own the item, has no card row,
and each card row states in its condition that the viewer owns the card and, in a tier column,
also holds that tier (§UI20.6.4). The card's
gates are `UI/Components/ContentItemListPanel.md §3.4` and `UI/Components/ContentItemPanel.md §3.4`.
The read-only roles each action answers to are those components' security and access matrices,
`UI/Components/ContentItemListPanel.md §5` and `UI/Components/ContentItemPanel.md §5`.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page: the heading, *Share what He has done*, the search bar, the status boxes and the results | ❌ No¹ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `ReadOnly` or `ContentItem-ReadOnly` — ***Share what He has done*** ≠ item 9 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| A card the viewer owns, holding the column's tier where the column names one — the view template, *read more*, the comments control, the filter clicks, Like and Save | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, the item `Approved` — **Share** | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, the item `Draft`, `Submitted` or `Rejected` — **Share** ≠ item 5 | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No |
| As the row above — **View** ≠ item 6 | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, no ReadOnly covering the item's type — **Edit** (labelled *View* today) | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, no ReadOnly covering the item's type — **Moderate** | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes |
| As the row above, the viewer holds a ReadOnly covering the item's type — **Edit** or **Moderate** | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No |

¹ The route guard shows *Access Restricted* and a *Login* button (rule 2.1).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — `/myposts`: the cards render before the settings read lands.** The
   page projects each element through `toContentItemSearchItem` from its own effective-settings read
   (`contentItemSettingService.useGetEffectiveSettingsFor`). It renders the list once the item read
   has landed, without waiting for that settings read. While the settings read is in flight, or if it
   fails, each element carries no setting, although a setting always applies
   (`UI/Components/ContentItemPanel.md rule 2.39`). The card then shapes itself by the component's
   fallback for a missing setting. Rule 2.18 (user rulings 2026-09-27): a card is not rendered
   until its setting has loaded, and the page shows the list's loading state meanwhile; if the
   settings read fails, the page shows its error, announced, with a Retry, in the cards' place.
   The page's alert answers the list read alone (rule 2.11), so a failed settings read still shows
   the cards. Evidence: `myPosts.tsx` — `contentItemSettings ?? []`. Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share of it.
2. (needs issue) **Page gap — `/myposts`: a chosen reaction is not persisted.** What follows
   `onReactionSelected` is the page's: a signed-in reader's reaction is persisted or cleared
   (`UI/Components/ContentItemPanel.md rule 3.2.4`). The page takes the hook from
   `useContentItemEngagement`, which only toggles the choice in page state for the visit, so a
   refresh loses it. Recording and withdrawing the reader's own reaction (§ARC16.8.1, designed and not yet built) are this item's work. The sign-in half of the
   same page gap does not arise here, because `SecuredRoute` admits no signed-out reader. Evidence:
   `src/hooks/useContentItemEngagement.ts` — `onReactionSelected`. Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share of it.
3. (needs issue) **Page gap — `/myposts`: Save saves nothing.** The page wires *Save* to a toast,
   "Saving posts is coming soon." (`useContentItemEngagement.ts` — `onSaveClick`), so the action is
   dead (§UI20.6.6 rule 4). Save has no design yet (§APR9.9 rule 6). By the plan §UI20.6.6 rule 4
   states, it comes after Likes, and it needs its design before its tasks.
4. (needs issue) **Page gap — `/myposts`: the comments control leads to a page with no comments.**
   `onCommentsClick` navigates to `/myposts/{id}#comments`. `/myposts/{id}` renders no comments and
   no element with the id `comments` (`myPostDetail.tsx`; a search of `src/` finds none), so the
   reader lands at the top of the item's detail. Comments are part of the plan §UI20.6.6 rule 4
   states, after Likes, Save and Share.
5. (needs issue) **Page gap — `/myposts`: Share is offered on items that are not approved.** Rule
   2.16 (user ruling 2026-09-27): Share is offered only on an `Approved` item. *Share* copies
   `{origin}/posts/{id}` for every card on this page, drafts and items in review included, and
   that address shows an approved item alone (`UI/Pages/PostDetail.md rule 2.1`). The page wires
   `onShareClick` for the whole list (`myPosts.tsx` — `useContentItemEngagement`), and the card
   shows Share wherever `showShareSection` is on and the hook is wired
   (`UI/Components/ContentItemPanel.md rule 3.1.12`). `showShareSection` is one switch for every
   card in the list (`UI/Components/ContentItemListPanel.md §4`), so the page cannot offer Share on
   one item and not another through the list today; the list's half is
   `UI/Components/ContentItemListPanel.md §10 item 14`.
   The same gap on `/Admin/Posts` is `UI/Pages/ContentItemModerationPage.md §6 item 9`.
6. (needs issue) **Page gap — `/myposts`: View is not wired.** Rule 2.15 (user ruling
   2026-09-27): View opens the item's detail view read-only, and from `/myposts` that is
   `/myposts/{id}`, which opens read-only unless the owner's Edit sent the reader there
   (`UI/Pages/MyPostDetail.md rule 2.5`). The card has no View action, hook or switch yet
   (`UI/Components/ContentItemPanel.md §10 item 20`), and the list labels the owner's Edit *View*
   (`UI/Components/ContentItemListPanel.md §10 item 9`), which the page routes to `/myposts/{id}`
   with `edit: true` (`myPosts.tsx`, lines 111-114). The page's half ships with that item: wire
   View's hook to `/myposts/{id}`, read-only, carrying `from`.
7. (needs issue) **Page gap — `/myposts`: the sign-in return drops the search.** The route guard's
   *Login* button sends a signed-out visitor to `/Account/Login?returnUrl=<path>`, and the path it
   passes omits the query string (`securedRoutes.tsx` — `goToLogin`, line 28 at 70dc72e7). On this
   page the query string holds the committed search (rule 2.6), so a signed-out reader who follows a
   filtered `/myposts` link comes back to the unfiltered list. §UI20.6.6 rule 2 returns a reader to
   exactly the place they came from (user ruling 2026-09-27). The user's ruling of the same day that
   puts route guards outside "hooks, not routes" (§UI20.6.4) takes them out of that rule alone, not
   out of the exact return, so the loss is a gap, not a question. The guard is one of the five
   places `UI/Pages/Home.md §6 item 3` names that compose the sign-in route from the path alone and
   lose the query. The same guard fronts `/Admin/Posts`, whose committed criteria also live in its
   URL (`UI/Pages/ContentItemModerationPage.md rule 2.6`).
8. (needs issue) **Page gap — `/myposts`: a card's tag, type chip, *Submitted by* and *Author*
   clicks do not open `/posts`.** Rule 2.14 (user rulings 2026-09-27): in the user section each
   click raises its hook, and the page opens `/posts` handed the value, its search bar showing it
   in the matching box with the advanced section expanded. The page wires none of `onTagClick`,
   `onContentTypeClick`, `onSubmittedByClick` and `onAuthorClick` (`myPosts.tsx` — the
   `ContentItemListPanel` element). The list turns the type chip and *Author* clicks into a search
   of this page's own list instead — it rewrites the criteria and raises `onSearch`, which the
   page writes into its own URL (`UI/Components/ContentItemListPanel.md §10 item 11`) — so the
   reader stays on their own shelf. No tag pill and no *Submitted by* segment renders here today
   (section 4.4). The page's half is to wire the four hooks to open `/posts` handed the value; that
   the bar there opens with its advanced section expanded is
   `UI/Components/ContentItemListPanel.md §10 item 12`. The same gap on `/` is
   `UI/Pages/Home.md §6 item 9`, which names the others.
9. (needs issue) **Page gap — `/myposts`: the page's own contribution link invites a reader who
   cannot contribute.** Rule 2.17 (user ruling 2026-09-27): *Share what He has done* behaves as
   `SharingPanel` does — hidden from a holder of `ReadOnly` or `ContentItem-ReadOnly`, and still
   shown to a reader whose only read-only roles are per content type
   (`UI/Components/SharingPanel.md rules 2.10 and 2.11`). The link is the page's own markup, shown
   to every signed-in reader, and the page reads no role for it (`myPosts.tsx`, lines 129-132). The
   same gap on `/posts` is `UI/Pages/Posts.md §6 item 9`.
10. (#702) **Page gap — `/myposts`: a changed setting does not reach the open page.** Rule 2.19
    (user rulings 2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open
    page: the page reads its settings through
    `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does, so a setting another
    person changes reaches it only on the query library's own triggers or on a reload. The live
    connection is designed under #702, *Push Live Updates To Open Pages* (user ruling 2026-09-27);
    this page's share — hearing of a change to a setting that governs an item it shows, and
    updating what it shows — is carved from that design. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
11. (needs issue) **Page gap — `/myposts`: the contribution link carries no origin.** Rule 2.20
    (user ruling 2026-09-27): every link to `/posts/contribute` passes its own address as the
    origin, this page's included, so the contribution page's Cancel returns the reader here
    (`UI/Pages/Contribute.md rule 2.9`). The link is a bare `<Link to="/posts/contribute">` with no
    router state (`myPosts.tsx`, lines 129-132), so the contribution page is told no origin. The
    contribution page's half is `UI/Pages/Contribute.md §6 item 3`. The same gap on `/posts` is
    `UI/Pages/Posts.md §6 item 12`.
12. (#700) **Page gap — `/myposts`: an unreadable Bible reference leads to the demo search page.**
    Held for #700, which decides how the Bible reference page is addressed for such a reference
    (`UI/Pages/BibleReference.md rule 2.18`). Rule 2.21 (user ruling 2026-09-28): a reference that
    cannot be read as a passage leads to the Bible reference page all the same, which says it could
    not be found. The page's `onBibleReferenceClick` sends it to `/Search?q=<reference>` instead
    (`contentItemFeedNavigation.ts`, lines 45-46; `toUsfmReference.ts`, line 72). No pill renders on
    a listed card today (section 4.4). The same gap on `/` is `UI/Pages/Home.md §6 item 14`, whose
    evidence stands for this page too.
