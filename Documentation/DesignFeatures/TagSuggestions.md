# Tag suggestions
Epic: [INTENT.md](../../INTENT.md)
Inherits: §APR9.2, §APR9.7.1 rules 1 and 2, §ARC12.1 rules 1–3, §ARC12.2.1 rules 1, 3, 5 and 6, §ARC12.3.1 rules 2a, 5 and 5a, §ARC12.5 (*an orchestration holds brokers*, and the two-to-three rule), §ARC12.5.2 rule 6 (`Association-Adding`'s write flow), §ARC16.2.1, §ARC16.2.2, §ARC16.8.1, §ARC17.2 (shape versus served literal), §ARC17.4, §DOM4.4 rule 4, §DOM4.5, §DOM4.6, §EVN13 rule 3, §EVN18 rule 1, §SEC14.3, §SEC14.5, §SEC14.6, §SEC14.7 posture A rules 1 and 4 and posture A′ rule 1, RejectedEndpoints.md rules 1, 10 and 12
Mockups: none. The *Suggest a tag* box is built (`Design/UI/Components/AssociationPanel.TagAssociationPanel.md`); this feature writes what it sends.
Design task: #1356

A reader types a tag into a post's *Suggest a tag* box. This feature sends it once, by its name, and the server finds the tag the name names, or adds it, and pairs it with the post unless that pairing already exists. A rejected tag refuses the suggestion instead. What becomes of the new rows afterwards is the approval process's.

## Problem

When this feature was designed (2026-10-28, `main` at `4a839256`), the pages answered a suggested tag with a *coming soon* notice (`UI/Pages/PostDetail.md §6 item 17`, `UI/Pages/MyPostDetail.md §6 item 18`). The tag panel raises `onAdd` with the text the reader typed (`Design/UI/Components/AssociationPanel.md` rule 3.1.15), and the only write that pairs a tag with a post, `POST /api/associations`, takes two endpoint ids (§ARC16.8.1). Nothing turned a name into a tag: `ITagService` had no read by name, its add took its `Id` from its caller, and every tag read is filtered by who asks, so a read the caller made could not say whether a name was already taken. And a name is held by at most one row, whatever that row's state (`IX_Tags_Name`, unique and unfiltered; §ARC12.3.1 rule 2a), so a name the caller cannot see the tag of could not be added either.

## The rulings

The product owner stated the requirement under #1352, before it was split:

> This is my expectation for both the tag and the bible reference components:
> 1. I submit a tag or a bible reference
> 2. this gets submitted via an api
> 3. the api does a read or add on the tag OR bible reference to get its Id
> 4. the tag / bible reference Id is then used to create an association if none exist already.
>
> The above is the submission process. The approval proces is separate. Both the tag / bible reference and the association will be subject to approval. That is separate to the submission process.

The rest of this section is the owner's rulings, word for word, each question as it was put where there was one. Each was recorded on #1352 or #1356 when it was made.

**Where the submission lives, and what it refuses** (2026-10-26):

> It will be good if we can extend the association controller to accommodate two endpoints that can cater for tag and bible reference. The association orchestration can facilitate the process of reading/creating the tag or bible reference and then creating the association.
>
> Duplicate associations should not be allowed.
> If a tag or bible reference was rejected, then the association should not be allowed. I new tag OR bible reference MUST NOT be created since we have already ruled that the one submitted is unfit.

Of the two endpoints, this feature is the tag's. The Bible reference's is #1357's.

**Both deviations are signed off** (2026-10-26):

> Q: The design will put the submission on AssociationOrchestrationService, a seventh-dependency exception of its own, and the two new routes will return a status instead of the created row. Do you sign off both, each recorded in this feature's own document?
>
> A: "Sign off both" — the option read: "The planner records both as approved by you, with today's date."

The question's *"seventh-dependency exception"* means the service's existing seven entity services: the submission adds no eighth. Each is recorded in the user story document of the component it is a deviation of, as `Design/design.md` has a service's deviation recorded (*Record every deviation where it happens*): `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md` and `Backend/Controllers/AssociationsController.TagSuggestions.md`, *Deviations*.

**The routes answer `200` where the pair already existed** (2026-10-27):

> Q: The two new routes depart from the exposer rule that a POST returns 201 with the created row. You signed off returning a status instead of the row. Do you also sign off answering 200 where nothing was created, for example when the association already exists?
>
> A: "Yes, sign off 200" — the option read: "201 when something was created, 200 when the pair already existed. Both recorded as approved by you."

**A tag the reader cannot see** (2026-10-27):

> Q: A reader types a tag name that already belongs to a tag they can't see: another reader's Draft or Submitted tag, or an Approved tag that is unpublished or scheduled. Names are unique, so no new tag can be added. What happens?
>
> A: "Pair it (Recommended)" — the option read: "Pair the post with that tag and answer as if the reader had created it. The pairing shows once the tag is approved and published. The tag's creator can still rename or remove it."

**A soft-deleted tag** (2026-10-27), in the owner's own words, chosen over both options put:

> Q: A reader types a tag name held by a soft-deleted tag. The name stays reserved (the index ignores deletion), and a deleted tag can't be paired. What happens?
>
> A: "If a tag was soft deleted, it can be undeleted and will be subject to approval.  I think this block should only be on items that has been marked rejected.  Any new associations can be linked to this existing item.
>
> If if a tag or bible reference has been marked as rejected, it will block creation of a duplicate tag / bible reference.  It MUST also block the creation of any associations on a rejected item."

**Who triggers the undelete** (2026-10-27):

> Q: A submission names a soft-deleted tag and you ruled it is undeleted and goes back through approval. Today only administrators and publishers may recover a soft-deleted item (#1326, ruled 2026-10-22). Who triggers this undelete, and from what?
>
> A: "Any deletion" — the option read: "The submission undeletes it on the reader's behalf, whoever deleted it (creator or moderator takedown), and it goes back to approval."

The undelete itself, and which routes reach it, are #1362's to design (#1362, *How far it reaches*, *Which endpoint types*, and its open questions 1 and 2, answered 2026-10-28). This feature reaches it for the soft-deleted tag its read by name finds (rule 4).

**A pairing that was itself rejected** (2026-10-27), in the owner's own words:

> Q: The pairing of this post with this tag was itself rejected before (the tag is fine). A reader submits it again. What happens?
>
> A: "Since this will lead to a duplicate, the db will not allow its creation. In terms of the UI we can just thank the user for the contribution with stating that this association has already been  suggested by another user and is under review."

**The reader's own pending pairing** (2026-10-27):

> Q: A reader resubmits a pairing they themselves suggested, which is still pending. "Already suggested by another user" would be untrue. What should the panel say?
>
> A: "Say it's theirs" — the option read: "Thank them and say they already suggested it and it is under review. Others still see the 'another user' wording."

**What a rejected item is** (2026-10-27), recorded on #1352 in the owner's own words: *"A rejected item is an item where its approval status is REJECTED"*. So a rejected post refuses the suggestion too (RejectedEndpoints.md rule 1).

## Business rules

1. **A reader suggests a tag for a post by its name, in one request** (*The requirement*). The page sends the post's id and the name, once for each name the panel raises. The server finds the tag the name names, or adds one, and pairs it with the post unless the pairing already exists. The submission is the whole of this feature: the tag's approval and the pairing's are the approval process's, and separate (*The requirement*; §APR9.7).
2. **What names a tag.** The submission decides it before anything is read:
   - **White space around the name is trimmed by the server**, and a name that is empty once trimmed is refused. White space inside the name is kept as it was typed.
   - **The server strips no hash.** The panel strips a leading hash before it raises `onAdd` (`Design/UI/Components/AssociationPanel.TagAssociationPanel.md` rule 2.3). A hash that reaches the server is part of the name, as it is on `POST /api/Tags`.
   - **Letter case is the column's to decide.** `Tag.Name` has no collation of its own, so it takes the catalogue's default, `SQL_Latin1_General_CP1_CI_AS` on every catalogue in use today (§EVN25, *What was found*), which ignores letter case and respects accents. So two names that differ only in letter case name one tag, and `grace` finds `Grace`, while `café` and `cafe` are two names. The tag keeps the name it was created with: the submission never renames a tag.
   - **A name is at most 30 characters once trimmed**, the cap `Tag.Name` has (`TagService.Validations.cs`). A longer name is refused before anything is read.
3. **Which tag the name names is decided by a read no identity filters.** The read by name (`Backend/Foundations/TagService.TagSuggestions.md §3`) finds the one row a name is held by, whatever its approval status, whoever owns it, and whether or not it is soft-deleted, and answers its id, its status and whether it is deleted, and nothing else. An identity-filtered read never decides whether a name is taken (root `CLAUDE.md`, *Non-negotiables*): the tag reads answer a row the caller may not see as not found (§SEC14.7 posture A rule 4; §SEC14.5 rules 1 and 3), and an add over a taken name would be refused by `IX_Tags_Name`. The foundation authors the read's condition, as a query-shaping function, and the storage broker hands it to the storage client (§ARC12.2.1 rules 1 and 3).
4. **What the submission does with the tag the name names:**

   | The name is held by | The submission | Ruling |
   | --- | --- | --- |
   | no tag | adds a tag, at `Submitted` (rule 14), and pairs it | *The requirement*, step 3 |
   | a public tag — `Approved`, published, its publish date absent or past | pairs it | *The requirement*, step 4 |
   | the caller's own `Draft` or `Submitted` tag | pairs it | *The requirement*, step 4 |
   | a tag the caller cannot see — another reader's `Draft` or `Submitted` tag, or an `Approved` tag that is unpublished or scheduled | pairs it, and answers as if the caller had created it (rule 5) | *A tag the reader cannot see* |
   | a `Rejected` tag, whoever's, soft-deleted or not | refuses: no pairing, and no new tag | *Where the submission lives* (2026-10-26); *A soft-deleted tag*, its second paragraph |
   | a soft-deleted tag that is not `Rejected` | refuses, as a rejected one, until #1362's undelete replaces the refusal on this route; then undeletes it on the caller's behalf, whoever deleted it, and pairs it, the tag going back to approval | *A soft-deleted tag*; *Who triggers the undelete*; §ARC12.3.1 rule 5 |

   §ARC12.3.1 rule 5 bars a soft-deleted endpoint from a new pairing, and stands as written until #1362's design replaces it (rule 5's own note). So the first build refuses a soft-deleted tag. #1362's design adds the section that replaces that refusal with its undelete on this route, beside `Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`, and carves its task; the read, the operation that writes the undelete, the fact it publishes and how the round starts again are #1362's (#1362, *What the design must settle*, points 3–5). This feature hands it the row its read by name found, at the place in the flow where a found tag is paired (rule 9, step 7).
5. **A tag the caller cannot see is paired, and nothing in the answer tells it from a tag the caller created** (*A tag the reader cannot see*). The answer carries a status and nothing else (rule 7): no tag id, and no association id either, since an id the caller holds can be read back, and a read answers not found where the tag is hidden (§SEC14.7 posture A rule 4; §SEC14.3 rules 3 and 4). Pairing a tag the caller cannot see departs from §ARC12.3.1 rule 5a's visibility precondition, on this route alone (*Deviations* 1). What the creator of that tag keeps, by the same ruling:
   - **A rename** moves the pairing with the tag: the pairing is made by the tag's id, never its name, so it shows the new name once the tag and the pairing are approved and published. §ARC12.3.1 rule 2a's rename question stays open.
   - **A removal** soft-deletes the tag. Every pairing made through it is hidden while it is deleted (§SEC14.3 rule 3), and its name stays reserved (§ARC12.3.1 rule 2a), so the next suggestion of the name meets the soft-deleted row of rule 4.
6. **No pairing is made twice** (*Where the submission lives*: *"Duplicate associations should not be allowed."*). The pairing is the upsert's editorial arm as it stands (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 3 and §6): the post and the tag are resolved into a pair the way the upsert resolves one (§DOM4.5), the exact-pair probe asks whether a row holds that pair and, if none does, the overlap probe asks whether a differently scoped live row covers it, and only a pair neither finds is inserted, at `Submitted` (§ARC16.8.1, #872). An occupied pair is never written again, whatever holds it: a live row at any status, a row `Rejected` included (*A pairing that was itself rejected*); a soft-deleted row, until #1326's design settles what an editorial add answers there (#1326, deliverable 2), the later of the two designs rebasing on the earlier's words; and a differently scoped live row that covers the pair.
7. **The submission answers a status and nothing else**, as §ARC16.8.1's result does, without its id (rule 5):

   | Status | When | Code |
   | --- | --- | --- |
   | `Created` | a pairing was inserted, whether the tag was found, added or undeleted | `201` |
   | `AlreadySuggestedByCaller` | the exact pair is held by a live row the caller created, at `Draft` or `Submitted` (*The reader's own pending pairing*) | `200` |
   | `AlreadySuggested` | the pair is held, or covered, by any other row: another reader's, at any status; an `Approved` row; a `Rejected` row; a soft-deleted row; a differently scoped live row that covers it | `200` |

   The codes are the owner's (*The routes answer `200` where the pair already existed*). A row covers a pair only by holding this post's pairing with this tag at one of the post's versions (§DOM4.6), so the pair already existed for every `AlreadySuggested` answer. An approved pairing answers `AlreadySuggested`, and never a status of its own, because the caller may not see it: it hangs off a tag the caller cannot see, and a state of a row the caller may not see is never told (§SEC14.5 rule 2). A pairing the caller can see, approved and published with a public tag, is in the panel's collection already, and the panel drops a value it already lists before raising `onAdd` (`Design/UI/Components/AssociationPanel.md` rule 2.22). The caller's own row that is approved, rejected or soft-deleted is open question 1.
8. **What the reader is told.** The page answers each outcome in a toast, as it answers today's *coming soon* (`toastSuccess`):
   - `Created`: *"Thank you for your contribution. Your tag has been suggested and is under review."*
   - `AlreadySuggested`: *"Thank you for your contribution. This tag has already been suggested by another user and is under review."* (*A pairing that was itself rejected*)
   - `AlreadySuggestedByCaller`: *"Thank you for your contribution. You have already suggested this tag, and it is under review."* (*The reader's own pending pairing*)
   - A refusal or a failure: the app's global error toast, as for any failed write (`src/brokers/apiBroker.globals.ts`; RejectedEndpoints.md rule 9).

   Once the write settles, whether it succeeded or failed, the page's association reads are read again (`UI/Foundations/AssociationService.TagSuggestions.md §1`), so the panel's collection shows whatever the reader may now see of the pairing once the page reads it (`UI/Pages/PostDetail.md §6 item 4`, `UI/Pages/MyPostDetail.md §6 item 5`).
9. **The gates come in this order, and nothing is read or written past a refusal that could be known first** (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`):
   1. authentication and the global `ReadOnly` (§SEC14.7 posture A′ rule 1), before anything is read;
   2. the suggestion's own fields: the post's id, and the name, under rule 2;
   3. the post, resolved under the caller's envelope as the upsert resolves an endpoint (§DOM4.5): a post that does not exist, or that the caller may not see, is not found (§SEC14.5 rule 1);
   4. the pairing's endpoint veto, composed from the post's derived content type and the tag's type: `ContentItem-ReadOnly`, `ContentItem-{ContentType}-ReadOnly` and `Tag-ReadOnly` (posture A′ rule 1). It is the tag add's veto as well, which is `ReadOnly` or `Tag-ReadOnly` (posture A rule 1), so a caller who could not add the tag is refused before the tag is read;
   5. a rejected post (RejectedEndpoints.md rules 1 and 10);
   6. the facet gate: the post's winning setting must allow `TagsAllowed` (§ARC16.2.1). It comes before the read by name, so a caller refused on the post's setting learns nothing of the name;
   7. the read by name, and a rejected or soft-deleted tag (rule 4);
   8. the add of a new tag, where none holds the name;
   9. the occupancy check (rule 6);
   10. the insert of the pairing.
10. **What a failure midway leaves, and what a retry then does.**
    - **The tag was added, and the pairing was not written** — the occupancy check or the insert failed. The tag stays, a pending suggestion at `Submitted`, unpaired, and its round is open, since its add published `Tag-Added` (rule 13). It is not removed: a removal would hold its name for good (§ARC12.3.1 rule 2a), and removal is its owner's act or an `Administrators`' takedown (§APR9.9). A retry of the same name finds the caller's own `Submitted` tag and pairs it, `Created`, and so does another reader's suggestion of the name (rule 4).
    - **Two readers add the same new name at once.** Both find no tag; the second's add is refused by `IX_Tags_Name` (`AlreadyExistsTagException`). The submission then reads the name once more and goes on with the tag it finds as rule 4 has it — the first reader's `Submitted` tag, which it pairs — and answers as rule 7 answers. So the second reader receives what they would have received had the first reader's tag been there before they asked (*A tag the reader cannot see*). If that second read finds no row, the add's refusal stands, as a fault (rule 11).
    - **Two readers pair the same tag with the same post at once.** Both probes find the pair free; the second insert is refused by the editorial pair index, and answered `409`, as the upsert's editorial arm answers it (`Backend/Controllers/AssociationsController.md`, the shared mapping). A retry answers `AlreadySuggested`.
    - **An undelete followed by a failed pairing** is #1362's to settle (#1362, point 8).
11. **Every refusal `ITagService` could make is asked first by the submission**, so none reaches the caller from beneath: the tag's veto (rule 9, step 4), the name's emptiness and length (step 2), and a taken name (rule 10, the second read). What still reaches the orchestration's closing `catch (Xeption)` from `ITagService` is a fault beneath it, not the caller's, and leaves as its dependency exception, `424`, as a refusal from the summary read's foundations does (`Backend/Orchestrations/AssociationOrchestrationService.md §4` rule 9). No catch arm is added, for that rule's reason.
12. **No request address: the submission is direct only**, as the personal upsert is, which has no `Association-Upserting` (§ARC16.2.2). Nothing in the system publishes a suggestion. Its answer is a status, and a reply on an event address carries an envelope of the row, which is what the status withholds (§ARC12.5.2 rule 6, *"Rule 2 — the occupancy check runs on both doors"*). And an address is an event contract nobody has asked for. If one is minted, it binds `IAssociationOrchestrationService`, which derives the pairing's content type, an authorization input (§ARC12.5.2 rule 6, *an address whose handler derives an authorization input binds the layer that derives it*), and its deduplication is designed with it.
13. **The facts are the writes' own.** The submission publishes no fact of its own, as the upsert publishes none: the orchestration holds no `IEventBroker` (§ARC12.5, *an orchestration holds brokers*). A new tag's add publishes `Tag-Added`, which `ApprovalOrchestrationService.OnTagAddedAsync` hears, opening the tag's round (§EVN18 rule 1; §APR9.2 rule 1). A new pairing's add publishes `Association-Added`, which `ApprovalOrchestrationService.OnAssociationAddedAsync` hears. #1362 designs what its undelete publishes. No subscriber changes.
14. **The status each new row opens at.** A new tag opens at `Submitted`: suggesting a tag *"is a finished act with no draft stage"* (§APR9.2), the add takes its status from its caller (§APR9.7.1 rule 1), and the submission is that caller. A new pairing opens at `Submitted` (§ARC16.8.1, #872). A tag the submission finds is left as it is: the submission writes nothing to a tag that exists, save #1362's undelete.
15. **The tag's add mints the new tag's `Id`.** An `Id` is an `IKey` member, the identifier broker's, never accepted from a caller (§APR9.7.1 rule 2), and the orchestration holds no identifier broker (§ARC12.5, *an orchestration holds brokers*), so it cannot hand the add one. Until this feature the add took its caller's and refused an empty one (`TagService.Validations.cs`, `ValidateOnAddTagAsync`). It mints it instead, on both entry paths, as the association's add does (`Backend/Foundations/AssociationService.md §13`; `Backend/Foundations/TagService.TagSuggestions.md §1` and §2). So `POST /api/Tags` writes the minted `Id` whatever its caller sends, and answers it.
16. **The two post pages send it.** `/posts/{id}` and `/myposts/{id}` send each tag their panel raises to this feature's route and answer it under rule 8 (`UI/Pages/PostDetail.md §6 item 17`, `UI/Pages/MyPostDetail.md §6 item 18`). Their panels' collections are the association read's (#1128), which each page wires separately (`UI/Pages/PostDetail.md §6 item 4`, `UI/Pages/MyPostDetail.md §6 item 5`). The panel itself is unchanged: it raises `onAdd`, and calls nothing (`Design/UI/Components/AssociationPanel.md` rule 2.7).
17. **A suggestion for a rejected post is refused, and the page answers the refusal as any failed write** (rules 8 and 9, step 5). `/myposts/{id}` shows its owner their rejected post, and offers the box there. Whether the box is withheld on a rejected post, as Like is (RejectedEndpoints.md rule 9), is open question 3.

## User stories

| User story | Level |
| --- | --- |
| [Backend/Brokers/StorageBroker.TagSuggestions.md](Backend/Brokers/StorageBroker.TagSuggestions.md) | broker — `IStorageBroker`, the tag pass-through the read by name hands its condition to |
| [Backend/Foundations/TagService.TagSuggestions.md](Backend/Foundations/TagService.TagSuggestions.md) | foundation — `ITagService`: the add mints the `Id`, on both doors, and the read by name |
| [Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md](Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md) | orchestration — `IAssociationOrchestrationService.SuggestTagAsync` |
| [Backend/Controllers/AssociationsController.TagSuggestions.md](Backend/Controllers/AssociationsController.TagSuggestions.md) | exposer — `AssociationsController`, the route |
| [UI/Brokers/AssociationBroker.TagSuggestions.md](UI/Brokers/AssociationBroker.TagSuggestions.md) | React broker — `AssociationBroker`, the write |
| [UI/Foundations/AssociationService.TagSuggestions.md](UI/Foundations/AssociationService.TagSuggestions.md) | React foundation — `associationService.useSuggestTag` |

Each component but the storage broker's tag partial already has a user story document under another feature, so this feature's work on each is in a document of its own beside it (`Design/design.md`, *A component that a second feature adds operations to gets a second user story document*).

The two pages' halves are designed in their page documents (§UI20.5.1), and their tasks are carved from their gaps:

| Page document and gap | What it is |
| --- | --- |
| `Design/UI/Pages/PostDetail.md §6 item 17` | `/posts/{id}` sends a suggested tag and says what came of it |
| `Design/UI/Pages/MyPostDetail.md §6 item 18` | `/myposts/{id}` does the same |

**Build order.** The storage broker's pass-through, then the read by name. The add's mint, then its event door's pins. The orchestration once both are in, and once #1358 has `ResolvedEndpoint` keep the status a rejected post is refused by. The route after the orchestration. The React broker, then its service, then the two pages, in either order.

**Layer placement — ruled.** The owner placed the submission on `IAssociationOrchestrationService`, behind a route on `AssociationsController` (*Where the submission lives*). The flow spans three entity types (below), which is §ARC12.1 rule 2's orchestration. The controller binds that one service (§EVN13 rule 3; `the-standard-exposers` ts-exposers-001). The rules that decide a pairing — the endpoint veto, the rejected post, the facet gate, the occupancy check — are already the orchestration's, so the submission reaches them there rather than copying them.

- **What it holds.** It adds no dependency: `ITagService`, `IContentItemService` and `IAssociationService` are among the seven entity services the orchestration holds, and the brokers it already holds are enough. Its seven entity services are a two-to-three deviation, approved for this operation on its own argument (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations* 1). The Likes user story's deviation (`Backend/Orchestrations/AssociationOrchestrationService.md`, *Deviations* 1) is not its reason.
- **The read by name is the tag foundation's.** It is a read of one entity, and its condition is a decision the foundation authors and unit-tests (§ARC12.2.1 rule 1). It is `internal`, as the identity-carrying tag read is, and it answers a projection rather than the row (`Backend/Foundations/TagService.TagSuggestions.md §3` rules 1 and 6), because `TagsController` binds the foundation (§EVN13 rule 3) and a public member answering the row would hand a row the caller may not see to any route wired to it (§SEC14.5 rules 1 and 3). This is §SEC14.6's pattern for a cross-row fact under visibility filtering — an unfiltered read that answers no row data and carries the contribution gate (§SEC14.6, *Cross-row rules under visibility filtering*) — as the pair probe's `AssociationPairMatch` is.
- **The find-or-add is the orchestration's.** A read by name and then an add, over one entity, is a probe-then-write, which §ARC12.1 rules 1 and 3 give to a processing service. One beneath the orchestration was weighed and rejected (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md`, *Deviations* 1).

## Entity count, events and storage

- **Entities: three.** The `Tag` the name names, read and perhaps added; the `Association` that pairs it; and the host `ContentItem`, read under the caller's envelope to show the caller may see it and to derive its content type, an authorization input of the pairing's veto and facet gate (§DOM4.5 rule 3).
- **Events.** None new: no request address (rule 12) and no fact of its own (rule 13). The writes publish the facts they publish today, `Tag-Added` and `Association-Added`, with their subscribers unchanged.
- **Storage.** No schema, no index and no migration. The read by name asks `Tags` by `Name`, which `IX_Tags_Name` indexes, through a new storage-broker pass-through (`Backend/Brokers/StorageBroker.TagSuggestions.md §1`).

## Risks

- **Reversible throughout.** No schema, no event and no migration. The add's mint changes what `POST /api/Tags` stores for a caller's `Id`, and nothing reads a caller's `Id` back but tests (rule 15).
- **The page's refresh can tell what the answer does not.** The answer to a tag the caller cannot see is the answer to a tag they created (rule 5). Once the page reads its associations, though, the panel shows the caller their own pending pairing with their own tag, since both rows are theirs (§SEC14.7 posture A rule 4), and does not show a pairing with another reader's hidden tag until that tag is approved and published (§SEC14.3 rule 4), which is what the owner's ruling has the pairing do (*"The pairing shows once the tag is approved and published."*).
- **A pairing with another reader's `Draft` tag waits on them.** A `Draft` tag reaches no reviewer until its creator submits it (§APR9.2 rule 2), so the pairing does not show until they do.
- **A tag added by a failed suggestion sits in review unpaired** (rule 10), until a retry pairs it or the review decides it.
- **The add's mint changes `POST /api/Tags`.** A caller's `Id` is ignored, and the route answers the minted one. Tests that post a tag and read it back by the `Id` they posted must read it by the `Id` answered (`Backend/Foundations/TagService.TagSuggestions.md §1` rule 5).
- **The refusal of a rejected name tells a caller something no read tells them** (*Deviations* 2, proposed).

## Out of scope

- The approval process for the tag and for the pairing, Berean's review among it (*The requirement*).
- Suggesting a Bible reference: #1357.
- The undelete itself, and which routes reach it: #1362 (rule 4).
- The refusal of a rejected item on every other door: `DesignFeatures/RejectedEndpoints.md`.
- Reading an entity's associations (#857's tasks), and removing one by id (#700).
- The panels on `/BibleReferences` (#700), and on `/Admin/Posts/{id}`, where the card's association chips replace them (`UI/Pages/ContentItemModerationDetailPage.md §6 item 17`).
- A reader's reaction, the personal arm of the upsert.
- Renaming a tag, and with it §ARC12.3.1 rule 2a's index fix.
- The panels' read-only roles and facet switches (`Design/UI/Components/AssociationPanel.md §10 items 1 and 20`; `UI/Pages/PostDetail.md §6 item 8`; `UI/Pages/MyPostDetail.md §6 item 6`).
- A per-tag length cap in the panel. The box caps what is typed at 100 characters (`Design/UI/Components/AssociationPanel.md` rule 3.1.14) and several tags may be typed at once; a single name over 30 characters is refused by the server (rule 2) and answered as a failed write (rule 8).

## Deviations

1. **§ARC12.3.1 rule 5a's visibility precondition, and §SEC14.3's paragraph *The symmetry with the write is the argument*, on this route.** Rule 5a binds a pairing to an endpoint the caller can see, *"An unrelated caller still cannot, because the endpoint reports not-found to them"*, and §SEC14.3 calls a pairing the caller could not then read back *"the one outcome neither rule intends"*. On this route a tag the caller cannot see is paired, and the caller cannot read the pairing back until the tag is public (rule 5). **Why:** a name is held by at most one tag (`IX_Tags_Name`), so a suggestion of a name another reader's hidden tag holds can neither add a tag nor, under rule 5a, pair the one there; the owner ruled that it pairs it. **Instead:** the tag is found by the read by name, which no identity filters (rule 3), and paired; the answer tells nothing of it (rules 5 and 7). The post, the other endpoint, keeps rule 5a's precondition: it is resolved under the caller's envelope (rule 9, step 3). Every other door keeps rule 5a as written. Recorded here rather than in rule 5a, because it is this route's departure and no rule every feature inherits (`Design/design.md`, *Record every deviation where it happens*). **Approved** by the owner on 2026-10-27 (*A tag the reader cannot see*: *"Pair the post with that tag and answer as if the reader had created it."*).
2. **§SEC14.5 rules 1 and 3, for the refusal of a name — proposed, not approved.** Rule 1 keeps a probe from telling a non-public row from a row that does not exist, and rule 3 keeps a soft-deleted row from every caller. The submission refuses a name held by a `Rejected` tag, or, until #1362's undelete replaces it, by a soft-deleted one (rule 4). A caller who cannot see that tag is then told something about it: where no tag holds a name the submission adds one and answers `Created`, and where another reader's hidden tag holds it the submission pairs it and answers `Created` too, so a refusal tells them that a tag holds the name and may not be paired. **Why:** the owner ruled that a rejected tag refuses the suggestion, with no pairing and no new tag (*Where the submission lives*), and any refusal is told apart from `Created`. **Instead:** the refusal says as little as one refusal can. It is one message for every such row, *"The tag cannot be suggested."*, naming no status, no owner and no id (§SEC14.5 rule 2), so a caller cannot tell a rejected tag from a soft-deleted one, and the true reason is logged server-side before it is thrown (§SEC14.5 rules 6 and 7). Beyond the refusal, `POST /api/Tags` already answers any signed-in caller `409` for a name any row holds, whatever its state (`TagsController.cs`, `PostTagAsync`). **Not approved:** open question 2 asks the owner.

## Open questions

1. **The caller's own pairing that is no longer pending.** Where the pair is held by a pairing the caller suggested that is `Approved`, `Rejected` or soft-deleted (withdrawn by them, or taken down), the owner's rulings, read as written, give it the *"another user"* wording: it is not pending (*The reader's own pending pairing*), and every other duplicate takes that wording (*A pairing that was itself rejected*; *"Others still see the 'another user' wording."*). So rule 7 answers it `AlreadySuggested`. That wording is untrue of the caller's own row, as it was of their own pending one. Does the caller's own pairing at any of those states answer *"You have already suggested this tag"* instead, and with what in place of *"and it is under review"*, which is untrue of an approved, rejected or removed pairing?
2. **The refusal of a rejected name** (*Deviations* 2). Do you approve the refusal telling a caller that a tag they cannot see holds the name and may not be suggested, as recorded there?
3. **The box on a rejected post.** On `/myposts/{id}` an owner's rejected post is shown, with the *Suggest a tag* box, and the write refuses a suggestion there (rule 17). Is the box withheld on a rejected post, as Like is (RejectedEndpoints.md rule 9), or offered with the refusal answered as a failed write? The tasks are carved for the second; the first would add a gap to the panel and the two pages.
