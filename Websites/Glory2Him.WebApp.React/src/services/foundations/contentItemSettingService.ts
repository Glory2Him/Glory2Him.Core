import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ContentItemSettingBroker from "../../brokers/apiBroker.contentItemSettings";
import { ContentItemSetting } from "../../models/foundations/contentItemSettings/contentItemSetting";

import {
    ContentItemSettingPage,
    ContentItemSettingQuery
} from "../../models/foundations/contentItemSettings/contentItemSettingQuery";

export const contentItemSettingService = {
    useGetAvailableForContribution: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        return useQuery<ContentItemSetting[]>({
            queryKey: ["ContentItemSettingsGetAvailableForContribution"],
            queryFn: async () => await contentItemSettingBroker.GetAvailableForContributionAsync(),
            staleTime: 60 * 1000
        });
    },

    useGetDefaults: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        return useQuery<ContentItemSetting[]>({
            queryKey: ["ContentItemSettingsGetDefaults"],
            queryFn: async () => await contentItemSettingBroker.GetDefaultsAsync(),
            staleTime: 60 * 1000
        });
    },

    // The query is part of the key, so changing a filter or turning a page is a separate cache
    // entry rather than a refetch of the same one. The previous page stays on screen while the
    // next one loads, which keeps the table from collapsing to a spinner on every keystroke.
    useGetContentItemSettings: (query: ContentItemSettingQuery) => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        return useQuery<ContentItemSettingPage>({
            queryKey: ["ContentItemSettingsGetAll", query],
            queryFn: async () => await contentItemSettingBroker.GetContentItemSettingsAsync(query),
            placeholderData: keepPreviousData,
            staleTime: 60 * 1000
        });
    },

    useGetContentItemSettingById: (contentItemSettingId: string, enabled = true) => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        return useQuery<ContentItemSetting>({
            queryKey: ["ContentItemSettingsGetById", contentItemSettingId],
            queryFn: async () =>
                await contentItemSettingBroker.GetContentItemSettingByIdAsync(contentItemSettingId),
            enabled,
            staleTime: 60 * 1000
        });
    },

    useUpdateContentItemSetting: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (contentItemSetting: ContentItemSetting) =>
                await contentItemSettingBroker.UpdateContentItemSettingAsync(contentItemSetting),

            onSuccess: (_, contentItemSetting) => {
                queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetAll"] });

                queryClient.invalidateQueries({
                    queryKey: ["ContentItemSettingsGetById", contentItemSetting.id]
                });

                // The contribute page reads the same rows to decide which types are open to
                // general users, and IsAvailableAsGeneralUserContribution is editable here.
                queryClient.invalidateQueries({
                    queryKey: ["ContentItemSettingsGetAvailableForContribution"]
                });

                // The post page reads the same rows for a type's name, icon and field shaping,
                // every one of which is editable here.
                queryClient.invalidateQueries({
                    queryKey: ["ContentItemSettingsGetDefaults"]
                });
            }
        });
    }
};
