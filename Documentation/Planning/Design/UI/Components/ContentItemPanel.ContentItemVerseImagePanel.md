# 1. ContentItemVerseImagePanel

- **Kind:** User story — child component of ContentItemPanel
- **Parent:** [ContentItemPanel.md](ContentItemPanel.md)
- **Children:** none
- **Composes:** none
- **Used by:** [ContentItemPanel.md](ContentItemPanel.md), for every item whose content type is `VerseImage`. Every page in `UI/Components/ContentItemPanel.md` **Used by** reaches it through the panel.
- **Inherits:** `UI/Components/ContentItemPanel.md §3 and rules 2.1 and 2.3`; `UI/Components/ContentItemPanel.Default.md` (every rule, through the derivation of rule 2.2 below); §UI20.6.4; §UI20.6.5; §UI20.6.6
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/contentItems/contentItemVerseImagePanel.tsx`
- **Sample page:** `/SamplePages/Components/Content-Item-Verse-Image-Panel` — `src/pages/samplePages/components/contentItemVerseImagePanelDoc.tsx` (it drives the dispatcher, through `shared/contentItemPanelPlayground.tsx`)

The view template for a verse image: a view-template override of `ContentItemPanel`, registered
for `ContentType.VerseImage`. A verse card is its verse. The content arrives whole — with its own
quotation marks, reference and translation — so the card renders it exactly as written, standing
large, and appends nothing. Everything else on the card is the default template's.

## 2. Business Rules

**2.1 [Must]** `ContentItemVerseImagePanel` is the view template registered for `ContentType.VerseImage` (`UI/Components/ContentItemPanel.md rule 2.3`). *(code: contentItemPanel.tsx — templateOverrides)*

**2.2 [Must]** It derives from `ContentItemDefaultPanel` by replacing only the content slot, as `UI/Components/ContentItemPanel.Default.md rule 2.3` requires of an override, so its author shows in the default meta row. *(code: contentItemVerseImagePanel.tsx — the component's header comment and its render)*

**2.3 [Must]** The verse renders exactly as written, whole, and nothing is appended to it — unlike the quote, which adds its author after an em-dash, because a verse's content already ends in its reference. *(test: contentItemPanel.test.tsx — "should render a verse image through the verses override, appending nothing")*

**2.4 [Could]** A verse image carries no title, so the verse itself is the way into the detail surface, under the same switch as a title: where the surface does not allow the title click, the verse stands as plain text. *(code: contentItemVerseImagePanel.tsx — verseText)*

**2.5 [Could]** The verse has two faces, decided by the imagery the consumer supplied: vertically centred over a dark hero where the element has an image, and on a quiet light block where it has none. *(code: contentItemVerseImagePanel.tsx — verseBlock)*

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** The chip row stands above the verse block (`UI/Components/ContentItemPanel.Default.md rule 3.1.11`), and the chip names the type from its setting. *(test: contentItemPanel.test.tsx — "should render a verse image through the verses override, appending nothing")*

**3.1.2** `truncateAt`, `isContentExpanded` and `allowInPlaceExpansion` do not reach the verse: it is never cut and carries no read-more affordance. *(code: contentItemVerseImagePanel.tsx — verseText)*

**3.1.3** The verse is a button raising `onTitleClick` only where `allowTitleClick` is on and `onTitleClick` is wired. *(code: contentItemVerseImagePanel.tsx — verseText)*

**3.1.4** With `imageUrl` present the verse renders in white over the image as a cover background; without it, on a light block, which is what ships until real header images land. *(code: contentItemVerseImagePanel.tsx — the component's header comment and verseBlock)*

**3.1.5** The verse face shows no thumbnail and no title, whatever the element carries. A verse image carries no title: its `ContentItemSetting` always has `HasTitle = false` — the seed sets it, and the server refuses `HasTitle = true` for a verse image, on the type default and on an item override alike (§ARC12.5.2 business rule 11; not yet built) — and the template provisions none. *(code: contentItemVerseImagePanel.tsx — verseBlock; user, 2026-09-26; user, 2026-09-27)*

**3.1.6** The template renders no fixed string of its own: the verse comes from the item, and the card's other strings are the default template's (`UI/Components/ContentItemPanel.Default.md rule 3.1.12`; §UI20.6.6 rule 1). *(code: contentItemVerseImagePanel.tsx — verseBlock)*

### 3.2 Driven by roles

**3.2.1** None of its own; inherits `UI/Components/ContentItemPanel.Default.md §3.2`.

### 3.3 Combinations

**3.3.1** Inherits `UI/Components/ContentItemPanel.Default.md §3.3`; the content slot adds no condition.

### 3.4 Role matrix

The template has no role gate; the matrix is `UI/Components/ContentItemPanel.Default.md §3.4`, unchanged, and the verse block renders identically for every persona.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| An item of type `VerseImage` — **the verse block** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `allowTitleClick=true`, `onTitleClick` wired — **the verse as a way in** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

## 4. Properties and Events

### 4.1 Properties

`ContentItemTemplateProps`, exactly as `UI/Components/ContentItemPanel.Default.md §4.1`
lists them, without `contentSlot`, which this template sets. Of them it reads `contentItem`
(`content`, `imageUrl`), `allowTitleClick` and `onTitleClick` itself, and hands every one to the
default template.

### 4.2 Events

Inherits `UI/Components/ContentItemPanel.Default.md §4.2`; `onTitleClick` is raised from the verse rather than a title.

### 4.3 Pass-through properties

Per §UI20.6.5. Every property is driven by `ContentItemPanel` and handed to
`ContentItemDefaultPanel` unchanged; nothing is out of the parent's reach.

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| The verse as a way in (`onTitleClick`) | Every persona, where `allowTitleClick` is on and the hook is wired (rule 3.1.3) | None — a read (§SEC18.6) | ✅ Allowed | Offered; raises `onTitleClick` | Nothing of its own: the read it leads to answers under §SEC14.7 posture A rule 4 |
| Every other control on the card | As `UI/Components/ContentItemPanel.md §5` | As there | As there | As there | As there |

None of its own (`UI/Components/ContentItemPanel.md §5`).

## 6. Composition and Usage

`ContentItemPanel` renders it for a `VerseImage` item; it renders `ContentItemDefaultPanel` with
its own `contentSlot`. No page renders it directly.

## 7. Dependencies

None beyond its properties.

## 8. States, Validation and Feedback

Inherits `UI/Components/ContentItemPanel.Default.md §8`; nothing to add.

## 9. Styling and Accessibility

- It shares the quote's hero classes, `g2h-content-item-quote-hero` and
  `g2h-content-item-quote-text`
  (`UI/Components/ContentItemPanel.ContentItemQuotesPanel.md §9`); the light face is `bg-light`.
- The `VerseImage` chip is a dark violet (`#5925b8`), purple by ruling and split from the
  lighter `Quote` purple by depth; the palette test measures the pair. *(code: contentItems.css
  — the `VerseImage` palette block)*
- The verse is an `h3`; as a button it keeps the heading's typography and wraps, as a verse
  must rather than running off the card.

## 10. Open Questions and Gaps

1. **§UI20.6.2 does not name this override.** It names `ContentItemQuotesPanel` as its example
   of an override; this document is derived from the code and its tests alone.
2. **Note — no title on a verse image, ruled.** The verse face never shows a title, whatever the
   element carries (rule 3.1.5), and does not ask `HasTitle`, which
   `UI/Components/ContentItemPanel.Default.md rule 2.4` has the view templates obey. The user
   ruled on 2026-09-26 that a titled verse image is not a case the design needs: a verse image's
   `ContentItemSetting` always has `HasTitle = false` — seeded so in
   `Websites/Glory2Him.WebApp/Data/ContentItemSettingSeedData.cs` (`ContentType.VerseImage`,
   line 231 at 70dc72e7), which is what holds it today, the server's refusal of a title being
   ruled and not yet built (§ARC12.5.2 business rule 11;
   `UI/Components/ContentItemPanel.md §10 item 18`) — and its template provisions no title. Rule 3.1.5 now says so, and the
   two rules agree.
