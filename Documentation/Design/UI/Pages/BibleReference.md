# 1. BibleReference

One Bible passage, read: a verse or a verse range, its reactions beneath it, and beside it the
tags and the related Bible references suggested for it. Everyone reads it, signed in or not. It is
where a Bible reference leads from the rest of the site — a reference pill on a card, a reference
chip in a side panel, the verse of the day on `/`.

**This page has not been designed yet: issue #700**, *Design The Bible Reference Page*, from a
mockup (user rulings 2026-09-27). This document records the page **as built** at 70dc72e7, and of
its design keeps only the rules that bind every page: the setting switches of §DOM6.9, the
read-only blocks, the page supplying every link, and sign-in. It also records the two rules the
product owner has ruled for this page itself — the search for the passage's items (rule 2.17)
and its message for a reference it cannot read (rule 2.18) — which #700 places. Every other rule
and gap specific to this page is held for #700 — what it shows for a passage, who may suggest there, what a reaction to a
passage records, where any hook specific to a passage leads, and telling the tag panel that its
host is a passage — so no task is carved before the mockup (item 5).

- **Route:** `/BibleReferences` (`src/routes/staticRoutes.tsx` line 17 at 70dc72e7), which shows
  the default passage; and `/BibleReferences/{reference}` (line 22), which renders this page
  through `BibleReferenceView` (`src/pages/bibleReferenceView.tsx`) when the reference names a
  verse or a verse range. There a chapter-only reference opens the Bible reader instead, and an
  unreadable one Not Found. `/BibleReferences/BibleReader` and its old address
  `/BibleReferences/Full-Chapter` are the reader's, not this page's.
- **Source:** `src/pages/bibleReference.tsx`; `src/pages/bibleReferenceView.tsx` for the deep-link
  route. No test renders the page.
- **Section:** user
- **Access:** no guard — every reader, signed in or not
- **Layout:** two columns — main with right sidebar
- **Components:** [AssociationPanel.TagAssociationPanel.md](../Components/AssociationPanel.TagAssociationPanel.md)
  and [AssociationPanel.BibleReferenceAssociationPanel.md](../Components/AssociationPanel.BibleReferenceAssociationPanel.md),
  each rendering [AssociationPanel.md](../Components/AssociationPanel.md). Undocumented building
  block: `ReactionBar` (`src/components/coreUI/reactionBar.tsx`). The passage is YouVersion's
  `BibleCard` (`@youversion/platform-react-ui`), a third-party component, and
  `YouVersionUnavailableMessage` (`src/components/youVersion/youVersionAppProvider.tsx`) stands in
  its place when YouVersion is not configured. The *Show Full Chapter* link is the page's own.

Paths in this document are under `Websites/Glory2Him.WebApp.React/` unless they start with
`Documentation/`. Line numbers are at 70dc72e7.

## 2. Business Rules

**2.1 [Must]** `/BibleReferences` is public: neither of its routes has a guard, so every reader reaches the page, signed in or not. *(code: staticRoutes.tsx — the `BibleReferences` and `BibleReferences/:reference` routes)*

**2.2 [Must]** The address names the passage and its version, as bible.com addresses them: `/BibleReferences/JHN.3.16.NIV` is John 3:16 in the NIV, and `JHN.3.16-17` or bible.com's dotted `JHN.3.16.17` a range. `/BibleReferences` alone shows John 14:6 in the NIV. A chapter-only address opens the Bible reader; one that cannot be read is rule 2.18's. *(code: bibleReferenceView.tsx — header comment, `BibleReferenceView`; code: bibleReference.tsx — header comment, the parameter defaults; user, 2026-09-28)* ≠ item 7

**2.3 [Must]** The passage's text is YouVersion's `BibleCard`: licensed scripture, with its own title and version picker. Choosing another translation navigates to this passage's address in that version, so a refresh keeps it and the address can be shared. *(code: bibleReference.tsx — header comment, `onVersionChange`)*

**2.4 [Should]** With no YouVersion app key configured, the page says *Bible content is unavailable — no YouVersion app key is configured.* in the passage's place, rather than failing. *(code: bibleReference.tsx — header comment, `passage`; code: youVersionAppProvider.tsx — `YouVersionUnavailableMessage`)*

**2.5 [Could]** *Show Full Chapter*, beneath the passage, leads to the passage's chapter in the Bible reader, in the same version: `/BibleReferences/JHN.14.NIV` for the default passage. *(code: bibleReference.tsx — `chapterHref`, `fullChapterLink`; code: bibleReferenceView.tsx — `chapterHref`)*

**2.6 [Won't]** What the page reads for the passage's tags and related Bible references is not designed here: it is held for #700 (item 5). As built, the panels hold sample material (item 1). *(user, 2026-09-27)*

**2.7 [Won't]** Who may suggest a tag or a related Bible reference here, and what the page writes for a suggestion and its withdrawal, are not designed here: they are held for #700 (item 5). As built, a suggestion is held on the page and nothing is written (item 1). *(user, 2026-09-27)*

**2.8 [Must]** The two panels' moderation actions stay off: nothing on the page decides anything (`UI/Components/AssociationPanel.md rule 2.15`). *(code: bibleReference.tsx — neither panel element sets `showModerationActions`)*

**2.9 [Must]** The panels follow the passage's `BibleReferenceSetting` — its own override where it has one, the system-wide default otherwise (§DOM6.9). The tag panel renders only where `ShowTags` is on, and its add box shows only where `TagsAllowed` is on; the related-references panel renders only where `ShowRelatedBibleReferences` is on, and its add box only where `RelatedBibleReferencesAllowed` is on. Each is ANDed with the page's own switch (`UI/Components/AssociationPanel.TagAssociationPanel.md rule 2.8`, `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 2.11`), and a related reference is permitted only where both passages allow it (§DOM6.10 rule 2). *(§DOM6.9; §DOM6.10)* ≠ item 2

**2.10 [Must]** The reaction bar follows the same setting: it renders where `ShowReactions` is on, accepts a reaction where `ReactionsAllowed` is on, and offers Love alone where `LimitReactionsToLoveOnly` is on (§DOM6.9 rule 7). *(§DOM6.9)* ≠ item 3

**2.11 [Must]** Choosing a reaction is the page's to act on. A signed-out reader is sent to sign in through the one reusable sign-in action, and returned to exactly this place afterwards — but not while their sign-in state is still being read. No read-only role withholds a reader's own reaction (§SEC14.7 posture A′ rule 1). What a signed-in reader's reaction to a passage records is held for #700 (item 5). *(§UI20.6.6 rule 2; user, 2026-09-27)* ≠ item 3

**2.12 [Must]** The page supplies where a tag and a related Bible reference lead, and the sign-in action the panels' login prompts raise; the panels compose no route of their own (§UI20.6.4; §UI20.6.6 rule 2). As on every page of the user section, a tag leads to the journal's search, `/posts`, handed the tag, its search bar showing it in the Tags box with the advanced section expanded, and a related reference to the page showing that passage — this page, at `/BibleReferences/{reference}` (`UI/Pages/PostDetail.md rule 2.13`). A related reference that cannot be read as a passage is rule 2.16. Any hook specific to a passage is held for #700 (item 5). *(user, 2026-09-27)* ≠ item 4

**2.13 [Could]** The tag panel's invitation speaks of a passage — *Think a tag is missing? Suggest one and help others find this passage.* — and the reference panel is headed *Related Bible References*. Every other text is the stories' own. *(code: bibleReference.tsx — the comment above `TagAssociationPanel`, the two panel elements)*

**2.14 [Could]** The reaction bar asks *How did this passage speak to you?*, and names the reaction the reader chose beneath it. *(code: bibleReference.tsx — the `ReactionBar` element, `reactedTo`)*

**2.15 [Could]** The browser tab names the passage: *John 14:6 — Glory 2 Him* for the default passage, the address's reference for any other. *(code: bibleReference.tsx — the `document.title` effect)*

**2.16 [Could]** A related reference that cannot be read as a passage leads to this page all the same, which says it could not be found and offers the search (rule 2.18), as on every page of the user section (`UI/Pages/PostDetail.md rule 2.23`). *(user, 2026-09-28)* ≠ item 4

**2.17 [Should]** A button on the page leads to the journal's search, `/posts`, with the passage in its Bible references box, the advanced section expanded, carried in the query string (`UI/Pages/Posts.md rule 2.22`), so the reader sees every item associated with the passage once a read narrows on it (`UI/Components/ContentItemListPanel.md §10 item 15`). Where the button sits and what it says are #700's (item 6). *(user, 2026-09-27)* ≠ item 6

**2.18 [Should]** A reference that cannot be read as a passage opens this page all the same. In the passage's place the page shows *"{x}" could not be found. Check and confirm this is correct or search for something else.*, `{x}` being the reference as it was written, and beneath it the search of rule 2.17. How the page is addressed for such a reference, and what surrounds the message, are #700's (item 7). *(user, 2026-09-28)* ≠ item 7

## 3. Layout

Two columns at the `lg` breakpoint and wider — the passage on the left in seven twelfths, its tags
and related references on the right in five — inside `Root`'s header and footer
(`src/components/root.tsx`). There is no shell sidebar. Narrower, the columns stack: the passage,
*Show Full Chapter* and the reaction bar first, then the two panels.

```text
+------------------------------------------------------------+
| header (Root)                                              |
+-----------------------------------+------------------------+
| main (col-lg-7)                   | sidebar (col-lg-5)     |
|   BibleCard (or the unavailable   |   TagAssociationPanel  |
|     message)                      |   ------------------   |
|   Show Full Chapter               |   BibleReference...    |
|   ReactionBar                     |                        |
+-----------------------------------+------------------------+
| footer (Root)                                              |
+------------------------------------------------------------+
```

| Region | Width | Components, in order |
| --- | --- | --- |
| Main | `col-lg-7` | `BibleCard`, or `YouVersionUnavailableMessage` in its place, and nothing while YouVersion's availability is being checked; the *Show Full Chapter* link; `ReactionBar`, and the line naming the reaction chosen |
| Right sidebar | `col-lg-5` | `TagAssociationPanel`; a rule; `BibleReferenceAssociationPanel` |

## 4. Components and their hooks

### 4.1 TagAssociationPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `associationCollection` | The default passage's tags from `src/data/sampleScripture.ts`, as approved, then this visit's suggestions; for any other passage, this visit's suggestions alone | Rule 2.6; item 1. |
| `onAdd`, `onRemove` | Append a suggestion as the reader's own; drop it again | Rule 2.7; item 1. |
| `suggestDescription` | *Think a tag is missing? Suggest one and help others find this passage.* | Rule 2.13. |
| Everything else | Left at the story's defaults: the add box on, the moderation actions off, the story's other texts, its moderation tier, its own chip link and the panel's own sign-in link | Rule 2.8; the chip link and the sign-in link are item 4; that the panel and its box do not follow `ShowTags` and `TagsAllowed` is item 2. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` ≠ item 1 | A signed-in reader commits a tag, once per tag | Held for #700: what the page writes (rule 2.7) | ❌ No — the suggestion is held on the page as the reader's own until they leave, and nothing is written (`bibleReference.tsx`, lines 143-145); item 1 |
| `onRemove` ≠ item 1 | The reader withdraws their own `Draft` or `Submitted` tag | Held for #700 (rule 2.7) | ❌ No — the suggestion is dropped from the page's list (`bibleReference.tsx`, lines 146-147); item 1 |
| `chipOnClick` or `chipHrefFor` ≠ item 4 | A tag is clicked | Opens `/posts` handed the tag, its search bar showing it in the Tags box with the advanced section expanded (rule 2.12) | ❌ No — the page supplies neither; the story's own default links the tag to `/Search?q=<tag>` (`UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 5`); item 4 |
| `loginButtonOnClick` or `loginHref` ≠ item 4 | A signed-out reader presses *Login to suggest a tag* | Sends them to sign in through the one reusable sign-in action (rule 2.12) | ❌ No — the page supplies neither; the panel composes its own sign-in route (`UI/Components/AssociationPanel.md §10 item 15`); item 4 |
| `onApprove`, `onReject` | Only with `showModerationActions` on | — | *Not wired — switched off*: a reading surface decides nothing (rule 2.8) |

### 4.2 BibleReferenceAssociationPanel

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `associationCollection` | The default passage's related references from `src/data/sampleScripture.ts`, as approved, then this visit's suggestions; for any other passage, this visit's suggestions alone | Rule 2.6; item 1. |
| `onAdd`, `onRemove` | As section 4.1 | Rule 2.7; item 1. |
| `title` | *Related Bible References* | Rule 2.13. |
| Everything else | Left at the story's defaults, as section 4.1 | That the panel and its box do not follow `ShowRelatedBibleReferences` and `RelatedBibleReferencesAllowed` is item 2. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onAdd` ≠ item 1 | A signed-in reader commits a reference, once per reference | Held for #700, as section 4.1 (rule 2.7) | ❌ No — held on the page only (`bibleReference.tsx`, lines 159-163); item 1 |
| `onRemove` ≠ item 1 | The reader withdraws their own `Draft` or `Submitted` reference | Held for #700, as section 4.1 (rule 2.7) | ❌ No — dropped from the page's list (`bibleReference.tsx`, lines 164-166); item 1 |
| `chipOnClick` or `chipHrefFor` ≠ item 4 | A related reference is clicked | Leads to the page showing that passage, `/BibleReferences/{reference}` (rule 2.12), or, for a reference it cannot read, to this page all the same, which says it could not be found (rule 2.16) | ❌ No — the page supplies neither; the story's own default builds `/BibleReferences/<USFM>`, or `/Search?q=<reference>` for a reference it cannot read (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`); item 4 |
| `loginButtonOnClick` or `loginHref` ≠ item 4 | A signed-out reader presses *Login to suggest a bible reference* | As section 4.1 (rule 2.12) | ❌ No — as section 4.1; item 4 |
| `onApprove`, `onReject` | Only with `showModerationActions` on | — | *Not wired — switched off* (rule 2.8) |

### 4.3 ReactionBar — building block

**Properties the page sets**

| Property | Value | Why |
| --- | --- | --- |
| `prompt` | *How did this passage speak to you?* | Rule 2.14. |
| `reactions` | The sample reactions of `src/pages/sampleContent.ts`, with their counts on the default passage and every count at zero on any other | Item 3. |

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onReact` ≠ item 3 | A reaction is pressed, by any reader | Sends a signed-out reader to sign in; what a signed-in reader's reaction records is held for #700 (rule 2.11) | ❌ No — the page names the choice for the visit, for every reader, signed in or not, and writes nothing (`bibleReference.tsx`, lines 84-85 and 127-131); item 3 |

### 4.4 BibleCard — third-party

**Properties the page sets:** `reference` and `versionId`, from the address (rule 2.2), and
`showVersionPicker`.

**Hooks**

| Hook | Raised when | What the page does | Built today |
| --- | --- | --- | --- |
| `onVersionChange` | The reader picks another translation | Navigates to `/BibleReferences/{reference}.{version}` (rule 2.3) | ✅ Yes (`bibleReference.tsx`, lines 89-94) |

## 5. Security and access

Every reader reaches the page (rule 2.1). What differs by persona is the association panels' boxes
and chips, and the reaction bar's sign-in. Owner means the contributor of a suggestion
(`UI/Components/AssociationPanel.md §3.4`); the passage itself has no owner.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| The page — the passage, *Show Full Chapter* and the approved chips | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The suggest boxes, where the passage's setting allows suggestions, no read-only role the panel composes — as built; who may suggest is #700's (rule 2.7) ≠ item 2 | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The persona also holds `BibleReference-ReadOnly` — **the tag panel's suggest box**, as built; the design is #700's (item 5) | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| The panels' login prompts ≠ item 4 | ✅ Yes | ❌ No¹ | ❌ No¹ | ❌ No¹ | ❌ No¹ | ❌ No¹ |
| Withdrawing one's own `Draft` or `Submitted` suggestion ≠ item 1 | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| Approve, Reject, or Remove of another reader's suggestion | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| The reaction bar, where the passage's setting shows reactions ≠ item 3 | ✅ Yes² | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |

¹ Shown to a signed-in reader, too, while their sign-in state is still being read (item 4).
² Offered; choosing raises the hook, and the page is to send them to sign in (rule 2.11).

**The read-only roles.** The page composes none. In the tag panel, `ReadOnly` or `Tag-ReadOnly`
withholds the box and the reader's own withdrawal; in the related-references panel, `ReadOnly` or
`BibleReference-ReadOnly` (`UI/Components/AssociationPanel.TagAssociationPanel.md §5`,
`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §5`). The panels do not yet
render that veto (`UI/Components/AssociationPanel.md §10 item 1`). A holder of any of them still
sees the chips. Whether the tag panel also answers to the passage's own `BibleReference-ReadOnly`
belongs to #700 (item 5). No read-only role withholds a reader's own reaction (rule 2.11).

**The server decides.** Nothing today: the page writes nothing and reads no data of this
system's — the passage comes from YouVersion. Once it is wired, a suggestion and its withdrawal
are decided under §SEC14.7 posture A′, and a reaction under posture A′ rule 1's exemption and the
§DOM6.10 facet gate (§ARC16.2.1).

## 6. Open Questions and Gaps

1. (#700) **Page gap — `/BibleReferences`: the association panels hold sample material and
   write nothing.** Held for #700 (item 5), with rules 2.6 and 2.7. The page reads no association: the default passage, John
   14:6, shows the tags and related references curated for it in `src/data/sampleScripture.ts`,
   and every other passage shows empty panels (`bibleReference.tsx` — header comment,
   `isCuratedReference`, lines 66-72). Each suggestion is held on the page, as the reader's own,
   until they navigate away, and nothing is written (the comment above the suggestion state,
   lines 54-60; the two panels' `onAdd` and `onRemove`). The association read and the two writes
   are designed (§ARC17.4) and not yet served: `AssociationsController` serves a reader's reaction alone (#728), and an editorial suggestion waits on #871. One step before them
   is not designed: the association read is keyed on the host's id (§DOM4.6 rule 1), while this
   page is addressed by the passage's USFM (rule 2.2), which §DOM5.4 makes the `BibleReference`
   row's unique key, and no section says how the page finds the passage's row from its address,
   or what it shows for a passage no row records yet. That needs design before the page's tasks,
   and #700 designs it.
2. (#700) **Page gap — `/BibleReferences`: the facet switches are not wired.** Held for #700
   (item 5). Copied from
   `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 2` and
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 4`, this page's
   share of them. Rule 2.9: the tag panel follows the passage's `ShowTags` and `TagsAllowed`, and
   the related-references panel its `ShowRelatedBibleReferences` and
   `RelatedBibleReferencesAllowed`, from `BibleReferenceSetting` (§DOM6.9). The page renders both
   panels unconditionally and passes no `showAdd`, so both add boxes show at the stories' default,
   on (`bibleReference.tsx`, lines 137-166). `BibleReferenceSetting` is not built, so `src/` has
   no model to read it from (§ARC16.2.1). The page's half is to read the passage's winning setting
   and wire the two panels by it once the entity exists. The same gap on a content item's pages is
   `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and
   `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`.
3. (#700) **Page gap — `/BibleReferences`: the reaction bar records nothing.** Held for #700
   (item 5). Rules 2.10 and 2.11. The bar's `onReact` names the chosen reaction beneath it for the visit, for every
   reader, signed in or not; nobody is sent to sign in, and nothing is written
   (`bibleReference.tsx`, lines 84-85 and 122-131). Its counts are the sample reactions' on the
   default passage and zero on every other (lines 74-76). It renders on every passage and accepts
   every reaction, since no `BibleReferenceSetting` exists to switch it (§DOM6.9 rule 7;
   §ARC16.2.1). Recording and withdrawing a reader's reaction are designed for a content item and not yet
   built (§ARC16.8.1); what a reaction to a passage records is #700's (rule 2.11). The sign-in half uses the
   one reusable sign-in action (`UI/Pages/Home.md §6 item 3`). Likes are the first feature in the
   user's order under §UI20.6.6 rule 4.
4. (#700) **Page gap — `/BibleReferences`: the page supplies no chip destination and no
   sign-in action to the two panels.** Held for #700 (item 5). Rule 2.12 (§UI20.6.4; §UI20.6.6 rule 2). The page passes
   none of `chipHrefFor`, `chipOnClick`, `loginHref` and `loginButtonOnClick` to either panel
   (`bibleReference.tsx`, lines 137-166), so each renders the route its component composes
   itself: `/Search?q=<tag>` for a tag; for a reference, `/BibleReferences/<USFM>`, or
   `/Search?q=<reference>` — the demo search page — for one it cannot read
   (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 5`); and
   `/Account/Login?returnUrl=<path>` for the login prompts. The prompts read `isAuthenticated`
   alone (`associationPanel.tsx`, line 213), which is false while a signed-in reader's sign-in
   state is still being read, so such a reader arriving on a full page load is shown *Login to
   suggest a tag* until it is (`UI/Components/AssociationPanel.md §10 item 15`). The components'
   halves are `UI/Components/AssociationPanel.TagAssociationPanel.md §10 items 1 and 5`,
   `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 items 2 and 5` and
   `UI/Components/AssociationPanel.md §10 item 15`. The sign-in half needs the one reusable
   sign-in action (`UI/Pages/Home.md §6 item 3`). Where each chip leads is rules 2.12 and 2.16's,
   as on every page of the user section; the page's half is to supply it.
5. **Note — what belongs to #700, ruled.** This item asked whether this document keeps the rules
   the design already settles for every page, or holds them for #700, *Design The Bible Reference
   Page*, which names the same points among those its design is to settle. The user ruled on
   2026-09-27, in their words: "Yes, we need to design this page still." The document keeps only
   the rules that bind every page: the setting switches of §DOM6.9 (rules 2.9 and 2.10); the
   read-only blocks (rule 2.11 and section 5); the page supplying every link, with a tag leading
   to `/posts` handed the tag and a related reference to the page showing the passage, which
   says it could not be found where it cannot be read, as on every page of the user
   section (rules 2.12 and 2.16); and sign-in (rules 2.11 and 2.12). Beyond those, it records the two
   rules the user ruled for this page itself, its search (rule 2.17) and its message for a
   reference it cannot read (rule 2.18), and holds them for #700 to place. It holds every
   rule and gap specific to this page for #700: what the page shows for a passage and where its
   text comes from; what it reads for the passage's tags and related references, and who may
   suggest one (rules 2.6 and 2.7); what a reader's reaction to a passage records (rule 2.11);
   where any hook specific to a passage leads (rule 2.12); and what shows while the settings
   load. Items 1–4, 6 and 7 are held for #700 and tagged with it, so the sweep
   carves no task for this page before the mockup; #700's design carves them. The rules drawn from
   the code record the page as built until then. Telling the tag panel that its host is a passage
   is #700's too: the server refuses a suggestion blocked on either end of the association
   (§SEC14.7 posture A′ rule 1), and on this page the host end is the passage, a `BibleReference`,
   so `BibleReference-ReadOnly` bars a tag suggested on it. The related-references panel composes
   `BibleReference-ReadOnly` for its own end
   (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 2.12`), while the tag
   panel composes `ReadOnly` and `Tag-ReadOnly` alone
   (`UI/Components/AssociationPanel.TagAssociationPanel.md rule 2.9`), and
   `UI/Components/AssociationPanel.md rule 2.30` provides for the host end's roles only where the
   panel hangs off a post. So a holder of `BibleReference-ReadOnly` alone is shown the tag panel's
   box here, and the server refuses what they suggest.
6. (#700) **Page gap — `/BibleReferences`: no button leads to the search for the passage.** Held
   for #700 (item 5). Rule 2.17 (user ruling 2026-09-27): a button leads to `/posts` with the
   passage in its Bible references box, listing every item associated with the passage. The page has
   none: its one link beneath the passage is *Show Full Chapter* (`bibleReference.tsx`, line 108). A
   reference narrows `/posts` only once the association read is exposed over HTTP (§ARC17.4, not yet
   built) and a read narrows on it (`UI/Components/ContentItemListPanel.md §10 item 15`, not yet
   designed), so until then the search shows the whole journal, the reference in its box. #700's
   mockup places the button.
7. (#700) **Page gap — `/BibleReferences`: a reference the page cannot read shows Not Found.** Held
   for #700 (item 5). Rule 2.18 (user ruling 2026-09-28, in their words: "open page, display "{x}"
   could not be found  check and confirm this is correct or search for something else.  then show
   the new search box"). Today an address the page cannot parse renders the site's Not Found page
   (`src/pages/bibleReferenceView.tsx`, lines 16-23), and no product link opens the page for such a
   reference: each sends it to `/Search?q=<reference>` (`UI/Pages/Home.md §6 item 14` and the items
   it names). #700 designs how the page is addressed for it and what surrounds the message, and
   how `{x}` is shown: it echoes the address back, so a crafted link could otherwise put wording of
   its own choosing on the page.
