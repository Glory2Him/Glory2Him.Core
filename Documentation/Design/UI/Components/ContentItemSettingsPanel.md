# 1. ContentItemSettingsPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** none as story files. It has two faces in code — `ContentItemSettingsViewPanel`, the read face, and `ContentItemSettingsModifyPanel`, the modify face — documented inside this file (sections 3.1, 4.3 and 6) by the user's ruling (2026-09-26): the file set mirrors the sample pages, and a face is split into a file of its own only if it grows.
- **Composes:** none
- **Used by:** `/Admin/Posts/:contentItemId` — `src/pages/admin/contentItemModerationDetailPage.tsx`. No component document renders it.
- **Inherits:** §DOM6.4, §DOM6.5, §DOM6.6, §DOM6.7, §ARC12.5.2 business rules 1, 2, 5, 6 and 9, §SEC14.6, §SEC18.6 rules 1 and 2, §UI20.3 principle 2, §UI20.6.4, §UI20.6.5, §UI20.6.6, and the effective-row resolution in `UI/Components/ContentItemPanel.md rules 2.21–2.22`
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItemSettings/contentItemSettingsPanel.tsx` (the dispatcher), `contentItemSettingsViewPanel.tsx`, `contentItemSettingsModifyPanel.tsx` and `contentItemSettings.css` beside it; the models `src/models/components/contentItemSettings/contentItemSettingsTemplate.ts` and `contentItemSettingFeature.ts`; the shared resolver `src/services/views/contentItems/resolveContentItemSetting.ts`; the test `contentItemSettingsPanel.test.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Settings-Panel` — `src/pages/samplePages/components/contentItemSettingsPanelDoc.tsx`

`ContentItemSettingsPanel` shows the `ContentItemSetting` row that actually governs **one** content item — the item's own override where it has one, the content type default otherwise — and lets the people allowed to narrow that one item do so. It answers the question a moderator asks beside a post: which policy is in force here, and what does it change?

It is one dispatcher over two faces, built the way `ContentItemPanel` is. The dispatcher owns everything the faces share: which row wins, whether that row is an override, whether the reader may write, and which face is showing. The **view** face shows the winning row read-only, with **Modify** and **Remove Override**. The **modify** face edits a draft of it, with **Save settings** and **Reset**.

Everything it writes is an item **override**. The per-type default is edited from `/Admin/ContentItemSettings`, never here.

## 2. Business Rules

**2.1 [Must]** The panel is a pure presentation component: props in, events out, no fetching and no mutation. The consumer owns every read and every write. *(§UI20.3 principle 2; code: `contentItemSettingsPanel.tsx` — `ContentItemSettingsPanelProps`)*

**2.2 [Must]** The row shown is the effective row `UI/Components/ContentItemPanel.md rule 2.21` resolves for this one item. *(§DOM6.4; §ARC12.5.2 business rules 1–2; test: "should prefer the item override over the content type default")*

**2.3 [Must]** The exclusion of a soft-deleted row in `UI/Components/ContentItemPanel.md rule 2.21` applies here unchanged. *(§DOM6.6 `IsDeleted`; test: "should exclude a soft-deleted override and fall back to the default")*

**2.4 [Must]** The override is matched on the item as `UI/Components/ContentItemPanel.md rule 2.22` requires. *(test: "should ignore an override belonging to a different content item")*

**2.5 [Must]** The panel resolves with the same shared resolver, over the same collection, as the content item beside it, so the two cannot disagree about which row is in force. How `ContentItemPanel` resolves the row is `UI/Components/ContentItemPanel.md rules 2.21–2.22`, and is not restated here. *(code: `resolveContentItemSetting.ts` — `resolveContentItemSetting`; code: `contentItemModerationDetailPage.tsx` — comment above the settings wiring)*

**2.6 [Must]** The consumer hands over a collection — the type defaults plus this item's override — never a single row. Handing over the defaults alone would silently un-override an overridden item. *(code: `contentItemSettingsPanel.tsx` — `contentItemSettingCollection`)*

**2.7 [Must]** Everything the panel saves is an override of this item. A save seeded from the type default carries the item's `ContentItemId` and an empty id, which the consumer creates; a save seeded from an existing override keeps that override's id, which the consumer updates. *(test: "should save the edits as an override when the form was seeded from the default"; test: "should save onto the existing override when one was already in force")*

**2.8 [Must]** Every field the form does not edit is carried through to the saved row unchanged: this panel edits only the fields of rule 2.9, and the others — the row's control fields, and the type's presentation and contribution shaping, which are amendable on the settings admin page — are not its to change (rule 2.9). *(test: "should carry every field the form does not edit through to the saved row")*

**2.9 [Must]** The form edits the six facet pairs of §DOM6.5 and `LimitReactionsToLoveOnly` (§DOM6.7), and nothing else. §ARC12.5.2 business rule 9 makes more of a row amendable — every field the settings admin page edits, the type's presentation (name, icon, sort order, description) and its contribution shaping (title, author, the length ceilings) among them (user ruling 2026-09-27) — but those are edited on that page, `/Admin/ContentItemSettings/{id}`, and not here. *(code: `contentItemSettingsModifyPanel.tsx` — header comment; `contentItemSettingFeature.ts` — `contentItemSettingFeatureFields`; §ARC12.5.2 business rule 9)*

**2.10 [Won't]** The panel never writes the per-type default. The default is `Administrators`-only and is edited from `/Admin/ContentItemSettings`, which is why the panel has no branch for it. *(§ARC12.5.2 business rule 6; code: `contentItemSettingsPanel.tsx` — comment on `canAdministerSettings`; code: `contentItemSettingsModifyPanel.tsx` — header comment)*

**2.11 [Must]** The writes — **Modify**, **Remove Override**, and so the modify face — are offered to `Administrators` and to the publisher tier for the item's content type: `Publishers`, `ContentItem-Publishers` and `ContentItem-{ContentType}-Publishers`. The grant set is the component's own and fixed, as the code has it, mirroring the override write gate; no page supplies it. That is this document's answer to the question the user left to it (section 10, item 1). *(§ARC12.5.2 business rule 6; §SEC18.6 rule 1; user, 2026-09-27; code: `contentItemSettingsPanel.tsx` — `canAdministerSettings`; test: "should offer the writes to %s")*

**2.12 [Must]** Any `ReadOnly` whose scope covers the row — `ReadOnly`, `ContentItem-ReadOnly` or `ContentItem-{ContentType}-ReadOnly` — is asked first and withholds every write, `Administrators` included. A block scoped to another content type is silent. *(§SEC18.6 rule 2; test: "should withhold the writes from an administrator blocked by %s"; test: "should ignore a block scoped to a different content type")*

**2.13 [Must]** A reviewer is offered no write. A reviewer reads the settings. *(test: "should offer no write to a reviewer, who cannot administer settings")*

**2.14 [Must]** With no content item named, nobody is offered a write, because there is nothing to override. Only the type default can resolve. *(test: "should offer no write when no content item is named")*

**2.15 [Must]** Every gate decides only what is rendered. The service re-decides the save and the removal against the stored row. *(§SEC14.6; §ARC12.5.2 business rule 6; code: `contentItemSettingsPanel.tsx` — comment on `canAdministerSettings`)*

**2.16 [Must]** **Remove Override** is offered against an override only, never against a type default: every content type must always have a live default, and the server refuses to remove one. *(§ARC12.5.2 business rule 5; test: "should not offer removal against a content type default")*

**2.17 [Must]** The panel raises which override the reader wants removed, and the consumer asks for confirmation (section 8). *(test: "should raise the override row when removal is taken"; code: `contentItemSettingsViewPanel.tsx` — the **Remove Override** `onClick`; code: `contentItemModerationDetailPage.tsx` — `overrideToRemove`)*

**2.18 [Must]** The read face always says which row is in force, Default or Override. *(code: `contentItemSettingsViewPanel.tsx` — `showRibbon` comment; test: "should fall back to a badge when the ribbon is turned off")*

**2.19 [Should]** Where the override differs from the type default, the differing switches are marked, and a legend explains the marking. *(test: "should mark only the settings that differ from the content type default"; test: "should explain the marking rather than leaving red unexplained")*

**2.20 [Must]** While the consumer is persisting, the buttons are frozen, so one click is one write. *(test: "should freeze the buttons so one click is one write")*

**2.21 [Could]** The panel opens on the view face unless `mode` says otherwise. *(code: `contentItemSettingsPanel.tsx` — `mode` comment; test: "should land straight on the modify face when the mode says so")*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**The dispatcher**

**3.1.1** With `mode` absent or `view`, the view face renders. With `mode="modify"`, the modify face renders — but only for a reader the write gate admits (section 3.2); anybody else gets the view face. A committed save returns it to the view face in either mode (rule 3.1.4). *(code: `contentItemSettingsPanel.tsx` — the `mode === 'modify' || isModifyTaken` branch; test: "should land straight on the modify face when the mode says so"; user, 2026-09-27)* ≠ item 3

**3.1.2** Taking **Modify** switches to the modify face in place and raises `onModify`. The face switch is internal state; `onModify` is a notification. *(test: "should notify the consumer when Modify is taken")*

**3.1.3** A change to `contentItemId` or to `mode` drops a Modify the reader took earlier, back to the view face. *(code: `contentItemSettingsPanel.tsx` — the `useEffect` on `[contentItemId, mode]`)*

**3.1.4** A committed **Save settings** raises `onModified` and returns the panel to its view face, in every mode — including when it opened straight on the modify face with `mode="modify"`. The view face shows the consumer's collection: a page that has re-read shows the saved override, one that has not shows the row it still holds. *(test: "should return to the read face once a save is committed"; user, 2026-09-27)* ≠ item 3

**3.1.5** The modify face is keyed on the resolved row's id. A save landing, or an override removed under the form, starts a fresh draft seeded from the new winner rather than an old draft edited on top of a row that no longer governs. *(code: `contentItemSettingsPanel.tsx` — `key={contentItemSetting?.id ?? 'none'}`)*

**The view face — `ContentItemSettingsViewPanel`**

**3.1.6** `isLoading=true` shows a spinner in place of the settings, and the spinner announces the load (§UI20.6.6 rule 5). *(code: `contentItemSettingsViewPanel.tsx` — the `isLoading` branch; code: `coreUI/spinner.tsx` — `role="status"`)*

**3.1.7** A state in which no row resolves is not a product state. A setting row always exists for every content type (§ARC12.5.2 business rule 5), so its absence is a configuration or seeding fault, not a state the panel is designed for, and no rule designs what the view face shows then. What it shows today — "No content settings apply to this item yet.", with no button — is not a requirement (section 10, item 4). A page that renders the panel before its settings read lands is the page's gap (section 10, item 5). *(user, 2026-09-27; code: `contentItemSettingsViewPanel.tsx` — the `contentItemSetting == null` branch)*

**3.1.8** Every switch renders disabled — the same rows, in the same order, as the modify face. *(test: "should render every switch disabled"; code: `contentItemSettingsViewPanel.tsx` — header comment)*

**3.1.9** With `showRibbon` on (the default) and a row resolved, a corner ribbon names the scope, `Default` or `Override`. With `showRibbon` off, the scope moves to an inline badge instead. The two never show together, and with no row resolved neither shows. *(test: "should name the default scope on the read face by default"; test: "should name the override scope when the item carries its own row"; test: "should fall back to a badge when the ribbon is turned off"; code: `contentItemSettingsViewPanel.tsx` — `wearsRibbon`)*

**3.1.10** A sentence under the switches says what the row is: on an override, "These settings apply to this content item alone, overriding the content type default."; on the default, "These are the content type defaults. This item has no settings of its own." *(code: `contentItemSettingsViewPanel.tsx`)*

**3.1.11** A switch is marked only when the winning row is an override, a type default resolves, and the two values differ. Nothing is marked when the default is what governs, or when no default resolves to compare against. The legend "Highlighted settings differ from the content type default." shows only when a row is marked. *(test: "should mark only the settings that differ from the content type default"; test: "should mark nothing when the content type default is what governs"; test: "should mark nothing when no content type default resolves")*

**3.1.12** **Modify** renders for an admitted reader when a row resolves. **Remove Override** renders beside it only when the row is this item's override; against a default it is absent, not disabled. *(test: "should not offer removal against a content type default"; code: `contentItemSettingsViewPanel.tsx` — the comment on the writes)*

**3.1.13** `isSubmitting=true` disables **Modify** and **Remove Override**. *(test: "should freeze the buttons so one click is one write")*

**3.1.14** `showBorder` on (the default) draws the card with a border; off, it removes it. The same answer holds on the modify face. *(test: "should draw the card bordered by default"; test: "should take the border away when the surface turns it off"; test: "should carry the same answer onto the modify face")*

**The modify face — `ContentItemSettingsModifyPanel`**

**3.1.15** The modify face wears no ribbon, whatever `showRibbon` says. A sentence says what the save will do instead: on an override, "Saving updates the settings for this content item alone."; on the default, "Saving creates settings for this content item alone. The content type default is left unchanged." *(test: "should wear no ribbon on the modify face"; code: `contentItemSettingsModifyPanel.tsx`)*

**3.1.16** The switches are live over a draft copy seeded from the values the view face displayed. *(test: "should seed the form from the values the read face displayed")*

**3.1.17** Marking follows the live draft against the type default: a switch moved away from the default is marked as it moves, and one moved back goes quiet. It does not ask whether an override exists yet, because saving from here always writes one. *(test: "should follow the draft as it moves on the modify face"; code: `contentItemSettingsModifyPanel.tsx` — `differsFromDefault`)*

**3.1.18** **Save settings** raises `onModified` with the row rule 2.7 describes. While `isSubmitting` is on, it reads "Saving..." and is disabled, and **Reset** is disabled too. *(code: `contentItemSettingsModifyPanel.tsx`)*

**3.1.19** **Reset** takes the draft back to the values the view face showed — not to the type default and not to the last save — stays on the modify face, and raises `onReset`. *(test: "should revert uncommitted edits to the values the read face displayed"; test: "should stay on the modify face and notify the consumer")*

**3.1.20** The same holds on the modify face, which `mode="modify"` reaches when no row resolves: what it shows then — "There are no content settings to modify for this item.", with no form and no button — is not a requirement (rule 3.1.7). *(user, 2026-09-27; code: `contentItemSettingsModifyPanel.tsx` — the `draft == null` branch)*

**3.1.21** Every visible string either face renders, and every string either renders for a screen reader alone, is a property whose default is today's text (§UI20.6.6 rule 1). *(user, 2026-09-27)* ≠ item 10

**3.1.22** While `isSubmitting` is on, the save in flight — **Save settings** reading "Saving..." (rule 3.1.18) — is announced, `role="status"` or equivalent, as a loading state is (§UI20.6.6 rule 5). *(user, 2026-09-27)* ≠ item 11

### 3.2 Driven by roles

**3.2.1** The writes are offered to `Administrators`, `Publishers`, `ContentItem-Publishers` and `ContentItem-{ContentType}-Publishers`, where `{ContentType}` is the enum member name of the `contentType` prop. Each tier is enough on its own. *(test: "should offer the writes to %s"; §SEC18.6 — the role segment is the `ContentType` enum member name)*

**3.2.2** A publisher of another content type is offered nothing. *(test: "should offer no write to a publisher of another content type")*

**3.2.3** A signed-out reader is offered no write. *(test: "should offer no write to a signed-out reader")*

**3.2.4** `Reviewers` and `ContentItem-Reviewers` are offered no write. *(test: "should offer no write to a reviewer, who cannot administer settings")*

**3.2.5** `ReadOnly`, `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` withhold every write from every grant, `Administrators` included. *(test: "should offer no write to a blocked administrator"; test: "should withhold the writes from an administrator blocked by %s")*

**3.2.6** Owning the content item grants nothing: the panel has no `[OWNER]` gate. *(code: `contentItemSettingsPanel.tsx` — no ownership term in `canAdministerSettings`)*

**3.2.7** Reading the settings has no role gate inside the component. Where the panel is shown is the page's decision (section 6). *(code: `contentItemSettingsViewPanel.tsx` — the settings render for every reader)*

### 3.3 Combinations

**3.3.1** The write gate is asked in this order: a content item must be named, then the block, then the grant. `mode="modify"` and a Modify taken earlier choose the face only after the gate has admitted the reader, so neither can open the form for somebody the gate refuses. *(code: `contentItemSettingsPanel.tsx` — `canAdministerSettings`, and the branch that requires it)*

**3.3.2** A block covers the row only when its scope does: `ContentItem-Quote-ReadOnly` held beside `Administrators` leaves a devotional's writes offered. *(§SEC18.6 rule 2; test: "should ignore a block scoped to a different content type")*

**3.3.3** **Remove Override** needs the write gate **and** an override in force. *(code: `contentItemSettingsViewPanel.tsx` — `canAdministerSettings && … isOverride &&`)*

**3.3.4** `isSubmitting` only subtracts: it disables buttons the gate already showed, and never shows one. *(code: `contentItemSettingsViewPanel.tsx`, `contentItemSettingsModifyPanel.tsx` — `disabled={isSubmitting}`)*

**3.3.5** `showRibbon` only moves the scope between the ribbon and the badge; it never hides it. *(code: `contentItemSettingsViewPanel.tsx` — `showRibbon` comment)*

### 3.4 Role matrix

"Owner" here is the content item's contributor, matched on account id. Ownership confers nothing on this panel (rule 3.2.6).

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| A row resolves — the settings, read-only, with their scope | ✅ Yes² | ✅ Yes² | ✅ Yes² | ✅ Yes² | ✅ Yes² | ✅ Yes² |
| `contentItemId` set, a row resolves, no `ReadOnly` covering it — **Modify** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes |
| As above, the row is this item's override — **Remove Override** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes |
| As above, the row is the type default — **Remove Override** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `mode="modify"`, `contentItemId` set, no `ReadOnly` covering the row — the modify face, **Save settings** and **Reset** | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes |
| `contentItemId` absent — **Modify** or the modify face | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Holding `ReadOnly`, `ContentItem-ReadOnly` or `ContentItem-{ContentType}-ReadOnly` — any write | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Holding a `ReadOnly` scoped to another content type only — **Modify** | ➖ n/a | ❌ No | ❌ No | ❌ No | ✅ Yes¹ | ✅ Yes |
| Owner also holding `ContentItem-{ContentType}-Publishers` — **Modify** | ➖ n/a | ➖ n/a | ✅ Yes | ➖ n/a | ➖ n/a | ➖ n/a |
| `isSubmitting=true` — **Modify**, **Remove Override** or **Save settings** enabled | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ `Publishers`, `ContentItem-Publishers`, or `ContentItem-{ContentType}-Publishers` for this item's content type. A publisher of another content type gets ❌ No.
² Inside the component. The one shipped page, `/Admin/Posts/:contentItemId`, admits `Administrators` only (section 6), and the sample page is admin-only too.

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `mode` | `'view' \| 'modify'` | absent — the view face | Lands the panel on a face. `modify` is still subject to the write gate. | the dispatcher only — it chooses the face |
| `contentItemId` | `string` | absent | The item being governed. Absent, only a type default can resolve and no write is offered. | modify face (`contentItemId`, unchanged); the dispatcher's resolution and write gate |
| `contentType` | `ContentType` | required | The item's content type. With `contentItemId` it is all the resolution needs, and it composes the narrow role names. | the dispatcher only |
| `contentItemSettingCollection` | `ReadonlyArray<ContentItemSetting>` | required | The candidate rows: the type defaults and this item's override. | the dispatcher only — resolved into the faces' `contentItemSetting`, `contentTypeDefault` and `isOverride` |
| `isLoading` | `boolean` | `false` | Shows the view face's spinner. | view face; modify face (forwarded, not honoured — section 10) |
| `isSubmitting` | `boolean` | `false` | Freezes the buttons while the consumer persists. | view face; modify face |
| `showRibbon` | `boolean` | `true` | The view face's scope ribbon; off, a badge instead. | view face only — the modify face never wears one (rule 3.1.15) |
| `showBorder` | `boolean` | `true` | Draws the card with a border. On by default, because the panel stands in a sidebar beside other cards. | view face; modify face |
| `cssClass` | `string` | `''` (the faces' default) | Classes appended to the card. | view face; modify face |
| `titleText` | `string` | `'Content Settings'` (the faces' default) | The card's heading. | view face; modify face |
| `ariaLabel` | `string` | `'Content settings'` (the faces' default) | The accessible name of the panel's region. | view face; modify face |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onModify` | none | **Modify** is taken on the view face. A notification: the face switch is internal. |
| `onReset` | none | **Reset** is taken on the modify face. A notification: the revert is internal. |
| `onModified` | `ContentItemSetting` | **Save settings** is taken. The complete row to persist, `contentItemId` stamped; an empty id is a create, the override's own id an update (rule 2.7). |
| `onOverrideRemoved` | `ContentItemSetting` | **Remove Override** is taken. The override row, id and all, to hard delete. Never raised against a type default. |

### 4.3 Pass-through properties

Per §UI20.6.5. The dispatcher is the parent of both faces.

| Dispatcher property | View face | Modify face |
| --- | --- | --- |
| `isLoading` | unchanged | unchanged — declared by the face's props, not used (section 10) |
| `isSubmitting` | unchanged | unchanged |
| `showRibbon` | unchanged | withheld — a recorded exception, below |
| `showBorder` | unchanged | unchanged |
| `cssClass`, `titleText`, `ariaLabel` | unchanged | unchanged |
| `contentItemId` | not forwarded — the face has no such property | unchanged (the face requires it) |
| `onOverrideRemoved` | unchanged | not forwarded — the face does not raise it |
| `onReset` | not forwarded — the face does not raise it | unchanged |
| `onModify` | wrapped: the dispatcher opens the modify face, then forwards | not forwarded — the face does not raise it |
| `onModified` | not forwarded — the face does not raise it | wrapped: the dispatcher closes the form, then forwards |

The exceptions this panel records (§UI20.6.5):

- **`showRibbon` is withheld from the modify face.** The modify face wears no ribbon whatever `showRibbon` says (rule 3.1.15): the read face's ribbon names the row in force, and on the modify face the scope is about to change. *(code: `contentItemSettingsModifyPanel.tsx` — "NO RIBBON ON THIS FACE, deliberately")*
- **The faces' `contentItemSetting`, `contentTypeDefault`, `isOverride` and `canAdministerSettings` are withheld from the page.** The dispatcher decides them — from `contentItemSettingCollection`, `contentType`, `contentItemId` and the signed-in roles — so the two faces cannot answer "which row wins" differently. *(code: `contentItemSettingsTemplate.ts` — `ContentItemSettingsTemplateProps` comment)* The page can steer the first three through the collection it hands over. It cannot steer `canAdministerSettings`: the role sets behind it are fixed in the dispatcher, which is this document's answer to the question the user left to it (rule 2.11; section 10, item 1).

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The settings in force, read-only, with their scope | Every persona the page shows the panel to (rule 3.2.7) | None — a read (§SEC18.6) | ✅ Allowed | Shown | §SEC14.7 posture C rule 2: `ContentItemSetting` is public-read |
| **Modify** (`onModify`) | `Administrators`, and the publisher tier for the item's content type (rule 2.11) | `ReadOnly`, `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` for the item's type (rule 2.12) | ❌ Refused: no button; the settings stay read-only (rule 3.2.5) | Not offered (rule 3.2.3) | §ARC12.5.2 business rule 6; §SEC18.6 rule 1 |
| **Save settings** (`onModified`) | As **Modify**, on the modify face (rule 3.3.1) | The same three (rule 2.12) | ❌ Refused: the modify face never opens (rule 3.3.1) | Not offered | §ARC12.5.2 business rule 6; §SEC18.6 rule 1 |
| **Reset** (`onReset`) | As **Modify**, on the modify face (rule 3.3.1) | The same three, through the same gate | ❌ Refused, with the modify face | Not offered | Nothing — no request |
| **Remove Override** (`onOverrideRemoved`) | As **Modify**, against the item's override only (rules 2.16 and 3.3.3) | The same three (rule 2.12) | ❌ Refused: no button | Not offered | §ARC12.5.2 business rules 5 and 6 |

**5.1** Every gate is a render courtesy. The service re-decides every save and removal against the stored row, and must (§SEC14.6). A hidden button is never the authorization boundary.

**5.2** The render gate mirrors the override write gate of §ARC12.5.2 business rule 6: the block first, from the same scope as the grant, then `Administrators` or the publisher tier for the content type (§SEC18.6 rules 1 and 2).

**5.3** The panel composes the narrow role names from the `contentType` prop. The server does not trust that value: an override's `ContentType` is derived from the content item it names (§ARC12.5.2 business rule 6). A wrong prop can therefore only mis-render a button, never widen a write.

**5.4** The signed-in identity and roles come from the auth context (`useAuth`) and are used for rendering only. The server decides against the security context on its own inbound envelope (§SEC14.6 rule 1).

**5.5** Reading is not gated: `ContentItemSetting` is public-read (§SEC14.7 posture C rule 2). Where the panel appears is the page's decision (section 6).

## 6. Composition and Usage

**The family.**

```
ContentItemSettingsPanel              ONE item's settings, on whichever face is asked for
├── ContentItemSettingsViewPanel        VIEW: the winner read-only + Modify / Remove Override
└── ContentItemSettingsModifyPanel      MODIFY: the feature form + Save settings / Reset
```

The dispatcher resolves; the faces render. Adding a third face is one more branch in the dispatcher, not a second resolution elsewhere. *(code: `contentItemSettingsPanel.tsx` — header comment)* The same dispatcher-and-faces shape is `ContentItemPanel`'s — `UI/Components/ContentItemPanel.md §6`.

**Both faces render the same rows** from one table, `contentItemSettingFeatureFields`: Tags, Reactions, Links, Attachments, Comments and Bible references, each as "… are shown" above "… can be added", then **Limit reactions to love only** on its own with its description. The table is shared with the `/Admin/ContentItemSettings` detail page, so which field carries which label cannot drift. *(code: `contentItemSettingFeature.ts`)*

**Where it is used.**

| Route | Page | Placement and wiring |
| --- | --- | --- |
| `/Admin/Posts/:contentItemId` | `src/pages/admin/contentItemModerationDetailPage.tsx` | In the decision column, beneath `ReviewPanel` — the round reads first and the standing policy below it. Passes `contentItemId`, `contentType`, `contentItemSettingCollection`, `isSubmitting` (either write pending), `onModified`, `onOverrideRemoved`, `showBorder` and `cssClass="mt-4"`. The route admits `Administrators` only (`securityMatrix.tsx` — `contentItems.view`). |
| `/SamplePages/Components/Content-Item-Settings-Panel` | `src/pages/samplePages/components/contentItemSettingsPanelDoc.tsx` | The live demo: a security-context board, the collection (override, default only, nothing), `mode`, `showRibbon`, `showBorder`, `isSubmitting`, and a side-by-side comparison. Admin-only. |

On the moderation page the panel reads the same collection that the page's `ContentItemPanel` and heading already resolve against, so showing it adds no read. *(code: `contentItemModerationDetailPage.tsx` — comments above the settings wiring)*

## 7. Dependencies

**Data the consumer supplies:** the candidate collection. The shipped consumer reads it with `contentItemSettingService.useGetEffectiveSettingsFor([contentItemId])`, which returns the type defaults and this item's override in one result.

**API endpoints** (called by the consumer, never the component):

| Concern | Endpoint |
| --- | --- |
| The candidate rows | `GET api/contentitemsettings?$filter=contentItemId eq null` (the defaults) and `GET api/contentitemsettings?$filter=contentItemId eq {id}` (the override), read together |
| **Save settings** — a create | `POST api/contentitemsettings`; the consumer mints the id when the row's id is empty |
| **Save settings** — an update | `PUT api/contentitemsettings`, the whole row |
| **Remove Override** | `DELETE api/contentitemsettings/{id}/Hard` — the permanent removal; the server refuses a type default |

The create-or-update choice is made on the row's id by `useCreateOrUpdateContentItemSettingOverride`. Two administrators creating the same override at once both post, and the second is refused with a 409 by the one-override-per-item index (§ARC12.5.2 business rule 4). The code comment on it names a move of the choice server-side that no design holds: section 10, item 12. *(code: `contentItemSettingService.ts` — comment on `useCreateOrUpdateContentItemSettingOverride`)*

**Indirect dependencies:** the signed-in identity and roles through the auth context (`useAuth`), for the render gates.

## 8. States, Validation and Feedback

**Loading.** `isLoading` shows a spinner on the view face, which announces the load (rule 3.1.6). The modify face has no loading state (section 10, item 2). The save's own progress — **Save settings** reading "Saving..." — is a saving state, which §UI20.6.6 rule 5 announces as it does a load (rule 3.1.22); nothing announces it today (section 10, item 11).

**Empty.** Not a product state: a setting row always exists, and what either face shows without one is not a requirement (rules 3.1.7 and 3.1.20; section 10, item 4).

**Error and validation read-back.** The panel has no error or validation surface of its own. The consumer reports the outcome: the shipped page toasts "Content settings saved." or the API's own message, falling back to "We could not save these content settings. Please try again later.", and "Content settings override removed." or "We could not remove this override. Please try again later." *(code: `contentItemModerationDetailPage.tsx` — `saveContentItemSettingAsync`, `removeContentItemSettingOverrideAsync`)*

**Confirmation — the page confirms, the panel does not.** **Remove Override** raises `onOverrideRemoved` with the override row and does nothing else. The consumer holds that row, asks, and sends the removal only once the reader confirms. On `/Admin/Posts/:contentItemId`:

- the page keeps the row in `overrideToRemove` between the click and the answer;
- a `ConfirmDialog` titled "Remove override?" says "This content item will go back to its content type defaults. The override is deleted permanently and cannot be recovered.", with a **Remove Override** confirm button;
- confirming sends the hard delete and clears the row; cancelling clears it and sends nothing.

*(code: `contentItemModerationDetailPage.tsx` — `overrideToRemove`, the second `ConfirmDialog`; test: "should raise the override row when removal is taken")* **Save settings** is not confirmed. This is the split `ReviewCommentPanel` follows for withdrawing a comment — `UI/Components/ReviewCommentPanel.md rule 2.14`.

**Freshness.** The panel shows the world as of the last props it was handed. The shipped page's writes invalidate every settings read, so the collection is re-read after a save or a removal. *(code: `contentItemSettingService.ts` — `invalidateContentItemSettingReads`)*

**Double submission.** `isSubmitting` freezes the buttons (rule 2.20); the shipped page sets it while either write is pending.

## 9. Styling and Accessibility

**9.1** Both faces are a `card`. `showBorder` adds `border` or `border-0` — spelled out both ways, because the theme's `.card` has no border of its own. `cssClass` is appended. *(code: both faces — `borderCss`)*

**9.2** The scope ribbon is the shared `.g2h-corner-ribbon` shape (`coreUI.css`), with `g2h-has-corner-ribbon` on the card. Its colour comes from `.g2h-settings-ribbon[data-setting-scope]` in `contentItemSettings.css`: grey (`#6c757d`) for Default, the same grey the approval ribbon gives Draft; purple (`#6f42c1`) for Override, which no approval status uses. The badge fallback is `text-bg-primary` for Override and `text-bg-secondary` for Default.

**9.3** A marked switch sits inside `.g2h-settings-overridden`: a red label at weight 600, a red switch border, and a red fill when checked. The border and the weight carry the mark as well as the colour, so a switch turned off is marked too, and the mark survives a reader who cannot tell the hues apart. *(code: `contentItemSettings.css`)*

**9.4** Each face's content is a `role="region"` named by `ariaLabel` (default "Content settings"), headed by an `h5` carrying `titleText`. *(test: "should expose the read face as a named region"; test: "should expose the modify face as a named region"; test: "should carry the surface’s own label onto the region")*

**9.5** Every switch is tied to its own label through a generated id, so it has an accessible name. *(code: `src/components/coreUI/formSwitch.tsx` — `useId`)*

**9.6** The panel has no responsive rules of its own: it is one stacked column, built for a sidebar.

## 10. Open Questions and Gaps

1. **Note — who may change an item's settings, ruled.** This item asked whether the write gate's grant set — fixed in `contentItemSettingsPanel.tsx` (`canAdministerSettings`) as `Administrators` and the publisher tier for the item's content type, with no property that lets a page change it — should become an overridable property, or stay a fixed mirror of §ARC12.5.2 business rule 6. The user ruled on 2026-09-27 that `Administrators` and the publisher tier for the item's content type (`Publishers`, `ContentItem-Publishers`, `ContentItem-{ContentType}-Publishers`) may define and remove an item's override settings, through this panel, on the admin page for the item, and that this document answers the question, with its security and access matrix. This document's answer, not the user's: the grant set stays the component's own and fixed, as the code has it, mirroring §ARC12.5.2 business rule 6 and §SEC18.6 rule 1, with no page-supplied role property. Rule 2.11 says so, and section 5 opens with the security and access matrix. The code already behaves so. That a publisher who is not an administrator cannot reach that page today is item 6.
2. (needs issue) **`isLoading` does not reach the modify face's rendering.** The dispatcher forwards `isLoading` to `ContentItemSettingsModifyPanel`, whose props declare it through `ContentItemSettingsTemplateProps`, but the face never reads it. With `mode="modify"` and an empty collection still loading, the face shows the "no content settings to modify" message.
3. (needs issue) **A save under `mode="modify"` stays on the form.** Rules 3.1.1 and 3.1.4 (user ruling 2026-09-27) return the panel to its view face after a committed save, in every mode. The dispatcher's own comment says a committed save closes the form back to the read face, but it clears only the internal flag, and the branch is `mode === 'modify' || isModifyTaken`, so with `mode="modify"` the modify face stays open after a save (`contentItemSettingsPanel.tsx` — the `onModified` wrapper and that branch). The test covers only the case without `mode` (test: "should return to the read face once a save is committed").
4. **Note — the state with no row, ruled.** This item asked whether to keep the comment in `contentItemSettingsTemplate.ts`, which says the modify face "cannot be reached from" the state where no row resolves, or the behaviour: `mode="modify"` reaches it, and the face renders "There are no content settings to modify for this item." without a named region. The user ruled on 2026-09-27 that there will always be a setting row for each content type — the default that also decides the tiles offered when a content item is added — and that its absence is a bigger problem, not a panel state. No rule designs that state, on either face, and what the code shows then is not a requirement (rules 3.1.7 and 3.1.20). The panel shows the default that governs the item, and lets an administrator or a publisher store an override (rule 2.11).
5. **Note — the shipped page renders the panel before its settings read lands, ruled.** `/Admin/Posts/:contentItemId` hands over `contentItemSettings ?? []` without the read's loading state, so while the settings read is still in flight the panel says "No content settings apply to this item yet." (rule 3.1.7). The user ruled on 2026-09-26 that a setting always applies — the item's own override where one exists, its content type default otherwise (`UI/Components/ContentItemPanel.md rule 2.39`) — so that message is never true of an item in the product. The page renders its panels before the read that supplies the setting has landed; that is the page's gap, recorded as `UI/Pages/ContentItemModerationDetailPage.md §6 item 2`.
6. **Note — the publisher-tier writes are unreachable on the shipped page.** The panel offers its writes to the publisher tier (rule 2.11), but its only page, `/Admin/Posts/:contentItemId`, admits `Administrators` only (`securityMatrix.tsx` — `contentItems.view`), so a publisher who is not an administrator never reaches the panel. The user's ruling of 2026-09-26 answers whether that is intended: every publisher gets Moderate, whose hook a page may route to that page, where the item's other moderation tasks are performed (`UI/Components/ContentItemPanel.md rule 2.38`). The user's ruling of 2026-09-27 (item 1) confirms that the page must admit them. The gap is the page's, recorded as `UI/Pages/ContentItemModerationDetailPage.md §6 item 1`.
7. **Note — §SEC14.7 posture C rule 1 and the override exception, settled.** Posture C rule 1 said every `ContentItemSetting` write, hard removal included, is `Administrators` only; it dates from `0f031f3c` (2026-08-03). Commit `25120313` (2026-09-06) later added §ARC12.5.2 business rule 6 and §SEC18.6 rule 1's deliberate exception on the same question — who may write a `ContentItemSetting` — and they admit the publisher tier for the row's content type on an item override. The later rule settles the earlier one (§UI20.6.4 case 1), and the user's ruling of 2026-09-27 (item 1) confirms it. Posture C rule 1 now names the override exception itself. Rule 2.11 and the rows that show it follow it and carry no marker for it.
8. (needs issue) **The doc page needs updating.** Its properties table, `contentItemSettingsPanelDoc.tsx` — `panelProps`, omits `isLoading`, which `ContentItemSettingsPanelProps` declares and the view face renders (rule 3.1.6).
9. (needs issue) **Stale code comment — the foundation's add validation.** `contentItemSettingsModifyPanel.tsx` says the foundation "refuses an add with no ContentTypeName" (line 30 at 70dc72e7), and that its add validation "requires a ContentTypeName, a description within its ceiling and a SortOrder of zero or more, so a create that dropped them would be a 400" (lines 99-101). The foundation only caps `ContentTypeName` at 50 characters (`ContentItemSettingService.Validations.cs`, lines 200-201 at 70dc72e7), and the column is nullable (`StorageBroker.ContentItemSetting.Configurations.cs`, lines 49-51); it refuses a description only over 500 characters and a `SortOrder` only below zero (lines 203-207), so a create that dropped those fields would not be refused either. Rule 2.8's requirement stands on its test; the comment's reason does not.
10. (needs issue) **Visible strings that are not properties.** Rule 3.1.21 (§UI20.6.6 rule 1). Only `titleText` and `ariaLabel` are properties. The view face fixes its scope ribbon and badge, *Default* and *Override* (`contentItemSettingsViewPanel.tsx`, lines 88 and 139 at 70dc72e7); *Features* (line 129); the legend, "Highlighted settings differ from the content type default." (line 185); the two scope sentences (lines 191-194); and **Modify** and **Remove Override** (lines 214 and 222). Its loading spinner is named for a screen reader by the `Spinner`'s own default label, *Loading...*, which the face passes no `label` to replace (`contentItemSettingsViewPanel.tsx`, line 118; `coreUI/spinner.tsx`) — a string §UI20.6.6 rule 1 covers too (user ruling 2026-09-27). The modify face fixes *Features* (`contentItemSettingsModifyPanel.tsx`, line 121); the two save sentences (lines 125-127); **Save settings** and *Saving...* (line 172); and **Reset** (line 188). Both faces take the feature titles and switch labels, and the love-only label and description, from a table shared with the `/Admin/ContentItemSettings` detail page (`contentItemSettingFeature.ts` — `contentItemSettingFeatureFields`). The two no-row messages are left out: that state is not designed (item 4).
11. (needs issue) **The save in flight is not announced.** Rule 3.1.22 (§UI20.6.6 rule 5, user
    ruling 2026-09-27). While `isSubmitting` is on, **Save settings** reads "Saving..." and is
    disabled, with **Reset** disabled beside it (rule 3.1.18), but the text changes inside the
    button alone: no `role="status"` or other live region carries it, on either face, so a screen
    reader is not told the save is under way. Evidence: `contentItemSettingsModifyPanel.tsx` — the
    **Save settings** button (lines 168-173 at 70dc72e7); neither face's file carries a
    `role="status"` or `aria-live`.
12. (needs issue) **The create-or-update comment names work no design holds.** Section 7
    records that `useCreateOrUpdateContentItemSettingOverride` decides between a `POST` and a
    `PUT` on the row's id, so two administrators creating the same override at once both post and
    the second is refused with a 409 by the one-override-per-item index (§ARC12.5.2 business rule
    4); the refusal carries the server's own message, so nothing is lost silently. Its comment
    says that #209's `ContentItemSettingsProcessingService` is to move the choice server-side and
    remove that window (`src/services/foundations/contentItemSettingService.ts`, lines 108-119 at
    70dc72e7). No design rule moves the choice server-side: #209, now closed, was the
    effective-value merge ("PROCESSINGS: Build ContentItemSettingsProcessingService For The
    Effective-Value Merge"), and §ARC12.5.2 leaves a processing service beneath the orchestration
    to single-entity work such as that merge, not yet built, and names no move of the choice. The comment is stale, and the work is to correct it; where
    the choice is made stays as it is until a design moves it.
