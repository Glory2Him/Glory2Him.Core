# 1. ContentItemAddPanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), which dispatches here when it has no item; through it `/posts/contribute` — `src/pages/contribute.tsx`. Rendered directly only by its sample page.
- **Inherits:** `UI/Components/ContentItemPanel.md rules 2.1, 2.7, 2.15–2.18, 2.21–2.24, 2.26–2.28 and 2.30–2.35`; §SEC14.7 posture A rule 1; §SEC18.6; §APR9.7.1 rule 1; §UI20.6.4; §UI20.6.5; §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemAddPanel.tsx`, which renders the shared form engine `src/components/contentItems/contentItemFormPanel.tsx` with no item
- **Sample page:** `/SamplePages/Components/Content-Item-Add-Panel` — `src/pages/samplePages/components/contentItemAddPanelDoc.tsx`
- **Relocated from:** §UI20.6.2 (part)

The add face of `ContentItemPanel`: the type picker and a blank form, where a signed-in reader
contributes a content item. It is the form engine the edit face also runs on, entered with no item.

It exists as a component of its own so the family tree names the surface, and so a page that
needs the form's deeper text and role overrides can render it directly
(`UI/Components/ContentItemPanel.md §4.3`).

This document also holds the rules of the form that both writing faces share (rules 2.13–2.19);
[ContentItemPanel.Edit.md](ContentItemPanel.Edit.md) inherits them.

## 2. Business Rules

In this document a content type **on offer** is one the picker offers under rule 2.3 — a content
type default available for general contribution — counted before rule 2.6 removes the types the
reader is blocked from. The tiles are what is left once it has.

**2.1 [Must]** The add template is the form engine with no item: the picker and a blank form. `ContentItemPanel` dispatches here when it has no item (`UI/Components/ContentItemPanel.md rule 2.2`). *(code: contentItemAddPanel.tsx — ContentItemAddPanel)*

**2.2 [Must]** The content type is create-only (§ARC12.4.1 rule 7a), so only the add face offers the choice. *(§UI20.6.2)*

**2.3 [Must]** The picker offers the content type defaults carrying `IsAvailableAsGeneralUserContribution`, which is exactly the question a tile asks. *(§UI20.6.2)*

**2.4 [Must]** An override is never a tile, however the consumer's collection arrived. *(§UI20.6.2)*

**2.5 [Should]** The tiles are ordered by the rows' own `SortOrder` (§DOM6.6), ascending, so the order a contributor meets the types in is a decision recorded on the setting rather than an accident of the order the consumer's read answered with. The panel sorts what it is handed — it is a presentation component, so it does not depend on the consumer having ordered the collection. *(§UI20.6.2)*

**2.6 [Must]** The narrow block lands on the **picker**, not only on the form: every content type the reader is blocked from by a per-content-type read-only role (`ContentItem-{ContentType}-ReadOnly`) is removed from the tiles — not shown disabled — so every tile left is a type the reader may contribute, and only a reader blocked from every available type — every type on offer — loses the form (rule 3.2.4). *(§UI20.6.2; user, 2026-09-27)* ≠ item 5

**2.7 [Should]** The type the picker lands on by default is the first tile in that order: the first tile that remains once the blocked types are removed (rule 2.6), so the form always opens on a type the reader may use. A tie keeps the order the rows arrived in. *(§UI20.6.2; user, 2026-09-27)*

**2.8 [Must]** The add face can only ever resolve a content type default, because an override belongs to an item that does not exist yet. *(§UI20.6.2)*

**2.9 [Must]** A contribution lands at the status the contributor asked for — `Draft` or `Submitted` and nothing else (§APR9.7.1 rule 1) — which is what the form's *Submit as* row answers. *(§UI20.6.2)*

**2.10 [Must]** What the add face emits is self-contained: the form item carries the winning setting it was shaped with, so the consumer can hand it straight to a detail surface. *(test: contentItemFormPanel.test.tsx — "should emit a projection carrying the winner it was shaped with")*

**2.11 [Must]** Cancel raises `onCancelled` and clears nothing itself. *(test: contentItemFormPanel.test.tsx — "should raise onCancelled rather than clearing anything itself")*

**2.12 [Must]** The contribution half of `UI/Components/ContentItemPanel.md rule 2.25`: a title typed under one content type and then abandoned by picking another whose setting has no title is **not** posted, because the contributor can no longer see it, the type is create-only, and no read surface would ever show it again. *(§UI20.6.2)*

**2.13 [Must]** The two writing faces run on one form engine, so they cannot drift: the same fields, the same shaping, the same mandatory permission rule and the same validation readback. *(code: contentItemFormPanel.tsx — ContentItemFormPanel)*

**2.14 [Must]** *How are you permitted to share this?* offers four bases — public domain, permission to share, own work released as public domain, own work with permission granted — and never the retired `Owned`, which only an item that already holds it carries. An untouched form stands on public domain. *(test: contentItemFormPanel.test.tsx — "should open the sharing dropdown on "It's public domain"")*

**2.15 [Must]** *Permission details* renders only under a permission basis, and is mandatory there (`UI/Components/ContentItemPanel.md rule 2.31`). It is a textarea opening at one row, because the answer is pasted evidence; what is pasted keeps its own line breaks. *(test: contentItemFormPanel.test.tsx — "should ask for the permission detail only once permission is the basis", "should carry pasted evidence through with its own line breaks")*

**2.16 [Could]** An owned basis fills an empty Author field with `submittedByDisplayName`, or the signed-in reader's display name where none is given, until the contributor touches the field. A name the contributor types, or a field they deliberately empty, is theirs and stands. *(test: contentItemFormPanel.test.tsx — "should put the contributor's own name in the field on an owned basis", "should let the contributor publish under another name", "should respect a field the contributor deliberately emptied")*

**2.17 [Must]** *Submit as* offers the two states a contributor owns — Submitted, then Draft — and opens on the item's own status, or on `approvalStatusDefault` (Submitted unless the consumer says otherwise) where the model names none. *(test: contentItemFormPanel.test.tsx — "should offer the two states a contributor owns, opening on Submitted", "should open on the consumer’s approvalStatusDefault")*

**2.18 [Must]** Until the contributor answers *Submit as*, it reports the model; once they have, their answer stands. *(test: contentItemFormPanel.test.tsx — "should follow the item when the status moves underneath an unanswered row", "should leave an answered row alone when the status moves underneath it")*

**2.19 [Must]** The form is not an HTML `<form>`, because the association panels beside it commit a chip on Enter, and inside a form that Enter would submit the page. *(code: contentItemFormPanel.tsx — the comment on renderAdd)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `isLoading` shows *Loading…* instead of the form, announced (§UI20.6.6 rule 5). *(test: contentItemFormPanel.test.tsx — "should show a loading line instead of a half-built form"; user, 2026-09-27)* ≠ item 9

**3.1.2** With no content type on offer — none available for general contribution, whatever the reader's roles — the add face is not shown: `ContentItemPanel` shows the restricted face in its place (`UI/Components/ContentItemPanel.md rule 2.43`). *(user, 2026-09-27)* ≠ item 10

**3.1.3** The picker asks *What are you sharing?*. Each tile shows the type's icon (`ContentTypeIconCssClass`), name (`ContentTypeName`) and description (`ContentTypeDescription`). *(code: contentItemFormPanel.tsx — renderTypePicker)*

**3.1.4** The fields shape from the selected type's effective setting: Title where `HasTitle`, Author where `HasAuthor`. The content field is labelled with the type's name. *(test: contentItemFormPanel.test.tsx — "should shape the fields from the chosen type settings")*

**3.1.5** The fields run Title, Author, the content, the sharing basis, Permission details, and *Submit as* last, directly above the buttons. *(test: contentItemFormPanel.test.tsx — "should stand last in the form, under the permission question and over the buttons")*

**3.1.6** The buttons are *Submit for review* and *Cancel*; `isSubmitting` disables both. *(test: contentItemFormPanel.test.tsx — "should render the picker, the fields and the submit pair for a signed-in reader", "should freeze the buttons while the consumer is persisting")*

**3.1.7** The add face wears no status ribbon: no item, no status. *(test: contentItemFormPanel.test.tsx — "should wear none in add mode — no item, no status")*

**3.1.8** A tile a contributor abandons and picks again gives back what they typed under it. *(test: contentItemFormPanel.test.tsx — "should keep what was typed when the reader picks the type back again")*

**3.1.9** Every visible string the face renders, and every string it renders for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). *(user, 2026-09-27)* ≠ item 8

**3.1.10** While `isSubmitting` is on, the submitting state is announced — `role="status"` or equivalent — as a loading state is (§UI20.6.6 rule 5). *(user, 2026-09-27)* ≠ item 11

### 3.2 Driven by roles

**3.2.1** A reader who is not signed in gets a *Login to contribute* link instead of the form, to the `loginHref` the page supplies: the page wiring the sign-in hook (§UI20.6.6 rule 2). The face composes no route of its own (§UI20.6.4): its default `loginHref`, a sign-in route it composes itself when the page passes none, is the coupling. A reader whose sign-in state has not yet been read back is not sent to sign in (§UI20.6.6 rule 2). *(test: contentItemFormPanel.test.tsx — "should offer a way in rather than a form when nobody is signed in"; user, 2026-09-26; §UI20.6.6 rule 2)* ≠ items 4 and 12

**3.2.2** A signed-in reader gets the picker and the form; the add set is empty by default (`UI/Components/ContentItemPanel.md rule 2.17`). *(test: contentItemFormPanel.test.tsx — "should render the picker, the fields and the submit pair for a signed-in reader")*

**3.2.3** A type the reader is blocked from by `ContentItem-{ContentType}-ReadOnly` has no tile; the other tiles and the rest of the form stay live. *(user, 2026-09-27)* ≠ item 5

**3.2.4** A reader blocked from every type on offer — by `ReadOnly`, by `ContentItem-ReadOnly`, or by a narrow block on each — is not shown the add face: `ContentItemPanel` shows the restricted face in its place (`UI/Components/ContentItemPanel.md rule 2.43`). A reader holding the global `ReadOnly` never reaches the contribution page, which is the page's to enforce (section 6); a holder of `ContentItem-ReadOnly` is let in and shown the restricted face (section 10, item 7). *(user, 2026-09-27; code: contentItemFormPanel.tsx — mayAdd, contributableSettings)* ≠ item 10

**3.2.5** Where `addRoles` names roles, a reader holding none of them gets *Contributions are not open to this account.* instead of the form. *(test: contentItemFormPanel.test.tsx — "should withhold the form from a reader without the required add role", "should open the form to a reader who holds one of them")*

**3.2.6** The picker lands on the first remaining tile in the order of rule 2.5 — rule 2.7 read with rule 2.6, which removes a blocked type. *(test: contentItemFormPanel.test.tsx — "should select the first type that is open rather than the first listed"; user, 2026-09-27)*

### 3.3 Combinations

**3.3.1** The loading line is asked first (rule 3.1.1), then sign-in, then whether any type is left to the reader once the blocks have removed theirs — none on offer, or none the blocks leave — and where none is, the restricted face shows in the add face's place (rules 3.1.2 and 3.2.4). The grant is asked last, so a block outranks a held `addRoles` grant. So the account's refusal shows only where types are left to the reader and they hold none of the roles `addRoles` names (rule 3.2.5). *(test: contentItemFormPanel.test.tsx — "should let a block outrank an add role the reader holds"; code: contentItemFormPanel.tsx — renderAdd; user, 2026-09-27)* ≠ item 10

### 3.4 Role matrix

There is no item on this face, so there is no owner: the Owner column is n/a throughout.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Not signed in — **the login link** ≠ item 12 | ✅ Yes | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |
| Default `addRoles`, no ReadOnly — **the picker and the form** | ❌ No | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |
| `ContentItem-{ContentType}-ReadOnly` for one type — **that type's tile** ≠ item 5 | ➖ n/a | ❌ No¹ | ➖ n/a | ❌ No¹ | ❌ No¹ | ❌ No¹ |
| `ReadOnly` or `ContentItem-ReadOnly` — **the form** | ➖ n/a | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |
| No type on offer, or the reader's read-only roles leave no tile — **the add face** (the restricted face shows in its place) ≠ item 10 | ✅ Yes³ | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |
| `addRoles` set, none of them held — **the form** | ❌ No | ❌ No² | ➖ n/a | ❌ No² | ❌ No² | ❌ No² |
| `isSubmitting=true` — **Submit for review and Cancel enabled** | ❌ No | ❌ No | ➖ n/a | ❌ No | ❌ No | ❌ No |

¹ Removed from the picker, not rendered disabled; every other tile stays open.
² ✅ Yes for a persona that holds one of the named roles and no block.
³ Its login link, which is asked before whether any type is left (rule 3.3.1).

## 4. Properties and Events

### 4.1 Properties

`ContentItemAddPanelProps` is `ContentItemFormPanelProps` without `contentItem`. The properties this
face reads:

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `contentItemSettingCollection` | `ContentItemSetting[]` | `[]` | The tiles and the field shaping. | — |
| `isLoading` | `boolean` | `false` | The loading line (rule 3.1.1). | — |
| `isSubmitting` | `boolean` | `false` | Disables the buttons (rule 3.1.6). | — |
| `validationIssues` | `Record<string, string[]>?` | — | The API's field messages. | — |
| `submittedByDisplayName` | `string?` | — | The owned-basis prefill (rule 2.16). | — |
| `approvalStatusDefault` | `ApprovalStatus` | `Submitted` | What *Submit as* opens on (rule 2.17). | — |
| `entityType` | `string` | `'ContentItem'` | The entity the role names are composed from. | — |
| `blockRoles` ≠ `UI/Components/ContentItemPanel.md §10 item 24` | `string?` | `ReadOnly, ContentItem-ReadOnly, ContentItem-{ContentType}-ReadOnly` | The block set, overriding the composed one. Retired: the face composes its read-only roles itself, and no page supplies them (`UI/Components/ContentItemPanel.md rule 2.16`; §UI20.6.6 rule 3). | — |
| `addRoles` | `string` | `''` | The add set; empty admits any signed-in reader. `[OWNER]` is ignored here. | — |
| `loginHref` | `string?` | the face's own sign-in route, today's coupling (section 10, item 4) | Where the login link goes; the page supplies it (rule 3.2.1). | — |
| `loginButtonText`, `loginButtonCssClass` | `string` | `'Login to contribute'`, `'btn-outline-primary'` | The login link. | — |
| `showBorder`, `cssClass`, `titleText`, `ariaLabel` | `boolean`, `string`, `string`, `string` | `false`, `''`, `''`, `'Content item'` | The frame, a heading, and the section's name when no heading renders. | — |
| `typePickerTitleText`, `titleLabelText`, `titlePlaceholderText`, `authorLabelText`, `authorPlaceholderText`, `authorPrefilledHintText`, `contentLabelText`, `shareabilityLabelText`, `sharePermissionLabelText`, `sharePermissionPlaceholderText`, `sharePermissionRequiredText`, `submitAsLabelText`, `maxLengthExceededText`, `submitButtonText`, `cancelButtonText`, `validationSummaryText`, `blockedText`, `noTypesText`, `loadingText` | `string` | as `contentItemFormPanel.tsx` states them | The face's wording. | — |
| `typeBlockedText` | `string` | `'Not open to this account'` | Labels a blocked tile. Retired with that tile (section 10, item 5). | — |
| `submitButtonCssClass` | `string` | `'btn-primary'` | A theme class, never a colour. | — |

`noTypesText`, and `blockedText` where a reader's blocks leave no tile, word the two refusals the
restricted face replaces (rules 3.1.2 and 3.2.4; section 10, item 10). `blockedText` still words
the `addRoles` refusal (rule 3.2.5) and the edit face's refusal
(`UI/Components/ContentItemPanel.Edit.md rule 3.1.1`).

The form props that belong to the edit face (`showEditSection`, `editRoles`, `deleteRoles`,
`showApprovalStatusRibbon`, the delete and save wording, `deleteButtonCssClass`) are accepted
and have no effect here. *(code: contentItemFormPanel.tsx — renderAdd)*

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onAdded` | `ContentItemFormItem` | *Submit for review* is pressed with a type selected, a permission note where the basis needs one, and every field within its ceiling. |
| `onCancelled` | none | *Cancel* is pressed. |

### 4.3 Pass-through properties

Per §UI20.6.5. `ContentItemPanel` drives `contentItemSettingCollection`, `isLoading`,
`isSubmitting`, `validationIssues`, `submittedByDisplayName`, `approvalStatusDefault`,
`ariaLabel`, `titleText`, `showBorder`, `onAdded` and `onCancelled`, unchanged. It withholds
`typeBlockedText` (`UI/Components/ContentItemPanel.md §4.3`). Every other property in section 4.1
is out of its reach today: `UI/Components/ContentItemPanel.md §10 item 6`.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27). The face still accepts one today, `blockRoles`, which is retired (`UI/Components/ContentItemPanel.md §10 item 24`).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| *Login to contribute* ≠ items 4 and 12 | A reader who is not signed in (rule 3.2.1) | None — it writes nothing | ➖ n/a: a role holder is signed in | Offered: a link to the `loginHref` the page supplies (rule 3.2.1; §UI20.6.6 rule 2) | Nothing — no request |
| A type's tile ≠ items 5 and 10 | A signed-in reader, for each content type on offer (rules 2.3 and 3.2.2) | `ContentItem-{ContentType}-ReadOnly` for that type; `ReadOnly` and `ContentItem-ReadOnly` for every type (rules 2.6 and 3.2.4) | ❌ Refused: the tile is removed, and a reader left with no tile is shown the restricted face in the add face's place (rules 2.6, 3.2.3 and 3.2.4) | Not offered: the login link instead | §SEC14.7 posture A rule 1, on the type of the row added |
| *Submit for review* (`onAdded`), with its *Submit as* row | A signed-in reader holding one of `addRoles`, which is empty by default and then admits any signed-in reader (rules 3.2.2 and 3.2.5) | The same three, for the selected type (`UI/Components/ContentItemPanel.md rule 2.15`) | ❌ Refused, with the form or the tile (rules 3.2.3 and 3.2.4) | Not offered: the login link instead | §SEC14.7 posture A rule 1; §APR9.7.1 rule 1 |
| *Cancel* (`onCancelled`) | Everyone the form renders for (rule 3.1.6) | None — it writes nothing | ✅ Allowed wherever the form renders | Not offered | Nothing — no request |

The add gates decide rendering only (`UI/Components/ContentItemPanel.md rule 2.7`). The
contribution gate the server applies is §SEC14.7 posture A rule 1 and §APR9.7.1 rule 1; the
role sets and the block veto are `UI/Components/ContentItemPanel.md rules 2.17 and 2.18`. The
narrow block is resolved against the **selected** type
(`UI/Components/ContentItemPanel.md rule 2.15`), so each tile asks it for its own type.

## 6. Composition and Usage

`/posts/contribute` renders `ContentItemPanel` with a settings collection and no item, and so
reaches this face; the page owns the `POST`, the redirect, the notification and the validation
readback (`UI/Components/ContentItemPanel.md §6.4`). A reader holding the global `ReadOnly`
never reaches that page: refusing them is the page's, not the face's (user ruling 2026-09-27),
and the page does not do it today (`UI/Pages/Contribute.md §6 item 1`). A holder of
`ContentItem-ReadOnly` is let in, and is shown the restricted face in the add face's place (user
ruling 2026-09-27; section 10, item 7). No page renders
`ContentItemAddPanel` directly today. Tags and bible references cannot render on an add surface
(`UI/Components/ContentItemPanel.md rule 2.35`).

## 7. Dependencies

**Direct API dependency** (called by the consumer, never the component): *(§UI20.6.2)*

| Concern | Endpoint |
| --- | --- |
| The contribution | `POST api/ContentItems` — seven caller-supplied members only (`ContentType`, `Title`, `Author`, `Content`, `ShareabilityBasis`, `SharePermission`, `ApprovalStatus`); the processing service mints the identifiers, hashes the content and lands the row unpublished at the status the caller asked for — `Draft` or `Submitted` and nothing else (§APR9.7.1 rule 1), which is what the panel's "Submit as" row answers — and the foundation beneath it stamps the audit trail |

The settings read, and the identity and roles, are `UI/Components/ContentItemPanel.md §7`.

## 8. States, Validation and Feedback

- **Loading** — rule 3.1.1, the face's one loading state; it is not announced today (section 10,
  item 9). **Empty** — no type left to the reader: the restricted face shows instead (rules 3.1.2
  and 3.2.4). **Submitting** — rules 3.1.6 and 3.1.10; it is not announced today (section 10,
  item 11).
- **Validation** — `UI/Components/ContentItemPanel.md rules 2.31–2.33`. On this face a
  `ContentType` message is placed on the picker; a message for a field this face does not
  render reaches the summary. *(test: contentItemFormPanel.test.tsx — "should place a
  ContentType message on the picker in add", "should summarise a message for a field the
  setting does not render")*
- **The permission refusal** — a submit under a permission basis with no note is refused, and
  the field says why until the reader answers it or moves the basis. *(test:
  contentItemFormPanel.test.tsx — "should refuse to submit a permission basis with no detail and
  say why", "should clear the refusal the moment the reader answers")*
- **Success and failure** — the consumer's: `contribute.tsx` thanks the contributor and navigates
  to `/myposts`, or hands the API's messages back and raises the failure toast.

## 9. Styling and Accessibility

- The face is a `<section class="g2h-content-item-panel">`, named by its heading where
  `titleText` is set and by `ariaLabel` otherwise.
- The picker is a `<fieldset>` with a `<legend>`. Each tile is a button carrying
  `g2h-content-item-type`, `aria-pressed` for the selection, and `data-content-type` set to the
  enum member name; the selected tile adds `g2h-content-item-type-selected` and takes the type's
  colour from the palette in `contentItems.css`. A disabled tile is dimmed with a not-allowed
  cursor.
- Required fields carry `aria-required`. A field with messages carries `aria-invalid` and
  `aria-describedby` pointing at them, and a clean field carries neither. *(test:
  contentItemFormPanel.test.tsx — "should attach each message to its field for a screen
  reader", "should leave a clean field unmarked")*
- *Permission details* carries `g2h-content-item-share-permission`, which resizes vertically only.
- Theme classes, never colours: `submitButtonCssClass`, `loginButtonCssClass`.

## 10. Open Questions and Gaps

1. **Note — the wording of rule 2.7, ruled.** Rule 2.7, as relocated, said the first tile in
   `SortOrder`. Read literally, a reader blocked from the first type would have landed on that
   type's fields with Submit live, which rule 2.6 forbids. The user ruled on 2026-09-27 that the
   tiles are the content types available as a general contribution, ordered by `SortOrder`, with
   every type the reader is blocked from by a per-content-type read-only role removed, so the
   first tile is always one the reader may use. Rules 2.6 and 2.7 now say so. The component
   already lands there (rule 3.2.6; `contentItemFormPanel.tsx` — selectedContentType); it does
   not yet remove the blocked tiles (item 5).
2. **Pass-through.** The add-face properties `ContentItemPanel` cannot reach are
   `UI/Components/ContentItemPanel.md §10 item 6`, which carries the tag — all but
   `typeBlockedText`, which the panel withholds (`UI/Components/ContentItemPanel.md §4.3`).
3. (needs issue) **The doc page needs updating.** `contentItemAddPanelDoc.tsx`'s `validationIssues` row calls
   the mandatory permission note "the one client-side rule the form decides itself"; the
   component also enforces the setting's `Max*Length` ceilings
   (`UI/Components/ContentItemPanel.md rule 2.24`; `contentItemFormPanel.tsx` —
   holdsFieldLengths).
4. (needs issue) **The default `loginHref` composes a route.** Rule 3.2.1 leaves the login
   link's route to the page (§UI20.6.4). Where the page passes no `loginHref`, the face
   composes `/Account/Login?returnUrl=<current path, URI-encoded>` itself, reading the current
   path from the router. `ContentItemPanel` does not forward `loginHref`
   (`UI/Components/ContentItemPanel.md §10 item 6`), so a page rendering this face through the
   panel, as `/posts/contribute` does, cannot supply one today. The edit face shares the form
   engine and declares the same property, but renders no login link. Evidence:
   `contentItemFormPanel.tsx` — `resolvedLoginHref` (lines 395-396 at 70dc72e7), used by
   `renderAdd` alone.
5. (needs issue) **A blocked type's tile renders disabled rather than being removed.** Rules 2.6
   and 3.2.3 (user ruling 2026-09-27) remove every content type the reader is blocked from by
   `ContentItem-{ContentType}-ReadOnly` from the picker. Today the picker renders a tile for
   every type on offer and disables a blocked one, showing `typeBlockedText` (*Not open to this
   account*) in place of its description and as its tooltip. `typeBlockedText` labels only that
   tile, so it is retired with it (section 4.1). Evidence: `contentItemFormPanel.tsx`
   — renderTypePicker, `isTypeBlocked` and `disabled={isTypeBlocked || isFrozen}` (lines 862 and
   879 at 70dc72e7); test: `contentItemFormPanel.test.tsx` — "should close one tile and leave the
   rest of the form live for a narrow block".
6. **Moved to the page documents.** Page gap — `/posts/contribute`: a holder of the global
   read-only role reaches it — now `UI/Pages/Contribute.md §6 item 1`.
7. **Note — `ContentItem-ReadOnly` at the contribution page's door, ruled.** `ContentItem-ReadOnly`
   makes its holder read-only for every content item (§SEC18.6). It is neither the global
   `ReadOnly`, which the page is to refuse at its door (section 6), nor a per-content-type role,
   whose holder the page lets in and whose blocked types the picker removes (rules 2.6 and
   3.2.3). It blocks every content type, so a holder who reaches the page is left with no tile.
   This item asked whether the page should refuse a holder at its door, as it refuses the global
   `ReadOnly`, or let them in to be shown the restricted face. The user ruled on 2026-09-27 that
   a `ContentItem-ReadOnly` holder who opens `/posts/contribute` is let in and shown the
   restricted face (`UI/Components/ContentItemPanel.md rule 2.43`); only the global `ReadOnly` is
   kept off the page. Rule 3.2.4 and section 6 now say so. The page's half is
   `UI/Pages/Contribute.md §6`.
8. (needs issue) **Visible strings that are not properties.** Rule 3.1.9 (§UI20.6.6 rule 1). The
   form engine both writing faces share composes three strings no property sets: the content
   field's placeholder, *Share your {type}…* (`contentItemFormPanel.tsx`, line 1029 at 70dc72e7);
   the sharing basis options' labels (`shareabilityBasisLabels`, line 1055); and the *Submit as*
   options' labels, *Submitted* and *Draft* (`contributorApprovalStatusLabels`, line 1124). The
   title placeholder and the content label compose a fallback too, but `titlePlaceholderText`
   and `contentLabelText` override it. The edit face shares these three and adds its own:
   `UI/Components/ContentItemPanel.Edit.md §10 item 7`.
9. (needs issue) **The loading line is not announced.** Rule 3.1.1 (§UI20.6.6 rule 5). While
   `isLoading` is on, the form engine renders *Loading…* in a plain paragraph with no
   `role="status"` or equivalent, on both writing faces (`contentItemFormPanel.tsx` — the
   `isLoading` branch, lines 1290-1291 at 70dc72e7).
10. (needs issue) **The add face shows the two refusals the restricted face replaces.** Rules
    3.1.2, 3.2.4 and 3.3.1 (user ruling 2026-09-27) have `ContentItemPanel` show the restricted
    face in the add face's place when no content type is left to the reader, whatever the reason
    (`UI/Components/ContentItemPanel.md rule 2.43`). Today the add face renders a refusal itself:
    with no type on offer, an alert reading `noTypesText`, *Contributions are not open for any
    content type right now.*; with types on offer and none the reader may use, an alert reading
    `blockedText`, *Contributions are not open to this account.*, the alert the `addRoles` refusal
    also uses (rule 3.2.5). Evidence: `contentItemFormPanel.tsx` — `renderAdd` (lines 1148-1154 at
    70dc72e7) and `mayAdd` (lines 531-534); tests: `contentItemFormPanel.test.tsx` — "should say so
    when no type is on offer", "should say nothing is open when every row is closed to
    contribution", "should say contributions are closed rather than render an empty panel for a
    blocked account", "should block the whole surface for the entity-type block role". The
    restricted face itself is `UI/Components/ContentItemPanel.Restricted.md §10 item 1`.
11. (needs issue) **The submitting state is not announced.** Rule 3.1.10 (§UI20.6.6 rule 5). While
    `isSubmitting` is on, the form engine disables its buttons — *Submit for review* and *Cancel*
    here, *Save*, *Cancel* and *Delete* on the edit face — and nothing announces the write in
    flight, on either writing face: the file has no `role="status"` or other live region.
    Evidence: `contentItemFormPanel.tsx` — `disabled={isSubmitting}` (lines 1166 and 1174 at
    70dc72e7 on this face; lines 1225, 1233 and 1244 on the edit face).
12. (needs issue) **The login link shows before the sign-in state is known.** Rule 3.2.1 applies
    §UI20.6.6 rule 2, whose last sentence is that a reader whose sign-in state has not yet been
    read back is not sent to sign in. The form engine reads `isAuthenticated` from the auth
    context and not its `isLoading` (`contentItemFormPanel.tsx` — `useAuth`, line 358 at
    70dc72e7), and `renderAdd` shows the login link whenever `isAuthenticated` is false (line
    1138). The context reports a reader as signed out until the current user has been read
    (`authProvider.tsx` — `AuthContext`), so a signed-in reader arriving on a full page load is
    shown *Login to contribute* until then, and pressing it sends them to sign in.
    `AssociationPanel` has the same defect in its login prompt
    (`UI/Components/AssociationPanel.md §10 item 15`), and the card's reaction handler already
    waits on the sign-in state (`UI/Components/ContentItemPanel.md rule 3.2.4`). The page's half
    is `UI/Pages/Contribute.md §6`.
