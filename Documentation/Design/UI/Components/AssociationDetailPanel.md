# 1. AssociationDetailPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** none
- **Composes:** none. It renders the core-UI primitives `Avatar` and `ConfirmDialog`, which have no component documents of their own.
- **Used by:** no other component document renders it. Pages: `/Admin/Posts/{contentItemId}/Associations/{associationId}` — `src/pages/admin/associationModerationDetailPage.tsx` ([`UI/Pages/AssociationModerationDetailPage.md`](../Pages/AssociationModerationDetailPage.md)), beneath the post's card in the left-hand column.
- **Inherits:** §SEC14.7 posture A′ rules 1 and 4, §SEC18.6, §APR8.6.2, §APR9.7.1 rule 5, §APR9.9 rule 7, §UI20.6.4, §UI20.6.5, §UI20.6.6; `DesignFeatures/PostModeration.md` rules 12, 13 and 16
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/associations/associationDetailPanel.tsx`, new; its item is `AssociationDetailItem` in `src/models/components/associations/associationDetailItem.ts`, new
- **Sample page:** `/SamplePages/Components/Association-Detail-Panel` — `src/pages/samplePages/components/associationDetailPanelDoc.tsx`, new

One association, a tag or a Bible reference on a post, as the association page shows it: what it
is, who suggested it and when, where its round stands, and — where one is recorded — the
confidence Berean's process gave it *(`Admin Association Detail.dc.html`, the association card)*.
It is the thing under review on that page; the round beside it is `ReviewPanel` and the thread
beneath it `ReviewCommentPanel`.

It is a pure presentation component. The page reads the association, its far end and the name of
whoever suggested it, projects them to `AssociationDetailItem`, and hands it over. It writes
nothing; its one action leaves as a hook.

## 2. Business Rules

**2.1 [Must]** `AssociationDetailPanel` is a pure presentation component: props in, events out, no fetching. *(§UI20.3 rule 2; user, 2026-10-11)*

**2.2 [Must]** It shows one association: whether it is a tag or a Bible reference, the tag or the reference itself, the reference's scripture where the item carries one, who suggested it and when, and its status (`DesignFeatures/PostModeration.md` rule 12). *(user, 2026-10-11)*

**2.3 [Must]** The association it shows is not a control: its chip leads nowhere, because the page it stands on is that association's own. *(user, 2026-10-11: the mock's association chip is a `span`)*

**2.4 [Must]** The confidence block renders only where the item carries both a confidence score and a confidence reason. Where either is null the block is not rendered, and nothing else on the panel changes for it (`DesignFeatures/PostModeration.md` rule 13). *(user, 2026-10-11)*

**2.5 [Must]** The panel never works out the suggestion. Which band the score falls in is §APR8.6.2's, against the round's resolved thresholds, and the page hands the answer over as `confidenceSuggestion`; the panel renders what it is handed, and with nothing handed renders no suggestion. *(§APR8.6.2; user, 2026-10-11)* ≠ `UI/Pages/AssociationModerationDetailPage.md §6 item 19`

**2.6 [Must]** The association's takedown is `Administrators`', at any status (§APR9.9 rule 7; §SEC14.7 posture A rule 3), and asks before it is sent: title *Are you sure?*, a message naming the tag or the reference, and button *Delete*; nothing is raised on Cancel (`DesignFeatures/PostModeration.md` rule 16). *(user, 2026-10-11)* ≠ item 3

**2.7 [Must]** A holder of a read-only role in scope is offered no takedown: the global `ReadOnly`, either endpoint's `{entityType}-ReadOnly`, or either `ContentItem` endpoint's `ContentItem-{contentType}-ReadOnly` — a block on one end bars the association (§SEC14.7 posture A′ rule 1). The panel composes those roles itself from the endpoints the page hands it (§UI20.6.6 rule 3), and the server refuses the takedown under every one of them (§SEC14.7 posture A′ rule 4). *(§SEC14.7 posture A′ rule 1)* ≠ item 3

**2.8 [Must]** Every visible string the panel renders, and every string it renders for a screen reader alone, is a property, whose default is the text below (§UI20.6.6 rule 1). *(§UI20.6.6 rule 1)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** A corner ribbon names the item's status — *Draft*, *Submitted*, *Approved* or *Rejected* — as the content item card's ribbon does (`UI/Components/ContentItemPanel.Default.md rule 3.1.10`). *(user, 2026-10-11: the mock's `assocStatus` ribbon)* ≠ item 1

**3.1.2** Above the chip, the kind reads *Tag* or *Bible reference*. The chip reads `#` and the label for a tag, and the label after a book glyph (`bi bi-book`) for a Bible reference, drawn larger than a card's pill. *(user, 2026-10-11: the mock's `assocTypeLabel` and `assocTitle`)* ≠ item 1

**3.1.3** A Bible reference whose item carries `scripture` shows it beneath the chip, in a shaded block, as written. A tag, or a reference without scripture, shows no block. *(user, 2026-10-11: the mock's `assocVerse`)* ≠ item 1

**3.1.4** The meta row: the suggester's avatar and *Suggested by* with their name, where the item names one; and *Date* with `createdWhen`, formatted as the card's *Date* is. *(user, 2026-10-11: the mock's meta row)* ≠ item 1

**3.1.5** The confidence block, under rule 2.4: Berean's identity — its glyph, *Berean*, and *Your AI Pair Reviewer* beneath — then *Confidence Score:* with the score to two decimal places, then *Confidence Reason* above the reason, rendered as text. *(user, 2026-10-11: the mock's Berean row and `assocScore`, `assocReason`)* ≠ item 2

**3.1.6** With `confidenceSuggestion` handed, the block carries a line beneath the score and is tinted to match: `Approval` — a raised-thumb glyph (`bi bi-hand-thumbs-up-fill`) and *Suggesting an approval*, in the success colour; `Rejection` — a lowered-thumb glyph (`bi bi-hand-thumbs-down-fill`) and *Suggesting a rejection*, in the danger colour; `Unable` — a question glyph (`bi bi-question-circle-fill`) and *Unable to suggest approve or reject*, in the warning colour. With none handed, the block has no line and no tint. *(user, 2026-10-11: the mock's `suggestLabel`, `suggestIcon`, `suggestColor` and `suggestBg`)* ≠ item 2

**3.1.7** *Take down* stands at the right of the panel's footer, under rules 2.6 and 2.7 and only where the page listens on `onTakeDown`. Pressed, it opens the confirmation; *Delete* raises `onTakeDown` with the item, and Cancel raises nothing. *(user, 2026-10-11)* ≠ item 3

### 3.2 Driven by roles

**3.2.1** *Take down* renders for a holder of `Administrators` alone, never for a signed-out viewer, and never for a holder of a read-only role in scope (rule 2.7). Nothing else on the panel is gated. *(§APR9.9 rule 7; §SEC14.7 posture A′ rule 1)* ≠ item 3

### 3.3 Combinations

**3.3.1** The block is asked before the grant: an administrator holding a read-only role in scope is offered no takedown. *(§SEC14.7 posture A′ rule 1)* ≠ item 3

**3.3.2** The confidence block's two conditions are its own: neither the status nor the viewer's roles change whether it renders (rule 2.4). *(user, 2026-10-11)*

### 3.4 Role matrix

Owner here is the account whose id matches the item's `suggestedById`, the association's
`CreatedBy`.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The association, its scripture, its meta row and its status | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes |
| Score and reason both recorded — **the confidence block** | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes |
| Score or reason null — **the confidence block** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `onTakeDown` wired — ***Take down*** ≠ item 3 | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| As the row above, a read-only role in scope held — ***Take down*** ≠ item 3 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ Only where a page renders the panel for them. The one page that does admits `Administrators`
alone today (`UI/Pages/AssociationModerationDetailPage.md rule 2.2`).

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `association` | `AssociationDetailItem` | required | The association, projected by the page: `associationId`, `kind` (`Tag` or `BibleReference`), `label`, `scripture?`, `suggestedById`, `suggestedByName?`, `createdWhen`, `approvalStatus`, `confidenceScore?` and `confidenceReason?`, the last two nullable. | — |
| `confidenceSuggestion` | `'Approval' \| 'Rejection' \| 'Unable'?` | — | The band §APR8.6.2 puts the score in (rule 2.5). | — |
| `roleSubjectCollection` ≠ item 3 | `ReadonlyArray<{ entityType: string; contentType?: string }>` | `[]` | The association's two endpoints, from which the takedown's read-only roles compose (rule 2.7). The same shape `ReviewPanel` takes (`UI/Components/ReviewPanel.md rule 2.54`). | — |
| `showBorder` | `boolean` | `true` | Bordered card. | — |
| `cssClass` | `string` | `''` | Appended to the panel's class list. | — |
| Kind text: `tagKindText`, `bibleReferenceKindText` | `string` | `'Tag'`, `'Bible reference'` | The kind above the chip (rule 3.1.2). | — |
| Meta text: `suggestedByLabelText`, `dateLabelText` | `string` | `'Suggested by'`, `'Date'` | The meta row (rule 3.1.4). | — |
| Ribbon text: `ribbonStatusLabels` | label map | *Draft*, *Submitted*, *Approved*, *Rejected* | The ribbon (rule 3.1.1). | — |
| Confidence text: `aiReviewerNameText`, `aiReviewerTaglineText`, `confidenceScoreLabelText`, `confidenceReasonLabelText` | `string` | `'Berean'`, `'Your AI Pair Reviewer'`, `'Confidence Score:'`, `'Confidence Reason'` | The block (rule 3.1.5). | — |
| Suggestion text: `approvalSuggestionText`, `rejectionSuggestionText`, `unableSuggestionText` | `string` | `'Suggesting an approval'`, `'Suggesting a rejection'`, `'Unable to suggest approve or reject'` | The suggestion line (rule 3.1.6). | — |
| Takedown text ≠ item 3: `takeDownButtonText`, `takeDownConfirmTitleText`, `takeDownConfirmMessageText`, `takeDownConfirmButtonText`, `takeDownCancelButtonText` | `string` | `'Take down'`, `'Are you sure?'`, `'This takes {label} down from this post. It cannot be undone from here.'`, `'Delete'`, `'Cancel'` | The action and its confirmation (rule 2.6). `{label}` is the chip's text, `#` and the name for a tag. | `ConfirmDialog` `title`, `message`, `confirmText`, `cancelText` |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onTakeDown` ≠ item 3 | the `AssociationDetailItem` | *Delete* is pressed on the takedown's confirmation (rule 3.1.7). |

### 4.3 Pass-through properties

Per §UI20.6.5. The panel renders no child that has a component document. Its takedown text drives
`ConfirmDialog` as section 4.1 says; the avatar's size is fixed and is not a gap (§UI20.6.5).

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3). What the page hands it is data: the association's two endpoints.

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The association, its scripture, its meta row, its status and its confidence block** (a view) | Everyone the page renders the panel for | None: a read | ✅ Allowed — the same view | Not reached: the one page admits no signed-out reader | §SEC14.7 posture A′ (the association read, #857) |
| ***Take down***, raising `onTakeDown` ≠ item 3 | `Administrators`, at any status (rule 2.6) | `ReadOnly`, and for each endpoint `{entityType}-ReadOnly` and, on a `ContentItem` endpoint, `ContentItem-{contentType}-ReadOnly` (rule 2.7) | ❌ Refused — no action | Not offered | §SEC14.7 posture A′ rule 4; §APR9.9 rule 7 |

1. Every gate decides rendering only; the server decides the takedown again against the stored row (§SEC14.6).
2. Identity is the signed-in account from the auth context, never a property.

## 6. Composition and Usage

- **Where it lands.** On `/Admin/Posts/{contentItemId}/Associations/{associationId}` it stands in the left-hand column, beneath the post's card and above `ReviewCommentPanel`, with `ReviewPanel` in the column beside it (`UI/Pages/AssociationModerationDetailPage.md §3`).
- **Minimal use.**

  ```tsx
  <AssociationDetailPanel
      association={associationDetailItem}
      roleSubjectCollection={endpointRoleSubjects}
      onTakeDown={(item) => void takeDownAssociationAsync(item)} />
  ```

## 7. Dependencies

**Data the consumer supplies:** the association, read by its id (`GET /api/associations/{id}`, #857);
its far end, the tag's name or the reference and its scripture (`UI/Foundations/TagService.md §1`,
`UI/Foundations/BibleReferenceService.md §1`); and the suggester's name (`contributorService.useGetContributorById`).
**The suggestion** comes from the read #863 designs. **The takedown** is `DELETE /api/associations/{id}`
(§ARC17.4), called by the page, never the component.

**Indirect:** the signed-in identity and roles from the auth context, for the takedown's gate.

## 8. States, Validation and Feedback

- **Loading and error.** None of its own: the page renders the panel once the association is read.
- **Empty.** A missing scripture, suggester or confidence leaves its part out (rules 3.1.3, 3.1.4 and 2.4).
- **Confirmation.** The takedown asks first (rule 2.6).

## 9. Styling and Accessibility

- The panel is a `section` named by its chip's text.
- The ribbon, the chip and the meta row reuse the content item card's classes, so both cards on the page read alike.
- The reason is rendered as text, never as markup: it is written by a process or a person (§APR9.7.1 rule 5).
- Every glyph is `aria-hidden`; the suggestion line's text names the band, so colour never carries it alone.

## 10. Open Questions and Gaps

1. (#1048) **Gap — the panel is not built.** Rules 2.1–2.3 and 3.1.1–3.1.4 (user rulings 2026-10-11): nothing in `src/components/` renders one association as its page shows it. The association page that renders it is `UI/Pages/AssociationModerationDetailPage.md`.
2. (#1049) **Gap — the confidence block is not built.** Rules 2.4, 2.5, 3.1.5 and 3.1.6 (user ruling 2026-10-11): the block, with the suggestion line it carries when one is handed. The page's half of the suggestion, the band it hands over, is `UI/Pages/AssociationModerationDetailPage.md §6 item 19`.
3. (needs issue) **Gap — the takedown is not built.** Rules 2.6, 2.7, 3.1.7, 3.2.1 and 3.3.1 (user rulings 2026-10-11). The page has no route to wire it to: `DELETE /api/associations/{id}` waits on `UI/Components/AssociationPanel.md §10 item 22`, and the page's half is `UI/Pages/AssociationModerationDetailPage.md §6 item 20`, so this is built with it and is not carved before it.
