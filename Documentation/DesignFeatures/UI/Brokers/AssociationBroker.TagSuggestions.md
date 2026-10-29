# Association broker — tag suggestions
Parent: [TagSuggestions.md](../../TagSuggestions.md)
Level: broker — `AssociationBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.associations.ts`), existing. Its other user stories are `UI/Brokers/AssociationBroker.md`, the Likes feature's, and `AssociationBroker.AssociationReads.md`.
Inherits: §ARC17.4, §UI20.9.1, `UI/Brokers/AssociationBroker.md` (the class and what crosses the wire), `Backend/Controllers/AssociationsController.TagSuggestions.md §1`, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to the tag suggestion's route. It forms the request, sends it and hands back what came, and holds no logic (§UI20.9.1). A broker member has no test paths: its behaviour is proven by the service's tests that mock it, and by the page's.

**The wire models are new**, under `src/models/foundations/associations/`, added by this member: the request, `TagSuggestion` — `contentItemId`, a string, and `name`, a string, and nothing else; and the answer, `AssociationSubmissionResult` — `status`, an `AssociationSubmissionStatus`, a numeric mirror of the server's enum in its order: `Created` `0`, `AlreadySuggested` `1`, `AlreadySuggestedByCaller` `2` (`UI/Brokers/AssociationBroker.md`, *What crosses the wire*).

## 1. PostTagSuggestionAsync (needs issue)

```ts
PostTagSuggestionAsync(tagSuggestion: TagSuggestion): Promise<AssociationSubmissionResult>
```

1. **It sends the suggestion as the body of `POST /api/associations/tagsuggestions`**, the route `Backend/Controllers/AssociationsController.TagSuggestions.md §1` serves, under the broker's `relativeAssociationsUrl`.
2. **It returns the result as it came**, typed as `AssociationSubmissionResult`. The route answers `201` or `200`, both with the result as the body, and `ApiBroker.PostAsync` resolves both, as it does for `PostAssociationAsync`.
