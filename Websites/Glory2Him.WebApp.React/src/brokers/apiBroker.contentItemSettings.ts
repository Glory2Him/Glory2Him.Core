import { ContentItemSetting } from "../models/foundations/contentItemSettings/contentItemSetting";
import ApiBroker from "./apiBroker";

class ContentItemSettingBroker {
    relativeContentItemSettingsUrl = '/api/contentitemsettings';
    private apiBroker: ApiBroker = new ApiBroker();

    // The type-selector on the contribute page needs the per-content-type DEFAULT rows
    // (contentItemId eq null) that are open to general users. The controller has
    // [EnableQuery], so the filter runs server-side rather than pulling every row (including
    // admin-only types and per-item overrides) just to discard most of them client-side.
    async GetAvailableForContributionAsync(): Promise<ContentItemSetting[]> {
        const filter = 'isAvailableAsGeneralUserContribution eq true and contentItemId eq null';
        const url = `${this.relativeContentItemSettingsUrl}?$filter=${encodeURIComponent(filter)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContentItemSetting[];
    }
}

export default ContentItemSettingBroker;
