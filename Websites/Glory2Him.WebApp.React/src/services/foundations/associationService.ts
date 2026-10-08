import { useMutation, useQueryClient } from '@tanstack/react-query';
import AssociationBroker from '../../brokers/apiBroker.associations';
import { AssociationRequest } from '../../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../../models/foundations/associations/associationSuggestionResult';
import { ContentItemReactionSummary } from '../../models/foundations/associations/contentItemReactionSummary';

export interface ReactionSummariesRead {
    summaries: Readonly<Record<string, ContentItemReactionSummary>>;
    isLoading: boolean;
    isError: boolean;
}

export const associationService = {
    // Gives or changes a reader's reaction. No suppressGlobalErrorToast: a failed reaction is
    // announced as every failed write is.
    //
    // The summaries are read again ON SETTLE, not on success: a write that failed may still have
    // landed, so the card is answered by the server either way. Matched by the ReactionSummaries
    // PREFIX, since each summary read is keyed on the page of ids it asked for.
    useUpsertAssociation: () => {
        const associationBroker = new AssociationBroker();
        const queryClient = useQueryClient();

        return useMutation<AssociationSuggestionResult, unknown, AssociationRequest>({
            mutationFn: async (association: AssociationRequest) =>
                await associationBroker.PostAssociationAsync(association),

            onSettled: () => {
                queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
            }
        });
    },

    useGetReactionSummaries: ((): ReactionSummariesRead =>
        ({ summaries: {}, isLoading: false, isError: false })) as (
            contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>,
            readerId: string | null | undefined) => ReactionSummariesRead
};
