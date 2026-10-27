# Rejected endpoints
Epic: [INTENT.md](../../INTENT.md)
Inherits: §ARC12.3.1 rules 5 and 5a, §ARC12.5 (*an orchestration holds brokers*, and the two-to-three rule), §ARC12.5.2 (`Association-Adding`'s write flow), §ARC16.2.1, §ARC16.8.1, §DOM4.5, §DOM4.10 rules 6–9, §EVN13 rule 3, §SEC14.1, §SEC14.5 rules 1 and 2, §SEC14.6 rules 1 and 4, §SEC14.7 posture A rule 4 and posture A′ rules 1 and 4, §APR9.9 rules 6 and 7, Likes.md rule 11a
Mockups: none. No page is redesigned: a page that offers a pairing on a rejected item answers the refusal as it answers any failed write (rule 9), and open question 1 asks whether it should offer it at all.
Design task: #1352

A moderator who rejects an item rules it unfit. This feature makes that ruling stick: an item whose approval status is `Rejected` is refused as an endpoint wherever a pairing is created, restored or changed, for every caller, and every removal stays as it is.

## Problem

When this feature was designed (2026-10-27, `main` at `ff450f3e`), the association write refused an endpoint only where it could not be resolved. §ARC12.3.1 rule 5a held that the precondition was *"resolvable and not deleted", never "approved"*: `AssociationOrchestrationService` read no endpoint's `ApprovalStatus`, so the owner of a `Rejected` item, and anyone holding a review role, could pair it, *"and that is correct"*. Three doors did so. `POST /api/associations`' editorial arm and the `Association-Adding` door paired a rejected item the caller could see. And a reader's reaction holds its host on endpoint A (§DOM4.10 rule 9), so a rejected post's or Bible reference's owner, or a reviewer, could react to it, revive a reaction they had withdrawn on it, or change a live reaction on it. The owner ruled that a rejected item may not be paired at all.

## The rulings

The owner ruled on 2026-10-26, under #1352:

> If a tag or bible reference was rejected, then the association should not be allowed. I new tag OR bible reference MUST NOT be created since we have already ruled that the one submitted is unfit.

The second sentence is the suggestion's, designed under #1356 for a tag and #1357 for a Bible reference. The rest of this section is the owner's answers through the session's question prompt, word for word, each question as it was put. The first five were asked of a tag or a Bible reference. The last defines a rejected item by its status, so each of them reaches every endpoint (rule 1).

**Every route** (2026-10-26):

> Q: Your ruling says a rejected tag or Bible reference means "the association should not be allowed". Does that also cover the existing pair-by-ID route (POST /api/associations) and its event-driven equivalent, overriding the rule that lets an owner or reviewer pair a Rejected tag or reference today?
>
> A: "Yes, every route" — the option read: "No association with a Rejected tag or reference, by any route, for anyone. Rule 5a is amended."

**The moderated restore** (2026-10-27):

> Q: Your rejection ruling refuses an association with a Rejected tag or Bible reference on every route. Does that include #1326's moderated restore of a soft-deleted association, so a pair whose tag was rejected cannot be brought back?
>
> A: "Yes, restore too" — the option read: "A moderator's restore refuses a pair whose tag or reference is Rejected. #1326 is held until this design amends it."

**A reader's reaction** (2026-10-27):

> Q: A reader's reaction (a Like) to a Bible reference passage is also an association. Today the reference's owner or a reviewer can react to a Rejected reference, or bring back a withdrawn reaction on it. Does your ruling "No association with a Rejected tag or reference, by any route, for anyone" also refuse those reactions?
>
> A: "Yes, reactions too" — the option read: "No reaction on a Rejected tag or Bible reference, new or restored, for anyone. The personal arm of the upsert becomes one of the routes the design amends."

**A changed reaction** (2026-10-27):

> Q: A reader's reaction on a Bible reference can stay live after the reference is rejected. Does your "reactions too" ruling also refuse that reader changing their live reaction (for example Love to Moved) on a Rejected reference, or only a new or restored reaction?
>
> A: "Refuse changes too" — the option read: "No new, restored or changed reaction on a Rejected tag or Bible reference. The existing live reaction stays as it is."

**No removal** (2026-10-27):

> Q: Your rejection ruling refuses adding, restoring or changing an association with a Rejected tag or Bible reference. Does it also stop anyone removing one, such as a reader withdrawing their own reaction or a moderator taking a pair down?
>
> A: "Removals allowed" — the option read: "The ruling covers creating, restoring and changing a pairing only. Withdrawals and takedowns stay as today."

**Any associations on a rejected item** (2026-10-27). Asked what a submission does with a tag name a soft-deleted tag holds, the owner closed their answer with:

> If if a tag or bible reference has been marked as rejected, it will block creation of a duplicate tag / bible reference.  It MUST also block the creation of any associations on a rejected item.

The whole answer is recorded in #1356, #1357 and #1362, whose designs it rules on. Its last sentence restates rule 1 below.

**What a rejected item is** (2026-10-27), in the owner's own words, chosen over both options put (*"Tags and references"* and *"Any rejected item"*):

> Q: "A rejected item" in your answer: the planner read it as a Rejected tag or Bible reference only. Did you mean only those, or any Rejected endpoint (a post, link, comment or reaction as well)?
>
> A: "A rejected item is an item where its approval status is REJECTED"

## Business rules

1. **An item whose approval status is `Rejected` is refused as an endpoint of a pairing that is being created, restored or changed**, whoever the caller (*The rulings*). The rule is by status, not by type (*What a rejected item is*): it reaches every endpoint type a pairing resolves today — a post (`ContentItem`), a link, a tag, a reaction, a comment and a Bible reference — each of which carries `ApprovalStatus` (§SEC14.7 posture A). Its owner and every review role are refused with everyone else. The refusal amends §ARC12.3.1 rule 5a, which is its single home in the epic.
2. **The doors, by act.** Each is a door that exists when this feature is designed:
   - **Creating a pairing:** `UpsertAssociationAsync`'s editorial arm, served as `POST /api/associations`, and the `Association-Adding` door, which runs the same write flow (§ARC12.5.2); and the upsert's personal arm, where it would create a reader's reaction on a rejected host, or with a rejected reaction (its `Created` outcome).
   - **Restoring a pairing:** the personal arm, where it would revive a reaction the reader withdrew on a rejected host, or with a rejected reaction (its `Restored` outcome); and #1326's moderated recovery of a soft-deleted association, which is not designed yet, and which #1352 amends to carry the refusal (rule 11).
   - **Changing a pairing:** the personal arm, where it would change a reader's live reaction on a rejected host, or to a rejected reaction (its `Repointed` outcome).

   The suggestion routes are doors that create a pairing too, and #1356 and #1357 design their refusal (rule 12). So is a pairing that undeletes a soft-deleted endpoint, which #1362 designs.
3. **No removal is a door** (*No removal*). The pair-keyed withdrawal, `DELETE api/Associations/Pair`, by which a reader withdraws their own reaction whatever its status (§APR9.9 rule 6), and an administrator's takedown at any status (§APR9.9 rule 7), through the remove by id (#700), are not refused. A reaction that is live on an item when it is rejected stays as it is.
4. **The read decides which refusal, never whether.** The refusal reads the endpoint row that endpoint resolution has already read under the caller's envelope (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 1, step 3). A `Rejected` row is never public (§SEC14.1), so that read answers it to its owner and the review roles alone (§SEC14.7 posture A rule 4). Every other caller is answered not-found at resolution, before the refusal is reached, as today, and learns nothing about the row (§SEC14.5 rules 1 and 2). So every caller is refused: the visibility-filtered read decides only whether the answer names the rejection or is not-found, and never decides the invariant (root `CLAUDE.md`, *Non-negotiables*).
5. **It is asked after an editorial pair's endpoint veto, and before the `UserId` derivation and the facet gate.** Authorization is refused first: a caller the veto blocks is answered as today. A refused pair asks no settings read and reaches no pair probe, so nothing is read for it beyond its two endpoints, and nothing is written. The facet gate keeps every place §ARC16.2.1 fixes for it: after both endpoints resolve and after the `UserId` derivation, and before the pair probe.
6. **It sits in the flow both add doors share, and never in the endpoint resolution the withdrawal shares.** One step in the write flow `UpsertAssociationAsync` and the `Association-Adding` door run, so it is on both doors by construction (§ARC16.2.1, *the gate is reached on both entry paths*; §SEC14.6 rule 1). The pair-keyed withdrawal resolves its endpoints through the same resolution and runs none of that flow, which is how rule 3 holds.
7. **Every upsert with a rejected endpoint is refused, whatever it would have answered.** On the personal arm, that includes an unchanged reaction and a row a moderator took down. The arm runs no probe of its own, and which outcome a reaction reaches is decided inside the foundation's write (§ARC16.8.1; §ARC16.2.2), so the refusal cannot wait to learn it. The two outcomes that write nothing, `Unchanged` and `TakenDown`, leave the row as it is when refused, which is what the owner's ruling has a live reaction do (*A changed reaction*). On the editorial arm, the refusal comes before the arm's two pair probes (rule 5), so a pair a row already holds is refused too, where the arm would have answered `AlreadyApproved`, `AlreadyPending` or `OverlapsExisting`. Nothing is written either way.
8. **The refusal is a validation failure, answered `400`.** It is `InvalidAssociationOrchestrationException`, which the service maps to `AssociationOrchestrationValidationException`, and `AssociationsController`'s shared mapping answers that `400` (`Backend/Controllers/AssociationsController.md`), unchanged. Its message names the endpoint, `A` before `B` where both are rejected: *"The {A|B} endpoint is rejected, so it cannot be paired."* The caller may see the row, so naming it discloses nothing (rule 4). On the `Association-Adding` door the refusal writes nothing, publishes no fact and records no `ProcessedEvents` row, as every refusal on that door does.
9. **A page that offers a pairing on a rejected item answers the refusal as it answers any failed write.** The only caller of the React write is a post card's reaction (`useContentItemEngagement.ts`). Like is offered on a post that is not public yet, its owner's rejected post among them, on `/myposts`, `/myposts/{id}`, `/Admin/Posts` and `/Admin/Posts/{id}` (Likes.md rule 11a). A Like there is now refused: the hook takes the card's overlay away and the app's global handler announces the failure, as for any failed reaction write (`UI/Hooks/ContentItemEngagement.md §2` rule 6). Whether the card should still offer Like on a rejected post is open question 1. No page sends any other pairing today: the post pages answer a suggestion with a *coming soon* notice (`UI/Pages/PostDetail.md` §6 items 17 and 18), and a passage's reaction bar records nothing (#700).
10. **A versioned endpoint is rejected when the version the pairing names is.** A post or a link has versions, and endpoint resolution reads the version the pairing names, keying the pairing on its group (§DOM4.5). That version's `ApprovalStatus` is the one read, as the owner's ruling puts it: *"an item where its approval status is REJECTED"*. A pairing that names an approved version of the same group is not refused.
11. **#1326's recovery carries the refusal.** #1326, which designs the recovery of a soft-deleted item, was held until this design amended it (*The moderated restore*). #1352 amends its deliverable 5, beside the association whose endpoint is still deleted, so that its recovery of a soft-deleted association refuses a pair either of whose endpoints is `Rejected`. Where the recovery reads the endpoint, and on which layer, is #1326's design, under rule 4: a read the recovering caller's visibility filters may decide which refusal, never whether.
12. **A door that does not resolve the endpoint under the caller's envelope refuses through its own read.** The suggestion routes find a tag by name and a reference by `USFM`, through reads that see rows the caller cannot (#1356, *A tag the reader cannot see*), and #1362's undelete finds a soft-deleted endpoint through a read of its own. Each refuses a rejected row through that read, without disclosing it (§SEC14.5 rules 1 and 2), and each of those designs says how.

## User stories

| User story | Level |
| --- | --- |
| [Backend/Orchestrations/AssociationOrchestrationService.RejectedEndpoints.md](Backend/Orchestrations/AssociationOrchestrationService.RejectedEndpoints.md) | orchestration — `IAssociationOrchestrationService`, the refusal in the upsert's write flow and its two pins |

The component already has a user story document under the Likes feature, so this feature's work is in a second document beside it (`Design/design.md`, *A component that a second feature adds operations to gets a second user story document*).

**Layer placement.** The refusal needs the endpoint row beside the `Association` being written: two entity types, which is §ARC12.1 rule 2's orchestration. The orchestration already reads that row, under the caller's envelope, to resolve the endpoint. Placed in the association foundation, the rule would make it read a second entity. Placed in the controller, it would put a rule in an exposer, which §EVN13 rule 3 and `the-standard-exposers` keep thin, and the `Association-Adding` door would walk past it.

**No dependency, no operation and no deviation.** `AssociationOrchestrationService` already holds the foundation service of every endpoint type it resolves, and this feature adds neither a dependency nor a member: it changes what two existing members refuse, and pins a third. The service's count of seven entity services is the Likes user story's approved deviation (`Backend/Orchestrations/AssociationOrchestrationService.md`, *Deviations* 1), which names those seven and the members this feature changes. Nothing here is argued from it, and nothing here needs a deviation of its own.

## Entity count, events and storage

- **Entities.** Unchanged. The write flow already reads `Association`, both endpoints and, for a content item host, the `ContentItem`. The refusal reads the status of an endpoint row the flow has already read.
- **Events.** None new. A refusal publishes nothing, and the facts the doors publish when they write are unchanged.
- **Storage.** None. No schema, no index and no migration.

## Risks

- **Reversible throughout.** No schema, no event and no migration. Taking the refusal out restores what an owner and a review role could pair before.
- **The refusal narrows what an owner and a review role may pair.** Before it, either could pair a rejected item they could see. One page sends such a pairing today: a Like on its owner's rejected post, or a moderator's on the admin pages, which is now refused (rule 9).
- **The existing tests build endpoints at `Draft`.** The orchestration's tests build each endpoint row explicitly, `new Tag { Id = … }` and its kind, with no random filler, and a row built so is `Draft`, the enum's zero value (`ApprovalStatus.cs`), so no existing test reaches the refusal. The `Rejected` cases in `AssociationOrchestrationServiceTests.Upsert.Logic.cs` are the pairing's own status, answered `AlreadyPending`, and no endpoint's.
- **A rejection reset by an administrator lifts the refusal.** §APR8.6 HR-4's override puts a decided round back to `Submitted`, and the row follows it (§APR9.8), so the refusal reads the status the row holds when the pairing is asked for, and no other.

## Out of scope

- The suggestion routes, and their own refusal of a rejected row (rule 12): #1356 for a tag, #1357 for a Bible reference.
- Every removal (rule 3), and every pairing that already exists when its endpoint is rejected.
- The approval process. Approving or rejecting an association whose endpoint is rejected creates, restores and changes no pairing, and the owner ruled the approval process separate from the submission (#1352, *Request*).
- §ARC12.3.1 rule 5, a soft-deleted endpoint, which these doors answer not-found until #1362's design replaces it. The owner ruled that a pairing on every route undeletes a soft-deleted endpoint and pairs it (#1362, *How far it reaches*).
- #1326's design, beyond the amendment rule 11 names.

## Open questions

1. **Like on a rejected post.** Likes.md rule 11a offers Like on a post that is not public yet, its owner's rejected post among them (the owner, 2026-09-28: *"Yes, I don't mind it being available there"*). The refusal now refuses that Like (rule 9). Does the card keep offering Like on a rejected post, answering a press with the failure, or withhold it there? Withholding it is a change to the card and to the pages that render it, which this feature would plan.
