import ApiBroker from './apiBroker';

import {
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    EntityTypeName,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../models/foundations/approvals/approval';

// The approval round's reads. Two hosts answer them and the split is not arbitrary: the
// ORCHESTRATION endpoints under api/Approvals answer per entity and per caller — the verdict,
// who may be asked, who has been — while api/ApprovalReviews is the plain foundation collection,
// OData-filtered like the other foundation reads in this folder.
class ApprovalBroker {
    relativeApprovalsUrl = '/api/approvals';
    relativeApprovalReviewsUrl = '/api/approvalreviews';
    private apiBroker: ApiBroker = new ApiBroker();

    // WHAT MAY HAPPEN TO THIS APPROVAL NOW, answered per caller (§16.7.2). It is also the only
    // way to learn the approval's ID from the entity's, which is why every other read in this
    // round waits on it.
    async GetApprovalVerdictAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<ApprovalVerdict> {
        const url = `${this.relativeApprovalsUrl}/${entityType}/${entityId}/Verdict`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalVerdict;
    }

    // The votes actually cast, keyed by the APPROVAL rather than the entity. Soft-deleted rows
    // are filtered server-side rather than thrown away here: a withdrawn review is no opinion,
    // and a page of them would waste the round trip it took to fetch.
    async GetApprovalReviewsAsync(approvalId: string): Promise<ApprovalReview[]> {
        const filter = `approvalId eq ${approvalId} and isDeleted eq false`;
        const url = `${this.relativeApprovalReviewsUrl}?$filter=${encodeURIComponent(filter)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalReview[];
    }

    async GetReviewerCandidatesAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<ReviewerCandidate[]> {
        const url =
            `${this.relativeApprovalsUrl}/${entityType}/${entityId}/ReviewerCandidates`;

        const result = await this.apiBroker.GetAsync(url);

        return result.data as ReviewerCandidate[];
    }

    async GetReviewRequestsAsync(
        entityType: EntityTypeName,
        entityId: string): Promise<ApprovalReviewRequest[]> {
        const url = `${this.relativeApprovalsUrl}/${entityType}/${entityId}/ReviewRequests`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ApprovalReviewRequest[];
    }

    // The names behind the account ids a review row carries — ONE round trip for the whole
    // round, repeating the parameter rather than joining on a comma, which is how the host's
    // string[] binder reads a query array and what keeps a name containing one intact.
    async GetReviewerDisplayNamesAsync(
        userIds: ReadonlyArray<string>): Promise<ReviewerDisplayName[]> {
        const query = userIds
            .map((userId) => `userIds=${encodeURIComponent(userId)}`)
            .join('&');

        const url = `${this.relativeApprovalsUrl}/ReviewerDisplayNames?${query}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ReviewerDisplayName[];
    }
}

export default ApprovalBroker;
