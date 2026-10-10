# 1. ContentItemListPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** [ContentItemListPanel.ContentItemSearchBarPanel.md](ContentItemListPanel.ContentItemSearchBarPanel.md), [ContentItemListPanel.ContentItemResultsPanel.md](ContentItemListPanel.ContentItemResultsPanel.md)
- **Composes:** [ContentItemPanel.md](ContentItemPanel.md) — once per element, rendered by ContentItemResultsPanel
- **Used by:** no other component document renders it. Pages: `/` (`src/pages/home.tsx`), `/posts` (`src/pages/posts.tsx`), `/myposts` (`src/pages/myPosts.tsx`), `/Admin/Posts` (`src/pages/admin/contentItemModerationPage.tsx`) — see section 6
- **Inherits:** §UI20.3 principle 2, §UI20.6.4, §UI20.6.5, §UI20.6.6, §SEC14.5 rule 4, §SEC14.6, §SEC14.7 posture A rule 4, §SEC18.6 rule 2, §DOM6.4; every card rule in `UI/Components/ContentItemPanel.md §3` and in `UI/Components/ContentItemPanel.md §5`
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemListPanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-List-Panel` — `src/pages/samplePages/components/contentItemListPanelDoc.tsx`

ContentItemListPanel shows many content items, searched and scrolled. It puts the
search bar (ContentItemSearchBarPanel) above the results (ContentItemResultsPanel),
and the results render each item through ContentItemPanel. One family serves the
public feed, the caller's own posts and the moderation queue.

A click on a card's type chip, *Submitted by*, *Author*, tag or Bible reference
raises the page's hook and nothing else (rules 2.8–2.13): the list runs no search
of its own on it, and where the click leads is the page's. For the page to take
such a value to a search, this panel hands its search bar the committed criteria
and whether the bar's advanced section opens expanded (rule 2.24). Everything
else — navigation, reactions and the per-surface switches — passes through to the
cards and to the page.

## 2. Business Rules

**2.1 [Must]** The panel is presentation only: props in, events out, with no fetching, no mutation and no sockets. *(§UI20.3 principle 2; code: contentItemListPanel.tsx — header comment)*

**2.2 [Must]** The panel does not know what read is behind the collection, and must not know. The same family serves the public feed, "my posts" and the moderation queue, and the server decides what each caller may see against the stored row. *(code: contentItemListPanel.tsx — header comment)*

**2.3 [Must]** This panel is the composer `UI/Components/ContentItemPanel.md rule 2.4` describes. *(`UI/Components/ContentItemPanel.md rule 2.4`; code: contentItemListPanel.tsx — `ContentItemListPanel`; code: contentItemResultsPanel.tsx — `ContentItemResultsPanel`)*

**2.4 [Must]** `contentItemCollection` holds the accumulated results. The consumer's infinite query keeps the pages, and the family appends nothing of its own. *(code: contentItemListPanel.tsx — `contentItemCollection`; code: contentItemResultsPanel.tsx — header comment)*

**2.5 [Must]** Each element is the self-contained element of `UI/Components/ContentItemPanel.md rule 2.5`: a card consults nothing beyond it, and updating one item never refetches the list. *(code: contentItemListPanel.tsx — `contentItemCollection`; code: contentItemSearchItem.ts — `ContentItemSearchItem`)*

**2.6 [Must]** `categorySettingCollection` feeds the search bar's Category box alone. The cards never read it. *(code: contentItemListPanel.tsx — `categorySettingCollection`)*

**2.7 [Won't]** The panel does not hand its cards a settings collection, so a listed card can never fall into ContentItemPanel's add face. *(code: contentItemPanel.tsx — `contentItem` and `contentItemSettingCollection` prop comments)* See section 4.3.

**2.8 [Must]** A click on a card's type chip, *Submitted by* or *Author* raises the page's hook of the same name — `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` — and nothing else: the panel rewrites no criteria, raises no `onSearch` and performs no navigation of its own. Where the reader goes is the page's (§UI20.6.4). *(user, 2026-09-27)* ≠ item 11

**2.9 [Must]** The type chip's hook carries the element, and with it the item's content type, which the page may hand to its search's Category box. *(user, 2026-09-27; code: contentItemTemplate.ts — `ContentItemEvents`)* ≠ item 11

**2.10 [Must]** The *Submitted by* segment's hook carries the element, and with it the submitter's account id and name together, since two accounts can share a name (rule 5.4); the page may hand them to its search's Submitted by box. *(user, 2026-09-27; code: contentItemSearchItem.ts — `submittedById`)* ≠ item 11

**2.11 [Must]** The *Author* segment's hook carries the element, and with it the item's author, which the page may hand to its search's Author box. *(user, 2026-09-27; code: contentItemTemplate.ts — `ContentItemEvents`)* ≠ item 11

**2.12 [Must]** A tag pill's click raises the page's `onTagClick` and nothing else: the panel performs no filtering and no navigation of its own. Where the reader goes — typically the search page filtered by the tag — is the page's, and differs between a user section and an admin section (§UI20.6.4). *(user, 2026-09-27)* ≠ item 8

**2.13 [Must]** A Bible reference pill's click raises the page's `onBibleReferenceClick` and nothing else, as rule 2.12 says of a tag. Where the reader goes — typically a page showing the verse — is the page's (§UI20.6.4). *(user, 2026-09-27)* ≠ item 8

**2.14 [Must]** The five click hooks of rules 2.8–2.13 reach the page unchanged, not wrapped: this panel adds no behaviour of its own to any of them. *(user, 2026-09-27; §UI20.6.4)* ≠ items 8 and 11

**2.15 [Must]** The navigation hooks pass straight through to the page: title, read more, comments, View, Edit, moderate, share, save, expand/collapse and reaction choice, and — by rules 2.12 and 2.13 — the tag and Bible reference clicks, and by rule 2.8 the type chip, *Submitted by* and *Author* clicks. Where each one leads is the page's decision, and the page puts the origin into router state so the destination can offer a true way back. *(code: contentItemListPanel.tsx — header comment; code: contentItemFeedNavigation.ts — `buildContentItemFeedNavigation`; user, 2026-09-27)* ≠ items 8, 9 and 11

**2.16 [Must]** View and Edit are two actions with two hooks on a listed card, as on the card itself (`UI/Components/ContentItemPanel.md rule 2.40`). This panel's properties decide which of them its cards offer, each through a switch of its own, and it never names one action with the other's label. Where each leads is the page's (§UI20.6.4). *(user, 2026-09-27)* ≠ item 9

**2.17 [Could]** `allowTitleClick` is on by default, the opposite of ContentItemPanel's default: a listed title leads to the detail surface. *(test: contentItemListPanel.test.tsx — "should let a listed title lead to the detail by default", "should stand every title as plain text when the list disallows the click")*

**2.18 [Must]** Per-surface decisions are made once, on this panel, and reach every card unchanged. They are `showModerationSection`, `showApprovalStatusRibbon`, `showApprovalStatus`, the six section switches, `showContentExpanded`, `truncateAt`, `allowInPlaceExpansion` and `reactionOptions`. ContentItemPanel owns what each one means. *(code: contentItemListPanel.tsx — `ContentItemListPanelProps` comment; test: contentItemListPanel.test.tsx — "should thread a section switch to every card", "should carry truncateAt from the list to every card")*

**2.19 [Won't]** ContentItemPanel's form-face properties (`showEditSection`, `onModified`, `validationIssues` and the rest) are not carried. They describe one item's write lifecycle, which no single list-level value can express, and a list row's edit is a navigation (`onEditClick`). *(code: contentItemListPanel.tsx — `ContentItemListPanelProps` comment)* See section 4.3.

**2.20 [Could]** `showSearchBar` is on by default. Turned off, the panel renders the results alone, which suits a surface that has already decided what it shows. *(code: contentItemListPanel.tsx — `showSearchBar`; test: contentItemListPanel.test.tsx — "should leave the list alone when the bar is switched off")*

**2.21 [Should]** Whether the bar offers the approval-status checkboxes, and which of them start ticked, are per-surface decisions this panel passes to the bar. The checkboxes are off by default. The public feed shows one status and has nothing to offer a reader here; "my posts" and the moderation queue are where a status is what the reader filters on. *(code: contentItemListPanel.tsx — `showApprovalStatusSearchOptions`; test: contentItemListPanel.test.tsx — "should leave the approval statuses out of the advanced options by default"; test: contentItemFeedPages.test.tsx — "should leave the approval statuses out of the search options")*

**2.22 [Must]** `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.21` applies to the four default-selection flags this panel passes to the bar. *(`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.21`)*

**2.23 [Won't]** The family renders no approval controls: the approval round is ReviewPanel's (§UI20.6, the `ReviewPanel` row). *(code: contentItemListPanel.tsx, contentItemResultsPanel.tsx — neither declares nor renders an approval action)*

**2.24 [Must]** A page can open the list's search with a criterion already in its box and the bar's advanced section expanded, so a value a card's hook handed on shows where the reader can see and remove it. This panel hands the bar the committed criteria (rule 3.1.4) and whether its advanced section opens expanded, each unchanged (§UI20.6.5; `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.23`). *(user, 2026-09-27)* ≠ item 12

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** When `titleText` is non-empty, the panel renders it as an `h2` heading, and the heading names the section. Otherwise the section is named by `ariaLabel`, which defaults to `Content items`. *(code: contentItemListPanel.tsx — the `<section>` element)*

**3.1.2** `showSearchBar=true` renders the search bar above the results. `false` renders no bar, and the results still render. *(test: contentItemListPanel.test.tsx — "should leave the list alone when the bar is switched off")*

**3.1.3** The type chip, *Submitted by* and *Author* clicks raise the page's hooks whether or not the bar is shown; neither `showSearchBar` nor `onSearch` has any bearing on them (rule 2.8). *(user, 2026-09-27)* ≠ item 11

**3.1.4** An absent `criteria` is treated as the empty criteria, and the bar is seeded from it. *(code: contentItemListPanel.tsx — `committedCriteria`)*

**3.1.5** `showApprovalStatusSearchOptions` and the four `searchApproval*Selected` flags affect only the bar. What each draws is at `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §3.1`. *(code: contentItemListPanel.tsx — the `<ContentItemSearchBarPanel>` element)*

**3.1.6** `isLoading`, `isLoadingMore`, `hasMore` and the four loading and empty texts affect only the results. What each draws is at `UI/Components/ContentItemListPanel.ContentItemResultsPanel.md §3.1`. *(code: contentItemListPanel.tsx — the `<ContentItemResultsPanel>` element)*

**3.1.7** `allowTitleClick`, `editButtonText`, `showModerationSection`, `showApprovalStatusRibbon`, `showApprovalStatus`, the six section switches and the content-length trio change what every card shows, as `UI/Components/ContentItemPanel.md §3` states. *(rule 2.18)*

**3.1.8** Every visible string this panel renders itself — its heading, and the texts it hands its two children — and every string it renders for a screen reader alone, its section's name, is a property whose default is today's text (§UI20.6.6 rule 1). The card's strings are `UI/Components/ContentItemPanel.md §10 item 21` and `UI/Components/ContentItemPanel.Default.md §10 item 2`. *(code: contentItemListPanel.tsx — `ContentItemListPanelProps`)*

### 3.2 Driven by roles

**3.2.1** The panel has no role gate of its own. The search bar, the results, the loading and empty states and the cards' pills render the same for every persona. *(code: contentItemListPanel.tsx, contentItemSearchBarPanel.tsx, contentItemResultsPanel.tsx — none reads the auth context)*

**3.2.2** Every role-driven affordance on a card — Edit, Moderate and choosing a reaction — is decided by ContentItemPanel from the signed-in identity (`UI/Components/ContentItemPanel.md §3`). This panel only sets the properties that feed those decisions: `showModerationSection`, and whether `onEditClick` and `onModerateClick` are wired. *(code: contentItemPanel.tsx — `showsEditButton`, `showsModerateButton`)*

### 3.3 Combinations

**3.3.1** A control whose hook the page did not wire does not render, whatever the roles. With no `onEditClick` there is no Edit, even for the owner; with no `onModerateClick` there is no Moderate, even for an administrator. `/Admin/Posts` wires no `onEditClick`. *(code: contentItemPanel.tsx — `showsEditButton`, `showsModerateButton`; code: pages/admin/contentItemModerationPage.tsx)*

**3.3.2** `showModerationSection=true` only subtracts and relabels. It removes Edit from every card, the owner's included, and Moderate then stands alone, wearing Edit's pencil and the label "Edit". The moderation-tier roles still decide who sees it. *(code: contentItemPanel.tsx — `showsEditButton`, `moderateButtonLabel`; test: contentItemFeedPages.test.tsx — "should keep Edit inside the admin area rather than the public post route")*

**3.3.3** A `ReadOnly` role whose scope covers the item's type outranks every grant, the owner's included. It removes both Edit and Moderate (§SEC18.6 rule 2). *(code: contentItemPanel.tsx — `isBlocked`)*

**3.3.4** On a list surface Moderate is never greyed out as locked, whatever the item's status. The lock applies only where a surface declares that its moderation action opens an editor, and this panel cannot declare that (section 10 item 2). Every list surface's Moderate is a route. *(code: contentItemPanel.tsx — `isModerateButtonLocked`; test: contentItemFeedPages.test.tsx — "should send a moderator to the admin address on a row they did not contribute")*

**3.3.5** `allowTitleClick=false` outranks a wired `onTitleClick`: every title stands as plain heading text. *(test: contentItemListPanel.test.tsx — "should stand every title as plain text when the list disallows the click")*

### 3.4 Role matrix

Owner here is the item's contributor: the signed-in account id equals the element's `submittedById`. The cells show what the panel renders. Which personas reach a given page is the route's decision (section 6).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| `showSearchBar=true` — **search bar** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showApprovalStatusSearchOptions=true` — **approval status checkboxes** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The type chip, *Submitted by* or *Author* clicked — **the page's hook, and nothing else** ≠ item 11 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showModerationSection=false`, `onEditClick` wired, no ReadOnly — **Edit**, the owner's, labelled *View* today ≠ item 9 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| `showModerationSection=false`, `onModerateClick` wired, no ReadOnly — **Moderate** (shield) | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes | ✅ Yes |
| `showModerationSection=false`, both wired, the viewer owns the item **and** holds the column's tier, no ReadOnly — **Edit and Moderate side by side** | ➖ n/a | ➖ n/a | ➖ n/a | ✅ Yes¹ | ✅ Yes | ✅ Yes |
| `showModerationSection=true`, `onEditClick` wired — **Edit** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationSection=true`, `onModerateClick` wired, no ReadOnly — **Edit** (Moderate wearing the pencil) | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes | ✅ Yes |
| Any hook wired, the viewer holds a ReadOnly covering the item's type — **Edit** or **Moderate** | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| The list's View switch on and View's hook wired, any read-only role or none — **View** ≠ item 9 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Approved or Rejected item the viewer did not contribute, `onModerateClick` wired — **Moderate is live, not locked** | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes | ✅ Yes |
| `reactionOptions` non-empty, `onReactionSelected` wired, the element's setting allows reactions — **Like and its choices** ≠ `UI/Components/ContentItemPanel.md §10 item 12` | ✅ Yes² | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

¹ The moderation tier includes the review tier at every §SEC18.6 scope (`UI/Components/ContentItemPanel.md rule 2.19`), although the review tier may not amend content (§SEC14.7 posture A rule 3). What Moderate leads to is the page's decision (`UI/Pages/ContentItemModerationDetailPage.md §6 item 1`). *(code: contentItemPanel.tsx — `viewerModerates`)*
² A signed-out reader who picks a choice raises `onReactionSelected` like any reader, and the page sends them to sign in (`UI/Components/ContentItemPanel.md rule 3.2.4`; user, 2026-09-27).

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItemCollection` | `ReadonlyArray<ContentItemSearchItem>` | `[]` | The accumulated, self-contained elements (rules 2.4, 2.5) | Results → one element per ContentItemPanel `contentItem` |
| `categorySettingCollection` | `ReadonlyArray<ContentItemSetting>` | `[]` | The Category box's rows (rule 2.6) | Bar `contentItemSettingCollection` |
| `showSearchBar` | `boolean` | `true` | Whether the bar renders (rule 2.20) | — |
| `criteria` | `ContentItemSearchCriteria` | empty criteria | The committed search | Bar `criteria` |
| `onSearch` | `(criteria) => void` | — | The one search signal | Bar `onSearch`. Today the card clicks raise it too (section 10, items 8 and 11) |
| `showApprovalStatusSearchOptions` | `boolean` | `false` | Status checkboxes in the bar (rule 2.21) | Bar, same name |
| `searchApprovalDraftSelected`, `…SubmittedSelected`, `…ApprovedSelected`, `…RejectedSelected` | `boolean` | unset — the bar's defaults apply | Which checkboxes start ticked | Bar, same names |
| `isLoading`, `isLoadingMore`, `hasMore` | `boolean` | `false` | Paging state | Results, same names |
| `onLoadMore` | `() => void` | — | Asks for the next page | Results, same name |
| `reactionOptions` | `ReadonlyArray<ContentItemReactionOption>` | `[]` | The choices behind every card's Like | Results → ContentItemPanel, same name |
| `showModerationSection` | `boolean` | `false` | Marks a moderated surface | Results → ContentItemPanel, same name |
| `allowTitleClick` | `boolean` | `true` | Titles lead to the detail (rule 2.17) | Results → ContentItemPanel, same name |
| `showApprovalStatusRibbon`, `showApprovalStatus` | `boolean` | `false` | The status ribbon and the status pill | Results → ContentItemPanel, same names |
| `showContentExpanded`, `truncateAt`, `allowInPlaceExpansion` | `boolean`, `number`, `boolean` | unset — ContentItemPanel's defaults apply | The content-length trio | Results → ContentItemPanel, same names |
| `showTagSection`, `showBibleReferenceSection`, `showReactionSection`, `showCommentsSection`, `showShareSection`, `showSaveSection` | `boolean` | unset — ContentItemPanel's defaults apply | The six section switches | Results → ContentItemPanel, same names |
| `editButtonText` | `string` | `'View'` | The card's Edit label; the `'View'` default relabels Edit (rule 2.16; section 10, item 9) | Results → ContentItemPanel, same name |
| Every other `ContentItemText` member (`submittedByLabelText`, `authorLabelText`, `shareabilityLabelText`, `dateLabelText`, `likeButtonText`, `commentsText`, `commentsNoCountText`, `shareButtonText`, `saveButtonText`, `readMoreText`, `expandLinkText`, `showLessText`, `allReactionsText`, `shareabilityBasisLabels`) | `string` or label map | unset — the card's defaults apply | Card text | Results → ContentItemPanel, same names |
| `cssClass` | `string` | `''` | Extra classes on the section | — |
| `ariaLabel` | `string` | `'Content items'` | The section's name when there is no title | — |
| `titleText` | `string` | `''` | An optional `h2` heading that names the section | — |
| `searchPlaceholderText` | `string` | `'Search posts, authors and topics'` | The query box's placeholder and name | Bar `placeholderText` |
| `categoryLabelText`, `anyCategoryText` | `string` | `'Category'`, `'Any category'` | The Category box's label and its "any" option | Bar, same names |
| `searchAuthorLabelText`, `searchAuthorPlaceholderText` | `string` | `'Author'`, `'Any author'` | The bar's Author box | Bar `authorLabelText`, `authorPlaceholderText` |
| `approvalStatusSearchLabelText` | `string` | `'Approval status'` | The checkbox group's legend | Bar `approvalStatusLabelText` |
| `loadingText`, `loadingMoreText`, `loadMoreButtonText`, `emptyText` | `string` | `'Loading…'`, `'Loading more…'`, `'Load more'`, `'Nothing matched that search.'` | Loading and empty texts | Results, same names |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onSearch` | `ContentItemSearchCriteria` | The bar commits (Search, or a status tick). Today a card's type chip, *Submitted by*, *Author*, tag or reference click raises it too (section 10, items 8 and 11) |
| `onLoadMore` | none | The results panel reaches its foot or its fallback button is pressed |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick` | the element | Passed through unchanged from the card (rules 2.8 and 2.14); today after this panel's rewrite of the criteria (section 10, item 11) |
| `onTagClick`, `onBibleReferenceClick` | the element, the tag or reference | Passed through unchanged from the card (rules 2.12–2.14); today after this panel's rewrite of the criteria (section 10, item 8) |
| `onTitleClick`, `onReadMore`, `onCommentsClick`, `onExpandCollapse`, `onEditClick`, `onModerateClick`, `onShareClick`, `onSaveClick` | the element | Passed through unchanged from the card (rule 2.15) |
| `onReactionSelected` | the element, the chosen `ContentItemReactionOption` | Passed through unchanged; the consumer persists and hands back a refreshed collection |

### 4.3 Pass-through properties

Per §UI20.6.5. The panel hands its children the properties that configure them.

| This panel's property | Reaches | As | Unchanged? |
| --- | --- | --- | --- |
| `criteria` | ContentItemSearchBarPanel | `criteria` | Yes; an absent value arrives as the empty criteria (rule 3.1.4) |
| `onSearch` | ContentItemSearchBarPanel | `onSearch` | Yes |
| `showApprovalStatusSearchOptions`, the four `searchApproval*Selected` | ContentItemSearchBarPanel | same names | Yes |
| `categorySettingCollection` | ContentItemSearchBarPanel | `contentItemSettingCollection` | Value unchanged, name changed |
| `searchPlaceholderText`, `searchAuthorLabelText`, `searchAuthorPlaceholderText`, `approvalStatusSearchLabelText` | ContentItemSearchBarPanel | `placeholderText`, `authorLabelText`, `authorPlaceholderText`, `approvalStatusLabelText` | Value unchanged, name changed |
| `categoryLabelText`, `anyCategoryText` | ContentItemSearchBarPanel | same names | Yes |
| `contentItemCollection`, `isLoading`, `isLoadingMore`, `hasMore`, `onLoadMore`, the four loading and empty texts | ContentItemResultsPanel | same names | Yes |
| `reactionOptions`, `showModerationSection`, `allowTitleClick`, `showApprovalStatusRibbon`, `showApprovalStatus`, `editButtonText` | ContentItemResultsPanel → ContentItemPanel | same names | Yes; `editButtonText` defaults to View at this level |
| `showContentExpanded`, `truncateAt`, `allowInPlaceExpansion`, the six section switches, every `ContentItemText` member, `onTitleClick`, `onReadMore`, `onCommentsClick`, `onExpandCollapse`, `onEditClick`, `onModerateClick`, `onShareClick`, `onSaveClick`, `onReactionSelected` | ContentItemResultsPanel → ContentItemPanel | same names | Yes, forwarded by spread at both levels |
| `onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick`, `onTagClick`, `onBibleReferenceClick` | ContentItemResultsPanel → ContentItemPanel | same names | Required unchanged (rule 2.14). **Not today** — wrapped: this panel rewrites the criteria, then calls the page's hook (section 10, items 8 and 11) |

What a page **cannot** control through this panel today:

| Child property | Child | Gap |
| --- | --- | --- |
| `submittedByLabelText`, `submittedByPlaceholderText`, `shareabilityLabelText`, `anyShareabilityText`, `tagsLabelText`, `tagPlaceholderText`, `tagMatchAnyText`, `tagMatchAllText`, `bibleReferencesLabelText`, `bibleReferencePlaceholderText` | ContentItemSearchBarPanel | section 10 item 1 |
| `moderationOpensEditor` | ContentItemPanel | section 10 item 2 |
| The view-face role sets | ContentItemPanel | section 10 item 3 |
| Whether the advanced section opens expanded — the bar has no such property yet | ContentItemSearchBarPanel | section 10 item 12 |

The exceptions this panel records (§UI20.6.5):

| Property | Exception | Why |
| --- | --- | --- |
| `categorySettingCollection` | Renamed: reaches the bar as `contentItemSettingCollection` | It feeds the Category box alone, and the cards never read it (rule 2.6). *(code: contentItemListPanel.tsx — the `categorySettingCollection` comment)* |
| `searchAuthorLabelText`, `searchAuthorPlaceholderText` | Renamed: reach the bar as `authorLabelText`, `authorPlaceholderText` | The names collide: this panel's own `authorLabelText` is the `ContentItemText` member it hands the cards (section 4.1). |
| `searchPlaceholderText`, `approvalStatusSearchLabelText` | Renamed: reach the bar as `placeholderText`, `approvalStatusLabelText` | Named for the search bar at this level, as the Author pair is, beside the card texts this panel also carries. No collision forces these two, and the code gives no further reason. |
| ContentItemPanel's `showEditSection`, `mode`, `onModified`, `onRemoved`, `onCancelled`, `isLoading` (the form's), `isSubmitting`, `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`, `ariaLabel`, `titleText`, `showBorder` | Withheld from every card | Rule 2.19: they describe one item's write lifecycle, which no single list-level value can express, and a list row's edit is a navigation (`onEditClick`). *(code: contentItemListPanel.tsx — `ContentItemListPanelProps` comment: "deliberately NOT here")* This panel's own `isLoading`, `ariaLabel` and `titleText` mean something else. |
| ContentItemPanel's `contentItemSettingCollection`, `onAdded` | Withheld from every card | Rule 2.7: a listed card can never fall into the add face. *(code: contentItemPanel.tsx — the `contentItemSettingCollection` comment: "ContentItemListPanel never populates this prop")* |

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The search bar: *Search*, its advanced options and its approval-status boxes (`onSearch`) | Every persona (rule 3.2.1) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onSearch` | Nothing of its own: the page's read decides which rows arrive (rule 5.2) |
| The type chip, *Submitted by* and *Author* clicks (`onContentTypeClick`, `onSubmittedByClick`, `onAuthorClick`) ≠ item 11 | Every persona (rules 2.8–2.11) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises the page's hook | Nothing of its own, as the search bar |
| The tag and Bible reference clicks (`onTagClick`, `onBibleReferenceClick`) ≠ item 8 | Every persona (rules 2.12 and 2.13) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises the page's hook | Nothing of its own, as the search bar |
| **View** ≠ item 9 | Every persona, where this panel's View switch is on (rule 2.16) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises View's hook | Nothing of its own, as the search bar |
| *Load more* and the infinite scroll (`onLoadMore`) | Every persona (`UI/Components/ContentItemListPanel.ContentItemResultsPanel.md rules 2.4 and 2.9`) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onLoadMore` | Nothing of its own, as the search bar |
| Every other card action — Edit, Moderate and Like among them | As `UI/Components/ContentItemPanel.md §5` | As there | As there | As there | As there |

**5.1** Every gate a card renders is a courtesy to the reader, not an authorization boundary. The services re-decide every write against the stored row (§SEC14.6). *(code: contentItemTemplate.ts — `onEditClick` comment)*

**5.2** The panel never filters and never decides visibility. Which rows arrive is decided by the read the page chose and the foundation's collection filter: rows the caller may not see drop out of the set (§SEC14.5 rule 4, §SEC14.7 posture A rule 4). *(code: contentItemListPanel.tsx — header comment)*

**5.3** The approval-status checkboxes name what the reader asks for within what the caller may see. They cannot widen the list past the foundation's filter, which is why they can be offered freely. *(code: contentItemSearchItem.ts — `approvalStatuses` comment)*

**5.4** A submitter's account id filters and is never rendered. The name renders. The two travel together on the element and on the submitted-by criterion, because two accounts can share a display name. *(code: contentItemSearchItem.ts — `submittedById`, `ContentItemSubmittedByCriterion`)*

**5.5** Identity reaches the cards through the auth context that ContentItemPanel reads, never through a property of this panel (`UI/Components/ContentItemPanel.md §5`). *(code: contentItemPanel.tsx — `useAuth`)*

## 6. Composition and Usage

```
ContentItemListPanel
├── ContentItemSearchBarPanel     the search bar and its advanced options
└── ContentItemResultsPanel       the results, scrolled
    └── ContentItemPanel          one element; see ContentItemPanel.md
```

`UI/Components/ContentItemPanel.md §6.4` records that the feeds render every element through ContentItemPanel via this panel. That is verified against the four pages below. *(code: the four page files)*

| Route | Page | Route gate | How the page configures the panel |
| --- | --- | --- | --- |
| `/` | `src/pages/home.tsx` | none | Bar without status checkboxes; the feed read when nothing is narrowed, the public read otherwise; View → `/posts/{id}`, Moderate → `/Admin/Posts/{id}`; titles, read more and comments → `/posts/{id}` |
| `/posts` | `src/pages/posts.tsx` | none | As `/`, over the caller-scoped read |
| `/myposts` | `src/pages/myPosts.tsx` | `SecuredRoute`, any signed-in reader | Status checkboxes on with all four ticked; ribbon on, pill off; read pinned to the signed-in account; View, titles, read more and comments → `/myposts/{id}`; Moderate → `/Admin/Posts/{id}` |
| `/Admin/Posts` | `src/pages/admin/contentItemModerationPage.tsx` | `Administrators` (`securityMatrix.tsx` — `contentItems.view`) | `showModerationSection`, `showApprovalStatus`, status checkboxes on with all four ticked; no `onEditClick`; every way into an item → `/Admin/Posts/{id}`; rendered bare, with no card around it |
| `/SamplePages/Components/Content-Item-List-Panel` | `src/pages/samplePages/components/contentItemListPanelDoc.tsx` | `Administrators` | Reference page and playground |

Each page renders an error alert in place of the panel when its read fails (section 8). *(code: the four page files; test: contentItemFeedPages.test.tsx — "should render the panel bare, without a card around the cards")*

## 7. Dependencies

**Data the consumer supplies:** the elements, projected from the wire entity by `toContentItemSearchItem` with each item's winning setting; the default setting rows for the Category box; the reaction choices; the committed criteria, which every page keeps in the URL. *(code: services/views/contentItems/toContentItemSearchItem.ts; code: services/views/contentItems/contentItemSearchCriteriaUrl.ts)*

**API endpoints the consumer calls** (never the component):

| Concern | Endpoint |
| --- | --- |
| The front page with nothing narrowed | `GET api/ContentItems/Feed` — the §DOM11.3 order, feed membership per §SEC14.2 |
| The front page once narrowed | `GET api/ContentItems/Public` with an OData `$filter` — the §SEC14.1 canonical set |
| `/posts`, `/myposts`, `/Admin/Posts` | `GET api/ContentItems` with an OData `$filter` — caller-scoped; `/myposts` adds `createdBy` for the signed-in account |
| The Category rows and each card's winning setting | `GET api/ContentItemSettings` — the defaults (`contentItemId eq null`) and the override rows of the items on screen |
| The reaction choices | `GET api/Reactions` — approved rows only |

The paging behind `hasMore` is the consumer's. *(code: services/foundations/contentItemService.ts — `useSearchContentItems`, which writes the page from #946, `BrokersHoldNoLogic.md`; code: services/foundations/contentItemSettingService.ts — `useGetEffectiveSettingsFor`; code: hooks/useContentItemEngagement.ts)*

**Indirect dependencies:** the auth context and a router, both through ContentItemPanel. The panel must render under a router, because today a card sends a signed-out reader to sign in itself when they choose a reaction (`UI/Components/ContentItemPanel.md §10 item 12`). *(test: contentItemListPanel.test.tsx — the `render` helper comment)*

## 8. States, Validation and Feedback

- **Loading, loading more and empty:** drawn by the results panel (`UI/Components/ContentItemListPanel.ContentItemResultsPanel.md §8`), which announces both loading states.
- **Error:** the panel has no error state. Every page renders its own error alert instead of the panel when the read fails. *(code: the four page files)*
- **Validation:** none. The panel writes nothing.
- **Confirmation:** none.
- **Freshness:** the consumer owns it. A reaction choice is persisted by the consumer, which hands back a refreshed collection; the panel holds no optimistic state. When `criteria` changes — a pill click, the back button, a shared link — the bar reseeds from it. *(code: contentItemTemplate.ts — `onReactionSelected`; `UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.3`)*
- **Criteria no read acts on:** the tag and bible-reference criteria commit and reach the URL, but no page's read narrows on them (section 10, item 15). *(code: contentItemSearchItem.ts — `tags` comment; code: services/foundations/contentItemService.ts — `useSearchContentItems`)*

## 9. Styling and Accessibility

- The root is a `<section class="g2h-content-item-list-panel {cssClass}">`. The stylesheet removes the theme's section padding, because the panel sits inside a page column. *(code: contentItems.css — `section.g2h-content-item-list-panel`)*
- The optional heading is `h2.h5.mb-3`. The bar sits in an `mb-4` wrapper. *(code: contentItemListPanel.tsx)*
- The section's accessible name comes from the heading when `titleText` is set, and from `ariaLabel` otherwise (rule 3.1.1).
- Pages render the panel bare: each row is already a card, so a card around the panel doubles the border and padding. *(test: contentItemFeedPages.test.tsx — "should render the panel bare, without a card around the cards")*

## 10. Open Questions and Gaps

1. (needs issue) A page cannot set ten of the bar's texts through this panel: `submittedByLabelText`, `submittedByPlaceholderText`, `shareabilityLabelText`, `anyShareabilityText`, `tagsLabelText`, `tagPlaceholderText`, `tagMatchAnyText`, `tagMatchAllText`, `bibleReferencesLabelText` and `bibleReferencePlaceholderText`. Two of them collide with card texts: this panel's own `submittedByLabelText` and `shareabilityLabelText` come from `ContentItemText` and go to the cards. Evidence: `contentItemListPanel.tsx`, the `<ContentItemSearchBarPanel>` element, forwards fourteen properties and none of these. (The doc page's props table claims the opposite: item 6.)
2. (needs issue) A page cannot set ContentItemPanel's `moderationOpensEditor` through this panel. Neither this panel nor ContentItemResultsPanel declares it, so every card takes ContentItemPanel's default (`false`). Every list surface's Moderate is a route today, so the default is what each one relies on (rule 3.3.4).
3. (needs issue) A page cannot configure the role sets behind a card's Edit and Moderate, because ContentItemPanel declares none for its view face: they are composed inside it (`contentItemPanel.tsx` — `viewerModerates`). `UI/Components/ContentItemPanel.md rule 2.16` describes every set but the block set as an overridable property. The gap starts in ContentItemPanel (`UI/Components/ContentItemPanel.md §4.3`); this panel would then need to forward what it adds. The block set is not among them: the card composes its read-only roles itself, and no page supplies them (§UI20.6.6 rule 3; user ruling 2026-09-27; `UI/Components/ContentItemPanel.md §10 item 23`).
4. **Note — a Bible reference click, ruled.** This item asked which behaviour is intended: a bible-reference click both toggles the criterion and fires the page's `onBibleReferenceClick`, and on all four pages that hook navigates to the passage, so the reader leaves the list just after it commits a search. The user ruled on 2026-09-27 that a tag click and a Bible reference click raise hooks, and that the component performs no navigation and no filtering of its own; where the reader goes is the page's (§UI20.6.4). Rules 2.12, 2.13 and 2.15 now say so, and the panel's rewrite of the criteria is item 8. Navigating to the passage is the pages' own choice.
5. **Moved to the page documents.** Page gap — `/Admin/Posts/{id}` — now `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`.
6. (needs issue) The doc page (`contentItemListPanelDoc.tsx`) needs updating; this document follows the component. The page disagrees with the code in six places:
   - It says there is "deliberately no Tags box", but the bar renders a Tags box and a Bible references box (test: contentItemListPanel.test.tsx — "should offer the full advanced grid — the two people, the basis, the tags").
   - It says ContentItemVerseImagePanel is "blocked" because there is no `ContentType.VerseImage`, but `contentItemPanel.tsx` registers it in `templateOverrides`.
   - It says clicked criteria "wear removable chips", in the prose, the props table and the family tree. The list test's comment says there is "no separate chip row to keep in sync any more".
   - It says the moderation queue is "pinned to Draft + Submitted", but the page defaults to all four statuses (test: contentItemFeedPages.test.tsx — "should read every status where the moderator has chosen none"). The `Admin/Posts` route comment in `adminRoutes.tsx` repeats "Draft + Submitted".
   - Its props table says every visible string is "threaded once through the family" and names `submittedByLabelText`, but that name reaches the cards, not the bar (item 1).
   - Its hooks listing and its filter-hook props row name four filter hooks and put the bible reference among the navigation hooks, while the component treats it as a fifth filter hook (item 4).
7. **Note — `ContentCardGrid`, ruled.** §UI20.6 planned `ContentCardGrid` — "Responsive grid of `ContentCard` components" — while this panel and its results panel render a single scrolled column of ContentItemPanel. This item asked whether ContentItemListPanel is what `ContentCardGrid` became, or a grid is still planned. The user ruled on 2026-09-27 that the planned catalogue entries built under other names are superseded, each pointing at the component actually built; §UI20.6 now marks `ContentCardGrid` superseded by this panel.
8. (needs issue) **A tag or Bible reference click rewrites the criteria.** Rules 2.12 and 2.13 (user ruling 2026-09-27; §UI20.6.4) have both clicks raise the page's hook and nothing else. Today the panel wraps both: a tag click toggles that tag in the committed tag criterion, compared case-insensitively, raises `onSearch`, and then calls the page's `onTagClick`; a Bible reference click does the same with the bible-reference criterion. Evidence: `contentItemListPanel.tsx` — `tagClicked` (lines 233-246 at 70dc72e7) and `bibleReferenceClicked` (lines 248-261), and the header comment, which counts `onTagClick` among the filter hooks (line 37); test: `contentItemListPanel.test.tsx` — "should set the tag criterion from a pill".
9. (needs issue) **Edit relabelled as View.** Rule 2.16 (user ruling 2026-09-27; `UI/Components/ContentItemPanel.md rule 2.40`) makes View and Edit two actions, this panel's properties deciding which its cards offer. Today the panel defaults `editButtonText` to *View* (`contentItemListPanel.tsx`, line 183 at 70dc72e7), so a listed card's owner-only Edit, raising `onEditClick`, reads *View* — one action wearing the other's name — and the pages route it to a detail surface (section 6). The panel has no property that offers View or Edit. Tests: `contentItemListPanel.test.tsx` — "should name a listed card’s pencil View rather than Edit", "should let the surface name the button itself". The card's half is `UI/Components/ContentItemPanel.md §10 item 20`.
10. **Note — the type chip, *Submitted by* and *Author* clicks, ruled.** This item asked whether
    those three clicks should raise the page's hook alone, as tag and Bible reference clicks do,
    or stay the list's own filters: a click rewrote the committed criteria and raised `onSearch`,
    and then the page's hook fired (`contentItemListPanel.tsx` — `contentTypeClicked`,
    `submittedByClicked`, `authorClicked`). The user ruled on 2026-09-27 that they raise hooks and
    the page decides; that in the user section the page hands the value to the search page,
    `/posts`, whose search bar shows it in the matching box with the advanced section expanded;
    and that the type chip works the same way. The list no longer turns those clicks into its own
    search. Rules 2.8–2.11 and 2.14 now say so, and rule 2.24 gives the page the way to open the search
    so. The code's half is items 11 and 12; where each page takes the clicks is the page's.
11. (needs issue) **A type chip, *Submitted by* or *Author* click rewrites the criteria.** Rules
    2.8–2.11 and 2.14 (user rulings 2026-09-27) have each click raise the page's hook and nothing
    else. Today the panel wraps all three: a type-chip click toggles the Category criterion —
    sets the item's type when the criterion is clear, clears it when it is already that type — a
    *Submitted by* click sets the submitted-by criterion to the element's account id and name,
    committing nothing for an element with no submitter id, and an *Author* click sets the author
    criterion to the element's author; each raises `onSearch` and then calls the page's hook.
    Evidence: `contentItemListPanel.tsx` — `contentTypeClicked` (lines 199-210 at 70dc72e7),
    `submittedByClicked` (lines 212-224), `authorClicked` (lines 226-229), the "THE FILTER HOOKS"
    comment (lines 195-198) and the header comment's "WHAT THIS LEVEL ADDS" (lines 36-39); tests:
    `contentItemListPanel.test.tsx` — "should toggle the category on when the type badge is
    clicked", "should toggle the category back off when it is already this type", "should set the
    submitted-by criterion from the byline", "should set the author criterion from the meta row".
12. (needs issue) **The panel cannot open the bar's advanced section.** Rule 2.24 (user rulings
    2026-09-27) has the page open the search with a criterion in its box and the advanced section
    expanded. This panel forwards the committed criteria to the bar, which seeds its boxes from
    them (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md rule 2.3`), but no
    property says whether the advanced section opens expanded: the bar has none to receive it
    (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 7`), and this panel
    declares none to forward. Evidence: `contentItemListPanel.tsx` — the `<ContentItemSearchBarPanel>`
    element (lines 275-289 at 70dc72e7), which forwards fourteen properties, none of them this.
13. (needs issue) **The list has no hook for a change in the search form.** The page keeps the
    search in the query string and has it follow the form as the reader changes it, without a
    reload (`UI/Pages/Posts.md rule 2.21`, `UI/Pages/ContentItemModerationPage.md rule 2.12`;
    user rulings 2026-09-27). The bar raises no event for a change before the search is
    committed (`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md §10 item 8`), and
    this panel declares none to forward it to the page: the `<ContentItemSearchBarPanel>` element
    passes `onSearch` alone among events (`contentItemListPanel.tsx`, lines 275-289 at 70dc72e7).
    Once the bar has one, this panel is to forward it unchanged (§UI20.6.5).
14. (needs issue) **Share cannot be switched per card.** `/myposts` and `/Admin/Posts` offer Share
    only on an `Approved` item (`UI/Pages/MyPosts.md rule 2.16`,
    `UI/Pages/ContentItemModerationPage.md rule 2.14`; user ruling 2026-09-27), and both list
    items at every status. This panel takes `showShareSection` once, among the section switches,
    and hands the same value to every card (`contentItemListPanel.tsx` —
    `ContentItemListPanelProps` extends `ContentItemSectionToggles`, whose comment calls the
    switches "per-SURFACE decisions a page makes once for every card", lines 44-51 at 70dc72e7;
    `contentItemResultsPanel.tsx` — the `<ContentItemPanel>` element, lines 109-117). The card
    offers Share wherever the switch is on and `onShareClick` is wired, and asks nothing of the
    item's status (`contentItemDefaultPanel.tsx` — `showsShare`, line 147). A page rendering
    `ContentItemPanel` itself can set the switch card by card; through this panel it cannot.
15. (needs issue) **No list narrows by tag or Bible reference.** The bar commits both criteria,
    each with its Any/All match mode, and the page keeps them in its URL, but no page's read acts
    on them: `useSearchContentItems` hands the broker the query, type, author, shareability,
    submitter and statuses, and none of the tags, the Bible references or their match modes
    (`services/foundations/contentItemService.ts`, lines 105-116 at dbefe8aa). A list narrowed by
    either shows what it showed before. Every page rule and gap that says a tag or a Bible
    reference narrows a list cites this item for the narrowing (`UI/Pages/Posts.md rule 2.12` and
    §6 item 7, `UI/Pages/Home.md rules 2.4 and 2.13` and §6 items 7 and 15,
    `UI/Pages/MyPosts.md rule 2.14`, `UI/Pages/ContentItemModerationPage.md rule 2.13`,
    `UI/Pages/BibleReference.md rule 2.17` and §6 item 6), and how a read narrows by either is
    not designed.
    The narrowing waits on reading associations over HTTP
    (`UI/Components/AssociationPanel.md §10 item 21`), and is not part of that read's design: the
    user ruled on 2026-10-05 that #857 plans only the read (#857, Open question 1: *"only the
    read"*).
