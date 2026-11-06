# Access client
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: client — `IAccessClient` (`Clients/SecurityClients/G2H.Security.Client/Clients/Access/IAccessClient.cs`), `securityClient.Access`, the pure decision function of §APR8.6.1 rule 2, served by `AccessService`
Inherits: §APR8.6 HR-1 and HR-5, §APR8.6.1 rules 2 and 5, §APR8.6.3 rule 7, §APR7.7 rule 1, §ARC12.3.1 (the eligibility row), §SEC18.6 rule 2

The client decides and gathers nothing: its caller hands it every row a decision needs, and every section of every request is `required`, so a forgotten gather is a compile error rather than an empty list read as permission (§APR8.6.1 rule 2). HR-5 joins HR-1 in the one decision every review write asks.

## 1. MayRecordApprovalReviewAsync — HR-5 (#1011)

```csharp
ValueTask<AccessVerdict> MayRecordApprovalReviewAsync(
    RecordReviewRequest recordReviewRequest);
```

1. **`RecordReviewRequest` gains `EntityAmenderUserIds`**, `required`, the account ids the entity's amender record names (§APR8.6.3 rule 1), beside `EntityCreatedBy`.
2. **An actor it names is refused with `AccessDenialReason.AmendedContentReviewNeverPermitted`**, a new member, matched on the account id as HR-1 matches the author: ordinally, and a blank id never matching. No setting relaxes it and no role is exempt, `Administrators` included (§APR8.6 HR-5).
3. **It is asked after HR-1 and before the window check**, so the read-only veto, the tier and HR-1 keep the refusals they give today, and an amender is refused on a round in any state.
4. **It is asked on every call**, the amendment of an actor's own review included (`IsAmendingOwnReview`), as HR-1 is — so the add, the amendment and the withdrawal of a review all refuse an amender (§ARC12.3.1, the eligibility row).
