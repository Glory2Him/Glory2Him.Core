# UI / UX Design

Carries `G2H Design.md` §20 "UI / UX Design" — the React application's pages,
components, navigation and authentication design.

Section numbers below carry a **`UI` prefix** and are otherwise the numbers
these sections already had: an old `§20.N` is now `§UI20.N`, and nothing was
renumbered, reordered, merged or split in the move. That is the
prefix-preserving rule of §IDX1.5. It means this file starts at `UI20` rather than
at 1 and its numbering is not contiguous with the other area files, so the
contents list below is what makes that gap read as a table of contents rather
than as missing content. The other area files under `Documentation/Design/` carry
their own prefixes — `ARC`, `APR`, `DOM`, `SEC`, `EVN` — so a bare `§UI20.6.1`
is unambiguous. Where this file cites one of them, the map at the top of
[`G2H Design.md`](../G2H%20Design.md) is what resolves it.

**`Events.md` is the one file a citation into it cannot be derived for.** It
renumbered rather than prefix-preserved, so an into-Events citation is looked up
in that file's own *(formerly §10.X)* annotations instead of having a prefix
applied to the number it already had. The one such citation this file carried —
`§EVN18`, in §UI20.6.1, which has since moved to `UI/Components/ReviewPanel.md` —
was resolved that way, and `§EVN10.17` would have been the wrong answer.

This repository's C# and TypeScript comments cite design sections extensively,
so every relocated section also carries a *(formerly §20.X)* annotation naming
its old position. The literal old string still appears on the right heading, so
an old citation resolves by grep even though the citable number is now prefixed.

**Contents**

- [UI20. UI / UX Design](#ui20-ui--ux-design-formerly-20)
  - [UI20.1 Purpose](#ui201-purpose-formerly-201)
  - [UI20.2 Technology Stack](#ui202-technology-stack-formerly-202)
  - [UI20.3 Architecture Principles](#ui203-architecture-principles-formerly-203)
  - [UI20.4 Folder Structure](#ui204-folder-structure-formerly-204)
  - [UI20.5 Pages](#ui205-pages-formerly-205)
    - [UI20.5.1 Page documents](#ui2051-page-documents-new-user-ruling-2026-09-27)
  - [UI20.6 Components](#ui206-components-formerly-206)
    - [UI20.6.1 ReviewPanel — contract and dependencies](#ui2061-reviewpanel--contract-and-dependencies-formerly-2061)
    - [UI20.6.2 ContentItemPanel — contract and dependencies](#ui2062-contentitempanel--contract-and-dependencies-formerly-2062)
    - [UI20.6.3 ReviewCommentPanel — contract and dependencies](#ui2063-reviewcommentpanel--contract-and-dependencies-formerly-2063)
    - [UI20.6.4 Component documents](#ui2064-component-documents-new-user-ruling-2026-09-26)
    - [UI20.6.5 Pass-through properties](#ui2065-pass-through-properties-new-user-ruling-2026-09-26)
    - [UI20.6.6 Rules every presentation component follows](#ui2066-rules-every-presentation-component-follows-new-user-rulings-2026-09-27)
  - [UI20.7 Navigation](#ui207-navigation-formerly-207)
  - [UI20.8 Authentication](#ui208-authentication-formerly-208)
    - [UI20.8.1 The return after sign-in](#ui2081-the-return-after-sign-in-new)
  - [UI20.9 Services and Brokers](#ui209-services-and-brokers-formerly-209)
    - [UI20.9.1 Departures from the broker skill](#ui2091-departures-from-the-broker-skill-new)
    - [UI20.9.2 Departures from the services skill](#ui2092-departures-from-the-services-skill-new)
  - [UI20.10 Live updates](#ui2010-live-updates-new)

---

## UI20. UI / UX Design *(formerly §20)*

### UI20.1 Purpose *(formerly §20.1)*

The G2H frontend is a React application responsible for presenting gospel content to users in a clean, readable, and accessible way.

The design reference is the Blogzine Bootstrap template (https://www.webestica.com/bootstrap-templates/blogzine-blog-magazine-template), which will be converted into a React + TypeScript + Vite + Bootstrap architecture with full componentisation and clean separation of concerns.

### UI20.2 Technology Stack *(formerly §20.2)*

| Layer | Technology |
| --- | --- |
| Framework | React 19+ |
| Language | TypeScript |
| Build tool | Vite |
| Styling | Bootstrap 5 |
| Routing | React Router v7 |
| State management | TBD — React Context or lightweight store |
| HTTP client | Axios or native Fetch with typed wrappers |
| Auth | Token-based — JWT or MSAL depending on identity provider |

### UI20.3 Architecture Principles *(formerly §20.3)*

The following principles apply to the frontend architecture:

1. Every visual element must be a reusable React component.
2. Components must not contain data-fetching logic — data flows in via props or context.
3. Pages are thin — they compose components and delegate data loading to services.
4. Services are typed wrappers over the HTTP layer and map API responses to frontend models.
5. Brokers are the lowest-level HTTP callers — one per API area — and are injected into services, in the sense §UI20.9.1 departure 4 gives.
6. Models are TypeScript interfaces that match API response shapes.
7. Navigation must support both unauthenticated public routes and authenticated, role-aware private routes.

### UI20.4 Folder Structure *(formerly §20.4)*

Recommended project structure:

```
src/
  brokers/          # Typed HTTP callers per API area
  services/         # Business logic, mapping, orchestration over brokers
  models/           # TypeScript interfaces matching API response shapes
  components/       # Reusable UI components (atoms, molecules, organisms)
  pages/            # Route-level page components — compose components and call services
  layouts/          # Layout wrappers (public layout, authenticated layout, admin layout)
  navigation/       # Route definitions, guards, role-based access
  hooks/            # Shared custom React hooks
  context/          # React Context providers for auth, theme, etc.
  assets/           # Static assets, images, fonts
```

### UI20.5 Pages *(formerly §20.5)*

Planned pages based on the Blogzine template and the G2H domain:

| Page | Purpose |
| --- | --- |
| `HomePage` | Feed of published content items ordered by publish date. |
| `ContentItemPage` | Full view of a single published content item. |
| `TopicPage` | Topic landing page with list of associated child content items. |
| `TopicListPage` | Browse all published topics. |
| `SearchPage` | Search results across published content. |
| `LoginPage` | User login. |
| `LogoutPage` | User logout and session cleanup. |
| `ProfilePage` | Authenticated user profile. |
| `SubmitContentPage` | Authenticated form to submit new content. |
| `EditContentPage` | Authenticated form to edit a draft or create a new version. |
| `ApprovalQueuePage` | Reviewer queue of content pending approval. |
| `ApprovalDetailPage` | Detail view of a content item under review with review actions. |
| `AdminDashboardPage` | Admin overview of content, settings, and approval configuration. |
| `NotFoundPage` | 404 fallback. |

Six of these are built as pages that have a page document (§UI20.5.1), each under its React
component's name: `HomePage` is `Home` (`/`), `ContentItemPage` is `PostDetail`
(`/posts/{contentItemId}`), `SearchPage` is `Posts` (`/posts`), the journal's search
(`UI/Pages/Posts.md rule 2.12`), `SubmitContentPage` is `Contribute` (`/posts/contribute`),
`ApprovalQueuePage` is `ContentItemModerationPage` (`/Admin/Posts`) and `ApprovalDetailPage` is
`ContentItemModerationDetailPage` (`/Admin/Posts/{contentItemId}`). A seventh, `EditContentPage`,
is built as no page of its own: it is the editor in place on `MyPostDetail`
(`/myposts/{contentItemId}`), where the owner edits a draft or forks a new version of a reviewed
item (`UI/Pages/MyPostDetail.md rules 2.3, 2.5 and 2.6`). Their page documents are their design.

#### UI20.5.1 Page documents *(new; user ruling 2026-09-27)*

Every product page that renders a documented presentation component has a **page document**
under [`Documentation/Design/UI/Pages/`](UI/Pages/), named after the page's React component — the
component in `Websites/Glory2Him.WebApp.React/src/pages/` that a route in `src/routes/` renders —
so that it traces to the code: `Home.md` for `Home`, in `home.tsx`. The sample pages under
`/SamplePages` are not product pages. They demonstrate the components — a component document
names its own as its **Sample page** (§UI20.6.4) — and have no page document, so a gap found in
one is recorded as §UI20.6.4 gives for a page without a document of its own. A page document records the page's layout —
single column; two columns, main with a right sidebar or a left sidebar with main; or three
columns — the components it renders in each region, and, in a table per component and per child
component, every hook and what the page does for it (user ruling 2026-09-27). Like the component
documents (§UI20.6.4), it differs from the layout `design.md` gives user story documents, and for
page documents this section wins.

**The page owns its hooks' destinations.** A presentation component raises hooks and knows no
route; the page supplies every link and every redirect (§UI20.6.4, *Hooks, not routes*). The page
document is where that is specified: for each hook, where it leads, what it writes and what it
refreshes — differently in a user section and an admin (moderation) section — and how the page
follows §UI20.6.6: the sign-in redirect of rule 2 and the no-dead-actions rule 4. A component
document specifies the component alone. A gap in a page that has a document is recorded in that
document's section 6, not in a component document. One first recorded in a component document
moves there, and an item that moves whole leaves the pointer §UI20.6.4 gives (*A gap moved to a
page document*).

**The template.** Every page document has these sections, in this order.

| Section | Holds |
| --- | --- |
| `# 1. <PageComponentName>` | What the page is for and who uses it, then the bullets **Route** (with its route file and line, and any alias or redirect), **Source**, **Section** — user, or admin (moderation) — **Access** (the route's guard and role list), **Layout** and **Components** (each documented component, linked, and each undocumented building block, by file). |
| `## 2. Business Rules` | The page-level rules alone — who reaches the page, what it wires, where its hooks lead, what it loads — numbered and MoSCoW-tagged by §UI20.6.4's criteria, each ending in a source tag of §UI20.6.4's forms. |
| `## 3. Layout` | The columns at desktop width, counting a shell's own sidebar, and how they stack on a narrow screen: a small text diagram of the regions, then a table of each region, its width and its components in order. |
| `## 4. Components and their hooks` | One subsection per component, and one per child component whose hooks the page wires through its parent: the properties the page sets, and why; then every hook the component can raise, wired or not, with when it is raised, what the page does, and whether that is built today — ✅ Yes with its file and line; ❌ No with the section-6 item that records it; *Not wired — switched off* where the page deliberately turns the action off, with the reason; *Not wired — nothing to do* where the hook only tells the page what the component has already done itself; or *Never raised* where the way the page renders the component leaves no path to the hook, with the reason. A hook nothing handles, on an action the page shows, is a gap (§UI20.6.6 rule 4). |
| `## 5. Security and access` | Who reaches the page and what each persona sees on it: a role matrix with §UI20.6.4's six persona columns, one row per region or action that differs; then the read-only roles, as the components compose them, cited from their security and access matrices rather than copied. |
| `## 6. Open Questions and Gaps` | Every question, divergence and gap, as one numbered list, in the form of a component document's section 10: a gap carries the tag of `design.md`'s *Conventions*, and a question states its facts plainly. A page gap first recorded in a component document keeps its facts here and cites the item it came from. |

The `?` and `≠` markers work as §UI20.6.4 gives them. A page document is cited as a component
document is, by its path relative to `Documentation/Design/` and a number, the whole citation in
one code span: `UI/Pages/Home.md rule 2.9`, `UI/Pages/Home.md §6 item 3`; inside the document, a
reference to its own rule or item is written `rule 2.9` or `item 3`.

**The page documents.** The nine product pages that render the documented components (user ruling
2026-09-27). The other product pages render none of them today, and are candidates for later:
from `src/routes/staticRoutes.tsx`, `/About-Us`, `/Contact-Us`, the Bible reader
(`/BibleReferences/BibleReader`, and `/BibleReferences/{reference}` for a chapter),
`/Style-Guide` and `/Not-Found`; from
`src/routes/shopRoutes.tsx`, `/Shop-Grid`, `/Shop-Detail`, `/My-Cart`, `/Empty-Cart` and
`/Checkout`; from `src/routes/adminRoutes.tsx`, `/Dashboard`, `/Admin/Users`, `/Admin/Users/{userId}`,
`/Admin/ContentItemSettings`, `/Admin/ContentItemSettings/{contentItemSettingId}`,
`/Admin/ApprovalSettings`, `/Admin/ApprovalSettings/New` and
`/Admin/ApprovalSettings/{approvalSettingId}`; and the account pages under `/Account/`, from
`src/routes/accountRoutes.tsx` and `src/routes/passkeyRoutes.tsx`.

| Document | Page | Route | Section | Layout |
| --- | --- | --- | --- | --- |
| [Home.md](UI/Pages/Home.md) | `src/pages/home.tsx` | `/` | user | single column |
| [Posts.md](UI/Pages/Posts.md) | `src/pages/posts.tsx` | `/posts` | user | single column |
| [PostDetail.md](UI/Pages/PostDetail.md) | `src/pages/postDetail.tsx` | `/posts/{contentItemId}` | user | two columns — main with right sidebar |
| [Contribute.md](UI/Pages/Contribute.md) | `src/pages/contribute.tsx` | `/posts/contribute` | user | single column |
| [BibleReference.md](UI/Pages/BibleReference.md) | `src/pages/bibleReference.tsx` | `/BibleReferences`, `/BibleReferences/{reference}` | user | two columns — main with right sidebar |
| [MyPosts.md](UI/Pages/MyPosts.md) | `src/pages/myPosts.tsx` | `/myposts` | user | single column |
| [MyPostDetail.md](UI/Pages/MyPostDetail.md) | `src/pages/myPostDetail.tsx` | `/myposts/{contentItemId}` | user | two columns — main with right sidebar |
| [ContentItemModerationPage.md](UI/Pages/ContentItemModerationPage.md) | `src/pages/admin/contentItemModerationPage.tsx` | `/Admin/Posts` | admin (moderation) | two columns — left sidebar with main |
| [ContentItemModerationDetailPage.md](UI/Pages/ContentItemModerationDetailPage.md) | `src/pages/admin/contentItemModerationDetailPage.tsx` | `/Admin/Posts/{contentItemId}` | admin (moderation) | three columns |

**The ported blog.** `/Post-Single` and `/Post-Single/{slug}` render `src/pages/postSingle.tsx`,
the ported template's post page, which shows one sample story whatever the address. It is a mock,
not a product page: a content item's page is `/posts/{contentItemId}` (`UI/Pages/PostDetail.md`),
and a content item is shown by the post card, `ContentItemPanel`, which leads there. The ported
template's blog is sample material (user rulings 2026-09-27): its store, read from `api/posts`, its
cards and building blocks, `/Post-Single`, and the links to `/Post-Single` from the menu,
`/Dashboard` and the old admin posts page all belong to the sample pages. So `/Post-Single` is
counted among the sample pages and has no page document, and nothing of the blog is pointed at
`/posts/{contentItemId}`: its cards show blog posts, not content items. The template's other blog
pages — `/Author`, `/Categories`, `/Tag`, `/Post-Grid`, `/Post-List` and
`/Post-Grid-Masonry-Filter` — and `/Search`, a demo, are sample material too, like `/Post-Single`:
they are not product pages and have no page documents, and the real search page is `/posts`
(`UI/Pages/Posts.md rule 2.12`; user ruling 2026-09-27). `/Search-Result` is the same ported
blog: ported from the template's `SearchResult.razor`, it lists the blog's own posts, read from
`api/posts`, through `PostListItem` (`src/pages/searchResult.tsx`, lines 8-9 and 24), so it is
counted with the blog, not among the candidate product pages. The building blocks' own composed
links stay gaps in §UI20.6.4's list, as the sample material's. These pages are still routed
beside the product pages, with their lines at 70dc72e7: item 1 below is the move the user ruled
for `/Post-Single`, and item 2 the move ruled for the others, each tagged as the list of building
blocks under §UI20.6.4 is.

1. (needs issue) **`/Post-Single` is routed as a product page.** The user ruled on 2026-09-27 that
   it moves under `/SamplePages`, and that everything that links to it follows it there, to its
   new address. The two routes are registered among the public post routes
   (`src/routes/publicPostRoutes.tsx`, lines 27 and 28). The cards that compose a link to it are
   `BlogSidebar`, `PodcastCard`, `PostCard`, `PostLargeCard`, `PostListItem` and
   `PostOverlayCard` (§UI20.6.4, its list of building blocks, items 5, 8, 9, 11, 12 and 13), each
   from the slug of the `PostView` it is handed (`src/models/coreUI/postView.ts`). A `PostView` is
   a row of the ported blog's own store, read from `api/posts` (`src/brokers/apiBroker.posts.ts`;
   `Websites/Glory2Him.WebApp/Infrastructure/PostApiEndpoints.cs`), not a content item. Four
   other links reach the route: the menu (`src/components/coreUI/megaMenu.tsx`, line 57),
   `/Dashboard` (`src/pages/dashboard.tsx`, line 150), the old admin posts page, now the sample
   page `/SamplePages/Posts` (`src/pages/admin/postsPage.tsx`, line 95), and the sample page
   `/SamplePages/Home/Blog-Tech` (`src/pages/samplePages/home/homeBlogTechSample.tsx`, line 66).
2. (needs issue) **The template's other blog pages, `/Search` and `/Search-Result` are routed as
   product pages.** The user ruled on 2026-09-27 that the eight move under `/SamplePages`, as
   `/Post-Single` does (item 1): `/Author`, `/Categories`, `/Tag`, `/Post-Grid`, `/Post-List`,
   `/Post-Grid-Masonry-Filter`, `/Search` and `/Search-Result`. The first six are registered among
   the public post routes (`src/routes/publicPostRoutes.tsx`, lines 21-26). Each lists the ported
   blog's posts, read from `api/posts` (`postService.useGetPosts`, in `author.tsx`,
   `categories.tsx`, `tag.tsx`, `postGrid.tsx`, `postList.tsx` and `journalMasonry.tsx` under
   `src/pages/`), through the same cards that link to `/Post-Single` (`PostCard` or
   `PostListItem`). `/Search` and `/Search-Result` are registered among the static routes
   (`src/routes/staticRoutes.tsx`, lines 27 and 28). `/Search` calls itself a demo: whatever is
   typed, the same sample posts come back (`src/pages/search.tsx`, header comment).
   `/Search-Result` is the ported blog's own search, above. The sample material's own links to
   them follow them to their new addresses, as the links to `/Post-Single` do (item 1): the
   building blocks' (§UI20.6.4, its list of building blocks, items 1, 3, 5, 8–13 and 16), the
   menu's (`src/components/coreUI/megaMenu.tsx`, lines 53 and 73), the blog pages' own
   (`src/pages/tag.tsx`, lines 41 and 49; `src/pages/postSingle.tsx`, line 70), and the sample
   pages' under `src/pages/samplePages/`. The product's own links to them lead to `/posts` instead
   (user ruling 2026-09-27): the layout's and the Bible reader's by §UI20.7 rule 4, whose gaps are
   `UI/Pages/Home.md §6 item 12` and, for the footer's topic links, `UI/Pages/Home.md §6 item 15`;
   and, in the user section, the tag panel's default link,
   `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`. An unreadable Bible
   reference's link, which reaches `/Search` today, leads to the Bible reference page instead (user
   ruling 2026-09-28; `UI/Pages/BibleReference.md rule 2.18`), its gap `UI/Pages/Home.md §6 item 14`
   and the items it names. In the admin section a tag or a Bible reference leads to the queue,
   `/Admin/Posts`, instead (`UI/Pages/ContentItemModerationPage.md rule 2.13`). Each of these
   product links changes before the page it points at moves, or with it: a link left pointing at a
   moved page reaches the Not Found page (`src/routes/staticRoutes.tsx`, line 31). Some of the
   links into `/Search` are held by design issues, so `/Search` moves only once they have changed:
   an unreadable Bible reference's and the Bible reference page's own chips, held for #700
   (`UI/Pages/Home.md §6 item 14` and the items it names, `UI/Pages/BibleReference.md §6 item 4`),
   and the admin post page's side-panel chips, held for #698
   (`UI/Pages/ContentItemModerationDetailPage.md §6 item 17`). No held gap links to the other seven
   pages.

### UI20.6 Components *(formerly §20.6)*

Planned reusable components based on the Blogzine template. A component that has a component
document (§UI20.6.4) links it, and its design lives there:

| Component | Purpose |
| --- | --- |
| `Navbar` | Top navigation bar with logo, links, search, and auth state. |
| `Footer` | Site footer with links and attribution. |
| `ContentCard` | SUPERSEDED — built as `ContentItemPanel`'s view face, the card every feed shows for one item, through `ContentItemDefaultPanel` or its content type's own template ([`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md), [`UI/Components/ContentItemPanel.Default.md`](UI/Components/ContentItemPanel.Default.md)). It was planned as a feed card for a single content item — header image (§DOM4.9), title, type, excerpt, publish date. |
| `ContentCardGrid` | SUPERSEDED — built as `ContentItemListPanel`, which renders the items in one scrolled column through `ContentItemResultsPanel` ([`UI/Components/ContentItemListPanel.md`](UI/Components/ContentItemListPanel.md)). It was planned as a responsive grid of `ContentCard` components. |
| `ContentCardFeatured` | Hero-style featured content card. |
| `ContentDetail` | SUPERSEDED — built as `ContentItemPanel` on a detail page, with the tags and Bible references beside it in the association panels ([`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md)). It was planned as the full content item display — body, author, tags, reactions, comments, Bible references. |
| `TopicCard` | Card for a topic landing page preview. |
| `TagBadge` | SUPERSEDED — built as the chips of `TagAssociationPanel` ([`UI/Components/AssociationPanel.TagAssociationPanel.md`](UI/Components/AssociationPanel.TagAssociationPanel.md)) and the tag pills of the card's tag section ([`UI/Components/ContentItemPanel.Default.md`](UI/Components/ContentItemPanel.Default.md)). It was planned as an individual tag badge. |
| `TagList` | SUPERSEDED — built as `TagAssociationPanel` and the card's tag section, as `TagBadge` above. It was planned as a list of `TagBadge` components. |
| `ReactionBar` | SUPERSEDED — built as the card's Like control and reaction counts ([`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md), [`UI/Components/ContentItemPanel.Default.md`](UI/Components/ContentItemPanel.Default.md)). It was planned as a row of available reactions with counts. A core-UI `ReactionBar` (`src/components/coreUI/reactionBar.tsx`) also exists, rendered on `/BibleReferences`, where what a reaction records is to be designed under #700. |
| `CommentList` | List of approved comments for a content item. |
| `CommentForm` | Authenticated form to submit a comment. |
| `BibleReferenceBlock` | Display block for a Bible reference and optional scripture text. |
| `ApprovalStatusBadge` | SUPERSEDED — built as the card's status pill and ribbon, which a page switches on (`showApprovalStatus`, `showApprovalStatusRibbon`; [`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md)), and `ReviewPanel`'s status pill ([`UI/Components/ReviewPanel.md`](UI/Components/ReviewPanel.md)). It was planned as a badge showing the current approval status. |
| `ApprovalReviewForm` | SUPERSEDED — built as `ReviewPanel`, whose vote records a reviewer's approval or rejection and whose decision is the publisher tier's ([`UI/Components/ReviewPanel.md`](UI/Components/ReviewPanel.md)). It was planned as a form for a reviewer to submit an approval or rejection decision. |
| `ApprovalCommentForm` | RETIRED — superseded by `ReviewCommentPanel` below, which is the whole thread rather than the box alone. A separate add-only form would have had to re-decide the same three gates. |
| `ReviewPanel` | The approval round rendered: reviews, the viewer's own vote, block reasons, bypass, the publisher-tier decision, and review requests ([`UI/Components/ReviewPanel.md`](UI/Components/ReviewPanel.md)). |
| `ReviewCommentPanel` | The round's conversation: the box and its Comment/Question choice, the thread newest-first, the settled tick on asks, and the author's Edit and Delete ([`UI/Components/ReviewCommentPanel.md`](UI/Components/ReviewCommentPanel.md)). |
| `ContentItemPanel` | One content item on whichever face the moment asks for — the add and edit templates and the per-type view templates, field-shaped per content type and gated per §SEC18.6 ([`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md)). Paste-to-upload for inline images (§DOM5.6.6) is not part of it yet. |
| `ContentItemAddPanel` | `ContentItemPanel`'s add face: the type picker and a blank form, where a signed-in reader contributes a content item ([`UI/Components/ContentItemPanel.Add.md`](UI/Components/ContentItemPanel.Add.md)). |
| `ContentItemRestrictedPanel` | `ContentItemPanel`'s restricted face: shown instead of the add face when the reader has no content type left to contribute — a very basic panel whose wording says contributions are not being taken. Not built yet ([`UI/Components/ContentItemPanel.Restricted.md`](UI/Components/ContentItemPanel.Restricted.md)). |
| `ContentItemEditPanel` | `ContentItemPanel`'s edit face: the item's type, frozen, and a form seeded from the item, with removal riding on it ([`UI/Components/ContentItemPanel.Edit.md`](UI/Components/ContentItemPanel.Edit.md)). |
| `ContentItemDefaultPanel` | The view template most content types render through — the card a feed or detail page shows for one item — and the base the per-type overrides derive from ([`UI/Components/ContentItemPanel.Default.md`](UI/Components/ContentItemPanel.Default.md)). |
| `ContentItemQuotesPanel` | The view template for a quote: the quote shown whole, large, as the card's heading, with its author after an em-dash ([`UI/Components/ContentItemPanel.ContentItemQuotesPanel.md`](UI/Components/ContentItemPanel.ContentItemQuotesPanel.md)). |
| `ContentItemVerseImagePanel` | The view template for a verse image: the verse exactly as written, standing large, with nothing appended ([`UI/Components/ContentItemPanel.ContentItemVerseImagePanel.md`](UI/Components/ContentItemPanel.ContentItemVerseImagePanel.md)). |
| `ContentItemListPanel` | Many content items, searched and scrolled: the search bar above the results, and every item rendered through `ContentItemPanel` — one family for the public feed, the caller's own posts and the moderation queue ([`UI/Components/ContentItemListPanel.md`](UI/Components/ContentItemListPanel.md)). |
| `ContentItemSearchBarPanel` | The list's search bar: a query box and Search, an advanced fold-out, and opt-in approval status checkboxes; it raises the committed criteria ([`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md`](UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md)). |
| `ContentItemResultsPanel` | The list's results: every matched element as one `ContentItemPanel`, scrolled rather than paged, with the first-page load, the empty result and the infinite scroll ([`UI/Components/ContentItemListPanel.ContentItemResultsPanel.md`](UI/Components/ContentItemListPanel.ContentItemResultsPanel.md)). |
| `ContentItemSettingsPanel` | The `ContentItemSetting` row that governs one content item — its override where it has one, the content type default otherwise — and the writes that narrow that one item ([`UI/Components/ContentItemSettingsPanel.md`](UI/Components/ContentItemSettingsPanel.md)). |
| `AssociationPanel` | A labelled set of association chips with an optional box beneath for suggesting another — the generic half of the tag and bible reference panels ([`UI/Components/AssociationPanel.md`](UI/Components/AssociationPanel.md)). |
| `TagAssociationPanel` | `AssociationPanel` dressed as the tag panel: green chips with a hash in front of each ([`UI/Components/AssociationPanel.TagAssociationPanel.md`](UI/Components/AssociationPanel.TagAssociationPanel.md)). |
| `BibleReferenceAssociationPanel` | `AssociationPanel` dressed as the bible reference panel: blue chips carrying a book icon once approved ([`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md`](UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md)). |
| `SharingPanel` | The invitation to contribute — an icon, a title, a description and a button — adapting to its container; it shares nothing outward ([`UI/Components/SharingPanel.md`](UI/Components/SharingPanel.md)). |
| `HeaderImagePicker` | Header-image candidates for a content item — upload, list, promote the default (§DOM4.9). |
| `SearchBar` | SUPERSEDED — built as `ContentItemSearchBarPanel`, on the core-UI `SearchBarComponent` (`src/components/coreUI/searchBar.tsx`), which commits on Search rather than debouncing ([`UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md`](UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md)). It was planned as a search input with debounce. |
| `Pagination` | SUPERSEDED — built as `ContentItemResultsPanel`'s infinite scroll: the feeds scroll rather than page ([`UI/Components/ContentItemListPanel.ContentItemResultsPanel.md`](UI/Components/ContentItemListPanel.ContentItemResultsPanel.md)). It was planned as paginated navigation for feed and topic child lists. A core-UI `Pagination` (`src/components/coreUI/pagination.tsx`) also exists, rendered by the demo page `/Search`, sample material moving under `/SamplePages` (§UI20.5.1). |
| `PrivateRoute` | Route guard for authenticated routes. |
| `RoleRoute` | Route guard for role-restricted routes. |
| `LoadingSpinner` | SUPERSEDED — built as the core-UI `Spinner` (`src/components/coreUI/spinner.tsx`), which announces itself as a status. It was planned as a generic loading indicator. |
| `ErrorMessage` | Generic error display. |

#### UI20.6.1 ReviewPanel — contract and dependencies *(formerly §20.6.1)*

Moved to [`UI/Components/ReviewPanel.md`](UI/Components/ReviewPanel.md), the component
document for `ReviewPanel` (§UI20.6.4). Each rule relocated there carries this section's number
as its source tag, *(§UI20.6.1)*, so a citation of this section still resolves by grep to the
rule's new home. This heading stays so that the citations in code land one hop from it; it
holds no design of its own. Cite the component document by its path and rule number —
`UI/Components/ReviewPanel.md rule 2.3`.

#### UI20.6.2 ContentItemPanel — contract and dependencies *(formerly §20.6.2)*

Moved to [`UI/Components/ContentItemPanel.md`](UI/Components/ContentItemPanel.md), the component
document for `ContentItemPanel` (§UI20.6.4), with the parts that belong to one face in that
face's story: [`ContentItemPanel.Add.md`](UI/Components/ContentItemPanel.Add.md),
[`ContentItemPanel.Edit.md`](UI/Components/ContentItemPanel.Edit.md),
[`ContentItemPanel.Default.md`](UI/Components/ContentItemPanel.Default.md) and
[`ContentItemPanel.ContentItemQuotesPanel.md`](UI/Components/ContentItemPanel.ContentItemQuotesPanel.md).
`UI/Components/ContentItemPanel.md §6` lists which part went where. Each rule relocated
carries this section's number as its source tag, *(§UI20.6.2)*, so a citation of this section
still resolves by grep to the rule's new home. This heading stays so that the citations in code
land one hop from it; it holds no design of its own. Cite the component document by its path
and rule number — `UI/Components/ContentItemPanel.md rule 2.17`.

#### UI20.6.3 ReviewCommentPanel — contract and dependencies *(formerly §20.6.3)*

Moved to [`UI/Components/ReviewCommentPanel.md`](UI/Components/ReviewCommentPanel.md), the
component document for `ReviewCommentPanel` (§UI20.6.4). Each rule relocated there carries this
section's number as its source tag, *(§UI20.6.3)*, so a citation of this section still resolves
by grep to the rule's new home. This heading stays so that the citations in code land one hop
from it; it holds no design of its own. Cite the component document by its path and rule
number — `UI/Components/ReviewCommentPanel.md rule 2.11`.

#### UI20.6.4 Component documents *(new; user ruling 2026-09-26)*

Every presentation component documented on its own has a **component document** under
[`Documentation/Design/UI/Components/`](UI/Components/). The location, the names and the
template below are the user's ruling. They differ from the layout `design.md` gives feature and
user story documents (`DesignFeatures/<Feature>.md`, `DesignFeatures/UI/<Story>.md`) and from
the operation-section format of `Documentation/DesignFeatures/README.md`; for component
documents, this section wins.

**Names.** The file name shows where a component stands in the tree.

- `<Root>.md` is a **feature**: a root presentation component — `ContentItemListPanel.md`.
- `<Root>.<Child>.md` is a **user story**: a child component of that root —
  `ContentItemListPanel.ContentItemSearchBarPanel.md`. `<Child>` is the child component's
  name or, for one of the root's own faces, the face's short name — `ContentItemPanel.Add.md`
  for `ContentItemAddPanel`.
- A component that works on its own but is also rendered inside another is a root of its own,
  so that no file path grows long — `ContentItemPanel.md`, although `ContentItemListPanel`
  renders it.

A child's document names its root as its parent, and the root lists every child. A document
names the other roots it renders, and a root names every component document and every page that
renders it, so a component that stands alone says everywhere it is used.

**Hooks, not routes** (user rulings 2026-09-26 and 2026-09-27). A presentation component
exposes hooks and knows no route. It performs no redirect, and it composes no route of its own;
it is the page's responsibility to wire the hooks. **The page supplies every link and every
redirect, and the component supplies the hook** — a Must for every component (user ruling
2026-09-27). Two instances were ruled the same day. A tag click and a Bible reference click each
raise a hook, and the component performs no navigation and no filtering of its own on the click:
where the reader goes — typically the search page filtered by the tag, and a page showing the
verse — is the page's, and differs between a user section and an admin section. And View and
Edit are two actions with two hooks, each switchable by a `show…` property: View opens the
item's detail view read-only, on the default template or the type's own, such as the quote's;
Edit opens the detail view straight in edit mode; where each leads is the page's. A link whose
route the page supplies — a property the page
passes, such as a `loginHref`, or a builder the page provides, such as a `chipHrefFor` — is the
page wiring the hook. A default the component composes for such a property is the coupling.
Each page specifies its own behaviours for the presentation components it renders: the
properties it passes, per viewer, and where each hook leads. A component document specifies
the component alone. Until a page has a document of its own, a gap in the page is recorded in
section 10 of the component document where it was found, tagged like any other gap and marked
as the page's: after the tag, the item opens with **Page gap —** and the page's route. The rule
reaches the documented components and the undocumented building blocks a page or component
renders — `ContributionPrompt`, `TagPillList` and the `coreUI` post cards that compose their own
links. Route guards, `Navbar`, `Footer` and the page layouts exist to route, and are outside it
(user ruling 2026-09-27). A building block's composed link is a gap like a documented
component's. The building blocks that compose a link or a navigation of their own today are the
list below, each file under `Websites/Glory2Him.WebApp.React/src/components/coreUI/`, with its
lines at 70dc72e7. The breadcrumb trails (`breadcrumb.tsx`, `pageHeader.tsx`, `heroBanner.tsx`)
and the menu (`megaMenu.tsx`) are navigation, and are not listed. Of the pages the list's links
name, `/Author`, `/Categories`, `/Tag`, `/Post-Single` and `/Search` are the ported blog's,
sample material moving under `/SamplePages` (§UI20.5.1).

1. (needs issue) **`ArticleCard` composes its links.** Its category badge links to `/Categories`
   (line 45), and its title link defaults `href` to `'#'` (lines 25 and 54). It renders
   `TagPillList` (item 16).
2. (needs issue) **`AuthCard` composes its links.** Its footer link defaults `footerHref` to `'#'`
   (lines 22 and 36), and its two social sign-in buttons link to `#` (lines 58 and 61).
3. (needs issue) **`AuthorByline` composes its link.** The author's name links to `/Author`
   (line 41).
4. (needs issue) **`BibleChapter` composes its links.** Its two share icons link to `#` (lines 23
   and 29).
5. (needs issue) **`BlogSidebar` composes its links.** A recent post links to
   `/Post-Single/{slug}` (line 52), and a topic to `/Tag?name={topic}` (line 72).
6. (needs issue) **`CommentThread` composes its link.** *Reply* links to `#reply` (line 36).
7. (needs issue) **`ContributionPrompt` composes its link.** Its contribution link defaults `href`
   to `/posts/contribute` (lines 32 and 55). Its sign-in link (line 59) goes to `loginHref`, which
   the page must supply, so it is not part of this gap.
8. (needs issue) **`PodcastCard` composes its links.** Its category badge links to `/Categories`
   (line 37), and its title to `/Post-Single/{slug}` (line 40).
9. (needs issue) **`PostCard` composes its links.** Its title links to `/Post-Single/{slug}`
   (lines 12 and 26), and its category badge to `/Categories` (line 20).
10. (needs issue) **`PostHeroCard` composes its links.** Its category badge links to
    `/Categories` (line 97), and its title link defaults `href` to `'#'` (lines 41 and 102).
11. (needs issue) **`PostLargeCard` composes its links.** Its category badge links to
    `/Categories` (line 34), and its title to `/Post-Single/{slug}` (lines 37-38).
12. (needs issue) **`PostListItem` composes its links.** Its title links to `/Post-Single/{slug}`
    (lines 11 and 25), and its category badge to `/Categories` (line 21).
13. (needs issue) **`PostOverlayCard` composes its links.** Its category badge links to
    `/Categories` (line 22), and its title to `/Post-Single/{slug}` (line 27).
14. (needs issue) **`ProductCard` composes its links.** Its image and name link to
    `/Shop-Detail/{slug}` (lines 25, 30 and 50).
15. (needs issue) **`ShareLinks` composes its links.** Its four share icons link to `#` (lines 14,
    20, 26 and 32).
16. (needs issue) **`TagPillList` composes its links.** A tag links to `/Search?q={tag}` (line 29),
    and a Bible reference to the route `bibleReferenceHref` builds (line 38).
17. (needs issue) **`VerseOfTheDay` composes its link.** Its verse link defaults `href` to `'#'`
    (lines 10 and 21).

**The template.** Every component document has these sections, in this order. Sections 1–3 are
the user's; sections 4–10 extend them, and are always present — `None.` where nothing applies,
or, in a story, *Inherits `UI/Components/<Root>.md §N`; nothing to add.*

| Section | Holds |
| --- | --- |
| `# 1. <ComponentName>` | The header bullets — **Kind**, **Parent**, **Children**, **Composes**, **Used by**, **Inherits**, **Source**, **Sample page**, and **Relocated from** on a document that received a relocated section — then what the component is for, its intent and purpose. |
| `## 2. Business Rules` | Every business rule the component follows, MoSCoW-tagged. |
| `## 3. Presentation / Behaviour rules` | What is visible, hidden, enabled or disabled: `3.1 Driven by properties`, `3.2 Driven by roles`, `3.3 Combinations` — the property switches first, then the roles, and which outranks which — and `3.4 Role matrix`. |
| `## 4. Properties and Events` | `4.1 Properties` (property, type, default, purpose, what it passes through to), `4.2 Events` (event, payload, when it is raised) and `4.3 Pass-through properties` (§UI20.6.5). |
| `## 5. Security Requirements` | The security and access matrix first, then the server-side rules the component's gates mirror, and where identity comes from. |
| `## 6. Composition and Usage` | The component's family, and every page that renders it. |
| `## 7. Dependencies` | The data the consumer supplies, the API endpoints the consumer calls — never the component — and the indirect dependencies, such as the auth context. |
| `## 8. States, Validation and Feedback` | Loading, empty, error, validation read-back, confirmation and freshness. |
| `## 9. Styling and Accessibility` | CSS class properties and hooks, responsive or container behaviour, and ARIA. |
| `## 10. Open Questions and Gaps` | Every question, every divergence between the design and the code, and every gap, as one numbered list. An item that needs a task is tagged as `design.md`'s *Conventions* rule for a component document's gaps; a question is not. |

**Business rules — MoSCoW.** Section 2 numbers its rules `2.1`, `2.2`, … in one flat list, and
each carries exactly one tag, so re-prioritising a rule never renumbers it. A rule is tagged by
what it protects, against fixed criteria, never by the modal verb its source uses and never to
fill a category (user ruling 2026-09-26):

- **[Must]** — the component's purpose, a security or visibility gate, data integrity, or the
  server contract.
- **[Should]** — feedback and states a user would miss: validation read-back, empty and
  loading states, ordering.
- **[Could]** — conveniences and cosmetics: tooltips, icons, default texts, in-place expansion.
- **[Won't]** — an explicit exclusion.

A priority the user states for a rule stands over the criteria (user ruling 2026-09-26). A
relocated rule is tagged by the same criteria, and tagging it changes neither its text nor
its source tag. A story never re-tags a rule it inherits: where it cites a rule of its root,
its tag is the root's.

**Source tags.** Every numbered rule in sections 2 and 3 ends with its source: a design section
it applies (`*(§SEC18.6 rule 2)*`, cited, never restated), a statement of the user's
(`*(user, 2026-09-26)*`), or existing behaviour — `*(code: <file> — <symbol>)*`,
`*(test: <file> — "<test name>")*` or `*(sample: <doc file>)*`. Existing behaviour is a
requirement until someone decides otherwise. The component's source and its tests are the
authority for it; its sample page under `/SamplePages/Components/` is secondary and may be
stale, so it is never the only source of a rule the component does not bear out, and a sample
page that disagrees with the component is a gap in section 10. A rule with no source is not
written: it goes to section 10 as a question.

**Required behaviour, divergence and open questions.** Sections 2 and 3 and the role matrix
state the behaviour the component is required to have. Where a rule meets another rule, or the
component, it is one of three cases:

1. **A later design rule settles an earlier one it contradicts.** This case applies only where
   the later rule was written about the same behaviour: its text, or the commit that made it,
   addresses what the earlier sentence says. Two rules that merely happen to be dated apart are
   not this case. The later rule stands; it is not a question. The earlier sentence, relocated
   or not, is corrected to agree with it and cites it, and section 10 records the correction as
   a note — no tag and no marker. Both are dated with `git log -S` before either is called the
   later. Where the earlier sentence sits in a global document (`Security.md`,
   `Architecture.md`, …), component work does not edit it: the component document follows the
   later rule, and section 10 records the global correction as an item tagged
   `(needs issue)`. No marker sits on the component's rules for it, since the rule they follow
   is settled.
2. **A clear design rule the component departs from.** The rule or the matrix row states the
   design and carries the divergence marker — `≠` and the section-10 item, or items, that
   record the divergence: `≠ item 3`, `≠ items 1 and 3`, or
   ``≠ `UI/Components/ContentItemPanel.md §10 item 3` `` for an item in another component
   document. A rule drawn from the code that contradicts a clear section-2 rule is written as
   the requirement with the marker: what the component does today is stated in section 10
   alone.
3. **An unresolved design** — rules that conflict at the same time, or a rule read against a
   feature it was written before. Two rules are written at the same time when neither was
   written with the other's behaviour in view: in the same change, for example, or on the same
   day without either addressing the other. The rule or row states the behaviour the component
   is built to, since existing behaviour is a requirement until someone decides otherwise, and
   a section-10 item holds the question for the user.

**The open-question marker.** Every rule and matrix row that stands under an open section-10
question — the rule as built and the design rule it conflicts with alike — carries `?` and the
item, or items, that hold the question: `? item 3`, `? items 1 and 3`, or
``? `UI/Components/ContentItemPanel.md §10 item 3` ``, so a reader of either sees the
question. A rule or matrix row stands under an open question when at least one answer the
question offers would change the rule's text or one of the row's cells.
It is distinct from `≠`, which marks a settled rule the component falls short of.
A rule carries either marker after its source tag, a matrix row at the end of its condition
cell, and each carries one marker.

**A gap moved to a page document.** A section-10 gap that moved to a page document (§UI20.5.1)
keeps its number here as a one-line pointer —
`N. **Moved to the page documents.** <title> — now <UI/Pages/X.md §6 item M, …>.` — with no
`(needs issue)` tag and no marker pointing at it, so citations of its number stay valid and the
sweep counts the gap once; the page item says where it was copied from.

**Relocated rules.** A rule relocated from this file keeps the number of the section it came
from as its source tag — `*(§UI20.6.2)*`. The tag is provenance, and works like a
*(formerly …)* annotation: a grep for the old number lands on the rule's new home.
§UI20.6.1–§UI20.6.3 are pointer stubs, so a document cites the component document for their
content, never those numbers. They stand in three places only: the **Relocated from** bullet,
the provenance tag, and prose whose subject is the old section itself.

**The role matrix.** Section 3.4 is a table with the same six persona columns in every
document:

| Column | Who |
| --- | --- |
| Anonymous | no signed-in identity |
| Signed-in reader | signed in; holds none of the tiers below; does not own the thing on screen |
| Owner | the account whose id matches the owner of the thing on screen (`[OWNER]`, matched on the account id, never a display name), holding none of the tiers below; each document says what owning means for it — a content item's contributor, a comment's author |
| Reviewer | holds `Reviewers` at a scope the component composes; not the owner |
| Publisher | holds `Publishers` at a scope the component composes; not the owner |
| Administrator | holds `Administrators`; not the owner |

A combined state — an owner who also holds a tier, any persona holding a `ReadOnly` role at any
scope — is a **row condition**, never an extra column. A cell is `✅ Yes` or `❌ No`, with a
footnote marker (`✅ Yes¹`) where an extra condition applies, and `➖ n/a` only where the row
cannot apply to that persona. The rows cover every gated affordance under each property state
that changes it. A component with no role gate still has the matrix, and says so.

**The security and access matrix** (user rulings 2026-09-27). Section 5 opens with it, before its
first rule: an unnumbered table headed **Security and access matrix**, with one row per action
the component offers — every button, link, hook-raising control and gated view:

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |

- **Offered to** — the grant: the persona or role set, as the component composes it.
- **Blocked by** — the read-only roles that withhold the action, composed for what the component
  represents (§UI20.6.6 rule 3) — a tag suggestion is blocked by `ReadOnly` and `Tag-ReadOnly`, for
  example — or *none*, with its source.
- **A blocked-role holder** — ✅ Allowed or ❌ Refused, and what they see instead.
- **A signed-out reader** — offered, and the hook it raises for the page (§UI20.6.6 rule 2), or
  not offered.
- **The server decides** — the design rule that decides the action again on the server, since the
  render gate is a courtesy (§SEC14.6) — `§SEC14.7 posture A′ rule 1`, for example.

Every cell rests on a source, as a rule does, given in the row or in a footnote. A row carries
the `?` and `≠` markers as a role-matrix row does, at the end of its first cell. The matrix
stands beside the section 3.4 role matrix and does not replace it; where the two could disagree,
they must not.

**Citation form.** A component document has no prefix, so it is cited by its path relative to
`Documentation/Design/` and a number (§IDX1.5), the whole citation inside one code span:
`UI/Components/ContentItemPanel.md rule 2.4` for a business rule,
`UI/Components/ContentItemPanel.md rule 3.2.1` for a presentation rule,
`UI/Components/ContentItemPanel.md rules 2.1–2.9` for a range,
`UI/Components/ContentItemPanel.md §7` for a section and
`UI/Components/ContentItemPanel.md §10 item 3` for a section-10 item — the same form
everywhere, inside the folder too. Inside a component document, a reference to its own section
or rule is written `section 4.1` or `rule 3.1.4`, never with a section sign: a section sign
before an unprefixed dotted number is the bare dotted number §IDX1.5 forbids inside
`Documentation/`, and every unprefixed number is also an old number of `G2H Design.md`. The
header bullets **Parent**, **Children**, **Composes** and **Used by** link the documents they
name with relative markdown links; a citation anywhere else, the **Inherits** bullet included,
is the code-span form. Code paths are backticked and
repository-relative.

#### UI20.6.5 Pass-through properties *(new; user ruling 2026-09-26)*

Presentation components use pass-through properties: a parent takes, and hands down unchanged,
the properties that control each child it renders, so the page can configure the whole tree from
the top (prop drilling).

Whenever a presentation component is developed or changed, the planner checks that each parent
that renders it can control it this way — through prop drilling, or pass-through properties.

**Scope — documented children only.** The check covers parent→child control between components
that have component documents (§UI20.6.4), a root's faces documented inside its file included.
A core-UI primitive a component renders — `ConfirmDialog`, `Avatar`, `Spinner`, `Card`,
`Button`, `FormSwitch` and the like — is styled through the parent's own CSS-class and text
properties, and a setting the parent fixes on it, such as an `Avatar` size, is not a gap
(user ruling 2026-09-26).

**Exceptions — renamed or withheld, if recorded.** A parent may hand a property down under
another name — where two names collide, for example — or deliberately withhold one, provided
its document records the exception and why. Reachability, not the name, is what the check asks
(user ruling 2026-09-26). A withholding is deliberate only where the code, a test or a design
rule says so; an omission nobody gave a reason for is a gap.

Each component document records the pass-through in its section 4.3, in both directions: a
parent names which of its properties reach which child, unchanged; a child names which parent
properties drive it. Section 4.3 also records each exception: the property, the name it takes
or that it is withheld, and why. Every other child property its parent cannot reach today is a gap in the
document's section 10, tagged as `design.md`'s *Conventions* rule for a component document's
gaps, with the evidence — the parent file, and the property it does not forward.

#### UI20.6.6 Rules every presentation component follows *(new; user rulings 2026-09-27)*

Every presentation component follows these five rules. A component that falls short of one
carries the divergence marker against a tagged section-10 gap in its document (§UI20.6.4, case
2).

1. **Every visible string is a property**, whose default is today's text, so a consumer need set
   nothing and may override anything. A string written for a screen reader alone — an accessible
   name, or visually hidden text — is a property on the same terms. This is a Must for every
   component.
2. **Sign-in is the page's; the component raises a hook.** A signed-out reader who tries to
   contribute or act raises the component's hook; the page sends them to the sign-in page
   carrying where they came from, and they return to exactly that place afterwards. One case
   returns them further on: a reader who accepts the invitation to contribute is returned to the
   contribution form it leads to, `/posts/contribute`, the place their press was heading for,
   rather than to the page they pressed it on (user ruling 2026-09-27). There are no
   sign-in pop-up modals. Sign-in is a global action: one reusable way every page uses, so the
   sign-in route is defined once. A reader whose sign-in state has not yet been read back is not
   sent to sign in.
3. **Blocked roles are stated per action, in a security and access matrix.** Every component
   document opens its section 5 with one, in the form §UI20.6.4 gives. The component composes its
   read-only roles itself from what it represents (§SEC18.6: `ReadOnly`, `%EntityType%-ReadOnly`,
   `ContentItem-%ContentType%-ReadOnly`). **No component takes a blocking-role list from its
   page**: none has a property for one — the `blockRoles` property the content item form once took
   is retired (user ruling 2026-09-27) — and a page can neither add to nor remove from the roles
   a component composes. What a page may hand a component is data the component composes roles
   from, such as the content type of the post an association panel hangs off. Each component's
   security and access matrix says which read-only roles it composes, and says that no page
   supplies them. Where a ruling or
   design rule does not settle what a blocked-role holder may do for an action, the planner asks
   the user, and the document records it as a section-10 question until then.
4. **No dead actions.** An action whose hook nothing handles yet is a gap the planner plans end to
   end — every task from the presentation layer down to the API, with API work planned too where
   it is needed — one UI feature at a time: Likes first, then Save, Share and Comments.
5. **Every loading state is announced** — `role="status"` or equivalent — consistently in every
   component. A saving or submitting state — a write in flight — is announced on the same terms.

### UI20.7 Navigation *(formerly §20.7)*

Navigation must support three levels:

1. **Public routes** — accessible to unauthenticated users. Includes feed, content item views, topic pages, and search.
2. **Authenticated routes** — require a valid session. Includes submit, edit, profile, and approval queue.
3. **Role-restricted routes** — require a specific role such as `Reviewers` or `Administrators`. Includes approval actions and admin dashboard.

Route guards should redirect unauthenticated users to the login page and unauthorised users to a 403 or not-found page.

The layout — the header, the footer and the off-canvas menu — is on every page, so where its links
lead is ruled here, with the Bible reader's, whose page has no document yet (§UI20.5.1):

4. **The layout's and the Bible reader's links to the ported blog's pages lead to the journal's
   search, `/posts`,** with any value carried in the query string (`UI/Pages/Posts.md rule 2.21`;
   user ruling 2026-09-27). The header's *Search*, the footer's *Journal* and *Authors*, and the
   off-canvas menu's *Our Journal* lead to `/posts`. The Bible reader,
   `/BibleReferences/BibleReader`, leads each of its passage's tags to `/posts` with the tag. Each of
   the footer's topic links leads to `/posts` with its word as a tag (user ruling 2026-09-27). A
   value handed over lands in its own box, never in the free-text query
   (`UI/Pages/Posts.md rule 2.22`). The code departs from this rule:
   `UI/Pages/Home.md §6 items 12 and 15`.

### UI20.8 Authentication *(formerly §20.8)*

The following authentication behaviour is required:

1. Login redirects to the identity provider or displays a username/password form depending on the configured auth strategy.
2. On successful login, a token or session is stored and the user is redirected to the page they originally requested.
3. Logout clears the session and then loads the home page afresh with `location.replace('/')`: a full page load, not a client-side navigation, that takes the place of the reader's page in the tab's history rather than stacking above it. Deleting one's own account ends the session too, and is followed the same way. The page or component that ran the logout or the deletion does the load, as `personalData.tsx` already sends the browser to a URL. A logout or a deletion that fails loads nothing, and the reader stays where they were.

   **Until the load replaces it, the page stays as it was.** The hook that ends the session, `accountService.useLogout` or `manageAccountService.useDeletePersonalData`, does not read the current user again when it succeeds: the load reads who is signed in. Read any sooner, the answer that nobody is signed in reaches `SecuredRoute` (`src/components/securitys/securedRoutes.tsx`), which puts its "Access Restricted" sign-in panel in place of a secured page before the load lands.

   **A failure is never announced twice.** A deletion that fails while the page is still rendered is shown on the delete-personal-data page (`src/pages/account/manage/deletePersonalData.tsx`) as a failure, in the status message's danger alert (`src/pages/account/statusMessage.tsx`), never in its success alert. The status message shows a message as a failure only when it begins with "Error", so the page shows the server's message after `Error: ` where the message does not already begin with "Error", as `disable2fa.tsx` and `changePassword.tsx` mark theirs, and unchanged where it does. Where the server's answer carries no message, such as a `401` or no answer at all, the page shows its own, `Error: Unexpected error occurred deleting user.` Since the page announces every failed deletion that answers while it is shown, `useDeletePersonalData` turns the app's global toast off (`meta.suppressGlobalErrorToast`, §UI20.9.2 departure 2). A logout that fails is announced by the global toast alone, as any failed write is: the header and the user menu show no message of their own.

   **The reader may leave the page before the server answers.** A reader can follow a link to another page of the app while a deletion is in flight. TanStack Query runs the callbacks a component hands to `mutate` only while that component is still rendered (`@tanstack/query-core`, `MutationObserver`), so the page waits on the write itself instead: the promise `mutateAsync` returns, which settles whether or not the page is still shown. A deletion that succeeds after the reader has left is followed by the load all the same, so the tab is loaded afresh wherever the reader has gone, and the guarantee below holds for every deletion the tab hears succeed. A deletion that fails after the reader has left is announced by nothing: the page and its message are gone, and the toast is off. No load follows. Where the server refused the deletion with a `400`, nothing has changed: the account and the session stand, and the reader is still signed in on the page they went to. A `401` means the account or the session was already gone before the request: the endpoint answers one when the cookie names no account, and the host when there is no valid cookie (`ManageAccountApiEndpoints.cs`, `PortalRegistration.cs`). The tab is then a tab whose session ends on the server (*Another open tab is not told*, below). A logout does not meet this case, because the header and the user menu are rendered on every page of the app (`src/components/root.tsx`), so the component that ran a logout is still rendered when the server answers.

   **A deletion whose answer never arrives may have happened.** The endpoint deletes the account before it answers (`ManageAccountApiEndpoints.cs`), so a request that gets no answer can follow a deletion that took place, whether or not the page is still shown. What it can follow is the account's deletion, not the session's end: the cookie the answer would have cleared stays, but names no account, so the current user's read answers that nobody is signed in (`/api/accounts/me`, `AccountApiEndpoints.cs`). The tab hears only a failure: the page shows its own message if it is still shown, and nothing follows if it is not. No load follows either way, and the tab is then a tab whose session ends on the server (*Another open tab is not told*, below). A logout whose answer never arrives has not happened: its only effect is the cookie its answer clears (§SEC18.7.1 rule 5), as the server keeps no session of its own (`AddIdentityCookies`, `PortalRegistration.cs`). The reader is still signed in, as after any failed logout.

   **A page the browser restores from its back-forward cache is hidden at once, and checks who is signed in before it is shown again.** The app moves between routes inside one page, so the back-forward cache holds a page of the app only when the tab left it by a full page load, such as typing an address or following a link to another site. A guard rendered once at the app's root does the check: `RestoredPageGuard`, in `src/components/securitys/` beside `SecuredRoute`. As the page goes into the cache (a `pagehide` event whose `persisted` is `true`), the guard notes which reader the page last read, and hides the page by `display: none` on `<html>`, whether or not anyone is signed in (owner ruling 2026-10-05). When the page is restored (a `pageshow` event whose `persisted` is `true`), the guard hides it in the same task where it is not hidden already, and reads the current user from the server again through `accountService.useGetCurrentUser`. Every frame drawn after that task finds the page hidden. Before that task runs, the browser may draw one frame of the page as it was cached, and that frame is accepted, however the tab left the page (owner rulings 2026-10-05). Where the tab left for another page of the same site by an address typed into the tab, that frame finds the page hidden too, since the page went into the cache hidden. Where it left for another site, or by a link clicked in the page, neither hiding may reach that frame. A snapshot of the page that the browser shows during a back gesture, such as Safari's swipe back or Chrome's back-gesture preview on Android, is accepted too, wherever the tab left for (owner ruling 2026-10-05). The page stays hidden until the guard decides. Hidden means all three of: not drawn, not exposed to assistive technology, and out of reach of pointer and keyboard. The guard hides the document's root element, `<html>`, not the app's container, so that what renders outside the container is hidden too, such as a dialog react-bootstrap's `Modal` renders into `document.body`. Hiding by `visibility` alone does not keep the page from being drawn, since the theme sets `visibility: visible` on parts of it (`Websites/Glory2Him.WebApp/wwwroot/assets/css/style.css`, such as `.offcanvas.show`). `display: none` meets all three parts by itself, since it takes the page out of the render, the accessibility tree and the reach of input. `opacity: 0` keeps the page from being drawn but leaves it in the accessibility tree and in reach of input, so it needs something beside it that takes the page out of both. It is shown again, and resumes as it was, work in progress included, only when a reader was signed in as it went into the cache and the same reader is signed in now. A reaction write still waiting to be sent does not resume: the page drops it as it goes into the cache, so no such write is sent before the guard decides (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §2` rule 11). A resumed page keeps its scroll position and the focus, the field being typed in with its caret: where hiding the page moves either, the guard puts it back as it shows the page again, in the same task, once the page is no longer hidden. A browser does not focus a field that is out of reach of input, and does not scroll a page taken out of the render, such as by `display: none`, so neither can be put back sooner, and putting the focus back must not scroll the page away from the position it had. In every other case it loads itself afresh, and stays hidden until the new page replaces it: a different reader, nobody signed in either time, or a read that is missing or fails. A read the app cannot send because it counts itself offline is a failed read: the guard does not wait for the connection, and the page reloads at once, into the app shell the service worker serves offline (`navigateFallback`, `vite.config.ts`). So a signed-out visitor coming back from another site loses their place, and nobody is shown what the last visitor typed into the sign-in or registration form beyond the first frame the browser may draw of a restored page and a back gesture's snapshot (owner ruling 2026-10-02, qualified by the owner rulings of 2026-10-05). Before the guard decides, a restored page may re-send reads that went stale while it was cached; the server answers them for whoever the session then names, and a reload discards them.

   **The browser's HTTP cache keeps no reader's answer either.** An `/api` answer that sets no cache policy of its own carries `Cache-Control: no-store` (the host, `Program.cs`), as a non-public attachment does (§DOM5.6.2 rule 3). An endpoint whose answer is the same for every caller may set its own, as the contributor read does (`ContributorApiEndpoints.cs`, `public, max-age=60`).

   Together these discard everything the tab read from the server for the reader who left. The query cache lives only in the page's memory, the service worker keeps no reader's API answer (it caches `/api/frontend-configurations` alone, `vite.config.ts`), and the HTTP cache keeps none. So in that tab nothing read for the reader who left is shown again beyond the first frame the browser may draw of a restored page and a back gesture's snapshot, no read answered after the session ended is kept for them, and the app reads who is signed in afresh. The shop's cart, kept in session storage (`cartContext.tsx`), survives the load as it survives any reload: it holds products and quantities, and no reader's identity.

   **Another open tab is not told,** and neither is a tab whose session ends on the server. Such a tab goes on naming the reader it last read until it reads the current user again: at its next load, or once that read has gone stale (five minutes, `accountService.ts`) and the tab regains focus. Until then it shows what it holds, as any open page shows what it rendered, and the server answers every read and write it sends for whoever the session then names.
4. The `Navbar` must reflect auth state — showing login or logout depending on session presence.
5. Role claims from the token must be used to control visibility of role-restricted navigation items.
6. Token refresh or silent renewal must be handled transparently.

#### UI20.8.1 The return after sign-in *(new)*

Rule 2 sends a reader back to the page they originally requested, and §SEC18.7.1 rule 7 says
which return addresses may be followed. The app follows one in four places, one for each way of
signing in: a password on the sign-in page, `/Account/Login`; an authenticator code on the
second-factor page, `/Account/LoginWith2fa`; a recovery code on the recovery-code page,
`/Account/LoginWithRecoveryCode`; and a passkey, from the sign-in page's passkey button. **Each
sends the reader on through the one shared return, `useSignInReturn`
(`DesignFeatures/UI/Hooks/SignInReturn.md`), so the rule is applied in one place and none of them
decides it for itself.** What a page does instead of returning the reader, such as sending a locked-out reader to
`/Account/Lockout` or a reader who needs a second factor to `/Account/LoginWith2fa`, stays the
page's. The external sign-in form
carries the address to the server rather than following it (`src/pages/account/externalLoginPicker.tsx`,
line 38), and that path is not usable from the app today (its comment, lines 30-34).
§SEC18.7.1 rule 7 governs it when it is.

The account pages have no page documents, since §UI20.5.1 counts them among the candidates for
later. So the gaps in how they send a reader on are recorded here and tagged as this file's other
gap lists are (`design.md`, *Conventions*), with their lines at a2b8bcbc. The four close together,
in one pull request, so that every way of signing in moves onto the shared return at the same moment
(user ruling 2026-09-30). The hook's own task, #780, lands in that pull request too. Each keeps its
own task:

1. (#781) **The sign-in page follows its return address itself after a password sign-in.** It
   checks and follows the address inline (`src/pages/account/login.tsx`, line 70) rather than
   through `useSignInReturn`. When a second factor is asked, it hands the address on as
   `ReturnUrl` (line 62), unchanged, which is what §SEC18.7.1 rule 7 asks of a step that carries
   it.
2. (#782) **The second-factor page follows its return address itself.** It checks and follows
   the address inline (`src/pages/account/loginWith2fa.tsx`, line 59) rather than through
   `useSignInReturn`.
3. (#783) **The recovery-code page follows its return address itself.** It checks and follows
   the address inline (`src/pages/account/loginWithRecoveryCode.tsx`, line 45) rather than
   through `useSignInReturn`.
4. (#784) **The passkey button follows its return address itself.** It checks and follows the
   address the sign-in page hands it (`src/pages/account/login.tsx`, line 168) inline
   (`src/pages/account/passkeySignInButton.tsx`, line 23) rather than through
   `useSignInReturn`.

### UI20.9 Services and Brokers *(formerly §20.9)*

| Layer | Responsibility |
| --- | --- |
| `ContentItemBroker` | Calls content item API endpoints. |
| `TagBroker` | Calls tag API endpoints. |
| `ReactionBroker` | Calls reaction API endpoints. |
| `CommentBroker` | Calls comment API endpoints. |
| `BibleReferenceBroker` | Calls Bible reference API endpoints. |
| `ApprovalBroker` | Calls approval, review, and comment API endpoints. |
| `FeedBroker` | Calls feed API endpoints. |
| `AuthBroker` | Handles token acquisition, refresh, and logout. |
| `ContentItemService` | Maps content item API responses to frontend models, composes broker calls. |
| `FeedService` | Builds feed page data from `FeedBroker`. |
| `ApprovalService` | Manages approval queue data and submission actions. |
| `AuthService` | Manages session state, role extraction, and token lifecycle. |

#### UI20.9.1 Departures from the broker skill *(new)*

The React app's brokers depart from `the-standard-reacttypescript-brokers` in the ways below, which are every departure this section approves. Every broker document inherits this section, so a feature records none of these as a deviation of its own. **This section is awaiting the owner's approval.**

**Scope.** This section records the shape of the brokers the app has. It approves no logic in a broker. Rule tsr-brokers-008 — *"Brokers MUST NOT contain business logic (filtering, validation, transformation beyond format conversion)."* — is the repository's own rule too (CLAUDE.md, *Brokers hold no logic and get no unit tests*). The owner ruled on 2026-10-01 that the logic some brokers hold today, such as query conditions, ordering, paging and chunking, is a defect, which #814 moves out. That includes `ApiBroker`'s connectivity interceptor (`apiBroker.ts`). A broker converts what it is handed to the wire's format and nothing more, and it has no unit tests. Code that reaches storage, the clock or the network without going through a broker (tsr-brokers-001, -006 and -010) is not approved here either, and nothing here covers it. How services pass a broker's error on (the services skill's tsr-services-003 and -017) is §UI20.9.2 departure 2's, not this section's. `apiBroker.globals.ts` holds the React Query client's configuration and is not a broker, whatever its prefix says.

The app's broker shape is a default-exported class, `<Entity>Broker`, in `src/brokers/apiBroker.<resource>.ts`. It holds an `ApiBroker` and has one member per call, named as departure 2 says (`apiBroker.reactions.ts`, `apiBroker.contentItems.ts`).

1. **tsr-brokers-003 — *"Brokers MUST map external exceptions to broker-layer exceptions with meaningful messages."* — and tsr-brokers-015 — *"Brokers MUST catch external exceptions and wrap them in broker-specific exceptions."*** No entity broker catches or wraps. A failed call rejects with the `AxiosError` that axios raised, unchanged. **Why:** the app's error contract is the `AxiosError` itself. The code that turns a failure into what a reader sees first tests it with `axios.isAxiosError`, then reads the server's answer off it: the problem body in `toContentItemApiFailure.ts`, `apiErrorMessage.ts` and `statusMessage.tsx`, and the status in `contributorService.ts`. A broker-layer exception would have to carry the response through for any of that to work, which is the same error under another name. And a wrapped error from some brokers alone would give every shared consumer two shapes to handle. **Instead:** the error leaves the broker unchanged. The app's global handler announces it (`apiBroker.globals.ts`) unless the caller opts out with `meta.suppressGlobalErrorToast`, and a view that needs the detail reads it off the `AxiosError`.
2. **tsr-brokers-004 — *"API broker methods MUST use standard HTTP verbs: Get, Post, Put, Delete."*** Many existing members are named for what they do rather than the verb they send, among them `LoginAsync`, `AddApprovalSettingAsync`, `SearchContentItemsAsync` and `GetCreationOptionsAsync` (which sends a `POST`). **Why:** a convention only, like departure 3. The names grew with the app, and no single rule sorts them: some name an action, some a resource operation, some neither. Renaming them and their callers would change no behaviour. The owner approves this on cost, not on a technical ground. **Instead:** an existing member keeps its name. A **new** member takes the HTTP verb it sends, `<Verb><Entity>Async` (`PostAssociationAsync`), and so does an existing member when a task already changes its signature.
3. **tsr-brokers-005 — *"Broker method names MUST follow pattern: `{verb}{Entity}Async` (e.g., `getPatientAsync`, `postPatientAsync`)."*** API broker members are PascalCase: `GetApprovedReactionsAsync`, `PostAssociationAsync`. **Why:** a convention only. Every API broker member in the app has been PascalCase from the start, and renaming them and their callers would change no behaviour. The owner approves this on cost, not on a technical ground. **Instead:** API broker members are PascalCase. Their names follow departure 2. The toast broker's functions (`toastError`, `toastSuccess`) are not API members, and this departure doesn't cover them.
4. **tsr-brokers-009 — *"Brokers MUST be injected via dependency injection, not instantiated directly in services."* — tsr-brokers-014 — *"Brokers SHOULD expose interfaces for testing and mocking."* — and, from the services skill, tsr-services-005 — *"Foundation services MUST use dependency injection to receive broker instances."*** A foundation service hook that calls a broker constructs its own (`const contentItemBroker = new ContentItemBroker();` in `contentItemService.ts`). **Why:** a React foundation service is an object whose members are hooks, not a class with a constructor, so there is nowhere to inject a broker. Passing one in through every call site, or through a context provider, would only rebuild the seam the tests already have. That seam is the broker module, which a service's tests replace with `vi.mock` (`contentItemService.test.tsx`, `approvalCommentService.test.tsx`), so the service is tested against a substitute broker as injection would allow. For the same reason no broker exposes an interface: the module is what a test substitutes, so an interface would have no consumer. **Instead:** the hook constructs the broker, and the tests substitute the module. §UI20.3 rule 5's "injected into services" is read in this sense.
5. **tsr-brokers-011 — *"Brokers MUST be organized by external dependency type: apis/, storages/, loggings/, datetimes/."* — and tsr-brokers-012 — *"Broker class names MUST follow pattern: `{Entity}{Type}Broker` (e.g., `PatientApiBroker`, `LocalStorageBroker`)."*** Brokers live flat in `src/brokers/`. The dependency type is the file name's prefix (`apiBroker.<resource>.ts`, `toastBroker.<kind>.ts`, `hubBroker.<hub>.ts`), and the class is `<Entity>Broker` (`ReactionBroker`, `AssociationBroker`, `LiveUpdateBroker`). **Why:** the brokers the app has come in three kinds, API, toast and hub (`DesignFeatures/UI/Brokers/LiveUpdateBroker.md`). A folder per kind would hold one prefix's worth of files that the prefix already groups. The class name drops the type because an entity has one broker, so the entity alone names it, and the file name's prefix says which kind it is. **Instead:** §UI20.4's `brokers/` folder, the type in the file name's prefix, and the class named for its entity.
6. **tsr-brokers-019 — *"Brokers MUST NOT call other brokers — composition happens in services."*** Every entity broker holds and calls `ApiBroker`. **Why:** `ApiBroker` is not a broker another broker composes with. It is the app's one wrapper over axios, and it carries the request configuration (`withCredentials`) for every call. An entity broker adds a route and a shape to it and nothing else, so nothing is composed. Calling axios from each entity broker would repeat the configuration in every one. **Instead:** entity brokers call `ApiBroker`, and no production code outside `src/brokers/` does.
7. **tsr-brokers-021 — *"API brokers SHOULD accept CancellationToken-equivalent (AbortSignal) for request cancellation."*** No broker member takes an `AbortSignal`. **Why:** no caller cancels a request today. React Query offers each query function a signal, but no service hook under `src/services/` passes it on. A parameter no caller fills would be unused surface. **Instead:** none until a caller needs cancellation. The first one adds the parameter to the members it calls and to `ApiBroker`.

#### UI20.9.2 Departures from the services skill *(new)*

The React app's foundation services depart from `the-standard-reacttypescript-services` in the ways below, which are every departure this section approves. Every React foundation service document inherits this section, so a feature records none of these as a deviation of its own. **This section is awaiting the owner's approval.**

**Scope.** This section records the shape of the foundation services under `src/services/foundations/`. A React foundation service is an exported object whose members are hooks (§UI20.9.1 departure 4). Each file holds one service (tsr-services-026), and no service holds UI logic (-008). Rule tsr-services-005 is §UI20.9.1 departure 4's. Nothing here approves logic in a hook that its task does not name.

**Outside this section:** `passkeyService.useAddPasskey` and `passkeyService.usePasskeySignIn`. They construct no broker: they run the browser's WebAuthn ceremony through `src/hooks/usePasskeys.ts`, which raises its own error type, `PasskeyCeremonyError`. Nothing here records or approves how those two hooks depart from the skill, and no statement below is about them. #833 records them.

1. **tsr-services-002 — *"Foundation services MUST validate all inputs before calling brokers."* — tsr-services-011 — *"Services MUST validate for null/undefined inputs."* — tsr-services-012 — *"Services MUST validate required string fields are not empty."* — tsr-services-013 — *"Services MUST validate required ID fields exist before retrieval operations."* — tsr-services-014 — *"Services SHOULD validate structural integrity (e.g., valid email format, date ranges)."* — tsr-services-015 — *"Validation exceptions MUST include the field name and reason for failure."* — tsr-services-016 — *"Validation MUST happen before broker calls, not after."* — and tsr-services-018 — *"Validation failures MUST throw ValidationException with descriptive messages."*** No hook validates what it is handed or throws a validation error. Some reads are not sent while their id is empty (`enabled: enabled && userId.length > 0` in `contributorService.ts`), which throws nothing. **Why:** the server validates every request, and its answer is what the reader is shown: a 400 carries a problem body that the app reads off the `AxiosError` (`toContentItemApiFailure.ts`, `apiErrorMessage.ts`). The server is the authority on what a request must carry, as `useModifyContentItem` says of an item (`contentItemService.ts`), so a copy of its rules in a hook would be a second copy to keep true. **Instead:** a hook validates nothing it is handed, and the server validates the request the hook sends. What a hook builds from its input, such as an id it mints or a flag it derives (`useAddApprovalComment` in `approvalCommentService.ts`), is logic its task names, not validation.
2. **tsr-services-003 — *"Foundation services MUST map broker exceptions to service-layer exceptions (ValidationException, DependencyException, ServiceException)."* — tsr-services-004 — *"Service methods MUST use try-catch pattern when calling brokers."* — tsr-services-007 — *"Services MUST implement TryCatch wrapper for exception mapping and logging."* — tsr-services-010 — *"Services MUST define clear exception types: ValidationException, DependencyException, ServiceException."* — tsr-services-017 — *"Broker exceptions MUST be caught and wrapped in DependencyException."* — tsr-services-019 — *"Unexpected service errors MUST be wrapped in ServiceException."* — tsr-services-020 — *"All service exceptions MUST preserve the inner exception for debugging."* — and tsr-services-021 — *"Exception messages MUST be user-facing and actionable."*** No hook maps or wraps its broker's error, no foundation service defines an exception type, and nothing logs a failure: the app has no logging broker. A failed broker call rejects with its `AxiosError`, unchanged. The one `try`/`catch` (`contributorService.ts`) turns a 404 into `null`, an answer rather than a failure, and rethrows every other error unchanged. **Why:** §UI20.9.1 departure 1's. The app's error contract is the `AxiosError`, and the code that turns a failure into what a reader sees reads the server's answer off it, so a service exception would have to carry it through under another name. **Instead:** the error leaves the hook unchanged. The app's global handler announces it with its own message (`apiBroker.globals.ts`) unless the hook declares `meta.suppressGlobalErrorToast`, and a view that needs the detail reads it off the `AxiosError`. **A hook with no logic of its own has no service-failure path.** A hook whose own code does nothing but call one broker member with what it was handed and, for a write, invalidate queries has nothing of its own to fail: its task's exception tests are the broker's failure, and need not rule the service path out. The options that only configure the call (its query key, `enabled`, `staleTime`, `retry`, `meta`) are not logic for this purpose. Code that decides what is sent, what comes back or what the cache holds is logic, and the hook's task covers that logic's failures: choosing between broker members, building a request, splitting one request into several, writing the cache on a condition (`useLogin` in `accountService.ts`), or the 404 rule in `contributorService.ts`.
3. **tsr-services-022 — *"Foundation services MUST be in `services/foundations/{domain}/` directory."* — tsr-services-023 — *"Service class names MUST follow pattern `{Domain}Service` (e.g., `PatientService`)."* — tsr-services-024 — *"Service interface names MUST follow pattern `I{Domain}Service` (e.g., `IPatientService`)."* — and tsr-services-025 — *"Services SHOULD expose interfaces for testing and dependency inversion."*** Foundation services live flat in `src/services/foundations/`, one file per domain, and each is an exported object named in camelCase (`contentItemService`), not a class, with no interface. **Why:** a React foundation service is an object whose members are hooks, not a class (§UI20.9.1 departure 4), so there is no class to name, and its name is camelCase because it is a value, not a type. A folder per domain would hold the one service file and at most its tests, which the file name already groups. An interface would have no consumer: a test that needs a substitute service replaces the service's module with `vi.mock` (`contentItemFeedPages.test.tsx`), as a service's own test replaces its broker's module (§UI20.9.1 departure 4). **Instead:** `src/services/foundations/<domain>Service.ts`, exporting `<domain>Service`.
4. **tsr-services-006 — *"Service method names SHOULD mirror broker method names (e.g., broker: `getPatientAsync` → service: `retrievePatientByIdAsync`)."*** A hook's name is `use` and what it does for its caller, and it mirrors a broker member's name only where the two happen to agree: `useGetContentItemById` calls `GetContentItemByIdAsync`, but `useModifyContentItem` calls `PutContentItemAsync`, and `useCreateOrUpdateContentItemSettingOverride` calls `AddContentItemSettingAsync` or `UpdateContentItemSettingAsync`. **Why:** React's rules of hooks require a hook's name to start with `use`, and the app's lint enforces them (`eslint-plugin-react-hooks`, in `eslint.config.js`). Past that prefix, a convention only: the names grew with the app, no single rule sorts them, and renaming the hooks and their callers would change no behaviour. The owner approves this on cost, not on a technical ground. **Instead:** a hook is named `use` and its operation. An existing hook keeps its name.

### UI20.10 Live updates *(new)*

The live connection tells open pages that something they show has changed (`DesignFeatures/LiveUpdates.md`, §SEC14.8). These rules hold for every page, so no page document restates them. They were designed under #702 on 2026-10-06, under the owner's rulings of that day.

1. **Each tab holds one connection, opened at the app's root for every reader.** `Root` (`src/components/root.tsx`) opens it once, through `liveUpdateService.useLiveUpdates` (`DesignFeatures/UI/Foundations/LiveUpdateService.md §1`), whether the reader is signed in or not, and it stays open while the tab does. No page and no component opens a connection of its own.
2. **A page does nothing to hear a change.** A message makes stale the reads it covers, and TanStack Query reads a stale read again wherever a page is showing it, through the read the page already makes. Which reads a message covers is the foundation service's (`DesignFeatures/UI/Foundations/LiveUpdateService.md §1`). So a page rule that a change reaches the open page without a reload, such as `UI/Pages/Home.md rule 2.24`, is met by this section, and the page needs no code of its own for it.
3. **A live update adds to the refresh a page already has, and replaces none of it.** The read a page makes after its own write stays, and so do the query library's own triggers — focus, reconnect and staleness — and the review round's 15-second refresh (`UI/Pages/ContentItemModerationDetailPage.md rule 2.12`). A read a message prompts is an ordinary read of the reaction summaries, to which the engagement hook's rule on the reader's overlay applies unchanged (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §2` rule 8).
4. **The reader is told nothing about the connection** (owner ruling 2026-10-06). While it is down, pages keep their own refresh. When it comes back, every read a message covers is read again, because a message may have been missed. A dropped connection costs freshness, and never correctness.

The gap in building it is recorded here and tagged as this file's other gap lists are (`design.md`, *Conventions*):

1. (#910) **`Root` opens no live connection.** Rule 1: `Root` opens the tab's one connection through `liveUpdateService.useLiveUpdates`, for every reader. It calls no such member (`src/components/root.tsx` at 65aee696). Building it closes the page's share of the seven page gaps whose share needs no page code (rule 2): `UI/Pages/Home.md §6 item 11` and the six items that one names. It closes `DesignFeatures/Likes.md` rule 12 in the same way.
