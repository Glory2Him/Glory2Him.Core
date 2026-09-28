# 1. ContentItemResultsPanel

- **Kind:** User story — child component of ContentItemListPanel
- **Parent:** [ContentItemListPanel.md](ContentItemListPanel.md)
- **Children:** none
- **Composes:** [ContentItemPanel.md](ContentItemPanel.md) — one per element
- **Used by:** [ContentItemListPanel.md](ContentItemListPanel.md) only — no page renders it directly
- **Inherits:** `UI/Components/ContentItemListPanel.md §5 and rules 2.1, 2.5, 2.18 and 2.19`; §UI20.6.4, §UI20.6.5, §UI20.6.6; every card rule in `UI/Components/ContentItemPanel.md §3`; `UI/Components/ContentItemListPanel.md rule 2.4`
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemResultsPanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Results-Panel` — `src/pages/samplePages/components/contentItemResultsPanelDoc.tsx`

The results half of the ContentItemListPanel family. It renders every matched
element as one ContentItemPanel, scrolled rather than paged, and handles the
first-page load, the empty result and the infinite scroll.

It fetches nothing. Its whole part in paging is noticing that the foot of the list
has come into view and saying so through `onLoadMore`.

## 2. Business Rules

**2.1 [Must]** Every element of `contentItemCollection` renders as one ContentItemPanel, keyed by the element's id, in the order the consumer gave. *(code: contentItemResultsPanel.tsx — the `contentItemCollection.map`)*

**2.2 [Should]** While `isLoading` is on (the first page), the panel shows a spinner and `loadingText` in place of the list rather than emptying it, so a re-search does not flash "nothing found" on its way to results. The spinner announces the load (§UI20.6.6 rule 5). *(test: contentItemListPanel.test.tsx — "should hold the list back rather than emptying it while the first page loads"; code: coreUI/spinner.tsx — `role="status"`)*

**2.3 [Should]** A settled, empty collection shows `emptyText`. *(test: contentItemListPanel.test.tsx — "should say so when nothing matched")*

**2.4 [Must]** While `hasMore` is on, a sentinel sits at the foot of the list, and the panel raises `onLoadMore` when the sentinel comes into view. The observer looks 200px beyond the viewport, so the next page usually arrives before the reader does. *(code: hooks/useInfiniteScrollSentinel.ts — `rootMargin`; test: contentItemListPanel.test.tsx — "should ask for the next page when the foot of the list comes into view")*

**2.5 [Must]** The panel never raises `onLoadMore` while `isLoadingMore` is on: one scroll is one fetch. *(test: contentItemListPanel.test.tsx — "should ask for nothing while a page is already on its way")*

**2.6 [Must]** When a page lands and the sentinel is still in view, the panel asks for the next page. Otherwise the list would stall with the sentinel on screen. *(code: hooks/useInfiniteScrollSentinel.ts — the comment on the observer effect)*

**2.7 [Must]** With `hasMore` off, the panel asks for nothing. *(test: contentItemListPanel.test.tsx — "should ask for nothing once there is nothing left")*

**2.8 [Should]** While `isLoadingMore` is on, a spinner and `loadingMoreText` show beneath the results, in a block that announces them (§UI20.6.6 rule 5). *(code: contentItemResultsPanel.tsx; test: contentItemListPanel.test.tsx — "should ask for nothing while a page is already on its way")*

**2.9 [Must]** Where IntersectionObserver is unavailable, a Load more button stands in for the sentinel while `hasMore` is on and no page is loading. Without it the list would stop with no explanation. *(test: contentItemListPanel.test.tsx — "should offer a button where the observer is not available")*

**2.10 [Could]** Where the observer is available, no Load more button renders. *(test: contentItemListPanel.test.tsx — "should offer no button where the observer does the asking")*

**2.11 [Won't]** ContentItemPanel's form-face properties are not declared here either: `UI/Components/ContentItemListPanel.md rule 2.19` applies to this panel as written. *(code: contentItemResultsPanel.tsx — `ContentItemResultsPanelProps` comment)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `isLoading=true` renders the loading block alone. No card, no empty message, no sentinel and no button render, even when the collection holds elements. *(code: contentItemResultsPanel.tsx — the `isLoading` early return)*

**3.1.2** Otherwise, an empty collection renders the empty message alone, with no sentinel and no button, even while `hasMore` is on. *(code: contentItemResultsPanel.tsx — the empty early return)*

**3.1.3** Otherwise the panel renders, in order: the cards; the sentinel while `hasMore`; the loading-more block while `isLoadingMore`; and the Load more button while `hasMore`, not `isLoadingMore`, and no observer. *(code: contentItemResultsPanel.tsx)*

**3.1.4** The card properties change what every card shows as `UI/Components/ContentItemPanel.md §3` states: `reactionOptions`, `showModerationSection`, `allowTitleClick`, `showApprovalStatusRibbon`, `showApprovalStatus`, the content-length trio, the section switches, the texts and the hooks.

**3.1.5** Every visible string the panel renders itself — its loading, loading-more, *Load more* and empty texts — and every string it renders for a screen reader alone is a property whose default is today's text (§UI20.6.6 rule 1). *(code: contentItemResultsPanel.tsx — `ContentItemResultsPanelProps`; user, 2026-09-27)* ≠ item 5

### 3.2 Driven by roles

**3.2.1** None of its own: this panel does not read the auth context. The cards' role gates are ContentItemPanel's, as `UI/Components/ContentItemListPanel.md §3.2` describes. *(code: contentItemResultsPanel.tsx)*

### 3.3 Combinations

**3.3.1** No role applies. Among the properties, `isLoading` outranks everything else (rule 3.1.1), and an empty collection outranks the paging state (rule 3.1.2). *(code: contentItemResultsPanel.tsx — the `isLoading` early return, the empty early return)*

### 3.4 Role matrix

This panel has no role gate. The card rows are in `UI/Components/ContentItemListPanel.md §3.4`.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| `isLoading=true` — **loading block** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `isLoading=false`, empty collection — **empty message** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `hasMore=true`, `isLoadingMore=false`, no IntersectionObserver — **Load more** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `isLoadingMore=true` — **loading-more block** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItemCollection` | `ReadonlyArray<ContentItemSearchItem>` | `[]` | The accumulated elements | ContentItemPanel `contentItem`, one each |
| `reactionOptions` | `ReadonlyArray<ContentItemReactionOption>` | `[]` | The choices behind Like | ContentItemPanel, same name |
| `showModerationSection` | `boolean` | `false` | Moderated surface | ContentItemPanel, same name |
| `allowTitleClick` | `boolean` | `true` — the list's default, the opposite of ContentItemPanel's | Titles lead to the detail | ContentItemPanel, same name |
| `showApprovalStatusRibbon`, `showApprovalStatus` | `boolean` | `false` | Ribbon and pill | ContentItemPanel, same names |
| `showContentExpanded`, `truncateAt`, `allowInPlaceExpansion` | `boolean`, `number`, `boolean` | unset — ContentItemPanel's defaults apply | The content-length trio | ContentItemPanel, same names (spread) |
| The six section switches, every `ContentItemEvents` hook, every `ContentItemText` member | as declared on ContentItemPanel | unset — ContentItemPanel's and its templates' defaults apply; `editButtonText` therefore reads Edit here unless the parent sets it | Card configuration | ContentItemPanel, same names (spread) |
| `isLoading` | `boolean` | `false` | The first page is in flight | — |
| `isLoadingMore` | `boolean` | `false` | A further page is in flight | — |
| `hasMore` | `boolean` | `false` | Another page exists — the consumer knows from its own paging | — |
| `onLoadMore` | `() => void` | — | Asks for the next page | — |
| `loadingText`, `loadingMoreText`, `loadMoreButtonText`, `emptyText` | `string` | `'Loading…'`, `'Loading more…'`, `'Load more'`, `'Nothing matched that search.'` | Loading and empty texts | — |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onLoadMore` | none | The sentinel comes into view (rules 2.4–2.6), or Load more is pressed (rule 2.9) |
| Every `ContentItemEvents` hook | as ContentItemPanel raises it | Passed through from the card unchanged |

### 4.3 Pass-through properties

Per §UI20.6.5. The parent drives this panel with the properties `UI/Components/ContentItemListPanel.md §4.3` lists for ContentItemResultsPanel, all under the same names; the five filter hooks arrive already wrapped by the parent.

This panel hands ContentItemPanel `reactionOptions`, `showModerationSection`, `allowTitleClick`, `showApprovalStatusRibbon` and `showApprovalStatus` by name, and everything else it does not consume by spread: the content-length trio, the section switches, the texts and the hooks. All reach the card unchanged. Of the ContentItemPanel properties this panel does not declare, and so cannot forward, the form-face properties and the settings collection are withheld by design — the exceptions recorded in `UI/Components/ContentItemListPanel.md §4.3` — and the rest are the gaps at `UI/Components/ContentItemListPanel.md §10 items 2 and 3`.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| *Load more* and the infinite scroll (`onLoadMore`) | Every persona, while `hasMore` is on (rules 2.4 and 2.9) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onLoadMore` | Nothing of its own: the page's read decides which rows arrive (`UI/Components/ContentItemListPanel.md rule 5.2`) |
| Every card action | As `UI/Components/ContentItemPanel.md §5` | As there | As there | As there | As there |

Inherits `UI/Components/ContentItemListPanel.md §5`; nothing to add.

## 6. Composition and Usage

Rendered by ContentItemListPanel beneath its search bar, always, whether or not the bar is shown. No page imports it directly. *(code: grep of `src/` for `<ContentItemResultsPanel`)*

```
ContentItemResultsPanel
├── ContentItemPanel × n     one per element — see ContentItemPanel.md
├── Spinner                  src/components/coreUI/spinner.tsx — the two loading blocks
└── the sentinel             useInfiniteScrollSentinel
```

## 7. Dependencies

- **Components:** ContentItemPanel and Spinner (section 6).
- **Hook:** `useInfiniteScrollSentinel` (`src/hooks/useInfiniteScrollSentinel.ts`) watches the sentinel and reports whether IntersectionObserver exists. It is the one implementation of infinite scroll, shared with ReviewCommentResultsPanel. *(code: hooks/useInfiniteScrollSentinel.ts — header comment)*
- **Data the consumer supplies:** the accumulated elements and the paging state. The OData reads answer with a plain array and no total, so the consumer asks for one row beyond the page and drops it to learn `hasMore`. *(code: contentItemResultsPanel.tsx — `hasMore` comment; code: brokers/apiBroker.contentItems.ts — `SearchContentItemsAsync`)*
- **API endpoints:** none; the consumer's are at `UI/Components/ContentItemListPanel.md §7`.

## 8. States, Validation and Feedback

| State | Condition | What renders |
| --- | --- | --- |
| First page loading | `isLoading` | Spinner and `loadingText`, announced by the spinner (rule 2.2) |
| Empty | not loading, empty collection | Info alert, `role="status"`, with `emptyText` (rule 2.3) |
| Results | not loading, one or more elements | The cards, then the paging foot (rule 3.1.3) |
| Loading more | `isLoadingMore` | Spinner and `loadingMoreText`, `role="status"`, beneath the cards (rule 2.8) |
| No observer | `hasMore`, not `isLoadingMore`, no IntersectionObserver | Load more button (rule 2.9) |

There is no error state and no validation; the page renders its own error in place of the parent (`UI/Components/ContentItemListPanel.md §8`).

## 9. Styling and Accessibility

- The sentinel is `div.g2h-content-item-sentinel`, one pixel tall and `aria-hidden`. It has height because engines disagree on whether a zero-area target intersects, and the failure would be a list that quietly stops loading. *(code: contentItems.css — `.g2h-content-item-sentinel`; code: contentItemResultsPanel.tsx — comment on the sentinel)*
- The empty message and the loading-more block carry `role="status"`. The first-page loading block's wrapper carries none; the spinner inside it carries its own `role="status"`, named by its visually hidden label, *Loading...* (section 10, item 2), which is not a property yet (section 10, item 5). *(code: contentItemResultsPanel.tsx; code: coreUI/spinner.tsx)*
- The Load more button is `btn btn-outline-primary`, centred. *(code: contentItemResultsPanel.tsx)*
- The panel renders a fragment, with no wrapper element of its own: the parent's section is the landmark. *(code: contentItemResultsPanel.tsx)*

## 10. Open Questions and Gaps

1. **Note — `Pagination`, ruled.** §UI20.6 planned `Pagination` — "Paginated navigation for feed and topic child lists" — while the feeds this panel serves scroll rather than page. This item asked whether `Pagination` was still planned for the feeds, only for topic child lists, or superseded by this panel. The user ruled on 2026-09-27 that the planned catalogue entries built under other names are superseded, each pointing at the component actually built; §UI20.6 now marks `Pagination` superseded by this panel's infinite scroll. The rules this item held open stand as written.
2. **Note — the first-page load is announced, ruled.** This item asked whether it is intended that the first-page loading block carries no `role="status"` while the loading-more block and the empty message do. The user ruled on 2026-09-27 that every loading state is announced, consistently in every component (§UI20.6.6 rule 5). Read against the code, the block already is: its wrapper carries no role, but the `Spinner` inside it carries `role="status"`, named by its own visually hidden label, *Loading...* (`coreUI/spinner.tsx`), which is what a screen reader hears; the visible `loadingText` stands outside that region. The spinner is a core-UI primitive, whose fixed label is not a pass-through gap (§UI20.6.5). Rule 2.2 and section 9 now say so.
3. **Note — the View default, ruled.** This item asked whether this panel should carry the parent's *View* default for the cards' edit button, since rendered directly the button reads *Edit*. The doc page's prose says "a page may render it directly when it has no bar to offer"; no page does. The user ruled on 2026-09-27 that View and Edit are two actions with two hooks, and that `ContentItemListPanel`'s properties decide which a card offers (`UI/Components/ContentItemListPanel.md rule 2.16`). No label default stands one in for the other at either level; the parent's is `UI/Components/ContentItemListPanel.md §10 item 9`.
4. (needs issue) The doc page (`contentItemResultsPanelDoc.tsx`) needs updating; this document follows the component. Its props table lists only `contentItemCollection`, `isLoading`, `isLoadingMore`/`hasMore`/`onLoadMore`, `showModerationSection`/`showApprovalStatusRibbon` and `emptyText`. It omits `reactionOptions`, `allowTitleClick` (whose default here, `true`, differs from ContentItemPanel's), `showApprovalStatus`, the content-length trio, the section switches, the three other texts and the pass-through hooks.
5. (needs issue) **A screen-reader-only string that is not a property.** Rule 3.1.5 applies
   §UI20.6.6 rule 1, which covers a string written for a screen reader alone as well as a visible
   one (user ruling 2026-09-27). Both loading blocks render a `Spinner` whose visually hidden
   label is *Loading...*, the spinner's own default, and the panel passes it no `label`, so no
   property of this panel sets what a screen reader hears. The spinner's fixed label is not a
   pass-through gap (§UI20.6.5; item 2); this item is rule 1's. Evidence:
   `contentItemResultsPanel.tsx` — the two `<Spinner />` elements (lines 96 and 132 at 70dc72e7);
   `coreUI/spinner.tsx` — `label = 'Loading...'`.
