# Association service (React) — tag suggestions
Parent: [TagSuggestions.md](../../TagSuggestions.md)
Level: foundation service — `associationService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/associationService.ts`), existing. Its other user stories are `UI/Foundations/AssociationService.md`, the Likes feature's, and `AssociationService.AssociationReads.md`.
Inherits: §UI20.9.1 departure 4, §UI20.9.2, `UI/Brokers/AssociationBroker.TagSuggestions.md`, `UI/Foundations/AssociationService.md §1` (a write that reads again on settle), `UI/Foundations/AssociationService.AssociationReads.md §1` rule 4 (the `Associations` key), `the-standard-reacttypescript-services` as §UI20.9.2 departs from it, TagSuggestions.md rule 8

The hook a page sends a suggested tag through: a member of the existing `associationService` object, constructing its broker (§UI20.9.1 departure 4).

## 1. useSuggestTag (needs issue)

```ts
useSuggestTag: () => UseMutationResult<AssociationSubmissionResult, unknown, TagSuggestion>
```

1. **A mutation that sends its request through `AssociationBroker.PostTagSuggestionAsync`** (`UI/Brokers/AssociationBroker.TagSuggestions.md §1`) and resolves with the result the broker returns.
2. **When it settles, whether it succeeded or failed, it invalidates the `Associations` reads**, every read keyed under the `['Associations', …]` family `useGetAssociationsByEffectiveId` keys its reads in (`UI/Foundations/AssociationService.AssociationReads.md §1` rule 4), matched by that prefix, so a page's association collection is read again after every suggestion. On settle rather than on success, for `useUpsertAssociation`'s reason: a write that failed may still have landed (`UI/Foundations/AssociationService.md §1`). It invalidates nothing else: a new tag's name reaches a page through the tag reads, which are keyed on the ids they ask for (`UI/Foundations/TagService.md §2` rule 2), so a collection naming a new tag asks for it.
3. **It leaves the app's global error toast on**: a refused or failed suggestion is announced as every failed write is (TagSuggestions.md rule 8; `src/brokers/apiBroker.globals.ts`). It shows nothing of its own: what the reader is told of an answer is the page's.
