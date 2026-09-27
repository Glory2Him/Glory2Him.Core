# 1. ContentItemDefaultPanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), for every content type with no registered override; [ContentItemPanel.ContentItemQuotesPanel.md](ContentItemPanel.ContentItemQuotesPanel.md) and [ContentItemPanel.ContentItemVerseImagePanel.md](ContentItemPanel.ContentItemVerseImagePanel.md), which render it with their own content slot. Every page in `UI/Components/ContentItemPanel.md` **Used by** reaches it through the panel.
- **Inherits:** `UI/Components/ContentItemPanel.md §3 and rules 2.1, 2.5 and 2.23`; §UI20.6.4; §UI20.6.5; §UI20.6.6; `UI/Components/ContentItemPanel.md rule 2.26`
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemDefaultPanel.tsx`; its props are `ContentItemTemplateProps` in `src/models/components/contentItems/contentItemTemplate.ts`
- **Sample page:** `/SamplePages/Components/Content-Item-Default-Panel` — `src/pages/samplePages/components/contentItemDefaultPanelDoc.tsx` (it drives the dispatcher, through `shared/contentItemPanelPlayground.tsx`)
- **Relocated from:** §UI20.6.2 (part)

The view template most content types render through: the card a feed or detail page shows for
one item. It carries the type chip and status pill, the content block, the meta row, the tag and
reference pills, and the engagement row.

It decides nothing. `ContentItemPanel` hands it a fully decided bundle — ownership, the moderation
tier, the reaction gating, the status pair and the content length are the panel's decisions — and
it renders them. It is also the base the per-type overrides derive from.

## 2. Business Rules

**2.1 [Must]** The template renders from the decided bundle it is handed and decides nothing: every role and ownership question arrives answered. *(code: contentItemTemplate.ts — ContentItemTemplateProps)*

**2.2 [Must]** Every affordance on the card is an event, not a link. Which surface a title, a comment count or a reference leads to, and what a click on the type chip, *Submitted by*, *Author*, a tag or a reference does, is decided above the card and never by it (§UI20.6.4) — the same card serves the public feed, "my posts" and the moderation queue. *(code: contentItemDefaultPanel.tsx — the component's header comment; user, 2026-09-27)*

**2.3 [Must]** An override derives from this template by rendering it with `contentSlot` replaced, so the meta row, the pills and the engagement row are written once and every override carries them identically. What an override may change is how the content reads; what it may not change is what the card offers. *(code: contentItemDefaultPanel.tsx — ContentItemDefaultPanelProps)*

**2.4 [Must]** `HasTitle` and `HasAuthor` govern the title and the author on the view templates, which additionally require the item to carry a value. The quote and verse image templates provision no title, and their settings always carry `HasTitle = false`: the seed sets it, and the server refuses `HasTitle = true` for those two types, on a type default and on an item override alike (§ARC12.5.2 business rule 11; not yet built) (`UI/Components/ContentItemPanel.ContentItemQuotesPanel.md rule 3.1.5`, `UI/Components/ContentItemPanel.ContentItemVerseImagePanel.md rule 3.1.5`). *(§UI20.6.2; user, 2026-09-26; user, 2026-09-27)*

**2.5 [Must]** A section the card offers renders only where the surface switch is on, the element's setting does not hide it, and there is something to show. *(code: contentItemDefaultPanel.tsx — showsTags, showsBibleReferences, showsAssignedReactions, showsComments)*

**2.6 [Could]** The engagement row renders only when it has something to offer, so an empty row spends no space at the foot of the card. *(code: contentItemDefaultPanel.tsx — showsEngagementRow)*

**2.7 [Could]** The status ribbon stands on the card root, so every derived template wears it identically. *(code: contentItemDefaultPanel.tsx — ribbonLabel)*

**2.8 [Should]** A locked action is disabled, not hidden, and states its reason both on hover and to assistive technology. *(test: contentItemPanel.test.tsx — "should word the lock from the action's own label")*

**2.9 [Should]** The way into the comments renders whenever the surface shows comments and a page is listening, whether or not a count was given. *(test: contentItemPanel.test.tsx — "should offer the comments control uncounted when no count was given")*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** The type chip names the type (`contentTypeName`) and raises `onContentTypeClick`; where it leads is the page's (rule 2.2; `UI/Components/ContentItemPanel.md rule 2.42`). Beside it, with `showApprovalStatus` on, the status pill reads *Draft*, *In review*, *Approved*, *Rejected* or *Dismissed*. *(test: contentItemPanel.test.tsx — "should raise onContentTypeClick from the type badge", "should wear its status pill when asked, whatever the status"; user, 2026-09-27)*

**3.1.2** The default content block: the item's image as a thumbnail where it has one, with the chip row and the title stacked beside it. The title is a button raising `onTitleClick` only when `allowTitleClick` is on and `onTitleClick` is wired; otherwise it is plain heading text. *(test: contentItemPanel.test.tsx — "should raise onTitleClick from the title where the surface allows it", "should stand the title as plain text when allowed but nobody is listening")*

**3.1.3** The content is cut at `truncateAt` characters with an ellipsis while `isContentExpanded` is off and the content is longer; content that fits is never cut. With `allowInPlaceExpansion` off, a cut card ends in *read more....*, raising `onReadMore`. With it on, a card whose content overruns the cut ends in *read more…* or, expanded, *show less*, raising `onExpandCollapse`. *(test: contentItemPanel.test.tsx — "should cut at truncateAt with an ellipsis and offer the way in", "should never cut content that fits, nor offer a way into nothing", "should toggle in place when the surface allows it")*

**3.1.4** The meta row: *Submitted by* with the submitter's avatar and name, raising `onSubmittedByClick`, where the element names a submitter; *Author*, raising `onAuthorClick`, per rule 2.4; *Shareability* with the basis's read label; and *Date* with the published date. Each segment renders only when its value is present. Where *Submitted by* and *Author* lead is the page's (rule 2.2; `UI/Components/ContentItemPanel.md rule 2.42`). *(test: contentItemPanel.test.tsx — "should raise onSubmittedByClick and onAuthorClick as two different people", "should drop the author segment on a type whose setting carries none"; user, 2026-09-27)*

**3.1.5** The pills: each tag, raising `onTagClick`, and each bible reference, raising `onBibleReferenceClick`, under rule 2.5 (`showTagSection` and `ShowTags`; `showBibleReferenceSection` and `ShowBibleReferences`). *(test: contentItemPanel.test.tsx — "should raise onTagClick and onBibleReferenceClick with the pill pressed", "should hide the tags and references where its setting says so")*

**3.1.6** The reaction counts, under rule 2.5 (`showReactionSection` and `ShowReactions`, and a non-empty summary): compact glyphs and a total, or — while `areReactionCountsExpanded` — *All* and the total followed by each reaction's count; pressing it raises `onAssignedReactionsClick`. *(test: contentItemPanel.test.tsx — "should show the compact cluster with the summed total", "should toggle to the per-reaction counts and back")*

**3.1.7** *Like* renders when `offeredReactions` is not empty and raises `onReactionClick`. While `isReactionPickerOpen`, the choices stand above it; the one the reader already gave is marked, and each raises `onReactionSelected`. *(test: contentItemPanel.test.tsx — "should open the choices from Like and raise the selection", "should mark the reaction this reader already gave")*

**3.1.8** The comments control, under rule 2.9 (`showCommentsSection`, `ShowComments`, and `onCommentsClick` wired), reads *n comments* with a count and *Comments* without. *Share* and *Save* render only where `showShareSection` / `showSaveSection` are on and `onShareClick` / `onSaveClick` are wired. *(test: contentItemPanel.test.tsx — "should raise onCommentsClick with the count on show", "should offer Share and Save only where they are wired", "should hide comments, share and save on their switches")*

**3.1.9** *Edit* renders where `showsEditButton`, raising `onEditClick`; the moderation action renders where `showsModerateButton`, wearing `moderateButtonIconCss` and `moderateButtonLabel`, raising `onModerateClick`, and disabled with `moderateButtonLockReason` where one is given. Both stand at the right of the engagement row. *(code: contentItemDefaultPanel.tsx — the actions span)*

**3.1.10** With `showApprovalStatusRibbon` on, a corner ribbon names the item's status — Draft, Submitted, Approved or Rejected. A `Dismissed` item wears none. *(test: contentItemPanel.test.tsx — "should wear a corner ribbon carrying the status member name when asked", "should wear no corner ribbon unless the surface opted in")*

**3.1.11** Where `contentSlot` is given, the chip row stands above it and the default content block does not render. *(code: contentItemDefaultPanel.tsx — the `contentSlot != null` branch)*

**3.1.12** Every visible string the template renders, and every string it renders for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). *(user, 2026-09-27)* ≠ item 2

### 3.2 Driven by roles

**3.2.1** None of its own. The template reads no identity and no role; *Edit*, the moderation action and its lock arrive decided (`UI/Components/ContentItemPanel.md §3.2`). *(code: contentItemDefaultPanel.tsx — showsEditButton, showsModerateButton, moderateButtonLockReason are props)*

### 3.3 Combinations

**3.3.1** A section is the surface switch AND the setting AND the data (rule 2.5); the title is `HasTitle` not false AND a value (rule 2.4); the title control is `allowTitleClick` AND a listener (rule 3.1.2). No property adds what another removes. *(code: contentItemDefaultPanel.tsx — showsTitle, showsAuthor, showsTags)*

### 3.4 Role matrix

The template has no role gate: it renders what it is handed, identically for every persona.
Which persona is handed each decision is `UI/Components/ContentItemPanel.md §3.4`.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The card, as its properties allow | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showsEditButton=true` — **Edit** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ |
| `showsModerateButton=true` — **the moderation action** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes |
| `moderateButtonLockReason` given — **the moderation action, disabled** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Only if handed it; `ContentItemPanel` never hands it to this persona.

## 4. Properties and Events

### 4.1 Properties

`ContentItemDefaultPanelProps` is `ContentItemTemplateProps` plus `contentSlot`.

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItem` | `ContentItemSearchItem` | — (required) | The element. | — |
| `contentItemSetting` | `ContentItemSetting?` | — | The element's effective setting. | — |
| `contentTypeName` | `string` | — (required) | The chip's text. | — |
| `offeredReactions` | `ContentItemReactionOption[]` | — (required) | Already gated; empty means no Like. | — |
| `showsEditButton`, `showsModerateButton` | `boolean` | — (required) | The decided actions. | — |
| `moderateButtonLockReason` | `string?` | — | Present, the moderation action is locked. | — |
| `moderateButtonIconCss`, `moderateButtonLabel` | `string` | — (required) | The moderation action's icon and label. | — |
| `allowTitleClick` | `boolean` | — (required) | Whether the title may be a control. | — |
| `showApprovalStatusRibbon` | `boolean` | — (required) | The ribbon. | — |
| `showApprovalStatus` | `boolean?` | `false` | The pill. | — |
| `truncateAt` | `number` | — (required) | The cut. | — |
| `allowInPlaceExpansion`, `isContentExpanded` | `boolean` | — (required) | Which read-more, and whether the content is whole. | — |
| `areReactionCountsExpanded`, `isReactionPickerOpen` | `boolean` | — (required) | The two per-card toggles. | — |
| `showTagSection`, `showBibleReferenceSection`, `showReactionSection`, `showCommentsSection`, `showShareSection`, `showSaveSection` | `boolean?` | `true` | The section switches. | — |
| `contentSlot` | `ReactNode?` | — | The derivation point (rule 2.3). | — |
| `submittedByLabelText`, `authorLabelText`, `shareabilityLabelText`, `dateLabelText` | `string?` | `'Submitted by'`, `'Author'`, `'Shareability'`, `'Date'` | The meta row's labels. | — |
| `likeButtonText`, `commentsText`, `commentsNoCountText`, `shareButtonText`, `saveButtonText`, `editButtonText`, `allReactionsText` | `string?` | `'Like'`, `'comments'`, `'Comments'`, `'Share'`, `'Save'`, `'Edit'`, `'All'` | The engagement row's wording. | — |
| `readMoreText`, `expandLinkText`, `showLessText` | `string?` | `'read more....'`, `'read more…'`, `'show less'` | The two read-more affordances, deliberately different. | — |
| `shareabilityBasisLabels` | label map | the read labels (`shareabilityBasisReadLabels`) | How each basis reads. | — |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onContentTypeClick` | the element | The type chip is pressed. |
| `onTitleClick` | the element | The title is pressed, where it is a control. |
| `onSubmittedByClick` | the element | The *Submitted by* segment is pressed. |
| `onAuthorClick` | the element | The *Author* segment is pressed. |
| `onTagClick` | the element, the tag | A tag pill is pressed. |
| `onBibleReferenceClick` | the element, the reference | A reference pill is pressed. |
| `onAssignedReactionsClick` | none | The reaction counts are pressed. |
| `onReactionClick` | none | *Like* is pressed. |
| `onReactionSelected` | the element, the reaction | A reaction choice is pressed. |
| `onCommentsClick` | the element | The comments control is pressed. |
| `onReadMore` | the element | *read more....* is pressed. |
| `onExpandCollapse` | the element | *read more…* or *show less* is pressed. |
| `onShareClick`, `onSaveClick` | the element | *Share* or *Save* is pressed. |
| `onEditClick` | the element | *Edit* is pressed. |
| `onModerateClick` | the element | The moderation action is pressed while it is not locked. |

### 4.3 Pass-through properties

Per §UI20.6.5. Every property in section 4.1 except `contentSlot` is driven by `ContentItemPanel`:
the consumer's own properties unchanged, and the decided ones as
`UI/Components/ContentItemPanel.md §4.1` lists them. `contentSlot` is set by the overrides alone.
No property in section 4.1 is out of the parent's reach. The `Avatar` this template renders
beside the submitter's name is a core-UI primitive, so its fixed size (`sizePx={32}`,
`contentItemDefaultPanel.tsx` — the *Submitted by* segment) is not a gap: §UI20.6.5 checks
documented children only.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| Every control the template renders | As `UI/Components/ContentItemPanel.md §5` | As there | As there | As there | As there |

None of its own. It renders decisions made in `ContentItemPanel` (`UI/Components/ContentItemPanel.md §5`).

## 6. Composition and Usage

`ContentItemPanel` renders it for every content type without an override
(`UI/Components/ContentItemPanel.md rule 2.3`); `ContentItemQuotesPanel` and
`ContentItemVerseImagePanel` render it with their own `contentSlot`. No page renders it directly.

## 7. Dependencies

None beyond its properties. It uses the shared `Avatar` and `formatDate` from `src/components/coreUI`.

## 8. States, Validation and Feedback

Inherits `UI/Components/ContentItemPanel.md §8`; nothing to add beyond rules 2.6 and 2.9. The
template has no loading state.

## 9. Styling and Accessibility

- The card is an `<article class="card border p-3 mb-3 g2h-content-item-card">`. With a ribbon it
  adds `g2h-has-corner-ribbon g2h-has-approval-ribbon`, and the ribbon is `g2h-corner-ribbon
  g2h-approval-ribbon` with `data-approval-status` set to the status member name, coloured by
  `contentItems.css`.
- The chip is `g2h-content-item-chip` with `data-content-type` set to the enum member name.
- The thumbnail is `g2h-content-item-thumb`, a fixed 72px square, with an empty `alt`.
- The meta row is `g2h-content-item-meta`, its segments divided by a rule.
- A heading whose text is a button keeps the heading's typography and wraps.
- The reaction counts button carries `aria-expanded` and the name *Reaction counts*. *Like*
  carries `aria-expanded` and `aria-haspopup`. The choices are a `role="menu"` named *Choose a
  reaction*, each a `menuitem` with `aria-pressed` and the reaction's name. Neither fixed name is
  a property yet (section 10, item 2).
- The locked action carries `g2h-content-item-action-locked`, which restores its tooltip on a
  disabled button, and a visually-hidden copy of its reason appended to its name.
- Every icon is `aria-hidden`.

## 10. Open Questions and Gaps

1. **Note — the facet pairs, ruled.** This template renders tags, bible references, reactions and
   the comments control gated on the page's switch AND the element's setting (rule 2.5), which is
   what `UI/Components/ContentItemPanel.md rule 2.29` now requires (user ruling 2026-09-26; see
   `UI/Components/ContentItemPanel.md §10 item 3`).
2. (needs issue) **Visible strings that are not properties.** Rule 3.1.12 (§UI20.6.6 rule 1). The
   status pill's labels — *Draft*, *In review*, *Approved*, *Rejected*, *Dismissed* — and the
   corner ribbon's — *Draft*, *Submitted*, *Approved*, *Rejected* — come from fixed tables
   (`contentItemTemplate.ts` — `approvalStatusBadgeLabels`, lines 198-204, and
   `approvalStatusRibbonLabels`, lines 232-237, at 70dc72e7; rendered by
   `contentItemDefaultPanel.tsx` — `renderStatusBadge` and `ribbonLabel`). No property sets them.
   Nor does one set the two names the template writes for a screen reader alone, which
   §UI20.6.6 rule 1 covers too (user ruling 2026-09-27): the reaction counts button's *Reaction
   counts* (`contentItemDefaultPanel.tsx`, line 380 at 70dc72e7) and the reaction menu's *Choose a
   reaction* (line 427).
