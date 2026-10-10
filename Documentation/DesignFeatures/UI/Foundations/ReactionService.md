# Reaction service (React)
Parent: [BrokersHoldNoLogic.md](../../BrokersHoldNoLogic.md)
Level: foundation service — `reactionService` (`Websites/Glory2Him.WebApp.React/src/services/foundations/reactionService.ts`), existing
Inherits: §DOM5.2 (`SortOrder`), Likes.md rule 5a, §UI20.9.2, §UI20.9.3 rule 2, `UI/Brokers/ReactionBroker.md`, `the-standard-reacttypescript-services` as §UI20.9.2 departs from it

The React Query hook over `ReactionBroker` that every Like control's choices come from. Today it hands the read to `ReactionBroker.GetApprovedReactionsAsync`, which writes the vocabulary's filter itself and asks for no order, so the choices come in whatever order SQL Server returns the rows. Under §UI20.9.3 rule 2 the hook writes the read's whole condition, the filter and the order Likes.md rule 5a adds, and hands it to `ReactionBroker.GetReactionsAsync` (`UI/Brokers/ReactionBroker.md §2`).

## 1. useGetApprovedReactions (#752)

```ts
useGetApprovedReactions: () => UseQueryResult<Reaction[]>
```

1. **It asks for the approved, published vocabulary only**: the filter `approvalStatus eq 'Approved' and isPublished eq true and isDeleted eq false`, unchanged from the one the broker writes today. The read is anonymous and widens with the caller, so an owner would otherwise see their own drafts, and a draft reaction offered as a choice would let a reader react with something the moderators have not accepted.
2. **It asks for the vocabulary in its order**: `orderBy` `sortOrder,name` (Likes.md rule 5a). `name` breaks a tie, as §DOM5.2 breaks it, so two reactions that share a sort order come in one order on every card. The server does the ordering: the route is `[EnableQuery]` over the foundation's queryable, and the host enables `$orderby` (`Program.cs`). The client sorts nothing.
3. **It answers with the reactions in the order the server sent them.** The engagement hook maps them into options in that order (`useContentItemEngagement.ts`), and the card narrows the options without reordering them (`contentItemPanel.tsx`, `offeredReactions`). Neither changes.
4. **It keeps its query key, `['ReactionsGetApproved']`, and its stale time of five minutes.**
