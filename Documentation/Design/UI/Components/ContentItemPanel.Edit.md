# 1. ContentItemEditPanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), which dispatches here when the editor opens in place; through it `/myposts/{contentItemId}` — `src/pages/myPostDetail.tsx`. Rendered directly by `/Admin/Posts/{contentItemId}` — `src/pages/admin/contentItemModerationDetailPage.tsx` — and by its sample page.
- **Inherits:** `UI/Components/ContentItemPanel.md rules 2.1, 2.7, 2.9–2.18, 2.21–2.28 and 2.30–2.34`; `UI/Components/ContentItemPanel.Add.md rules 2.13–2.19` (the shared form); §SEC14.7 posture A rule 3; §SEC18.6; §DOM3.4 rule 16; §ARC12.4.1 rule 7a; §APR9.9; §UI20.6.4; §UI20.6.5; §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemEditPanel.tsx`, which renders the shared form engine `src/components/contentItems/contentItemFormPanel.tsx` with an item
- **Sample page:** `/SamplePages/Components/Content-Item-Edit-Panel` — `src/pages/samplePages/components/contentItemEditPanelDoc.tsx`
- **Relocated from:** §UI20.6.2 (part)

The edit face of `ContentItemPanel`: the item's type, frozen, and a form seeded from the item, with
removal riding on it. It is the same form engine as the add face, entered with an item.

It exists as a component of its own so the family tree names the surface, and so a page can land
straight on an editor — `/Admin/Posts/{contentItemId}` does, from its moderation action.

## 2. Business Rules

**2.1 [Must]** The edit template is the form engine with an item. `ContentItemPanel` dispatches here when its Edit is taken in place or `mode="edit"` is passed (`UI/Components/ContentItemPanel.md rules 2.9 and 2.10`). *(code: contentItemEditPanel.tsx — ContentItemEditPanel)*

**2.2 [Must]** The content type is create-only, so the edit template wears the same tile layout as the add face with every tile disabled and the item's own still selected — one look for both writing faces. *(§UI20.6.2)*

**2.3 [Must]** While `showEditSection` is off, the edit template refuses outright — neither the editor nor its *Delete* renders — rather than downgrading: the read surface belongs to the view templates. The switch governs this editor and its *Delete* only (`UI/Components/ContentItemPanel.md rule 2.13`). *(§UI20.6.2; user, 2026-09-26)*

**2.4 [Should]** Where no default rows were handed over to stand as tiles, the edit template falls back to a frozen chip. *(§UI20.6.2)*

**2.5 [Should]** An item whose type is not a contributable tile still shows its type selected: its own setting joins the frozen row. *(test: contentItemFormPanel.test.tsx — "should still shape the editor from a row the picker would not offer")*

**2.6 [Should]** A refusal is never the face's only content: it carries *Cancel*, the way back every other path off the editor offers. *(test: contentItemFormPanel.test.tsx — "should offer a way back from an editor that refuses")*

**2.7 [Must]** The editor is seeded from the item, and reseeds only when a different item — by id — arrives, so a consumer re-rendering an equivalent row does not wipe what is being typed. *(test: contentItemFormPanel.test.tsx — "should seed the editor from the item", "should reseed the editor when a different item arrives")*

**2.8 [Must]** *Save* raises `onModified` with the amendments over the item's original identity, always under the item's own content type, whatever the draft holds. *(test: contentItemFormPanel.test.tsx — "should raise onModified with the amendments over the original identity"; code: contentItemFormPanel.tsx — submitModify)*

**2.9 [Must]** *Cancel* restores the item's values and raises `onCancelled`. *(test: contentItemFormPanel.test.tsx — "should reseed the original values and tell the page on Cancel")*

**2.10 [Must]** The amendment half of `UI/Components/ContentItemPanel.md rule 2.25`: hiding is never destructive. A value already on the row survives an edit it was not shown for, so a setting changed after the item was written cannot silently blank it. *(§UI20.6.2)*

**2.11 [Must]** Removal rides on the editor. *Delete* asks *Are you sure?* before it raises `onRemoved`, and a refused confirmation raises nothing. *(test: contentItemFormPanel.test.tsx — "should confirm before it raises onRemoved", "should raise nothing when the confirmation is refused")*

**2.12 [Must]** The item's own embedded setting beats the collection for its own type, so a projection that already resolved §DOM6.4 is never silently un-overridden. *(test: contentItemFormPanel.test.tsx — "should let the embedded winner beat the collection for its own item")*

**2.13 [Must]** On a decided item — `Approved`, `Rejected` or `Dismissed` — *Submit as* does not render, and a save files the decided status untouched. A decision that arrives while the editor is open takes the row away. *(test: contentItemFormPanel.test.tsx — "should carry a decided status through a save untouched", "should take the row away when another process decides the item underneath it")*

**2.14 [Must]** A contributor may withdraw a submitted item back to Draft. *(test: contentItemFormPanel.test.tsx — "should let a contributor withdraw a submitted item back to draft")*

**2.15 [Must]** The owned-basis prefill (`UI/Components/ContentItemPanel.Add.md rule 2.16`) takes the submitter's name from `submittedByDisplayName`, not the editor's, and never overwrites an author already on the item. *(test: contentItemFormPanel.test.tsx — "should prefill an amendment from the submitter, not from the editor", "should never overwrite an author already on the item")*

**2.16 [Must]** A stored permission note survives an amendment whose basis still rests on permission, and drops when the amendment withdraws it (`UI/Components/ContentItemPanel.md rule 2.27`). *(test: contentItemFormPanel.test.tsx — "should keep the note when the basis still says permission was granted", "should drop a stored permission note when an amendment withdraws the basis")*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `showEditSection` off: the face shows *Contributions are not open to this account.* and *Cancel*, whatever the roles. *(test: contentItemFormPanel.test.tsx — "should refuse the editor while showEditSection is off, roles regardless")*

**3.1.2** The frozen tiles stand under the label *Type*, not the picker's question; every tile is disabled and the item's own carries `aria-pressed`. With no tile to show, the type renders as a chip under the same label. *(test: contentItemFormPanel.test.tsx — "should freeze the type tiles rather than offering the picker")*

**3.1.3** `showApprovalStatusRibbon` on: the face wears a corner ribbon naming the item's status — Draft, Submitted, Approved or Rejected. A `Dismissed` item or one with no status wears none. *(test: contentItemFormPanel.test.tsx — "should wear no ribbon unless the surface opted in", "should wear the status member name for the stylesheet to colour")*

**3.1.4** The buttons are *Save*, *Cancel*, and — right-aligned, where rules 3.2.4 and 3.2.5 allow it — *Delete*. `isSubmitting` disables all three. *(code: contentItemFormPanel.tsx — renderEdit)*

**3.1.5** *Submit as* stands while the item is `Draft` or `Submitted`, and follows the item until the contributor answers it (`UI/Components/ContentItemPanel.Add.md rule 2.18`). *(test: contentItemFormPanel.test.tsx — "should seed the row from the item it is amending")*

**3.1.6** Hidden fields follow the effective setting, and a resolved `false` beats a value the item carries. *(test: contentItemFormPanel.test.tsx — "should hide both fields in the editor for the same setting", "should let a resolved false beat what the item carries")*

**3.1.7** `isLoading` shows *Loading…* instead of the form, announced (§UI20.6.6 rule 5). *(code: contentItemFormPanel.tsx — the `isLoading` branch; user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Add.md §10 item 9`

**3.1.8** Every visible string the face renders, and every string it renders for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). *(user, 2026-09-27)* ≠ item 7

**3.1.9** While `isSubmitting` is on, the submitting state is announced — `role="status"` or equivalent — as a loading state is (§UI20.6.6 rule 5). *(user, 2026-09-27)* ≠ `UI/Components/ContentItemPanel.Add.md §10 item 11`

### 3.2 Driven by roles

**3.2.1** The editor opens for a signed-in viewer, not blocked for the item's type, who either owns the item — at any status, an amendment of a reviewed item forking a new version (§APR9.9) — or holds a non-owner member of the edit set while the item is `Draft` or `Submitted` (an item with no status counts as amendable). Anyone else gets the refusal of rule 2.6. *(test: contentItemFormPanel.test.tsx — "should give the owner the editor and the takedown on their own item", "should keep the owner editing their own item after it is decided", "should offer the publisher tier an edit on a live item and none on a decided one")*

**3.2.2** The review tier never gets the editor. *(test: contentItemFormPanel.test.tsx — "should never offer the reviewer tier the editor — a reviewer reviews")*

**3.2.3** A plain signed-in reader, and an owner blocked for the item's type, get the refusal. *(test: contentItemFormPanel.test.tsx — "should leave a plain reader with no editor at all", "should strip the editor from an owner blocked for that content type")*

**3.2.4** *Delete* renders for the owner, not blocked, while the item is `Draft` or `Submitted`: an `Approved` or `Rejected` item is locked to its owner. It never renders for the publisher tier (`UI/Components/ContentItemPanel.md rule 2.17`, §APR9.9). *(test: contentItemFormPanel.test.tsx — "should withhold removal from the publisher tier — a takedown is not moderation"; user, 2026-09-27)* ≠ item 5

**3.2.5** *Delete* renders for an `Administrators` holder, not blocked, wherever the editor is open to them, as a takedown (§APR9.9). On a reviewed item that is not theirs the editor is refused (rule 3.2.1), so the takedown the design keeps at any status is not reached here. *(test: contentItemFormPanel.test.tsx — "should give an administrator removal on somebody else's item"; code: contentItemFormPanel.tsx — mayDelete)*

### 3.3 Combinations

**3.3.1** `showEditSection` is asked first and refuses everyone. Then the block, which refuses the owner too. Then the grants: ownership at any status, or the non-owner edit set bounded by status. `Delete` is asked separately on the same order: the owner's is bounded by status (rule 3.2.4), and an administrator's is not, though it renders only inside an open editor (rule 3.2.5). *(code: contentItemFormPanel.tsx — mayEdit, mayDelete; user, 2026-09-27)* ≠ item 5

**3.3.2** An owner who also holds `Publishers` or `Administrators` edits at any status, as the owner. *(code: contentItemFormPanel.tsx — mayEdit)*

### 3.4 Role matrix

Owner means the item's contributor: the form item's `createdBy` is the viewer's account id.
`showEditSection=true` except where the row says otherwise.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| `showEditSection=false` — **the editor** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Item `Draft` or `Submitted`, no ReadOnly — **fields and Save** | ❌ No | ❌ No | ✅ Yes | ❌ No | ✅ Yes¹ | ✅ Yes |
| Item `Approved` or `Rejected`, no ReadOnly — **fields and Save**, the owner's save forking a new version (§APR9.9) | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Item `Draft` or `Submitted`, no ReadOnly — **Delete** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ✅ Yes |
| Item `Approved` or `Rejected`, the viewer's own, no ReadOnly — **Delete** ≠ item 5 | ➖ n/a | ➖ n/a | ❌ No | ➖ n/a | ➖ n/a | ➖ n/a |
| Item `Approved` or `Rejected`, not the viewer's, no ReadOnly — **Delete** | ❌ No | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No² |
| Any ReadOnly covering the item's type — **fields, Save or Delete** | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Editor open, item `Draft` or `Submitted` — ***Submit as*** | ❌ No | ❌ No | ✅ Yes | ❌ No | ✅ Yes¹ | ✅ Yes |
| Editor open, item `Approved` or `Rejected` — ***Submit as*** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Editor refused — **the refusal and Cancel** | ✅ Yes | ✅ Yes | ❌ No³ | ✅ Yes | ❌ No⁴ | ❌ No⁴ |

¹ At the global `Publishers`, `ContentItem-Publishers` or `ContentItem-{ContentType}-Publishers` scope for the item's type.
² Delete renders inside the editor, and on a decided item that is not theirs the editor itself is refused (rule 3.2.1), so Delete is never reached.
³ ✅ Yes for an owner who is blocked.
⁴ ✅ Yes on a decided item, or when blocked.

## 4. Properties and Events

### 4.1 Properties

`ContentItemEditPanelProps` is `ContentItemFormPanelProps` with `contentItem` required. The
properties this face reads:

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItem` | `ContentItemFormItem` | — (required) | The item under amendment. Hand over a stable object; the editor reseeds on a new id. | — |
| `contentItemSettingCollection` | `ContentItemSetting[]` | `[]` | The frozen tiles, and the fallback behind the item's embedded setting. | — |
| `showEditSection` | `boolean` | `false` | The surface switch (rule 2.3). | — |
| `showApprovalStatusRibbon` | `boolean` | `false` | The status ribbon (rule 3.1.3). | — |
| `isLoading` | `boolean` | `false` | A loading line instead of the form. | — |
| `isSubmitting` | `boolean` | `false` | Disables Save, Cancel and Delete. | — |
| `validationIssues` | `Record<string, string[]>?` | — | The API's field messages. | — |
| `submittedByDisplayName` | `string?` | — | The submitter's name for the owned-basis prefill (rule 2.15). | — |
| `approvalStatusDefault` | `ApprovalStatus` | `Submitted` | What *Submit as* opens on for an item whose projection left the status unset. | — |
| `entityType` | `string` | `'ContentItem'` | The entity the role names are composed from. | — |
| `blockRoles` ≠ `UI/Components/ContentItemPanel.md §10 item 24` | `string?` | `ReadOnly, ContentItem-ReadOnly, ContentItem-{ContentType}-ReadOnly` | The block set, overriding the composed one. Retired: the face composes its read-only roles itself, and no page supplies them (`UI/Components/ContentItemPanel.md rule 2.16`; §UI20.6.6 rule 3). | — |
| `editRoles` | `string?` | `[OWNER], Publishers, ContentItem-Publishers, ContentItem-{ContentType}-Publishers, Administrators` | The edit set. | — |
| `deleteRoles` | `string?` | `[OWNER], Administrators` | The delete set. | — |
| `showBorder`, `cssClass`, `titleText`, `ariaLabel` | `boolean`, `string`, `string`, `string` | `false`, `''`, `''`, `'Content item'` | The frame, a heading, and the section's name when no heading renders. | — |
| `typeLabelText`, `titleLabelText`, `titlePlaceholderText`, `authorLabelText`, `authorPlaceholderText`, `authorPrefilledHintText`, `contentLabelText`, `shareabilityLabelText`, `sharePermissionLabelText`, `sharePermissionPlaceholderText`, `sharePermissionRequiredText`, `submitAsLabelText`, `maxLengthExceededText`, `saveButtonText`, `cancelButtonText`, `deleteButtonText`, `deleteConfirmTitleText`, `deleteConfirmMessageText`, `deleteConfirmButtonText`, `validationSummaryText`, `blockedText`, `loadingText` | `string` | as `contentItemFormPanel.tsx` states them | The face's wording. | — |
| `submitButtonCssClass`, `deleteButtonCssClass` | `string` | `'btn-primary'`, `'btn-outline-danger'` | Theme classes for Save and Delete. | — |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onModified` | `ContentItemFormItem` | *Save* is pressed with a permission note where the basis needs one and every field within its ceiling. |
| `onRemoved` | `ContentItemFormItem` | *Delete* is confirmed. |
| `onCancelled` | none | *Cancel* is pressed, on the form or on a refusal. |

### 4.3 Pass-through properties

Per §UI20.6.5. `ContentItemPanel` drives `contentItem` (projected from its element),
`contentItemSettingCollection`, `showEditSection`, `isLoading`, `isSubmitting`,
`validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`,
`showApprovalStatusRibbon`, `ariaLabel`, `titleText`, `showBorder`, `onRemoved`, and
`onModified` and `onCancelled` wrapped to close the editor first. Every other property in section 4.1
is out of its reach today: `UI/Components/ContentItemPanel.md §10 item 7`.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27). The face still accepts one today, `blockRoles`, which is retired (`UI/Components/ContentItemPanel.md §10 item 24`).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The editor's fields and *Save* (`onModified`) | The owner at any status, the owner's save of a reviewed item forking a new version (§APR9.9); the publisher tier and `Administrators` while the item is `Draft` or `Submitted` (rule 3.2.1) | `ReadOnly`, `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` for the item's type (rule 3.2.3) | ❌ Refused: the refusal and *Cancel* instead (rule 2.6) | Not offered: the refusal and *Cancel* (section 3.4) | §SEC14.7 posture A rule 3; §DOM3.4 rule 16 |
| *Submit as*: withdrawing to Draft, or submitting | The same viewers, while the item is `Draft` or `Submitted` (rule 3.1.5) | The same three (rule 3.2.3) | ❌ Refused, with the editor | Not offered | §SEC14.7 posture A rule 3's `Draft` ↔ `Submitted` carve-out (§APR9.2 rules 4–6) |
| *Delete*, the owner's (`onRemoved`) ≠ item 5 | The owner while the item is `Draft` or `Submitted` (rule 3.2.4) | The same three (rule 3.2.3) | ❌ Refused: no *Delete* | Not offered | §SEC14.7 posture A rule 3; §APR9.9 |
| *Delete*, an administrator's takedown (`onRemoved`) | `Administrators`, wherever the editor is open to them (rule 3.2.5) | The same three (rule 3.2.3) | ❌ Refused: no *Delete* | Not offered | §SEC14.7 posture A rule 3; §APR9.9 |
| *Cancel* (`onCancelled`) | Everyone the face renders for, on the form or on the refusal (rule 2.6) | None — it writes nothing | ✅ Allowed | Offered on the refusal; raises `onCancelled` | Nothing — no request |

The edit and delete gates decide rendering only (`UI/Components/ContentItemPanel.md rule 2.7`).
The server's modify and remove gates are §SEC14.7 posture A rule 3 — the non-owner branch bounded
by status, the owner's amendment of a terminal item forking a new version (§DOM3.4 rule 16) —
and the role sets are `UI/Components/ContentItemPanel.md rule 2.17`. The block is composed from
the item's own content type (`UI/Components/ContentItemPanel.md rule 2.15`), and the status
the non-owner bound reads is the one on the item the consumer handed over.

## 6. Composition and Usage

- Through `ContentItemPanel`, in place: `/myposts/{contentItemId}` switches `showEditSection` on
  and listens on `onModified` (`UI/Components/ContentItemPanel.md §6.4`).
- Directly: `/Admin/Posts/{contentItemId}` renders this face when its moderation action is
  taken, with `showEditSection` and `showApprovalStatusRibbon` on, and wires `onModified`,
  `onRemoved` and `onCancelled` *(code: contentItemModerationDetailPage.tsx)*.

## 7. Dependencies

**Direct API dependencies** (called by the consumer, never the component):

| Concern | Endpoint |
| --- | --- |
| An amendment | `PUT api/ContentItems`, or the version fork on a terminal item *(§UI20.6.2)* |
| A removal | `DELETE api/ContentItems/{contentItemId}`, with an optional `deletionReason` — a soft delete *(code: contentItemService.ts — useRemoveContentItem; its broker member is DeleteContentItemAsync from #947, `BrokersHoldNoLogic.md`)* |

The settings read, the item read, and the identity and roles, are
`UI/Components/ContentItemPanel.md §7`.

## 8. States, Validation and Feedback

- **Loading and submitting** — as the add face (`UI/Components/ContentItemPanel.Add.md §8`). The
  face's one loading state is its *Loading…* line (rule 3.1.7), which is not announced today
  (`UI/Components/ContentItemPanel.Add.md §10 item 9`); nor is its submitting state (rule 3.1.9;
  `UI/Components/ContentItemPanel.Add.md §10 item 11`).
- **Validation** — `UI/Components/ContentItemPanel.md rules 2.31–2.33`. On this face a
  `ContentType` message reaches the summary, since there is no picker to answer for; a
  *Submit as* message sits on the row while it stands and reaches the summary once it has gone.
  A stored value over a lowered ceiling is refused at save with the limit named. *(test:
  contentItemFormPanel.test.tsx — "should summarise a ContentType message in edit, where there is
  no picker", "should summarise an ApprovalStatus message once the row is gone", "should refuse
  to save a stored value over a lowered ceiling, naming it")*
- **Confirmation** — rule 2.11.
- **Freshness** — the status is read from the item the consumer last handed over (rule 2.13).

## 9. Styling and Accessibility

- As the add face (`UI/Components/ContentItemPanel.Add.md §9`), with the frozen tiles disabled.
- The ribbon is `g2h-corner-ribbon g2h-approval-ribbon` with `data-approval-status` set to the
  status member name; the section adds `g2h-has-corner-ribbon g2h-has-approval-ribbon`.
- The delete confirmation is the shared `ConfirmDialog`.

## 10. Open Questions and Gaps

1. **Pass-through.** The edit-face properties `ContentItemPanel` cannot reach are
   `UI/Components/ContentItemPanel.md §10 item 7`, which carries the tag.
2. **§UI20.6.2 names no removal endpoint.** Its dependency table lists the amendment and not the
   removal that rides on this face; the section 7 row comes from the code.
3. (needs issue) **The doc page needs updating.** `contentItemEditPanelDoc.tsx`'s `contentItem` row says the
   editor "states it as a frozen chip rather than offering the picker"; the component renders the
   frozen tiles and falls back to the chip only when no rows were handed over (rules 2.2 and
   2.4). Its props table also has no rows for `contentItemSettingCollection`,
   `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`, `isLoading` or the
   role sets.
4. (needs issue) **Stale code comments.** `contentItemFormPanel.tsx` (the `showEditSection` prop comment) and
   `contentItemModerationDetailPage.tsx` (above its `ContentItemEditPanel`) say `mode="edit"` is
   "refused back to read"; the form engine has no `mode` prop, and without `showEditSection` it
   renders the refusal of rule 3.1.1.
5. (needs issue) **The owner is offered *Delete* on a reviewed item.** Rule 3.2.4 and
   `UI/Components/ContentItemPanel.md rule 2.17` (user ruling 2026-09-27, §APR9.9) confine the
   owner's *Delete* to an item that is `Draft` or `Submitted`: an `Approved` or `Rejected` item is
   locked to its owner. Today the face renders *Delete* for the owner at every status, since the
   delete gate asks no status (`contentItemFormPanel.tsx` — `mayDelete`, lines 523-529 at
   70dc72e7); no test pins *Delete* on a reviewed item either way. The user confirmed on
   2026-09-27 that an owner can never delete a reviewed item — the server's gate refuses it — and
   that the UI does not show them the remove option. The server's side is
   `UI/Components/ContentItemPanel.md §10 item 19`.
6. **Note — where an administrator takes down a reviewed item, ruled.** This item asked where an
   administrator takes down a reviewed item that is not theirs. *Delete* rides on the editor
   (rule 2.11), and the editor is refused to a viewer who does not own an `Approved` or
   `Rejected` item, `Administrators` included (rule 3.2.1), so an administrator never reaches
   *Delete* here on such an item. `/Admin/Posts/{contentItemId}` wires its takedown only to this
   face's `onRemoved` (`contentItemModerationDetailPage.tsx` — `removeContentItemAsync`, lines 202
   and 711 at 70dc72e7), so no screen offers it today. The user ruled on 2026-09-27 that in the
   admin area `Administrators` see every item and may soft-delete an item at any status — "This
   would allow cleanup of duplicate items that might have gone through approval" — as §APR9.9
   rule 7 already keeps the takedown. Where that control sits on `/Admin/Posts/{contentItemId}` is
   the page's, tracked as `UI/Pages/ContentItemModerationDetailPage.md §6 item 12`. The page's
   redesign, #698 (https://github.com/Glory2Him/Glory2Him.Core/issues/698), holds that placement:
   its item 7, *Where the post's own takedown sits*, has the redesign place the `Administrators`'
   takedown on that page. Until the page's design places it, this face's gates stand as rules
   3.2.1 and 3.2.5 state them.
7. (needs issue) **Visible strings that are not properties.** Rule 3.1.8 (§UI20.6.6 rule 1). The
   status ribbon's labels — Draft, Submitted, Approved, Rejected — come from a fixed table
   (`contentItemFormPanel.tsx` — `ribbonLabel`, line 1260 at 70dc72e7;
   `contentItemTemplate.ts` — `approvalStatusRibbonLabels`). The form strings this face shares
   with the add face are `UI/Components/ContentItemPanel.Add.md §10 item 8`.
