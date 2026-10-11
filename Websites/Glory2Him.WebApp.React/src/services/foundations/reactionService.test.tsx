import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { reactionService } from './reactionService';
import { Reaction } from '../../models/foundations/reactions/reaction';

const getReactionsAsync = vi.fn();

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
});
