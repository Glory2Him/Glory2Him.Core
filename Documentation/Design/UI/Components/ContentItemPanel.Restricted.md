# 1. ContentItemRestrictedPanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), which shows it in the add face's place when the reader has no content type left to contribute (`UI/Components/ContentItemPanel.md rule 2.43`); through it `/posts/contribute` — `src/pages/contribute.tsx`. Not built, so nothing renders it today.
- **Inherits:** `UI/Components/ContentItemPanel.md rules 2.1 and 2.43`; §UI20.6.4; §UI20.6.5; §UI20.6.6
- **Source:** not built — designed by the user's ruling of 2026-09-27 (section 10, item 1)
- **Sample page:** none — not built

The restricted face of `ContentItemPanel`: what a signed-in reader sees on the contribution
surface when there is no content type left for them to contribute. It stands in the add face's
place, and says that contributions are not being taken.

It is a very basic panel: wording, and nothing to press.

## 2. Business Rules

**2.1 [Must]** The face is shown instead of `ContentItemAddPanel` when the reader has no content type left to contribute. `ContentItemPanel` decides when (`UI/Components/ContentItemPanel.md rule 2.43`). *(user, 2026-09-27)* ≠ item 1

**2.2 [Must]** It is a very basic panel with wording. *(user, 2026-09-27)* ≠ item 1

**2.3 [Must]** Its wording is a property whose default is "Sorry, we are not taking any contributions at the moment." (§UI20.6.6 rule 1). *(user, 2026-09-27; §UI20.6.6 rule 1)* ≠ item 1

**2.4 [Won't]** It offers no action: no button, no link and no hook. *(user, 2026-09-27)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** The wording property renders as the panel's text; with none passed, the default of rule 2.3 renders. *(user, 2026-09-27)* ≠ item 1

### 3.2 Driven by roles

**3.2.1** None of its own. Whether the face shows at all is `ContentItemPanel`'s decision, made from the reader's read-only roles and the settings it was handed (`UI/Components/ContentItemPanel.md rule 2.43`). *(user, 2026-09-27)*

### 3.3 Combinations

**3.3.1** None. With no role gate of its own and one property, nothing combines. *(user, 2026-09-27)*

### 3.4 Role matrix

There is no item on this face, so there is no owner: the Owner column is n/a. The face renders
what it is handed identically for every persona; which persona is shown it is
`UI/Components/ContentItemPanel.md §3.4`, row 13.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Shown by `ContentItemPanel` — **the wording** ≠ item 1 | ❌ No¹ | ✅ Yes | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes |

¹ A signed-out reader gets the add face's login link instead (`UI/Components/ContentItemPanel.md rule 2.43`).

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| The wording — its name is fixed by the task that builds the face | `string` | `'Sorry, we are not taking any contributions at the moment.'` | The panel's text (rule 2.3). | — |

### 4.2 Events

None. The face offers no action (rule 2.4).

### 4.3 Pass-through properties

Per §UI20.6.5. The face renders no child component, so it passes nothing through.
`ContentItemPanel` drives its wording property, unchanged, so a page can set it from the top
(`UI/Components/ContentItemPanel.md §4.3`). Neither exists yet (section 10, item 1).

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The panel** (a view) ≠ item 1 | A signed-in reader with no content type left to contribute (rule 2.1) | None — it is what a reader left with no type is shown | ✅ Shown in the add face's place (rule 2.1) | Not offered: the add face's login link instead (`UI/Components/ContentItemPanel.md rule 2.43`) | Nothing — it offers no action and sends no request (rule 2.4) |

None of its own beyond the matrix: the face writes nothing. The read-only roles that can leave a
reader with no type are composed by the add face's gates
(`UI/Components/ContentItemPanel.Add.md §5`), and the contribution itself is decided by the
server where it is made (§SEC14.7 posture A rule 1).

## 6. Composition and Usage

`ContentItemPanel` renders it in the add face's place
(`UI/Components/ContentItemPanel.md rule 2.43`), so `/posts/contribute` reaches it through the
panel. No page renders it directly.

The same ruling of the user's (2026-09-27) governs the invitation that leads a reader to
`/posts/contribute`: `SharingPanel` shows to a signed-out reader, is hidden from a holder of
`ReadOnly` or `ContentItem-ReadOnly`, and shows to a reader whose read-only roles are per content
type alone (`UI/Components/SharingPanel.md rules 2.9–2.11`). This face is the ruling's other
half, what such a reader meets at the form when no content type is left to them.

## 7. Dependencies

None beyond its property.

## 8. States, Validation and Feedback

None. The face has no loading, saving, submitting, empty, error, validation or confirmation state.

## 9. Styling and Accessibility

None ruled beyond rule 2.2: a very basic panel. Its wording is a property (rule 2.3).

## 10. Open Questions and Gaps

1. (needs issue) **The component does not exist.** The user ruled on 2026-09-27 that
   `ContentItemRestrictedPanel` must be added: a very basic panel whose wording is a property
   defaulting to "Sorry, we are not taking any contributions at the moment." (rules 2.1–2.3),
   shown by `ContentItemPanel` instead of the add face when the reader has no content type left
   to contribute (`UI/Components/ContentItemPanel.md rule 2.43`), with its wording property
   forwarded unchanged from `ContentItemPanel` (§UI20.6.5). None of it is built. Evidence: there
   is no such component under `src/components/contentItems/` at 70dc72e7, and `contentItemPanel.tsx`
   renders `ContentItemAddPanel` whenever it has no item (`ContentItemPanel` — the
   `contentItem == null` branch). The add face's two refusals it replaces are
   `UI/Components/ContentItemPanel.Add.md §10 item 10`.
