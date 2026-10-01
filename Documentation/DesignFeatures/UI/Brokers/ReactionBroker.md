# Reaction broker
Parent: [Likes.md](../../Likes.md)
Level: broker — `ReactionBroker` (`Websites/Glory2Him.WebApp.React/src/brokers/apiBroker.reactions.ts`), existing
Inherits: §DOM5.2 (`SortOrder`), `UI/Components/ContentItemPanel.md rule 3.1.8`, `the-standard-reacttypescript-brokers` as §UI20.9.1 departs from it

The Like control offers the vocabulary that `GET api/Reactions` returns, in the order it arrives. The engagement hook maps it into options without reordering (`useContentItemEngagement.ts`), and the card narrows those options without reordering (`contentItemPanel.tsx`, `offeredReactions`). Today the read asks for no order, so the choices come in whatever order SQL Server returns the rows. §DOM5.2 orders the vocabulary by its `SortOrder`, so the broker asks for that order.

## 1. GetApprovedReactionsAsync (#752)

**On hold, pending #814.** Today this broker writes the vocabulary's filter, and rule 1 below adds an order. Both are query conditions, and the owner ruled on 2026-10-01 that a broker holds no logic (§UI20.9.1, *Scope*). #814 rules where a React read's condition lives and re-carves #752 against that ruling. The order the reader sees (§DOM5.2) is unchanged; only where it is asked for moves.

1. **It asks for the vocabulary ordered by `sortOrder`, then `name`** — `$orderby=sortOrder,name`, sent beside today's filter, which does not change. The server does the ordering: the route is `[EnableQuery]`, and the host enables `$orderby` (`Program.cs`).
2. **The wire model gains nothing.** The client does no ordering of its own, and `reaction.ts` types only what the choices surface reads.
