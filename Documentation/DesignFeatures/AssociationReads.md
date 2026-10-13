# Association reads
Epic: [INTENT.md](../../INTENT.md)
Inherits: §ARC12.2.1 rule 7, §ARC12.5 entry 1, §ARC16.8 (*Where each shared rule lives*), §ARC17.2 (shape versus served literal), §ARC17.4, §DOM4.4, §DOM4.6 rules 1 and 3, §EVN13 rule 3, §SEC14.3, §SEC14.5, §SEC14.6, §SEC14.7 posture A′ rules 2, 5 and 7, §UI20.9.1, §UI20.9.2, §UI20.9.3
Mockups: none. The pages that show associations have their own.
Design task: #857

A page reads associations over HTTP in two ways: an entity's associations, and one association by
its id. This feature serves both, from the foundation's posture up to the React reads a page
calls.

## Problem

The two reads are built beneath the exposer. `IAssociationOrchestrationService` composes
§SEC14.3 rules 3 and 4 over the foundation's self-only filter and hands up an unenumerated
`IQueryable`, and its read by id answers a row the caller may not see as not-found (§ARC12.5
entry 1, #310). When this feature was designed (2026-10-13), no route served either, and none
may until §SEC14.7 posture A′ rule 7 is built (§ARC17.4, *The reads*): served before it, the
collection read would publish who gave every public reaction. Every page that shows an association waits on them: the admin post page and
the admin association page (`PostModeration.md`, *Reads and writes this feature builds on*),
the Bible reference page (#700), and the content item pages' association panels
(`Design/UI/Components/AssociationPanel.md §10 item 21`).

## Business rules

1. **Two reads, both open to every caller.** `GET /api/associations` and
   `GET /api/associations/{id}`, each `[AllowAnonymous]` (§ARC17.4, *The reads*). Which rows a
   caller receives is decided by the services beneath the route, never by the route.
2. **Rule 7 is built first, on both reads and both entry paths of the read by id.** A personal
   association is answered only to its owner, the account its `UserId` names, and to the review
   tier, whatever its approval and publication state (§SEC14.7 posture A′ rule 7). It is closed
   in the foundation's collection filter and its by-id read (*How it is closed*), and the read
   by id's event handler takes it through the body both paths share (§SEC14.6 rule 4). Each route
   ships only after rule 7 is built on the foundation read beneath it (§ARC17.4).
3. **The owner is matched ignoring letter case** (§DOM4.6 rule 3). The collection read compares
   in SQL, under the column's collation. The read by id compares the row it has read in C#,
   ordinally and ignoring case.
4. **A caller with no user id owns no personal association.** An anonymous caller, and a
   signed-in caller whose identity carries no user id, are nobody's owner: an editorial row's
   `UserId` is null (§DOM4.6 rule 2), and a caller term with no id would otherwise match it
   (`Backend/Foundations/AssociationService.md §4` rule 2 gives the same reason for the caller's
   own reactions).
5. **Rule 6 does not hold the reads back** (owner ruling 2026-10-13, under #857). §SEC14.3 rule 6
   is a named vacancy in the composite both reads go through (§ARC12.5 entry 1), and the reads
   ship without it. §ARC17.4 records what that serves until it is built.
6. **The collection read is queryable and capped.** `[EnableQuery]` composes over the
   orchestration's unenumerated `IQueryable` (§ARC17.4), so a `$filter` runs in SQL with
   everything beneath it. Each answer holds at most the host's page size, `OData:PageSize`, 50
   in `Websites/Glory2Him.WebApp/appsettings.json`, and does not say whether it was cut short
   (`Websites/Glory2Him.WebApp/Program.cs`, the comment above `AddOData`). So a read of every
   association of an entity asks page after page.
7. **The read by id answers one association, or not-found for every miss.** No row, a row taken
   down, a row the caller's posture hides, rule 7 included, and a row with an endpoint the
   caller may not see each answer `404` with the same body (§SEC14.5 rule 1, *Indistinguishability
   is a property of the outward answer*).
8. **Each read answers the row.** Both answer `Association` as the orchestration hands it up,
   every column included, and project nothing away: a projection is refused as the way rule 7 is
   kept (§SEC14.7 posture A′ rule 7, *How it is closed*). What a page reads from it is the page's.
9. **No read answers an association's approval id yet.** `Association` carries no `ApprovalId`.
   §APR7.4 item 6 designs the column on all eight `IApproval` implementors under #699, and it
   ships in stage 2, as one deployment across the eight, a build that has no task
   (`Design/UI/Components/ReviewPanel.md §10 item 12`, `(needs issue)`). Once that build adds the
   column to the row, both reads answer it under rule 8. Nothing here is built on the field
   ahead of it.
10. **An entity's associations are those holding it on either endpoint, by its effective id.**
    Canonical order puts an entity on A or on B by its type's name (§DOM4.4), so the read asks
    both: endpoint A of the entity's type with that effective id, or endpoint B of it
    (§DOM4.6 rule 1). An association held across a versioned entity's versions is keyed by its
    group id and one held to one version by that version's id (§DOM4.5 rule 1, §DOM4.6), so a
    page that needs both asks once for each
    (`Design/UI/Pages/ContentItemModerationDetailPage.md rule 2.30`).

## User stories

Each names this document as its parent. Every component here already has a user story document
under the Likes feature, so this feature's operations are in a second document beside each
(`Design/design.md`, *A component that a second feature adds operations to gets a second user
story document*).

| User story | Level |
| --- | --- |
| [Backend/Foundations/AssociationService.AssociationReads.md](Backend/Foundations/AssociationService.AssociationReads.md) | foundation — `IAssociationService`, rule 7 on its two reads |
| [Backend/Controllers/AssociationsController.AssociationReads.md](Backend/Controllers/AssociationsController.AssociationReads.md) | exposer — `AssociationsController`, the two routes |
| [UI/Brokers/AssociationBroker.AssociationReads.md](UI/Brokers/AssociationBroker.AssociationReads.md) | broker — `AssociationBroker` (React) |
| [UI/Foundations/AssociationService.AssociationReads.md](UI/Foundations/AssociationService.AssociationReads.md) | foundation service — `associationService` (React) |

**Layer placement.** Rule 7 needs the row's own columns and the caller, so it is a self-only term
and sits in the foundation, where rules 5 and 6 of the posture already sit (§SEC14.7 posture A′
rule 7, *How it is closed*). The routes bind the orchestration alone: a public read surface binds
the service that holds the composite (§SEC14.3, *Layer*), and an entity's exposer binds its
top-layer service (§EVN13 rule 3). **The orchestration does not change.** Its two reads compose
over the foundation's, so rule 7 reaches them through the foundation, and the orchestration
re-derives none of the posture (§SEC14.3, *The composite carries no caller term*). In the React
app, the broker carries each request and the foundation service writes the read's condition and
pages it (§UI20.9.3 rules 1 and 2).

## Entity count, events and storage

- **Entities.** Rule 7 reads `Association` alone. The routes add none: the endpoint entities the
  orchestration's composite reads are #310's, unchanged.
- **Events.** None new. `Association-RetrievingById` already reaches the read by id's handler,
  which takes rule 7 through the body it shares with the direct path. The collection read has
  no event path.
- **Storage.** None. The collection read's owner term compares `UserId` in SQL under the
  collation `Backend/Models/Association.md §1` pins (#1023).

## Risks

- **Reversible throughout.** No schema, no event and no migration. Rule 7 narrows what two reads
  answer, and taking it out widens them again.
- **Rule 7 narrows what the foundation's reads answer, and only one caller reads them.**
  Each read's one production caller is the orchestration's read of the same name
  (`AssociationOrchestrationService.Reads.cs`). The approval workflow reads an association's state
  through `IAccessBroker`, not through these reads, so a personal row hidden from a caller here is
  not hidden from the approval process.
- **The unit tests' random association is personal.** `CreateAssociationFiller`
  (`Glory2Him.Core.Tests.Unit/Services/Foundations/Associations/AssociationServiceTests.cs`) pins
  no `UserId`, so the filler draws one, and a drawn row is personal. Once rule 7 is built, a read
  test that expects a public row returned to a caller who is not its owner must make that row
  editorial. The foundation's two tasks say which tests.
- **A caller holding a global review role reads every reaction on an entity**, and a caller
  holding a scoped review role every reaction on an entity with an endpoint in its scope. The
  review tier sees the live rows its roles cover (§SEC14.7 posture A rule 4, posture A′ rules 2
  and 5), personal rows included (rule 2). So the React read of an entity's associations asks one
  page per 50 rows that caller may see, and on a post with many reactions an administrator's read
  asks many pages. Narrowing that read by the far end's type is not designed. No page asks for it
  yet.
- **A read that pages can meet a set that changes between its pages.** It pages by offset
  (rule 6), so a row added or removed between two of its requests shifts the rows after it. A row
  may then come back on two pages, and the read answers it once
  (`UI/Foundations/AssociationService.AssociationReads.md §1` rule 3). Or a row may be passed
  over, and the next read of the same entity answers it. On the post moderation pages, a chip
  can show late, never twice.

## Out of scope

- Every association write: the upsert (#728), the pair-keyed withdrawal (#729), the remove by
  id and the two gates beneath it (#700), and §ARC17.4's modify and hard delete.
- The reaction summaries (#730).
- §SEC14.3 rule 6 (rule 5): the composite's named vacancy stays where §ARC12.5 entry 1 records it.
- Narrowing a content item list by tag or Bible reference (owner ruling 2026-10-05, under #857):
  `Design/UI/Components/ContentItemListPanel.md §10 item 15`.
- The resolved confidence thresholds the admin association page's suggestion needs (#863).
- An association's approval id (rule 9).
- Invalidating these reads after an association write. No page yet writes an association and
  reads them on one screen, and the first page that does carries it.
- Every page that reads through them: each page's own task.
