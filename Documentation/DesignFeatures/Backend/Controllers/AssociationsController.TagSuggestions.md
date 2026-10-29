# Associations controller — tag suggestions
Parent: [TagSuggestions.md](../../TagSuggestions.md)
Level: exposer — `AssociationsController` (`Websites/Glory2Him.WebApp/Controllers/Associations/`), existing. Its other user stories are `Backend/Controllers/AssociationsController.md`, the Likes feature's, and `AssociationsController.AssociationReads.md`.
Inherits: §ARC17.2 (shape versus served literal), §ARC17.4, §EVN13 rule 3, §SEC14.5 rule 1, `Backend/Controllers/AssociationsController.md` (the controller's shape and its shared mapping), `the-standard-exposers`, TagSuggestions.md rules 7 and 9–11

The route of the tag suggestion. The controller keeps binding `IAssociationOrchestrationService` and nothing else (§EVN13 rule 3; `the-standard-exposers` ts-exposers-001), and holds no logic. This document is the single home of the route's literal and its response codes; §ARC17.4 tables its shape beside the other association routes and points here.

## 1. PostTagSuggestionAsync (needs issue)

`POST api/Associations/TagSuggestions`, through `[HttpPost("TagSuggestions")]` under the controller's `[Route("api/[controller]")]` (§ARC17.2), `[Authorize]`, the body bound `[FromBody] TagSuggestion` and handed to `SuggestTagAsync` (`Backend/Orchestrations/AssociationOrchestrationService.TagSuggestions.md §1`).

1. **`201 Created` when the result's status is `Created`**, and **`200 OK` when it is `AlreadySuggested` or `AlreadySuggestedByCaller`**, each with the `AssociationSubmissionResult` as the body: its status, and nothing else (TagSuggestions.md rule 7).
2. **An anonymous caller is refused `401` by the attribute**, before the orchestration is asked.
3. **The shared mapping applies with every arm** (`Backend/Controllers/AssociationsController.md`): a validation exception whose inner exception is `UnauthorizedAssociationOrchestrationException` is `401`, one whose inner exception is `NotFoundAssociationOrchestrationException` is `404`, and any other is `400`; a dependency validation exception whose inner exception is `AlreadyExistsAssociationException` is `409`, and any other is `400`; a dependency exception is `424`; a service exception is `500`. So the gates answer:
   - **`401`** — a caller holding the global `ReadOnly`, or a read-only role of the post or of tags (TagSuggestions.md rule 9, steps 1 and 4);
   - **`404`** — a post that does not exist, or that the caller may not see, the same answer for both (§SEC14.5 rule 1);
   - **`400`** — the suggestion's own fields, a rejected post, the facet gate, and a rejected or soft-deleted tag (steps 2, 5, 6 and 7);
   - **`409`** — the pair inserted by another caller between the probe and the insert (TagSuggestions.md rule 10);
   - **`424`** — a fault beneath the orchestration (TagSuggestions.md rule 11).
4. **The controller's summary**, which lists the routes it serves and why each departs from `the-standard-exposers` (`AssociationsController.cs`), gains this route, and the deviation below.

## Deviations

1. **ts-exposers-003 — *"POST endpoints must return 201 Created with the created resource in the body."*** §1 answers `201` only when a pairing was created, and `200` when the pair already existed; and its body is the result, a status, rather than the resource. **Why:** a suggestion whose pair already exists creates nothing (TagSuggestions.md rule 6), so `201` would say something that did not happen; and the row as the body would carry its author and its id, where the answer must tell nothing of a tag the caller cannot see (TagSuggestions.md rules 5 and 7). **Instead:** the status code says whether a pairing was created, and the body's status says which outcome was reached. **Approved** by the owner: the result rather than the row on 2026-10-26 (TagSuggestions.md, *Both deviations are signed off*), and `201` where something was created with `200` where the pair already existed on 2026-10-27 (TagSuggestions.md, *The routes answer `200` where the pair already existed*). `AssociationsController.md`'s *Deviations* 1 is §1 of that document's, for its route alone, and is not this deviation's reason.
