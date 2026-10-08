import { useMutation, useQueries, useQueryClient } from '@tanstack/react-query';
import AssociationBroker from '../../brokers/apiBroker.associations';
import { AssociationRequest } from '../../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../../models/foundations/associations/associationSuggestionResult';
import { ContentItemReactionSummary } from '../../models/foundations/associations/contentItemReactionSummary';

export interface ReactionSummariesRead {
    summaries: Readonly<Record<string, ContentItemReactionSummary>>;
    isLoading: boolean;
    isError: boolean;
}

// The route answers at most 25 ids in one ask, so a page holding more is asked in chunks of 25
// (§ARC16.8, The set, its bounds). A page never reaches 25 at today's page size; a caller may
// set a larger one.
const reactionSummaryChunkSize = 25;

const chunkContentItemIds = (
    contentItemIds: ReadonlyArray<string>): ReadonlyArray<ReadonlyArray<string>> => {
    const chunks: ReadonlyArray<string>[] = [];

    for (let index = 0; index < contentItemIds.length; index += reactionSummaryChunkSize) {
        chunks.push(contentItemIds.slice(index, index + reactionSummaryChunkSize));
    }

    return chunks;
};

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

    useGetReactionSummaries: (
        contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>,
        readerId: string | null | undefined): ReactionSummariesRead => {
        const associationBroker = new AssociationBroker();

        useQueries({
            queries: contentItemIdPages.map(contentItemIds => ({
                queryKey: ['ReactionSummaries', readerId, contentItemIds],
                queryFn: async () => {
                    const chunks = chunkContentItemIds(contentItemIds);

                    const answers = await Promise.all(chunks.map(chunk =>
                        associationBroker.GetReactionSummariesAsync(chunk)));

                    return answers.flat();
                }
            }))
        });

        return { summaries: {}, isLoading: false, isError: false };
    }
};
