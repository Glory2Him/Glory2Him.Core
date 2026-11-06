# 1. ContentItemQuotesPanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), for every item whose content type is `Quote`. Every page in `UI/Components/ContentItemPanel.md` **Used by** reaches it through the panel.
- **Inherits:** `UI/Components/ContentItemPanel.md §3 and rules 2.1 and 2.3`; `UI/Components/ContentItemPanel.Default.md` (every rule, through the derivation of rule 2.2 below); §UI20.6.4; §UI20.6.5; §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemQuotesPanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Quotes-Panel` — `src/pages/samplePages/components/contentItemQuotesPanelDoc.tsx` (it drives the dispatcher, through `shared/contentItemPanelPlayground.tsx`)
- **Relocated from:** §UI20.6.2 (part)

The view template for a quote. A quote is short enough to show whole, and showing it whole is what
makes engaging with it from a list fair, so the quote itself stands large as the card's heading,
with its author after an em-dash. Everything else on the card is the default template's.

## 2. Business Rules

**2.1 [Must]** `ContentItemQuotesPanel` is the view template registered for `ContentType.Quote` (`UI/Components/ContentItemPanel.md rule 2.3`). *(code: contentItemPanel.tsx — templateOverrides)*

**2.2 [Must]** It derives from `ContentItemDefaultPanel` via `contentSlot`. *(§UI20.6.2)*

**2.3 [Must]** It replaces the content slot and nothing else, as `UI/Components/ContentItemPanel.Default.md rule 2.3` requires of an override. *(test: contentItemPanel.test.tsx — "should carry the default meta row under the quotes override")*

**2.4 [Must]** The quote is shown whole. *(test: contentItemPanel.test.tsx — "should render a quote through the quotes override, whole")*

**2.5 [Must]** Where the element carries an author and the effective setting does not withhold it (`UI/Components/ContentItemPanel.Default.md rule 2.4`), it follows the quote inline after an em-dash (*— William Temple*). *(test: contentItemPanel.test.tsx — "should render a quote through the quotes override, whole")* ≠ item 1

**2.6 [Could]** A quote carries no title, so the quote itself is the way into the detail surface — the destination `onTitleClick` names on every other card, under the same switch: where the surface does not allow the title click, the quote stands as plain text. *(test: contentItemPanel.test.tsx — "should raise onTitleClick from the quote itself where the surface allows it", "should stand the quote as plain text by default even with onTitleClick wired")*

**2.7 [Could]** The quote has two faces, decided by the imagery the consumer supplied: vertically centred over a dark hero where the element has an image, and on a quiet light block where it has none. *(code: contentItemQuotesPanel.tsx — quoteBlock)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** The chip row stands above the quote block (`UI/Components/ContentItemPanel.Default.md rule 3.1.11`). *(code: contentItemDefaultPanel.tsx — the `contentSlot != null` branch)*

**3.1.2** `truncateAt`, `isContentExpanded` and `allowInPlaceExpansion` do not reach the quote: it is never cut and carries no read-more affordance (rule 2.4). *(code: contentItemQuotesPanel.tsx — quoteWords)*

**3.1.3** The quote is a button raising `onTitleClick` only where `allowTitleClick` is on and `onTitleClick` is wired (rule 2.6). *(code: contentItemQuotesPanel.tsx — quoteText)*

**3.1.4** With `imageUrl` present the quote renders in white over the image as a cover background; without it, on a light block. No quote image is supplied today — the projection gives `Quote` no placeholder — so the light block is what ships. *(code: contentItemQuotesPanel.tsx — the component's header comment and quoteBlock)*

**3.1.5** The quote face shows no thumbnail and no title, whatever the element carries. A quote carries no title: its `ContentItemSetting` always has `HasTitle = false` — the seed sets it, and the server refuses `HasTitle = true` for a quote, on the type default and on an item override alike (§ARC12.5.2 business rule 11; not yet built) — and the template provisions none. *(code: contentItemQuotesPanel.tsx — quoteBlock; user, 2026-09-26; user, 2026-09-27)*

**3.1.6** The template renders no fixed string of its own: the quote and its author come from the item, and the card's other strings are the default template's (`UI/Components/ContentItemPanel.Default.md rule 3.1.12`; §UI20.6.6 rule 1). *(code: contentItemQuotesPanel.tsx — quoteBlock)*

### 3.2 Driven by roles

**3.2.1** None of its own; inherits `UI/Components/ContentItemPanel.Default.md §3.2`.

### 3.3 Combinations

**3.3.1** Inherits `UI/Components/ContentItemPanel.Default.md §3.3`; the content slot adds no condition.

### 3.4 Role matrix

The template has no role gate; the matrix is `UI/Components/ContentItemPanel.Default.md §3.4`, unchanged, and the quote block renders identically for every persona.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| An item of type `Quote` — **the quote block** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `allowTitleClick=true`, `onTitleClick` wired — **the quote as a way in** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

## 4. Properties and Events

### 4.1 Properties

`ContentItemTemplateProps`, exactly as `UI/Components/ContentItemPanel.Default.md §4.1`
lists them, without `contentSlot`, which this template sets. Of them it reads `contentItem`
(`content`, `author`, `imageUrl`), `allowTitleClick` and `onTitleClick` itself, and hands every
one to the default template.

### 4.2 Events

Inherits `UI/Components/ContentItemPanel.Default.md §4.2`; `onTitleClick` is raised from the quote rather than a title.

### 4.3 Pass-through properties

Per §UI20.6.5. Every property is driven by `ContentItemPanel` and handed to
`ContentItemDefaultPanel` unchanged; nothing is out of the parent's reach.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The quote as a way in (`onTitleClick`) | Every persona, where `allowTitleClick` is on and the hook is wired (rule 3.1.3) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onTitleClick` | Nothing of its own: the read it leads to answers under §SEC14.7 posture A rule 4 |
| Every other control on the card | As `UI/Components/ContentItemPanel.md §5` | As there | As there | As there | As there |

None of its own (`UI/Components/ContentItemPanel.md §5`).

## 6. Composition and Usage

`ContentItemPanel` renders it for a `Quote` item; it renders `ContentItemDefaultPanel` with its
own `contentSlot`. No page renders it directly.

## 7. Dependencies

None beyond its properties.

## 8. States, Validation and Feedback

Inherits `UI/Components/ContentItemPanel.Default.md §8`; nothing to add.

## 9. Styling and Accessibility

- The hero is `g2h-content-item-quote-hero` (a 14rem minimum height on a dark ground, so a long
  quote grows the block) with the theme's `card-overlay-bottom`; the text is
  `g2h-content-item-quote-text`, lifted above the overlay. The light face is `bg-light`.
- The quote is an `h3`; as a button it keeps the heading's typography and wraps
  (`UI/Components/ContentItemPanel.Default.md §9`).

## 10. Open Questions and Gaps

1. (needs issue) **`HasAuthor` is not asked of the quote's author.**
   `UI/Components/ContentItemPanel.Default.md rule 2.4` has the view templates obey `HasAuthor`. The quote appends the author whenever the
   element carries one (`contentItemQuotesPanel.tsx` — quoteWords); only the meta row's *Author*
   segment asks the setting.
2. **Note — no title on a quote, ruled.** The quote face never shows a title, whatever the
   element carries (rule 3.1.5), and does not ask `HasTitle`. The user ruled on 2026-09-26 that
   a titled quote is not a case the design needs: a quote's `ContentItemSetting` always has
   `HasTitle = false` — seeded so in `Websites/Glory2Him.WebApp/Data/ContentItemSettingSeedData.cs`
   (`ContentType.Quote`, line 89 at 70dc72e7), which is what holds it today, the server's
   refusal of a title being ruled and not yet built (§ARC12.5.2 business rule 11;
   `UI/Components/ContentItemPanel.md §10 item 18`) — and its template provisions no title. Rule 3.1.5
   now says so, and it agrees with `UI/Components/ContentItemPanel.Default.md rule 2.4`.
3. (needs issue) **The doc page needs updating.** `contentItemQuotesPanelDoc.tsx` says the author stands
   *beneath* the words (its summary and its derivation sample); the component appends it inline
   after an em-dash (rule 2.5).
