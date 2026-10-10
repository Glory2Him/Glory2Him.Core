# Content item setting service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `contentItemSettingService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/contentItemSettingService.ts`), existing
Inherits: §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/ContentItemSettingBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `ContentItemSettingBroker`. Four of them hand their broker reads whose filter, chunks, order or page the broker decides (`BrokersHoldNoLogic.md` rule 2). Each takes that over and asks `GetContentItemSettingsByQueryAsync` (`UI/Brokers/ContentItemSettingBroker.md §1`). What the pages see does not change. Every hook keeps its query key, its stale time and its other options.

**An OData literal** below is a value in single quotes, with each single quote inside it doubled.

## 1. useGetAvailableForContribution (#948)

```ts
useGetAvailableForContribution: () => UseQueryResult<ContentItemSetting[]>
```

1. **It asks for the per-type defaults open to general contribution**: the filter `isAvailableAsGeneralUserContribution eq true and contentItemId eq null`, the contribute page's type selector.

## 2. useGetDefaults (#949)

```ts
useGetDefaults: () => UseQueryResult<ContentItemSetting[]>
```

1. **It asks for every per-type default**: the filter `contentItemId eq null`, whether or not the type is open to contribution, because a page that renders an item needs its type's settings whoever may contribute one.

## 3. useGetEffectiveSettingsFor (#950)

```ts
useGetEffectiveSettingsFor: (contentItemIds: ReadonlyArray<string>) => UseQueryResult<ContentItemSetting[]>
```

1. **It asks for the defaults as §2 does**: the filter `contentItemId eq null`.
2. **It asks for the overrides of exactly the ids it is handed**, in its sorted order: one request for each chunk of up to 12 ids, filtered `contentItemId eq <id> or contentItemId eq <id> …`. Twelve, because the route validates `$filter` against OData's default limit of 100 nodes, which an or-chain of 17 ids already passes (a `400`, not a short answer), and twelve stays inside it and inside IIS's 2,048-character query string.
3. **It asks for no overrides when it is handed no ids.**
4. **It answers with the defaults followed by every chunk's overrides**, the chunks in order, as one collection for the resolver.

## 4. useGetContentItemSettings (#951)

```ts
useGetContentItemSettings: (query: ContentItemSettingQuery) => UseQueryResult<ContentItemSettingPage>
```

1. **The admin list is filtered by each criterion it is handed, and by no other.** Each that applies adds one clause, the clauses are joined with ` and `, and a read with no clause is asked for no filter:
   1. **A search term** that is not blank once trimmed matches the type's name or description: `(contains(tolower(contentTypeName),T) or contains(tolower(contentTypeDescription),T))`, where `T` is the trimmed term, lower-cased, as an OData literal.
   2. **A content type**, where one is given, matches by its member name: `contentType eq '<name>'`.
   3. **The `Default` scope** asks for the defaults alone, `contentItemId eq null`, and **the `Override` scope** for the overrides alone, `contentItemId ne null`. `All`, or no scope, adds no clause.
2. **It is ordered by type, then by item, and asked for its page**: `orderBy` `contentType,contentItemId`, `skip` the page, counted from one, less one, times the page size, and `top` the page size plus one.
3. **It answers with one page.** The page holds the first page-size rows, says another page follows only when more rows came, and carries back the page and page size it was asked for.
