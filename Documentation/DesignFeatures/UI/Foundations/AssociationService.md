# Association service (React)
Parent: [Likes.md](../../Likes.md)
Level: foundation service — `associationService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/associationService.ts`), new
Inherits: §ARC16.8 (*The set, its bounds*, *Where each shared rule lives*), `UI/Brokers/AssociationBroker.md`, §UI20.9.1 departure 4

The React Query hooks over `AssociationBroker`, in the app's foundation-service shape: an exported object whose members are hooks, each constructing its own broker (§UI20.9.1 departure 4). The two writes invalidate what they change; the read asks per page.

**One query key family, `ReactionSummaries`**, so a write can invalidate every summary read holding its item by prefix, as `useModifyContentItem` invalidates `ContentItemsSearch`.

## 1. useUpsertAssociation (#734)

A mutation that sends its request through `AssociationBroker.PostAssociationAsync` and resolves with the suggestion result. When it settles, whether it succeeded or failed, it invalidates the `ReactionSummaries` reads, so a read follows every write. Which read replaces the card's overlay is the engagement hook's rule (`UI/Hooks/ContentItemEngagement.md §2` rule 8; §ARC16.8, *Which reaction this viewer holds*). It leaves the app's global error toast on: a failed reaction is announced as any failed write is.

## 2. useRemoveAssociationByPair (#735)

A mutation that sends its request through `AssociationBroker.DeleteAssociationPairAsync`, and invalidates the `ReactionSummaries` reads when it settles, on the same terms as §1.

## 3. useGetReactionSummaries (#736)

```ts
useGetReactionSummaries: (contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>) => ...
```

1. **One query per page of ids it is handed**, keyed `['ReactionSummaries', <that page's ids>]`, so a list that loads a third page asks for the third page's ids alone and the first two stay cached (§ARC16.8: *keyed on the ids of the page just delivered, never on the accumulated list*). A detail page hands one page of one id.
2. **A page of more than 25 ids is asked in chunks of 25**, and the chunks' answers are one answer (§ARC16.8: *a caller ever holding more than 25 ids in one ask chunks at 25*). A chunk that fails fails its page, as rule 3 treats a failed page: none of that page's ids has a summary, even those a chunk that answered asked for. An empty page asks nothing.
3. **It answers one summary per content item id across every page**, keyed on the id, together with whether any page is still loading and whether any failed. A page that failed leaves its ids without a summary; it does not take the others' away.

## 4. useReadReactionSummariesAgain (#759)

```ts
useReadReactionSummariesAgain: () => () => Promise<void>
```

1. **The function it returns re-reads every active `ReactionSummaries` read and waits for the answer.** The active reads are the enabled ones `useGetReactionSummaries` is serving on the page. A read cached from another screen, or a disabled one such as an empty page's, is neither re-read nor waited for. The function sends a fresh read of each active one, and resolves once a read sent at or after the call has landed for each of them.
2. **An unchanged answer counts.** The read that lands may answer exactly what the last one did, as for an item that is not public yet (Likes.md rule 11a), and it still ends the wait. TanStack Query keeps the same data when an answer is unchanged and tells no reader, so the wait watches the reads that land, not the data they carry: a query's `dataUpdateCount` and `errorUpdateCount` move on every read that lands.
3. **A read in flight at the call never ends the wait.** TanStack Query decides by whether the query holds an answer, not by which read it is (`@tanstack/query-core`, `Query.fetch`). A read in flight on a query that holds one is cancelled, and the fresh read supersedes it. A read in flight on a query that holds none is joined by a refetch instead of cancelled. That covers its first read, and a re-read of a page whose first read failed. So the wait lets such a read land, and then sends a fresh read after it. From then on every read that lands was sent after the call, and the first to land ends the wait.
4. **A superseded read is waited past, however often it is superseded.** Another write's refresh, another call's re-read, or both may supersede the call's read. The wait ends only when a read sent at or after the call has landed, never when a superseded read's promise returns.
5. **A failed read ends the wait too**, so no caller waits forever. The failure reaches the page as any failed read does (§3 rule 3).
6. **Its caller is the engagement hook**, which waits on it after each write settles (`UI/Hooks/ContentItemEngagement.md §2` rule 8).
