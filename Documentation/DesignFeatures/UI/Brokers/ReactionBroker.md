# Reaction broker
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: broker — `ReactionBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.reactions.ts`), existing
Inherits: §UI20.9.1, §UI20.9.3, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The React app's door to `GET api/Reactions`, the reaction vocabulary every Like control offers. It has two members. One writes the vocabulary's `$filter` itself (`BrokersHoldNoLogic.md` rule 2). The other sends the query it is handed (§2); the reaction service is to write the vocabulary's condition and its order and hand them to it (`UI/Foundations/ReactionService.md §1`), and the old member then goes (§UI20.9.3 rule 6).

This document was the Likes feature's until #814, for the vocabulary's order (Likes.md rule 5a), which #752 was to add here. Under §UI20.9.3 rule 2 the order is the service's, and #752 delivers it there.

## 1. GetApprovedReactionsAsync — deleted (#961)

Deleted once `reactionService.useGetApprovedReactions` calls §2 instead (`UI/Foundations/ReactionService.md §1`) and nothing calls it. It writes the vocabulary's `$filter` itself (`apiBroker.reactions.ts` lines 14-20 at `a78075ee`). No broker test covers it.

## 2. GetReactionsAsync (#932)

```ts
GetReactionsAsync(query: ODataQuery): Promise<Reaction[]>
```

1. **It sends `GET /api/reactions` with each option of the query it is handed**, under its `$` name, encoded, and none it is not handed (§UI20.9.3 rules 2 and 4).
2. **It returns the reactions as they came**, typed as `Reaction[]`, in the order they came.
3. **It adds `ODataQuery`**, `{ filter?: string; orderBy?: string; skip?: number; top?: number }`, in `src/models/foundations/oDataQueries/oDataQuery.ts` (§UI20.9.3 rule 2). The other brokers' new read members take the same model, and build on this one.
