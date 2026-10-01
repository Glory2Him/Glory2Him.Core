import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { associationService } from './associationService';
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
});
