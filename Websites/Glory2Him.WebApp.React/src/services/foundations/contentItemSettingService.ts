import { useQuery } from '@tanstack/react-query';
import ContentItemSettingBroker from "../../brokers/apiBroker.contentItemSettings";
import { ContentItemSetting } from "../../models/foundations/contentItemSettings/contentItemSetting";

export const contentItemSettingService = {
    useGetAvailableForContribution: () => {
        const contentItemSettingBroker = new ContentItemSettingBroker();

        return useQuery<ContentItemSetting[]>({
            queryKey: ["ContentItemSettingsGetAvailableForContribution"],
            queryFn: async () => await contentItemSettingBroker.GetAvailableForContributionAsync(),
            staleTime: 60 * 1000
        });
    }
};
