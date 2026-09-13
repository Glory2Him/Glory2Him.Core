# UI / UX Design

Carries `G2H Design.md` §20 "UI / UX Design" — the React application's pages,
components, navigation and authentication design.

Section numbers below carry a **`UI` prefix** and are otherwise the numbers
these sections already had: an old `§20.N` is now `§UI20.N`, and nothing was
renumbered, reordered, merged or split in the move. That is the
prefix-preserving rule of [`Documentation/Design/Split.md`](Split.md) §S1.2. It
means this file starts at `UI20` rather than at 1 and its numbering is not
contiguous with the other area files, so the contents list below is what makes
that gap read as a table of contents rather than as missing content. The other
`Documentation/Design/*.md` files carry their own prefixes — `ARC`, `APR`,
`DOM`, `SEC`, `EVN` — so a bare `§UI20.6.1` is unambiguous once they exist.
Until they do, the map at the top of [`G2H Design.md`](../G2H%20Design.md) is
what resolves a citation into a file not yet written.

**`Events.md` is the one file a citation into it cannot be derived for.** It
renumbered rather than prefix-preserved, so an into-Events citation is looked up
in that file's own *(formerly §10.X)* annotations instead of having a prefix
applied to the number it already had. The one such citation this file carries —
`§EVN18`, in §UI20.6.1 — was resolved that way, and `§EVN10.17` would have been
the wrong answer.

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
  - [UI20.6 Components](#ui206-components-formerly-206)
    - [UI20.6.1 ReviewPanel — contract and dependencies](#ui2061-reviewpanel--contract-and-dependencies-formerly-2061)
    - [UI20.6.2 ContentItemPanel — contract and dependencies](#ui2062-contentitempanel--contract-and-dependencies-formerly-2062)
    - [UI20.6.3 ReviewCommentPanel — contract and dependencies](#ui2063-reviewcommentpanel--contract-and-dependencies-formerly-2063)
  - [UI20.7 Navigation](#ui207-navigation-formerly-207)
  - [UI20.8 Authentication](#ui208-authentication-formerly-208)
  - [UI20.9 Services and Brokers](#ui209-services-and-brokers-formerly-209)

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
5. Brokers are the lowest-level HTTP callers — one per API area — and are injected into services.
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

### UI20.6 Components *(formerly §20.6)*

Planned reusable components based on the Blogzine template:

| Component | Purpose |
| --- | --- |
| `Navbar` | Top navigation bar with logo, links, search, and auth state. |
| `Footer` | Site footer with links and attribution. |
| `ContentCard` | Feed card for a single content item — header image (§DOM4.9), title, type, excerpt, publish date. |
| `ContentCardGrid` | Responsive grid of `ContentCard` components. |
| `ContentCardFeatured` | Hero-style featured content card. |
| `ContentDetail` | Full content item display — body, author, tags, reactions, comments, Bible references. |
| `TopicCard` | Card for a topic landing page preview. |
| `TagBadge` | Individual tag badge. |
| `TagList` | List of `TagBadge` components. |
| `ReactionBar` | Row of available reactions with counts. |
| `CommentList` | List of approved comments for a content item. |
| `CommentForm` | Authenticated form to submit a comment. |
| `BibleReferenceBlock` | Display block for a Bible reference and optional scripture text. |
| `ApprovalStatusBadge` | Badge showing current approval status. |
| `ApprovalReviewForm` | Form for a reviewer to submit an approval or rejection decision. |
| `ApprovalCommentForm` | RETIRED — superseded by `ReviewCommentPanel` below, which is the whole thread rather than the box alone. A separate add-only form would have had to re-decide the same three gates. |
| `ReviewPanel` | The approval round rendered: reviews, the viewer's own vote, block reasons, bypass, the publisher-tier decision, and review requests (§UI20.6.1). |
| `ReviewCommentPanel` | The round's conversation: the box and its Comment/Question choice, the thread newest-first, the settled tick on asks, and the author's Edit and Delete (§UI20.6.3). |
| `ContentItemPanel` | One content item on whichever face the moment asks for — the add and edit templates and the per-type view templates, field-shaped per content type and gated per §SEC18.6 (§UI20.6.2). Paste-to-upload for inline images (§DOM5.6.6) is not part of it yet. |
| `HeaderImagePicker` | Header-image candidates for a content item — upload, list, promote the default (§DOM4.9). |
| `ShareBar` | Share buttons composing real short-link URLs (§DOM19.7). |
| `SearchBar` | Search input with debounce. |
| `Pagination` | Paginated navigation for feed and topic child lists. |
| `PrivateRoute` | Route guard for authenticated routes. |
| `RoleRoute` | Route guard for role-restricted routes. |
| `LoadingSpinner` | Generic loading indicator. |
| `ErrorMessage` | Generic error display. |

#### UI20.6.1 ReviewPanel — contract and dependencies *(formerly §20.6.1)*

`ReviewPanel` is a **pure presentation component**: props in, events out, no fetching, no sockets. Every gate it renders is a courtesy — the orchestration re-decides votes, decisions, bypass and requests against the stored rows (§SEC14.6). Wherever the server has already answered a question per caller (`CanApprove`, `IsBypassAllowedForCurrentUser`), the verdict's answer is used verbatim rather than re-derived from role names; the remaining render gates compose roles per §SEC18.6, capability-last and plural.

**The consumer owns freshness.** The panel shows the world as of the last props it was handed, so its consumer must re-fetch and re-render when the round changes underneath it — another vote cast, a comment added or resolved, a decision or auto-approval, a request made or answered. SignalR, polling, or a refetch after each event callback are all acceptable; without one of them the panel is simply stale. Server side, the EventHighway facts the approval workflow already publishes (§EVN18) are the signal a push channel would forward — a future SignalR hub subscribes to those; it does not add new facts.

**Direct API dependencies** (called by the consumer, never the component):

| Concern | Endpoint |
| --- | --- |
| The outcome section | `GET api/Approvals/{entityType}/{entityId}/Verdict` (§ARC16.7.2 — moderation tier only, so the read-only view gets the status pill without block reasons) |
| The decision | `POST api/Approvals/{entityType}/{entityId}/Decision` (bypass reason mandatory when bypassing) |
| The viewer's vote | `POST` / `PUT api/ApprovalReviews` |
| The review rows | `GET api/ApprovalReviews` filtered by `ApprovalId` |
| The request rows and picker | The §ARC16.7.4 candidates and review-request endpoints |
| The names on its reviewers and its invitations | `GET api/Approvals/{entityType}/{entityId}/ReviewerDisplayNames` — the §ARC16.7.4 resolver, asked once for the round. Candidates are NOT in it: the candidates read above already carries a display name for every person it offers, and both are composed by the same method, so the two never disagree |

**Indirect dependencies:** the signed-in identity and roles (`/api/accounts/me` via the auth context) for the render gates, and the approval's status for the frozen/live switch — deliberately a prop of its own, because the read-only view has a status to show and no verdict to read it from.

#### UI20.6.2 ContentItemPanel — contract and dependencies *(formerly §20.6.2)*

`ContentItemPanel` is a **pure presentation component**: props in, events out, no fetching, no mutation, no sockets. It is the one dispatcher for a content item's every face: handed a settings collection and no item it renders the add template (`ContentItemAddPanel`); Edit taken in place — or `mode="edit"` passed — renders the edit template (`ContentItemEditPanel`); otherwise the item renders through the view template registered for its content type (`ContentItemDefaultPanel`, or an override such as `ContentItemQuotesPanel` deriving from it via `contentSlot`). `ContentItemListPanel` composes the search bar and the scrolled results, rendering this same panel for every element — one family, one tree, no second detail component to keep in sync. Every face runs on the family's one projection: a self-contained element carrying the item and its §DOM6.4 winning setting, so a list element hands to a detail surface — and seeds its editor — with no further read, and an update is one element swapped by the consumer.

**Security posture.** Every gate it renders decides what to SHOW and nothing more. The foundation and processing services re-decide add, modify and remove against the stored row (§SEC14.6, §SEC14.7 posture A), and must: a hidden button is a courtesy to the reader, never an authorization boundary.

**Where Edit goes is the page's wiring.** A page listening on `onEditClick` alone gets the event and routes to its own edit surface, carrying its back context; a page that switches `showEditSection` on and listens on `onModified`/`onRemoved` gets the editor **in place** — the owner's Edit swaps the card for the edit template, and both a committed Save and Cancel swap the card back (`mode="edit"` lands straight on the editor, still subject to the same gates). What the card then shows is the consumer's element: the page persists and swaps it, so the amendments appear; Cancel discards the draft and reopening seeds from the original.

**`showEditSection` is the surface switch, ahead of every role check**, and it is off by default — the safe posture `AssociationPanel` takes with `showModerationActions`. While it is off the panel renders no action affordance at all: no `Edit`, no `Delete`, no route into the editor however the roles fall, and the edit template refuses outright rather than downgrading — the read surface belongs to the view templates. A public page renders the panel without it and gets a view surface that cannot accidentally become an edit one; a profile or admin area switches it on and the role gates below then decide, per action, what is actually shown. It only ever subtracts.

**Role composition** follows §SEC18.6 — capability last and plural, resolved against the content type IN PLAY (the selected type while adding, the item's own type when reading or editing). Every set is an overridable comma-separated prop in which `{ContentType}` resolves to the enum member name, and `[OWNER]` names the item's contributor, matched on the account id and never on a display name.

| Gate | Default |
| --- | --- |
| blocked by | `ReadOnly`, `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` |
| add | empty — any authenticated reader, since there is no `Contributor` role |
| edit | `[OWNER]`, `Publishers`, `ContentItem-Publishers`, `ContentItem-{ContentType}-Publishers`, `Administrators` — the non-owner half further confined to `Draft` / `Submitted` |
| delete | `[OWNER]`, `Administrators` — removal is a takedown, not a moderation step (§SEC14.7 posture A.3) |

**The block set is asked first and outranks every grant**, `[OWNER]` included: a contributor holding `ContentItem-Devotional-ReadOnly` sees no `Edit` and no `Delete` on their own devotional, and no add surface for that type, while stories and quotes stay open to them. The narrow block therefore lands on the **picker**, not only on the form: a blocked tile renders disabled with its reason on it, and only a reader blocked from every available type loses the form. The `Reviewers` tier appears in no set at all — a reviewer reviews.

The panel's block set is still a render courtesy (§SEC14.6), but it is no longer courtesy alone: `ContentItem-{ContentType}-ReadOnly` is a real role now — seeded, and refused by the foundation, the processing layer and the approval surface alike (§SEC18.6 rule 2). The two answers agree by construction rather than by coincidence, because both compose the same name from the row's own content type.

**The content type is create-only** (§ARC12.4.1 rule 7a), so only `add` offers the choice: the edit template wears the same tile layout with every tile disabled and the item's own still selected — one look for both writing faces — falling back to a frozen chip when no default rows were handed over.

**Which fields exist is per content type and is passed in, never fetched — and the panel resolves the EFFECTIVE row itself.** The consumer hands over the `ContentItemSetting` rows it already holds and the most specific one wins, exactly as §DOM6.4 and §ARC12.5.2 rules 1–2 require: an item-level override takes **full precedence** over the content type default, and a soft-deleted row is excluded from resolution entirely (§DOM6.6). The override is matched on the **item** as well as the type, so a mixed collection is safe — one item's override is never applied to another's. `add` can therefore only ever resolve a default, because an override belongs to an item that does not exist yet.

What the panel reads off the resolved row is the **field shaping and the type's presentation**: `HasTitle`, `HasAuthor`, `ContentTypeName`, `ContentTypeDescription`, `ContentTypeIconCssClass`. `HasTitle` and `HasAuthor` govern every face — the inputs in `add` and `edit`, and the title and author on the view templates (which additionally require the item to carry a value). The `Max*Length` ceilings cap the fields client-side: the input refuses further typing, and a stored value already over a lowered ceiling is refused at submit with the limit named. **A field the reader cannot see contributes nothing, and the row keeps whatever it already had.** One rule, settling both halves. On an amendment it means hiding is never destructive: a value already on the row survives an edit it was not shown for, so a setting changed after the item was written cannot silently blank it. On a contribution it means the opposite is equally true — a title typed under one content type and then abandoned by picking another whose setting has no title is **not** posted, because the contributor can no longer see it, the type is create-only, and no read surface would ever show it again. Where no row resolves at all there is no flag to obey, and the panel shows whichever of the two the item carries. **The page above the panel obeys the same rule**: a heading that named a title the panel deliberately hides would make the suppressed value the loudest thing on the screen, so `/posts/{id}` resolves the effective row through the same shared projection and falls back to the type's name.

**`SharePermission` is the exception, and drops rather than persisting.** It is hidden by the contributor's own answer to a question in front of them — not by a setting they never chose — so "the row keeps what it had" does not apply: a note reading *permission granted by the author* stored against an item its contributor has just declared `Owned` is a provenance claim they withdrew. Nothing server-side correlates the two (the foundation length-checks `SharePermission` and no more), and no read surface renders it once the basis has moved, so preserving it would file a contradiction nobody can see or clear. The field, the placement of its validation messages and what is submitted all read the same flag, so the three cannot disagree. The **facet pairs** (§DOM6.5 — `TagsAllowed`/`ShowTags` and the same for comments, reactions, links, attachments and bible references) govern surfaces this panel does not own; the panels rendering beside it read those, against this same effective row.

**The picker offers the content type defaults carrying `IsAvailableAsGeneralUserContribution`**, which is exactly the question a tile asks. An override is never a tile however the consumer's collection arrived **The tiles are ordered by the rows' own `SortOrder`** (§DOM6.6), ascending, so the order a contributor meets the types in is a decision recorded on the setting rather than an accident of the order the consumer's read answered with. The panel sorts what it is handed — it is a presentation component, so it does not depend on the consumer having ordered the collection — and the type it lands on by default is the first tile in that order. A tie keeps the order the rows arrived in.

**The consumer owns persistence and freshness.** The panel raises `onAdded`, `onModified`, `onRemoved` and `onCancelled`, and does nothing else: the page decides whether `onModified` is a `PUT` or a fork of a new version on a terminal item (§DOM3.4 rule 16), swaps the amended element so the closed editor's card shows it, and re-fetches whenever the item changes underneath it. The panel shows the world as of the last props it was handed.

**Validation comes back from the API, not from the browser — with two ruled exceptions the panel is the right surface for.** A permission basis makes the `SharePermission` box mandatory (a claim of permission with no permission named is not a submission the product accepts), and the effective setting's `Max*Length` ceilings are enforced as above; both speak through the same field-issue channel the server's messages use. Everything else the panel leaves to the server — a second opinion in the browser would drift from it. The consumer submits, and hands the `errors` dictionary of the returned `ValidationProblemDetails` back to the panel as `validationIssues`; the panel matches those keys onto its fields case-insensitively (they are the server's parameter names) and summarises anything it cannot place rather than dropping it. The failure also raises a timed notification through the existing toast framework, carrying the API's own reason rather than a generic one.

**Associations render beside it, never within it.** Tags and bible references belong to `AssociationPanel` and its two wrappers, which have their own approval and role rules and need an item to associate to — so they cannot render on an add surface at all. Approval controls belong to `ReviewPanel` (§UI20.6.1).

**Direct API dependencies** (called by the consumer, never the component):

| Concern | Endpoint |
| --- | --- |
| The type picker and field shaping | `GET api/ContentItemSettings` (`[AllowAnonymous]`; `$filter=contentItemId eq null` for the defaults, plus `isAvailableAsGeneralUserContribution eq true` for the contribution surface). A page rendering one item may also pass that item's override row alongside the defaults — the panel resolves which wins. |
| The contribution | `POST api/ContentItems` — seven caller-supplied members only (`ContentType`, `Title`, `Author`, `Content`, `ShareabilityBasis`, `SharePermission`, `ApprovalStatus`); the processing service mints the identifiers, hashes the content and lands the row unpublished at the status the caller asked for — `Draft` or `Submitted` and nothing else (§APR9.7.1 rule 1), which is what the panel's "Submit as" row answers — and the foundation beneath it stamps the audit trail |
| The item | `GET api/ContentItems/{contentItemId}` (`[AllowAnonymous]` — the service's own visibility filter decides what a caller may see) |
| An amendment | `PUT api/ContentItems`, or the version fork on a terminal item |

**Indirect dependencies:** the signed-in identity and roles (`/api/accounts/me` via the auth context) for the render gates, and the item's `ApprovalStatus` for the non-owner edit gate.

**Consumers.** `/posts/contribute` renders the add face and owns the `POST`, the redirect to `/myposts/{contentItemId}`, the notification and the validation readback. `/posts/{contentItemId}` renders the view face with `showEditSection` left off; `/myposts/{contentItemId}` renders it with editing in place; and the feeds (`/`, `/posts`, `/myposts`, `/Admin/Posts`) render every element through this same panel via `ContentItemListPanel`.

#### UI20.6.3 ReviewCommentPanel — contract and dependencies *(formerly §20.6.3)*

`ReviewCommentPanel` is a **pure presentation component**: props in, events out, no fetching, no mutation, no sockets. It is the conversation the round is made of — the thing `ReviewPanel` can only report as a count of unresolved comments.

```
ReviewCommentPanel                 the thread, and who may do what to it
├── ReviewCommentAddPanel          the box, the Comment/Question radios, Clear / Save
└── ReviewCommentResultsPanel      the rows, newest first, scrolled rather than paged
    ├── ReviewCommentViewPanel     READ:  author, chip, timestamp, resolve tick, Edit / Delete
    └── ReviewCommentEditPanel     EDIT:  the words and the type, Save / Cancel
```

The dispatcher owns everything the faces share — the ordering, the ownership gate, the resolve tier and the `ReadOnly` veto — and the templates render what it decides, the way `ContentItemPanel` is built (§UI20.6.2).

**The type is stated, not inferred.** The add face's radio pair writes `ApprovalCommentType` and derives the birth value of `IsResolved` from it — a `Question` is created outstanding and holds the approval shut, a `Comment` is created settled and never blocks. That is §APR7.8's own sentence rather than a rule the client invents: the flag carries no SHAPE rule, so a caller who said nothing would make every remark a blocker. The client is agreeing with the gate rather than being it — the server refuses the settled ask on both the add and the amend path.

**Retyping moves the flag with the type, and only then.** The edit face offers the same pair, so an amendment that changes `ApprovalCommentType` re-derives `IsResolved` exactly as birth does — a remark retyped as a question is *outstanding*, a question retyped as a remark is *settled*. Sending the new type over the stored flag was the earlier shape and it could not work: a remark is born settled, so retyping one produced a question already resolved, which is the single pairing the amend gate refuses outright — the save came back a flat refusal and only the words could ever be edited. Where the type does **not** move the flag is left alone, because a settled ask may be edited by its author and re-deriving would silently re-open it; resolving and re-opening answer to their own operation and its tier (§SEC14.7 rule 5).

**Three gates, and the sanction reaches them differently.** Adding is any authenticated reader (§SEC14.7 posture D rule 5 — submitters converse in review threads), stopped by the global `ReadOnly` alone. Amending and withdrawing are the **author alone**; no role widens them. The settled tick renders on an **ask only**, to the author or the publisher tier for the entity, and is stopped by a `ReadOnly` at any scope the entity composes — because that one control clears a §APR8.5 gate (§SEC18.6 rule 3). Every one of them decides rendering only; the foundation re-decides each write against the stored row (§SEC14.6).

**The consumer owns freshness, and here that is a requirement rather than a note.** Two moderators working the same submission is the case this surface exists for, so the collection must be kept moving — `approvalCommentService.useGetApprovalComments` polls and refetches on focus. It also owns the confirmation: the panel raises which row the reader wants gone, and the page asks "Are you sure?", the same split `ContentItemSettingsPanel` makes for Remove Override.

**Direct API dependencies** (called by the consumer, never the component):

| Concern | Endpoint |
| --- | --- |
| The thread | `GET api/ApprovalComments` filtered by `ApprovalId` and `IsDeleted eq false` |
| A new comment | `POST api/ApprovalComments` — the id minted client-side, the audit values stamped server-side |
| An amend | `PUT api/ApprovalComments` — the whole row that was read, since four fields are pinned against storage |
| A withdrawal | `DELETE api/ApprovalComments/{id}?deletionReason=` — the SOFT delete, which is what leaves the §APR8.5 block |
| The settled flag | `POST api/ApprovalComments/{id}/Resolve?isResolved=` — always sent, since the endpoint binds it required |
| The approval id, and the author names | The §ARC16.7.2 verdict and the §ARC16.7.4 resolver, both already read for `ReviewPanel` |

Every write invalidates the thread **and** the verdict: an outstanding comment is one of the block reasons `ReviewPanel` prints in the column beside it.

**Where it lands.** Beneath the bible references on `/Admin/Posts/{id}` — in the column with the thing being discussed, not in the decision column, which has to stay readable at a glance while a thread grows without limit.

### UI20.7 Navigation *(formerly §20.7)*

Navigation must support three levels:

1. **Public routes** — accessible to unauthenticated users. Includes feed, content item views, topic pages, and search.
2. **Authenticated routes** — require a valid session. Includes submit, edit, profile, and approval queue.
3. **Role-restricted routes** — require a specific role such as `Reviewers` or `Administrators`. Includes approval actions and admin dashboard.

Route guards should redirect unauthenticated users to the login page and unauthorised users to a 403 or not-found page.

### UI20.8 Authentication *(formerly §20.8)*

The following authentication behaviour is required:

1. Login redirects to the identity provider or displays a username/password form depending on the configured auth strategy.
2. On successful login, a token or session is stored and the user is redirected to the page they originally requested.
3. Logout clears the session and redirects to the home page.
4. The `Navbar` must reflect auth state — showing login or logout depending on session presence.
5. Role claims from the token must be used to control visibility of role-restricted navigation items.
6. Token refresh or silent renewal must be handled transparently.

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
