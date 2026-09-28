# 1. SharingPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** none
- **Composes:** none
- **Used by:** `/` — `src/pages/home.tsx`; `/posts/:contentItemId` — `src/pages/postDetail.tsx`; `/myposts/:contentItemId` — `src/pages/myPostDetail.tsx`. No component document renders it.
- **Inherits:** §UI20.3 principle 2, §SEC14.6, §SEC18.6, §UI20.6.4, §UI20.6.5, §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/sharingPanel.tsx`; its styles in `contentItems.css` (the *SharingPanel* block); the test `sharingPanel.test.tsx`
- **Sample page:** `/SamplePages/Components/Sharing-Panel` — `src/pages/samplePages/components/sharingPanelDoc.tsx`

`SharingPanel` is the invitation to contribute: an icon, a title, a description and a button. It asks the reader to share something of their own — "Have something to share?" — and raises `onSubmit` when they accept. The page decides where that leads; every page that renders it today goes to `/posts/contribute`.

It adapts to its **container**, not the viewport. The same panel is a one-row banner in a wide slot and a stacked card in a sidebar column, and only its own width decides which.

**It shares nothing outward.** `SharingPanel` knows no content item; it brings a contribution **in**. A reader shares a content item outward through the post card's Share action (`UI/Components/ContentItemPanel.md §5`). The placeholder `href="#"` share buttons in code today are a different component, `ShareLinks` (`src/components/coreUI/shareLinks.tsx`). An overlap with another contribution prompt, sample material only, is ruled in section 10 (item 2), as is the name (item 1).

## 2. Business Rules

**2.1 [Must]** The panel is a pure presentation component. It raises `onSubmit`, and the page decides where that leads: no router, no fetching, no mutation. *(§UI20.3 principle 2; code: `sharingPanel.tsx` — header comment; test: `sharingPanel.test.tsx` — the header comment)*

**2.2 [Must]** Every string and the icon are properties, and each default is the design's wording, so a page that wants the standard invitation passes only `onSubmit` and may override any of it (§UI20.6.6 rule 1). The defaults: the icon `bi bi-pencil-square`, the title "Have something to share?", the description "A quote, a story, a testimony, or a verse that carried you through — if it might encourage someone else, we would love to read it.", and the button "Submit a contribution". This document is the design those defaults were written to: the component was built before any design document, and the user ruled on 2026-09-27 that the document written for it states today's texts as its defaults. *(user, 2026-09-27; §UI20.6.6 rule 1; test: "should render its defaults as the design wrote them"; test: "should render whatever text it is handed instead"; test: "should default the icon to the pencil")*

**2.3 [Could]** The panel adapts to its own width through a CSS container query, with no property and no script. At 40rem wide or more, the description and the button share one row with the button on the right — the banner face. Narrower, they stack, left-aligned — the sidebar face. *(code: `contentItems.css` — `.g2h-sharing-panel`, `@container (min-width: 40rem)`; code: `sharingPanel.tsx` — header comment)*

**2.4 [Could]** The icon sits inline in the heading, so a narrow title wraps to beneath it. *(test: "should draw the icon with the css it was given, inside the heading")*

**2.5 [Could]** The button's text never wraps. A long label widens the button instead. *(test: "should never let the button text wrap"; code: `contentItems.css` — the comment on the wide face)*

**2.6 [Could]** The panel keeps the container class and the body class the adaptation keys off; losing either freezes it in one face. *(test: "should carry the container and body classes the adaptation keys off")*

**2.7 [Must]** Pressing the button raises `onSubmit` once. *(test: "should raise onSubmit when the button is pressed")*

**2.8 [Won't]** The panel does not navigate. The button raises an event and the page decides where it leads. *(code: `sharingPanel.tsx` — header comment; test: `sharingPanel.test.tsx` — the header comment, "No router")*

**2.9 [Must]** Signed out, the panel shows as it is. A signed-out reader's press raises `onSubmit` like anyone's, and the page sends them to sign in and, afterwards, on to the contribution form, `/posts/contribute`, the place their press was heading for (§UI20.6.6 rule 2). *(user, 2026-09-27; code: `sharingPanel.tsx` — `onSubmit?.()`)*

**2.10 [Must]** Signed in with `ReadOnly` or `ContentItem-ReadOnly`, the reader is not shown the panel: it is hidden. The panel composes those two roles itself, from what it represents — an invitation to contribute a content item (§UI20.6.6 rule 3); no page supplies them. *(user, 2026-09-27)* ≠ item 5

**2.11 [Must]** A signed-in reader whose only read-only roles are per content type, `ContentItem-{ContentType}-ReadOnly`, is shown the panel. The contribution page their press leads to offers only the types left to them (`UI/Components/ContentItemPanel.Add.md rule 2.6`), or the restricted face where none is left (`UI/Components/ContentItemPanel.md rule 2.43`). *(user, 2026-09-27)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `title` is the heading, and also the accessible name of the panel's section. *(code: `sharingPanel.tsx` — `aria-label={title}`)*

**3.1.2** `description` is the body paragraph, beside the button when the panel is wide and above it when it is narrow. *(code: `sharingPanel.tsx`; code: `contentItems.css` — `.g2h-sharing-panel-body`)*

**3.1.3** `buttonText` is the button's label, followed by an arrow icon. *(code: `sharingPanel.tsx`)*

**3.1.4** `iconCss` is the icon's class list, drawn in the primary colour inside the heading. Any icon-font class the page's stylesheets carry works. *(test: "should draw the icon with the css it was given, inside the heading"; code: `sharingPanel.tsx` — `iconCss` comment)*

**3.1.5** With no `onSubmit`, the button still renders and pressing it does nothing. That is a dead action, which §UI20.6.6 rule 4 plans away by wiring the hook; every page that renders the panel today passes `onSubmit` (section 4.3). *(code: `sharingPanel.tsx` — `onSubmit?.()`; user, 2026-09-27)*

**3.1.6** `cssClass` defaults to `mb-4` and is appended to the panel's own classes. Passing any value replaces that default spacing. *(code: `sharingPanel.tsx` — `cssClass = 'mb-4'`)*

### 3.2 Driven by roles

**3.2.1** A signed-in reader holding `ReadOnly` or `ContentItem-ReadOnly` is not shown the panel (rule 2.10). Every other reader is, signed out included, and it renders the same for each of them (rules 2.9 and 2.11). *(user, 2026-09-27)* ≠ item 5

### 3.3 Combinations

**3.3.1** Whether the panel renders at all is first the page's decision (section 6). Where the page renders it, the role gate of rule 3.2.1 then withholds it from a holder of `ReadOnly` or `ContentItem-ReadOnly`, whatever properties the page passes. *(user, 2026-09-27)* ≠ item 5

### 3.4 Role matrix

The panel's one role gate is the read-only roles of rule 2.10. "Owner" would be the owner of the content item on the page; the panel knows no item, so ownership changes nothing.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Any properties, no `ReadOnly` or `ContentItem-ReadOnly` held — the heading, the description and the button | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Viewer also holds `ReadOnly` or `ContentItem-ReadOnly` — **the panel** ≠ item 5 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Viewer's only read-only roles are per content type — **the panel** | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `onSubmit` given — pressing the button raises it | ✅ Yes¹ | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Inside the component. `/myposts/:contentItemId` sits behind `SecuredRoute`, so an anonymous reader never reaches that instance; `/` and `/posts/:contentItemId` are public.

## 4. Properties and Events

### 4.1 Properties

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `iconCss` | `string` | `'bi bi-pencil-square'` | The icon's classes, inline in the heading. | none — no children |
| `title` | `string` | `'Have something to share?'` | The heading, and the section's accessible name. | none — no children |
| `description` | `string` | the invitation in rule 2.2 | The body copy. | none — no children |
| `buttonText` | `string` | `'Submit a contribution'` | The button's label; never wraps. | none — no children |
| `cssClass` | `string` | `'mb-4'` | Appended to the panel's classes, for spacing. | none — no children |

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `onSubmit` | none | The button is pressed. The consumer navigates. |

### 4.3 Pass-through properties

Per §UI20.6.5. `SharingPanel` renders no child component, so it passes nothing through.

The pages drive it directly. All three pass `onSubmit` only — each navigates to `/posts/contribute` with the page's own path and query in router state as `from` — and none sets the icon, a string or `cssClass`. *(code: `home.tsx`, `postDetail.tsx`, `myPostDetail.tsx`)*

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The panel and its button**, raising `onSubmit` ≠ item 5 | Every reader the page renders the panel for, save a holder of `ReadOnly` or `ContentItem-ReadOnly` (rule 3.2.1) | `ReadOnly`, `ContentItem-ReadOnly` (rule 2.10); not a per-type `ContentItem-{ContentType}-ReadOnly` (rule 2.11) | ❌ Refused under `ReadOnly` or `ContentItem-ReadOnly`: the panel is hidden (rule 2.10); ✅ Allowed under per-type roles alone (rule 2.11) | ✅ Offered; raises `onSubmit`, and the page sends them to sign in and then on to the contribution form (rule 2.9, §UI20.6.6 rule 2) | Nothing at the panel. The contribution it invites is decided where it is made (§SEC14.7 posture A rule 1) |

The panel's one gate is rule 2.10's, and it decides rendering only (§SEC14.6). Identity comes from the auth context, since the panel composes its read-only roles itself and no page supplies them (§UI20.6.6 rule 3); the panel reads none today (section 10, item 5). The contribution it invites is gated where it is made: the service applies the contribution gate of §SEC14.7 posture A rule 1.

## 6. Composition and Usage

| Route | Page | Placement |
| --- | --- | --- |
| `/` | `src/pages/home.tsx` | Full width above the feed, so it wears the banner face. |
| `/posts/:contentItemId` | `src/pages/postDetail.tsx` | The right-hand column, beneath `TagAssociationPanel` and `BibleReferenceAssociationPanel` — the sidebar face. |
| `/myposts/:contentItemId` | `src/pages/myPostDetail.tsx` | The same place as on `/posts/:contentItemId`. Behind `SecuredRoute`. |
| `/SamplePages/Components/Sharing-Panel` | `src/pages/samplePages/components/sharingPanelDoc.tsx` | Wide and narrow live demos, and a playground whose boards set every string and the icon and narrow the container. Admin-only. |

*(code: the three pages — the comments above each `SharingPanel`)*

## 7. Dependencies

**Data the consumer supplies:** none. Every property is optional.

**API endpoints:** none. The consumer calls none on the panel's behalf either — it navigates.

**Indirect dependencies:** the auth context, for the signed-in reader's roles (rule 2.10). The panel reads none today (section 10, item 5).

## 8. States, Validation and Feedback

None. The panel has no loading, empty, error, validation or confirmation state, and holds no data that could go stale. With no loading state, it has nothing to announce (§UI20.6.6 rule 5).

## 9. Styling and Accessibility

**9.1** The panel is a `section` with `g2h-sharing-panel border rounded-3 bg-body p-3 p-lg-4`, then `cssClass`. *(code: `sharingPanel.tsx`)*

**9.2** `.g2h-sharing-panel` sets `container-type: inline-size`, which is what lets `.g2h-sharing-panel-body` ask its width. The body is a column, left-aligned, with a 0.75rem gap; at `@container (min-width: 40rem)` it becomes one row, centred vertically, spaced between, with a 1.5rem gap. *(code: `contentItems.css`)*

**9.3** The button is `btn btn-primary-soft fw-bold text-nowrap mb-0`, `type="button"`. *(code: `sharingPanel.tsx`)*

**9.4** The section is named by `title` through `aria-label`, which makes it a named region. The heading is an `h3` styled as `h4`. Both icons are `aria-hidden`. *(code: `sharingPanel.tsx`)*

**9.5** jsdom cannot run a container query, so the test pins the structure the query depends on, not the two faces themselves. *(test: `sharingPanel.test.tsx` — the header comment)*

## 10. Open Questions and Gaps

1. **Note — the name, ruled.** This item asked whether `SharingPanel` and a planned `ShareBar` — share buttons composing short-link URLs (§DOM19.7), listed in §UI20.6's catalogue and never built — should be told apart, since both names say "share". The user ruled on 2026-09-27 that `SharingPanel` keeps its name, and that the planned `ShareBar` is removed from the catalogue; the short-link design of §DOM19.7 stays, and the post card's Share action is how a reader shares a content item. Section 1 no longer names `ShareBar`.
2. **Note — an overlap with `ContributionPrompt`, ruled.** This item asked whether `SharingPanel` and `src/components/coreUI/contributionPrompt.tsx` should converge: both carry the heading "Have something to share?", the same button text, and `/posts/contribute` as their destination, though `ContributionPrompt` links rather than raising an event, shows a login link to a signed-out reader, and has no container query. The user's rulings of 2026-09-27 settle it. `SharingPanel` keeps its name and is the product's invitation to contribute. `ContributionPrompt` is sample material: it is rendered only on `/Post-Single` (`src/pages/postSingle.tsx`), a mocked page rather than a product page, and on the sample page `/SamplePages/Post/Post-Single-Magazine` (`src/pages/samplePages/post/postSingleMagazineSample.tsx`); the ported template's blog, `/Post-Single` with it, is sample material moving under `/SamplePages` (§UI20.5.1). No convergence is designed.
3. **Note — where the default wording is recorded, ruled.** The test says the defaults are rendered "as the design wrote them", and no design document stated them. The user ruled on 2026-09-27 that this document is the component's design and states today's texts as its defaults, each a property a consumer may override. Rule 2.2 states them.
4. **Note — a reader holding a read-only role, ruled.** This item asked whether the panel, which
   invites every reader to contribute and reads no identity (`sharingPanel.tsx`), should withhold
   the invitation from a holder of `ReadOnly` or `ContentItem-ReadOnly`, who cannot contribute, or
   keep inviting every reader and leave the refusal to the contribution page. The user ruled on
   2026-09-27: signed out, the panel shows as it is, and the page sends the reader to sign in and
   back to the same place; signed in with `ReadOnly` or `ContentItem-ReadOnly`, the panel is
   hidden; with only a per-type `ContentItem-{ContentType}-ReadOnly`, it shows, and the
   contribution page offers only the types left to the reader, or the restricted face where none
   is. The user refined the signed-out half the same day: the page sends the reader to sign in
   and then on to the contribution form they were heading for. Rules 2.9–2.11 say so. The code's
   half is item 5; where the pages send the reader is theirs.
5. (needs issue) **Gap — the panel does not hide itself from a read-only role.** Rules 2.10, 3.2.1
   and 3.3.1 (user ruling 2026-09-27) withhold the panel from a signed-in holder of `ReadOnly` or
   `ContentItem-ReadOnly`, the panel composing those roles itself. The panel reads no identity:
   it renders the same section for every reader, with no auth context and no role check
   (`sharingPanel.tsx` — `SharingPanel`, lines 27-59 at 70dc72e7), so a holder of either role is
   invited to a contribution they cannot make.
