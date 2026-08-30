import { ContentItemSetting } from "../models/foundations/contentItemSettings/contentItemSetting";

import {
    ContentItemSettingPage,
    ContentItemSettingQuery
} from "../models/foundations/contentItemSettings/contentItemSettingQuery";

import { ContentType } from "../models/foundations/contentItemSettings/contentType";
import ApiBroker from "./apiBroker";

// OData string literals are single-quoted, and a single quote inside one is escaped by doubling
// it. Search terms come from a free-text box, so this is the difference between a working
// filter and a 400.
const toODataLiteral = (value: string): string =>
    `'${value.replace(/'/g, "''")}'`;

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

    // Every per-content-type DEFAULT row, whether or not it is open to general contribution: a
    // page that RENDERS an item needs its type's name, icon and field shaping regardless of who
    // may contribute one. The reads are [AllowAnonymous] (the effective settings drive rendering
    // for signed-out readers too), so this is safe on a public page.
    async GetDefaultsAsync(): Promise<ContentItemSetting[]> {
        const filter = 'contentItemId eq null';
        const url = `${this.relativeContentItemSettingsUrl}?$filter=${encodeURIComponent(filter)}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContentItemSetting[];
    }

    // The admin list. Searching, filtering, ordering and paging all run server-side through
    // [EnableQuery] — the host caps a collection read at OData:PageSize rows, so a client that
    // paged in memory would silently stop at that cap once the overrides outgrow it.
    //
    // One row beyond the page is asked for and then dropped. The response is a plain array with
    // no total in it, so an extra row is the only thing that distinguishes a full last page from
    // a page with more behind it.
    async GetContentItemSettingsAsync(query: ContentItemSettingQuery): Promise<ContentItemSettingPage> {
        const filters: string[] = [];

        if (query.searchTerm != null && query.searchTerm.trim().length > 0) {
            const term = toODataLiteral(query.searchTerm.trim().toLowerCase());

            filters.push(
                `(contains(tolower(contentTypeName),${term})`
                + ` or contains(tolower(contentTypeDescription),${term}))`);
        }

        if (query.contentType != null) {
            filters.push(`contentType eq '${ContentType[query.contentType]}'`);
        }

        if (query.scope === 'Default') {
            filters.push('contentItemId eq null');
        }

        if (query.scope === 'Override') {
            filters.push('contentItemId ne null');
        }

        const parameters = new URLSearchParams();

        if (filters.length > 0) {
            parameters.set('$filter', filters.join(' and '));
        }

        parameters.set('$orderby', 'contentType,contentItemId');
        parameters.set('$skip', String((query.page - 1) * query.pageSize));
        parameters.set('$top', String(query.pageSize + 1));

        const url = `${this.relativeContentItemSettingsUrl}?${parameters.toString()}`;
        const result = await this.apiBroker.GetAsync(url);
        const rows = result.data as ContentItemSetting[];

        return {
            items: rows.slice(0, query.pageSize),
            page: query.page,
            pageSize: query.pageSize,
            hasNextPage: rows.length > query.pageSize
        };
    }

    async GetContentItemSettingByIdAsync(contentItemSettingId: string): Promise<ContentItemSetting> {
        const url = `${this.relativeContentItemSettingsUrl}/${contentItemSettingId}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContentItemSetting;
    }

    // The exposer routes the update on the body's Id rather than a route segment, so the whole
    // entity goes back — audit fields included, which the foundation overwrites.
    async UpdateContentItemSettingAsync(contentItemSetting: ContentItemSetting): Promise<ContentItemSetting> {
        const result = await this.apiBroker.PutAsync(
            this.relativeContentItemSettingsUrl,
            contentItemSetting);

        return result.data as ContentItemSetting;
    }
}

export default ContentItemSettingBroker;
