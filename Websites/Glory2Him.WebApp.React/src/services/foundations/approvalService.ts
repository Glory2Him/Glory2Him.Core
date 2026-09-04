import { useQuery } from '@tanstack/react-query';
import ApprovalBroker from '../../brokers/apiBroker.approvals';

import {
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    EntityTypeName,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../../models/foundations/approvals/approval';

// The approval round's reads, one hook per endpoint. What ORDERS them is that only the verdict
// knows the approval's id: an ApprovalReview names the approval it belongs to and nothing about
// the post it judges, so the reviews read waits on the verdict rather than racing it.
//
// NOTHING IS RETRIED AND NOTHING IS CACHED LONG. A round moves while it is being looked at — a
// vote lands, a comment is resolved, and the block reasons change under the moderator — so a
// short staleTime is the honest setting for every read here. Retry is off because the refusals
// these endpoints give are ANSWERS, not failures: an entity with no approval row is a 404, and a
// caller outside the moderation tier is a 404 by design (§14.5 rule 1, so the endpoint cannot be
// used to probe what exists). Retrying either just delays the panel settling.
const approvalStaleTime = 15 * 1000;

export const approvalService = {
    useGetApprovalVerdict: (
        entityType: EntityTypeName,
        entityId: string,
        enabled = true) => {
        const approvalBroker = new ApprovalBroker();

        return useQuery<ApprovalVerdict>({
            queryKey: ['ApprovalVerdict', entityType, entityId],
            queryFn: async () =>
                await approvalBroker.GetApprovalVerdictAsync(entityType, entityId),
            enabled: enabled && entityId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalStaleTime
        });
    },

    useGetApprovalReviews: (approvalId: string, enabled = true) => {
        const approvalBroker = new ApprovalBroker();

        return useQuery<ApprovalReview[]>({
            queryKey: ['ApprovalReviews', approvalId],
            queryFn: async () => await approvalBroker.GetApprovalReviewsAsync(approvalId),
            enabled: enabled && approvalId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalStaleTime
        });
    },

    useGetReviewerCandidates: (
        entityType: EntityTypeName,
        entityId: string,
        enabled = true) => {
        const approvalBroker = new ApprovalBroker();

        return useQuery<ReviewerCandidate[]>({
            queryKey: ['ReviewerCandidates', entityType, entityId],
            queryFn: async () =>
                await approvalBroker.GetReviewerCandidatesAsync(entityType, entityId),
            enabled: enabled && entityId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalStaleTime
        });
    },

    useGetReviewRequests: (
        entityType: EntityTypeName,
        entityId: string,
        enabled = true) => {
        const approvalBroker = new ApprovalBroker();

        return useQuery<ApprovalReviewRequest[]>({
            queryKey: ['ReviewRequests', entityType, entityId],
            queryFn: async () =>
                await approvalBroker.GetReviewRequestsAsync(entityType, entityId),
            enabled: enabled && entityId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalStaleTime
        });
    },

    // The ids are the KEY as well as the argument, sorted so the same set asked in a different
    // order is the same cache entry rather than a second round trip for the same answer.
    useGetReviewerDisplayNames: (userIds: ReadonlyArray<string>, enabled = true) => {
        const approvalBroker = new ApprovalBroker();
        const sortedUserIds = [...userIds].sort();

        return useQuery<ReviewerDisplayName[]>({
            queryKey: ['ReviewerDisplayNames', sortedUserIds],
            queryFn: async () =>
                await approvalBroker.GetReviewerDisplayNamesAsync(sortedUserIds),
            enabled: enabled && sortedUserIds.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalStaleTime
        });
    }
};
