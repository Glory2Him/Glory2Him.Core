# Content item broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ContentItemBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.contentItems.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, §DOM11.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/ContentItems`. Two of its members hold logic today (`BrokersHoldNoLogic.md` rule 2). `SearchContentItemsAsync` chooses between three routes by the query's scope, builds a `$filter` from the query's fields, orders and pages the read, and cuts the answer to the page. `DeleteContentItemByIdAsync` trims the deletion reason and leaves a blank one out. The three reads become one member each, sending what they are handed, and so does the delete. `contentItemService` writes what they send (`UI/Foundations/ContentItemService.md`), and the old members go (§UI20.9.3 rule 6).

The three reads differ in what the route accepts. `GET api/ContentItems` and `GET api/ContentItems/Public` carry `[EnableQuery]` and take OData options. `GET api/ContentItems/Feed` carries none: it takes its page as plain `skip` and `take`, and ignores anything dollar-prefixed rather than refusing it (§DOM11.3; `contentItemSearchQuery.ts`).

## 1. GetContentItemsAsync (#933)

```ts
GetContentItemsAsync(query: ODataQuery): Promise<ContentItem[]>
```

1. **It sends `GET /api/contentitems` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4). This is the read that widens with the caller: their own rows, and every row a review role covers.
2. **It returns the rows as they came**, typed as `ContentItem[]`. Cutting them to a page is the service's.

## 2. GetPublicContentItemsAsync (#934)

```ts
GetPublicContentItemsAsync(query: ODataQuery): Promise<ContentItem[]>
```

1. **It sends `GET /api/contentitems/Public` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed. This read is the §SEC14.1 canonical set, the same for every caller.
2. **It returns the rows as they came**, typed as `ContentItem[]`.

## 3. GetContentItemFeedAsync (#935)

```ts
GetContentItemFeedAsync(skip: number, take: number): Promise<ContentItem[]>
```

1. **It sends `GET /api/contentitems/Feed?skip=<skip>&take=<take>`**, the two numbers it is handed written as the route reads them, and nothing else. The feed's order is the read's own (§DOM11.3), so nothing here asks for one.
2. **It returns the rows as they came**, typed as `ContentItem[]`.

## 4. DeleteContentItemAsync (#936)

```ts
DeleteContentItemAsync(contentItemId: string, deletionReason?: string): Promise<ContentItem>
```

1. **It sends `DELETE /api/contentitems/<contentItemId>`, with `deletionReason` on the query string, encoded, when it is handed one**, and without it when the reason is `undefined` (§UI20.9.3 rule 4). It sends the reason it is handed as it is: deciding that a blank reason is no reason is the service's (`UI/Foundations/ContentItemService.md §2`).
2. **It returns the row the server answers with**, typed as `ContentItem`. The delete is soft: the row keeps its place and its history (§APR9.7.6).

## 5. SearchContentItemsAsync — deleted (#962)

Deleted once `contentItemService.useSearchContentItems` calls §1, §2 and §3 instead (`UI/Foundations/ContentItemService.md §1`) and nothing calls it. With it go its private `GetFeedPageAsync` and `GetSearchPageAsync`, the file's `approvalStatusMemberNames` and `toODataLiteral`, which only they use, and `apiBroker.contentItems.test.ts`, whose every case has moved to `contentItemService.test.tsx` (`BrokersHoldNoLogic.md` rule 5).

## 6. DeleteContentItemByIdAsync — deleted (#963)

Deleted once `contentItemService.useRemoveContentItem` calls §4 instead (`UI/Foundations/ContentItemService.md §2`) and nothing calls it. No broker test covers it.
