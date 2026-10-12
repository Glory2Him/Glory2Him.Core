# Tag service (React)
Parent: [PostModeration.md](../../PostModeration.md)
Level: foundation service — `tagService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/tagService.ts`), new
Inherits: §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/TagBroker.md`, §UI20.9.1 departure 4, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `TagBroker`, in the app's foundation-service shape: an exported object
whose members are hooks, each that calls the broker constructing its own (§UI20.9.1 departure 4).
Both are reads; this service writes nothing.

**An id in a filter** is written bare, as `useGetEffectiveSettingsFor` writes one
(`UI/Foundations/ContentItemSettingService.md §3`): `id eq <id>`.

## 1. useGetTagById (#1038)

```ts
useGetTagById: (tagId: string, enabled?: boolean) => UseQueryResult<Tag>
```

1. **It reads one tag through `TagBroker.GetTagByIdAsync`** (`UI/Brokers/TagBroker.md §1`), keyed
   `['Tag', <tagId>]`.
2. **It asks nothing for an empty id**, nor while `enabled` is `false`; `enabled` defaults to
   `true`.
3. **A failed read is the query's error**, which the page reads; the hook adds no toast of its own.

## 2. useGetTagsByIds (#1040)

```ts
useGetTagsByIds: (tagIds: ReadonlyArray<string>) => UseQueryResult<Tag[]>
```

1. **It asks for exactly the tags whose ids it is handed**, through `TagBroker.GetTagsAsync`
   (`UI/Brokers/TagBroker.md §2`), the ids sorted: one request for each chunk of up to 12 ids,
   filtered `id eq <id> or id eq <id> …`. Twelve, for the reason
   `UI/Foundations/ContentItemSettingService.md §3` rule 2 gives: the route validates `$filter`
   against OData's default limit of 100 nodes.
2. **It is keyed `['Tags', <the sorted ids>]`**, so the same set handed in another order is one
   read.
3. **It asks nothing when it is handed no ids**, and answers an empty collection.
4. **It answers with every chunk's rows, the chunks in order, as one collection.** A chunk that
   fails fails the read.
