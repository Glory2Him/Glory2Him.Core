# Associations controller
Parent: [Likes.md](../../Likes.md)
Level: exposer — `AssociationsController` (`Websites/Glory2Him.WebApp/Controllers/Associations/`), new
Inherits: §ARC17.2 (shape versus served literal), §ARC17.4, §ARC16.8 (*The route*), §ARC16.8.1, §EVN13 rule 3, §SEC14.5, `the-standard-exposers`

The association resource's exposer. It is built with §1's route (#728) and §3's (#730) — §ARC12.6's controller table records it as entry 14 — and that entry rules that its routes are added **by the work that needs them rather than all at once**: the Likes feature needs three, and adds them. The CRUD six stay unserved, and the two reads among them must not ship before §SEC14.7 posture A′ rule 7 is built (§ARC17.4, *The reads*).

It carries `[ApiController]` and `[Route("api/[controller]")]`, derives from `RESTFulController`, and takes `IAssociationOrchestrationService` and nothing else (§EVN13 rule 3: an entity's exposer binds its top-layer service; `the-standard-exposers` ts-exposers-001). It holds no logic: every rule is the orchestration's, and each action maps the orchestration's exception families to status codes the way the solution's other controllers do (`ContentItemSettingsController`).

The shared mapping for this controller's actions: a validation exception whose inner exception is `UnauthorizedAssociationOrchestrationException` is `401`, one whose inner exception is `NotFoundAssociationOrchestrationException` is `404`, and any other is `400`; a dependency validation exception whose inner exception is `AlreadyExistsAssociationException` is `409`, and any other is `400`; a dependency exception is `424`; a service exception is `500`.

## 1. PostAssociationAsync (#728)

`POST api/Associations`, `[Authorize]`, the body the association's two endpoints, bound `[FromBody] Association` and handed to `UpsertAssociationAsync` (§ARC16.8.1).

1. **`201 Created` when the result's status is `Created`**, and **`200 OK` for every other status** — `Restored`, `Repointed`, `AlreadyApproved`, `AlreadyPending`, `OverlapsExisting` — each with the `AssociationSuggestionResult` as the body: its status and the row's id, and never the row (§ARC16.8.1).
2. **An anonymous caller is refused `401` by the attribute**, before the orchestration is asked.
3. **The facet gate's two refusals are `400`**, each naming its switch (§ARC16.2.1, *each refusal is a validation failure … answered `400`*).
4. **An endpoint that does not exist, or that the caller may not see, is `404`**, the same answer for both (§SEC14.5 rule 1).

## 2. DeleteAssociationPairAsync (#729)

`DELETE api/Associations/Pair`, `[Authorize]`, the pair bound `[FromQuery] Association` and handed to `RemoveAssociationByPairAsync` (§ARC16.8.1, which is this route's single home and records its binding).

1. **`204 No Content` on both outcomes**, `Removed` and `NothingToRemove` alike, with no body (§ARC16.8.1).
2. **An anonymous caller is refused `401` by the attribute.**
3. **An editorial pair is `400`**, and an endpoint that does not exist or is not visible is `404`, as on §1.

## 3. GetReactionSummariesAsync (#730)

`GET api/Associations/ReactionSummaries`, `[AllowAnonymous]`, bound `[FromQuery] Guid[] contentItemIds` and handed to `RetrieveContentItemReactionSummariesAsync` (§ARC16.8, *The route*, which owns its literal).

1. **`200 OK` with the summaries**, one per answerable distinct id — an anonymous caller and a signed-in one alike, the viewer's two members `null` for the anonymous one (§ARC16.8, *What a signed-out caller receives*).
2. **A set that is empty, larger than 25 distinct ids, or carrying an empty id is `400`** (§ARC16.8, *The set, its bounds*).
3. **No `[EnableQuery]`**: any `$`-prefixed option is off the surface and changes nothing (§ARC16.8, *The route*).

## Deviations

Two, both from `the-standard-exposers`, and both approved by the owner on 2026-09-28 (*"I agree with this"*):

1. **ts-exposers-003 — *"POST endpoints must return 201 Created with the created resource in the body."*** §1 answers `201` only when a row was created, and `200` when the upsert revived, repointed or found the reader's reaction or the pair already in place; and its body is the result rather than the resource. **Why:** the upsert creates a row on one outcome of six, so `201` on the others would say something that did not happen; and §ARC16.8.1 forbids the row as the body — *"status and id and nothing else … because the row body would leak authorship"*. **Instead:** the status code says whether a row was created, and the body's status says which outcome was reached.
2. **ts-exposers-006 — *"DELETE endpoints must return 200 OK with the deleted resource."*** §2 answers `204` with no body. **Why:** ruled by §ARC16.8.1 — the end state the caller asked for is the same on both outcomes, a `404` for the second would make the withdrawal a probe for which rows exist, and the deleted row would leak authorship. **Instead:** `204` on both.
