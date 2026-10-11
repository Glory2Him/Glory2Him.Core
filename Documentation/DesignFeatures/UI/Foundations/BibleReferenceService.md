# Bible reference service (React)
Parent: [PostModeration.md](../../PostModeration.md)
Level: foundation service — `bibleReferenceService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/bibleReferenceService.ts`), new
Inherits: §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/BibleReferenceBroker.md`, §UI20.9.1 departure 4, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `BibleReferenceBroker`, in the app's foundation-service shape: an
exported object whose members are hooks, each that calls the broker constructing its own
(§UI20.9.1 departure 4). Both are reads; this service writes nothing.

**An id in a filter** is written bare, as `UI/Foundations/TagService.md` writes one: `id eq <id>`.

## 1. useGetBibleReferenceById (needs issue)

```ts
useGetBibleReferenceById: (bibleReferenceId: string, enabled?: boolean) => UseQueryResult<BibleReference>
```

1. **It reads one reference through `BibleReferenceBroker.GetBibleReferenceByIdAsync`**
   (`UI/Brokers/BibleReferenceBroker.md §1`), keyed `['BibleReference', <bibleReferenceId>]`.
2. **It asks nothing for an empty id**, nor while `enabled` is `false`; `enabled` defaults to
   `true`.
3. **A failed read is the query's error**, which the page reads; the hook adds no toast of its own.

## 2. useGetBibleReferencesByIds (needs issue)

```ts
useGetBibleReferencesByIds: (bibleReferenceIds: ReadonlyArray<string>) => UseQueryResult<BibleReference[]>
```

1. **It asks for exactly the references whose ids it is handed**, through
   `BibleReferenceBroker.GetBibleReferencesAsync` (`UI/Brokers/BibleReferenceBroker.md §2`), the
   ids sorted: one request for each chunk of up to 12 ids, filtered `id eq <id> or id eq <id> …`,
   for the reason `UI/Foundations/ContentItemSettingService.md §3` rule 2 gives.
2. **It is keyed `['BibleReferences', <the sorted ids>]`**, so the same set handed in another order
   is one read.
3. **It asks nothing when it is handed no ids**, and answers an empty collection.
4. **It answers with every chunk's rows, the chunks in order, as one collection.** A chunk that
   fails fails the read.
