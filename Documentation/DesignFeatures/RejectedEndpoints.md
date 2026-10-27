# Rejected endpoints
Epic: [INTENT.md](../../INTENT.md)
Inherits: §ARC12.3.1 rules 5 and 5a, §ARC12.5 (*an orchestration holds brokers*, and the two-to-three rule), §ARC12.5.2 (`Association-Adding`'s write flow), §ARC16.2.1, §ARC16.8.1, §DOM4.10 rules 6–9, §EVN13 rule 3, §SEC14.5 rules 1 and 2, §SEC14.6 rules 1 and 4, §SEC14.7 posture A rule 4 and posture A′ rules 1 and 4, §APR9.9 rules 6 and 7
Mockups: none. No page changes: no page sends a pairing with a `Tag` or `BibleReference` endpoint today (rule 9).
Design task: #1352

A moderator who rejects a tag or a Bible reference rules it unfit. This feature makes that ruling stick: a rejected tag or Bible reference is refused as an endpoint wherever a pairing is created, restored or changed, for every caller, and every removal stays as it is.

## Problem

When this feature was designed (2026-10-27, `main` at `ff450f3e`), the association write refused an endpoint only where it could not be resolved. §ARC12.3.1 rule 5a held that the precondition was *"resolvable and not deleted", never "approved"*: `AssociationOrchestrationService` read no endpoint's `ApprovalStatus`, so the owner of a `Rejected` tag or Bible reference, and anyone holding a review role, could pair it, *"and that is correct"*. Three doors did so. `POST /api/associations`' editorial arm and the `Association-Adding` door paired a rejected tag or reference the caller could see. And a reader's reaction holds its host on endpoint A (§DOM4.10 rule 9), so the reference's owner or a reviewer could react to a rejected Bible reference, revive a reaction they had withdrawn on it, or change a live reaction on it. The owner ruled that a rejected tag or Bible reference may not be paired at all.

## The rulings

The owner ruled on 2026-10-26, under #1352:

> If a tag or bible reference was rejected, then the association should not be allowed. I new tag OR bible reference MUST NOT be created since we have already ruled that the one submitted is unfit.

The second sentence is the suggestion's, designed under #1356 for a tag and #1357 for a Bible reference. The rest of this section is the owner's answers through the session's question prompt, word for word, each question as it was put.

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

**A rejected item** (2026-10-27). Asked what a submission does with a tag name a soft-deleted tag holds, the owner closed their answer with:

> If if a tag or bible reference has been marked as rejected, it will block creation of a duplicate tag / bible reference.  It MUST also block the creation of any associations on a rejected item.

The whole answer is recorded in #1356 and #1357, whose suggestions it rules on. Its last sentence restates rule 1 below, and this feature reads *"a rejected item"* as the tag or Bible reference the sentence before it names (rule 10).

## Business rules

1. **A rejected tag or Bible reference is refused as an endpoint of a pairing that is being created, restored or changed**, whoever the caller (*The rulings*). A `Tag` or `BibleReference` endpoint is rejected when its stored `ApprovalStatus` is `Rejected`. Its owner and every review role are refused with everyone else. The refusal amends §ARC12.3.1 rule 5a, which is its single home in the epic.
2. **The doors, by act.** Each is a door that exists when this feature is designed:
   - **Creating a pairing:** `UpsertAssociationAsync`'s editorial arm, served as `POST /api/associations`, and the `Association-Adding` door, which runs the same write flow (§ARC12.5.2); and the upsert's personal arm, where it would create a reader's reaction on a rejected Bible reference host (its `Created` outcome).
   - **Restoring a pairing:** the personal arm, where it would revive a reaction the reader withdrew on a rejected Bible reference host (its `Restored` outcome); and #1326's moderated recovery of a soft-deleted association, which is not designed yet, and which #1352 amends to carry the refusal (rule 11).
   - **Changing a pairing:** the personal arm, where it would change a reader's live reaction on a rejected Bible reference host (its `Repointed` outcome).

   A reaction on a tag is already refused at every status, because canonical order would put the reaction on endpoint A (§DOM4.10 rule 9). The suggestion routes are doors that create a pairing too, and #1356 and #1357 design their refusal (rule 12).
3. **No removal is a door** (*No removal*). The pair-keyed withdrawal, `DELETE api/Associations/Pair`, by which a reader withdraws their own reaction whatever its status (§APR9.9 rule 6), and an administrator's takedown at any status (§APR9.9 rule 7), through the remove by id (#700), are not refused. A reaction that is live on a reference when it is rejected stays as it is.
4. **The read decides which refusal, never whether.** The refusal reads the endpoint row that endpoint resolution has already read under the caller's envelope (`Backend/Orchestrations/AssociationOrchestrationService.md §1` rule 1, step 3). A `Rejected` row is not public (§SEC14.1), so that read answers it to its owner and the review roles alone (§SEC14.7 posture A rule 4). Every other caller is answered not-found at resolution, before the refusal is reached, as today, and learns nothing about the row (§SEC14.5 rules 1 and 2). So every caller is refused: the visibility-filtered read decides only whether the answer names the rejection or is not-found, and never decides the invariant (root `CLAUDE.md`, *Non-negotiables*).
5. **It is asked after an editorial pair's endpoint veto, and before the `UserId` derivation and the facet gate.** Authorization is refused first: a caller the veto blocks is answered as today. A refused pair asks no settings read and reaches no pair probe, so nothing is read for it beyond its two endpoints, and nothing is written. The facet gate keeps every place §ARC16.2.1 fixes for it: after both endpoints resolve and after the `UserId` derivation, and before the pair probe.
6. **It sits in the flow both add doors share, and never in the endpoint resolution the withdrawal shares.** One step in the write flow `UpsertAssociationAsync` and the `Association-Adding` door run, so it is on both doors by construction (§ARC16.2.1, *the gate is reached on both entry paths*; §SEC14.6 rule 1). The pair-keyed withdrawal resolves its endpoints through the same resolution and runs none of that flow, which is how rule 3 holds.
7. **Every upsert with a rejected endpoint is refused, whatever it would have answered.** On the personal arm, that includes an unchanged reaction and a row a moderator took down. The arm runs no probe of its own, and which outcome a reaction reaches is decided inside the foundation's write (§ARC16.8.1; §ARC16.2.2), so the refusal cannot wait to learn it. The two outcomes that write nothing, `Unchanged` and `TakenDown`, leave the row as it is when refused, which is what the owner's ruling has a live reaction do (*A changed reaction*). On the editorial arm, the refusal comes before the arm's two pair probes (rule 5), so a pair a row already holds is refused too, where the arm would have answered `AlreadyApproved`, `AlreadyPending` or `OverlapsExisting`. Nothing is written either way.
8. **The refusal is a validation failure, answered `400`.** It is `InvalidAssociationOrchestrationException`, which the service maps to `AssociationOrchestrationValidationException`, and `AssociationsController`'s shared mapping answers that `400` (`Backend/Controllers/AssociationsController.md`), unchanged. Its message names the endpoint, `A` before `B` where both are rejected: *"The {A|B} endpoint is rejected, so it cannot be paired."* The caller may see the row, so naming it discloses nothing (rule 4). On the `Association-Adding` door the refusal writes nothing, publishes no fact and records no `ProcessedEvents` row, as every refusal on that door does.
9. **No page changes.** No page sends a pairing with a `Tag` or `BibleReference` endpoint today. The only caller of the React write is a content item's reaction (`useContentItemEngagement.ts`), whose far end is a `Reaction`. The post pages answer a suggestion with a *coming soon* notice (`UI/Pages/PostDetail.md` §6 items 17 and 18), and a passage's reaction bar records nothing (#700).
10. **"A rejected item" is a rejected tag or Bible reference.** The owner's sentence *"It MUST also block the creation of any associations on a rejected item"* follows the one that names *"a tag or bible reference"*, and restates the ruling of 2026-10-26 (*The rulings*). A rejected endpoint of another type, such as a content item, a link, a comment or a reaction, is not refused by this feature.
11. **#1326's recovery carries the refusal.** #1326, which designs the recovery of a soft-deleted item, was held until this design amended it (*The moderated restore*). #1352 amends its deliverable 5, beside the association whose endpoint is still deleted, so that its recovery of a soft-deleted association refuses a pair whose tag or Bible reference is `Rejected`. Where the recovery reads the endpoint, and on which layer, is #1326's design, under rule 4: a read the recovering caller's visibility filters may decide which refusal, never whether.
12. **A door that does not resolve the endpoint under the caller's envelope refuses through its own read.** The suggestion routes find a tag by name and a reference by `USFM`, through reads that see rows the caller cannot (#1356, *A tag the reader cannot see*). Each refuses a rejected row through that read, without disclosing it (§SEC14.5 rules 1 and 2), and #1356 and #1357 design how.

## User stories

| User story | Level |
| --- | --- |
| [Backend/Orchestrations/AssociationOrchestrationService.RejectedEndpoints.md](Backend/Orchestrations/AssociationOrchestrationService.RejectedEndpoints.md) | orchestration — `IAssociationOrchestrationService`, the refusal in the upsert's write flow and its two pins |

The component already has a user story document under the Likes feature, so this feature's work is in a second document beside it (`Design/design.md`, *A component that a second feature adds operations to gets a second user story document*).

**Layer placement.** The refusal needs the endpoint row, a `Tag` or a `BibleReference`, beside the `Association` being written: two entity types, which is §ARC12.1 rule 2's orchestration. The orchestration already reads that row, under the caller's envelope, to resolve the endpoint. Placed in the association foundation, the rule would make it read a second entity. Placed in the controller, it would put a rule in an exposer, which §EVN13 rule 3 and `the-standard-exposers` keep thin, and the `Association-Adding` door would walk past it.

**No dependency, no operation and no deviation.** `AssociationOrchestrationService` already holds `ITagService` and `IBibleReferenceService`, and this feature adds neither a dependency nor a member: it changes what two existing members refuse, and pins a third. The service's count of seven entity services is the Likes user story's approved deviation (`Backend/Orchestrations/AssociationOrchestrationService.md`, *Deviations* 1), which names those seven and the members this feature changes. Nothing here is argued from it, and nothing here needs a deviation of its own.

## Entity count, events and storage

- **Entities.** Unchanged. The write flow already reads `Association`, both endpoints and, for a content item host, the `ContentItem`. The refusal reads the status of an endpoint row the flow has already read.
- **Events.** None new. A refusal publishes nothing, and the facts the doors publish when they write are unchanged.
- **Storage.** None. No schema, no index and no migration.

## Risks

- **Reversible throughout.** No schema, no event and no migration. Taking the refusal out restores what an owner and a review role could pair before.
- **The refusal narrows what an owner and a review role may pair.** Before it, either could pair a rejected tag or reference they could see. No page sends such a pairing (rule 9), so no page changes what it shows.
- **The existing tests stub endpoints at `Draft`.** The orchestration's tests resolve a tag or reference as `new Tag { Id = … }` or a mock returning one, whose `ApprovalStatus` is `Draft`, the enum's zero value (`ApprovalStatus.cs`), so no existing test reaches the refusal. The `Rejected` cases in `AssociationOrchestrationServiceTests.Upsert.Logic.cs` are the pairing's own status, answered `AlreadyPending`, and no endpoint's.
- **A rejection reset by an administrator lifts the refusal.** §APR8.6 HR-4's override puts a decided round back to `Submitted`, and the row follows it (§APR9.8), so the refusal reads the status the row holds when the pairing is asked for, and no other.

## Out of scope

- The suggestion routes, and their own refusal of a rejected row (rule 12): #1356 for a tag, #1357 for a Bible reference.
- Every removal (rule 3), and every pairing that already exists when its tag or reference is rejected.
- The approval process. Approving or rejecting an association whose tag or reference is rejected creates, restores and changes no pairing, and the owner ruled the approval process separate from the submission (#1352, *The requirement*).
- A rejected endpoint of another type (rule 10).
- §ARC12.3.1 rule 5, a soft-deleted endpoint, which these doors still answer not-found. Whether the owner's ruling on a soft-deleted tag, that a suggestion *"can be undeleted"* and paired, reaches these doors is open on #1356 (its open question 2).
- #1326's design, beyond the amendment rule 11 names.

## Open questions

None. Rule 10's reading of *"a rejected item"* is the owner's to widen, and a widening would be a change to this feature.
