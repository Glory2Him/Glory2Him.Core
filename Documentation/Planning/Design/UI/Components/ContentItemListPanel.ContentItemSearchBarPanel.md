# 1. ContentItemSearchBarPanel

- **Kind:** User story — child component of ContentItemListPanel
- **Parent:** [ContentItemListPanel.md](ContentItemListPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemListPanel.md](ContentItemListPanel.md) only — no page renders it directly
- **Inherits:** `UI/Components/ContentItemListPanel.md §5 and rules 2.1, 2.2 and 2.21`; §UI20.6.4, §UI20.6.5, §UI20.6.6, §DOM6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemSearchBarPanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Search-Bar-Panel` — `src/pages/samplePages/components/contentItemSearchBarPanelDoc.tsx`

The search bar of the ContentItemListPanel family. It has a query box and a Search
button, and an advanced fold-out with Category, Author, Submitted by, Shareability,
Tags and Bible references. A surface can also opt in to Approval status checkboxes.

The bar holds the reader's half-typed drafts and raises `onSearch` with the
committed criteria. What a search means is the consumer's decision.

## 2. Business Rules

**2.1 [Must]** The bar holds the drafts itself and raises `onSearch` with the committed criteria. It never runs a search: what a search means is the consumer's decision. *(code: contentItemSearchBarPanel.tsx — header comment)*

**2.2 [Must]** Every typed criterion commits on Search — the Search button, or Enter in the query box. That covers the query, Category, Author, Submitted by, Shareability, Tags with their match mode, and Bible references with theirs. *(code: contentItemSearchBarPanel.tsx — `committed`, `search`; test: contentItemListPanel.test.tsx — "should raise onSearch with everything the boxes were set to", "should commit the typed submitted-by, basis and entered tags on Search")*

**2.3 [Should]** The boxes are seeded from `criteria`, and reseeded whenever it changes. A page landing from a shared link shows what it searched for, and a pill clicked on a card shows up in its box, where the reader can remove it. *(test: contentItemListPanel.test.tsx — "should seed the boxes from the criteria it was landed with", "should follow the criteria when something else navigates here", "should seed the clicked criteria into their boxes")*

**2.4 [Must]** The reseed watches the criteria's values, not the object. A consumer that builds the criteria inline on every render does not wipe what the reader is typing. *(code: contentItemSearchBarPanel.tsx — the reseed `useEffect` and its comment)*

**2.5 [Must]** The Category box offers only the content type default rows. An override belongs to one item and is never a category. *(code: contentItemSearchBarPanel.tsx — `filterableSettings`; test: contentItemListPanel.test.tsx — "should offer non-contributable types and never an override")*

**2.6 [Must]** A soft-deleted setting row is not offered: it is excluded from active policy resolution (§DOM6.6). *(code: contentItemSearchBarPanel.tsx — `filterableSettings`)*

**2.7 [Should]** The categories are offered in the rows' own `SortOrder`, ascending, after "Any category". *(test: contentItemListPanel.test.tsx — "should offer every default type in the order the administrator set")*

**2.8 [Must]** The Category box is not filtered by `IsAvailableAsGeneralUserContribution`. Searching is not contributing: a reader must be able to narrow to a type whether or not they may write one. *(code: contentItemSearchBarPanel.tsx — `contentItemSettingCollection` comment; test: contentItemListPanel.test.tsx — "should offer non-contributable types and never an override")*

**2.9 [Must]** Author is free text, because there is no useful upper bound on authors. It asks about the author of the words, not about whoever submitted the item. *(code: contentItemSearchBarPanel.tsx — comment on the Author box)*

**2.10 [Must]** Submitted by is free text. The bar cannot resolve a display name to an account, so a typed name commits with an empty account id. While the reader leaves the box alone, the account id a pill click carried survives a Search; retyping the name drops it. *(code: contentItemSearchBarPanel.tsx — `committedSubmittedBy`; test: contentItemListPanel.test.tsx — "should commit the typed submitted-by, basis and entered tags on Search", "should seed the clicked criteria into their boxes")*

**2.11 [Should]** Shareability is a closed list, and it uses the contribution picker's labels rather than the read labels. The read labels give owned and non-owned bases the same words, so the options would repeat. *(code: contentItemSearchBarPanel.tsx — comment on the Shareability box)*

**2.12 [Must]** In the Tags and Bible references boxes, Enter turns the typed text into a removable pill. Each list has its own Any/All match mode, starting at Any, and the pills commit with Search like every other typed criterion. *(code: contentItemSearchBarPanel.tsx — the two `TagInput` blocks; test: contentItemListPanel.test.tsx — "should commit the typed submitted-by, basis and entered tags on Search")*

**2.13 [Could]** Bible reference pills wear the association surface's blue and its book icon — one look for a reference wherever it appears. Tag pills carry a `#` prefix. *(code: contentItemSearchBarPanel.tsx — the two `TagInput` elements)*

**2.14 [Should]** The Approval status checkboxes are off by default. A public feed's reader should not be offered a status they can never see. *(code: contentItemSearchBarPanel.tsx — `showApprovalStatusSearchOptions` comment; test: contentItemListPanel.test.tsx — "should leave the approval statuses out of the advanced options by default")*

**2.15 [Must]** When on, the group offers Draft, Submitted, Approved and Rejected, in that order. Dismissed is not offered, because no shipped surface lists dismissed rows. *(code: contentItemSearchItem.ts — `contentItemSearchApprovalStatusMembers`; test: contentItemListPanel.test.tsx — "should offer every searchable approval status once the surface opts in")*

**2.16 [Must]** Ticking or unticking a status box commits at once, with no Search press, and carries every other drafted criterion with it. A filter that needs a second press reads as broken. *(test: contentItemListPanel.test.tsx — "should commit the search the moment an approval status is ticked", "should commit the statuses it still carries when one is ticked off", "should carry what else is drafted on the status commit")*

**2.17 [Must]** The four `searchApproval*Selected` flags set the surface's default ticks. Unset, they rest at Approved and Rejected — the decided rows, which is what a journal shows. `/myposts` and `/Admin/Posts` turn all four on. *(code: contentItemSearchItem.ts — `defaultContentItemSearchApprovalStatusSelection`; test: contentItemListPanel.test.tsx — "should start at the decided statuses where the surface names none", "should start every box where the surface ticks all four flags")*

**2.18 [Must]** A selection committed in the criteria overrides the flags: once the reader has chosen, the boxes show their choice. *(test: contentItemListPanel.test.tsx — "should let a committed selection override the flags")*

**2.19 [Must]** Unticking a default commits the remaining defaults. Unticking the last box commits no selection, which returns the surface to its flags, because "no status at all" is not a search anybody means. *(test: contentItemListPanel.test.tsx — "should commit the remaining defaults when one of them is ticked off"; code: contentItemSearchBarPanel.tsx — `searchApprovalDraftSelected` comment)*

**2.20 [Must]** Where the group is not shown, a Search carries the criteria's statuses untouched. The surface's defaults never reach the URL from boxes the reader could not see. *(test: contentItemListPanel.test.tsx — "should commit no statuses of its own where the boxes are not offered")*

**2.21 [Must]** The bar only draws the boxes. The page hands the same four flags to its read (`defaultApprovalStatuses`), so the results are read with exactly the statuses shown ticked. *(code: contentItemSearchBarPanel.tsx — `searchApprovalDraftSelected` comment; code: pages/myPosts.tsx, pages/admin/contentItemModerationPage.tsx)*

**2.22 [Should]** The advanced fields sit outside the query form, so Enter in the Tags box adds a pill instead of running the search. *(code: coreUI/searchBar.tsx — comment above the advanced block)*

**2.23 [Must]** The page can open the bar with its advanced section expanded, through a property, so that a criterion handed in through `criteria` — a value a card's type chip, *Submitted by* or *Author* hook carried, say — stands in its box where the reader sees it and can remove it (rule 2.3). Unset, the section starts folded (rule 3.1.1). *(user, 2026-09-27)* ≠ item 7

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** The advanced options stay folded until the reader presses the chevron beside Search, unless the page asks for them expanded (rule 2.23). The chevron shows whenever there are advanced options, and this bar always supplies them. *(code: coreUI/searchBar.tsx — `isAdvancedOpen`; user, 2026-09-27)* ≠ item 7

**3.1.2** The Approval status group renders only while `showApprovalStatusSearchOptions` is `true`. *(test: contentItemListPanel.test.tsx — "should offer every searchable approval status once the surface opts in")*

**3.1.3** A status box is ticked when the committed selection names its status. With no committed selection, the four flags decide (rules 2.17, 2.18). *(code: contentItemSearchBarPanel.tsx — `seededApprovalStatuses`)*

**3.1.4** In each Any/All pair, the chosen mode is solid and marked pressed; the other is outlined. *(code: contentItemSearchBarPanel.tsx — the match-mode button groups)*

**3.1.5** The Category box lists "Any category" (`anyCategoryText`) first, then the default rows (rules 2.5–2.7). The Shareability box lists "Any shareability" first, then every basis. *(code: contentItemSearchBarPanel.tsx)*

**3.1.6** Every visible string the bar renders, and every string it renders for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). *(user, 2026-09-27)* ≠ item 6

### 3.2 Driven by roles

**3.2.1** None. The bar does not read the auth context: every persona is offered the same boxes. *(code: contentItemSearchBarPanel.tsx)*

### 3.3 Combinations

**3.3.1** No role applies, so properties alone decide. For the status boxes, a committed selection outranks the flags (rule 2.18), and hiding the group outranks both, since no box is drawn to commit anything (rule 2.20). *(test: contentItemListPanel.test.tsx — "should let a committed selection override the flags", "should commit no statuses of its own where the boxes are not offered")*

### 3.4 Role matrix

The bar has no role gate. Which personas reach it is decided by the page's route (`UI/Components/ContentItemListPanel.md §6`).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Always — **query box and Search** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Always — **advanced options** (Category, Author, Submitted by, Shareability, Tags, Bible references) | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The page asks for the advanced section expanded — **advanced options open on arrival** ≠ item 7 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showApprovalStatusSearchOptions=true` — **approval status checkboxes** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showApprovalStatusSearchOptions=false` — **approval status checkboxes** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `criteria` | `ContentItemSearchCriteria` | unset — every box empty | As last committed; seeds and reseeds the boxes | — |
| `contentItemSettingCollection` | `ReadonlyArray<ContentItemSetting>` | `[]` | The rows the Category box is built from | — |
| `showApprovalStatusSearchOptions` | `boolean` | `false` | Adds the status checkboxes | — |
| `searchApprovalDraftSelected`, `searchApprovalSubmittedSelected`, `searchApprovalApprovedSelected`, `searchApprovalRejectedSelected` | `boolean` | `false`, `false`, `true`, `true` | The default ticks | — |
| `placeholderText` | `string` | `'Search posts, authors and topics'` | The query box's placeholder and accessible name | SearchBarComponent `placeholder` |
| `categoryLabelText`, `anyCategoryText` | `string` | `'Category'`, `'Any category'` | Category box texts | — |
| `authorLabelText`, `authorPlaceholderText` | `string` | `'Author'`, `'Any author'` | Author box texts | — |
| `submittedByLabelText`, `submittedByPlaceholderText` | `string` | `'Submitted by'`, `'Anyone'` | Submitted by box texts | — |
| `shareabilityLabelText`, `anyShareabilityText` | `string` | `'Shareability'`, `'Any shareability'` | Shareability box texts | — |
| `tagsLabelText`, `tagPlaceholderText` | `string` | `'Tags'`, `'Type a tag and press Enter'` | Tags box texts; the placeholder is also the input's accessible name | TagInput `placeholder`, `ariaLabel` |
| `tagMatchAnyText`, `tagMatchAllText` | `string` | `'Any'`, `'All'` | The match-mode buttons, for both lists | — |
| `bibleReferencesLabelText`, `bibleReferencePlaceholderText` | `string` | `'Bible references'`, `'Type a bible reference and press Enter (e.g. John 3:16)'` | Bible references box texts | TagInput `placeholder`, `ariaLabel` |
| `approvalStatusLabelText` | `string` | `'Approval status'` | The checkbox group's legend and name | — |

Rule 2.23 requires a property that opens the advanced section expanded; none exists yet (section
10, item 7).

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onSearch` | `ContentItemSearchCriteria` — all ten members | Search is pressed or Enter is hit in the query box (rule 2.2), or a status box changes (rule 2.16) |

### 4.3 Pass-through properties

Per §UI20.6.5. The parent drives this bar as `UI/Components/ContentItemListPanel.md §4.3` tabulates: `criteria`, `onSearch`, `showApprovalStatusSearchOptions`, the four flags, `categoryLabelText` and `anyCategoryText` under the same names, and `contentItemSettingCollection`, `placeholderText`, `authorLabelText`, `authorPlaceholderText` and `approvalStatusLabelText` from renamed parent properties — renames the parent records as exceptions, with the reason for each. The parent cannot reach the bar's other ten texts: `UI/Components/ContentItemListPanel.md §10 item 1`. Nor can it say whether the advanced section opens expanded (rule 2.23): the bar has no property for it (section 10, item 7), and the parent none to forward (`UI/Components/ContentItemListPanel.md §10 item 12`).

The bar passes `placeholderText` to SearchBarComponent, and each tag or reference placeholder to its TagInput, unchanged.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| *Search*, or Enter in the query box (`onSearch`) | Every persona (rule 3.2.1) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onSearch` | Nothing of its own: the page's read decides which rows arrive (`UI/Components/ContentItemListPanel.md rule 5.2`) |
| The advanced options' chevron | Every persona (rule 3.1.1) | None — no request | ✅ Allowed | Offered; toggles in place | Nothing — no request |
| A pill added or removed in the Tags or Bible references box, and an Any/All choice | Every persona (rule 2.12) | None — a draft, no request | ✅ Allowed | Offered; changes the draft | Nothing — no request |
| An approval-status box (`onSearch`) | Every persona, where `showApprovalStatusSearchOptions` is on (rule 3.1.2) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onSearch` | Nothing of its own: the boxes cannot widen the list past the foundation's filter (`UI/Components/ContentItemListPanel.md rule 5.3`) |

Inherits `UI/Components/ContentItemListPanel.md §5`; nothing to add.

## 6. Composition and Usage

Rendered by ContentItemListPanel above the results while its `showSearchBar` is on (`UI/Components/ContentItemListPanel.md rule 2.20`). No page imports it directly. *(code: grep of `src/` for `<ContentItemSearchBarPanel`)*

```
ContentItemSearchBarPanel
├── SearchBarComponent   src/components/coreUI/searchBar.tsx — the query box, Search, the chevron
└── TagInput × 2         src/components/coreUI/tagInput.tsx — the Tags and Bible references pills
```

## 7. Dependencies

- **Components:** SearchBarComponent and TagInput (section 6).
- **Data the consumer supplies:** the default setting rows, through the parent's `categorySettingCollection` (`UI/Components/ContentItemListPanel.md §7`).
- **Shared rules:** `resolveContentItemSearchApprovalStatuses` seeds the boxes. The consumer's search hook uses the same function to build its request, so the boxes and the read cannot disagree. *(code: contentItemSearchItem.ts — `resolveContentItemSearchApprovalStatuses`; code: services/foundations/contentItemService.ts — `useSearchContentItems`)*
- **API endpoints:** none.

## 8. States, Validation and Feedback

- **Drafts versus committed:** what is typed in a box is a draft until Search. A status tick is the one exception (rule 2.16).
- **Validation:** none. Every box accepts what is typed, and the consumer decides what a criterion means.
- **Feedback:** the only signal is `onSearch`; the parent's results show the outcome.
- **Loading:** none. The bar has no loading state; the results' are the parent's children's
  (`UI/Components/ContentItemListPanel.ContentItemResultsPanel.md §8`).

## 9. Styling and Accessibility

- The root is `div.g2h-content-item-search-bar`. The advanced options are a Bootstrap `row g-3` grid: Category and Author, then Submitted by and Shareability, half width from `sm` up; Tags, Bible references and Approval status full width. *(code: contentItemSearchBarPanel.tsx)*
- The query box is `type="search"`, named by the placeholder. The chevron carries `aria-expanded`, `aria-controls` and the name "Advanced search options", which is not a property yet (section 10, item 6). *(code: coreUI/searchBar.tsx)*
- Every advanced box has a `<label for>` tied to a `useId` id. Each match-mode pair is a `role="group"` named "{label} match mode", and its buttons carry `aria-pressed`. *(code: contentItemSearchBarPanel.tsx)*
- The status boxes sit in a `<fieldset>` whose legend is `approvalStatusLabelText`, inside a `role="group"` with the same name. *(code: contentItemSearchBarPanel.tsx)*

## 10. Open Questions and Gaps

1. The parent cannot reach ten of this bar's texts: recorded at `UI/Components/ContentItemListPanel.md §10 item 1`.
2. (needs issue) SearchBarComponent gives the fold-out the fixed id `advancedSearchOptions`, on the stated assumption that "a page carries one search bar". The ContentItemListPanel sample page renders two bars, so the id repeats and `aria-controls` is ambiguous (test: componentDocs.test.tsx — "should offer the full advanced options", whose comment notes the two bars). *(code: coreUI/searchBar.tsx — `advancedPanelId`)*
3. (needs issue) A comment in `contentItemSearchBarPanel.tsx`, above the status group, says "nothing ticked is every status, not none". The behaviour — the model comment on `approvalStatuses` and the tests under rule 2.19 — is that nothing ticked means the surface's defaults. The comment is stale.
4. (needs issue) The doc page (`contentItemSearchBarPanelDoc.tsx`) needs updating; this document follows the component. Its summary ends "and the removable filter chips", but the bar renders no chip row: a clicked criterion lands in its box (rule 2.3), and the list test's comment says there is "no separate chip row to keep in sync any more". Its props table also omits all sixteen text properties in section 4.1.
5. **Note — `SearchBar`, ruled.** §UI20.6 planned `SearchBar` — "Search input with debounce". This bar builds on SearchBarComponent (`coreUI/searchBar.tsx`), which does not debounce: it commits on submit (rule 2.2). This item asked whether SearchBarComponent is the planned `SearchBar`, and whether debouncing is still wanted. The user ruled on 2026-09-27 that the planned catalogue entries built under other names are superseded, each pointing at the component actually built; §UI20.6 now marks `SearchBar` superseded by this bar, which commits on Search rather than debouncing.
6. (needs issue) **Visible strings that are not properties.** Rule 3.1.6 (§UI20.6.6 rule 1). The bar renders three sets of strings no property sets: the Shareability options' labels, the contribution picker's (`contentItemSearchBarPanel.tsx` — `shareabilityBasisLabels`, line 354 at 70dc72e7); the approval-status boxes' labels, *Draft*, *Submitted*, *Approved* and *Rejected* (`approvalStatusRibbonLabels`, line 483); and the *Search* button, which `SearchBarComponent` renders with fixed text and no property to set it (`coreUI/searchBar.tsx`, line 48). §UI20.6.6 rule 1 covers the strings written for a screen reader alone too (user ruling 2026-09-27), and these have no property either: the chevron's name and tooltip, "Advanced search options" (`coreUI/searchBar.tsx`, lines 56-57); the " match mode" the bar appends to each match-mode pair's name (`contentItemSearchBarPanel.tsx`, lines 370 and 415); and each Tags or Bible references pill's remove button, named *Remove {tag}* with the tooltip *Remove tag*, which `TagInput` renders with fixed text (`coreUI/tagInput.tsx`, lines 71-72).
7. (needs issue) **The page cannot open the advanced section.** Rule 2.23 (user rulings
   2026-09-27) has the page open the bar with its advanced section expanded, so a criterion it
   hands in stands where the reader sees it. The bar has no property for it: `SearchBarComponent`
   keeps whether the section is open in its own state, starting folded, and takes no property to
   start it open (`coreUI/searchBar.tsx` — `isAdvancedOpen`, line 31 at 70dc72e7), and the bar
   declares none to pass it (`contentItemSearchBarPanel.tsx` — `ContentItemSearchBarPanelProps`).
   The criterion half already works: `criteria` seeds the boxes (rule 2.3). The parent's half is
   `UI/Components/ContentItemListPanel.md §10 item 12`.
8. (needs issue) **The bar raises nothing as its boxes change.** `UI/Pages/Posts.md rule 2.21`
   and `UI/Pages/ContentItemModerationPage.md rule 2.12` (user rulings 2026-09-27) keep the
   search in the query string and have it follow the form as the reader changes it, without a
   reload, not only when the search is committed. The page can do that only if it hears each
   change. The bar raises one event, `onSearch` (`contentItemSearchBarPanel.tsx` —
   `ContentItemSearchBarPanelProps`, line 43 at 70dc72e7), and raises it on Search, on Enter in
   the query box and on an approval-status box (`search` and `approvalStatusToggled`, lines 238
   and 249). A typed box, the Category and Shareability selects, and the Tags and Bible
   references pills only move the bar's own drafts and raise nothing (`onCategoryChanged` and
   `onShareabilityChanged`, lines 252-260; `setDraftAuthor`, line 313). The parent's half, the
   forwarding of such a hook (§UI20.6.5), is `UI/Components/ContentItemListPanel.md §10 item 13`.
