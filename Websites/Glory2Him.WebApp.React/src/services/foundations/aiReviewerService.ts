import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import AIReviewerBroker from '../../brokers/apiBroker.aiReviewers';

import {
    AIReviewerAssignment,
    AIReviewerStatus,
    EntityTypeName
} from '../../models/foundations/approvals/approval';

// BEREAN'S OWN SLICE (design §8.6.2) — one read and two writes over api/AIReviewers, split from
// approvalService for the same reason the endpoints were split from the approval contract: an AI
// assignment is not part of the approval round. The round is what may happen to an approval and
// who is judging it; this is whether Berean is offered, whether it has been asked and how far it
// has got. One service, one broker, one subject.
//
// NOTHING IS RETRIED AND NOTHING IS CACHED LONG, matching the round's reads this one is asked
// alongside: the status moves while it is being looked at, and the refusals this endpoint gives
// are ANSWERS rather than failures — a caller outside the moderation tier is a 404 by design
// (§14.5 rule 1, so the endpoint cannot be used to probe what exists). Retrying just delays the
// panel settling.
const aiReviewerStaleTime = 15 * 1000;

// THE ONE READ THAT CAN ANSWER DIFFERENTLY after either write, spelled out in full rather than
// left as the bare ['AIReviewerStatus'] prefix: that prefix matches every entity's status query,
// so a write against one post would refetch Berean's status on every other round the client is
// holding. approvalService's invalidateRound keeps the prefix for the opposite reason — a
// round-wide write knows only the approval's id and cannot reconstruct the entity-keyed ones.
const invalidateAIReviewerStatus = (
    queryClient: ReturnType<typeof useQueryClient>,
    entityType: EntityTypeName,
    entityId: string) =>
    queryClient.invalidateQueries({
        queryKey: ['AIReviewerStatus', entityType, entityId]
    });

export const aiReviewerService = {
    // Keyed by ENTITY, like the round's candidates and requests, so it starts alongside them
    // rather than waiting on the verdict's approval id.
    useGetAIReviewerStatus: (
        entityType: EntityTypeName,
        entityId: string,
        enabled = true) => {
        const aiReviewerBroker = new AIReviewerBroker();

        return useQuery<AIReviewerStatus>({
            queryKey: ['AIReviewerStatus', entityType, entityId],
            queryFn: async () =>
                await aiReviewerBroker.GetAIReviewerStatusAsync(entityType, entityId),
            enabled: enabled && entityId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: aiReviewerStaleTime
        });
    },

    // ── Writes ────────────────────────────────────────────────────────────────
    //
    // suppressGlobalErrorToast on both, for the same reason every write on the round carries it:
    // a refusal here is an ANSWER (§14.5) — the server says why an assignment is not the
    // caller's to make — and the page shows that reason beside the control rather than letting
    // the generic toast talk over it.
    //
    // NEITHER INVALIDATES THE ROUND the way every human write does. Nothing about Berean's own
    // assignment moves a human's votes, candidates or requests — the one thing these writes can
    // change is answered by the one read above.

    // ASSIGN Berean — or RE-REQUEST it once its review has completed; the endpoint is the same
    // upsert either way, so one hook covers both actions.
    useAssignAIReviewer: () => {
        const aiReviewerBroker = new AIReviewerBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                entityType: EntityTypeName;
                entityId: string;
            }): Promise<AIReviewerAssignment> =>
                await aiReviewerBroker.PostAIReviewerAsync(request.entityType, request.entityId),

            onSuccess: (_, request) =>
                invalidateAIReviewerStatus(queryClient, request.entityType, request.entityId)
        });
    },

    // WITHDRAW BEREAN: a DELETE of the live assignment, answering 200 with the removed row or
    // 204 with nothing when none was standing — which is why the broker's result is nullable and
    // why nothing here reads it. The status read is what says whether Berean is assigned.
    //
    // The same narrow, entity-keyed invalidation as the assign above, and it is the whole
    // repaint: with isRequested false the panel drops Berean's row from the round and offers it
    // in the picker's Suggestions again.
    useWithdrawAIReviewer: () => {
        const aiReviewerBroker = new AIReviewerBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                entityType: EntityTypeName;
                entityId: string;
            }): Promise<AIReviewerAssignment | null> =>
                await aiReviewerBroker.DeleteAIReviewerAsync(
                    request.entityType, request.entityId),

            onSuccess: (_, request) =>
                invalidateAIReviewerStatus(queryClient, request.entityType, request.entityId)
        });
    }
};
