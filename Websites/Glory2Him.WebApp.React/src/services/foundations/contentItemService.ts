import {
    useInfiniteQuery,
    useMutation,
    useQuery,
    useQueryClient
} from '@tanstack/react-query';
import ContentItemBroker from '../../brokers/apiBroker.contentItems';

import {
    ContentItem,
    ContentItemAddRequest
} from '../../models/foundations/contentItems/contentItem';

import {
    ContentItemPage
} from '../../models/foundations/contentItems/contentItemSearchQuery';

import {
    ApprovalStatus,
    ContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

// Comfortably under the host's OData:PageSize cap of 50, which the +1 probe row also rides
// inside. Small enough that a first page arrives quickly and the scroll has somewhere to go.
export const contentItemSearchPageSize = 8;

export const contentItemService = {
    useGetContentItemById: (contentItemId: string, enabled = true) => {
        const contentItemBroker = new ContentItemBroker();

        return useQuery<ContentItem>({
            queryKey: ["ContentItemsGetById", contentItemId],
            queryFn: async () =>
                await contentItemBroker.GetContentItemByIdAsync(contentItemId),
            enabled,
            staleTime: 60 * 1000
        });
    },

    // The infinite scroll behind ContentItemListPanel. useInfiniteQuery rather than useQuery
    // because the panel renders the ACCUMULATED list: react-query keeps the pages, so a page that
    // has already been fetched is never fetched again on the way down.
    //
    // The CRITERIA ARE THE KEY, so changing the search starts a fresh scroll from page zero
    // rather than appending to the last one — a list that grew by adding new results to old ones
    // would be a list nobody could read.
    // The options are what make three surfaces of one hook: the page names its read (`scope`),
    // and pins the narrowing its surface is FOR — "my posts" pins submittedById to the signed-in
    // account, the moderation queue pins the Draft + Submitted statuses. The criteria are the
    // reader's half; the options are the page's, which is why they are separate parameters.
    useSearchContentItems: (
        criteria: ContentItemSearchCriteria,
        options: {
            scope?: 'public' | 'caller';
            submittedById?: string | null;
            approvalStatuses?: ReadonlyArray<ApprovalStatus> | null;
            pageSize?: number;
            enabled?: boolean;
        } = {}) => {
        const contentItemBroker = new ContentItemBroker();

        const scope = options.scope ?? 'caller';
        const pageSize = options.pageSize ?? contentItemSearchPageSize;

        // The reader's clicked submitted-by filter narrows within whatever the page pinned; a
        // page that pinned its own (my posts) wins, because that pin is what the surface IS.
        const submittedById =
            options.submittedById ?? criteria.submittedBy?.id ?? null;

        // The reader's ticked statuses narrow WITHIN whatever the page pinned, the same way:
        // the moderation queue is Draft + Submitted whatever is ticked, so ticking Approved
        // there asks for nothing the queue was not already showing. Nothing ticked leaves the
        // pin alone — an empty selection is "any status", not "no status".
        //
        // AN EMPTY INTERSECTION FALLS BACK TO THE PIN rather than travelling as an empty list:
        // the broker reads an empty list as "no status clause at all", so sending one would
        // turn a selection that asked for LESS into a read that shows MORE. A tick with
        // nothing behind it on this surface is a tick that changes nothing.
        const pinnedStatuses = options.approvalStatuses ?? null;
        const searchedStatuses = criteria.approvalStatuses;

        const narrowedStatuses = pinnedStatuses == null
            ? searchedStatuses
            : searchedStatuses.filter(
                (approvalStatus) => pinnedStatuses.includes(approvalStatus));

        const approvalStatuses =
            searchedStatuses.length === 0 || narrowedStatuses.length === 0
                ? pinnedStatuses
                : narrowedStatuses;

        return useInfiniteQuery<ContentItemPage>({
            queryKey: [
                'ContentItemsSearch',
                scope,
                criteria.query,
                criteria.contentType,
                criteria.author,
                criteria.shareabilityBasis,
                submittedById,
                approvalStatuses,
                pageSize
            ],

            enabled: options.enabled ?? true,
            initialPageParam: 0,

            queryFn: async ({ pageParam }) =>
                await contentItemBroker.SearchContentItemsAsync({
                    scope,
                    searchTerm: criteria.query,
                    contentType: criteria.contentType,
                    author: criteria.author,
                    shareabilityBasis: criteria.shareabilityBasis,
                    submittedById,
                    approvalStatuses,
                    pageIndex: pageParam as number,
                    pageSize
                }),

            // undefined is how react-query is told there is no next page, and it is what turns
            // the panel's sentinel off — the probe row the broker dropped is the whole signal.
            getNextPageParam: (lastPage) =>
                lastPage.hasNextPage ? lastPage.pageIndex + 1 : undefined,

            staleTime: 60 * 1000
        });
    },

    // meta.suppressGlobalErrorToast: the caller shows the API's own message — the field-level
    // validation readback and a toast naming what is actually wrong — so the generic
    // "An unknown error has occurred" from the global mutation cache would be a second, less
    // useful toast on top of it.
    // The modify write behind every editing surface. suppressGlobalErrorToast for the same
    // reason the add has it: the API is the authority on what an item must carry, so a 400 is
    // marked up on the form the caller is looking at rather than thrown at them as a toast.
    //
    // WHAT IT INVALIDATES IS THE POINT. A surface that closes its editor and re-renders from a
    // cache nobody told about the write shows the reader their own change reverting in front of
    // them — the row is saved and the page says otherwise, which is worse than not saving.
    useModifyContentItem: () => {
        const contentItemBroker = new ContentItemBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (contentItem: ContentItem) =>
                await contentItemBroker.PutContentItemAsync(contentItem),

            onSuccess: (_, contentItem) => {
                queryClient.invalidateQueries({
                    queryKey: ['ContentItemsGetById', contentItem.id]
                });

                // Every feed and queue reading this row. The key carries the search criteria, so
                // the prefix is matched rather than any one of them reconstructed — a moderation
                // queue filtered one way and a journal filtered another both hold this item.
                queryClient.invalidateQueries({ queryKey: ['ContentItemsSearch'] });

                // THE ROUND MOVES WITH THE ROW. A modify carries the Draft <-> Submitted
                // carve-out (§9.2 rules 3-6), so the verdict this item was blocked by — and the
                // reasons it gave — are answered afresh the moment the status changes. Left
                // alone they would go on reporting a draft that has just been offered.
                queryClient.invalidateQueries({ queryKey: ['ApprovalVerdict'] });
                queryClient.invalidateQueries({ queryKey: ['ApprovalReviews'] });
                queryClient.invalidateQueries({ queryKey: ['ReviewerCandidates'] });
                queryClient.invalidateQueries({ queryKey: ['ReviewRequests'] });
            }
        });
    },

    // The takedown behind the editor's Delete. It invalidates exactly what the modify does:
    // a removed row leaves every feed and queue that was holding it, and the round it was
    // being judged by is answering about something no longer there.
    useRemoveContentItem: () => {
        const contentItemBroker = new ContentItemBroker();
        const queryClient = useQueryClient();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (
                request: { contentItemId: string; deletionReason?: string }) =>
                await contentItemBroker.DeleteContentItemByIdAsync(
                    request.contentItemId, request.deletionReason),

            onSuccess: (_, request) => {
                queryClient.invalidateQueries({
                    queryKey: ['ContentItemsGetById', request.contentItemId]
                });

                queryClient.invalidateQueries({ queryKey: ['ContentItemsSearch'] });
                queryClient.invalidateQueries({ queryKey: ['ApprovalVerdict'] });
                queryClient.invalidateQueries({ queryKey: ['ApprovalReviews'] });
                queryClient.invalidateQueries({ queryKey: ['ReviewerCandidates'] });
                queryClient.invalidateQueries({ queryKey: ['ReviewRequests'] });
            }
        });
    },

    useAddContentItem: () => {
        const contentItemBroker = new ContentItemBroker();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (contentItem: ContentItemAddRequest) =>
                await contentItemBroker.PostContentItemAsync(contentItem)
        });
    }
};
