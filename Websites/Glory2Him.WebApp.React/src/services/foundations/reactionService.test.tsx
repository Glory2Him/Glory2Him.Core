import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { reactionService } from './reactionService';
import { ApprovalStatus } from '../../models/components/associations/associationItem';
import { Reaction } from '../../models/foundations/reactions/reaction';

const getReactionsAsync = vi.fn();

const createReaction = (id: string, name: string, unicodeEmoji: string): Reaction => ({
    id,
    name,
    unicodeEmoji,
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
});

vi.mock('../../brokers/apiBroker.reactions', () => ({
    default: class {
        GetReactionsAsync = getReactionsAsync;
    }
}));

describe('reactionService.useGetApprovedReactions', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        getReactionsAsync.mockResolvedValue([] as Reaction[]);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should ask for the approved and published vocabulary only', async () => {
        // given
        const expectedFilter = "approvalStatus eq 'Approved' and isPublished eq true and isDeleted eq false";

        // when
        renderHook(() => reactionService.useGetApprovedReactions(), { wrapper });

        // then
        await waitFor(() => expect(getReactionsAsync).toHaveBeenCalledTimes(1));

        expect(getReactionsAsync).toHaveBeenCalledWith(
            expect.objectContaining({ filter: expectedFilter }));
    });

    it('should ask for the vocabulary in its sort order', async () => {
        // given
        const expectedOrderBy = 'sortOrder,name';

        // when
        renderHook(() => reactionService.useGetApprovedReactions(), { wrapper });

        // then
        await waitFor(() => expect(getReactionsAsync).toHaveBeenCalledTimes(1));

        expect(getReactionsAsync).toHaveBeenCalledWith(
            expect.objectContaining({ orderBy: expectedOrderBy }));
    });

    it('should answer with the vocabulary in the order the server sent it', async () => {
        // given
        const reactionsInServerOrder: Reaction[] = [
            createReaction('reaction-3', 'Moved', 'moved-emoji'),
            createReaction('reaction-1', 'Amen', 'amen-emoji'),
            createReaction('reaction-2', 'Love', 'love-emoji')
        ];

        const expectedReactions: Reaction[] =
            reactionsInServerOrder.map(reaction => ({ ...reaction }));

        getReactionsAsync.mockResolvedValue(reactionsInServerOrder);

        // when
        const { result } = renderHook(
            () => reactionService.useGetApprovedReactions(), { wrapper });

        // then
        await waitFor(() => expect(result.current.isSuccess).toBe(true));
        expect(result.current.data).toEqual(expectedReactions);
    });

    it("should report the vocabulary read's failure unchanged", async () => {
        // given
        const readFailureMessage = 'The vocabulary read failed.';
        const readFailure = new Error(readFailureMessage);
        const expectedFailure = new Error(readFailureMessage);
        getReactionsAsync.mockRejectedValue(readFailure);

        // when
        const { result } = renderHook(
            () => reactionService.useGetApprovedReactions(), { wrapper });

        // then
        await waitFor(() => expect(result.current.isError).toBe(true));
        expect(result.current.error).toBe(readFailure);
        expect(result.current.error).toStrictEqual(expectedFailure);
    });
});
