# Content item engagement
Parent: [Likes.md](../Likes.md)
Level: view — `useContentItemEngagement` (`Websites/Glory2Him.WebApp.React/src/hooks/useContentItemEngagement.ts`), the engagement wiring every page that renders the card shares
Inherits: §ARC16.8 (*Where each shared rule lives*, *The projection*), §UI20.6.6 rule 2, `UI/Components/ContentItemPanel.md rules 3.1.8 and 3.2.4`, Likes.md rules 1–7

Seven pages take their card's engagement from this hook (`home.tsx`, `posts.tsx`, `postDetail.tsx`, `myPosts.tsx`, `myPostDetail.tsx`, `admin/contentItemModerationPage.tsx`, `admin/contentItemModerationDetailPage.tsx`). Today it keeps a chosen reaction in page state for the visit and reads nothing but the vocabulary. The page decides what follows the card's hook (`UI/Components/ContentItemPanel.md rule 3.2.4`), and this hook is how every page decides it the same way. Its other members — `reactionOptions`, `onShareClick`, `onSaveClick` — are unchanged.

**There is one tally, the server's** (§ARC16.8, *Which reaction this viewer holds*). What the reader sees between their press and the next read is an optimistic overlay on the element — the reaction pressed and the counts moved — and the next read replaces it wholesale. It is never a second store kept in step with the first.

## 1. withReactions — the counts and the reader's own reaction on each card (#738)

1. **The page hands the hook the ids of each page of cards it has delivered** — a list page its delivered pages, a detail page one page of its one id — and the hook reads their summaries through `associationService.useGetReactionSummaries` (`UI/AssociationService.md §3`), so the set is keyed on what was delivered and never on the accumulated list (§ARC16.8, *The set, its bounds*).
2. **`withReactions` projects each summary onto its element**, in place of today's `withViewerReactions`: `reactionSummary` gets one entry per reaction, in the summary's order — the reaction's `name` as its `label`, its `unicodeEmoji` as its `glyph`, and its `count` — and `viewerReactionLabel` gets `viewerReactionName`. Renaming into the view's vocabulary is this layer's (§ARC16.8, *The projection*).
3. **An element with no summary shows no counts and no pressed reaction**, and still offers Like: its read has not landed, its page's read failed, or the server did not answer for it (§ARC16.8: an id the read cannot answer for is absent). A card is never shown another item's counts.
4. **The overlay of §2 applies on top of the summary**, and is dropped for an item once a read for that item lands after the write that made it.

## 2. onReactionSelected — give, change or withdraw (#739)

1. **While the reader's sign-in state is still being read, a choice does nothing**: the reader is not sent to sign in, and nothing is recorded (§UI20.6.6 rule 2; today the card holds this guard, `contentItemPanel.tsx` — the `onReactionSelected` handler).
2. **A signed-out reader is sent to sign in**, through the one reusable sign-in action, and returned to exactly where they were (`UI/Pages/Home.md §6 item 3`; Likes.md rule 4).
3. **A signed-in reader who chooses a reaction they do not hold gives it** — or changes to it, where they hold another — through `associationService.useUpsertAssociation` (`UI/AssociationService.md §1`), the item as endpoint A and the reaction as endpoint B.
4. **A signed-in reader who chooses the reaction they hold withdraws it**, through `associationService.useRemoveAssociationByPair` (`UI/AssociationService.md §2`). Which reaction they hold is the element's `viewerReactionLabel`, as §1 and the overlay leave it.
5. **The overlay moves at once**: the chosen reaction pressed, or none after a withdrawal; the chosen reaction's count up by one, and the count of the one it replaced or withdrew down by one, an entry that reaches nought leaving the cluster. An entry the choice adds takes its place in the vocabulary's order — the order of the hook's `reactionOptions` (`UI/ReactionBroker.md §1`) — so the cluster does not reorder when the next read lands (§ARC16.8, *The projection*).
6. **A failed write takes its overlay away**, so the card shows what the last read said, and the failure is announced as every failed write is. When a write settles either way, the summaries are read again (`UI/AssociationService.md §1`), and the read replaces the overlay.
7. **The reaction's id travels on the option.** `ContentItemReactionOption` gains `id`, which `toContentItemReactionOption` maps from the vocabulary row's `id`; the option's `label` stays its identity everywhere else (`contentItemSearchItem.ts`).
