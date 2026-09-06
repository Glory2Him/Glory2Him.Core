import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ApprovalCommentBroker from '../../brokers/apiBroker.approvalComments';

import {
    ApprovalComment,
    ApprovalCommentType
} from '../../models/foundations/approvals/approvalComment';

// THE REVIEW THREAD, one hook per endpoint. Like the round beside it, nothing is retried and
// nothing is cached long: a refusal from these endpoints is an ANSWER, not a failure — an
// approval nobody may see answers 404 by design (§14.5 rule 1, so the endpoint cannot be used to
// probe what exists) — and retrying only delays the panel settling.
//
// A THREAD MOVES WHILE IT IS BEING READ, and more visibly than the round does: two moderators
// working the same submission are exactly the case this surface exists for. THE POLLING IS NOT
// HERE, though. It belongs to the round's one freshness channel (§20.6.1) — useApprovalRound's
// refresh, driven by useApprovalRoundChanges — because that channel already answers three things
// a refetchInterval on this query could not: it does not poll a hidden tab, it refreshes on
// reconnect, and its refetches join an in-flight read instead of cancelling and reissuing it.
// One channel is also one thing to replace when SignalR lands.
const approvalCommentStaleTime = 10 * 1000;

export const approvalCommentService = {
    useGetApprovalComments: (approvalId: string, enabled = true) => {
        const approvalCommentBroker = new ApprovalCommentBroker();

        return useQuery<ApprovalComment[]>({
            queryKey: ['ApprovalComments', approvalId],
            queryFn: async () =>
                await approvalCommentBroker.GetApprovalCommentsAsync(approvalId),
            enabled: enabled && approvalId.length > 0,
            retry: false,
            meta: { suppressGlobalErrorToast: true },
            staleTime: approvalCommentStaleTime
        });
    },

    // ── Writes ────────────────────────────────────────────────────────────────
    //
    // EVERY WRITE INVALIDATES THE VERDICT AS WELL AS THE THREAD, and that is not belt-and-braces.
    // A comment born outstanding, one settled, and one withdrawn each move
    // unresolvedApprovalCommentCount and the block reasons ReviewPanel renders on the same
    // screen. Refetching only the thread would leave a moderator reading "held by 2 unresolved
    // comments" beside a thread showing none outstanding.
    //
    // suppressGlobalErrorToast on all four: a refusal here is an ANSWER (§14.5) — the round has
    // closed, the row is not yours to edit — and the page shows that reason beside the control
    // rather than letting the generic toast talk over it.

    // A COMMENT IS A ROW (§7.8). The id is minted here because the foundation refuses an empty
    // Guid and never generates one.
    //
    // isResolved IS DERIVED FROM THE TYPE, and this is the one place that mapping lives: a
    // Question is born OUTSTANDING and holds the approval shut, a Comment is born SETTLED and
    // never blocks. That is §7.8's own sentence rather than a rule this client invents — the add
    // path deliberately applies no rule to the field, so a caller who said nothing would make
    // every remark a blocker.
    useAddApprovalComment: () => {
        const approvalCommentBroker = new ApprovalCommentBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                approvalId: string;
                comment: string;
                commentType: ApprovalCommentType;
            }) => await approvalCommentBroker.PostApprovalCommentAsync({
                id: crypto.randomUUID(),
                approvalId: request.approvalId,
                comment: request.comment,
                commentType: request.commentType,
                isResolved: request.commentType === ApprovalCommentType.Comment,
                isDeleted: false
            }),

            onSuccess: (_, request) =>
                invalidateThread(queryClient, request.approvalId)
        });
    },

    // AN EDIT IS A PUT OF THE ROW THAT WAS READ, audit fields and all — the foundation compares
    // CreatedBy, CreatedWhen, ApprovalId and UpdatedWhen against storage before it will accept
    // the write, so a fresh object carrying only the new words is refused.
    useModifyApprovalComment: () => {
        const approvalCommentBroker = new ApprovalCommentBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (approvalComment: ApprovalComment) =>
                await approvalCommentBroker.PutApprovalCommentAsync(approvalComment),

            onSuccess: (approvalComment) =>
                invalidateThread(queryClient, approvalComment.approvalId)
        });
    },

    // SOFT removal. The row stays and goes quiet, which is what leaves the §8.5 block: the
    // evaluation counts comments where IsDeleted is false && IsResolved is false, so withdrawing
    // an outstanding one UNBLOCKS the approval — which is exactly why the verdict is invalidated
    // beside the thread.
    useRemoveApprovalComment: () => {
        const approvalCommentBroker = new ApprovalCommentBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                approvalCommentId: string;
                deletionReason: string;
            }) => await approvalCommentBroker.DeleteApprovalCommentByIdAsync(
                request.approvalCommentId,
                request.deletionReason),

            onSuccess: (approvalComment) =>
                invalidateThread(queryClient, approvalComment.approvalId)
        });
    },

    // THE SETTLED FLAG, both ways (§14.7 rule 5). Symmetric by design: a comment recorded as an
    // observation may later turn out to need action, and one settled prematurely must be able to
    // block again.
    useResolveApprovalComment: () => {
        const approvalCommentBroker = new ApprovalCommentBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (request: {
                approvalCommentId: string;
                isResolved: boolean;
            }) => await approvalCommentBroker.PostApprovalCommentResolveAsync(
                request.approvalCommentId,
                request.isResolved),

            onSuccess: (approvalComment) =>
                invalidateThread(queryClient, approvalComment.approvalId)
        });
    }
};

// The thread, the verdict that counts it, and the names that render it. The last two are
// prefix-matched rather than reconstructed, because both are keyed by ENTITY and a comment knows
// only its approval.
//
// THE VERDICT because the count it carries is what the review panel beside this one renders as a
// block reason.
//
// THE NAMES because a FIRST comment adds somebody the round did not involve before — the
// submitter answering a question is the ordinary case — and the name read is round-keyed, so
// until it is asked again the author of the row that just landed renders as "Unknown author".
// This is the same reason invalidateRound refreshes them after a first vote or a fresh
// invitation; it was missing here, and it showed.
const invalidateThread = (
    queryClient: ReturnType<typeof useQueryClient>,
    approvalId: string) => {
    queryClient.invalidateQueries({ queryKey: ['ApprovalComments', approvalId] });
    queryClient.invalidateQueries({ queryKey: ['ApprovalVerdict'] });
    queryClient.invalidateQueries({ queryKey: ['ReviewerDisplayNames'] });
};
