# 1. ContentItemModerationPage

The moderation queue. It lists every content item the caller's tier may see, at every approval
status by default, searched and scrolled, in the admin shell. It uses the same list family as the
public feed. A moderator works it to find what needs their attention, and every way into an item
leads to that item's moderation page, `/Admin/Posts/{id}`. Only administrators reach it today.

- **Route:** `/Admin/Posts` (`src/routes/adminRoutes.tsx` lines 85-100 at 70dc72e7). It is a
  child of the `SidebarLayout` layout route (line 23).
- **Source:** `src/pages/admin/contentItemModerationPage.tsx`; its
  tests are in `src/pages/contentItemFeedPages.test.tsx` (the `ContentItemModerationPage` and
  "the moderate destination" blocks) and `src/pages/likeControlSurfaces.test.tsx`
- **Section:** admin (moderation)
- **Access:** `Administrators`. The route is wrapped in `SecuredRoute` with
  `securityPoints.contentItems.view` (`adminRoutes.tsx` line 97; `src/securityMatrix.tsx` line 27,
  `contentItems.view: ['Administrators']`). A signed-in reader outside the list gets the guard's
  *Invalid Access* alert. A signed-out visitor gets *Access Restricted* and a *Login* button. The
  route comment says a fixed role list cannot express the suffix-matched review tier, and assigns
  widening who reaches the page to #361 (closed); the widening is item 7.
- **Layout:** two columns — left sidebar with main (the admin shell's navigation beside the
  page); single column when the shell's menu is folded
- **Components:** [ContentItemListPanel.md](../Components/ContentItemListPanel.md), and through it
  [ContentItemListPanel.ContentItemSearchBarPanel.md](../Components/ContentItemListPanel.ContentItemSearchBarPanel.md),
  [ContentItemListPanel.ContentItemResultsPanel.md](../Components/ContentItemListPanel.ContentItemResultsPanel.md)
  and one [ContentItemPanel.md](../Components/ContentItemPanel.md) card per item, on its view
  templates ([ContentItemPanel.Default.md](../Components/ContentItemPanel.Default.md),
  [ContentItemPanel.ContentItemQuotesPanel.md](../Components/ContentItemPanel.ContentItemQuotesPanel.md),
  [ContentItemPanel.ContentItemVerseImagePanel.md](../Components/ContentItemPanel.ContentItemVerseImagePanel.md)).
  The one undocumented building block is `Breadcrumb` (`src/components/coreUI/breadcrumb.tsx`),
  which is navigation (§UI20.6.4).

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `Administrators`, `Publishers` and `Reviewers` reach the queue, each seeing what their permissions let them see: the server decides which rows the caller's roles reach, against the stored row (§SEC14.5 rule 4), so no status box can widen the list past what the caller may see. Only `Administrators` reach it today. *(user, 2026-09-27; code: adminRoutes.tsx — the `Admin/Posts` route and its comment; code: contentItemModerationPage.tsx — the header comment)* ≠ item 7

**2.2 [Must]** The queue reads every status by default. The read asks for Draft, Submitted, Approved and Rejected, and the search bar offers the four status boxes with all four ticked, so the boxes always describe the results (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.21`). A moderator narrows by unticking. *(code: contentItemModerationPage.tsx — `defaultApprovalStatuses`, the four `searchApproval*Selected`; test: contentItemFeedPages.test.tsx — "should read every status where the moderator has chosen none", "should offer every approval status ticked in the search options")*

**2.3 [Must]** It is a moderated surface whose cards offer **View** and no Edit. View opens the item's page, `/Admin/Posts/{id}`, read-only: in the admin area more than one thing is happening, and a moderator may want to review rather than edit, so they are taken to the read-only view first and choose Edit there to modify the item (`UI/Pages/ContentItemModerationDetailPage.md rule 2.7`). No card offers the owner's Edit, nor Moderate labelled *Edit*. *(user, 2026-09-27; code: contentItemModerationPage.tsx — `showModerationSection`)* ≠ item 5

**2.4 [Must]** Every card wears its status as the pill beside the type chip, which on this page is the point of the status. *(code: contentItemModerationPage.tsx — `showApprovalStatus`, the header comment)*

**2.5 [Must]** Every way into an item stays in the admin area. The title, *read more* and the comments control lead to `/Admin/Posts/{id}`, as View does (rule 2.3), carrying the queue's path and query as `from` in router state, so a moderator cannot fall out of the queue by clicking the heading instead of the card's action. *(code: contentItemModerationPage.tsx — `feedNavigation`; test: contentItemFeedPages.test.tsx — "should send the card title to the same admin address as Edit"; user, 2026-09-26)*

**2.6 [Must]** The committed search criteria live in the URL, as on `/myposts` (`UI/Pages/MyPosts.md rule 2.6`). *(code: contentItemModerationPage.tsx — `search`, `criteria`)*

**2.7 [Must]** Each element carries its winning setting: the page reads the defaults plus the overrides of the items on screen and projects each item with its winner (§DOM6.4; `UI/Components/ContentItemPanel.md rule 2.39`). *(code: contentItemModerationPage.tsx — `useGetEffectiveSettingsFor`, `toContentItemSearchItem`)* ≠ item 1

**2.8 [Should]** The list renders bare, with no card around it. Every row is already a card, and a card around them doubles the border and the padding. *(code: contentItemModerationPage.tsx — the comment above the panel; test: contentItemFeedPages.test.tsx — "should render the panel bare, without a card around the cards")*

**2.9 [Should]** A failed read replaces the list with an alert: "We could not load the moderation queue right now. Please try again later." An empty result reads "No posts matched that search." *(code: contentItemModerationPage.tsx — the `isError` branch, `emptyText`)*

**2.10 [Could]** The page is headed *Posts*, with the breadcrumb *Admin* › *Posts* beside the heading. *(test: contentItemFeedPages.test.tsx — "should render in the admin chrome with its breadcrumb")*

**2.11 [Must]** Like, Share and Save come from the shared engagement hook, `useContentItemEngagement` (section 4.4). *(code: contentItemModerationPage.tsx — `useContentItemEngagement`)*

**2.12 [Must]** The search lives in the query string, as on `/posts` (`UI/Pages/Posts.md rules 2.21 and 2.22`): as the moderator changes the search form, the query string follows without a reload, so the address is always a bookmark or a shareable link of the queue as they see it, and the advanced section opens whenever the query string carries a criterion that lives there. *(user, 2026-09-27)* ≠ item 6

**2.13 [Must]** In the admin section, a click on a card's tag, Bible reference, type chip, *Submitted by* or *Author* raises its hook, and the page applies the value to its own queue, `/Admin/Posts`: it puts the value in its criteria, in its query string, and its search bar shows it in the matching box with the advanced section expanded (rule 2.12). No click leaves the admin area. A tag or a Bible reference narrows the queue only once the association read is exposed over HTTP (§ARC17.4, not yet built) and a read narrows on it (`UI/Components/ContentItemListPanel.md §10 item 15`, not yet designed). *(user, 2026-09-27)* ≠ item 6

**2.14 [Must]** Share is offered only on an `Approved` item: the queue reads every status by default (rule 2.2), and the address Share copies, `/posts/{id}`, shows an approved item alone (`UI/Pages/PostDetail.md rule 2.1`). *(user, 2026-09-27)* ≠ item 9

**2.15 [Must]** A card is rendered only once its setting has loaded: until the effective settings read lands, the page holds the cards back and shows the list's loading state, announced (§UI20.6.6 rule 5). If the settings read fails, the page shows its error, announced, with a Retry, in the cards' place — never the cards without their settings. *(user, 2026-09-27)* ≠ item 1

**2.16 [Must]** A change to an item's setting reaches the open page without a reload: comments switched off for an item, say, take the comments control off its card (§ARC12.5.2 business rule 12). How the page learns of the change is §UI20.10's: the live connection makes the page's settings read stale, and the page reads it again, with no code of its own (`DesignFeatures/LiveUpdates.md` rule 1, #702). *(user, 2026-09-27; §ARC12.5.2 business rule 12)* ≠ item 10

## 3. Layout

The admin shell, `SidebarLayout`, puts its navigation menu in a left-hand column beside the page.
The page itself is one column. The menu can be folded away entirely, and the page then takes the
whole row. Below the `lg` breakpoint the menu stacks above the page.

```text
+----------------------------------------------------------------------+
| site header                                                          |
+----------------------------------------------------------------------+
| +-------------+ +--------------------------------------------------+ |
| | admin menu  | | [=] Posts                         Admin > Posts  | |
| | (NavMenu)   | | ContentItemListPanel                             | |
| |             | |   search bar  (4 statuses, all ticked)           | |
| |             | |   results     (one card per item, scrolled)      | |
| +-------------+ +--------------------------------------------------+ |
+----------------------------------------------------------------------+
| site footer                                                          |
+----------------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Left sidebar (the shell's) | `col-lg-3`; absent when the menu is folded | the admin navigation menu — not the page's |
| Main | `col-lg-9`, or `col-12` when the menu is folded | the shell's fold toggle; the heading *Posts* and the `Breadcrumb`; `ContentItemListPanel`, or the error alert in its place |

## 4. Components and their hooks

### 4.1 ContentItemListPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `ariaLabel` | `Posts awaiting moderation` | Names the list's section (item 8). |
| `contentItemCollection` | The loaded pages, projected with each item's winning setting, with the visit's chosen reactions folded in | Rule 2.7. |
| `categorySettingCollection` | The settings read (`?? []`) | Feeds the Category box. |
| `criteria`, `onSearch` | Read from, and written to, the URL | Rules 2.6 and 2.12. |
| `isLoading`, `isLoadingMore`, `hasMore`, `onLoadMore` | The infinite read's own state and `fetchNextPage` | The scroll. |
| `showApprovalStatusSearchOptions` and the four `searchApproval*Selected` | `true` | Rule 2.2. |
| `showModerationSection` | `true` | Rule 2.3. |
| `showApprovalStatus` | `true` | Rule 2.4. |
| `reactionOptions` | The approved reactions (`GET api/Reactions`) | The choices behind Like. |
| `emptyText` | "No posts matched that search." | Rule 2.9. |

Every other property keeps its default. So there is no status ribbon, titles lead to the detail,
the six section switches are on, the content is cut at 400 characters with a navigating *read
more*, and `editButtonText` is *View*. No card shows `editButtonText` here, because no card offers
the owner's Edit (rule 2.3).

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onSearch` | The bar commits, a status box changes, or a card's type chip or *Author* is clicked; *Submitted by* does not render here (section 4.4) | Writes the criteria to the URL, and the read follows them. | ✅ Yes (`contentItemModerationPage.tsx`, lines 90-91 and 137) |
| `onLoadMore` | The foot of the results comes into view, or *Load more* is pressed | Fetches the next page. | ✅ Yes (`contentItemModerationPage.tsx`, line 141) |

### 4.2 ContentItemSearchBarPanel, through the list

**Properties the page sets:** those the list forwards (`UI/Components/ContentItemListPanel.md §4.3`):
`criteria`, the Category rows and the four status flags. The page sets no text.

**Hooks:** the bar's one hook, `onSearch`, reaches the page as the list's `onSearch` (section 4.1).

### 4.3 ContentItemResultsPanel, through the list

**Properties the page sets:** the list's paging state and empty text (section 4.1).

**Hooks:** the panel's one hook, `onLoadMore`, reaches the page as the list's `onLoadMore` (section 4.1).

### 4.4 ContentItemPanel — each card, through the results

**Properties the page sets:** the card properties in section 4.1, through the list. The navigation
hooks come from `buildContentItemFeedNavigation(navigate, location, (item) => '/Admin/Posts/' +
item.id)`, which supplies `onTitleClick`, `onReadMore`, `onCommentsClick` and
`onBibleReferenceClick`.

**Hooks** — every hook the card raises. `onAssignedReactionsClick` and `onReactionClick` are
handled inside `ContentItemPanel` and never reach the page.

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onTitleClick` | The title is pressed | Navigates to `/Admin/Posts/{id}` with `from` (rule 2.5). | ✅ Yes (`contentItemModerationPage.tsx`, lines 97-98; `contentItemFeedNavigation.ts`, line 35) |
| `onReadMore` | *read more....* is pressed on a cut card | Navigates to `/Admin/Posts/{id}` with `from`. | ✅ Yes (`contentItemModerationPage.tsx`, lines 97-98; `contentItemFeedNavigation.ts`, line 36) |
| `onExpandCollapse` | The in-place toggle is pressed | Not offered: `allowInPlaceExpansion` is left off. | *Not wired — switched off* (the list's default) |
| `onContentTypeClick` | The type chip is pressed | Puts the type in the queue's criteria, its search bar showing it in the Category box with the advanced section expanded (rule 2.13). Today the list toggles the Category criterion and raises `onSearch`, and the page wires no hook of its own. ≠ item 6 | ❌ No — the list's own filter narrows the queue, and the advanced section stays folded (`contentItemListPanel.tsx`, lines 199-209); item 6 |
| `onSubmittedByClick` | *Submitted by* is pressed | Were it to render, the page would put the submitter in the queue's criteria (rule 2.13). | *Never raised*: the projection carries no submitter name, so the segment does not render (`toContentItemSearchItem.ts`, lines 62-80) |
| `onAuthorClick` | *Author* is pressed | Puts the author in the queue's criteria, as the type chip (rule 2.13). Today the list sets the author criterion and raises `onSearch`. ≠ item 6 | ❌ No — the list's own filter narrows the queue, and the advanced section stays folded (`contentItemListPanel.tsx`, lines 226-229); item 6 |
| `onTagClick` | A tag pill is pressed | No pill renders today, because the projection carries no tags until the association read is exposed over HTTP (§ARC17.4, not yet built). When one does, the page puts the tag in the queue's criteria, as the type chip (rule 2.13); the list toggles the tag criterion instead (`UI/Components/ContentItemListPanel.md §10 item 8`), and the page wires no hook of its own. ≠ item 6 | ❌ No — item 6 |
| `onBibleReferenceClick` | A Bible reference pill is pressed | No pill renders today: the association read is not yet exposed over HTTP (§ARC17.4). When one does, the page puts the reference in the queue's criteria, as the type chip (rule 2.13). Today it leaves the admin area for the public passage, `/BibleReferences/{USFM}`, or for `/Search?q=<reference>` where it cannot read the reference, with `from`, after the list toggles the reference criterion (`UI/Components/ContentItemListPanel.md §10 item 8`). ≠ item 6 | ❌ No — it leaves for the public passage (`contentItemFeedNavigation.ts`, lines 45-46; `toUsfmReference.ts`, lines 67-73); item 6 |
| `onCommentsClick` | The comments control is pressed | Navigates to `/Admin/Posts/{id}#comments` with `from`. That page shows no comments. ≠ item 4 | ✅ Yes (`contentItemFeedNavigation.ts`, lines 40-41); the destination is item 4 |
| `onReactionSelected` | The reader chooses a reaction | Records, changes or clears the reader's own reaction. ≠ item 2 | ❌ No — the choice is held in page state for the visit, and nothing is recorded (`useContentItemEngagement.ts` — `onReactionSelected`); item 2 |
| `onShareClick` | *Share* is pressed, on an `Approved` item alone (rule 2.14) | Copies `{origin}/posts/{id}` and toasts "Link copied." ≠ item 9 | ✅ Yes (`useContentItemEngagement.ts`, lines 62-67), but Share is offered on every card, whatever its status; item 9 |
| `onSaveClick` | *Save* is pressed | Saves the post for the reader; Save has no design yet. ≠ item 3 | ❌ No — it toasts "Saving posts is coming soon." and saves nothing (`useContentItemEngagement.ts` — `onSaveClick`); item 3 |
| `onEditClick` | The owner's Edit is pressed | Never offered: the moderated surface removes it from every card (rule 2.3). | *Not wired — switched off* (`UI/Components/ContentItemListPanel.md rule 3.3.1`) |
| `onModerateClick` | The moderation action, labelled *Edit* here, is pressed | Never offered: no card on the queue offers Edit (rule 2.3). ≠ item 5 | ❌ No — the page wires it, and it renders on every card: it navigates to `/Admin/Posts/{id}`, carrying `from` and `moderate: true` in router state, which nothing reads (item 8) (`contentItemModerationPage.tsx`, lines 110-113 and 157); item 5 |
| View's hook ≠ item 5 | View is pressed | Navigates to `/Admin/Posts/{id}`, read-only, carrying `from` (rule 2.3). | ❌ No — the card has no View action, hook or switch yet (`UI/Components/ContentItemPanel.md §10 item 20`); item 5 |

## 5. Security and access

Owner here is the item's contributor, holding no tier. Which card action each persona is offered
is `UI/Components/ContentItemListPanel.md §3.4`, and the read-only roles each action answers to are
`UI/Components/ContentItemListPanel.md §5` and `UI/Components/ContentItemPanel.md §5`. The rows
below are what the page adds: who reaches it, and the card's actions for those who do.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page: the search bar, the status boxes and the list ≠ item 7 | ❌ No¹ | ❌ No² | ❌ No² | ✅ Yes³ | ✅ Yes³ | ✅ Yes |
| A card — **the moderation action** (labelled *Edit*), or the owner's **Edit** ≠ item 5 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| A card — **Like**, **Save**, the comments control, the filter clicks | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes³ | ✅ Yes³ | ✅ Yes |
| A card, the item `Approved` — **Share** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes³ | ✅ Yes³ | ✅ Yes |
| A card, the item `Draft`, `Submitted` or `Rejected` — **Share** ≠ item 9 | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No |
| A card — **View** ≠ item 5 | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes³ | ✅ Yes³ | ✅ Yes |

¹ The route guard shows *Access Restricted* and a *Login* button.
² The route guard shows *Invalid Access*.
³ The design (rule 2.1). Today the route admits `Administrators` alone, and shows a reviewer or a publisher who is not one *Invalid Access* (item 7).

## 6. Open Questions and Gaps

1. (needs issue) **Page gap — `/Admin/Posts`: the cards render before the settings read lands.** Rule
   2.15 (user rulings 2026-09-27): a card is not rendered until its setting has loaded, and the page
   shows the list's loading state meanwhile; if the settings read fails, the page shows its error,
   announced, with a Retry, in the cards' place. The page renders the list once the item read has
   landed, without waiting for its effective-settings read. While that read is in flight, or if it fails, each element carries no setting, although a
   setting always applies (`UI/Components/ContentItemPanel.md rule 2.39`). The page's alert
   answers the list read alone (rule 2.9), so a failed settings read still shows the cards. Evidence:
   `contentItemModerationPage.tsx` — `contentItemSettings ?? []`. Copied from
   `UI/Components/ContentItemPanel.md §10 item 15`, this page's share of it.
2. (#746) **Page gap — `/Admin/Posts`: a chosen reaction is not persisted.** The page takes
   `onReactionSelected` from `useContentItemEngagement`, which toggles the choice in page state for
   the visit only. Recording the reader's own reaction (§ARC16.8.1, served by #728) and withdrawing it (its member built by #725, its route not yet) are this item's work. The sign-in half does not
   arise, because `SecuredRoute` admits no signed-out reader. Copied from
   `UI/Components/ContentItemPanel.md §10 item 17`, this page's share of it.
   **The cards' counts are this item's work too** (the Likes feature, `DesignFeatures/Likes.md`).
   The page reads no reaction summary, so no card shows the reactions its item has been given,
   and the reader's own reaction is the visit's page state rather than the one they hold
   (`toContentItemSearchItem.ts` leaves `reactionSummary` unset, lines 71-86). The page hands
   `useContentItemEngagement` the ids of each page of cards it has delivered and renders what
   `withReactions` projects (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §1`; §ARC16.8).
3. (needs issue) **Page gap — `/Admin/Posts`: Save saves nothing.** The page wires *Save* to the
   toast "Saving posts is coming soon.", a dead action (§UI20.6.6 rule 4), as `/myposts` does
   (`UI/Pages/MyPosts.md §6 item 3`). Save has no design yet (§APR9.9 rule 6).
4. (needs issue) **Page gap — `/Admin/Posts`: the comments control leads to a page with no
   comments.** `onCommentsClick` navigates to `/Admin/Posts/{id}#comments`. That page renders the
   review thread, which is a different thing from an item's comments, and no element with the id
   `comments` (`admin/contentItemModerationDetailPage.tsx`). The moderator lands at the top of the
   item. Comments are part of the plan §UI20.6.6 rule 4 states.
5. (needs issue) **Page gap — `/Admin/Posts`: the cards offer Edit, and no View.** Rule 2.3 (user
   ruling 2026-09-27): the queue's cards offer View, which opens `/Admin/Posts/{id}` read-only, and
   no Edit — neither the owner's Edit nor Moderate labelled *Edit*; the moderator chooses Edit on
   the item's page. Today `showModerationSection` is on, so no card offers the owner's Edit, but
   Moderate stands on every card wearing Edit's pencil and label
   (`UI/Components/ContentItemListPanel.md rule 3.3.2`), and the page wires it to
   `/Admin/Posts/{id}` (`contentItemModerationPage.tsx` — `moderateContentItem`, lines 110-113; test:
   `contentItemFeedPages.test.tsx` — "should keep Edit inside the admin area rather than the public
   post route"). The card has no View action, hook or switch yet
   (`UI/Components/ContentItemPanel.md §10 item 20`). The page's half ships with that item: offer
   View, wired to `/Admin/Posts/{id}` with `from`, and no longer offer Moderate. The item's page
   already opens read-only, the editor waiting on its own Edit
   (`UI/Pages/ContentItemModerationDetailPage.md rule 2.7`).
6. (needs issue) **Page gap — `/Admin/Posts`: a card's clicks are the list's own filters, and the
   query string follows only a committed search.** Rules 2.12 and 2.13 (user rulings 2026-09-27):
   in the admin section a card's tag, Bible reference, type chip, *Submitted by* or *Author* click
   raises its hook, and the page puts the value in the queue's criteria and its query string, the
   bar showing it with the advanced section expanded; the query string follows the form as it
   changes, and the advanced section opens whenever the query string carries one of its criteria,
   as on `/posts` (`UI/Pages/Posts.md §6 item 10`). This page wires none of `onTagClick`,
   `onContentTypeClick`, `onSubmittedByClick` and `onAuthorClick`: the list turns the type chip and
   *Author* into its own filters on the queue (`contentItemListPanel.tsx`, lines 199-209 and
   226-229; `UI/Components/ContentItemListPanel.md §10 item 11`), which leaves the advanced section
   folded, and toggles a tag criterion itself (`UI/Components/ContentItemListPanel.md §10 item 8`).
   *Submitted by* does not render here (section 4.4). A reference click leaves the admin area for
   the public passage page, `/BibleReferences/{USFM}`, or for `/Search?q=<reference>` where the
   reference cannot be read (`contentItemFeedNavigation.ts` — `onBibleReferenceClick`;
   `toUsfmReference.ts`, lines 67-73). The page writes its query string only when the list raises
   `onSearch` (`contentItemModerationPage.tsx` — `search`, lines 90-91). No pill renders until
   the association read is exposed over HTTP (§ARC17.4, not yet built). The same clicks on an item's page are
   `UI/Pages/ContentItemModerationDetailPage.md §6 items 5 and 17`.
7. (needs issue) **Page gap — `/Admin/Posts`: a reviewer or a publisher cannot reach the queue.**
   Rule 2.1 (user ruling 2026-09-27): `/Admin/Posts` shows `Administrators`, `Publishers` and
   `Reviewers` what their permissions let them see. The route admits `Administrators` alone
   (`adminRoutes.tsx`, line 97; `securityMatrix.tsx` — `contentItems.view`, line 27), so a reviewer
   or a publisher who is not an administrator is shown *Invalid Access*. `SecuredRoute` takes a
   fixed list of role names, and the review and publisher tiers are suffix-matched per content type
   (§SEC18.6), which the route comment says such a list cannot express. The route comment assigns
   "widening who reaches this surface" to #361, which is closed: the widening is this item's own
   work, and the route comment goes with it.
   The same gap on an item's moderation page is
   `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`; both routes take the same security point, `contentItems.view`.
8. (needs issue) **Stale code comments and a stale name.** `contentItemModerationPage.tsx` says the
   `moderate` intent "still rides in state for the surface #350 will build there" (the comment above
   `moderateContentItem`). #350, *Add The Freshness Channel To The Review Panel*, is closed; the item's
   moderation page exists; and nothing reads `moderate` from router state
   (`admin/contentItemModerationDetailPage.tsx` reads `from` alone). `contentItemFeedNavigation.ts`'s
   header comment likewise says the queue "will point at the moderation detail once #350 builds one",
   which it already does. The list's accessible name is *Posts awaiting moderation* (`ariaLabel`),
   while the list reads every status by default (rule 2.2). The route comment's "Draft + Submitted"
   is `UI/Components/ContentItemListPanel.md §10 item 6`.
9. (needs issue) **Page gap — `/Admin/Posts`: Share is offered on items that are not approved.**
   Rule 2.14 (user ruling 2026-09-27): Share is offered only on an `Approved` item. The page wires
   `onShareClick` for the whole list (`contentItemModerationPage.tsx` — `useContentItemEngagement`),
   so every card offers Share, whatever its status, and copies an address that shows nothing for
   an item that is not approved. As on `/myposts`, the list offers one `showShareSection` for every
   card, so the page cannot offer Share on one item and not another through the list today
   (`UI/Pages/MyPosts.md §6 item 5`).
10. (#910) **Page gap — `/Admin/Posts`: a changed setting does not reach the open page.** Rule
    2.16 (user rulings 2026-09-27; §ARC12.5.2 business rule 12). Nothing pushes a change to an open
    page: the page reads its settings through
    `contentItemSettingService.useGetEffectiveSettingsFor`, as `/` does, so a setting another
    person changes reaches it only on the query library's own triggers or on a reload. The live
    connection was designed under #702 (`DesignFeatures/LiveUpdates.md`, user rulings 2026-09-27
    and 2026-10-06): this page's share — hearing of a change to a setting that governs an item it
    shows, and updating what it shows — needs no code of its own (§UI20.10 rule 2), and is built
    when `Root` opens the connection (§UI20.10 item 1), the task this item carries. The same gap on `/` is
    `UI/Pages/Home.md §6 item 11`, whose evidence stands for this page too.
