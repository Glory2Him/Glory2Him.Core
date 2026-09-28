# Association service (React)
Parent: [Likes.md](../../Likes.md)
Level: foundation service — `associationService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/associationService.ts`), new
Inherits: §ARC16.8 (*The set, its bounds*, *Where each shared rule lives*), `UI/Brokers/AssociationBroker.md`

The React Query hooks over `AssociationBroker`, in the app's foundation-service shape: an exported object whose members are hooks, each holding its broker (`contentItemService.ts`, `reactionService.ts`). The two writes invalidate what they change; the read asks per page.

**One query key family, `ReactionSummaries`**, so a write can invalidate every summary read holding its item by prefix, as `useModifyContentItem` invalidates `ContentItemsSearch`.

## 1. useUpsertAssociation (#734)

A mutation that sends its request through `AssociationBroker.PostAssociationAsync` and resolves with the suggestion result. When it settles, whether it succeeded or failed, it invalidates the `ReactionSummaries` reads, so the next read replaces whatever the page showed in the meantime (§ARC16.8: the optimistic overlay is discarded on the next read). It leaves the app's global error toast on: a failed reaction is announced as any failed write is.

## 2. useRemoveAssociationByPair (#735)

A mutation that sends its request through `AssociationBroker.DeleteAssociationPairAsync`, and invalidates the `ReactionSummaries` reads when it settles, on the same terms as §1.

## 3. useGetReactionSummaries (#736)

```ts
useGetReactionSummaries: (contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>) => ...
```

1. **One query per page of ids it is handed**, keyed `['ReactionSummaries', <that page's ids>]`, so a list that loads a third page asks for the third page's ids alone and the first two stay cached (§ARC16.8: *keyed on the ids of the page just delivered, never on the accumulated list*). A detail page hands one page of one id.
2. **A page of more than 25 ids is asked in chunks of 25**, and the chunks' answers are one answer (§ARC16.8: *a caller ever holding more than 25 ids in one ask chunks at 25*). An empty page asks nothing.
3. **It answers one summary per content item id across every page**, keyed on the id, together with whether any page is still loading and whether any failed. A page that failed leaves its ids without a summary; it does not take the others' away.
