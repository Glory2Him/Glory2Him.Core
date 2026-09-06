import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useApprovalRound } from './useApprovalRound';
import { ApprovalStatus } from '../models/components/approvals/approvalReviewItem';

import {
    ApprovalReview,
    ApprovalVerdict,
    EntityTypeName
} from '../models/foundations/approvals/approval';

// WHICH READS THE ROUND FIRES, AND WITH WHAT, is this hook's own business, and nothing above it
// can catch a mistake here: every page test mocks approvalService wholesale, so its five reads
// become object literals whose refetch is a bare spy — one that models no `enabled` gate, no
// query key and no broker. A refresh that asked the server for `approvalId eq ` would satisfy
// every one of those assertions while failing against the real service. So this suite mocks the
// BROKER and nothing else, and asserts on the URLs that actually leave.
const getApprovalVerdictAsync = vi.fn();
const getApprovalReviewsAsync = vi.fn();
const getReviewerCandidatesAsync = vi.fn();
const getReviewRequestsAsync = vi.fn();
const getReviewerDisplayNamesAsync = vi.fn();

const getApprovalCommentsAsync = vi.fn();

vi.mock('../brokers/apiBroker.approvals', () => ({
    default: class {
        GetApprovalVerdictAsync = getApprovalVerdictAsync;
        GetApprovalReviewsAsync = getApprovalReviewsAsync;
        GetReviewerCandidatesAsync = getReviewerCandidatesAsync;
        GetReviewRequestsAsync = getReviewRequestsAsync;
        GetReviewerDisplayNamesAsync = getReviewerDisplayNamesAsync;
    }
}));

// THE THREAD RIDES THE SAME CHAIN, so it is mocked at the same boundary and for the same reason:
// its filter interpolates the approval id, so an ungated refetch would ask for
// `approvalId eq  and isDeleted eq false` — malformed, refused, and refused silently.
vi.mock('../brokers/apiBroker.approvalComments', () => ({
    default: class {
        GetApprovalCommentsAsync = getApprovalCommentsAsync;
    }
}));

const verdict: ApprovalVerdict = {
    entityType: 0,
    entityId: 'quote-1',
    approvalId: 'approval-1',
    approvalStatus: ApprovalStatus.Submitted,
    blockReasons: [],
    isBlocked: false,
    isBypassAllowedForCurrentUser: false,
    canApprove: true,
    approvalCount: 1,
    requiredNumberOfApprovals: 3,
    unresolvedApprovalCommentCount: 0
};

const review: ApprovalReview = {
    id: 'review-1',
    approvalId: 'approval-1',
    statusId: ApprovalStatus.Approved,
    comment: '',
    isDeleted: false,
    createdBy: 'user-john',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-john',
    updatedWhen: '2026-07-01T00:00:00Z'
};

describe('useApprovalRound', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const renderRound = (entityId = 'quote-1', enabled = true) =>
        renderHook(
            () => useApprovalRound(EntityTypeName.ContentItem, entityId, enabled),
            { wrapper });

    beforeEach(() => {
        vi.clearAllMocks();
        getApprovalVerdictAsync.mockResolvedValue(verdict);
        getApprovalReviewsAsync.mockResolvedValue([review]);
        getReviewerCandidatesAsync.mockResolvedValue([]);
        getReviewRequestsAsync.mockResolvedValue([]);
        getReviewerDisplayNamesAsync.mockResolvedValue([]);
        getApprovalCommentsAsync.mockResolvedValue([]);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should read the reviews with the id the verdict returned', async () => {
        // when
        const { result } = renderRound();

        // then
        await waitFor(() => expect(result.current.isLoading).toBe(false));

        expect(getApprovalVerdictAsync)
            .toHaveBeenCalledWith(EntityTypeName.ContentItem, 'quote-1');

        expect(getApprovalReviewsAsync).toHaveBeenCalledWith('approval-1');
        expect(result.current.approvalReviewCollection).toHaveLength(1);

        // the thread hangs off the same id, and waits on the same read to learn it
        expect(getApprovalCommentsAsync).toHaveBeenCalledWith('approval-1');
    });

    it('should re-read the round on refresh, still naming the approval', async () => {
        // given
        const { result } = renderRound();
        await waitFor(() => expect(result.current.isLoading).toBe(false));
        vi.clearAllMocks();

        // when
        await act(async () => { await result.current.refresh(); });

        // then
        expect(getApprovalVerdictAsync).toHaveBeenCalledTimes(1);
        expect(getApprovalReviewsAsync).toHaveBeenCalledWith('approval-1');
        expect(getReviewRequestsAsync).toHaveBeenCalledTimes(1);
        expect(getReviewerDisplayNamesAsync).toHaveBeenCalledTimes(1);

        // §20.6.1 names "a comment added or resolved" as a trigger, so the thread is in the
        // channel rather than polling on its own
        expect(getApprovalCommentsAsync).toHaveBeenCalledWith('approval-1');
    });

    // §7.9 leaves everybody in the candidate list whether or not they have answered, so no round
    // event moves it — and it is the most expensive of the five reads. Polling it four times a
    // minute for an answer that cannot change is the one read worth leaving out.
    it('should leave the candidates alone on refresh, since no round event moves them',
        async () => {
            // given
            const { result } = renderRound();
            await waitFor(() => expect(result.current.isLoading).toBe(false));
            expect(getReviewerCandidatesAsync).toHaveBeenCalledTimes(1);

            // when
            await act(async () => { await result.current.refresh(); });

            // then
            expect(getReviewerCandidatesAsync).toHaveBeenCalledTimes(1);
        });

    // REFRESH ANSWERS TO THE READS' OWN GATES, because refetch() does not. Each of these would
    // otherwise reach past a gate the caller declared and ask the server anyway.
    describe('when the round is switched off', () => {
        const expectNothingAsked = () => {
            expect(getApprovalVerdictAsync).not.toHaveBeenCalled();
            expect(getApprovalReviewsAsync).not.toHaveBeenCalled();
            expect(getReviewRequestsAsync).not.toHaveBeenCalled();
            expect(getReviewerDisplayNamesAsync).not.toHaveBeenCalled();
            expect(getApprovalCommentsAsync).not.toHaveBeenCalled();
        };

        it('should ask for nothing on refresh when the caller disabled it', async () => {
            // given
            const { result } = renderRound('quote-1', false);

            // when
            await act(async () => { await result.current.refresh(); });

            // then
            expectNothingAsked();
        });

        it('should ask for nothing on refresh before an entity id arrives', async () => {
            // given
            const { result } = renderRound('');

            // when
            await act(async () => { await result.current.refresh(); });

            // then
            expectNothingAsked();
        });
    });

    // THE ONE A MOCKED SERVICE CANNOT CATCH. refetch() ignores `enabled`, so an ungated call
    // asks the server for `approvalId eq ` — a malformed filter, refused, and refused silently
    // because these reads suppress their toasts by design.
    describe('when the entity has no approval round', () => {
        beforeEach(() => {
            getApprovalVerdictAsync.mockRejectedValue({
                isAxiosError: true,
                response: { status: 404 }
            });
        });

        it('should never ask for reviews or comments, on the first read or on any refresh',
            async () => {
            // given
            const { result } = renderRound();
            await waitFor(() => expect(result.current.isLoading).toBe(false));

            // when
            await act(async () => { await result.current.refresh(); });
            await act(async () => { await result.current.refresh(); });

            // then
            expect(result.current.approvalVerdict).toBeUndefined();
            expect(getApprovalReviewsAsync).not.toHaveBeenCalled();
            expect(getApprovalCommentsAsync).not.toHaveBeenCalled();
        });
    });
});
