# Access broker — Berean's review
Parent: [BereanReview.md](../../BereanReview.md)
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`). The Likes feature's work on the same broker is `Backend/Brokers/AccessBroker.md`, and the amender record's `Backend/Brokers/AccessBroker.Amenders.md`.
Inherits: §APR8.6.1 rules 2–4, §APR8.6.2.1, *The security boundary* rule 3, §ARC12.2.1, §ARC12.5, BereanReview.md rules 9–11, 18, 20 and 21

The broker gathers what the decision function and the workflow need, unfiltered by identity, because each answer decides an invariant (§APR8.6.2.1, *The security boundary* rule 3). Its gathers are unit-tested, as every `AccessBroker` gather is. None adds a storage-broker member: the live assignment is read through the existing keyed `IStorageBroker.SelectAIReviewerAssignmentByApprovalIdAsync`, which answers the round's one live row (`UX_AIReviewerAssignments_ApprovalId`), and each item through the read of its row the subject traversal already makes.

## 1. RetrieveLiveAIReviewerAssignmentAsync (#1157)

```csharp
ValueTask<AIReviewerAssignment?> RetrieveLiveAIReviewerAssignmentAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

1. **It answers the round's live Berean assignment, pending or completed, or `null` where there is none** — none ever, or one withdrawn or dismissed. The approval process reads it to dismiss it or clear it on a data change, to know whether step 4.2 has anything to assign, and to publish it on the review event at step 4.3 (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`, `§7` and `§8`).
2. **No identity filters it**: the approval process runs as whoever changed the item, who may see nothing on the round (§APR9.7.4).

## 2. FindResettableAIReviewerAssignmentIdAsync, retired (#1169)

Once the reset and the Modified flow stop calling the helper that asks it (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §1–§2`), nothing calls the gather. It leaves `IAccessBroker` and `AccessBroker`, with its tests.

## 3. RetrieveApprovalSubjectConfidenceAsync (#1158)

```csharp
ValueTask<ApprovalSubjectConfidence?> RetrieveApprovalSubjectConfidenceAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

`ApprovalSubjectConfidence` is `(bool IsSubjectAvailable, decimal? ConfidenceScore)`, in `Models/Securities/`.

1. **It answers whether the round's item reads back, and not soft deleted, and that item's stored score**, read through the subject traversal for every approvable type with a row (BereanReview.md rule 2). `null` where no round carries the id.
2. **It reads the item unfiltered**, so a non-public item answers its own score, and an item that is gone answers `IsSubjectAvailable = false` — never a null score that could pass for "not yet scored" (BereanReview.md rule 20).
3. The data change reads it to send a clear only where a score stands (`Backend/Orchestrations/ApprovalOrchestrationService.BereanReview.md §2`). The review process may read it too, to skip the scoring where a score stands (BereanReview.md rule 6); that is #1216's to decide.
4. **This section owns the traversal's change, and it reaches two decisions at once.** The traversal is one private read, `ResolveEntityAsync`, whose `Association` arm alone hands a score today and whose seven other arms — `ContentItem`, `Link`, `Tag`, `Reaction`, `Comment`, `BibleReference` and `Attachment` — hand `null`. Extended to hand each arm's row `ConfidenceScore`, it hands that score to `EvaluateApprovalConditionsByIdAsync` (§4) and `MayDecideApprovalByIdAsync` (§6) as well, since both read the item through it. So this section pins the score on both, because a test of that change can fail first only where the change is made, and §4 and §6 add no score of their own. It pins it on its own gather too, for each of the seven arms: a gather that answered a score for an association alone would pass every other case. Until `Backend/Models/IApproval.md §2` maps the columns, every one of the seven reads back null, so the decisions are handed what they are handed today.

## 4. EvaluateApprovalConditionsByIdAsync — the item's score and Berean's assignment (#1180)

1. **The item's stored score comes through the subject traversal, for every type**, as §3 rule 4 extends it. This section adds nothing to the score.
2. **`IsAIReviewerAssigned`, `IsAIReviewStarted` and `IsAIReviewCompleted` are gathered** from the round's live assignment, read as §1 reads it: `true` and that row's two flags where one stands, and `false`, `false` and `false` where none does. They replace the `false`s `Backend/Clients/AccessClient.BereanReview.md §1` and `§5` pass.
3. **It waits for the review process** (BereanReview.md, *What ships together* step 4): once it is merged, every round Berean is assigned to is held until its review records a start and a completion, which #1216 builds.

## 5. MayDecideApprovalAsync — Berean's assignment (#1181)

1. **`IsAIReviewerAssigned`, `IsAIReviewStarted` and `IsAIReviewCompleted` are gathered** for the round the query names, as §4 rule 2 gathers them, in place of the `false`s `Backend/Clients/AccessClient.BereanReview.md §2` and `§6` pass. The score is the foundation's stored one, which the query carries (each item's `Transition<Entity>ApprovalAsync`, `Backend/Foundations/<Entity>Service.BereanReview.md §5`).
2. It waits as §4 does, **and lands after all six items' §5**: before them, the query carries no score for those six, which reads as `0.00` once Berean's assignment is gathered, so a manual approve of any of their rounds Berean is assigned to would be refused on a score nobody recorded, while §4's conditions, read through §3's traversal, call the same round met.

## 6. MayDecideApprovalByIdAsync — the item's score and Berean's assignment (#1182)

The approval record's own decision door (§APR9.7.5, *Built on the `Approval` row*), which reads the item through the same traversal.

1. **The item's stored score is the traversal's**, for every item type, as §3 rule 4 extends it, and **`IsAIReviewerAssigned`, `IsAIReviewStarted` and `IsAIReviewCompleted` are gathered** as §4 rule 2 gathers them.
2. It waits as §4 does.
3. **Whichever of §4, §5 and §6 merges last rewrites §APR8.6.2's account of the blocks**, which it makes untrue, as it takes off §APR8.6.1's parenthesis: the clause *"and the three blocks the review feeds (§APR8.5 rule 8)"* leaves the paragraph's list of what is not built, and *Berean's review*'s item 5, *"The review feeds three blocks"*, says that they are built. §4 and §5 each rewrite §APR8.5 rule 8's *Not yet built* note for their own gather.

## 7. IsAIReviewerEverAssignedAsync, retired (#1228)

Gate 6's unfiltered presence read (§APR8.6.2.1, *The security boundary* rule 3) has one caller, the automatic assignment's two subscriptions, and both leave `AIReviewerOrchestrationService` (`Backend/Orchestrations/AIReviewerOrchestrationService.BereanReview.md §5–§6`). Once both are gone it leaves `IAccessBroker` and `AccessBroker`, with its tests — `AccessBrokerTests` and the integration suite's `AIReviewerEverAssignedReadTests` (`Glory2Him.Core.Tests.Integration/Services/Foundations/AIReviewerAssignments/`), which calls it. The approval process's step 4.2 asks a different question, whether a live assignment stands, through §1 (BereanReview.md rule 9). `IStorageBroker.SelectAllAIReviewerAssignmentsAsync`, which the read composed its condition over, stays: it is the collection read every other entity on that broker carries. Its summary, which rests it on the automatic assignment's need to tell a round nobody has decided about from one whose row a moderator withdrew, says instead that it is that collection read. §APR8.6.2 setting 3's account of the automatic assignment, which still lists this read and the retired subscriptions among what *"all exist"*, is rewritten with it, as the last of the move: the approval process's step 4 consumes the verdict, and the subscriptions and this read are retired.
