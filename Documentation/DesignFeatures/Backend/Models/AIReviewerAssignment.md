# AI reviewer assignment — where Berean's review stands
Parent: [BereanReview.md](../../BereanReview.md)
Level: model — `AIReviewerAssignment` (`Glory2Him.Core/Models/Foundations/AIReviewerAssignments/`), its storage configuration (`Glory2Him.Core/Brokers/Storages/Sql/StorageBroker.AIReviewerAssignment.Configurations.cs`) and one migration
Inherits: §APR8.6.2, *Assignment*, BereanReview.md rule 21, *Storage and migration* item 5, *What ships together* step 2

The owner ruled on 2026-10-14 that Berean's review is recorded on its assignment row: *"Berean's status is stored on the AIReviewerAssignment row, in AIReviewerAssignment.cs on these properties: IsAIReviewStarted = true, AIReviewQueuedAt as DateTimeOffset, IsAIReviewCompleted = true, AIReviewCompletedAt as DateTimeOffset, IsAIReviewCommentsPresent = true"* (BereanReview.md, I1.5a). `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` exist; this story adds the other three. It reaches storage in two steps, because a column that exists before the caller-facing add refuses the fields, and before the caller-facing modify has gone, would store whatever a caller sends: the model first, ignored by storage, and the columns once both writes keep a caller's copy out. Groundwork has no operation and no test paths of its own. Each section's proof is the build, CI's model checks, and §2's integration suite.

## 1. The model, not yet stored (#1217)

1. **`AIReviewerAssignment` gains three properties**: `bool IsAIReviewStarted`, `false` by default; and `DateTimeOffset? AIReviewQueuedAt` and `DateTimeOffset? AIReviewCompletedAt`, each null by default.
2. **Storage ignores the three** in `StorageBroker.AIReviewerAssignment.Configurations.cs`, so no column exists, no migration is added and nothing a caller sends is stored. §2 replaces each ignore with the column.
3. **Each property's summary says what it records and who writes it**: that the review process sets the start pair as it starts the review and the completion time as it completes it (#1216), that the approval process clears all five tracking fields on a data change (BereanReview.md rule 9, step 3), and that nothing else clears them (rule 21). The class summary's *"The two flags are system-only, today"* paragraph says the same of all five, and stops saying that `Modify` exists for the future process.
4. **Berean's status read is unchanged.** `AIReviewerStatus` carries `IsAIReviewCompleted` and `IsAIReviewCommentsPresent` alone, which is all Berean's row shows (BereanReview.md rule 8).

## 2. The columns and their migration (#1219)

1. **`AIReviewerAssignments` gains** `IsAIReviewStarted bit NOT NULL` with a default of `0`, `AIReviewQueuedAt datetimeoffset NULL` and `AIReviewCompletedAt datetimeoffset NULL`, mapped as the table's two existing flags and its audit times are.
2. **One migration**: three `ADD`s, the bit's default filling every existing row in the same statement, as `20260913140000_AddApprovalSettingAutomaticAIReviewerRequest` fills its column. Existing rows read `0` and null, which is true of every one, since nothing has ever started or completed an assignment (§APR8.6.2, *Trigger event*). The script is one batch with nothing to `EXEC`. No index: every reader reads one round's live row through `UX_AIReviewerAssignments_ApprovalId`.
3. **It waits for the caller-facing add's refusal** (`Backend/Foundations/AIReviewerAssignmentService.md §6`) **and the caller-facing modify's retirement** (§4, #1165) on `main`. Mapped before either, a caller could store a review as started or complete.
4. **It comes before the review process** (BereanReview.md, *What ships together* step 4): a start written with no column is stored nowhere.
5. `Down` drops the three columns, for the developer's rollback alone. In effect the change is not reversible once a review has started, because dropping the columns loses where it stood (BereanReview.md, *Risks*).
