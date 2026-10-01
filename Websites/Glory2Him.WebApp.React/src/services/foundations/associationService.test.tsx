import { ReactNode } from 'react';
import { MutationCache, QueryClient, QueryClientProvider } from '@tanstack/react-query';
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
    let invalidated: Array<ReadonlyArray<unknown>>;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        postAssociationAsync.mockResolvedValue(createdResult);
        invalidated = [];

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });

        vi.spyOn(queryClient, 'invalidateQueries').mockImplementation((filters) => {
            invalidated.push((filters?.queryKey ?? []) as ReadonlyArray<unknown>);

            return Promise.resolve();
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

    // Matched by PREFIX: a summary read is keyed on the page of ids it asked for, and every
    // page holding the item is stale once its reaction is written.
    it('should read the summaries again once a reaction is written', async () => {
        // given
        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper });

        // when
        await result.current.mutateAsync(reactionRequest);
        await waitFor(() => expect(invalidated.length).toBeGreaterThan(0));

        // then
        expect(invalidated).toEqual([['ReactionSummaries']]);
    });

    // A FAILED WRITE MAY STILL HAVE LANDED: the server can write the row and the answer be lost
    // on the way back, so the summaries are read again either way and the card shows what the
    // server holds rather than what the page guessed.
    it('should read the summaries again when a reaction write fails', async () => {
        // given
        postAssociationAsync.mockRejectedValue(new Error('refused'));

        const { result } = renderHook(
            () => associationService.useUpsertAssociation(), { wrapper });

        // when
        await expect(result.current.mutateAsync(reactionRequest)).rejects.toThrow('refused');
        await waitFor(() => expect(invalidated.length).toBeGreaterThan(0));

        // then
        expect(invalidated).toEqual([['ReactionSummaries']]);
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
