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

    // The reaction summaries of the cards a list has delivered: one query per delivered page,
    // never one over the accumulated list, so a newly loaded page asks for its own ids alone
    // (§ARC16.8). Keyed on the reader too, because a summary carries the reader's own reaction;
    // the id only separates the cached answers and is never sent.
    useGetReactionSummaries: (
        contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>,
        readerId: string | null | undefined): ReactionSummariesRead => {
        const associationBroker = new AssociationBroker();

        // While the reader is unknown the hook holds no query at all, rather than a disabled one:
        // React Query hashes an undefined key element as null, so a disabled query would still
        // answer from the signed-out reader's cache.
        const pagesToRead = readerId === undefined ? [] : contentItemIdPages;

        const pageReads = useQueries({
            queries: pagesToRead.map(contentItemIds => ({
                queryKey: ['ReactionSummaries', readerId, contentItemIds],
                queryFn: async () => {
                    const chunks = chunkContentItemIds(contentItemIds);

                    const answers = await Promise.all(chunks.map(chunk =>
                        associationBroker.GetReactionSummariesAsync(chunk)));

                    return answers.flat();
                },

                // An empty page asks nothing, so it never reads and is never loading.
                enabled: contentItemIds.length > 0
            }))
        });

        // A page whose latest read failed answers nothing, even where an earlier read of it
        // answered: React Query keeps that earlier answer, and the page must not show it.
        const summaries = Object.fromEntries(pageReads
            .filter(pageRead => pageRead.isError === false)
            .flatMap(pageRead => pageRead.data ?? [])
            .map(summary => [summary.contentItemId, summary]));

        const isLoading = pageReads.some(pageRead => pageRead.isLoading);
        const isError = pageReads.some(pageRead => pageRead.isError);

        return { summaries, isLoading, isError };
    },

    useReadReactionSummariesAgain: (() => async () => undefined) as () => () => Promise<void>
};
