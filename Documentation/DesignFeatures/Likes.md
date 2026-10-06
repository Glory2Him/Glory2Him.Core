# Likes
Epic: [INTENT.md](../../INTENT.md)
Inherits: §DOM4.2, §DOM4.4–§DOM4.6, §DOM4.10, §DOM5.2, §DOM6.4, §DOM6.10, §SEC14.1, §SEC14.3, §SEC14.5, §SEC14.6, §SEC14.7 posture A′, §SEC18.6, §APR7.5.1, §APR8.4, §APR8.8, §APR9.7.4, §APR9.8, §APR9.9, §ARC12.2.1, §ARC12.3.1, §ARC16.2.1, §ARC16.2.2, §ARC16.8, §ARC16.8.1, §ARC17.4, §EVN2, §EVN13, §UI20.6.6
Mockups: none. The card's Like control is built and documented (`UI/Components/ContentItemPanel.md rule 3.1.8`, `UI/Components/ContentItemPanel.Default.md rule 3.1.7`); what it lacks is the write behind it and the counts it shows.
Design task: #706

A reader's *like* is their reaction to a content item. The card's **Like** control offers the approved reaction vocabulary (§DOM5.2) — seeded as Amen, Love, Joy, Moved and Praying (`ReactionSeedData.cs`) — and the reaction the reader chooses is theirs, recorded as a personal association between the item and the reaction (§DOM4.10). *Like* is the control's name, not a reaction's.

## Problem

When this feature was designed (#706), every card on the site offered Like, and nothing stood behind it. A chosen reaction lived in page state for the visit and was gone on reload (`src/hooks/useContentItemEngagement.ts`), no card showed the reactions an item had actually been given (`toContentItemSearchItem.ts` left `reactionSummary` unset), and no route recorded, changed or withdrew a reaction. A reader could not say how a post moved them, and nobody could see that it did. This is the state the feature set out from, and it is not kept current as tasks merge. What of the write surface (§ARC16.8.1) and the summary read (§ARC16.8) is built is recorded in §ARC12.5 entry 1.

## Business rules

1. **A signed-in reader gives a reaction by choosing it** on the card's Like control, and then holds it (§DOM4.10 rule 6, §ARC16.8.1).
2. **Choosing a different reaction changes the one they hold.** A reader never holds two on one item (§DOM4.10 rule 6); the change moves their one row (§DOM4.5 rule 4's exception).
3. **Choosing the reaction they already hold withdraws it.** A second press of the same choice is a change of mind, not a second reaction (code: `useContentItemEngagement.ts` — "The same choice again is a change of mind — withdrawn, not doubled"; test: `postDetail.test.tsx` — "should withdraw the reaction when the reader chooses it again"). Giving or changing is a `POST`, withdrawing a `DELETE` (`UI/Components/ContentItemPanel.md §5`, the Like row).
4. **A signed-out reader who chooses a reaction is sent to sign in**, and returns to exactly where they were; a reader whose sign-in state has not been read back yet is not sent (§UI20.6.6 rule 2, `UI/Components/ContentItemPanel.md rule 3.2.4`).
5. **Every reader sees each item's reaction counts**, signed in or not, and the same counts whoever asks. Only approved reactions count, a reaction nobody gave does not appear, and the total is the client's sum (§ARC16.8).
5a. **Reactions appear in the vocabulary's order**: the card lists its counts, and the Like control offers its choices, by `Reaction.SortOrder`, lower first, with `Name` breaking a tie (§DOM5.2; §ARC16.8, *The projection*). The owner ruled it on 2026-09-28: *"They should use the sort order of the reaction"*. `Reaction` had no sort order, so this feature adds one, and the seeded five keep their seeded order: Amen, Love, Joy, Moved, Praying.
6. **A signed-in reader sees their own reaction marked**, even while it waits on review — the glyph pressed and the count unmoved (§ARC16.8, *What a reader sees when their own reaction is not yet `Approved`*).
7. **The item's winning setting governs reactions.** A reaction is given, changed or revived only where the setting allows reactions, and only Love where it limits reactions to Love; withdrawing is never gated by it (§ARC16.2.1). The card offers only what the setting allows (`UI/Components/ContentItemPanel.md rule 3.1.8`), and an item whose setting hides reactions shows no counts (§ARC16.8, the rule 6 row of *Which rows are counted*).
8. **A reader's own reaction is not a contribution.** Giving, changing and withdrawing it asks none of the read-only roles, and the lock after review does not reach it (§SEC14.7 posture A′ rule 1, §APR9.9 rule 6).
9. **A changed reaction goes back through the approval process**, in a round that starts with no reviews. Its comments and its outstanding review requests stay as they are, as the generic process leaves them (§APR9.7.4, *What else starts empty*, ruled by the owner on 2026-09-28). Under the seeded personal tier the round is approved in the same act (§DOM4.5 rule 4, §APR9.7.4).
10. **A withdrawn reaction given again comes back at the status it was withdrawn at; a reaction a moderator took down is never revived**, and the attempt looks to the reader like a reaction already pending (§DOM4.10 rules 7 and 8).
11. **The Like control works on every page that renders the card with it**: `/`, `/posts`, `/posts/{id}`, `/myposts`, `/myposts/{id}`, `/Admin/Posts` and `/Admin/Posts/{id}` (user, 2026-09-28: *"The UI work includes all pages that has this ContentItemPanel"*). `/posts/contribute` renders the card's add face only, which has no Like (`UI/Components/ContentItemPanel.md §1`). The sample pages under `/SamplePages` are excluded (user, 2026-09-28: *"exclude sample pages, they are just mockups"*). The Bible reference page's reaction bar is not the card, and is held for #700 (`UI/Pages/BibleReference.md §6 item 3`).
11a. **Like is offered on an item that is not public yet**, on the setting alone, as on any other item (the owner, 2026-09-28: *"Yes, I don't mind it being available there"*). By design only `/myposts`, `/myposts/{id}`, `/Admin/Posts` and `/Admin/Posts/{id}` show such an item (*"General users will NEVER see things that are not approved so they have zero scope to see this"*). `/posts/{id}` shows one today only through a gap its own document records (`UI/Pages/PostDetail.md rule 2.1`, §6 item 12). The reaction is recorded, because the write is bound to an endpoint the caller may see (§ARC12.3.1 rule 5a, as §SEC14.3 recalls it). Neither its count nor the reader's own mark shows on that card until the item is public, because the summary answers only for publicly visible items (§ARC16.8, *Anonymity*), so the reader sees their press take effect and then disappear when the read that follows their write lands.
12. (#702) **Counts update live.** A page already showing an item comes to show another reader's reaction without a reload (user, 2026-09-28: *"We will need to consider #702 as well since this mechanism will be required to update like counts in real time on the UI"*). The mechanism is #702's to design (§ARC12.5.2 business rule 12), and its tasks are carved from that design. Until it is built, a reaction moves the counts on the reacting reader's own page only (§ARC16.8, *Where each shared rule lives*).

## User stories

The feature's own user stories, bottom up. Each names this document as its parent.

| User story | Level |
| --- | --- |
| [Backend/Clients/StorageClient.md](Backend/Clients/StorageClient.md) | client — `IEFCoreClient` |
| [Backend/Brokers/StorageBroker.md](Backend/Brokers/StorageBroker.md) | broker — `IStorageBroker` |
| [Backend/Brokers/AccessBroker.md](Backend/Brokers/AccessBroker.md) | broker — `IAccessBroker` |
| [Backend/Models/EntityTypePersonalisation.md](Backend/Models/EntityTypePersonalisation.md) | model — the personal-type lookup |
| [Backend/Models/Reaction.md](Backend/Models/Reaction.md) | model — `Reaction.SortOrder`, its column, migration and seed |
| [Backend/Foundations/ContentItemService.md](Backend/Foundations/ContentItemService.md) | foundation — `IContentItemService` |
| [Backend/Foundations/ReactionService.md](Backend/Foundations/ReactionService.md) | foundation — `IReactionService` |
| [Backend/Foundations/AssociationService.md](Backend/Foundations/AssociationService.md) | foundation — `IAssociationService` |
| [Backend/Orchestrations/AssociationOrchestrationService.md](Backend/Orchestrations/AssociationOrchestrationService.md) | orchestration — `IAssociationOrchestrationService` |
| [Backend/Orchestrations/ApprovalOrchestrationService.md](Backend/Orchestrations/ApprovalOrchestrationService.md) | orchestration — `IApprovalOrchestrationService` |
| [Backend/Controllers/AssociationsController.md](Backend/Controllers/AssociationsController.md) | exposer — `AssociationsController` |
| [UI/Brokers/AssociationBroker.md](UI/Brokers/AssociationBroker.md) | broker — `AssociationBroker` |
| [UI/Foundations/AssociationService.md](UI/Foundations/AssociationService.md) | foundation service — `associationService` |
| [UI/Brokers/ReactionBroker.md](UI/Brokers/ReactionBroker.md) | broker — `ReactionBroker` |
| [UI/Views/ContentItemReactionOption.md](UI/Views/ContentItemReactionOption.md) | view — `toContentItemReactionOption` |
| [UI/Views/ChosenReactionSummary.md](UI/Views/ChosenReactionSummary.md) | view — `toChosenReactionSummary` |
| [UI/Hooks/SignIn.md](UI/Hooks/SignIn.md) | hook — `useSignIn` |
| [UI/Hooks/ContentItemEngagement.md](UI/Hooks/ContentItemEngagement.md) | hook — `useContentItemEngagement` |

The rest of the UI is already designed in the documents the presentation components and pages own (§UI20.6.4, §UI20.5.1), and this feature's tasks are carved from their gaps:

| Document and gap | What it is |
| --- | --- |
| `UI/Pages/Home.md §6 item 2`, `UI/Pages/Posts.md §6 item 2`, `UI/Pages/PostDetail.md §6 item 2`, `UI/Pages/MyPosts.md §6 item 2`, `UI/Pages/MyPostDetail.md §6 item 4`, `UI/Pages/ContentItemModerationPage.md §6 item 2`, `UI/Pages/ContentItemModerationDetailPage.md §6 item 3` | each page's share: its cards' counts and the reader's own reaction read from the server, and the reaction recorded and withdrawn |
| `UI/Components/ContentItemPanel.md §10 item 12` | the card stops sending a signed-out reader to sign in itself |

**Why the write and the read sit on the orchestration** is §ARC16.2.1's, §ARC16.8's and §ARC16.8.1's, and is not restated: the write derives authorization inputs from two endpoints and runs the facet gate, and the read composes §SEC14.3's rules 3, 4 and 6 across `Association`, `Reaction`, `ContentItem` and `ContentItemSetting`.

## Entity count, events and storage

**Entity count.** The give, change and withdraw flow spans **three** entities — `Association`, `ContentItem` and `Reaction` — plus the `ContentItemSetting` the facet gate asks through `IAccessBroker`, a broker (§ARC16.2.1). The summary read spans **three** too — `Association`, `Reaction` and `ContentItem` — plus the `ContentItemSetting` it asks for §SEC14.3 rule 6 through the same `IAccessBroker` arm (§ARC16.8). Both flows count `ContentItemSetting` the same way, outside: an entity's settings gathered through `IAccessBroker` leave a service's entity count where it was (§APR8.6.1 rule 3). Both are `AssociationOrchestrationService`'s, which already holds seven entity services and is recorded as breaking the dependency-count guidance (§ARC12.5 entry 1's provisional note). The owner approved a named deviation for that count on 2026-09-28 (`Backend/Orchestrations/AssociationOrchestrationService.md`, *Deviations*). **This feature adds no service dependency to it**: `IAccessBroker` is a broker, outside the Florance count (§ARC12.5, *an orchestration holds brokers*). The approval change spans `Approval`, `ApprovalReview` and the association, and adds one subscription to `ApprovalOrchestrationService`, not a dependency (§ARC16.2.2).

**Events.** Two new facts, both on `AssociationService`, and one new subscription (§ARC16.2.2):

| Address | Published by | Heard by |
| --- | --- | --- |
| `Association-Added` *(exists)* | the upsert's create arm, and the add | `ApprovalOrchestrationService` (exists) |
| `Association-Restored` *(new)* | the upsert's revive to the same reaction | nobody, deliberately (§ARC16.2.2) |
| `Association-Repointed` *(new)* | the upsert's repoint, live or revived | `ApprovalOrchestrationService.OnAssociationRepointedAsync` *(new)* |
| `Association-Removed` *(exists)* | the foundation's soft delete the withdrawal ends in | nobody (§APR9.7.6) |

`Association-Upserting` is **not minted** (§ARC16.2.2), so the upsert has no event path. `Association-Adding` keeps its address and its binding (#631), and refuses a reader's reaction (`Backend/Orchestrations/AssociationOrchestrationService.md §2`).

**Storage and migration.** **One migration**, for rule 5a: it adds `Reactions.SortOrder` and backfills the five seeded reactions (`Backend/Models/Reaction.md §1`). Nothing else needs one. The one-live-row-per-reader guarantee is `UX_Associations_PersonalPair`, built by #627 (`20260923220407_SplitAssociationPairIndexIntoEditorialAndPersonal`, §DOM4.6 rule 2). **One seed change**, for the same rule: the vocabulary's seed writes each reaction's sort order (`ReactionSeedData.cs`). The `(Association, IsPersonal = true)` approval tier that closes a reaction's round on submission is seeded already (`ApprovalSettingSeedData.cs:202-211`). No role is added.

**The storage client gains one terminal shape** of the three §ARC12.2.1 rule 3 names — the matching rows, optionally projected — and the storage broker a pass-through per entity this feature reads, so every new read here asks its question through a query-shaping function its caller authors and the client awaits (§ARC12.2.1 rules 1–3). The other two shapes, and converting the reads that exist, are §ARC12.2.1's own work and not this feature's.

## Risks

**Reversible.** Everything here is code but one column, `Reactions.SortOrder`, whose migration's `Down` drops it; there is no new index and no new constraint. `AddAssociationAsync` is renamed `UpsertAssociationAsync` on the orchestration (§ARC16.8.1), and nothing outside the solution calls it — no controller serves it yet.

**Not reversible.** The two new event names, `Association-Restored` and `Association-Repointed`, and their stable identifiers: an event name sits inside the envelope's signature, and an identifier never changes once deployed (`EventBrokerIdentifiers.cs`). The new `AssociationSuggestionStatus` member crosses the wire as a number, so it is appended and never renumbered.

**Failure midway.** The upsert writes one row and then publishes one fact, as every foundation write does today. A publish that fails after the write leaves the row changed with no fact heard — for a repoint, a change the approval ear never re-tests. That is §EVN19's write-and-publish atomicity, ruled and not built, and this feature neither widens nor closes it. Two concurrent first reactions by one reader on one item both find no row; the personal index refuses the second insert, which reaches the reader as a conflict rather than a second row (§DOM4.6 rule 2).

**Exposure before the ear.** A repoint reachable before `Association-Repointed` is heard would skip re-review under any tier that requires it — the hazard §DOM4.5 rule 4 names. The route that makes a repoint reachable is therefore built after the ear, and the tasks' order says so.

## Out of scope

- **Live counts on other readers' pages** — rule 12, held for #702.
- **The Bible reference page's reaction bar** — held for #700; it needs `BibleReferenceSetting` (§DOM6.9), which is not built.
- **Save, Share and Comments** — §UI20.6.6 rule 4 plans each end to end after Likes.
- **The association routes this feature does not need**: the collection read, the read by id, modify, the remove by id and the hard remove (§ARC17.4). The two reads must not ship before §SEC14.7 posture A′ rule 7 is built, which keeps a personal row from every caller but its owner and the review tier (§ARC17.4, *The reads*); this feature serves neither and builds neither.
- **The orchestration's own remove by id moving its global block below the read** (§SEC14.7 posture A′ rule 4). The withdrawal here ends in the foundation's remove by id, and only the foundation's gate changes.
- **§SEC14.3 rule 6 inside the collection read's evaluator.** That vacancy stays (§ARC16.8's shared-rule table); the summary read answers rule 6 through the same `IAccessBroker` arm the facet gate uses, for its `ContentItem` host.
- **The set-scope pre-check's personal key**, and whether set-scope may change a personal row at all (§DOM4.6, open).
- **`ApprovalId` on the association row** (§SEC14.7 posture A rule 5, #699, not built). When it is built, the upsert's arms take it from the stored row as that rule says.
- **Converting the storage broker's existing reads** to query-shaping functions (§ARC12.2.1), and the client's other two terminal shapes.
- **A surface for setting a reaction's sort order.** No page manages the vocabulary. `POST` and `PUT api/Reactions` carry `SortOrder` as they carry `Name`, and an approved reaction's order is as fixed as its name (§ARC12.3.1 shared rule 9).
- **Registering `AssociationOrchestrationService` correctly in Core's own registration helper** (§ARC12.5, the singleton note). The host registers it scoped, which is the registration this feature runs under.

## Open questions

None. The first draft carried three, and the owner ruled on all three on 2026-09-28. Each is folded in: what a changed reaction's round keeps (rule 9), the order of the reactions (rule 5a), and Like on an item that is not public yet (rule 11a).

## Deviations

None at the feature level. The controller's response codes depart from two rules of `the-standard-exposers`; `Backend/Controllers/AssociationsController.md` records both, and the owner approved both on 2026-09-28. `Backend/Orchestrations/AssociationOrchestrationService.md` records two more, both approved the same day: the service's dependency count, and the summary's viewer members for a reaction outside the public vocabulary.
