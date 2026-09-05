import {
    keepPreviousData,
    QueryClient,
    useMutation,
    useQuery,
    useQueryClient
} from '@tanstack/react-query';
import ContentItemSettingBroker from "../../brokers/apiBroker.contentItemSettings";
import { ContentItemSetting } from "../../models/foundations/contentItemSettings/contentItemSetting";

import {
    ContentItemSettingPage,
    ContentItemSettingQuery
} from "../../models/foundations/contentItemSettings/contentItemSettingQuery";

// EVERY READ OF THESE ROWS, dropped together. A write here can change which row governs an item
// (a new override), what a type is called and how it is shaped (the defaults), and whether a type
// is open to general contribution — and the panels, the contribute page, the post page and the
// admin list all read from different cache keys. Invalidating one and forgetting another is the
// drift this exists to prevent, so a write invalidates the lot rather than reasoning per caller
// about which of them could possibly have changed.
const invalidateContentItemSettingReads = (queryClient: QueryClient): void => {
    queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetEffective"] });
    queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetAll"] });
    queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetById"] });
    queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetAvailableForContribution"] });
    queryClient.invalidateQueries({ queryKey: ["ContentItemSettingsGetDefaults"] });
};

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

    // THE EFFECTIVE SET for a surface showing these items: the per-type defaults PLUS the
    // item-level overrides of exactly these ids, in one collection for the §6.4 resolver to
    // pick from — most specific wins, per item. This is what a rendering surface hands its
    // panel; handing defaults alone silently un-overrides every overridden item, which is
    // precisely the drift the resolver exists to prevent.
    //
    // Keyed on the SORTED ids so the cache does not treat a reordering as a new question,
    // and kept while the next page's answer loads so cards do not flicker back to their
    // defaults mid-scroll.
    useGetEffectiveSettingsFor: (contentItemIds: ReadonlyArray<string>) => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        const sortedIds = [...contentItemIds].sort();

        return useQuery<ContentItemSetting[]>({
            queryKey: ["ContentItemSettingsGetEffective", sortedIds],
            placeholderData: keepPreviousData,

            queryFn: async () => {
                const [defaults, overrides] = await Promise.all([
                    contentItemSettingBroker.GetDefaultsAsync(),
                    contentItemSettingBroker.GetOverridesForContentItemsAsync(sortedIds)
                ]);

                return [...defaults, ...overrides];
            },

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

    // CREATE OR UPDATE THIS ITEM'S OVERRIDE — the one place the create/update decision is made,
    // so there is exactly one thing to replace when #209's ContentItemSettingsProcessingService
    // lands and moves the decision server-side.
    //
    // THE BRANCH IS THE ROW'S OWN ID. The panel empties it when the form was seeded from the type
    // default: there is no override yet, so this mints one and POSTs. A row that arrived with an
    // id is this item's existing override, so this PUTs it.
    //
    // THE KNOWN COST of deciding here rather than in a processing service: two administrators
    // creating the same override at once both see no row, both POST, and the second gets a 409
    // off UX_ContentItemSettings_OverridePerEntity. It surfaces as a refused save with the
    // server's own message rather than as silent data loss, and #209 removes the window.
    useCreateOrUpdateContentItemSettingOverride: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (contentItemSetting: ContentItemSetting) =>
                contentItemSetting.id.length === 0
                    ? await contentItemSettingBroker.AddContentItemSettingAsync({
                        ...contentItemSetting,
                        id: crypto.randomUUID()
                    })
                    : await contentItemSettingBroker.UpdateContentItemSettingAsync(
                        contentItemSetting),

            onSuccess: () => invalidateContentItemSettingReads(queryClient)
        });
    },

    // Withdraws an item-level override permanently, leaving the item governed by its content
    // type default again. The server refuses a per-type default here (§12.5.2 business rule 5),
    // so a caller that has misread which row it holds is stopped rather than obeyed.
    useHardRemoveContentItemSetting: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (contentItemSettingId: string) =>
                await contentItemSettingBroker.HardDeleteContentItemSettingByIdAsync(
                    contentItemSettingId),

            onSuccess: () => invalidateContentItemSettingReads(queryClient)
        });
    },

    useUpdateContentItemSetting: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();
        const queryClient = useQueryClient();

        return useMutation({
            mutationFn: async (contentItemSetting: ContentItemSetting) =>
                await contentItemSettingBroker.UpdateContentItemSettingAsync(contentItemSetting),

            onSuccess: () => invalidateContentItemSettingReads(queryClient)
        });
    }
};
