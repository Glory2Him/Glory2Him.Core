# 1. BibleReferenceAssociationPanel

- **Kind:** User story — child component of AssociationPanel
- **Parent:** [AssociationPanel.md](AssociationPanel.md)
- **Children:** none
- **Composes:** [AssociationPanel.md](AssociationPanel.md) — its parent, and nothing else
- **Used by:** the pages in section 6
- **Inherits:** `UI/Components/AssociationPanel.md §2–9` (every rule not restated here), §SEC18.6, §DOM4.7, §DOM6.1, §DOM6.5, §DOM6.9, §DOM6.10, §UI20.6.4, §UI20.6.5, §UI20.6.6, §APR9.9
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/associations/bibleReferenceAssociationPanel.tsx`; the href from `src/services/views/bibleReferences/toUsfmReference.ts`
- **Sample page:** `/SamplePages/Components/Bible-Reference-Association-Panel` — `src/pages/samplePages/components/bibleReferenceAssociationPanelDoc.tsx`

`AssociationPanel` dressed as the bible reference panel: blue chips carrying a book icon once approved. Where a clicked reference leads is the page's (rule 2.3). It adds no gate and no behaviour of its own beyond its defaults. Every property of `AssociationPanel` stays available, so a page configures it exactly as it would the generic panel.

## 2. Business Rules

**2.1 [Must]** Every `AssociationPanel` property stays overridable; the defaults below only fill the gaps. A caller overriding one default keeps the rest. *(code: bibleReferenceAssociationPanel.tsx — BibleReferenceAssociationPanelProps; test: bibleReferenceAssociationPanel.test.tsx — "should let a caller override a default without giving up the rest")*

**2.2 [Must]** Defaults are applied by destructuring, for the same reason as `TagAssociationPanel`: a property passed as `undefined` still lands on its default. *(code: bibleReferenceAssociationPanel.tsx — BibleReferenceAssociationPanel)*

**2.3 [Must]** A reference click raises a hook, and where it leads is the page's — typically a page showing the verse, and differently in a user section and an admin section. The chip reads as the post cites it. It raises `chipOnClick`, or follows the link the page supplies through `chipHrefFor` (§UI20.6.4); the story performs no navigation of its own on the click, and composes no route of its own. Its default `chipHrefFor`, `bibleReferenceHref`, is the coupling: it addresses a readable reference as the deep-link route parses it, never from the label — `Romans 3:23` links to `/BibleReferences/ROM.3.23`. *(code: bibleReferenceAssociationPanel.tsx — referenceHref; test: bibleReferenceAssociationPanel.test.tsx — "should address each chip as the deep-link route parses it, not as it reads"; user, 2026-09-26; user, 2026-09-27)* ≠ item 5

**2.4 [Could]** `bibleReferenceHref` links a reference citing separate verse groups, such as `Joshua 10:8, 12–13`, to its whole chapter: one URL can name only one passage, and spanning 8–13 would include verses the post never quoted. *(code: toUsfmReference.ts — toUsfmReference; test: toUsfmReference.test.ts)*

**2.5 [Could]** Where a value that cannot be read as a reference leads is the page's too, and the page supplies that link, as it supplies every link (rule 2.3). In the user section it is the Bible reference page all the same, which says it could not be found (`UI/Pages/PostDetail.md rule 2.23`, `UI/Pages/MyPostDetail.md rule 2.23`, `UI/Pages/BibleReference.md rule 2.16`). In the admin section, where no click leaves the admin area, it is the queue, `/Admin/Posts`, handed the value, as for every reference there (`UI/Pages/ContentItemModerationDetailPage.md rule 2.26`, `UI/Pages/ContentItemModerationPage.md rule 2.13`). *(user, 2026-09-27)* ≠ item 5

**2.6 [Should]** The book is the Approved icon, not a flat chip icon, so a reference still waiting on a decision shows the hourglass instead: one icon slot, filled by whichever status applies. *(code: bibleReferenceAssociationPanel.tsx — approvedIconCssClass; test: bibleReferenceAssociationPanel.test.tsx — "should carry the book icon once approved and the hourglass while it waits")*

**2.7 [Must]** A suggested reference is kept as typed — trimmed, and nothing stripped from the front, unlike a tag. *(code: bibleReferenceAssociationPanel.tsx — no normalizeAddedValue default; test: bibleReferenceAssociationPanel.test.tsx — "should keep the reference as typed, hash and all, unlike a tag")*

**2.8 [Must]** The default moderation tier is the global tier plus the `BibleReference`-scoped pair (§SEC18.6), so a moderator trusted with bible references alone sees a submitted reference without holding a global role, and a `BibleReference-Publishers` holder decides on one. A `BibleReference-Reviewers` holder does not decide (`UI/Components/AssociationPanel.md rule 3.2.4`). *(code: bibleReferenceAssociationPanel.tsx — moderationRoles; test: bibleReferenceAssociationPanel.test.tsx — "should let a BibleReference-scoped moderator decide without holding the global role")* ≠ `UI/Components/AssociationPanel.md §10 item 16`

**2.9 [Must]** A moderator scoped to a different entity type does not decide on a bible reference. *(test: bibleReferenceAssociationPanel.test.tsx — "should let a moderator scoped to a different entity type see but not decide")*

**2.10 [Could]** Adding is on by default, so the bare component matches the post-detail panel. *(code: bibleReferenceAssociationPanel.tsx — showAdd; test: bibleReferenceAssociationPanel.test.tsx — "should render the post-detail reference panel from defaults alone")*

**2.11 [Must]** On a content item, the panel renders only where the page renders it and the item's effective `ShowBibleReferences` is on, and its add box shows only where `showAdd` is on and `BibleReferenceAllowed` is on (§DOM6.1, §DOM6.5; `UI/Components/AssociationPanel.md rule 2.28`). As the related-references panel of a bible reference page, the switches are `ShowRelatedBibleReferences` and `RelatedBibleReferencesAllowed` from `BibleReferenceSetting` (§DOM6.9). A reference-to-reference association is permitted only when both ends allow it (§DOM6.10 rule 2). *(§DOM6.1; user, 2026-09-26)* ≠ `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`

**2.12 [Must]** The read-only roles the story composes, which withhold the add box and every action here (`UI/Components/AssociationPanel.md rules 2.30 and 3.3.6`), are the global `ReadOnly` and `BibleReference-ReadOnly` (§SEC18.6). On a post, the post's `ContentItem-ReadOnly` and its `ContentItem-{ContentType}-ReadOnly` for the post's own type withhold them too, composed from the post's type the page hands the panel (`UI/Components/AssociationPanel.md rule 2.30`). A blocked reader still sees the references, as anyone else does, in the read-only view. *(user, 2026-09-26; user, 2026-09-27; §SEC18.6)* ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20`

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** Its defaults, beside `AssociationPanel`'s: `title` `Bible references`; `suggestTitle` `Suggest a bible reference`; `suggestDescription` `Know a matching verse? Suggest it below.`; `addPlaceholderText` `e.g. Romans 3:23…`; `chipCssClass` `btn-primary-soft`; `approvedIconCssClass` `bi-book`; `loginButtonText` `Login to suggest a bible reference`; `showAdd` `true`. *(code: bibleReferenceAssociationPanel.tsx — BibleReferenceAssociationPanel; test: bibleReferenceAssociationPanel.test.tsx — "should render the post-detail reference panel from defaults alone")*

**3.1.2** A semicolon separates references in the box, and nothing else does: the story sets `AssociationPanel`'s separators to the semicolon alone (`UI/Components/AssociationPanel.md rule 2.21`), so the colon inside a reference and the comma between its verse groups — `Joshua 10:8, 12–13` (rule 2.4) — keep it one reference. *(test: bibleReferenceAssociationPanel.test.tsx — "should separate a semicolon-separated list of references"; user, 2026-09-27)* ≠ item 1

**3.1.3** Signed out, the reader gets the reference-specific login prompt in place of the box. *(test: bibleReferenceAssociationPanel.test.tsx — "should offer the reference-specific login prompt to an anonymous reader")*

**3.1.4** Every other property-driven rule is `UI/Components/AssociationPanel.md §3.1`, unchanged.

### 3.2 Driven by roles

**3.2.1** `UI/Components/AssociationPanel.md §3.2` applies unchanged, with `moderationRoles` defaulting to `Reviewers, Publishers, Administrators, BibleReference-Reviewers, BibleReference-Publishers`. So in this document the **Reviewer** column is a holder of `Reviewers` or `BibleReference-Reviewers`, and the **Publisher** column a holder of `Publishers` or `BibleReference-Publishers`. *(code: associationRoles.ts — scopedModerationRoles)*

### 3.3 Combinations

**3.3.1** `UI/Components/AssociationPanel.md §3.3` applies unchanged.

**3.3.2** A holder of another entity type's scoped role — `Tag-Publishers`, say — is neither a Reviewer nor a Publisher here. A Submitted reference is hidden from them unless `viewAllRoles` names their role; even then they get no decision. *(test: bibleReferenceAssociationPanel.test.tsx — "should let a moderator scoped to a different entity type see but not decide")*

### 3.4 Role matrix

The full matrix is `UI/Components/AssociationPanel.md §3.4`, read with the Reviewer and Publisher columns of rule 3.2.1. The rows below are the ones this story changes. Owner means the contributor of the bible reference association.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| defaults (`showAdd` omitted), no read-only role the story composes held (rule 2.12) — **add box** | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| defaults (`showAdd` omitted), viewer also holds `ReadOnly` or `BibleReference-ReadOnly` (rule 2.12) — **add box** ≠ `UI/Components/AssociationPanel.md §10 item 1` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| defaults (`showAdd` omitted), on a post whose type the page gives, viewer also holds `ContentItem-ReadOnly` or that type's `ContentItem-{ContentType}-ReadOnly` (rule 2.12) — **add box** ≠ `UI/Components/AssociationPanel.md §10 item 20` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| defaults (`showAdd` omitted) — **login prompt** | ✅ Yes | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=true`, Submitted reference, viewer holds only `BibleReference-Reviewers` or `BibleReference-Publishers` — **Reject / Approve** ≠ `UI/Components/AssociationPanel.md §10 item 16` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes | ➖ n/a |
| `showModerationActions=true`, Submitted reference, viewer holds only another entity type's scoped role, `viewAllRoles` widened to it — **Reject / Approve** | ➖ n/a | ❌ No | ➖ n/a | ➖ n/a | ➖ n/a | ➖ n/a |

## 4. Properties and Events

### 4.1 Properties

`BibleReferenceAssociationPanelProps` is `Partial<AssociationPanelProps>`: every property in `UI/Components/AssociationPanel.md §4.1`, all optional. Only the defaults differ, as rule 3.1.1 lists, plus these:

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `chipHrefFor` | `(item) => string` | `bibleReferenceHref(item.value)`, today's coupling (section 10, item 5) | Where a clicked reference leads; the page supplies it (rule 2.3). What the default builds is rules 2.3 and 2.4, and item 5. | `AssociationPanel.chipHrefFor` |
| `approvedIconCssClass` | `string` | `'bi-book'` | Rule 2.6. | `AssociationPanel.approvedIconCssClass` |
| `moderationRoles` | `string` | `Reviewers, Publishers, Administrators, BibleReference-Reviewers, BibleReference-Publishers` | Rule 2.8. | `AssociationPanel.moderationRoles` |

`normalizeAddedValue` has no default here and reaches the panel's own trim.

### 4.2 Events

Inherits `UI/Components/AssociationPanel.md §4.2`; nothing to add. `onRemove`, `onReject` and `onApprove` reach the panel unchanged. *(test: bibleReferenceAssociationPanel.test.tsx — "should thread the remove and decision hooks through to the panel")*

### 4.3 Pass-through properties

Per §UI20.6.5. This story is the parent of `AssociationPanel` in the render tree.

- **Forwarded with a default of its own:** `title`, `suggestTitle`, `suggestDescription`, `addPlaceholderText`, `chipCssClass`, `approvedIconCssClass`, `chipHrefFor`, `loginButtonText`, `showAdd`, `moderationRoles`. The caller's value wins whenever it is not `undefined`.
- **Forwarded unchanged through the rest spread:** every other `AssociationPanel` property and event, `chipPrefixText` and `normalizeAddedValue` included.
- **Not reachable:** `chipOnClick` has no effect, because `chipHrefFor` always has a value here and the panel lets it win (`UI/Components/AssociationPanel.md rule 2.25`). Nor can a page have an Approved chip fall back to `chipIconCssClass`: `approvedIconCssClass` always has a value. See section 10.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

The actions are the panel's; this story composes their roles for bible references. Every cell not changed here is `UI/Components/AssociationPanel.md §5`.

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The chips** (a gated view) | Every viewer the visibility gate admits, with this story's moderation tier (rule 3.2.1) | None: a read (§SEC14.7 posture A′ rule 3) | ✅ Allowed | ✅ Approved references only | The association read the consumer hands in (§SEC14.3) |
| **A reference's label** — a link or a hook ≠ item 5 | Everyone who sees the reference | None: it writes nothing | ✅ Allowed | ✅ Offered; raises `chipOnClick`, or follows the link the page supplies through `chipHrefFor` (rule 2.3) | Nothing: where it leads is the page's (§UI20.6.4) |
| **The add box** — suggesting a reference ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20` | A signed-in reader; on by default here (rule 2.10) | `ReadOnly`, `BibleReference-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.12) | ❌ Refused — the box is withheld | Not offered; the reference-specific login prompt stands in its place (rule 3.1.3) | §SEC14.7 posture A′ rule 1, which also asks the host end's blocks; on a reference-to-reference association, §DOM6.10 rule 2 as well |
| **The login prompt** ≠ `UI/Components/AssociationPanel.md §10 item 15` | Every signed-out reader (rule 3.1.3) | None: its reader holds no role | Not shown — a blocked-role holder is signed in | ✅ Offered; raises `loginButtonOnClick`, or follows the `loginHref` the page supplies | Nothing: sign-in is the page's (§UI20.6.6 rule 2) |
| **Remove** ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20` | As the panel: the owner on their own `Draft` or `Submitted` reference, and `removeRoles` on a moderation surface | `ReadOnly`, `BibleReference-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.12) | ❌ Refused — withheld, the owner's withdrawal included | Not offered | §SEC14.7 posture A rule 3 and posture A′ rule 4; §APR9.9 for the owner's window |
| **Reject** ≠ `UI/Components/AssociationPanel.md §10 items 1, 16 and 20` | The publisher tier — `Publishers`, `BibleReference-Publishers` — and `Administrators`, on somebody else's `Submitted` reference, with `showModerationActions` on (rule 2.8) | `ReadOnly`, `BibleReference-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.12) | ❌ Refused — withheld | Not offered | `TransitionAssociationApprovalAsync` (§SEC14.7 posture A′, §APR8.6 HR-3) |
| **Approve** ≠ `UI/Components/AssociationPanel.md §10 items 1, 16 and 20` | As Reject (rule 2.8) | `ReadOnly`, `BibleReference-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.12) | ❌ Refused — withheld | Not offered | As Reject |

Nothing else to add beyond rule 2.8's role tier and rule 2.12's read-only roles.

## 6. Composition and Usage

| Route | Page | What the page sets |
| --- | --- | --- |
| `/posts/:contentItemId` | `src/pages/postDetail.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/myposts/:contentItemId` | `src/pages/myPostDetail.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/Admin/Posts/:contentItemId` | `src/pages/admin/contentItemModerationDetailPage.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/BibleReferences` | `src/pages/bibleReference.tsx` | `title` (`Related Bible References`), `associationCollection`, `onAdd`, `onRemove` |
| `/SamplePages/Post/Post-Single-Magazine` | `src/pages/samplePages/post/postSingleMagazineSample.tsx` | `chipHrefFor` (to the single-verse sample page), `associationCollection`, `onAdd`, `onRemove` |
| `/SamplePages/Components/Bible-Reference-Association-Panel` | `src/pages/samplePages/components/bibleReferenceAssociationPanelDoc.tsx` | the playground |

`/Post-Single` and `/Post-Single/:slug` (`src/pages/postSingle.tsx`) also render the story, setting `associationCollection`, `onAdd` and `onRemove`, but that route is a mocked sample page, not a product page: the real item page is `/posts/{id}` (user ruling 2026-09-27). It has no page document, and moving it under the sample pages is recorded in §UI20.5.1.

On `/BibleReferences` the host is a passage, so the panel shows related references — a `BibleReference` ↔ `BibleReference` pair (§DOM4.1).

## 7. Dependencies

Inherits `UI/Components/AssociationPanel.md §7`. The default chip links, today's coupling (item 5), point at the `/BibleReferences/:reference` deep-link route (rule 2.3), or, for a value that cannot be read, at `/Search` — the ported template's demo, sample material moving under `/SamplePages` (§UI20.5.1) — where rule 2.5 has the page supply its link: `/posts` in the user section, `/Admin/Posts` in the admin section.

## 8. States, Validation and Feedback

Inherits `UI/Components/AssociationPanel.md §8`. The panel does not check that a suggested value is a readable reference; an unreadable one is accepted, and its chip leads where the page sends it (rule 2.5) — today, with no page supplying a link, to `/Search` (item 5). Its one loading state is the panel's loading line, which is not announced today (`UI/Components/AssociationPanel.md §10 item 18`). Every string the story adds is a property with its text as the default (rule 3.1.1).

## 9. Styling and Accessibility

Inherits `UI/Components/AssociationPanel.md §9`. The default chip class is `btn-primary-soft`, which follows the light/dark theme.

## 10. Open Questions and Gaps

1. (needs issue) **Gap — a comma splits a citation.** References are written with commas between verse groups — `Joshua 10:8, 12–13` (rule 2.4). Asked whether this story should separate on the semicolon alone, the user ruled on 2026-09-27 that `AssociationPanel` takes its separators as a property, defaulting to a comma and a semicolon, and that this story overrides it to the semicolon alone (rule 3.1.2). The story sets no separators, and the panel has no property to set (`UI/Components/AssociationPanel.md §10 item 17`), so the box still separates on a comma: typing that citation raises two suggestions, `Joshua 10:8` and `12–13`, and the second is not a reference. Evidence: `associationPanel.tsx` — `separateValues`; `bibleReferenceAssociationPanel.tsx` — the defaults it destructures; `toUsfmReference.ts` — its opening comment.
2. (needs issue) **`chipOnClick` unreachable.** A page cannot make a reference chip raise `chipOnClick`, or render it as plain text. `chipHrefFor` defaults to `referenceHref`, passing `undefined` restores that default (rule 2.2), and `AssociationPanel` lets `chipHrefFor` win. Evidence: `bibleReferenceAssociationPanel.tsx` — `chipHrefFor = referenceHref`; `associationPanel.tsx` — `renderChipLabel`.
3. (needs issue) **`chipIconCssClass` unreachable on an Approved chip.** `approvedIconCssClass` defaults to `bi-book` and `undefined` restores it; passing an empty string renders an empty icon element rather than falling back. Evidence: `bibleReferenceAssociationPanel.tsx` — `approvedIconCssClass = 'bi-book'`; `associationPanel.tsx` — `iconFor`, `renderChipLabel`.
4. **Moved to the page documents.** Facet switches not wired — now `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`.
5. (needs issue) **The default `chipHrefFor` composes a route.** Rule 2.3 (user rulings 2026-09-26 and 2026-09-27) makes a reference click a hook and leaves where it leads to the page (§UI20.6.4). Where the page passes no `chipHrefFor`, the story composes the route itself through `bibleReferenceHref`: `/BibleReferences/<USFM>` for a readable reference, addressed as the deep-link route parses it and never from the label — `Romans 3:23` links to `/BibleReferences/ROM.3.23` (rule 2.3), and a citation of separate verse groups to its whole chapter (rule 2.4) — and `/Search?q=<value>` otherwise, where rule 2.5 has the page supply an unreadable reference's link: the Bible reference page, which says it could not be found, in the user section (user ruling 2026-09-28), and `/Admin/Posts`, handed the value, in the admin section (user ruling 2026-09-27). `/Search` is not a passage search: it is the ported template's demo, which returns the same sample posts whatever is typed (`src/pages/search.tsx`, its opening comment), and it is sample material moving under `/SamplePages` (§UI20.5.1; user rulings 2026-09-27). A test pins today's fallback and must change with it: "falls back to a search when the reference cannot be read" (`toUsfmReference.test.ts`, lines 45-47 at 70dc72e7). Only the magazine sample passes its own (`postSingleMagazineSample.tsx`); every other page in section 6 renders the story's route. The same default is what leaves `chipOnClick` unreachable (item 2). Evidence: `bibleReferenceAssociationPanel.tsx` — `referenceHref` (line 19 at 70dc72e7), `chipHrefFor = referenceHref` (line 28 at 70dc72e7); `toUsfmReference.ts` — `bibleReferenceHref` (lines 67-73 at 70dc72e7).
6. (needs issue) **Stale comments on the unreadable-reference link.** `toUsfmReference.ts` says `bibleReferenceHref` sends a reference it cannot read to "the passage search", because "a link that lands somewhere useful beats one that 404s" (lines 65-66 at 70dc72e7), and the sample page repeats the `/Search` link and its reason (`bibleReferenceAssociationPanelDoc.tsx` — `hrefSample`, lines 66-67 at 70dc72e7). `/Search` is no passage search (item 5), and rule 2.5 has the page supply an unreadable reference's link — the Bible reference page in the user section, `/Admin/Posts` in the admin section. Both comments are to say so when item 5 is built.
