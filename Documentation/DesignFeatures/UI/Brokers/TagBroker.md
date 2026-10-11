# Tag broker
Parent: [PostModeration.md](../../PostModeration.md)
Level: broker — `TagBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.tags.ts`), new
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to the two tag reads `TagsController` already serves, `GET api/Tags` with
`[EnableQuery]` and `GET api/Tags/{tagId}`, both `[AllowAnonymous]`
(`Websites/Glory2Him.WebApp/Controllers/Tags/TagsController.cs`). It takes the app's broker shape
(§UI20.9.1) and holds no logic: it forms the request, sends it and hands back what came
(§UI20.9.3 rule 1). The admin pages read tags through it to name the post's tag chips and an
association's tag (`PostModeration.md` rules 6 and 12).

**The wire model is new**, `Tag` under `src/models/foundations/tags/tag.ts`, added by the first
member that receives it. It types what the pages read, `id` and `name`, and leaves the rest of the
server's `Tag` untyped, as `src/models/foundations/approvals/approval.ts` does.

## 1. GetTagByIdAsync (#1034)

```ts
GetTagByIdAsync(tagId: string): Promise<Tag>
```

1. **It sends `GET /api/Tags/{tagId}`**, the id it is handed encoded into the path, and returns the
   body typed as `Tag`.
2. **A refusal reaches its caller as axios raised it.** A tag the caller may not see answers `404`
   (§SEC14.5 rule 1), and the member neither catches nor reads it.

## 2. GetTagsAsync (#1036)

```ts
GetTagsAsync(query: ODataQuery): Promise<Tag[]>
```

1. **It sends `GET /api/Tags` with each option of the query it is handed**, under its `$` name,
   encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the rows as they came**, typed as `Tag[]`.
3. **Its name carries no `ByQuery`**: no member of this broker holds `GetTagsAsync` (§UI20.9.3
   rule 3).
