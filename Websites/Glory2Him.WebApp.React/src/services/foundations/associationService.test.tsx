import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook } from '@testing-library/react';
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
});
