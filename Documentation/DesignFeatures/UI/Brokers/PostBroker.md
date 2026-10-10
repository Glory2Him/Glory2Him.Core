# Post broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `PostBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.posts.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `api/Posts`, which the admin post pages and the ported template's pages read. One member holds logic today (`BrokersHoldNoLogic.md` rule 2): `GetPostsAsync` leaves out each field of its query that is an empty string or `0`, not only one that is absent, which decides that those values count as none. It gains a replacement that sends what it is handed, `postService.useGetPosts` makes the decision (`UI/Foundations/PostService.md §1`), and the old member goes (§UI20.9.3 rule 6).

## 1. GetPostsByQueryAsync (#944)

```ts
GetPostsByQueryAsync(query: PostQuery): Promise<PagedPosts>
```

1. **It sends `GET /api/posts` with each field of the query it is handed that is not `undefined`**, under its own name — `q`, `category`, `tag`, `author`, `page` and `pageSize` — encoded, the numbers written as the route reads them (§UI20.9.3 rule 4). A query with no field to send sends no query string.
2. **It returns the page as it came**, typed as `PagedPosts`.
3. **Its name carries `ByQuery`** because §2's member holds `GetPostsAsync` (§UI20.9.3 rule 3).

## 2. GetPostsAsync — deleted (#974)

Deleted once `postService.useGetPosts` calls §1 instead (`UI/Foundations/PostService.md §1`) and nothing calls it. No broker test covers it.
