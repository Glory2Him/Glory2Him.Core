# Design

How this system is built: layer placement, entity boundaries, event contracts,
the security boundary, storage and migration shape. `INTENT.md` says what the
system is for; the design says how it is put together.

**The design is authoritative.** An issue that disagrees with it is stale intent,
not an instruction — correct the issue.

This file is the index to the design. It holds no design of its own: it says
where each part of the design lives, and the conventions every design document
follows. The planner writes the design. Nobody else does.

---

## How the work breaks down

| Level | What it is | Example | Where it lives |
| --- | --- | --- | --- |
| **Epic** | the whole product | Glory2Him | `INTENT.md`, and the global documents below |
| **Feature** | something that ships and works on its own | Saved searches | a feature document, `DesignFeatures/<Feature>.md` |
| **Sub-feature** | a part of a feature too large to plan in one go, coherent on its own | Search alerts, inside Saved searches | a sub-feature document, `DesignFeatures/<SubFeature>.md`, naming its parent feature |
| **User story** | one component at one level — it can do something, but need not work on its own | the saved-search foundation service; a master page | a user story document, `DesignFeatures/Backend/<Level>/<Story>.md` or `DesignFeatures/UI/<Level>/<Story>.md`, naming its parent feature or sub-feature |
| **Task** | one operation of a user story | `ISavedSearchService.AddSavedSearchAsync` | a GitHub issue, naming its parent user story |

A presentation component's documents live under `Design/UI/Components/` instead, as §UI20.6.4
rules: a root component is a feature, `<Root>.md`, and each of its child components a user
story, `<Root>.<Child>.md`.

A feature is built by several user stories, usually at several levels — a storage
broker, a foundation service, a controller, a page. A user story never spans
levels, so it is always either backend or UI. Its operations are its tasks, and
each task is one GitHub issue carrying its own sign-off checklist.

A feature can span several screens, and each screen is its own user story. A user
portal is one feature: its master page and its detail page are two UI user
stories, and the operations they offer between them — add a user, search for a
user, view, modify and remove one — are their tasks.

**Every level names its parent.** A sub-feature document names its feature and a
user story document names its feature or sub-feature, on a `Parent:` line — its
folder shows its level, not its parent. A task names its user story, on its
`User story:` line. Each parent lists its children in turn.

**Level folders.** Under `Backend/` and `UI/`, each user story sits in the folder its
level has in code, so a story is found where its code is:

| Side | Folders |
| --- | --- |
| `Backend/` | `Brokers`, `Foundations`, `Processings`, `Orchestrations`, `Coordinations`, `Controllers`, `Hubs` for a SignalR hub, the exposer a live connection reaches (§ARC12.12), `Clients`, and `Models` for a story that changes a model alone |
| `UI/` | `Brokers`, `Foundations`, `Views`, `Hooks` |

Any other level takes the name The Standard gives its folder, and a folder is created
with its first user story. This repository documents its presentation components and
its pages elsewhere — under `Design/UI/Components/` and `Design/UI/Pages/` (§UI20.6.4,
§UI20.5.1) — so `UI/` has no `Components` or `Pages` folder, and a citation such as
`UI/Pages/Home.md` can only mean the page document.

**A component that a second feature adds operations to gets a second user story document.** A
user story is one component's work for one feature, and names one parent. So the second
feature's work on a component that already has a user story document goes in a document of its
own, in the same level folder, named `<Component>.<Feature>.md` and naming that feature as its
parent. `Backend/Foundations/ContentItemService.LiveUpdates.md` stands beside the Likes feature's
`Backend/Foundations/ContentItemService.md`, and each feature lists its own.

Writing or changing design documents is work too, tracked as a **design task** — a
`DESIGN:` issue.

---

## Map

### Epic — the rules every feature inherits

| Document | Prefix | Governs |
| --- | --- | --- |
| [G2H Design.md](<../G2H Design.md>) | `IDX` | the design overview and principles, the numbering and citation rules (§IDX1.5), and the map from every pre-split section number to the file it lives in now |
| [Architecture.md](Architecture.md) | `ARC` | the layer model, per-service responsibilities, and the API surface |
| [Security.md](Security.md) | `SEC` | visibility, enforcement posture, authentication and authorisation |
| [Events.md](Events.md) | `EVN` | event naming, addressing, the envelope, and the substrate |
| [Domain.md](Domain.md) | `DOM` | the entity model: content, associations, supporting entities, settings, topic and feed, SEO |

### Areas designed before feature documents

| Document | Prefix | Governs |
| --- | --- | --- |
| [Approval.md](Approval.md) | `APR` | the approval entity, its settings, its lifecycle, and AI content analysis |
| [UI.md](UI.md) | `UI` | the React application's pages, components, navigation and authentication |

These two hold feature-level design in the area layout, because they were written
before features had documents of their own. A new feature gets a feature document.

### Features

Add each feature as its document is written, with its sub-features and user
stories beside it:

| Feature | Sub-features | User stories |
| --- | --- | --- |
| [Likes.md](../DesignFeatures/Likes.md) | none | [Backend/Clients/StorageClient.md](../DesignFeatures/Backend/Clients/StorageClient.md), [Backend/Brokers/StorageBroker.md](../DesignFeatures/Backend/Brokers/StorageBroker.md), [Backend/Brokers/AccessBroker.md](../DesignFeatures/Backend/Brokers/AccessBroker.md), [Backend/Models/EntityTypePersonalisation.md](../DesignFeatures/Backend/Models/EntityTypePersonalisation.md), [Backend/Models/Reaction.md](../DesignFeatures/Backend/Models/Reaction.md), [Backend/Foundations/ContentItemService.md](../DesignFeatures/Backend/Foundations/ContentItemService.md), [Backend/Foundations/ReactionService.md](../DesignFeatures/Backend/Foundations/ReactionService.md), [Backend/Foundations/AssociationService.md](../DesignFeatures/Backend/Foundations/AssociationService.md), [Backend/Orchestrations/AssociationOrchestrationService.md](../DesignFeatures/Backend/Orchestrations/AssociationOrchestrationService.md), [Backend/Orchestrations/ApprovalOrchestrationService.md](../DesignFeatures/Backend/Orchestrations/ApprovalOrchestrationService.md), [Backend/Controllers/AssociationsController.md](../DesignFeatures/Backend/Controllers/AssociationsController.md), [UI/Brokers/AssociationBroker.md](../DesignFeatures/UI/Brokers/AssociationBroker.md), [UI/Foundations/AssociationService.md](../DesignFeatures/UI/Foundations/AssociationService.md), [UI/Brokers/ReactionBroker.md](../DesignFeatures/UI/Brokers/ReactionBroker.md), [UI/Views/ContentItemReactionOption.md](../DesignFeatures/UI/Views/ContentItemReactionOption.md), [UI/Views/ChosenReactionSummary.md](../DesignFeatures/UI/Views/ChosenReactionSummary.md), [UI/Hooks/SignIn.md](../DesignFeatures/UI/Hooks/SignIn.md), [UI/Hooks/ContentItemEngagement.md](../DesignFeatures/UI/Hooks/ContentItemEngagement.md) |
| [UI.md §UI20.8](UI.md), authentication, designed before feature documents | none | [UI/Hooks/SignInReturn.md](../DesignFeatures/UI/Hooks/SignInReturn.md) |
| [LiveUpdates.md](../DesignFeatures/LiveUpdates.md) | none | [Backend/Models/LiveUpdate.md](../DesignFeatures/Backend/Models/LiveUpdate.md), [Backend/Brokers/LiveUpdateBroker.md](../DesignFeatures/Backend/Brokers/LiveUpdateBroker.md), [Backend/Foundations/LiveUpdateService.md](../DesignFeatures/Backend/Foundations/LiveUpdateService.md), [Backend/Foundations/ContentItemService.LiveUpdates.md](../DesignFeatures/Backend/Foundations/ContentItemService.LiveUpdates.md), [Backend/Orchestrations/LiveUpdateOrchestrationService.md](../DesignFeatures/Backend/Orchestrations/LiveUpdateOrchestrationService.md), [Backend/Hubs/LiveUpdatesHub.md](../DesignFeatures/Backend/Hubs/LiveUpdatesHub.md), [UI/Brokers/LiveUpdateBroker.md](../DesignFeatures/UI/Brokers/LiveUpdateBroker.md), [UI/Foundations/LiveUpdateService.md](../DesignFeatures/UI/Foundations/LiveUpdateService.md) |

### Presentation components

The component documents of §UI20.6.4, in a table of their own because the table above lists
product features under `DesignFeatures/`. Each root component is a feature, and its child
components are its user stories.

| Root component (feature) | Child components (user stories) |
| --- | --- |
| [UI/Components/AssociationPanel.md](UI/Components/AssociationPanel.md) | [AssociationPanel.TagAssociationPanel.md](UI/Components/AssociationPanel.TagAssociationPanel.md), [AssociationPanel.BibleReferenceAssociationPanel.md](UI/Components/AssociationPanel.BibleReferenceAssociationPanel.md) |
| [UI/Components/ContentItemListPanel.md](UI/Components/ContentItemListPanel.md) | [ContentItemListPanel.ContentItemSearchBarPanel.md](UI/Components/ContentItemListPanel.ContentItemSearchBarPanel.md), [ContentItemListPanel.ContentItemResultsPanel.md](UI/Components/ContentItemListPanel.ContentItemResultsPanel.md) |
| [UI/Components/ContentItemPanel.md](UI/Components/ContentItemPanel.md) | [ContentItemPanel.Add.md](UI/Components/ContentItemPanel.Add.md), [ContentItemPanel.Restricted.md](UI/Components/ContentItemPanel.Restricted.md), [ContentItemPanel.Edit.md](UI/Components/ContentItemPanel.Edit.md), [ContentItemPanel.Default.md](UI/Components/ContentItemPanel.Default.md), [ContentItemPanel.ContentItemQuotesPanel.md](UI/Components/ContentItemPanel.ContentItemQuotesPanel.md), [ContentItemPanel.ContentItemVerseImagePanel.md](UI/Components/ContentItemPanel.ContentItemVerseImagePanel.md) |
| [UI/Components/ContentItemSettingsPanel.md](UI/Components/ContentItemSettingsPanel.md) | none |
| [UI/Components/ReviewCommentPanel.md](UI/Components/ReviewCommentPanel.md) | none |
| [UI/Components/ReviewPanel.md](UI/Components/ReviewPanel.md) | none |
| [UI/Components/SharingPanel.md](UI/Components/SharingPanel.md) | none |

### Page documents

The page documents of §UI20.5.1, one per product page that renders the presentation components
above: its layout, the components it renders, and where each of their hooks leads. The sample
pages under `/SamplePages` demonstrate the components and have none, and neither has the ported
template's blog — `/Post-Single`, `/Author`, `/Categories`, `/Tag`, `/Post-Grid`, `/Post-List`,
`/Post-Grid-Masonry-Filter`, the demo `/Search` and the blog's own `/Search-Result` — which is
sample material counted among them, each page of it to move under `/SamplePages` (§UI20.5.1).

| Page document | Route | Section |
| --- | --- | --- |
| [UI/Pages/Home.md](UI/Pages/Home.md) | `/` | user |
| [UI/Pages/Posts.md](UI/Pages/Posts.md) | `/posts` | user |
| [UI/Pages/PostDetail.md](UI/Pages/PostDetail.md) | `/posts/{contentItemId}` | user |
| [UI/Pages/Contribute.md](UI/Pages/Contribute.md) | `/posts/contribute` | user |
| [UI/Pages/BibleReference.md](UI/Pages/BibleReference.md) | `/BibleReferences`, `/BibleReferences/{reference}` | user |
| [UI/Pages/MyPosts.md](UI/Pages/MyPosts.md) | `/myposts` | user |
| [UI/Pages/MyPostDetail.md](UI/Pages/MyPostDetail.md) | `/myposts/{contentItemId}` | user |
| [UI/Pages/ContentItemModerationPage.md](UI/Pages/ContentItemModerationPage.md) | `/Admin/Posts` | admin (moderation) |
| [UI/Pages/ContentItemModerationDetailPage.md](UI/Pages/ContentItemModerationDetailPage.md) | `/Admin/Posts/{contentItemId}` | admin (moderation) |

---

## Conventions

**Epic, feature or user story — decide before writing.** A rule every feature must
follow — how a service implements eventing, how identity travels, how a user
authenticates — belongs to the epic, in the global document that owns its area.
What one feature does belongs in its feature document, and what one component does
in its user story document. A global document stays global: the screens a security
rule needs, such as sign-in or 2FA, are UI user stories that cite `Security.md`,
not UI written into it.

**Inherit, never duplicate.** Feature and user story documents inherit every global
rule, and a user story inherits its feature's business rules. They cite the rule —
`per §EVN2`, `per SavedSearches.md rule 3` — and never restate it: a copy is a
second statement that will drift from the first.

**Record every deviation where it happens.** A feature or user story that must
depart from a global rule says so in its document's **Deviations** section: the
rule it departs from, the reason, and how it is done instead. A service's own
deviation, such as a count over two-to-three, goes in its user story document's,
as §ARC12.5 says. The planner proposes
a deviation and the user's approval grants it. Another document's deviation is
never the reason for one. A service designed before feature documents has no such
section, so its approved two-to-three deviation is recorded in §ARC12.5's register
instead. That register holds two-to-three deviations only.

**Numbering and citation.** A feature document numbers its business rules, and a
user story document numbers its operation sections. Number rules within a section
too, so a citation can say which one it means. §IDX1.5 gives the citation form
for every design document, feature and user story documents included:

```csharp
// design §EVN2 rule 5: a service publishes a fact only about its own unit of work
// design SavedSearches.md rule 3
// design Backend/Foundations/SavedSearchService.md §1
```

**Every operation section in a user story document carries exactly one tag,
never bare:**

<!-- Indented so that a line-based reader, such as the `(needs issue)` sweep,
     does not take these two example lines for headings. Do not dedent. -->
```markdown
  ## 1. AddSavedSearchAsync (#512)
  ## 2. RemoveSavedSearchByIdAsync (needs issue)
```

`(#N)` names the task — the GitHub issue — that delivers the operation.
`(needs issue)` is an explicit, greppable flag for an operation nobody has
scheduled yet, and is what the planner's sweep mode looks for:

```bash
grep -rnE --include=*.md "^#{2,3} .*\(needs issue\)" Documentation/DesignFeatures
```

The tag is mandatory rather than inferred, because a bare heading is ambiguous —
deliberately skipped, or just missed? Requiring the tag forces the decision every
time an operation is touched.

**A component document tags its gaps, not operations.** A presentation
component's document (§UI20.6.4) has no operation sections. What it records that
needs a task — a behaviour gap, a stale sample page or code comment, a test to
rename — is an item of the numbered list in its section 10, and carries the tag
straight after the item's number, before anything else on the line:

<!-- Indented so that a line-based reader, such as the sweep below, does not take
     these two example lines for tagged items. Do not dedent. -->
```markdown
  3. (needs issue) **Berean's re-request control is ungated.** …
  4. (#512) **The doc page needs updating.** …
```

`(needs issue)` becomes `(#N)` once the task exists, as an operation's tag does. A
question for the user carries no tag: it becomes work, and takes one, only once it
is answered. A page document (§UI20.5.1) tags the items of its section 6 the same
way, and so does `UI.md` its gaps — the list of building-block gaps under §UI20.6.4, the
ported blog's under §UI20.5.1, and the account pages' under §UI20.8.1. One sweep
covers the component documents, the page documents and `UI.md`:

```bash
grep -rnE --include=*.md "^[0-9]+\. \(needs issue\)" Documentation/Design/UI/Components Documentation/Design/UI/Pages Documentation/Design/UI.md
```

**A gap that a design task holds carries that task's number** (user ruling 2026-09-27). Where a
`DESIGN:` issue already holds a gap — its design settles the work, and the tasks that build it are
carved only after it — the item carries the design issue's number instead of `(needs issue)`, so
the sweep above, which finds only `(needs issue)`, does not carve a task that duplicates it. For
example: `(#698)`, *Redesign The Post Moderation Page For Association Approvals*, on
`UI/Pages/ContentItemModerationDetailPage.md §6 items 9, 10, 12 and 17`; `(#700)`, *Design The
Bible Reference Page*, on `UI/Pages/BibleReference.md §6 items 1–4, 6 and 7` and on the
unreadable-reference gap of five other page documents (`UI/Pages/Home.md §6 item 14` and the items
it names); and `(#701)`, *Record Who Amended An Item's Content*, on
`UI/Components/ReviewPanel.md §10 item 10`. When the design issue closes, its designed work gets
tasks, and anything it leaves unbuilt goes back to `(needs issue)`, so the sweep finds it again.
`(#702)`, *Push Live Updates To Open Pages*, held the live-update gap of seven page documents until
its design carved the task that builds it, which those items now carry
(`UI/Pages/Home.md §6 item 11` and the items it names).

**Relocations.** §IDX1.5 rules how a relocated section is annotated, so that a
citation of its old number still resolves by grep, and where a retired number
may still stand. Nothing in CI validates
citations. `Tools/design-split-audit.sh` checks their form (gate G4) when someone
runs it by hand; otherwise the annotation convention is the whole guarantee.

**Keep the map current.** A document this index does not list is one nobody
finds.
