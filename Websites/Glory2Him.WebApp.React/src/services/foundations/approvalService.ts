import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ApprovalBroker from '../../brokers/apiBroker.approvals';

import {
    ApprovalOutcome,
    ApprovalReview,
    ApprovalReviewRequest,
    ApprovalVerdict,
    EntityTypeName,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../../models/foundations/approvals/approval';

import {
    ApprovalDecision,
    ApprovalStatus
} from '../../models/components/approvals/approvalReviewItem';

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
    },

    // ── Writes ────────────────────────────────────────────────────────────────
    //
    // EVERY WRITE INVALIDATES THE WHOLE ROUND, not the one read it obviously moved. A vote
    // changes the verdict's count and its block reasons; a request changes who is outstanding
    // AND who may still be asked; a decision closes the round and moves the item itself. The
    // panel reads all of them off one screen, and a screen that refetched only the obvious one
    // would show a verdict that disagreed with the votes beside it.
    //
    // suppressGlobalErrorToast on all four: a refusal here is an ANSWER (§14.5) — the server
    // says why a vote is refused or a bypass is not yours to make — and the page shows that
    // reason beside the control rather than letting the generic toast talk over it.

    // A vote is a row (§7.7). One active review per reviewer per round, so the first vote is a
    // POST and every change after it is a PUT of the row that was read — which is why the
    // caller passes the standing review when there is one rather than a bare verdict.
    useCastApprovalReview: () => {
        const approvalBroker = new ApprovalBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                approvalId: string;
                vote: ApprovalStatus;
                standingReview?: ApprovalReview;
            }) => request.standingReview == null
                ? await approvalBroker.PostApprovalReviewAsync({
                    id: crypto.randomUUID(),
                    approvalId: request.approvalId,
                    statusId: request.vote,
                    comment: '',
                    isDeleted: false
                })
                : await approvalBroker.PutApprovalReviewAsync({
                    ...request.standingReview,
                    statusId: request.vote
                }),

            onSuccess: (_, request) => invalidateRound(queryClient, request.approvalId)
        });
    },

    // THE DECISION (§16.7.3). The item's own status follows through the workflow, so the item
    // and every feed holding it are invalidated alongside the round.
    useDecideApproval: () => {
        const approvalBroker = new ApprovalBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                entityType: EntityTypeName;
                entityId: string;
                decision: ApprovalDecision;
                isBypassRequested: boolean;
                bypassReason: string;
            }): Promise<ApprovalOutcome> =>
                await approvalBroker.PostApprovalDecisionAsync(
                    request.entityType,
                    request.entityId,
                    request.decision,
                    request.isBypassRequested,
                    request.bypassReason),

            onSuccess: (outcome, request) => {
                invalidateRound(queryClient, outcome.approvalId);
                queryClient.invalidateQueries({
                    queryKey: ['ContentItemsGetById', request.entityId]
                });
                queryClient.invalidateQueries({ queryKey: ['ContentItemsSearch'] });
            }
        });
    },

    useRequestReview: () => {
        const approvalBroker = new ApprovalBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                entityType: EntityTypeName;
                entityId: string;
                requestedUserId: string;
            }) =>
                await approvalBroker.PostReviewRequestAsync(
                    request.entityType, request.entityId, request.requestedUserId),

            onSuccess: (reviewRequest) => invalidateRound(queryClient, reviewRequest.approvalId)
        });
    },

    useWithdrawReviewRequest: () => {
        const approvalBroker = new ApprovalBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                entityType: EntityTypeName;
                entityId: string;
                requestedUserId: string;
            }) =>
                await approvalBroker.DeleteReviewRequestAsync(
                    request.entityType, request.entityId, request.requestedUserId),

            onSuccess: (reviewRequest) => invalidateRound(queryClient, reviewRequest.approvalId)
        });
    }
};

// The four reads a round is made of, by prefix: the verdict and the candidates and requests
// are keyed by entity, the reviews by approval. Prefix-matched rather than reconstructed, so a
// write that knows only the approval's id still reaches the entity-keyed reads.
const invalidateRound = (
    queryClient: ReturnType<typeof useQueryClient>,
    approvalId: string) => {
    queryClient.invalidateQueries({ queryKey: ['ApprovalVerdict'] });
    queryClient.invalidateQueries({ queryKey: ['ApprovalReviews', approvalId] });
    queryClient.invalidateQueries({ queryKey: ['ReviewerCandidates'] });
    queryClient.invalidateQueries({ queryKey: ['ReviewRequests'] });
};
