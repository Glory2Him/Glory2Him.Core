# Content item service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `contentItemService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/contentItemService.ts`), existing
Inherits: §DOM11.3, §UI20.9.2, §UI20.9.3 rules 2 and 4, `UI/Brokers/ContentItemBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `ContentItemBroker`. Two of them hand their broker work it should not hold (`BrokersHoldNoLogic.md` rule 2): the list read, whose route, filter, order and page `SearchContentItemsAsync` decides, and the takedown, whose reason `DeleteContentItemByIdAsync` trims. Each takes that work over and calls the broker's new members (`UI/Brokers/ContentItemBroker.md` §1–§4). What the pages see does not change.

**An OData literal** below is a value in single quotes, with each single quote inside it doubled, so a term a reader typed with a quote in it stays one literal rather than breaking the filter.

## 1. useSearchContentItems (#946)

```ts
useSearchContentItems: (
    criteria: ContentItemSearchCriteria,
    options?: {
        scope?: 'feed' | 'public' | 'caller';
        submittedById?: string | null;
        defaultApprovalStatuses?: ReadonlyArray<ApprovalStatus>;
        pageSize?: number;
        enabled?: boolean;
    }) => UseInfiniteQueryResult<InfiniteData<ContentItemPage>>
```

Its signature, its query key, its statuses' resolution, its page param and its stale time are unchanged. What changes is that it writes each page's request itself rather than handing `SearchContentItemsAsync` a query to write it from.

1. **The page's scope chooses the read.** `'feed'` reads `GetContentItemFeedAsync`, `'public'` reads `GetPublicContentItemsAsync`, and `'caller'`, the default, reads `GetContentItemsAsync` (`UI/Brokers/ContentItemBroker.md` §1–§3).
2. **The feed is asked for its page alone**: `skip` is the page index times the page size, and `take` is the page size plus one. It is asked for no filter and no order, whatever the criteria carry: the route takes none, and its order is the read's own (§DOM11.3).
3. **A search read is filtered by each criterion the reader gave, and by no other.** Each criterion that applies adds one clause, the clauses are joined with ` and `, and a read with no clause is asked for no filter:
   1. **A term** that is not blank once trimmed matches the title, the content or the author: `(contains(tolower(title),T) or contains(tolower(content),T) or contains(tolower(author),T))`, where `T` is the trimmed term, lower-cased, as an OData literal.
   2. **A content type**, where one is given, matches by its member name, which is what `$filter` parses: `contentType eq '<name>'`. The type numbered zero is a type like any other.
   3. **An author** that is not blank once trimmed matches as a substring, so a surname is enough: `contains(tolower(author),A)`, where `A` is the trimmed author, lower-cased, as an OData literal.
   4. **A submitter**, an account id that is not blank once trimmed, matches exactly, because half an id identifies nobody: `createdBy eq S`, where `S` is the trimmed id as an OData literal.
   5. **A shareability basis**, where one is given, matches by its member name: `shareabilityBasis eq '<name>'`.
   6. **Statuses**, where at least one is given, match any of them by member name, as one clause: `(approvalStatus eq '<name>' or approvalStatus eq '<name>' …)`. The clause only narrows within what the read lets the caller see (§SEC14.5).
4. **A search read is ordered newest first by when it was written, and asked for its page**: `orderBy` `createdWhen desc`, `skip` the page index times the page size, and `top` the page size plus one. `createdWhen`, not the publish date, because a draft has none and the caller's read holds drafts.
5. **Every read answers with one page.** The page holds the first page-size rows, says another page follows only when more rows came, and carries back the page index and page size it was asked for. The row beyond the page is the only sign that one follows: neither route answers with a total.

## 2. useRemoveContentItem (#947)

```ts
useRemoveContentItem: () => UseMutationResult<ContentItem, unknown, { contentItemId: string; deletionReason?: string }>
```

1. **It sends the reason the reader typed, trimmed**, through `DeleteContentItemAsync` (`UI/Brokers/ContentItemBroker.md §4`).
2. **A reason that is missing, or blank once trimmed, is no reason**, and it hands the broker none, so the request carries no `deletionReason` rather than an empty one.
3. **What it invalidates, and its `meta`, are unchanged.**
