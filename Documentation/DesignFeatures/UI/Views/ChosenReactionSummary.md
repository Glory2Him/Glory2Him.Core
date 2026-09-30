# Chosen reaction summary
Parent: [Likes.md](../../Likes.md)
Level: view — `toChosenReactionSummary` (`Websites/Glory2Him.WebApp.React/src/services/views/contentItems/toChosenReactionSummary.ts`), new
Inherits: §ARC16.8 (*The projection*), Likes.md rules 3 and 5a

Between a reader's choice and the read that follows the item's latest write, their card shows an optimistic overlay (`UI/Hooks/ContentItemEngagement.md §2`). This view service computes the counts that overlay shows, so the hook only decides when an overlay is laid and when it goes.

## 1. toChosenReactionSummary (#756)

1. **It returns the summary the card shows until the read that follows the item's latest write replaces it.** It takes the item's `reactionSummary`, the reaction the reader holds (or none), the reaction they chose (or none, for a withdrawal) and the options in the vocabulary's order. It never changes what it is handed. An item with no summary yet starts from an empty one.
2. **The chosen reaction's count goes up by one and the held one's down by one**, and a withdrawal takes one from the held reaction, wherever the held reaction has an entry. A reaction still awaiting review is marked as the reader's without being counted (Likes.md rule 6). It may have no entry, and then nothing is taken and no entry appears for it. It may have an entry that other readers' reactions make up, and then the card shows one short until the read that follows the write corrects it. No count goes below nought. The chosen reaction has the mirror case: under a tier that holds a reaction for review, the overlay counts it at once, and the read that follows the write takes the count away again while the mark stays. The overlay assumes the seeded personal tier, which approves a reaction in the same act, so there both cases agree with the read that follows the write.
3. **An entry that reaches nought leaves the summary**, as a reaction nobody gave never appears (§ARC16.8, *A reaction with a zero count does not appear*).
4. **An entry the choice adds takes its place in the options' order**, which is the vocabulary's (`UI/Brokers/ReactionBroker.md §1`), so the cluster does not reorder when the read that follows the write lands (§ARC16.8, *The projection*; Likes.md rule 5a).
