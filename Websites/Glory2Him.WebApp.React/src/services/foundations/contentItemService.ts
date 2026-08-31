import { useInfiniteQuery, useMutation, useQuery } from '@tanstack/react-query';
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

    // The infinite scroll behind ContentItemSearchPanel. useInfiniteQuery rather than useQuery
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

        return useInfiniteQuery<ContentItemPage>({
            queryKey: [
                'ContentItemsSearch',
                scope,
                criteria.query,
                criteria.contentType,
                criteria.author,
                submittedById,
                options.approvalStatuses ?? null,
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
                    submittedById,
                    approvalStatuses: options.approvalStatuses ?? null,
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
    useAddContentItem: () => {
        const contentItemBroker = new ContentItemBroker();

        return useMutation({
            meta: { suppressGlobalErrorToast: true },

            mutationFn: async (contentItem: ContentItemAddRequest) =>
                await contentItemBroker.PostContentItemAsync(contentItem)
        });
    }
};
