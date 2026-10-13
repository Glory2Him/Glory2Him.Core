# Association broker — association reads
Parent: [AssociationReads.md](../../AssociationReads.md)
Level: broker — `AssociationBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.associations.ts`), existing
Inherits: §ARC17.4, §UI20.9.1, §UI20.9.3, `UI/Brokers/AssociationBroker.md` (the class and what crosses the wire), `Backend/Controllers/AssociationsController.AssociationReads.md`, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to the two association reads. Each member forms its request, sends it and
hands back what came, and holds no logic (§UI20.9.3 rule 1). The read's condition, order and page
are the foundation service's (`UI/Foundations/AssociationService.AssociationReads.md`).

**The wire model is new**: `Association`, under
`src/models/foundations/associations/association.ts`, added by the first member that receives it.
It types what the pages read and leaves the rest of the server's `Association` untyped, as
`src/models/foundations/approvals/approval.ts` does:

- `id`;
- each endpoint's `entityAType`, `entityAKeyId` and `entityAEffectiveId`, and `entityBType`,
  `entityBKeyId` and `entityBEffectiveId`, each type an `EntityType`, the app's existing mirror
  (`src/models/foundations/approvalSettings/approvalSetting.ts`), carried as its number
  (`UI/Brokers/AssociationBroker.md`, *What crosses the wire*);
- `userId`, a string or `null`;
- `approvalStatus`, an `ApprovalStatus`, typed as `src/models/foundations/contentItems/contentItem.ts`
  types it;
- `confidenceScore`, a number or `null`, and `confidenceReason`, a string or `null`;
- `createdBy` and `createdWhen`.

It types no approval id: the server's row carries none yet (AssociationReads.md rule 9).

## 1. GetAssociationsAsync (#1126)

```ts
GetAssociationsAsync(query: ODataQuery): Promise<Association[]>
```

1. **It sends `GET /api/associations` with each option of the query it is handed**, under its
   `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the rows as they came**, typed as `Association[]`.
3. **Its name carries no `ByQuery`**: no member of this broker holds `GetAssociationsAsync`
   (§UI20.9.3 rule 3).

## 2. GetAssociationByIdAsync (#1127)

```ts
GetAssociationByIdAsync(associationId: string): Promise<Association>
```

1. **It sends `GET /api/associations/{associationId}`**, the id it is handed encoded into the
   path, and returns the body typed as `Association`.
2. **A refusal reaches its caller as axios raised it.** An association the caller may not see
   answers `404` (§SEC14.5 rule 1), and the member neither catches nor reads it (§UI20.9.1
   departure 1).
