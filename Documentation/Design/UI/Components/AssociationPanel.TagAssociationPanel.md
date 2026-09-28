# 1. TagAssociationPanel

- **Kind:** User story — child component of AssociationPanel
- **Parent:** [AssociationPanel.md](AssociationPanel.md)
- **Children:** none
- **Composes:** [AssociationPanel.md](AssociationPanel.md) — its parent, and nothing else
- **Used by:** the pages in section 6
- **Inherits:** `UI/Components/AssociationPanel.md §2–9` (every rule not restated here), §SEC18.6, §DOM4.7, §DOM6.1, §DOM6.5, §DOM6.9, §UI20.6.4, §UI20.6.5, §UI20.6.6, §APR9.9
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/associations/tagAssociationPanel.tsx`
- **Sample page:** `/SamplePages/Components/Tag-Association-Panel` — `src/pages/samplePages/components/tagAssociationPanelDoc.tsx`

`AssociationPanel` dressed as the tag panel: green chips with a hash in front of each. Where a clicked tag leads is the page's (rule 2.4). It adds no gate and no behaviour of its own beyond its defaults and its tag normalizer. Every property of `AssociationPanel` stays available, so a page configures it exactly as it would the generic panel.

## 2. Business Rules

**2.1 [Must]** Every `AssociationPanel` property stays overridable; the defaults below only fill the gaps. A caller overriding one default keeps the rest. *(code: tagAssociationPanel.tsx — TagAssociationPanelProps; test: tagAssociationPanel.test.tsx — "should let a caller override a default without giving up the rest")*

**2.2 [Must]** Defaults are applied by destructuring, not by spreading a defaults object, so a property passed as `undefined` still lands on its default rather than blanking it. *(code: tagAssociationPanel.tsx — TagAssociationPanel)*

**2.3 [Must]** A leading hash is how people write tags, but it is not part of the tag. Each separated value is trimmed and stripped of its leading hashes before the duplicate check and before `onAdd`. *(code: tagAssociationPanel.tsx — normalizeTag; test: tagAssociationPanel.test.tsx — "should separate a tag list and strip the hash from each of them")*

**2.4 [Must]** A tag click raises a hook, and where it leads is the page's — typically the search page filtered by the tag, and differently in a user section and an admin section. The chip raises `chipOnClick`, or follows the link the page supplies through `chipHrefFor` (§UI20.6.4); the story performs no navigation and no filtering of its own on the click, and composes no route of its own. Its default `chipHrefFor`, which sends a clicked tag to a search for itself, is the coupling. *(code: tagAssociationPanel.tsx — tagSearchHref; test: tagAssociationPanel.test.tsx — "should hash-prefix each chip and link it to the search"; user, 2026-09-26; user, 2026-09-27)* ≠ item 5

**2.5 [Must]** The default moderation tier is the global tier plus the `Tag`-scoped pair (§SEC18.6), so a moderator trusted with tags alone sees a submitted tag without holding a global role, and a `Tag-Publishers` holder decides on one. A `Tag-Reviewers` holder does not decide (`UI/Components/AssociationPanel.md rule 3.2.4`). *(code: tagAssociationPanel.tsx — moderationRoles; test: tagAssociationPanel.test.tsx — "should let a Tag-scoped moderator decide without holding the global role")* ≠ `UI/Components/AssociationPanel.md §10 item 16`

**2.6 [Must]** A moderator scoped to a different entity type does not decide on a tag. *(test: tagAssociationPanel.test.tsx — "should let a moderator scoped to a different entity type see but not decide")*

**2.7 [Could]** Adding is on by default, so the bare component matches the post-detail panel. *(code: tagAssociationPanel.tsx — showAdd; test: tagAssociationPanel.test.tsx — "should render the post-detail tag panel from defaults alone")*

**2.8 [Must]** On a content item, the panel renders only where the page renders it and the item's effective `ShowTags` is on, and its add box shows only where `showAdd` is on and `TagsAllowed` is on (§DOM6.1, §DOM6.5; `UI/Components/AssociationPanel.md rule 2.28`). On a bible reference page, the same two switches are read from `BibleReferenceSetting` (§DOM6.9); that page is to be designed under #700 (section 6). *(§DOM6.1; user, 2026-09-26)* ≠ `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`

**2.9 [Must]** The read-only roles the story composes, which withhold the add box and every action here (`UI/Components/AssociationPanel.md rules 2.30 and 3.3.6`), are the global `ReadOnly` and `Tag-ReadOnly` (§SEC18.6): a tag contribution is blocked by `ReadOnly` or `Tag-ReadOnly`. On a post, the post's `ContentItem-ReadOnly` and its `ContentItem-{ContentType}-ReadOnly` for the post's own type withhold them too, composed from the post's type the page hands the panel (`UI/Components/AssociationPanel.md rule 2.30`). A blocked reader still sees the tags, as anyone else does, in the read-only view. *(user, 2026-09-26; user, 2026-09-27; §SEC18.6)* ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20`

**2.10 [Must]** The add box keeps `AssociationPanel`'s default separators, a comma and a semicolon (`UI/Components/AssociationPanel.md rule 2.21`), so `#faith, #healing` is two tags. *(user, 2026-09-27; test: tagAssociationPanel.test.tsx — "should separate a tag list and strip the hash from each of them")*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** Its defaults, beside `AssociationPanel`'s: `title` `Tags`; `suggestTitle` `Suggest a tag`; `suggestDescription` `Think a tag is missing? Suggest one and help others find this post.`; `addPlaceholderText` `Start typing a tag…`; `chipCssClass` `btn-success-soft`; `chipPrefixText` `#`; `loginButtonText` `Login to suggest a tag`; `showAdd` `true`. *(code: tagAssociationPanel.tsx — TagAssociationPanel; test: tagAssociationPanel.test.tsx — "should render the post-detail tag panel from defaults alone")*

**3.1.2** A chip reads `#<value>`, and is a link where the page supplies `chipHrefFor` (rule 2.4). *(test: tagAssociationPanel.test.tsx — "should hash-prefix each chip and link it to the search"; user, 2026-09-26)* ≠ item 5

**3.1.3** Signed out, the reader gets the tag-specific login prompt in place of the box. *(test: tagAssociationPanel.test.tsx — "should offer the tag-specific login prompt to an anonymous reader")*

**3.1.4** Every other property-driven rule is `UI/Components/AssociationPanel.md §3.1`, unchanged.

### 3.2 Driven by roles

**3.2.1** `UI/Components/AssociationPanel.md §3.2` applies unchanged, with `moderationRoles` defaulting to `Reviewers, Publishers, Administrators, Tag-Reviewers, Tag-Publishers`. So in this document the **Reviewer** column is a holder of `Reviewers` or `Tag-Reviewers`, and the **Publisher** column a holder of `Publishers` or `Tag-Publishers`. *(code: associationRoles.ts — scopedModerationRoles)*

**3.2.2** The owner sees their own pending tag, drawn as pending, with a way to withdraw it. *(test: tagAssociationPanel.test.tsx — "should show the owner their own pending tag with a way to withdraw it")*

### 3.3 Combinations

**3.3.1** `UI/Components/AssociationPanel.md §3.3` applies unchanged.

**3.3.2** A holder of another entity type's scoped role — `BibleReference-Publishers`, say — is neither a Reviewer nor a Publisher here. A Submitted tag is hidden from them unless `viewAllRoles` names their role; even then they get no decision. *(test: tagAssociationPanel.test.tsx — "should let a moderator scoped to a different entity type see but not decide")*

### 3.4 Role matrix

The full matrix is `UI/Components/AssociationPanel.md §3.4`, read with the Reviewer and Publisher columns of rule 3.2.1. The rows below are the ones this story changes. Owner means the contributor of the tag association.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| defaults (`showAdd` omitted), no read-only role the story composes held (rule 2.9) — **add box** | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| defaults (`showAdd` omitted), viewer also holds `ReadOnly` or `Tag-ReadOnly` (rule 2.9) — **add box** ≠ `UI/Components/AssociationPanel.md §10 item 1` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| defaults (`showAdd` omitted), on a post whose type the page gives, viewer also holds `ContentItem-ReadOnly` or that type's `ContentItem-{ContentType}-ReadOnly` (rule 2.9) — **add box** ≠ `UI/Components/AssociationPanel.md §10 item 20` | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| defaults (`showAdd` omitted) — **login prompt** | ✅ Yes | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=true`, Submitted tag, viewer holds only `Tag-Reviewers` or `Tag-Publishers` — **Reject / Approve** ≠ `UI/Components/AssociationPanel.md §10 item 16` | ➖ n/a | ➖ n/a | ➖ n/a | ❌ No | ✅ Yes | ➖ n/a |
| `showModerationActions=true`, Submitted tag, viewer holds only another entity type's scoped role, `viewAllRoles` widened to it — **Reject / Approve** | ➖ n/a | ❌ No | ➖ n/a | ➖ n/a | ➖ n/a | ➖ n/a |

## 4. Properties and Events

### 4.1 Properties

`TagAssociationPanelProps` is `Partial<AssociationPanelProps>`: every property in `UI/Components/AssociationPanel.md §4.1`, all optional. Only the defaults differ, as rule 3.1.1 lists, plus these:

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `chipHrefFor` | `(item) => string` | `/Search?q=<value>`, today's coupling (section 10, item 5) | Where a clicked tag leads; the page supplies it (rule 2.4). | `AssociationPanel.chipHrefFor` |
| `normalizeAddedValue` | `(rawValue) => string` | trim, then strip leading `#` | Rule 2.3. | `AssociationPanel.normalizeAddedValue` |
| `moderationRoles` | `string` | `Reviewers, Publishers, Administrators, Tag-Reviewers, Tag-Publishers` | Rule 2.5. | `AssociationPanel.moderationRoles` |

### 4.2 Events

Inherits `UI/Components/AssociationPanel.md §4.2`; nothing to add. `onRemove`, `onReject` and `onApprove` reach the panel unchanged. *(test: tagAssociationPanel.test.tsx — "should thread the remove and decision hooks through to the panel")*

### 4.3 Pass-through properties

Per §UI20.6.5. This story is the parent of `AssociationPanel` in the render tree.

- **Forwarded with a default of its own:** `title`, `suggestTitle`, `suggestDescription`, `addPlaceholderText`, `chipCssClass`, `chipPrefixText`, `chipHrefFor`, `normalizeAddedValue`, `loginButtonText`, `showAdd`, `moderationRoles`. The caller's value wins whenever it is not `undefined`.
- **Forwarded unchanged through the rest spread:** every other `AssociationPanel` property and event.
- **Not reachable:** `chipOnClick` has no effect, because `chipHrefFor` always has a value here and the panel lets it win (`UI/Components/AssociationPanel.md rule 2.25`). For the same reason a tag chip cannot be rendered as plain text. See section 10.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

The actions are the panel's; this story composes their roles for tags. Every cell not changed here is `UI/Components/AssociationPanel.md §5`.

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The chips** (a gated view) | Every viewer the visibility gate admits, with this story's moderation tier (rule 3.2.1) | None: a read (§SEC14.7 posture A′ rule 3) | ✅ Allowed | ✅ Approved tags only | The association read the consumer hands in (§SEC14.3) |
| **A tag's label** — a link or a hook ≠ item 5 | Everyone who sees the tag | None: it writes nothing | ✅ Allowed | ✅ Offered; raises `chipOnClick`, or follows the link the page supplies through `chipHrefFor` (rule 2.4) | Nothing: where it leads is the page's (§UI20.6.4) |
| **The add box** — suggesting a tag ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20` | A signed-in reader; on by default here (rule 2.7) | `ReadOnly`, `Tag-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.9) | ❌ Refused — the box is withheld | Not offered; the tag-specific login prompt stands in its place (rule 3.1.3) | §SEC14.7 posture A′ rule 1, which also asks the host end's blocks |
| **The login prompt** ≠ `UI/Components/AssociationPanel.md §10 item 15` | Every signed-out reader (rule 3.1.3) | None: its reader holds no role | Not shown — a blocked-role holder is signed in | ✅ Offered; raises `loginButtonOnClick`, or follows the `loginHref` the page supplies | Nothing: sign-in is the page's (§UI20.6.6 rule 2) |
| **Remove** ≠ `UI/Components/AssociationPanel.md §10 items 1 and 20` | As the panel: the owner on their own `Draft` or `Submitted` tag, and `removeRoles` on a moderation surface | `ReadOnly`, `Tag-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.9) | ❌ Refused — withheld, the owner's withdrawal included | Not offered | §SEC14.7 posture A rule 3 and posture A′ rule 4; §APR9.9 for the owner's window |
| **Reject** ≠ `UI/Components/AssociationPanel.md §10 items 1, 16 and 20` | The publisher tier — `Publishers`, `Tag-Publishers` — and `Administrators`, on somebody else's `Submitted` tag, with `showModerationActions` on (rule 2.5) | `ReadOnly`, `Tag-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.9) | ❌ Refused — withheld | Not offered | `TransitionAssociationApprovalAsync` (§SEC14.7 posture A′, §APR8.6 HR-3) |
| **Approve** ≠ `UI/Components/AssociationPanel.md §10 items 1, 16 and 20` | As Reject (rule 2.5) | `ReadOnly`, `Tag-ReadOnly`, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` (rule 2.9) | ❌ Refused — withheld | Not offered | As Reject |

Nothing else to add beyond rule 2.5's role tier and rule 2.9's read-only roles.

## 6. Composition and Usage

| Route | Page | What the page sets |
| --- | --- | --- |
| `/posts/:contentItemId` | `src/pages/postDetail.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/myposts/:contentItemId` | `src/pages/myPostDetail.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/Admin/Posts/:contentItemId` | `src/pages/admin/contentItemModerationDetailPage.tsx` | `associationCollection` (empty), `onAdd`, `showBorder`, `cssClass` |
| `/BibleReferences` | `src/pages/bibleReference.tsx` | `suggestDescription` (a passage rather than a post), `associationCollection`, `onAdd`, `onRemove` |
| `/SamplePages/Post/Post-Single-Magazine` | `src/pages/samplePages/post/postSingleMagazineSample.tsx` | `chipHrefFor` (to `/Tag?name=<value>`, a ported-blog page, sample material moving under `/SamplePages`, §UI20.5.1), `associationCollection`, `onAdd`, `onRemove` |
| `/SamplePages/Components/Tag-Association-Panel` | `src/pages/samplePages/components/tagAssociationPanelDoc.tsx` | the playground |

`/Post-Single` and `/Post-Single/:slug` (`src/pages/postSingle.tsx`) also render the story, setting `associationCollection`, `onAdd` and `onRemove`, but that route is a mocked sample page, not a product page: the real item page is `/posts/{id}` (user ruling 2026-09-27). It has no page document, and moving it under the sample pages is recorded in §UI20.5.1.

The same component renders against a post and against a passage, which is why it cannot name its host's moderation tier itself (`UI/Components/AssociationPanel.md rule 2.27`). *(code: associationRoles.ts)*

On `/BibleReferences` the host end is the passage, a `BibleReference`, so the server refuses a tag suggested there by a holder of `BibleReference-ReadOnly` (§SEC14.7 posture A′ rule 1), while the panel, composing only `ReadOnly` and `Tag-ReadOnly` for that page (rule 2.9), still shows them the box. Whether the page tells the panel its host is a passage, and in what form, belongs to the design of that page, which has not been designed yet: #700 (https://github.com/Glory2Him/Glory2Him.Core/issues/700), `DESIGN: Design The Bible Reference Page` (user ruling 2026-09-27; section 10, item 6).

## 7. Dependencies

Inherits `UI/Components/AssociationPanel.md §7`; nothing to add. The default chip links, today's coupling (item 5), point at the `/Search` route (rule 2.4) — the ported template's demo, sample material moving under `/SamplePages` (§UI20.5.1).

## 8. States, Validation and Feedback

Inherits `UI/Components/AssociationPanel.md §8`. Validation adds only the hash strip of rule 2.3. Its one loading state is the panel's loading line, which is not announced today (`UI/Components/AssociationPanel.md §10 item 18`). Every string the story adds is a property with its text as the default (rule 3.1.1).

## 9. Styling and Accessibility

Inherits `UI/Components/AssociationPanel.md §9`. The default chip class is `btn-success-soft`, which follows the light/dark theme.

## 10. Open Questions and Gaps

1. (needs issue) **`chipOnClick` unreachable.** A page cannot make a tag chip raise `chipOnClick`, or render it as plain text. `chipHrefFor` defaults to `tagSearchHref`, passing `undefined` restores that default (rule 2.2), and `AssociationPanel` lets `chipHrefFor` win over `chipOnClick`. Evidence: `tagAssociationPanel.tsx` — `chipHrefFor = tagSearchHref`; `associationPanel.tsx` — `renderChipLabel`.
2. **Moved to the page documents.** `TagsAllowed` and `ShowTags` not wired — now `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`.
3. **Note — suggestion copy on the moderation page, part of #698.** The default `suggestDescription` speaks to a reader ("help others find this post"), and `/Admin/Posts/:contentItemId` keeps it. Whether that page offers moderators the suggest box — hidden there, or kept with wording for moderators — is a question of the page's redesign, #698 (https://github.com/Glory2Him/Glory2Him.Core/issues/698), recorded in `UI/Components/AssociationPanel.md §10 item 10`.
4. (needs issue) **The doc page needs updating.** Its "Minimal usage" sample, presented as "everything the post-detail panel needs", passes `onApprove` and `onReject` without `showModerationActions`, so those two can never fire. Evidence: `tagAssociationPanelDoc.tsx` — `minimalSample`.
5. (needs issue) **The default `chipHrefFor` composes a route.** Rule 2.4 (user rulings 2026-09-26 and 2026-09-27) makes a tag click a hook and leaves where it leads to the page, with no navigation and no filtering of the story's own (§UI20.6.4). Where the page passes no `chipHrefFor`, the story composes `/Search?q=<value>`, the value URL-encoded, itself, and so navigates to a filtered search on its own. `/Search` is the ported template's demo, sample material moving under `/SamplePages` (§UI20.5.1; user rulings 2026-09-27). Only the magazine sample passes its own (`postSingleMagazineSample.tsx`); every other page in section 6 renders the story's route. The same default is what leaves `chipOnClick` unreachable (item 1). Evidence: `tagAssociationPanel.tsx` — `tagSearchHref` (line 19 at 70dc72e7), `chipHrefFor = tagSearchHref`.
6. **Note — the tag panel's host on a passage page belongs to #700.** Asked whether the Bible
   reference page should tell the tag panel its host is a passage, so that the panel withholds
   the suggest box from a holder of `BibleReference-ReadOnly` as the server refuses their
   suggestion (§SEC14.7 posture A′ rule 1), the user answered on 2026-09-27 that the page has
   not been designed yet, and asked for a design issue to mock it and derive its rules from. That
   issue is #700 (https://github.com/Glory2Him/Glory2Him.Core/issues/700), whose second point
   carries this question; section 6 records it, and the page's document is
   `UI/Pages/BibleReference.md`. Until #700 settles it, the story composes the roles rule 2.9
   names and no more.
