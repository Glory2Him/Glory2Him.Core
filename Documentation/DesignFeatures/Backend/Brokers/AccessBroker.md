# Access broker
Parent: [Likes.md](../../Likes.md)
Level: broker — `IAccessBroker` (`Glory2Him.Core/Brokers/Securities/`), the policy gatherer of §APR8.6.1 rule 3
Inherits: §APR8.6.1 rules 3 and 4, §ARC12.2.1 rules 3–6, §ARC16.2.1, §ARC16.8 (*Where each shared rule lives*), §DOM6.4, §DOM6.6, §APR9.7.4

`AccessBroker` gathers and does not decide; its gathers are unit-tested, which is where this solution tests them (`AccessBrokerTests.*.Logic.cs`). The Likes feature gives it two new arms. Each authors its condition as a query-shaping function and hands it down a storage-broker pass-through (§ARC12.2.1 rule 3), so its unit tests execute the condition over an in-memory set seeded with a matching row and, for each term, a row that misses on that term alone (rule 5). Integration proves what that set cannot (rule 6). The arms that exist today query through `SelectAll*Async` and run their terminal operator in memory; converting them is §ARC12.2.1's work, not this feature's, and the two arms below do not copy them.

## 1. RetrieveEffectiveContentItemSettingsAsync (#713)

```csharp
ValueTask<IReadOnlyList<EffectiveContentItemSetting>> RetrieveEffectiveContentItemSettingsAsync(
    IReadOnlyList<ContentItemSettingKey> contentItemSettingKeys,
    CancellationToken cancellationToken = default);
```

`ContentItemSettingKey` is `(ContentType ContentType, Guid ContentItemId)`; `EffectiveContentItemSetting` is `(Guid ContentItemId, ContentItemSetting ContentItemSetting)`.

1. **It returns the winning row per key: the item's live override where one exists, the live default for the item's content type otherwise** (§DOM6.4). The tiers are a selection, never a merge (§DOM6.9 rule 4). This arm is the single home of that precedence (§ARC16.8's shared-rule table, the §DOM6.10 row), and every consumer reaches it rather than restating it.
2. **A soft-deleted row never wins** (§DOM6.6): a deleted override leaves its item on its type's default.
3. **A key that resolves no row is absent from the answer.** A type default always exists in the product (§ARC12.5.2 business rule 5), so an absent key means the store is not what the design says, and each caller treats absence as its own rules say — the facet gate refuses (§ARC16.2.1, *the gate never falls open*).
4. **One query answers every key**, shaped here and awaited in the client through `SelectContentItemSettingsAsync` (`Backend/Brokers/StorageBroker.md §3`), so the selection runs in SQL in one round trip and no candidate list is picked over above it (§ARC16.8, the §DOM6.10 row).
5. **It decides nothing further.** It returns the row; which switch matters is its caller's question. This departure from the broker's verdict charter is declared in §ARC16.2.1 and is not repeated here.

Its two callers are the facet gate on the association write (`Backend/Orchestrations/AssociationOrchestrationService.md §1 and §2`) and the summary read's §SEC14.3 rule 6 (`Backend/Orchestrations/AssociationOrchestrationService.md §4`).

## 2. FindDismissableApprovalReviewsAsync (#714)

```csharp
ValueTask<IReadOnlyList<DismissableApprovalReview>> FindDismissableApprovalReviewsAsync(
    Guid approvalId,
    CancellationToken cancellationToken = default);
```

`DismissableApprovalReview` is `(Guid Id, DateTimeOffset CreatedWhen, bool IsRejection)`.

1. **It returns the round's active reviews** — the reviews of that approval that are not soft deleted and not `Dismissed` — the same set `FindDismissableApprovalReviewIdsAsync` answers with ids alone, with each review's `CreatedWhen` and whether it rejects (`StatusId` is `Rejected`).
2. **It is unfiltered by the caller**, for the reason `FindDismissableApprovalReviewIdsAsync` records: the flow that reads it runs as the reader who changed their reaction, who may see none of the round's reviews, and an identity-filtered read never decides an invariant (§APR9.7.4).
3. **It is not bounded by a time.** The changed-reaction flow compares each review's `CreatedWhen` with the change's itself, because one of its cases needs the new pair's rejections as well as the old pair's (§APR9.7.4).
4. **`FindDismissableApprovalReviewIdsAsync` stays as it is**, for the callers that dismiss every active review; nothing here changes what they read.

Its caller is the changed-reaction flow (`Backend/Orchestrations/ApprovalOrchestrationService.md §1`).
