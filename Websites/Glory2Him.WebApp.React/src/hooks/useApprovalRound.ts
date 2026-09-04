import { useMemo } from 'react';
import { approvalService } from '../services/foundations/approvalService';
import { EntityTypeName } from '../models/foundations/approvals/approval';

import {
    ApprovalReviewItem,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../models/components/approvals/approvalReviewItem';

import {
    toApprovalReviewItem,
    toApprovalVerdictItem,
    toRequestedReviewerItem,
    toReviewerCandidateItem
} from '../services/views/approvals/toReviewPanelItems';

// ONE ROUND, ASSEMBLED. ReviewPanel takes five separate collections and no fetching of its own,
// and four endpoints answer them — so the assembling is a page's job, and every page that shows
// a round would otherwise write the same chain out again.
//
// THE CHAIN IS NOT A CHOICE. Only the verdict knows the approval's id: an ApprovalReview names
// the approval it belongs to and nothing about the post it judges. So the verdict is read first,
// the reviews wait on the id it returns, and the reviewer names wait on the account ids those
// reviews carry. Two of the four reads are independent of that chain — the candidates and the
// outstanding requests are asked per ENTITY — and they start immediately alongside it.
//
// A REFUSAL IS AN ANSWER HERE. A post with no approval row 404s, and so does a caller outside
// the moderation tier (§14.5 rule 1, so the endpoint cannot be used to probe what exists). Both
// leave the verdict undefined, which is exactly what the panel wants for "no verdict": it shows
// the round read-only rather than a decision surface nobody is entitled to.
export const useApprovalRound = (
    entityType: EntityTypeName,
    entityId: string,
    enabled = true) => {
    const {
        data: approvalVerdict,
        isLoading: isVerdictLoading
    } = approvalService.useGetApprovalVerdict(entityType, entityId, enabled);

    const approvalId = approvalVerdict?.approvalId ?? '';

    const {
        data: approvalReviews,
        isLoading: areReviewsLoading
    } = approvalService.useGetApprovalReviews(approvalId, enabled);

    const { data: reviewerCandidates } =
        approvalService.useGetReviewerCandidates(entityType, entityId, enabled);

    const { data: reviewRequests } =
        approvalService.useGetReviewRequests(entityType, entityId, enabled);

    // Every reviewer named ONCE, however many rows they hold. The read is by id, so asking for
    // the same id twice would buy nothing and lengthen the URL.
    const reviewerUserIds = useMemo(
        () => Array.from(new Set((approvalReviews ?? []).map((review) => review.createdBy))),
        [approvalReviews]);

    const { data: reviewerDisplayNames } =
        approvalService.useGetReviewerDisplayNames(reviewerUserIds, enabled);

    const approvalReviewCollection: ReadonlyArray<ApprovalReviewItem> = useMemo(
        () => (approvalReviews ?? []).map(
            (review) => toApprovalReviewItem(review, reviewerDisplayNames ?? [])),
        [approvalReviews, reviewerDisplayNames]);

    const requestedReviewerCollection: ReadonlyArray<ReviewerCandidateItem> = useMemo(
        () => (reviewRequests ?? [])
            .filter((request) => request.isDeleted !== true)
            .map(toRequestedReviewerItem),
        [reviewRequests]);

    const reviewerCandidateCollection: ReadonlyArray<ReviewerCandidateItem> = useMemo(
        () => (reviewerCandidates ?? []).map(toReviewerCandidateItem),
        [reviewerCandidates]);

    const approvalVerdictItem: ApprovalVerdictItem | undefined = useMemo(
        () => approvalVerdict == null ? undefined : toApprovalVerdictItem(approvalVerdict),
        [approvalVerdict]);

    // THE ROWS ARE WHAT IS LOADING, not the verdict alone. The panel's isLoading holds back the
    // reviews AND the outcome derived from them, so it must stay true until the reviews the
    // verdict unlocked have arrived too — otherwise the round paints itself empty for a beat
    // between the two reads and reads as "nobody has reviewed this".
    const isLoading =
        isVerdictLoading
        || (approvalId.length > 0 && areReviewsLoading);

    return {
        approvalVerdict: approvalVerdictItem,
        approvalReviewCollection,

        // THE ROWS THEMSELVES, beside their projection: a changed vote is a PUT of the row that
        // was read (§7.7 rule 1), audit fields and all, and the projection deliberately carries
        // none of that.
        approvalReviews: approvalReviews ?? [],
        requestedReviewerCollection,
        reviewerCandidateCollection,
        isLoading
    };
};
