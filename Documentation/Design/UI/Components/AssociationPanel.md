# 1. AssociationPanel

- **Kind:** Feature — root presentation component
- **Parent:** none — root
- **Children:** [AssociationPanel.TagAssociationPanel.md](AssociationPanel.TagAssociationPanel.md), [AssociationPanel.BibleReferenceAssociationPanel.md](AssociationPanel.BibleReferenceAssociationPanel.md)
- **Composes:** none
- **Used by:** [AssociationPanel.TagAssociationPanel.md](AssociationPanel.TagAssociationPanel.md) and [AssociationPanel.BibleReferenceAssociationPanel.md](AssociationPanel.BibleReferenceAssociationPanel.md), which render it. No page renders it directly, apart from its sample page; every other page reaches it through a story — the pages are listed in section 6.
- **Inherits:** §SEC14.3, §SEC14.5, §SEC14.6 rule 1, §SEC14.7 posture A′, §SEC18.6, §APR8.6 HR-3, §APR9.9, §DOM4.6 rule 1, §DOM4.8, §DOM6.1, §UI20.6.4, §UI20.6.5, §UI20.6.6, `UI/Components/ContentItemPanel.md rule 2.35`
- **Source:** `Websites/Glory2Him.WebApp.React/src/components/associations/associationPanel.tsx`, `associationRoles.ts`, `associations.css`; the item model `src/models/components/associations/associationItem.ts`
- **Sample page:** `/SamplePages/Components/Association-Panel` — `src/pages/samplePages/components/associationPanelDoc.tsx`

A labelled set of association chips — a post's tags, its bible references, anything projected to AssociationItem — with an optional box beneath for suggesting another. Every gate below decides what to render; the foundation services re-decide add, delete and approval against the stored row themselves, so a hidden button is a courtesy to the reader and never an authorization boundary. *(user, 2026-09-26)*

The panel is the generic half of a family. It knows nothing about tags or bible references: a page projects whatever it holds down to `AssociationItem`, and each story dresses the panel for one entity type with its own defaults.

## 2. Business Rules

**2.1 [Must]** The association panel is a generic presentation component that will allow you to present associations that exist between entities. *(user, 2026-09-26)*

**2.2 [Must]** The component must be configurable to allow features to be switched on or off. *(user, 2026-09-26)*

**2.3 [Must]** The component must be configurable so that presentation can be changed via CSS classes. *(user, 2026-09-26)* ≠ item 2

**2.4 [Must]** As this is a generic component, more specific implementations can inherit from this component to cater for a specific need, i.e. Tag Association Panel or Bible Reference Association Panel. *(user, 2026-09-26)*

**2.5 [Must]** The component must be aware of the security context in which it operates so that features can be enabled / disabled based on roles. *(user, 2026-09-26)* ≠ item 1

**2.6 [Must]** The panel never depends on any one entity. A page projects what it holds — a `Tag`, a `BibleReference`, an `Association` row — down to `AssociationItem`: `value`, `createdBy`, `approvalStatus`, `isDeleted` and `id`. *(code: associationItem.ts — AssociationItem)*

**2.7 [Must]** The parent owns the collection. The panel never appends to it or mutates it, and makes no server call; an optimistic chip and a server round-trip are both the caller's call. *(code: associationPanel.tsx — onAdd)*

**2.8 [Could]** A chip's look is a theme CSS class, never a literal colour, so a chip follows the light/dark theme like everything else on the page. *(code: associationPanel.tsx — THEMING; test: associationPanel.test.tsx — "should keep the caller theme class on the chip so it follows light and dark mode")*

**2.9 [Must]** A removed (soft-deleted) item is never rendered to anyone, whatever their role. Removal outranks every other gate, approval included. *(test: associationPanel.test.tsx — "should never render a removed item, whatever the role")*

**2.10 [Must]** A rejected or dismissed suggestion never lingers publicly on the item that refused it — its own contributor included. *(code: associationPanel.tsx — isVisible; test: associationPanel.test.tsx — "should hide a rejected item from its own contributor entirely")*

**2.11 [Must]** A draft is visible to its contributor and to `viewAllRoles` alone. `viewAllRoles` opens every status, so it is the widest grant and is administrators-only by default. *(code: associationPanel.tsx — viewAllRoles; test: associationPanel.test.tsx — "should hide a draft from the publishing tier — it was never put forward")*

**2.12 [Must]** A moderator sees what is waiting on their decision, and only that: a draft was never put forward for anyone to judge, and a refusal has already been judged. *(code: associationPanel.tsx — isVisible; test: associationPanel.test.tsx — "should not let a reviewer see a draft or a refusal, only what awaits them")*

**2.13 [Must]** Switching the actions on never widens who can see a chip. *(test: associationPanel.test.tsx — "should apply the gates even on a moderation surface")*

**2.14 [Must]** `showModerationActions` is the single switch over Remove, Reject and Approve, and it is off by default. Off is the safe posture: chips render by the visibility gates and the panel is read-only, with one carve-out — the contributor may still withdraw their own unapproved item. *(code: associationPanel.tsx — showModerationActions; test: associationPanel.test.tsx — "should offer an administrator nothing at all in the default read-only posture")*

**2.15 [Must]** Turn the actions on for a moderation surface, and leave them off everywhere else. *(code: associationPanel.tsx — showModerationActions)*

**2.16 [Must]** Nobody rules on their own submission. Owning an item suppresses Reject and Approve, an administrator included, whatever `AllowSelfApproval` says; the owner is left with Remove. *(code: associationPanel.tsx — mayModerate; test: associationPanel.test.tsx — "should give the owner a delete rather than a decision on their own submission"; user, 2026-09-26)*

**2.17 [Must]** Remove and Reject are different acts. Remove deletes the association outright; Reject is a recorded verdict that leaves the row in place. A moderator commonly wants both, so the actions compose rather than exclude. *(code: associationPanel.tsx — onRemove; test: associationPanel.test.tsx — "should raise onReject separately from onRemove")*

**2.18 [Must]** A contributor may withdraw (soft-delete) their own suggestion only while it is `Draft` or `Submitted`. Once it is `Approved` or `Rejected` it is locked to them: no withdrawal. An `Administrators` takedown at any status stays, as a separate moderation action (rule 3.2.3). This is the rule for everything subject to approval (§APR9.9). *(code: associationPanel.tsx — mayRemove; test: associationPanel.test.tsx — "should not let the owner withdraw their own item once it is approved"; user, 2026-09-26; user, 2026-09-27; §APR9.9)*

**2.19 [Must]** Ownership is matched on the account id — `createdBy` against the signed-in user's `userId` — and never on a display name, which two accounts can share. *(code: associationPanel.tsx — isOwnedByViewer)*

**2.20 [Should]** When adding is on and nobody is signed in, the add box is replaced by a way in, never simply hidden — otherwise the reader cannot tell the panel accepts suggestions at all. The way in is the login prompt of rule 3.1.16. It is offered to every signed-out reader, because every signed-out reader who would contribute is sent to sign in (§UI20.6.6 rule 2). *(code: associationPanel.tsx — showLoginPrompt; test: associationPanel.test.tsx — "should replace the add box with a login link carrying the return url"; user, 2026-09-27)*

**2.21 [Must]** One box can hold several associations. The characters that separate them are a property, whose default is a comma and a semicolon. Nothing else separates, so "grace and faith" stays one association and a bible reference keeps the colons and dashes it is written with. A story keeps the default or sets its own: the tag story keeps it, and the bible reference story separates on the semicolon alone (`UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md rule 3.1.2`). *(code: associationPanel.tsx — separateValues; test: associationPanel.test.tsx — "should separate on a semicolon and leave the words inside a value alone"; user, 2026-09-27)* ≠ item 17

**2.22 [Must]** Each separated value is judged on its own: an empty value or one already listed is dropped without costing the others. The duplicate check is case-insensitive and runs against the whole collection — a suggestion the reader cannot see is still a duplicate — and against what the same commit has already accepted. The box clears either way. *(code: associationPanel.tsx — commitDraft; test: associationPanel.test.tsx — "should drop the blanks and the repeats and still add the rest")*

**2.23 [Must]** Enter and the add button share one commit path, so separating, normalizing and the duplicate check cannot drift apart between them. *(code: associationPanel.tsx — commitDraft; test: associationPanel.test.tsx — "should separate a list committed through the button just as Enter does")*

**2.24 [Must]** Enter inside the box never submits an enclosing form before the value is read. *(code: associationPanel.tsx — onKeyDown)*

**2.25 [Could]** A chip that navigates is a real link, which can be middle-clicked, opened in a new tab and is announced as a destination. `chipHrefFor` therefore wins over `chipOnClick` when both are given. *(code: associationPanel.tsx — chipHrefFor; test: associationPanel.test.tsx — "should prefix each chip and link it when a href builder is supplied")*

**2.26 [Should]** The actions sit in escalating order of consequence, left to right: Remove, Reject, Approve. *(test: associationPanel.test.tsx — "should order the three actions by escalating consequence")*

**2.27 [Must]** The default moderation tier is the global `Reviewers, Publishers, Administrators` (§SEC18.6). A panel knows only its own end of an association, so it composes only the tier it can state truthfully; a surface that knows its host passes the counterpart tier through `moderationRoles`. Composing narrowly can only hide a control from somebody the server would allow, never show one it would refuse. *(code: associationRoles.ts — GlobalModerationRoles, scopedModerationRoles)* ≠ item 16

**2.28 [Must]** Whether the panel renders, and whether its add box shows, follow the host's effective settings: `Show<Facet>` governs the display component and `<Facet>Allowed` the contribute component (§DOM6.1). Each is ANDed with the page's own switch: the panel renders only where the page renders it and `Show<Facet>` is on, and its add box shows only where `showAdd` is on and `<Facet>Allowed` is on. If either is false, the part is not shown. The facet a story answers to is named in that story's section 2. *(§DOM6.1; user, 2026-09-26)* ≠ `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`

**2.29 [Won't]** The panel never renders within `ContentItemPanel`; it renders beside it, per `UI/Components/ContentItemPanel.md rule 2.35`. *(§UI20.6.2)*

**2.30 [Must]** A viewer who belongs to a read-only role is never offered the add box: they cannot suggest a new association. The panel composes the read-only roles itself (§UI20.6.6 rule 3), for both ends of the association it would add: the global `ReadOnly`; the `%EntityType%-ReadOnly` of the entity type the panel shows, which each story names in its section 2; and, where the panel hangs off a post, the post's `ContentItem-ReadOnly` and its `ContentItem-{ContentType}-ReadOnly` for the post's own type. For the post's type the page hands the panel that type, as data — a property, not a role list — and the panel composes the roles from it; no page supplies a blocking-role list. A block on either end bars the write at the server too (§SEC14.7 posture A′ rule 1). A blocked reader still sees the chips, as anyone else does, in the read-only view (rule 3.2.2). *(user, 2026-09-26; user, 2026-09-27; §SEC18.6; §SEC14.7 posture A′ rule 1)* ≠ items 1 and 20

**2.31 [Must]** Every visible string the panel renders, and every string it renders for a screen reader alone, is a property, whose default is today's text, so a page need set none and may override any (§UI20.6.6 rule 1). *(§UI20.6.6 rule 1)* ≠ item 19

**2.32 [Should]** The panel's one loading state, the loading line of rule 3.1.3, is announced as a status (§UI20.6.6 rule 5). *(§UI20.6.6 rule 5)* ≠ item 18

## 3. Presentation / Behaviour rules

### 3.1 Driven by properties

**3.1.1** `title` is always rendered as the panel's heading. *(code: associationPanel.tsx — AssociationPanel; test: associationPanel.test.tsx — "should render the title and a chip per item")*

**3.1.2** `showBorder` wraps the panel in the bordered card `ContributionPrompt` uses. It is off by default. *(code: associationPanel.tsx — panelCssClass; test: associationPanel.test.tsx — "should leave the panel unbordered by default", "should surround the panel with a border when asked")*

**3.1.3** While `isLoading` is true, a loading line replaces both the chips and the empty text. *(test: associationPanel.test.tsx — "should render the loading text instead of the empty text while loading")*

**3.1.4** When no chip is visible, `emptyText` is shown if it is set; otherwise nothing is shown. *(test: associationPanel.test.tsx — "should render the empty text when nothing is visible"; code: associationPanel.tsx — emptyText)*

**3.1.5** A chip reads `[status icon][chipPrefixText][value]`. Neither the icon nor the prefix is part of the stored value. *(code: associationPanel.tsx — renderChipLabel)*

**3.1.6** The status icon is chosen per status: Approved takes `approvedIconCssClass`, Draft and Submitted take `pendingIconCssClass` (default `bi-hourglass-split`), and Rejected and Dismissed take `rejectedIconCssClass` (default `bi-slash-circle`). Each falls back to `chipIconCssClass` when unset. Because the pending and rejected icons have defaults, the fallback reaches an Approved chip only. *(code: associationPanel.tsx — iconFor; test: associationPanel.test.tsx — "should fall back to the flat chip icon when no status icon is set")*

**3.1.7** The chip's tooltip is chosen per status: `approvedTooltip` (default empty, which renders no tooltip), `pendingTooltip` (default `Pending approval`) and `rejectedTooltip` (default `Not approved`). *(code: associationPanel.tsx — tooltipFor)*

**3.1.8** A Draft or Submitted chip is drawn as pending: the same colour, so it still reads as part of the set, but dashed and with its label faded, so approved and pending are never confused. *(code: associations.css — .g2h-association-chip-pending; test: associationPanel.test.tsx — "should show the pending icon on a draft chip too")*

**3.1.9** An item with no `approvalStatus` is treated as Approved. *(code: associationPanel.tsx — statusOf)*

**3.1.10** The chip's label is a link when `chipHrefFor` is set, a button raising `chipOnClick` when only that is set, and plain text when neither is. *(code: associationPanel.tsx — renderChipLabel; test: associationPanel.test.tsx — "should render a button and raise chipOnClick when no href builder is supplied")*

**3.1.11** `showAdd` switches the suggestion surface on. It is off by default. *(code: associationPanel.tsx — mayAdd)*

**3.1.12** `suggestTitle` and `suggestDescription` each render only when set, and only while the add box or the login prompt is showing. *(code: associationPanel.tsx — AssociationPanel)*

**3.1.13** The add button sits beside the box and reads `addButtonText` (default `Add`). *(test: associationPanel.test.tsx — "should offer the add button on its default label without being asked")*

**3.1.14** `addMaxLength` (default 100) caps what can be typed into the box. *(code: associationPanel.tsx — addMaxLength)*

**3.1.15** `normalizeAddedValue` (default a trim) is applied to each separated value, before the duplicate check and before `onAdd`. *(code: associationPanel.tsx — normalizeAddedValue; test: associationPanel.test.tsx — "should normalize each separated value rather than the box as a whole")*

**3.1.16** The login prompt raises the panel's sign-in hook for the page (§UI20.6.6 rule 2). It is a button raising `loginButtonOnClick` when that is set, and a link to the `loginHref` the page supplies when only that is set (§UI20.6.4). When the page passes neither, it is still the button raising the hook. The panel composes no sign-in route of its own (§UI20.6.4): its default `loginHref`, a sign-in route it composes itself when the page passes none, is the coupling. *(code: associationPanel.tsx — resolvedLoginHref; test: associationPanel.test.tsx — "should raise loginButtonOnClick instead of linking when a handler is supplied"; user, 2026-09-26; user, 2026-09-27)* ≠ item 15

### 3.2 Driven by roles

**3.2.1** Every role property is a comma-separated list; names are trimmed and blanks dropped. `[OWNER]` is never matched as an ordinary role name: it means the contributor of that specific item. *(code: associationPanel.tsx — parseRoles, holdsAnyRole, OwnerRole)*

**3.2.2** **Visibility**, asked per chip in this order: removed → hidden from everyone (rule 2.9); Approved → shown to everyone; the viewer holds a `viewAllRoles` role → shown at any status; the viewer owns it and it is Draft or Submitted → shown; it is Submitted and the viewer holds a `moderationRoles` role → shown; otherwise hidden. *(code: associationPanel.tsx — isVisible)*

**3.2.3** **Remove** is never offered to an anonymous reader. The owner branch is resolved first: when `removeRoles` lists `[OWNER]` and the viewer owns a Draft or Submitted item, Remove is offered whatever `showModerationActions` says. Every other Remove needs `showModerationActions` on, and then either an empty `removeRoles` (any signed-in reader) or a listed role. The default `removeRoles` is `[OWNER], Administrators`. *(code: associationPanel.tsx — mayRemove; test: associationPanel.test.tsx — "should let any authenticated reader remove when the role list is empty")*

**3.2.4** **Reject and Approve** are offered together, and only when `showModerationActions` is on, the viewer is signed in, the item is Submitted, the viewer does not own it, and the viewer holds a `moderationRoles` role of the publisher tier or `Administrators`. The review tier is never offered either, at any scope: a reviewer may never set an `ApprovalStatus` (§APR8.6 HR-3), and the server admits only the publisher tier to an association's approval transition (§SEC14.7 posture A′). `[OWNER]` in `moderationRoles` is ignored. *(code: associationPanel.tsx — mayModerate; test: associationPanel.test.tsx — "should not offer a decision on an already approved item"; §APR8.6 HR-3)* ≠ item 16

**3.2.5** **The add box** shows when `showAdd` is on, the viewer is signed in, the viewer holds no read-only role (rule 2.30), and `addRoles` is empty (the default: any signed-in reader) or the viewer holds a listed role. A signed-in reader lacking the role sees neither the box nor a login prompt. `[OWNER]` in `addRoles` is ignored — there is no item yet to own. *(code: associationPanel.tsx — mayAdd; test: associationPanel.test.tsx — "should hide the add box entirely from a signed-in reader lacking the add role"; user, 2026-09-26; user, 2026-09-27)* ≠ items 1 and 20

### 3.3 Combinations

**3.3.1** `isDeleted` outranks every grant, `viewAllRoles` included. *(test: associationPanel.test.tsx — "should never render a removed item, whatever the role")*

**3.3.2** Visibility (rule 3.2.2) is asked independently of `showModerationActions`: the switch changes which actions a visible chip carries, never which chips are visible. *(test: associationPanel.test.tsx — "should apply the gates even on a moderation surface")*

**3.3.3** With `showModerationActions` off, the owner's withdrawal is the only action any viewer gets; an administrator gets none. *(test: associationPanel.test.tsx — "should still let a contributor withdraw their own unapproved item with actions off")*

**3.3.4** An owner who also holds a moderation role gets Remove on their own Submitted item and never Reject or Approve. With actions on and `Administrators` held, they also get Remove on their own Approved item, through the role rather than the owner branch. *(code: associationPanel.tsx — mayRemove, mayModerate; test: associationPanel.test.tsx — "should give the owner a delete rather than a decision on their own submission")*

**3.3.5** The login prompt depends on `showAdd` and on being signed out alone. It is shown even when `addRoles` would refuse the reader after they sign in (rule 2.20). *(code: associationPanel.tsx — showLoginPrompt; user, 2026-09-27)*

**3.3.6** The read-only roles the panel composes (rule 2.30), the host post's included, are asked before every grant, and withhold the add box, Remove, Reject and Approve, the owner's withdrawal included (§SEC18.6 rule 2, rule 2.5), as the server refuses a write blocked on either end (§SEC14.7 posture A′ rule 1). The chips stay visible (rule 3.3.2). *(§SEC18.6 rule 2; §SEC14.7 posture A′ rule 1; user, 2026-09-26; user, 2026-09-27)* ≠ items 1 and 20

### 3.4 Role matrix

Defaults throughout: `viewAllRoles="Administrators"`, `moderationRoles="Reviewers, Publishers, Administrators"`, `removeRoles="[OWNER], Administrators"`, `addRoles=""`. **Owner** is the account whose id equals the chip's `createdBy` — the contributor of that association row. Ownership is per chip, so the Owner column describes the viewer's own chips and every other column describes somebody else's.

| Property and/or Role Condition | Anonymous | Signed-in reader | Owner | Reviewer | Publisher | Administrator |
| --- | --- | --- | --- | --- | --- | --- |
| Approved chip — **visible** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Submitted chip — **visible** | ❌ No | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Draft chip — **visible** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ✅ Yes |
| Rejected or Dismissed chip — **visible** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| Removed chip (`isDeleted`) — **visible** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=false`, Draft or Submitted chip — **Remove** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ❌ No |
| `showModerationActions=false`, any chip — **Reject / Approve** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=false`, Approved, Rejected or Dismissed chip — **Remove** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=true`, Draft chip — **Remove** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ✅ Yes |
| `showModerationActions=true`, Submitted chip — **Remove** | ❌ No | ❌ No | ✅ Yes | ❌ No | ❌ No | ✅ Yes |
| `showModerationActions=true`, Submitted chip — **Reject / Approve** ≠ item 16 | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes | ✅ Yes |
| `showModerationActions=true`, Approved chip — **Remove** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| `showModerationActions=true`, Rejected or Dismissed chip — **Remove** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| `showModerationActions=true`, Draft, Approved, Rejected or Dismissed chip — **Reject / Approve** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=true`, the viewer's own Submitted chip, viewer also holds the column's tier — **Reject / Approve** | ➖ n/a | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No |
| `showModerationActions=true`, the viewer's own Submitted chip, viewer also holds the column's tier — **Remove** | ➖ n/a | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showAdd=false` — **add box** or **login prompt** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showAdd=true`, no read-only role the panel composes (rule 3.3.6) — **add box** | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showAdd=true` — **login prompt** | ✅ Yes | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showAdd=true`, `addRoles="Administrators"`, no read-only role the panel composes — **add box** | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No | ✅ Yes |
| `showAdd=true`, `addRoles="Administrators"` — **login prompt** | ✅ Yes¹ | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showAdd=true`, viewer also holds a read-only role the panel composes (rule 2.30) — **add box** ≠ items 1 and 20 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| `showAdd=true`, the host post's type given, viewer also holds that post's `ContentItem-ReadOnly` or `ContentItem-{ContentType}-ReadOnly` for its type (rule 2.30) — **add box** ≠ item 20 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Viewer also holds a read-only role the panel composes (rule 2.30) — **Approved chips**, the read-only view | ➖ n/a | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| `showModerationActions=true`, Submitted chip, viewer also holds a read-only role the panel composes (rule 2.30) — **Reject / Approve** ≠ items 1 and 20 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |
| Either `showModerationActions`, any chip, the viewer's own included, viewer also holds a read-only role the panel composes (rule 2.30) — **Remove**, the owner's withdrawal included ≠ items 1 and 20 | ➖ n/a | ❌ No | ❌ No | ❌ No | ❌ No | ❌ No |

¹ Shown although the reader, once signed in, would not get the box (rules 2.20 and 3.3.5).

## 4. Properties and Events

### 4.1 Properties

The panel renders no child presentation component, so no property passes through to one.

| Property | Type | Default | Purpose | Passes through to |
| --- | --- | --- | --- | --- |
| `title` | `string` | required | Panel heading; also names the add box for assistive technology when `suggestTitle` is empty. | — |
| `showBorder` | `boolean` | `false` | Wraps the panel in the bordered card. | — |
| `cssClass` | `string` | `''` | Extra classes on the panel's `<section>`. | — |
| `associationCollection` | `ReadonlyArray<AssociationItem>` | `[]` | The chips. Owned by the parent. | — |
| `chipCssClass` | `string` | `'btn-success-soft'` | Theme class carrying the chip's look. | — |
| `chipPrefixText` | `string` | `''` | Literal text before every value, such as a hash. | — |
| `chipIconCssClass` | `string` | none | Icon used when no status icon applies (rule 3.1.6). | — |
| `approvedIconCssClass` | `string` | none | Icon on an Approved chip. | — |
| `pendingIconCssClass` | `string` | `'bi-hourglass-split'` | Icon on a Draft or Submitted chip. | — |
| `rejectedIconCssClass` | `string` | `'bi-slash-circle'` | Icon on a Rejected or Dismissed chip. | — |
| `approvedTooltip` | `string` | `''` | Tooltip on an Approved chip; empty renders none. | — |
| `pendingTooltip` | `string` | `'Pending approval'` | Tooltip on a Draft or Submitted chip. | — |
| `rejectedTooltip` | `string` | `'Not approved'` | Tooltip on a Rejected or Dismissed chip. | — |
| `chipHrefFor` | `(item) => string` | none | Renders the label as a link to the returned address. Wins over `chipOnClick`. | — |
| `isLoading` | `boolean` | `false` | Shows the loading line. | — |
| `emptyText` | `string` | `''` | Shown when no chip is visible. | — |
| `viewAllRoles` | `string` (comma-separated) | `'Administrators'` | Roles that see an item at any status. | — |
| `showModerationActions` | `boolean` | `false` | The switch over Remove, Reject and Approve (rule 2.14). | — |
| `removeTooltip` | `string` | `'Remove'` | Tooltip and accessible-name prefix of Remove. | — |
| `removeIconCssClass` | `string` | `'bi-x-lg'` | Icon of Remove. | — |
| `removeButtonCssClass` | `string` | `'btn-danger'` | Theme class of Remove. | — |
| `removeRoles` | `string` (comma-separated) | `'[OWNER], Administrators'` | Who may remove; empty means any signed-in reader. | — |
| `approveTooltip` | `string` | `'Approve'` | Tooltip and accessible-name prefix of Approve. | — |
| `rejectTooltip` | `string` | `'Reject'` | Tooltip and accessible-name prefix of Reject. | — |
| `approveIconCssClass` | `string` | `'bi-check-lg'` | Icon of Approve. | — |
| `rejectIconCssClass` | `string` | `'bi-slash-circle'` | Icon of Reject. | — |
| `approveButtonCssClass` | `string` | `'btn-success'` | Theme class of Approve. | — |
| `rejectButtonCssClass` | `string` | `'btn-warning'` | Theme class of Reject. | — |
| `moderationRoles` | `string` (comma-separated) | `'Reviewers, Publishers, Administrators'` | Who may decide; also who sees a Submitted item. | — |
| `showAdd` | `boolean` | `false` | Switches the suggestion surface on. | — |
| `addRoles` | `string` (comma-separated) | `''` | Further restricts who may suggest; empty means any signed-in reader. | — |
| `suggestTitle` | `string` | `''` | Heading above the box, rendered uppercase. | — |
| `suggestDescription` | `string` | `''` | Prompt under the heading. | — |
| `addPlaceholderText` | `string` | `''` | Placeholder inside the box. | — |
| `addMaxLength` | `number` | `100` | Cap on what can be typed. | — |
| `addButtonText` | `string` | `'Add'` | Label of the add button. | — |
| `normalizeAddedValue` | `(rawValue) => string` | trim | Applied to each separated value before the duplicate check. | — |
| `loginHref` | `string` | the panel's own sign-in route, today's coupling (section 10, item 15) | Where the login prompt points; the page supplies it (rule 3.1.16). | — |
| `loginButtonText` | `string` | `'Login to suggest'` | Label of the login prompt. | — |
| `loginButtonCssClass` | `string` | `'btn-outline-primary'` | Theme class of the login prompt. | — |

Rule 2.21 requires a property for the add box's separators, rule 2.31 one for the loading line's text, and rule 2.30 one carrying the host post's content type; none exists yet (section 10, items 17, 19 and 20).

### 4.2 Events

| Event | Payload | Raised when |
| --- | --- | --- |
| `chipOnClick` | the `AssociationItem` | the chip label is clicked and no `chipHrefFor` is set |
| `onRemove` | the `AssociationItem` | Remove is clicked |
| `onReject` | the `AssociationItem` | Reject is clicked |
| `onApprove` | the `AssociationItem` | Approve is clicked |
| `onAdd` | one normalized `string` | once per accepted value, on Enter or the add button (rules 2.21–2.23). Several calls can land in one tick, so a handler that appends to state must do so functionally. |
| `loginButtonOnClick` | none | the login prompt is clicked; supplying it turns the prompt into a button |

### 4.3 Pass-through properties

Per §UI20.6.5.

- **As a parent:** the panel renders no child presentation component, so it forwards nothing.
- **As the component its stories render:** each story renders the panel with its own defaults and forwards every other property unchanged. The story files list what each one forwards and where it cannot: `UI/Components/AssociationPanel.TagAssociationPanel.md §4.3` and `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §4.3`.
- **Through a consumer's parent:** no component renders the panel or its stories — `ContentItemPanel` draws its own tag and bible reference sections and does not render this family. Pages therefore set every property directly, and no intermediate parent has to forward anything. *(code: contentItemDefaultPanel.tsx — showTagSection; grep of `src/` for the three component names)*

## 5. Security Requirements

**Security and access matrix**

Every read-only role in the **Blocked by** column is composed by the component itself, from what it represents: no page hands it a blocking-role list, and no page can add to or remove from those roles (§UI20.6.6 rule 3; user ruling 2026-09-27). The host post's content type, which the page hands the panel as data, is what it composes the post's read-only roles from; it is not a role list (rule 2.30).

| Action | Offered to | Blocked by | A blocked-role holder | A signed-out reader | The server decides |
| --- | --- | --- | --- | --- | --- |
| **The chips** (a gated view) | Every viewer the visibility gate admits, per status (rule 3.2.2) | None: a read, and reads stay outside the veto (§SEC14.7 posture A′ rule 3) | ✅ Allowed — the same chips as anyone of their tier, in the read-only view (rule 2.30) | ✅ Approved chips only (rule 3.2.2) | The association read the consumer hands in (§SEC14.3, §SEC14.7 posture A rule 4) |
| **A chip's label** — a link or a hook | Everyone who sees the chip (rule 3.1.10) | None: it writes nothing | ✅ Allowed | ✅ Offered; raises `chipOnClick`, or follows the link the page supplies through `chipHrefFor` (rule 3.1.10) | Nothing: where it leads is the page's (§UI20.6.4) |
| **The add box** — typing, Enter and Add, raising `onAdd` ≠ items 1 and 20 | A signed-in reader when `showAdd` is on; `addRoles` narrows it (rule 3.2.5) | `ReadOnly`, the `%EntityType%-ReadOnly` the story names, and on a post the post's `ContentItem-ReadOnly` and `ContentItem-{ContentType}-ReadOnly` for its type (rule 2.30) | ❌ Refused — the box is withheld, with no login prompt in its place (rules 2.30, 3.2.5) | Not offered; the login prompt stands in its place (rule 2.20) | §SEC14.7 posture A′ rule 1, which also asks the host end's blocks |
| **The login prompt** ≠ item 15 | Every signed-out reader, when `showAdd` is on (rules 2.20, 3.3.5) | None: its reader holds no role | Not shown — a blocked-role holder is signed in | ✅ Offered; raises `loginButtonOnClick`, or follows the `loginHref` the page supplies (rule 3.1.16) | Nothing: sign-in is the page's (§UI20.6.6 rule 2) |
| **Remove**, raising `onRemove` ≠ items 1 and 20 | The owner, on their own `Draft` or `Submitted` chip, whatever `showModerationActions` says (rule 2.18); with `showModerationActions` on, `removeRoles` — by default `Administrators`, at any status (rule 3.2.3) | As the add box (rule 2.30) | ❌ Refused — withheld, the owner's withdrawal included (rule 3.3.6) | Not offered (rule 3.2.3) | §SEC14.7 posture A rule 3 and posture A′ rule 4; the owner's window is §APR9.9, which the server does not ask yet (section 10, item 12) |
| **Reject**, raising `onReject` ≠ items 1, 16 and 20 | A `moderationRoles` holder of the publisher tier or `Administrators`, on somebody else's `Submitted` chip, with `showModerationActions` on (rule 3.2.4) | As the add box (rule 2.30) | ❌ Refused — withheld (rule 3.3.6) | Not offered (rule 3.2.4) | `TransitionAssociationApprovalAsync`: the publisher tier, never the review tier (§APR8.6 HR-3) (§SEC14.7 posture A′). The row's author may approve it by the ordinary route only where `AllowSelfApproval` permits, and an `Administrators` author may bypass over their own submission (§APR8.6 HR-2); the panel is stricter and offers its author neither Reject nor Approve (rule 2.16; item 11) |
| **Approve**, raising `onApprove` ≠ items 1, 16 and 20 | As Reject (rule 3.2.4) | As the add box (rule 2.30) | ❌ Refused — withheld (rule 3.3.6) | Not offered (rule 3.2.4) | As Reject |

The stories name the entity type each composes: `UI/Components/AssociationPanel.TagAssociationPanel.md §5` and `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §5`.

- **Render-only.** Every gate in section 3 decides what to render and nothing more. Each write the panel raises is re-decided by the services against the stored row (§SEC14.6 rule 1, §SEC14.7 posture A′). *(user, 2026-09-26; code: associationPanel.tsx — SECURITY POSTURE)*
- **Identity comes from the auth context.** The panel reads `isAuthenticated`, `user.userId` and `userRoles` from `useAuth()` itself, rather than taking them as properties, because the owner rules need the viewer's identity per item. *(code: associationPanel.tsx — AssociationPanel)*
- **Ownership is the account id** (rule 2.19).
- **Role names follow §SEC18.6**: plural capabilities, entity-type-scoped pairs `%EntityType%-Reviewers` / `-Publishers` (rule 2.27).
- **The visibility gate subtracts from what the consumer supplies.** The rows a consumer hands in must already be what the association read returns to that caller (§SEC14.3, §SEC14.5 rule 4); the panel's gate hides further and never reveals a row the read withheld.
- **The `ReadOnly` veto** (§SEC18.6 rule 2) is asked before every grant (rules 2.30 and 3.3.6). ≠ items 1 and 20

## 6. Composition and Usage

Every page renders the family through its stories. No page renders `AssociationPanel` directly; only its sample page does.

| Route | Page | Renders |
| --- | --- | --- |
| `/posts/:contentItemId` | `src/pages/postDetail.tsx` | both stories |
| `/myposts/:contentItemId` | `src/pages/myPostDetail.tsx` | both stories |
| `/Admin/Posts/:contentItemId` | `src/pages/admin/contentItemModerationDetailPage.tsx` | both stories |
| `/BibleReferences` | `src/pages/bibleReference.tsx` | both stories |
| `/SamplePages/Post/Post-Single-Magazine` | `src/pages/samplePages/post/postSingleMagazineSample.tsx` | both stories |
| `/SamplePages/Components/Association-Panel` | `src/pages/samplePages/components/associationPanelDoc.tsx` | `AssociationPanel` |

`/Post-Single` and `/Post-Single/:slug` (`src/pages/postSingle.tsx`) render both stories too, but that route is a mocked sample page, not a product page: the real item page is `/posts/{id}` (user ruling 2026-09-27). It has no page document, and moving it under the sample pages is recorded in §UI20.5.1.

- The three item-detail pages switch `ContentItemPanel`'s own `showTagSection` and `showBibleReferenceSection` off, so the tags and references render once, in these panels, beside the item. *(code: postDetail.tsx, myPostDetail.tsx, contentItemModerationDetailPage.tsx)*
- No page turns `showModerationActions` on today. *(grep of `src/pages`)*
- Every consumer today renders editorial tags and bible references. None renders a personal association (§DOM4.10) through this family.

## 7. Dependencies

**Data the consumer supplies:** `associationCollection`, projected to `AssociationItem`. The shared projections are `asApprovedAssociations`, `asSuggestedAssociation` and `withoutAssociationValue` in `src/services/views/associations/toAssociationItems.ts`. A suggestion needs `createdBy` set to the viewer's account id, or the owner rule cannot fire and the reader cannot take back what they just typed. *(code: toAssociationItems.ts — asSuggestedAssociation)*

**API endpoints the consumer calls — never the component:**

| Purpose | Endpoint | State |
| --- | --- | --- |
| Read the associations for an entity — "associations for this entity", keyed on the effective id (§DOM4.6 rule 1) | `GET /api/associations` (§ARC17.4) | designed; not yet served (section 10, item 21) |
| Suggest an association (`onAdd`) | `POST /api/associations` (§ARC17.4, §ARC16.8.1) | served for a reader's reaction alone (#728); an editorial suggestion waits on #871 (section 10, item 21) |
| Remove an association (`onRemove`) | `DELETE /api/associations/{id}` (§ARC17.4) — owner or `Administrators` | designed; not yet exposed (section 10, item 22) |
| Approve or reject (`onApprove`, `onReject`) | not ruled — see section 10 | — |

No consumer calls any of them today. `postDetail.tsx`, `myPostDetail.tsx` and `contentItemModerationDetailPage.tsx` pass empty collections and answer `onAdd` with a "coming soon" toast; `postSingle.tsx`, `bibleReference.tsx` and the magazine sample hold suggestions in local state only. *(code: those files — suggestTag, asSuggestion)*

**Indirect:** the auth context from `AuthProvider` (`src/components/securitys/authProvider.tsx`), which reads the current user through `accountService.useGetCurrentUser`; React Router, whose `Link` renders a linked chip label and the login link, and which supplies the current path to the default `loginHref` — today's coupling, section 10, item 15.

## 8. States, Validation and Feedback

- **Loading:** one state, the loading line of rule 3.1.3. It must be announced (rule 2.32); today it is a plain paragraph that no status role announces, and its text, `Loading…`, is not a property (section 10, items 18 and 19).
- **Empty:** rule 3.1.4.
- **Error:** none. The panel receives no error state; a failed read or write is the consumer's to report.
- **Validation:** the box refuses a blank value and a duplicate silently (rule 2.22) and caps length at `addMaxLength` (rule 3.1.14). Anything else is the server's to refuse; the panel has no channel to read a refusal back.
- **Confirmation:** none. Remove, Reject and Approve raise their event at once; any confirmation is the consumer's.
- **Freshness:** the panel shows the collection as of the last properties it was handed. A consumer re-renders it when the collection changes. *(code: associationPanel.tsx — onAdd)*

## 9. Styling and Accessibility

**CSS class properties:** `cssClass`, `chipCssClass`, `chipIconCssClass`, `approvedIconCssClass`, `pendingIconCssClass`, `rejectedIconCssClass`, `removeIconCssClass`, `removeButtonCssClass`, `rejectIconCssClass`, `rejectButtonCssClass`, `approveIconCssClass`, `approveButtonCssClass`, `loginButtonCssClass`. Icons are Bootstrap Icons class names.

**Hooks in `associations.css`:**

- `section.g2h-association-panel` zeroes the theme's section padding, which is page-band spacing and wrong inside a sidebar or card. The `showBorder` padding utilities still win.
- `.g2h-association-chip` is a wrapper, not the clickable element. Its label and its actions sit beside each other inside it, because a button nested in a link or in another button is invalid HTML. The wrapper carries the look; the children carry the behaviour.
- `.g2h-association-chip-label` inherits the chip's colour whether it is a link, a button or text.
- `.g2h-association-chip-action` buttons are square, flush with the chip's edge and clipped to its radius, so they read as attached to the chip.
- `.g2h-association-chip-pending` dashes the border and fades the label only, so action buttons look the same on every chip.

**Hard-coded, not configurable:** the heading's `h4 mb-3`, the add button's `btn btn-primary`, the input's `form-control`, and the loading and empty lines' classes. See section 10.

**Accessibility:**

- The panel is a `<section>` named by its heading (`aria-labelledby`).
- Each action button's accessible name is its tooltip followed by the item's value, such as `Remove faith`.
- The add box is named by `suggestTitle`, or `Add to <title>` when that is empty; the fixed `Add to` is not a property yet (section 10, item 19).
- Status icons are `aria-hidden`; the status is carried by the tooltip, and an Approved chip carries none by default.

## 10. Open Questions and Gaps

1. (needs issue) **`ReadOnly` veto not rendered.** No gate consults `ReadOnly`, `Tag-ReadOnly` or `BibleReference-ReadOnly`. A blocked viewer is offered the add box, Remove, Reject and Approve, and the server then refuses each one (§SEC14.7 posture A′ rule 1, §SEC18.6 rule 2). Evidence: `associationPanel.tsx` — `mayAdd`, `mayRemove`, `mayModerate`. This is the one place the panel falls short of rule 2.5. For the add box the gate is also the user's ruling of 2026-09-26 (rule 2.30). The host post's blocks, which the panel cannot compose until it is told the post's type, are item 20. `ContentItemFormPanel` asks the block question first (`contentItemFormPanel.tsx`, SECURITY POSTURE). `ReviewPanel` withholds its request and reset under the block too, by the user's ruling of 2026-09-26 (`UI/Components/ReviewPanel.md rule 2.50`; today it does not, `UI/Components/ReviewPanel.md §10 item 1`), and differs in one way: by the user's ruling of 2026-09-27 its vote, its decision and its bypass render disabled beside the block, carrying a tooltip that names it (`UI/Components/ReviewPanel.md rules 2.50, 2.53 and 3.3.5`; today they stay live, `UI/Components/ReviewPanel.md §10 items 1 and 9`), where this panel withholds, as its other gates do.
2. (needs issue) **Not every element is styleable through a class property.** Rule 2.3 requires presentation to be changeable through CSS classes. The heading, the add button, the input and the loading and empty lines take hard-coded classes and have no class property. Evidence: `associationPanel.tsx` — `<h4 className="mb-3">`, `className="btn btn-primary mb-0 text-nowrap"`, `className="form-control"`.
3. (needs issue) **Stale comment on drafts.** The `isVisible` comment says a draft is visible to "the publishing tier". The default `viewAllRoles` is `Administrators` alone, and the test "should hide a draft from the publishing tier — it was never put forward" proves Publishers do not see one. Evidence: `associationPanel.tsx` — comment above `isVisible`.
4. (needs issue) **Stale comment on the owner match.** The `createdBy` comment in `associationItem.ts` says it is compared against the user's id **and** username. The panel compares the account id only, and deliberately (rule 2.19). Evidence: `associationItem.ts` — `createdBy`; `associationPanel.tsx` — `isOwnedByViewer`.
5. (needs issue) **The doc page needs updating.** This document follows the component where they disagree. Evidence, all in `associationPanelDoc.tsx`:
   - "Which chips render" names a `hideUnapprovedFromOthers` property and "the filter switched off". The panel has no such property. The test named "should not render a removed item even with the filter turned off" sets no filter either (`associationPanel.test.tsx`).
   - The same section says "an unapproved chip is visible only to someone who has an action available on it". With `showModerationActions` off that is not so: a reviewer sees a Submitted chip, and an administrator sees a Draft chip, with no action on either (rules 3.2.2, 3.3.3).
   - "Which action appears" does not say it assumes `showModerationActions` on. Under the default posture only the owner's withdrawal appears (section 3.4).
   - The props table (`propRows`) omits `cssClass`, `approvedTooltip`, `pendingTooltip`, `rejectedTooltip`, `removeIconCssClass`, `approveTooltip`, `rejectTooltip`, `approveIconCssClass` and `rejectIconCssClass`, all of which `AssociationPanelProps` declares.
   - The `addMaxLength` row says the cap matches "the storage cap". The component does not say so, and this document does not adopt it.
6. (needs issue) **Test name contradicts its setup.** "should still let the owner withdraw their own item with moderation switched off" renders with `showModerationActions={true}`. The switched-off case is covered by "should still let a contributor withdraw their own unapproved item with actions off". Evidence: `associationPanel.test.tsx`, line 731 at 70dc72e7.
7. (needs issue) **Magazine sample suggestions cannot be withdrawn.** `postSingleMagazineSample.tsx` projects each suggestion with `asSuggestedAssociation(value, undefined)`, so the owner rule never fires and the `onRemove` it wires is unreachable. `postSingle.tsx` and `bibleReference.tsx` pass `user?.userId`.
8. **Note — a button with no listener, ruled.** Remove, Reject and Approve render whenever their gate passes, whether or not `onRemove`, `onReject` or `onApprove` is supplied; clicking one then does nothing. For example, `postDetail.tsx` supplies no `onRemove`, so an owner's own pending chip would carry a dead Remove. Asked whether a button should show only when the page connects its hook, the user ruled on 2026-09-27 that there must never be a dead action, that a component may be built before the page that wires it, and that every dead action is planned end to end, down to the API (§UI20.6.6 rule 4). The panel's gates therefore stay as sections 3.2 and 3.3 state them, and a button whose hook nothing handles is closed by wiring it. Where a page leaves one unwired, that is the page's to record.
9. **Note — which endpoint answers Approve and Reject, settled.** This item asked which route decides an association from this panel: the design named none, and nothing maps `onApprove` or `onReject` to one. It was settled on 2026-09-27 in the redesign of `/Admin/Posts/{id}`, #698 (https://github.com/Glory2Him/Glory2Him.Core/issues/698). A suggestion is an association with its own approval round, so it is decided through the same generic approval routes as the post, addressed by the round's id: `GET api/Approvals/{approvalId}/Verdict`, `PUT` or `POST api/Approvals/{approvalId}` with the decision in the body, and `POST api/Approvals/{approvalId}/Reset` (§ARC17.5; ruled under #699, https://github.com/Glory2Him/Glory2Him.Core/issues/699, not yet built). Every item that implements `IApproval`, an association among them, stores its approval id (§APR7.4 item 6), so the page reads it off each association. The panel raises the hooks and calls nothing; mapping them to those routes is the page's (§UI20.6.4), and the page that will do it is the one #698 redesigns.
10. **Note — the admin moderation detail page and associations, ruled.** This item asked whether `/Admin/Posts/:contentItemId` is a moderation surface for associations. It renders both stories without `showModerationActions`, `onRemove`, `onApprove` or `onReject`, and without the counterpart `ContentItem-Reviewers` / `ContentItem-Publishers` tier in `moderationRoles` that `associationRoles.ts` says a host-aware surface passes, while rule 2.15 says a moderation surface turns the actions on; its collections stay empty until the association read is exposed over HTTP (item 21). The user ruled on 2026-09-27 that the page needs a design session, and it is redesigned under #698 (https://github.com/Glory2Him/Glory2Him.Core/issues/698), from the product owner's mocks in the design session of #705 (https://github.com/Glory2Him/Glory2Him.Core/issues/705; user ruling 2026-09-28). Every tag and Bible reference is an approvable association with its own round, and the redesign settles where each one's round appears, what a moderator can do on each — see its reviews, cast a review, decide it, reset it, request reviewers and take it down — and whether the suggest box shows there. The page's document records the page as built (`UI/Pages/ContentItemModerationDetailPage.md`).
11. (needs issue) **Global correction — the `AllowSelfApproval` setting is to be removed.** The panel never offers Approve to the owner (rule 2.16), while the server admits self-approval where `AllowSelfApproval` permits, and an `Administrators` bypass over their own submission (§APR8.6 HR-2). The user ruled on 2026-09-26 that the panel stays strict, whatever the setting says, and that the setting itself is to be removed. The panel already behaves so, and no marker sits on its rules. The removal is the global correction (§UI20.6.4 case 1): `Approval.md` describes the setting as live (§APR8.6 HR-2, §APR8.2), and removing it is neither designed nor built. The issue that tracked it, #697 ("DESIGN: Remove The AllowSelfApproval Setting"), was closed as not planned when the older issues were closed on 2026-09-27, so this item is where the sweep finds the work.
12. **Note — the server's removal rule agrees; its gate is not yet built to it.** The owner may withdraw (soft-delete) their suggestion only while it is Draft or Submitted; once it has been reviewed it is locked (user rulings 2026-09-26 and 2026-09-27). Rule 2.18 is that rule, and the panel already follows it. On 2026-09-27 the user ruled that the same holds for everything subject to approval, with an `Administrators` takedown at any status kept as a separate moderation action; that global rule is §APR9.9. §SEC14.7 posture A rule 3 now says it for removal — the owner only while the stored row is `Draft` or `Submitted`, `Administrators` at any status — and posture A′ rule 4 asks the owner test with its status bound in the foundation, once the row is loaded. The server's gate is not yet built to it: `AssociationService.Validations.cs` — `ValidateUserCanRemoveStorageAssociationAsync` asks ownership or `Administrators`, never the status. §APR9.9 rule 8 records that server work as not yet built, so this document carries no marker for it, as `UI/Components/ContentItemPanel.md §10 item 19` does for the same work on content items.
13. **Note — login prompt versus `addRoles`, ruled.** A signed-out reader is invited to sign in even when `addRoles` would then refuse them the box (rule 3.3.5). Asked whether every signed-out reader should be invited even where suggestions are limited to roles, the user ruled on 2026-09-27 that every signed-out reader who would contribute is sent to the sign-in page (§UI20.6.6 rule 2). Rules 2.20 and 3.3.5 state it, and the panel already behaves so.
14. **Moved to the page documents.** Facet switches not wired — now `UI/Pages/BibleReference.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 8`, `UI/Pages/MyPostDetail.md §6 item 6` and `UI/Pages/ContentItemModerationDetailPage.md §6 item 9`, by way of the stories' own pointers, `UI/Components/AssociationPanel.TagAssociationPanel.md §10 item 2` and `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 4`.
15. (needs issue) **The default `loginHref` composes a route.** Rule 3.1.16 leaves the login prompt's route to the page (§UI20.6.4): the prompt raises the panel's sign-in hook, and the page sends the reader to sign in (§UI20.6.6 rule 2). Asked what the prompt should be when the page supplies no sign-in link, the user ruled on 2026-09-27 that the component provides the hook, so it is then the button raising `loginButtonOnClick` (rule 3.1.16). Where the page passes neither `loginHref` nor `loginButtonOnClick`, the panel composes `/Account/Login?returnUrl=<current path, URI-encoded>` itself, reading the current path from the router. No page in section 6 passes either, so each renders the panel's own route; the stories keep this default. The default also breaks rule 2's last sentence, that a reader whose sign-in state has not been read back is not sent to sign in. The panel reads no `isLoading` from the auth context, and the context reports a reader as signed out until the current user has been read (`authProvider.tsx` — `AuthContext`). So until then, a signed-in reader is shown the prompt, and the default link sends them to sign in. Evidence: `associationPanel.tsx` — `resolvedLoginHref` (line 219 at 70dc72e7), `showLoginPrompt`.
16. (needs issue) **Gap — the review tier is offered Approve and Reject.** Rule 3.2.4 withholds both from the review tier: a reviewer may never set an `ApprovalStatus` (§APR8.6 HR-3, written 2026-08-07), and the association's approval transition admits only the publisher tier (§SEC14.7 posture A′, `TransitionAssociationApprovalAsync`, written 2026-08-17). Both rules predate the panel (`25527be6`, 2026-08-28). Neither later change to the panel's roles addressed who may decide (`9a9a753b`, `9d8bf0bf`). The panel offers Reject and Approve to any `moderationRoles` holder, and the default `moderationRoles` includes `Reviewers`, and the stories add `Tag-Reviewers` or `BibleReference-Reviewers`. The server then refuses the reviewer's click. `moderationRoles` also decides who sees a `Submitted` chip (rule 3.2.2), which the review tier keeps: posture A rule 4 admits the review roles to non-public rows. So the fix separates who decides from who sees; how is the building task's to settle. `ReviewPanel` already withholds its decision from the review tier (`UI/Components/ReviewPanel.md rule 2.22`). No test asserts a reviewer's decision either way; the scoped-moderator tests use `Tag-Publishers` and `BibleReference-Publishers`. Evidence: `associationPanel.tsx` — `mayModerate`; `associationRoles.ts` — `GlobalModerationRoles`, `scopedModerationRoles`.
17. (needs issue) **Gap — the add box's separators are not a property.** Rule 2.21 (user ruling 2026-09-27) makes them a property whose default is a comma and a semicolon. They are hard-coded, so no story can narrow them. Evidence: `associationPanel.tsx` — `separateValues` (line 157 at 70dc72e7). The bible reference story's use of it is `UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md §10 item 1`.
18. (needs issue) **Gap — the loading line is not announced.** Rule 2.32 applies §UI20.6.6 rule 5. The loading line is a plain paragraph with no `role="status"` or other live region, so assistive technology is not told the chips are loading. Evidence: `associationPanel.tsx` — the `isLoading` branch, `<p className="small text-muted mb-3">Loading…</p>` (line 475 at 70dc72e7).
19. (needs issue) **Gap — a visible string is not a property.** Rule 2.31 applies §UI20.6.6 rule 1. One string the panel renders has no property: the loading line's `Loading…`. Every other visible string is a property already (section 4.1). §UI20.6.6 rule 1 covers a string written for a screen reader alone too (user ruling 2026-09-27), and one of those has none either: the add box's accessible name falls back to `Add to <title>` when `suggestTitle` is empty, and `Add to` is fixed. Evidence: `associationPanel.tsx` — the `isLoading` branch (line 475 at 70dc72e7) and the add box's `aria-label` (line 515).
20. (needs issue) **Gap — the host post's read-only roles are not composed.** Rule 2.30 (user
    ruling 2026-09-27) withholds the add box from a holder of the host post's
    `ContentItem-{ContentType}-ReadOnly` for its type, and of its `ContentItem-ReadOnly`, since
    §SEC14.7 posture A′ rule 1 has the server refuse a block on either end; and rule 3.3.6
    withholds Remove, Reject and Approve on the same terms. The page is to hand the panel the
    post's content type, as data, for it. The panel takes no such property and composes no role
    for the host's end: `mayAdd` asks only `showAdd`, sign-in and `addRoles`
    (`associationPanel.tsx`, lines 340-343 at 70dc72e7), and the roles file says a panel "KNOWS
    ONLY ITS OWN END" (`associationRoles.ts`, lines 8-16). The stories forward every panel
    property through their rest spread
    (`UI/Components/AssociationPanel.TagAssociationPanel.md §4.3`), so the property reaches them
    with the panel's. The panel's own end's roles, which it does not compose either, are item 1.
21. (#857) **Gap — this family cannot read or write its associations over HTTP.** The reads and writes the
    page calls for this family — reading an entity's associations, suggesting one and removing
    one (section 7; §ARC17.4) — are designed and not built: `AssociationsController` serves only a reader's
    reaction (#728) and the reaction summaries (#730), so no product page can read or write an
    association through the family, and every
    page in section 6 passes an empty collection or holds suggestions in its own state. A page
    list narrowed by tag or Bible reference waits on the same read, and the narrowing itself is
    not this item's: it is `UI/Components/ContentItemListPanel.md §10 item 15`, since #857 plans
    only the read (user ruling 2026-10-05). The work was filed as #318 (closed). Suggesting
    one is item 24. Reading an entity's associations is #857's, which
    designs §ARC17.4's two reads with §SEC14.7 posture A′ rule 7 ahead of them (user ruling
    2026-10-02). Removing one is item 22.
22. (#700) **Gap — an association cannot be removed over HTTP.** Removing one, the third part of
    item 21's gap, `DELETE /api/associations/{id}` (section 7; §ARC17.4), is held by a design task
    other than item 21's: #700's point 6 plans the remove by id and the two gates beneath it.
23. (needs issue) **Gap — code comments still send a reader to #318.** #318 was closed as not
    planned on 2026-09-27, yet 39 lines in 20 files at 45db6477 still name it as the work that
    exposes associations over HTTP — reading, writing or counting them — or that narrows a list by
    tag or Bible reference. Evidence (`git grep -n "#318" -- Websites Glory2Him.Core`), under
    `Websites/Glory2Him.WebApp.React/src/`:
    `components/contentItems/contentItemDefaultPanel.tsx` lines 128 and 145;
    `hooks/useContentItemEngagement.ts` lines 18 and 20;
    `models/components/contentItems/contentItemSearchItem.ts` lines 76, 157 and 166;
    `pages/admin/contentItemModerationDetailPage.tsx` lines 640 and 735;
    `pages/admin/contentItemModerationPage.tsx` line 39; `pages/contentItemFeedPages.test.tsx` line
    169; `pages/home.tsx` line 166; `pages/myPostDetail.tsx` lines 38, 142 and 201;
    `pages/postDetail.test.tsx` lines 42, 307, 359, 416, 448 and 471; `pages/postDetail.tsx` lines
    30, 60, 133 and 189; `pages/posts.test.tsx` lines 200 and 215; `pages/posts.tsx` line 151;
    `pages/samplePages/components/contentItemListPanelDoc.tsx` lines 89, 633 and 835;
    `pages/samplePages/components/shared/contentItemShapeSamples.ts` line 42;
    `services/views/contentItems/contentItemFeedScope.ts` line 23;
    `services/views/contentItems/toContentItemSearchItem.test.ts` line 114; and
    `services/views/contentItems/toContentItemSearchItem.ts` lines 77 and 80. Beyond the React app:
    `Glory2Him.Core/Services/Orchestrations/Associations/AssociationOrchestrationService.Writes.cs`
    line 67, `Websites/Glory2Him.WebApp/Infrastructure/CoreRegistration.cs` line 237 and
    `Websites/Glory2Him.WebApp.Tests.Unit/Infrastructure/CoreRegistrationTests.cs` line 87. Each
    sends a reader to a closed issue. Where that work now has a holder, it is recorded in items 21
    and 22, in `UI/Components/ContentItemListPanel.md §10 item 15`, or, for the reaction
    summaries, in #730 (§ARC16.8). Saving a reader's reaction — giving, changing or withdrawing
    it — is #739's (`DesignFeatures/UI/Hooks/ContentItemEngagement.md §2`), whose writes go
    through #728's upsert and #729's withdrawal by pair. Rewriting three of these lines is #738's:
    under its *Constraints* it rewrites `useContentItemEngagement.ts`'s header comment, lines
    14-21, which holds lines 18 and 20, and `toContentItemSearchItem.ts`'s comment at lines 71-83
    to say that the counts arrive through its hook, which rewrites line 80. The task carved from
    this item leaves those three lines to #738, and takes up any of them that still names #318
    once #738 has merged.
24. (#871) **Gap — an editorial association cannot be suggested over HTTP.** Suggesting one, the
    second part of item 21's gap, `POST /api/associations` (section 7; §ARC16.8.1), is held by a
    design task other than item 21's. The route is #728's, which serves a reader's reaction alone
    (user ruling 2026-10-05). Its editorial arm refuses the two-endpoint body until #871 rules
    where an editorial row's `Id` is minted (§APR9.7.1 rule 2), and #871 plans the work.
