# Bible reference broker
Parent: [PostModeration.md](../../PostModeration.md)
Level: broker — `BibleReferenceBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.bibleReferences.ts`), new
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to the two Bible reference reads `BibleReferencesController` already serves,
`GET api/BibleReferences` with `[EnableQuery]` and `GET api/BibleReferences/{bibleReferenceId}`,
both `[AllowAnonymous]`
(`Websites/Glory2Him.WebApp/Controllers/BibleReferences/BibleReferencesController.cs`). It takes the
app's broker shape (§UI20.9.1) and holds no logic (§UI20.9.3 rule 1). The admin pages read
references through it to name the post's Bible reference chips and an association's reference,
with its scripture (`PostModeration.md` rules 6 and 12). The Bible reference page's read by
`USFM`, which #700 designs, joins this document as a member of its own (ruling 13 on #705).

**The wire model is new**, `BibleReference` under
`src/models/foundations/bibleReferences/bibleReference.ts`, added by the first member that
receives it. It types what the pages read — `id`, `reference`, `translation` and `scripture`, the
last nullable as the server's `Scripture` is — and leaves the rest of the server's
`BibleReference` untyped, as `src/models/foundations/approvals/approval.ts` does.

## 1. GetBibleReferenceByIdAsync (#1035)

```ts
GetBibleReferenceByIdAsync(bibleReferenceId: string): Promise<BibleReference>
```

1. **It sends `GET /api/BibleReferences/{bibleReferenceId}`**, the id it is handed encoded into the
   path, and returns the body typed as `BibleReference`.
2. **A refusal reaches its caller as axios raised it.** A reference the caller may not see answers
   `404` (§SEC14.5 rule 1), and the member neither catches nor reads it.

## 2. GetBibleReferencesAsync (#1037)

```ts
GetBibleReferencesAsync(query: ODataQuery): Promise<BibleReference[]>
```

1. **It sends `GET /api/BibleReferences` with each option of the query it is handed**, under its
   `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the rows as they came**, typed as `BibleReference[]`.
3. **Its name carries no `ByQuery`**: no member of this broker holds `GetBibleReferencesAsync`
   (§UI20.9.3 rule 3).
