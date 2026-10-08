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
import { ContentItemReactionSummary } from '../../models/foundations/associations/contentItemReactionSummary';

import {
    AssociationSuggestionResult,
    AssociationSuggestionStatus
} from '../../models/foundations/associations/associationSuggestionResult';

const postAssociationAsync = vi.fn();
const getReactionSummariesAsync = vi.fn();

vi.mock('../../brokers/apiBroker.associations', () => ({
    default: class {
        PostAssociationAsync = postAssociationAsync;
        GetReactionSummariesAsync = getReactionSummariesAsync;
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

describe('associationService.useGetReactionSummaries', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const summaryFor = (contentItemId: string): ContentItemReactionSummary => ({
        contentItemId,
        reactions: [{ reactionId: 'reaction-1', name: 'Like', unicodeEmoji: '👍', count: 1 }],
        viewerReactionId: null,
        viewerReactionName: null
    });

    // The broker answers every id it is asked for, in the order asked, as the route does.
    const answerEveryIdAsked = () =>
        getReactionSummariesAsync.mockImplementation(
            async (contentItemIds: ReadonlyArray<string>) => contentItemIds.map(summaryFor));

    const summaryKey = (readerId: string | null, page: ReadonlyArray<string>) =>
        ['ReactionSummaries', readerId, page];

    const idsFrom = (prefix: string, count: number) =>
        Array.from({ length: count }, (_, index) => `${prefix}-${index + 1}`);

    beforeEach(() => {
        vi.clearAllMocks();
        getReactionSummariesAsync.mockReset();
        answerEveryIdAsked();

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it("should ask once per delivered page, for that page's ids", async () => {
        // given
        const firstPage = ['quote-1', 'quote-2'];
        const secondPage = ['quote-3', 'quote-4'];

        // when
        renderHook(
            () => associationService.useGetReactionSummaries([firstPage, secondPage], 'reader-1'),
            { wrapper });

        // then
        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', firstPage))).toBeDefined());

        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', secondPage))).toBeDefined());

        expect(getReactionSummariesAsync).toHaveBeenCalledTimes(2);
        expect(getReactionSummariesAsync).toHaveBeenNthCalledWith(1, firstPage);
        expect(getReactionSummariesAsync).toHaveBeenNthCalledWith(2, secondPage);

        expect(queryClient.getQueryData(summaryKey('reader-1', firstPage)))
            .toEqual(firstPage.map(summaryFor));

        expect(queryClient.getQueryData(summaryKey('reader-1', secondPage)))
            .toEqual(secondPage.map(summaryFor));

        expect(queryClient.getQueryCache().getAll().map(query => query.queryKey))
            .toEqual([summaryKey('reader-1', firstPage), summaryKey('reader-1', secondPage)]);
    });

    // A list page hands the read its delivered pages afresh on every render, so the pages it
    // already read arrive as new arrays holding the same ids.
    it('should ask only for the page just delivered', async () => {
        // given
        const firstPage = ['quote-1', 'quote-2'];
        const secondPage = ['quote-3', 'quote-4'];
        const thirdPage = ['quote-5', 'quote-6'];

        const { rerender } = renderHook(
            ({ pages }) => associationService.useGetReactionSummaries(pages, 'reader-1'),
            { wrapper, initialProps: { pages: [firstPage, secondPage] } });

        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', secondPage))).toBeDefined());

        // when
        rerender({ pages: [[...firstPage], [...secondPage], thirdPage] });

        // then
        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', thirdPage))).toBeDefined());

        expect(getReactionSummariesAsync).toHaveBeenCalledTimes(3);
        expect(getReactionSummariesAsync).toHaveBeenNthCalledWith(3, thirdPage);

        expect(queryClient.getQueryCache().getAll().map(query => query.queryKey)).toEqual([
            summaryKey('reader-1', firstPage),
            summaryKey('reader-1', secondPage),
            summaryKey('reader-1', thirdPage)
        ]);
    });

    it('should chunk a page of more than 25 ids at 25', async () => {
        // given
        const page = idsFrom('quote', 30);

        // when
        renderHook(
            () => associationService.useGetReactionSummaries([page], 'reader-1'),
            { wrapper });

        // then
        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', page))).toBeDefined());

        expect(getReactionSummariesAsync).toHaveBeenCalledTimes(2);
        expect(getReactionSummariesAsync).toHaveBeenNthCalledWith(1, page.slice(0, 25));
        expect(getReactionSummariesAsync).toHaveBeenNthCalledWith(2, page.slice(25));

        expect(queryClient.getQueryData(summaryKey('reader-1', page)))
            .toEqual(page.map(summaryFor));
    });

    // A page that answers is read beside the empty one, so the test waits on a read that
    // happens rather than on time passing.
    it('should ask nothing for an empty page', async () => {
        // given
        const emptyPage: string[] = [];
        const answeredPage = ['quote-1'];

        // when
        renderHook(
            () => associationService.useGetReactionSummaries([emptyPage, answeredPage], 'reader-1'),
            { wrapper });

        // then
        await waitFor(() => expect(
            queryClient.getQueryData(summaryKey('reader-1', answeredPage))).toBeDefined());

        expect(getReactionSummariesAsync).toHaveBeenCalledTimes(1);
        expect(getReactionSummariesAsync).toHaveBeenCalledWith(answeredPage);
    });

    it('should answer one summary per content item id across every page', async () => {
        // given
        const firstPage = ['quote-1', 'quote-2'];
        const secondPage = ['quote-3', 'quote-4'];

        const expectedSummaries = {
            'quote-1': summaryFor('quote-1'),
            'quote-2': summaryFor('quote-2'),
            'quote-3': summaryFor('quote-3'),
            'quote-4': summaryFor('quote-4')
        };

        // when
        const { result } = renderHook(
            () => associationService.useGetReactionSummaries([firstPage, secondPage], 'reader-1'),
            { wrapper });

        // then
        await waitFor(() => expect(result.current.summaries).toEqual(expectedSummaries));
    });

    it('should keep the summaries of the pages that answered when another fails', async () => {
        // given
        const answeredPage = ['quote-1', 'quote-2'];
        const failedPage = ['quote-3', 'quote-4'];

        getReactionSummariesAsync.mockImplementation(
            async (contentItemIds: ReadonlyArray<string>) => {
                if (contentItemIds.includes('quote-3')) {
                    throw new Error('refused');
                }

                return contentItemIds.map(summaryFor);
            });

        // when
        const { result } = renderHook(
            () => associationService.useGetReactionSummaries([answeredPage, failedPage], 'reader-1'),
            { wrapper });

        // then
        await waitFor(() => expect(result.current.isError).toBe(true));

        await waitFor(() => expect(result.current.summaries).toEqual({
            'quote-1': summaryFor('quote-1'),
            'quote-2': summaryFor('quote-2')
        }));
    });
});
