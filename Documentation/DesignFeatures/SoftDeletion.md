# Soft deletion
Epic: [INTENT.md](../../INTENT.md)
Inherits: §SEC14.5 rules 1–3, 6 and 7, §SEC14.6 rules 1–4, §SEC14.6.1, §SEC14.7 posture A rules 1 and 3, posture A′ rule 4, §EVN4, §APR9.7.1, §APR9.7.2 rule 1, §APR9.7.5 (*A retracted approval is closed to writes*), §APR9.7.6, §APR9.7.7 rules 6 and 7, §APR9.9, §ARC12.3.1 shared rule 9, §DOM3.4.1, §DOM4.10
Mockups: none. No page changes: an edit of a soft-deleted item is answered not found, as an edit of a missing item already is, and no read answers a soft-deleted item to any caller (§SEC14.5 rule 3), so no page offers one for editing.
Design tasks: #1325, which designs what this document holds today: no edit reaches a soft-deleted item. #1326 (recovering a soft-deleted item), #1327 (reinstating a soft-deleted approval round) and #1329 (unpublishing a row when it is removed) add their parts to this document as each is designed.

An item is soft-deleted when its removal sets `IsDeleted` and stamps who removed it, when and why (§EVN4). The row stays, every read hides it from every caller (§SEC14.5 rule 3), and its approval round is left as it was (§APR9.7.6). This feature settles what may still happen to such a row.

## Problem

When this design was written (2026-10-26, `main` at d8352b2d), the general modify of each of the seven soft-deletable items with a foundation refused only a missing row. Each `DoModify<Entity>Async` reads the stored row through `Select<Entity>ByIdAsync`, refuses it where it does not exist (`ValidateStorage<Entity>`), asks the write-permission gate and then the terminal-status refusal, and reads `IsDeleted` nowhere (`Glory2Him.Core/Services/Foundations/<Entity>s/<Entity>Service.cs`). So the owner of a removed item, or the publisher tier, could edit it on a route that binds the foundation — `PUT api/Tags`, `PUT api/Reactions`, `PUT api/Comments` or `PUT api/BibleReferences` — or through a signed `<Entity>-Modifying` request. The write kept the row deleted, because `EnsureOtherAuditValuesRemainsUnchangedOnModifyAsync` copies the four deletion fields back from the stored row (`Clients/SecurityClients/G2H.Security.Client/Services/Foundations/Audits/AuditService.cs`). But it published `<Entity>-Modified`, which the approval workflow hears with the Modified flow (§APR9.7.4), so an edit of a taken-down item reached its round. §APR9.7.2 rule 1 holds that every item service refuses to write a soft-deleted row, and rests the approval resolution's want of a visibility probe on it. The owner ruled that no edit reaches a soft-deleted item.

## The rulings

**The owner ruled on 2026-10-22, answering QA's fourth round on design PR #1274** (https://github.com/Glory2Him/Glory2Him.Core/pull/1274#issuecomment-6328784225). QA had found the confidence reads' modify criteria comparing a soft-deleted row, as each item's general modify writes one, while §APR9.7.2 rule 1 has every item service refuse to. The owner's words:

> Editing a soft deleted item must be refused for everyone.
>
> I would expect a different method "RecoverSoftDelete" that only administrators / publishers have access to. This method must only allow it to be undeleted, no other fields can be changed in this method. This "RecoverSoftDelete" method MUST trigger an update event so that an approval round can take place as content could have been deleted for a specific reason.

The first sentence is rule 1 of this document, designed under #1325. The rest is the recovery's, designed under #1326.

**The owner ruled on 2026-10-26, answering the two questions this design raised** (#1325 deliverable 4). Two writes built today edit a soft-deleted row, and each was put to the owner before this design ruled on it:

- **The publication swap's unpublish.** Asked *"When a newer version of a content item or link is approved, the publication swap clears IsPublished and PublishDate on the group's previously published row, even when that row was soft-deleted (removed while published). Is that write an edit that your 2026-10-22 ruling ("Editing a soft deleted item must be refused for everyone") refuses?"*, the owner chose the option put to them as *"No, keep it (Recommended) — Leave the unpublish outside the ruling. It only clears publication on a row every read already hides. It publishes -Unpublished, not -Modified, and opens no approval round, and §APR9.7.6 rule 1 wants a removed row unpublished anyway. Refusing it either fails the promotion or leaves the tombstone flagged as published."* (rule 15).
- **A reader's revive of their own withdrawn reaction.** Asked *"A reader withdraws their own reaction and then reacts again. Today their withdrawn row is revived: the same reaction just clears the deletion (Association-Restored, no new round), and a different reaction clears it and repoints the row in one write (Association-Repointed, back through approval). Does the no-edit ruling, or the RecoverSoftDelete ruling (admins/publishers only), reach either arm?"*, the owner chose the option put to them as *"Keep both (Recommended) — Neither ruling reaches a reader's own reaction. It is not a contribution (§APR9.9 rule 6), re-giving revives it (§DOM4.10 rule 6), and a moderator takedown is already never revived. Both arms stay as built."* (rule 15). This is the question #1325 and #1326 share (#1326, *What exists today* item 2 and deliverable 9), answered under both rulings, so #1326's design reads it here.

## Business rules

1. **No edit reaches a soft-deleted item** (the owner's ruling of 2026-10-22, *The rulings*). The general modify of every soft-deletable item with a foundation — `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment`, `BibleReference` and `Association` — refuses a stored row whose `IsDeleted` is `true`, on its direct path and on its `<Entity>-Modifying` door, whoever the caller. A refused modify writes nothing, publishes no `<Entity>-Modified` and records no processed event, so the approval workflow hears no edit of a taken-down item. Rules 4–10 build it, one item each. The refusal reads `IsDeleted` off the stored row the modify has already read through `Select<Entity>ByIdAsync`, never off a caller-facing read's not-found: identity travels on the envelope, and an identity-filtered read never decides an invariant (§SEC14.6; root `CLAUDE.md`, *Non-negotiables*).

2. **It answers not found, as a missing row is answered, and logs why.**
   - **The answer** is `NotFound<Entity>Exception` with the message a missing row is given, `"<Noun> not found with id: {id}."`, so what reaches the caller — the exception, its message and, on a route, its status code — is the missing row's, word for word (§SEC14.5 rules 1–3). The modify's own `TryCatch` wraps it as `<Entity>ValidationException` and logs that as an error, as it does for a missing row. The four controllers that bind a foundation's modify answer it `404 Not Found`, as they answer a missing row (`PUT` in `TagsController.cs`, `ReactionsController.cs`, `CommentsController.cs` and `BibleReferencesController.cs`, under `Websites/Glory2Him.WebApp/Controllers/`).
   - **The log** is written before the throw, at information level, as a state-based miss (§SEC14.5 rules 6 and 7), in the form each item's read by id already logs its soft-deleted miss: `"<Noun> modify denied. <Noun> {id} is soft-deleted; reported to the caller as not found."` The noun is the read's: `Content item`, `Link`, `Tag`, `Reaction`, `Comment`, `Bible reference` and, for an association, `Content item association` (`DoRetrieve<Entity>ByIdAsync` in each `<Entity>Service.cs`).

3. **It is asked after the write-permission gate and before the terminal-status refusal.** `DoModify<Entity>Async` asks, in order, the contribution gate, the input's validation, the stored row's existence, the write-permission gate `ValidateUserCanModifyStorage<Entity>Async`, and the terminal refusal `ValidateStorage<Entity>IsNotTerminal` (§ARC12.3.1 shared rule 9). The refusal sits between the last two.
   - **After the gate**, as the remove asks its already-removed return after its gate (`DoRemove<Entity>ByIdAsync`) and the approval's modify its own refusal after its gates (§APR9.7.5, *A retracted approval is closed to writes*). A caller the gate refuses is answered unauthorised on a soft-deleted row as on a live one, so a caller who may not touch the row learns nothing of its deletion state.
   - **Before the terminal refusal**, because a soft-deleted row is not found to every caller (§SEC14.5 rule 3), and the terminal refusal names the stored status in its message (`"<Noun> cannot be modified from status Approved."`). Asked first, it would report the status of a row the caller is answered does not exist. So a soft-deleted `Approved` or `Rejected` row is answered not found, and the terminal refusal is asked of live rows alone.
   - **Every caller the gate admits is refused.** Each gate admits the row's owner, its `CreatedBy`, and the publisher tier (§SEC14.7 posture A rule 3): the global `Publishers` and `Administrators`; the item's own `<Entity>-Publishers`; on a content item, `ContentItem-%ContentType%-Publishers` of the stored type; and on an association, a publisher role matching either endpoint (`HasPublisherRoleForAssociation`, `AssociationService.Transitions.Validations.cs`). Rule 1 refuses each of them.
   - **The system identity is refused before the refusal is asked.** It carries no role: `CreateSystemAsync` and `CreateElevatedAsync` both drop them (`IEventEnvelopeBroker.cs`). On its own account its subject is `SystemIdentity.UserId`, `"system"` (`Glory2Him.Core/Models/Securities/SystemIdentity.cs`), which owns no item row: no item is added under a system envelope, and the seeds stamp `"system-seed"` or a demo contributor (`Websites/Glory2Him.WebApp/Data/ReactionSeedData.cs`, `ContentItemSeedData.cs`). So every gate answers it unauthorised, on a soft-deleted row as on a live one. An elevated context carries the deciding person's subject (§SEC14.6.1). The workflow mints one only for its own write of a decided approval row, `IApprovalWorkflowService.ModifyApprovalAsync` under `WorkflowAttribution.DecidingCaller` (`ApprovalService.Workflow.cs`), and sends its `-Approving` commands on the system's own account (`PublishCommandAsync`, `ApprovalOrchestrationService.Decisions.cs`), so no elevated context reaches a general modify (rule 13). A context any gate admits, whoever it names, is refused by rule 1.

4. (#1330) **`ContentItem`'s general modify refuses a soft-deleted row**: `IContentItemService.ModifyContentItemAsync` (`Backend/Foundations/ContentItemService.SoftDeletion.md §1`), and its `ContentItem-Modifying` door through the body they share, which §2 pins.

5. (#1332) **`Link`'s general modify refuses a soft-deleted row**: `ILinkService.ModifyLinkAsync` (`Backend/Foundations/LinkService.SoftDeletion.md §1`), and its `Link-Modifying` door through the body they share, which §2 pins.

6. (#1334) **`Tag`'s general modify refuses a soft-deleted row**: `ITagService.ModifyTagAsync` (`Backend/Foundations/TagService.SoftDeletion.md §1`), and its `Tag-Modifying` door through the body they share, which §2 pins.

7. (#1336) **`Reaction`'s general modify refuses a soft-deleted row**: `IReactionService.ModifyReactionAsync` (`Backend/Foundations/ReactionService.SoftDeletion.md §1`), and its `Reaction-Modifying` door through the body they share, which §2 pins.

8. (#1338) **`Comment`'s general modify refuses a soft-deleted row**: `ICommentService.ModifyCommentAsync` (`Backend/Foundations/CommentService.SoftDeletion.md §1`), and its `Comment-Modifying` door through the body they share, which §2 pins.

9. (#1340) **`BibleReference`'s general modify refuses a soft-deleted row**: `IBibleReferenceService.ModifyBibleReferenceAsync` (`Backend/Foundations/BibleReferenceService.SoftDeletion.md §1`), and its `BibleReference-Modifying` door through the body they share, which §2 pins.

10. (#1342) **`Association`'s general modify refuses a soft-deleted row**: `IAssociationService.ModifyAssociationAsync` (`Backend/Foundations/AssociationService.SoftDeletion.md §1`), and its `Association-Modifying` door through the body they share, which §2 pins.

11. **`Attachment` is left out, because it has no general modify.** It implements `IApproval` and is soft-deletable (§DOM5.6), but it has no service, so no modify and no door to refuse in (§ARC12.3 entry 12, *future*; §SEC14.7 posture A, *"when implemented"*). The owner's ruling reaches every item, so the design of its foundation gives its general modify rule 1's refusal when that service is designed.

12. **What reaches each general modify, and what it refused when this design was written.** Outside the tests, these call the seven (rule 13):

    | Caller | What it reaches | A soft-deleted row at d8352b2d |
    | --- | --- | --- |
    | `IContentItemProcessingService.ModifyContentItemAsync` and its `ContentItemProcessing-Modifying` door, served at `PUT api/ContentItems` | `IContentItemService.ModifyContentItemAsync`, for an in-place edit (`ModifyContentItemInPlaceAsync`) | **Refused, and stays so.** It reads the row through the foundation's read by id, which answers a soft-deleted row not found to every caller, and `ValidateCurrentContentItemIsModifiable` refuses one as not found too (`ContentItemProcessingService.cs`, `ContentItemProcessingService.Validations.cs`). An edit of a terminal row forks a new version, an add, and reaches no general modify. |
    | `ILinkProcessingService.ModifyLinkAsync` and its `LinkProcessing-Modifying` door, served at `PUT api/Links` | `ILinkService.ModifyLinkAsync`, for an in-place edit | **Refused, and stays so**, the same way (`LinkProcessingService.cs`, `LinkProcessingService.Validations.cs`). |
    | `TagsController`, `ReactionsController`, `CommentsController` and `BibleReferencesController`, `PUT api/<Entity>s` | the foundation's modify directly | **Written.** Rules 6–9 refuse it, and the route answers `404` (rule 2). |
    | `IAssociationOrchestrationService.ModifyAssociationAsync`, which no route serves | `IAssociationService.ModifyAssociationAsync`, after authentication and the global block (`AssociationOrchestrationService.Writes.cs`) | **Written.** Rule 10 refuses it, and the orchestration answers the refusal as its dependency validation, as it answers a missing row (`AssociationOrchestrationService.Exceptions.cs`). |
    | Each foundation's `<Entity>-Modifying` door, `OnModifying<Entity>Async` | the body its direct path runs, `DoModify<Entity>Async` | **Written.** Rules 4–10 refuse it in that body, and each door's section pins it. |

13. **Nothing in the solution writes a soft-deleted row through a general modify, beyond a caller who names one.** Rule 12's callers are the only ones outside the tests, and no service publishes a request on an `<Entity>-Modifying` address: each foundation's door publishes there only its own reply (`<Entity>Service.Substrate.cs`). The approval workflow's sync of a decision and its reset of a decided round write an item only through `-Approving` commands (`PublishEntityApprovalCommandAsync`, `Glory2Him.Core/Services/Orchestrations/Approvals/ApprovalOrchestrationService.Decisions.cs`), which reach the approval transition, through the processing tier's approval on a content item and a link, and never a general modify; the transition refuses a soft-deleted row (rule 14). So, until its item's refusal lands (rules 4–10), a general modify writes a soft-deleted row only where its caller sends one: the owner or the publisher tier, on a route that binds a foundation, or on a door. Nothing is moved off a general modify, and nothing changes but the refusal. The removal's demotion (§APR9.7.6 rule 1) is not built — each `DoRemove<Entity>ByIdAsync` writes the deletion fields alone, and no removal writes `IsPublished` — so this design checks no such path, and the demotion is #1329's to design.

14. **What each other write does with a soft-deleted row today.**

    | Write | On | A soft-deleted row |
    | --- | --- | --- |
    | The approval transition, `Transition<Entity>ApprovalAsync`, and its `<Entity>-Approving` door | all seven | **Refused** as not found, through `LoadTransitionTargetAsync` (§APR9.7.6 rule 3). |
    | The submit, `Submit<Entity>ByIdAsync`, and its door | six; an association has none | **Refused** the same way. |
    | Set-confidence | built on an association (`SetAssociationConfidenceAsync`); designed for the six (`Backend/Foundations/<Entity>Service.BereanReview.md §3`) | **Refused**: the association's loads through `LoadTransitionTargetAsync`, and the six's design refuses a missing or soft-deleted row as not found. |
    | Sort, `SortAssociationAsync` | an association | **Refused**, through `LoadTransitionTargetAsync`. It writes its target alone, never its anchor. |
    | Set-scope, `SetAssociationScopeAsync` | an association | **Refused**, through `LoadTransitionTargetAsync`. |
    | Set-default, `SetAssociationDefaultAsync` | an association; designed, not built (§DOM4.9 rule 4) | **Refused** as a target (*"or is deleted"*). It clears the host's same-purpose siblings in the same save, and §DOM4.9 rule 4 keeps that clear to live siblings, since clearing a soft-deleted one would edit it (rule 1). `UX_Associations_DefaultPurpose` counts live rows alone (§DOM4.9 rule 6), so a soft-deleted sibling never blocks the target. A recovered sibling that still carries `IsDefault` is a collision #1326 weighs (its deliverable 5). |
    | The personal upsert, `UpsertPersonalAssociationAsync` | an association, a reader's own reaction | **Written, by the owner's ruling (rule 15).** Its created arm inserts, its unchanged arm writes nothing, and a row a moderator took down is never revived: its taken-down arm writes nothing (§DOM4.10 rule 7). Its restored and repointed arms write the reader's own withdrawn row. The first clears `IsDeleted`, `DeletedBy` and `DeletedWhen` and publishes `Association-Restored`; the second does the same, repoints the row in the same write and publishes `Association-Repointed` (§DOM4.10 rules 6–8; `AssociationService.PersonalUpsert.cs`). |
    | The publication swap's unpublish, `UnpublishContentItemByIdAsync` and `UnpublishLinkByIdAsync`, and the processing tiers' approval that calls them, `OnApprovingContentItemAsync` and `OnApprovingLinkAsync` | a content item and a link | **Written on purpose, by the owner's ruling (rule 15).** The unpublish reads its row without `LoadTransitionTargetAsync`. On a tombstone the swap's unfiltered probe finds, it clears `IsPublished` and `PublishDate`, stamps the modify audit values and publishes `<Entity>-Unpublished`, which nothing hears (§EVN18; `ContentItemService.Transitions.cs`, `LinkService.Transitions.cs`). The approval it serves then transitions its own target, which refuses a soft-deleted row. |
    | The remove, `Remove<Entity>ByIdAsync`, and its door | all seven | **Not written**: an already-removed row is answered as stored, after the gate, with no write and no fact (`DoRemove<Entity>ByIdAsync`). Whether a re-remove ever demotes such a row is #1329's (its deliverables 6 and 7). |
    | The hard remove, `HardRemove<Entity>ByIdAsync`, and its door | all seven | **Deleted, not edited**: `Administrators` only, it deletes the row and writes none of its fields (§APR9.7.1 rule 7). |
    | The add, the version fork, and the association's editorial upsert | the add on all seven; the fork on a content item and a link; the editorial upsert on an association | **Not written**: each inserts a row. The editorial upsert never inserts past a soft-deleted row on its pair, and answers `AlreadyPending` (`AssociationOrchestrationService.cs`). |

15. **Two writes write a soft-deleted row by the owner's ruling, and stay as built** (2026-10-26, *The rulings*).
    - **The publication swap's unpublish.** It is the flow half of §APR9.7.6 rule 1's defence in depth: until the removal's demotion is built (#1329), a row removed while published keeps `IsPublished`, and the swap clears it, with its `PublishDate`, once a later version of the group is promoted (§APR9.7.7 rule 7). Its `-Unpublished` has no subscriber and opens no round, so §APR9.7.2 rule 1's reason holds of it: a takedown still produces no `-Added`, `-Modified` or `-Submitted`.
    - **A reader's revive of their own withdrawn reaction, both arms.** A reader's own reaction is not a contribution (§APR9.9 rule 6), and re-giving revives it (§DOM4.10 rule 6). Each arm revives only a row its reader withdrew, never a takedown (§DOM4.10 rule 7), and writes it live before its fact goes out, so its `Association-Restored` or `Association-Repointed` describes a live row, and §APR9.7.2 rule 1's reason holds of it too. The recovery ruling does not reach it either, so #1326's recovery leaves both arms as they are. Which deletion fields a revive clears stays #1328's, and whether it republishes a reaction its withdrawal demoted stays #1329's (its deliverable 4).

    So §APR9.7.2 rule 1's *"every one of those services refuses to write a soft-deleted row"* is untrue of these two writes, and stays untrue of them. The rule's record says so (*The record*).

16. **A tombstone holds no published slot.** Each versioned entity's published-slot index is filtered `IsPublished = 1 AND IsDeleted = 0`, declared once for all three (`Glory2Him.Core/Brokers/Storages/Sql/StorageBroker.PublishedSlot.Configurations.cs`; §DOM3.4.1). So a soft-deleted row never holds its group's slot, even while it still carries `IsPublished`, and cannot block a later version's promotion. This design rewrites what the design said otherwise: §APR9.7.7 rule 7's account of the index, of the incumbent probe and of the partial failure, and §ARC12.4.1 rule 10's account of the index. The code says otherwise in comments, summaries and test names, in five components, and rules 17–21 rewrite them, one component each. A text that says a tombstone may still carry `IsPublished` is true until the removal's demotion is built (#1329), and stays. So does a text outside those five that speaks of a live incumbent holding the slot, as the subscription registration's, the two controllers' and the workflow's do (`EventSubscriptionRegistration.cs`, `ContentItemsController.cs`, `LinksController.cs`, `ApprovalOrchestrationService.Decisions.cs`).

17. (#1344) **The storage broker says that a tombstone holds no slot**: the summaries of its published-row reads, and the integration tests that prove them (`Backend/Brokers/StorageBroker.SoftDeletion.md §1`).

18. (#1346) **`ContentItemService` says so**: its unpublish's and its probe's comments and summaries, its collection read's summary, and its tests (`Backend/Foundations/ContentItemService.SoftDeletion.md §3`).

19. (#1347) **`LinkService` says so**, as rule 18 has it of a content item (`Backend/Foundations/LinkService.SoftDeletion.md §3`).

20. (#1348) **`ContentItemProcessingService` says so**: its published read's comment, its publication swap's comments, and the swap's tests (`Backend/Processings/ContentItemProcessingService.SoftDeletion.md §1`).

21. (#1349) **`LinkProcessingService` says so**, as rule 20 has it of a content item (`Backend/Processings/LinkProcessingService.SoftDeletion.md §1`).

## User stories

In build order (*What ships together*). Each names this document as its parent.

| Story | Level | Sections |
| --- | --- | --- |
| [Backend/Foundations/ContentItemService.SoftDeletion.md](Backend/Foundations/ContentItemService.SoftDeletion.md) | foundation | the modify's refusal (§1) and its door's (§2); what its code says of the published slot (§3) |
| [Backend/Foundations/LinkService.SoftDeletion.md](Backend/Foundations/LinkService.SoftDeletion.md) | foundation | the modify's refusal (§1) and its door's (§2); what its code says of the published slot (§3) |
| [Backend/Foundations/TagService.SoftDeletion.md](Backend/Foundations/TagService.SoftDeletion.md), [ReactionService.SoftDeletion.md](Backend/Foundations/ReactionService.SoftDeletion.md), [CommentService.SoftDeletion.md](Backend/Foundations/CommentService.SoftDeletion.md), [BibleReferenceService.SoftDeletion.md](Backend/Foundations/BibleReferenceService.SoftDeletion.md) | foundation | the modify's refusal (§1) and its door's (§2) |
| [Backend/Foundations/AssociationService.SoftDeletion.md](Backend/Foundations/AssociationService.SoftDeletion.md) | foundation | the modify's refusal (§1) and its door's (§2) |
| [Backend/Brokers/StorageBroker.SoftDeletion.md](Backend/Brokers/StorageBroker.SoftDeletion.md) | broker | what its published-row reads' summaries and tests say of the published slot (§1) |
| [Backend/Processings/ContentItemProcessingService.SoftDeletion.md](Backend/Processings/ContentItemProcessingService.SoftDeletion.md), [LinkProcessingService.SoftDeletion.md](Backend/Processings/LinkProcessingService.SoftDeletion.md) | processing | what its code says of the published slot (§1) |

The sections of rules 17–21 rewrite code text and add no operation. Each has a numbered section in its component's story so that its task has a section to name, as a storage story gives its model and its migration one.

## Layer placement

1. **Each item's foundation refuses**, in the body its direct path and its door share. The rule reads the row its own modify has just read and asks no other entity, so it is the foundation's (§SEC14.6 rule 3), and asked there it reaches every caller whichever layer they come through: an exposer may bind the foundation directly, and a rule enforced only above it is not enforced (§SEC14.6 rule 1; §APR8.6.1). One entity per service, and no new dependency.
2. **No processing tier, orchestration, controller or page changes what it does.** The processing tiers already refuse a soft-deleted row before they reach the foundation (rule 12), and keep doing so (§SEC14.6 rule 2). The controllers and the association orchestration pass the foundation's answer on as they pass a missing row's.
3. **Code text changes at three levels**, the storage broker, the two foundations and the two processing tiers, one component per rule and per task (rules 17–21), since a user story never spans levels.

## Entity count, events and storage

- **Entities.** One per service: each foundation reads and refuses its own row.
- **Events.** None new, and none changed. A refused modify publishes nothing, so §APR9.7.4's Modified flow no longer hears an edit of a soft-deleted item. Each door's reply on a refusal is the shared body's exception, as on a missing row.
- **Storage.** None: no table, column, index or migration, and no seed consequence.

## The record

- **§APR9.7.2 rule 1 records the owner's ruling**, dated and in their words, in a paragraph after its *Resolution carries no such probe*, and says where the rule's *"every one of those services refuses to write a soft-deleted row"* does not hold: on the general modify of an item until that item's rule here (rules 4–10) is built, and on the two writes of rule 15, by the owner's ruling. Its words are written to stay true as each item's refusal lands, so only the last refusal to land rewrites them. The *Resolution carries no such probe* paragraph stands as it is (*Signed-off tasks this design touches*).
- **Each refusal task records its build** on its own rule here: the rule loses its tag and stays a rule, with its number (`Design/design.md`, *A gap that is built stays in its list*, point 1). The doors' pins record nothing, since their direct path's build is the rule's.
- **The lines each build makes untrue, and who rewrites them** (`Design/design.md`, *A gap that is built stays in its list*, points 4 and 5). A search of the design and the code for text that says the general modify's refusal is not built, or that a general modify writes a soft-deleted row, found these three, and only these:
  - `ConfidenceReads.md` *The rulings*: *"as each item's general modify writes one today, refusing only a missing row"*, and *"That refusal is not yet built."*
  - `ConfidenceReads.md` *Risks*: *"That refusal is not yet built (#1325): each `DoModify<Entity>Async` refuses only a missing row today, so until it is, the modify writes and answers a soft-deleted row, and a copy that dropped `IsDeleted` would answer one as live. No test of this sub-feature modifies a soft-deleted row, since it would pin a write the ruling refuses."*
  - §APR9.7.2 rule 1's record, above.

  The first two are untrue of `ContentItem` from the moment rule 4 is built, so rule 4's task rewrites both into words that stay true as each later item lands, pointing at rules 4–10 here. Rule 10's task, the last refusal to land, rewrites them again, and §APR9.7.2 rule 1's record, to say that the refusal is built on all seven. Each task's criterion fixes the words. The open tasks whose bodies say the refusal is not yet built — among them the ConfidenceReads tasks of each item's modify (#1238, #1245, #1251, #1256, #1261, #1266 and #1270, in their *Constraints*) — are not lines of the repository. Each says that it neither builds the refusal nor waits for it, so each holds whichever lands first.

## What ships together, and in what order

Every merge to `main` deploys (§APR7.4 item 6). No stage needs a pull request that closes more than one task.

1. **Rule 4's task, #1330, `ContentItem`'s modify, lands first** among the refusals. Its build makes the two `ConfidenceReads.md` lines untrue, and it rewrites them into the words the later refusals keep true (*The record*).
2. **Rules 5–9's tasks** — `Link` (#1332), `Tag` (#1334), `Reaction` (#1336), `Comment` (#1338) and `BibleReference` (#1340) — each land after rule 4's, in any order among themselves.
3. **Rule 10's task, #1342, `Association`'s modify, lands last**, after each of rules 4–9's. It rewrites the record to say the refusal is built on all seven.
4. **Each door's pin lands after its own direct path** — #1331, #1333, #1335, #1337, #1339, #1341 and #1343 — since the door converges on the body its direct path changes, and its tests pass as soon as they are written.
5. **Rules 17–21's tasks — #1344, #1346, #1347, #1348 and #1349 — wait for none of them, nor for each other.** Each rewrites one component's code text. #1329's demotion task rewrites the same sentences where they say a soft delete never clears `IsPublished`, and whichever lands second rebases on the first's words (#1329, *Constraints*, *Text another design rewrites too*).
6. **Nothing here waits for another design or task.** The open tasks that change the same `DoModify<Entity>Async` bodies — each item's modify under the Berean review (`Backend/Foundations/<Entity>Service.BereanReview.md §2`, `Backend/Foundations/AssociationService.BereanReview.md §3`), each modify's answer under the confidence reads (`ConfidenceReads.md` rule 3) and the amender record (§APR8.6.3) — each add a step of their own to the same body, and whichever lands second rebases on the first. None of their tests modifies a soft-deleted row (`ConfidenceReads.md`, *Risks*), and none of the refusal's tests changes theirs.

## Signed-off tasks this design touches

- **§APR9.7.7 rule 7** changes in three places: its ordering paragraph's account of the index, its *The incumbent probe must not filter on `IsDeleted`* paragraph, and its partial-failure paragraph's *"the index would refuse it anyway"* (rule 16). Three open, signed-off tasks cite rule 7. Each moved back to `status: needs-scoping` before the design PR was pushed, and QA's task review signed each off again:
  - #1197 and #1199, whose criteria give the unpublish's code comments the words *"reads it without the transitions' load, which would refuse a soft-deleted row that still holds its publication (design §9.7.7 rule 7)"*. Those words stay true, since a tombstone can still carry `IsPublished`, and rule 7 still gives that reason. No criterion of theirs changes.
  - #1329, whose *Constraints* give rule 7's index claim to this design. No deliverable of it changes.
  - #1220 and #1224 cite §APR9.7.7 too, and stay signed off. #1220 cites its first bullet, on a round that requires no review, and #1224 the shared evaluation as a whole, which its story inherits. Neither relies on rule 7's account of the index, the probe or the partial failure, which is all of §APR9.7.7 that changes.
- **§ARC12.4.1 rule 10** changes its account of the index (rule 16). No open, signed-off task cites it.
- **§DOM4.9 rule 4** keeps set-default's sibling clear to live siblings (rule 14). No open task but #1325 cites §DOM4.9.
- **§APR9.7.2 rule 1** gains a paragraph, *The record*, and its *Resolution carries no such probe* paragraph stands word for word. Open, signed-off tasks that cite rule 1: the ConfidenceReads tasks of each item's modify (#1238, #1245, #1251, #1256, #1261, #1266 and #1270), for that paragraph's rule that the general modify refuses a soft-deleted row, whose refusal their *Constraints* already record as not yet built; #1225 and #1226, for the paragraphs on opening a round, which do not change; and #1326, #1327 and #1329, whose *Text another design rewrites too* name this record. None of them moves back for it: no paragraph any of them cites changes, and the record contradicts none of their criteria.
- **`ConfidenceReads.md`** does not change here. Rule 4's and rule 10's tasks rewrite its two lines when each is built (*The record*), and no task that cites them moves back for that (`Design/design.md`, *A gap that is built stays in its list*, point 5).
- **The seven `Backend/Foundations/<Entity>Service.ConfidenceReads.md`** each gain this feature's user story in their *Level* line's list of the component's other user stories. No section of them changes, so no task that cites one moves back.

## Risks

- **Reversible throughout.** No schema, no event and no migration: taking a refusal out lets the modify write a soft-deleted row again.
- **A stale page answers not found.** A page that loaded an item before it was taken down, and saves it after, is answered `404`, as it would be had the item been hard-removed meanwhile. No read answers a soft-deleted row (§SEC14.5 rule 3), so no page loads one to edit.
- **A redelivered refused request is refused again.** A refused door records no processed event, so a redelivery runs the body again and is refused the same way, writing nothing.
- **The terminal refusal's tests stand.** They modify live rows: every item's foundation filler pins `IsDeleted` at `false` (`ConfidenceReads.md`, *Risks*), so none of them reaches the new refusal, and a soft-deleted terminal row is the new tests' case alone.

## Out of scope

- **Recovering a soft-deleted item**: #1326.
- **Reinstating a soft-deleted approval round**: #1327.
- **The removal's demotion of a published row**, and whether a re-remove demotes one: #1329.
- **Which deletion fields a reader's revive clears**: #1328.
- **The workflow's own records and the settings entities**, which are not items (§APR9.9 rule 1). The approval round's own modify already refuses a soft-deleted row (§APR9.7.5).
- **What the confidence reads answer**: `ConfidenceReads.md`.

## Open questions

None. The two writes this design found editing a soft-deleted row were put to the owner and answered (*The rulings*).

## Deviations

None.
