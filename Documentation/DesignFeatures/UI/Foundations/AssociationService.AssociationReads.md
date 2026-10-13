# Association service (React) — association reads
Parent: [AssociationReads.md](../../AssociationReads.md)
Level: foundation service — `associationService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/associationService.ts`), existing
Inherits: §DOM4.4, §DOM4.6 rule 1, §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/AssociationBroker.AssociationReads.md`, §UI20.9.1 departure 4, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it, AssociationReads.md rules 6 and 10

Two read hooks over `AssociationBroker`, in the app's foundation-service shape: members of the
existing `associationService` object, each constructing its broker (§UI20.9.1 departure 4).
Neither writes.

**In a filter**, an `EntityType` is written as its member's name in single quotes, as
`contentItemSettingService` writes a content type (`UI/Foundations/ContentItemSettingService.md
§4` rule 1.2), and an id is written bare, as `UI/Foundations/TagService.md` writes one.

**Neither read is kept per reader.** What each answers depends on who asks (AssociationReads.md
rule 1), as the app's other caller-widening reads do, and like them each is keyed on what it asks
for alone (`contentItemService.useGetContentItemById`, keyed on the id).

## 1. useGetAssociationsByEffectiveId (#1128)

```ts
useGetAssociationsByEffectiveId: (entityType: EntityType, effectiveId: string) => UseQueryResult<Association[]>
```

1. **It asks for every association holding the entity on either endpoint**, through
   `AssociationBroker.GetAssociationsAsync` (`UI/Brokers/AssociationBroker.AssociationReads.md §1`),
   filtered `(entityAType eq '<name>' and entityAEffectiveId eq <id>) or (entityBType eq '<name>'
   and entityBEffectiveId eq <id>)`, where `<name>` is the entity type's member name and `<id>` the
   effective id it is handed (AssociationReads.md rule 10).
2. **It reads page after page until the answer is whole.** Each request is ordered `id`, asks
   `top` 50, and asks `skip` 0 for the first page, then 50 more for each page after it. It asks the
   next page only after a page that held 50 rows, and stops at the first page that held fewer
   (AssociationReads.md rule 6: no answer says whether it was cut short).
3. **It answers with every page's rows, the pages in order, as one collection, and each row
   once.** A row whose id an earlier page already answered is left where it first came. The set
   can change between two requests, so a row can come back on a later page
   (`AssociationReads.md`, *Risks*).
4. **It is keyed `['Associations', <entityType>, <effectiveId>]`**, so a page that asks for one
   entity by two effective ids holds two reads, each with its own state and its own `refetch`.
5. **It asks nothing for an empty effective id.**
6. **A page that fails fails the read**, with that page's error unchanged. A failed read is the
   query's error, which the page reads; the hook adds no toast of its own.

7. **The host's page-size setting names this read.** The page size, 50, is the host's
   (`OData:PageSize`). Under a host page size below 50, a request for 50 rows would be answered
   with fewer, and the read would stop early. So the host's page size must not drop below this
   one, and the comment on that setting in `Websites/Glory2Him.WebApp/appsettings.json`,
   `//PageSize`, says so.

## 2. useGetAssociationById (#1129)

```ts
useGetAssociationById: (associationId: string, enabled?: boolean) => UseQueryResult<Association>
```

1. **It reads one association through `AssociationBroker.GetAssociationByIdAsync`**
   (`UI/Brokers/AssociationBroker.AssociationReads.md §2`), keyed `['Association', <associationId>]`.
2. **It asks nothing for an empty id**, nor while `enabled` is `false`; `enabled` defaults to
   `true`.
3. **A failed read is the query's error**, which the page reads; the hook adds no toast of its own.
   An association the caller may not see fails as a `404` (§SEC14.5 rule 1).
