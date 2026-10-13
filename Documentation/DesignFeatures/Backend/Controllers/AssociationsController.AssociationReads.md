# Associations controller — association reads
Parent: [AssociationReads.md](../../AssociationReads.md)
Level: exposer — `AssociationsController` (`Websites/Glory2Him.WebApp/Controllers/Associations/`), existing
Inherits: §ARC17.2 (shape versus served literal), §ARC17.4, §EVN13 rule 3, §SEC14.5, §SEC14.7 posture A′ rule 7, `Backend/Controllers/AssociationsController.md` (the class and its shared mapping), `the-standard-exposers`, AssociationReads.md rules 1, 2 and 6–8

§ARC17.4's two reads on the association resource's exposer. The class, its binding to
`IAssociationOrchestrationService` alone and its shared exception mapping are
`Backend/Controllers/AssociationsController.md`'s. Each section here names the arms of that
mapping its action carries. Neither action holds logic: which rows a caller receives is decided
by the services beneath (AssociationReads.md rule 1).

**Each route ships after rule 7 is built on the foundation read beneath it**
(AssociationReads.md rule 2; §ARC17.4, *The reads*): §1 after
`Backend/Foundations/AssociationService.AssociationReads.md §1`, and §2 after that document's §2.

## 1. Get (#1124)

`GET api/Associations`, `[HttpGet]`, `[EnableQuery]`, `[AllowAnonymous]`, answering
`IAssociationOrchestrationService.RetrieveAllAssociationsAsync`. It is named `Get`, as every
controller's `[EnableQuery]` collection read is (`TagsController.Get`, `ContentItemsController.Get`).

1. **`200 OK` with the orchestration's queryable as the body, unenumerated**, so that
   `[EnableQuery]` composes over it and its options run in SQL with everything beneath it
   (§ARC17.4, *`[EnableQuery]` sits over the collection read*).
2. **It names no page size of its own.** `ODataPageSizeConvention` gives it the host's,
   `OData:PageSize` (AssociationReads.md rule 6).
3. **What each caller receives is the orchestration's answer**, and every row it answers has both
   endpoints visible to that caller (§SEC14.3 rules 3 and 4). An anonymous caller receives the
   public editorial rows, and no personal row. A signed-in reader also receives the editorial
   rows they created, at any status, and their own reactions. A caller in the review tier
   receives every live row (§SEC14.7 posture A rule 4, posture A′ rules 2 and 7).
4. **A `$filter` finds nothing the caller may not see.** A `$filter` on `userId` naming another
   reader finds none of that reader's reactions, for an anonymous caller and for a signed-in
   reader outside the review tier (§SEC14.7 posture A′ rule 7, *Why*).
5. **It carries the shared mapping's dependency and service arms alone**: a dependency exception
   is `424` and a service exception is `500`. The read raises neither validation family: it
   takes no input, and nothing beneath it refuses a caller, as `TagsController.Get` maps those two
   arms alone.

## 2. GetAssociationByIdAsync (#1125)

`GET api/Associations/{associationId}`, `[HttpGet("{associationId}")]`, `[AllowAnonymous]`, the
id bound from the route and handed to `IAssociationOrchestrationService.RetrieveAssociationByIdAsync`.

1. **`200 OK` with the association as the body.**
2. **Every miss is `404`, with the same body** (§SEC14.5 rule 1, *Indistinguishability*). The
   misses leave the orchestration as two families, and both carry the message *"Content item
   association not found with id: {associationId}."*:
   - a dependency validation exception whose inner exception is `NotFoundAssociationException`:
     no row, a row taken down, and a row the caller's posture hides, rule 7 included;
   - a validation exception whose inner exception is `NotFoundAssociationOrchestrationException`:
     a row with an endpoint the caller may not see.

   **The first is an arm the shared mapping does not have.** On this action a dependency
   validation exception whose inner exception is `NotFoundAssociationException` is `404`, and
   every other dependency validation exception is `400`.
3. **An empty id is `400`**: the orchestration refuses it as invalid, which is its validation
   exception with another inner exception.
4. **A dependency exception is `424` and a service exception `500`.** It has no `401` arm,
   because a read refuses no caller and answers not-found instead (ts-foundations-012), and no
   `409` arm, because it writes nothing.
5. **The literal routes keep their own actions.** `GET api/Associations/ReactionSummaries` is
   still answered by `GetReactionSummariesAsync`: ASP.NET Core's routing ranks a literal segment
   above a route parameter, so this route takes no `ReactionSummaries` request.
