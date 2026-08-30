import { useMutation, useQuery } from '@tanstack/react-query';
import ContentItemBroker from '../../brokers/apiBroker.contentItems';

import {
    ContentItem,
    ContentItemAddRequest
} from '../../models/foundations/contentItems/contentItem';

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
