import {
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../../../models/foundations/approvals/approval';

import {
    ApprovalReviewItem,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../../../models/components/approvals/approvalReviewItem';

// The wire shapes projected down to what ReviewPanel renders. The panel takes no wire model on
// purpose — it renders an approval round, whatever produced one — so the mapping lives here
// rather than in the panel or in the page.

// A review row names its reviewer only by ACCOUNT ID. The names arrive from a separate read, so
// resolution is a lookup with an honest fallback: an account that has gone leaves the vote
// standing and the name absent, which is the right shape for both — a vote is a fact about the
// round, not about whoever is still on the system.
const resolvedNameOf = (
    userId: string,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName>
): ReviewerDisplayName | undefined =>
    reviewerDisplayNameCollection
        .find((reviewerDisplayName) => reviewerDisplayName.userId === userId);

const UnknownReviewerText = 'Unknown reviewer';

export const toApprovalReviewItem = (
    approvalReview: ApprovalReview,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName> = []
): ApprovalReviewItem => {
    // RESOLVED ONCE, and both labels read off that one row. Said as a claim in a comment first
    // and then not done — the display name and the username each ran their own lookup — which is
    // the shape of bug the single resolver (§16.7.4) exists to prevent: two lookups are two
    // chances to render one person as two.
    const resolved = resolvedNameOf(approvalReview.createdBy, reviewerDisplayNameCollection);

    return {
        id: approvalReview.id,

        // CreatedBy, not some reviewer column: the audit trail records who cast the vote, and
        // that is the id the panel compares against the signed-in user to find "my" row.
        reviewerUserId: approvalReview.createdBy,
        reviewerDisplayName: resolved?.displayName ?? UnknownReviewerText,

        // Absent where the id named no account, which is the same row the display name falls
        // back for.
        reviewerUserName: resolved?.userName,

        vote: approvalReview.statusId,
        isDeleted: approvalReview.isDeleted
    };
};

// A REQUEST carries the name it was addressed to, unlike a review — but it carries no username,
// and §16.7.4 is explicit that it never will: denormalising a name at write time leaves every
// historical row asserting something its owner may since have changed, and the trade is not
// repeated. So the username comes off the resolver, which names outstanding invitations as well
// as cast reviews. The DISPLAY name stays the row's own: it is the write-time record of who was
// asked, and it is what the panel labels the request with when the identity store says nothing.
export const toRequestedReviewerItem = (
    approvalReviewRequest: ApprovalReviewRequest,
    reviewerDisplayNameCollection: ReadonlyArray<ReviewerDisplayName> = []
): ReviewerCandidateItem => ({
    userId: approvalReviewRequest.requestedUserId,
    displayName: approvalReviewRequest.requestedUserDisplayName,

    userName: resolvedNameOf(
        approvalReviewRequest.requestedUserId, reviewerDisplayNameCollection)?.userName
});

// §16.7.4 returns the minimum a picker needs — an id, a name and a username — so there is
// nothing else to carry across. No suggestionReason: the panel's own comment is explicit that
// ranking people is the consumer's call and the panel must not invent one, and neither may this
// projection.
export const toReviewerCandidateItem = (
    reviewerCandidate: ReviewerCandidate): ReviewerCandidateItem => ({
        userId: reviewerCandidate.userId,
        displayName: reviewerCandidate.displayName,
        userName: reviewerCandidate.userName
    });

// The verdict crosses almost unchanged — it is already the per-caller answer the panel's gates
// are written against. entityType and entityId are dropped rather than carried: the panel is
// looking at one round and has no use for the address of the thing under it.
export const toApprovalVerdictItem = (
    approvalVerdict: ApprovalVerdict): ApprovalVerdictItem => ({
        approvalId: approvalVerdict.approvalId,
        approvalStatus: approvalVerdict.approvalStatus,
        blockReasons: approvalVerdict.blockReasons,
        isBlocked: approvalVerdict.isBlocked,
        isBypassAllowedForCurrentUser: approvalVerdict.isBypassAllowedForCurrentUser,
        canApprove: approvalVerdict.canApprove,
        approvalCount: approvalVerdict.approvalCount,
        requiredNumberOfApprovals: approvalVerdict.requiredNumberOfApprovals,
        unresolvedApprovalCommentCount: approvalVerdict.unresolvedApprovalCommentCount
    });
