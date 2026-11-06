# Post service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `postService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/postService.ts`), existing
Inherits: §UI20.9.2, §UI20.9.3 rule 4, `UI/Brokers/PostBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hooks over `PostBroker`. One of them hands its broker a query whose empty values the broker decides to leave out (`BrokersHoldNoLogic.md` rule 2). It takes that decision over and asks `GetPostsByQueryAsync` (`UI/Brokers/PostBroker.md §1`). What the pages see does not change.

## 1. useGetPosts (#958)

```ts
useGetPosts: (query: PostQuery) => UseQueryResult<PagedPosts>
```

1. **It hands the broker each field of the query that carries a value.** `q`, `category`, `tag` and `author` count as no value when they are missing or an empty string, and `page` and `pageSize` when they are missing or `0`, as the broker decides today. A field with no value is handed on as `undefined`, so the request leaves it out.
2. **Its query key, `['PostsGetAll', query]`, and its stale time are unchanged.**
