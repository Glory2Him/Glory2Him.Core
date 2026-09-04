import ApiBroker from './apiBroker';

import {
    ApprovalOutcome,
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    EntityTypeName,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../models/foundations/approvals/approval';

import { ApprovalDecision } from '../models/components/approvals/approvalReviewItem';

// The approval round's reads and writes. Two hosts answer them and the split is not
// arbitrary: the ORCHESTRATION endpoints under api/Approvals answer per entity and per caller —
// the verdict, who may be asked, who has been, and the decision itself — while
// api/ApprovalReviews is the plain foundation collection, OData-filtered like the other
// foundation reads in this folder, and written to like them: a vote is a row.
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

    // ── Writes ────────────────────────────────────────────────────────────────

    // A VOTE IS A ROW. The id travels in the body, minted by the caller — the foundation
    // refuses an empty Guid and never generates one — and the audit fields are stamped
    // server-side from the caller's own identity, so nothing here claims who the reviewer is.
    async PostApprovalReviewAsync(approvalReview: ApprovalReview): Promise<ApprovalReview> {
        const result = await this.apiBroker.PostAsync(
            this.relativeApprovalReviewsUrl, approvalReview);

        return result.data as ApprovalReview;
    }

    // A CHANGED VOTE amends the row that was read (§7.7 rule 1: one active review per reviewer
    // per round), so the whole row goes — the foundation compares CreatedBy and CreatedWhen
    // against storage before it will accept the write.
    async PutApprovalReviewAsync(approvalReview: ApprovalReview): Promise<ApprovalReview> {
        const result = await this.apiBroker.PutAsync(
            this.relativeApprovalReviewsUrl, approvalReview);

        return result.data as ApprovalReview;
    }

    // THE DECISION (§16.7.3), keyed by the entity like the verdict that offered it. Everything
    // rides the query string: the host binds the enum from its number, and a bypass is a
    // REQUEST — what lands on the row comes back on the outcome, decided server-side against
    // the policy and the caller's tier, never copied from here.
    async PostApprovalDecisionAsync(
        entityType: EntityTypeName,
        entityId: string,
        decision: ApprovalDecision,
        isBypassRequested: boolean,
        bypassReason: string): Promise<ApprovalOutcome> {
        const parameters = new URLSearchParams();
        parameters.set('decision', String(decision));
        parameters.set('isBypassRequested', String(isBypassRequested));

        // Absent rather than empty: the orchestration treats a missing reason as the ordinary
        // "none supplied" and validates a bypass against that, so an empty string would be one
        // more thing for it to recognise as blank.
        if (bypassReason.trim().length > 0) {
            parameters.set('bypassReason', bypassReason.trim());
        }

        const url =
            `${this.relativeApprovalsUrl}/${entityType}/${entityId}/Decision?${parameters}`;

        const result = await this.apiBroker.PostAsync(url, {});

        return result.data as ApprovalOutcome;
    }

    // ASKING SOMEBODY (§7.9). Keyed by the entity; the round is resolved server-side.
    async PostReviewRequestAsync(
        entityType: EntityTypeName,
        entityId: string,
        requestedUserId: string): Promise<ApprovalReviewRequest> {
        const url =
            `${this.relativeApprovalsUrl}/${entityType}/${entityId}/ReviewRequests`
                + `?requestedUserId=${encodeURIComponent(requestedUserId)}`;

        const result = await this.apiBroker.PostAsync(url, {});

        return result.data as ApprovalReviewRequest;
    }

    // Withdrawing the ask. Refused server-side once the invitation has been answered (§7.9
    // rule 5), which the panel already keeps out of reach.
    async DeleteReviewRequestAsync(
        entityType: EntityTypeName,
        entityId: string,
        requestedUserId: string): Promise<ApprovalReviewRequest> {
        const url =
            `${this.relativeApprovalsUrl}/${entityType}/${entityId}/ReviewRequests`
                + `?requestedUserId=${encodeURIComponent(requestedUserId)}`;

        const result = await this.apiBroker.DeleteAsync(url);

        return result.data as ApprovalReviewRequest;
    }
}

export default ApprovalBroker;
