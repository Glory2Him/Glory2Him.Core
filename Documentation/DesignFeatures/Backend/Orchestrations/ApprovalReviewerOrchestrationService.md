# Approval reviewer orchestration service
Parent: [Approval.md §APR8.6](../../../Design/Approval.md) — HR-5 and its record, §APR8.6.3
Level: orchestration — `IApprovalReviewerOrchestrationService` (`Glory2Him.Core/Services/Orchestrations/ApprovalReviewers/`)
Inherits: §APR8.6 HR-5, §APR8.6.3 rule 7, §APR7.9 rule 3, §ARC16.7.4, §ARC12.5.4 business rules 1, 2 and 5

Who may be asked to review a round, and who is offered as a candidate, are this service's (§ARC12.5.4). HR-5 adds a third kind of person to the two it already refuses and subtracts — the owner and the blocked — and it reads them off the reviewer scope it already gathers (`Backend/Brokers/AccessBroker.Amenders.md §2`). No dependency is added (§ARC12.5.4 business rule 1). Both operations are addressed by the item today and move to the round's approval id under #699 (§ARC17.5); the rules below hold whichever key they carry when built.

## 1. RequestApprovalReviewAsync — an amender cannot be invited (#1017)

1. **A request aimed at anyone the entity's amender record names is refused**, as one aimed at the owner is (§APR7.9 rule 3, §APR8.6 HR-5), with the same exception family and nothing written.
2. **The record is `ApprovalReviewerScope.EntityAmenderUserIds`**, matched on the account id as the owner is.
3. **A pending invitation to someone who amends after it was made stays as it is** (§APR8.6.3 rule 7): this operation neither retires nor refuses to report it.

## 2. RetrieveReviewerCandidatesAsync — amenders are not candidates (#1018)

1. **Everyone the entity's amender record names is subtracted**, beside the owner and the blocked — the third of the read's three subtractions (§ARC16.7.4).
2. **Nobody else moves.** People already invited and people who have already answered stay in, as §ARC16.7.4 has them, and the read's posture — the requesting tier, and an account id, display name and username per person — is unchanged.
