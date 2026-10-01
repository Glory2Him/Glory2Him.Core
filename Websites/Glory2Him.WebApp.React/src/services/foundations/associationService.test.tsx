import { ReactNode } from 'react';
import {
    MutationCache,
    QueryClient,
    QueryClientProvider,
    useQuery
} from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { associationService } from './associationService';
import { queryClientGlobalOptions } from '../../brokers/apiBroker.globals';
import { EntityType } from '../../models/foundations/approvalSettings/approvalSetting';
import { AssociationRequest } from '../../models/foundations/associations/associationRequest';

import {
    AssociationSuggestionResult,
    AssociationSuggestionStatus
} from '../../models/foundations/associations/associationSuggestionResult';

const postAssociationAsync = vi.fn();

vi.mock('../../brokers/apiBroker.associations', () => ({
    default: class {
        PostAssociationAsync = postAssociationAsync;
    }
}));

vi.mock('../../brokers/toastBroker.error', () => ({
    toastError: vi.fn()
}));

const { toastError } = await import('../../brokers/toastBroker.error');
const toastErrorMock = vi.mocked(toastError);

const reactionRequest: AssociationRequest = {
    entityAType: EntityType.ContentItem,
    entityAKeyId: 'quote-1',
    entityBType: EntityType.Reaction,
    entityBKeyId: 'reaction-1'
};

const createdResult: AssociationSuggestionResult = {
    status: AssociationSuggestionStatus.Created,
    associationId: 'association-1'
};

describe('associationService.useUpsertAssociation', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        postAssociationAsync.mockResolvedValue(createdResult);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should send the reaction through the broker and resolve with its result', async () => {
        // given
        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper });

        // when
        const actualResult = await result.current.mutateAsync(reactionRequest);

        // then
        expect(postAssociationAsync).toHaveBeenCalledTimes(1);
        expect(postAssociationAsync).toHaveBeenCalledWith(reactionRequest);
        expect(actualResult).toEqual(createdResult);
    });

    // THE SUMMARY READS A WRITE MUST REACH, seeded for real rather than spied on: one a card is
    // serving (active, so it is read again at once) and one cached from another screen
    // (inactive, so it is marked stale and NOT fetched). Both are keyed on a page of ids under
    // the ReactionSummaries prefix, as every summary read is, and the write is held open so what
    // happens before it settles can be told apart from what happens after.
    //
    // A cached read outside the family stands beside them: the write invalidates what it
    // changes and nothing else, so a filter that widens past the prefix reds the test.
    const seedSummaryReads = async () => {
        const activeSummaryRead = vi.fn().mockResolvedValue([]);
        const cachedSummaryRead = vi.fn().mockResolvedValue([]);
        const unrelatedRead = vi.fn().mockResolvedValue([]);

        renderHook(
            () => useQuery({
                queryKey: ['ReactionSummaries', ['quote-1']],
                queryFn: activeSummaryRead
            }),
            { wrapper });

        await queryClient.prefetchQuery({
            queryKey: ['ReactionSummaries', ['quote-2']],
            queryFn: cachedSummaryRead
        });

        await queryClient.prefetchQuery({
            queryKey: ['ContentItemsSearch'],
            queryFn: unrelatedRead
        });

        await waitFor(() => expect(activeSummaryRead).toHaveBeenCalledTimes(1));

        return { activeSummaryRead, cachedSummaryRead, unrelatedRead };
    };

    const isStale = (queryKey: ReadonlyArray<unknown>) =>
        queryClient.getQueryState(queryKey)?.isInvalidated;

    const holdTheWriteOpen = () => {
        let settle: { resolve: (value: unknown) => void; reject: (reason: unknown) => void } =
            { resolve: () => undefined, reject: () => undefined };

        postAssociationAsync.mockReturnValue(
            new Promise((resolve, reject) => { settle = { resolve, reject }; }));

        return () => settle;
    };

    const expectTheSummariesReadAgainOnSettle = async (
        settleTheWrite: () => Promise<unknown>,
        reads: Awaited<ReturnType<typeof seedSummaryReads>>) => {

        // then, before the write settles
        await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(1));
        expect(reads.activeSummaryRead).toHaveBeenCalledTimes(1);
        expect(isStale(['ReactionSummaries', ['quote-2']])).toBe(false);

        // when
        await settleTheWrite();

        // then
        await waitFor(() => expect(reads.activeSummaryRead).toHaveBeenCalledTimes(2));
        expect(isStale(['ReactionSummaries', ['quote-2']])).toBe(true);
        expect(reads.cachedSummaryRead).toHaveBeenCalledTimes(1);
        expect(isStale(['ContentItemsSearch'])).toBe(false);
        expect(reads.unrelatedRead).toHaveBeenCalledTimes(1);
    };

    // Matched by PREFIX: a summary read is keyed on the page of ids it asked for, and every
    // page holding the item is stale once its reaction is written.
    it('should read the summaries again once a reaction is written', async () => {
        // given
        const reads = await seedSummaryReads();
        const settle = holdTheWriteOpen();

        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper });

        // when
        const write = result.current.mutateAsync(reactionRequest);

        // then
        await expectTheSummariesReadAgainOnSettle(
            async () => {
                settle().resolve(createdResult);
                await write;
            },
            reads);
    });

    // A FAILED WRITE MAY STILL HAVE LANDED: the server can write the row and the answer be lost
    // on the way back, so the summaries are read again either way and the card shows what the
    // server holds rather than what the page guessed.
    it('should read the summaries again when a reaction write fails', async () => {
        // given
        const reads = await seedSummaryReads();
        const settle = holdTheWriteOpen();

        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper });

        // when
        const write = result.current.mutateAsync(reactionRequest);

        // then
        await expectTheSummariesReadAgainOnSettle(
            async () => {
                settle().reject(new Error('refused'));
                await expect(write).rejects.toThrow('refused');
            },
            reads);
    });

    // A FAILED REACTION IS ANNOUNCED as every failed write is. Driven through the app's own
    // global handler, so what is proven is the toast the reader sees, not the absence of a flag.
    // The handler rethrows by design, which react-query surfaces as an unhandled rejection; it
    // is swallowed here and nowhere else, so the decision to toast stays the app's.
    it("should fail with the broker's error and leave the global toast on", async () => {
        // given
        const brokerError = new Error('refused');
        postAssociationAsync.mockRejectedValue(brokerError);

        const globalOnError =
            queryClientGlobalOptions.getMutationCache().config.onError!;

        const globalClient = new QueryClient({
            mutationCache: new MutationCache({
                onError: (...args) => {
                    try {
                        globalOnError(...args);
                    } catch {
                        // the global handler's deliberate rethrow
                    }
                }
            })
        });

        const globalWrapper = ({ children }: { children: ReactNode }) => (
            <QueryClientProvider client={globalClient}>{children}</QueryClientProvider>
        );

        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper: globalWrapper });

        // when
        await expect(result.current.mutateAsync(reactionRequest)).rejects.toBe(brokerError);

        // then
        expect(toastErrorMock).toHaveBeenCalledTimes(1);
    });
});
