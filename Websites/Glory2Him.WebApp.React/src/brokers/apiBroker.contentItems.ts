import {
    ContentItem,
    ContentItemAddRequest
} from '../models/foundations/contentItems/contentItem';

import {
    ContentItemPage,
    ContentItemSearchQuery
} from '../models/foundations/contentItems/contentItemSearchQuery';

import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import ApiBroker from './apiBroker';

// OData string literals are single-quoted, and a single quote inside one is escaped by doubling
// it. Search terms come from a free-text box, so this is the difference between a working filter
// and a 400. Restated from the settings broker rather than shared: a broker depending on another
// broker is the one dependency the layer does not have.
const toODataLiteral = (value: string): string =>
    `'${value.replace(/'/g, "''")}'`;

class ContentItemBroker {
    relativeContentItemsUrl = '/api/contentitems';
    private apiBroker: ApiBroker = new ApiBroker();

    // The contribution. Only the six caller-supplied members travel — the processing service
    // mints the identifiers and hashes the content, and the foundation beneath it stamps the audit
    // trail from the request's own security context, so anything else sent here would be discarded
    // on arrival.
    async PostContentItemAsync(contentItem: ContentItemAddRequest): Promise<ContentItem> {
        const result = await this.apiBroker.PostAsync(this.relativeContentItemsUrl, contentItem);

        return result.data as ContentItem;
    }

    async GetContentItemByIdAsync(contentItemId: string): Promise<ContentItem> {
        const url = `${this.relativeContentItemsUrl}/${contentItemId}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContentItem;
    }

    // ONE PAGE of the list, searched, filtered, ordered and paged SERVER-SIDE through
    // [EnableQuery]. The host caps a collection read at OData:PageSize, so a client that paged in
    // memory would silently stop at that cap once the table outgrew it.
    //
    // THE CALLER-SCOPED READ, not /Public: it is [AllowAnonymous] and widens with the caller — the
    // canonically visible set for a visitor, plus their own rows when signed in, plus everything a
    // review role covers. One read serves the public feed, "my contributions" and a moderation
    // queue, and the foundation decides which of those the caller actually gets against the
    // stored row. Filtering that here would be deciding it twice, and badly.
    //
    // One row beyond the page is asked for and then dropped — see ContentItemSearchQuery.
    async SearchContentItemsAsync(query: ContentItemSearchQuery): Promise<ContentItemPage> {
        const filters: string[] = [];
        const searchTerm = query.searchTerm.trim();

        if (searchTerm.length > 0) {
            const term = toODataLiteral(searchTerm.toLowerCase());

            filters.push(
                `(contains(tolower(title),${term})`
                + ` or contains(tolower(content),${term})`
                + ` or contains(tolower(author),${term}))`);
        }

        // The MEMBER NAME, which is what $filter parses — the JSON body carries the number.
        if (query.contentType != null) {
            filters.push(`contentType eq '${ContentType[query.contentType]}'`);
        }

        const author = query.author.trim();

        if (author.length > 0) {
            filters.push(`contains(tolower(author),${toODataLiteral(author.toLowerCase())})`);
        }

        const parameters = new URLSearchParams();

        if (filters.length > 0) {
            parameters.set('$filter', filters.join(' and '));
        }

        // CreatedWhen rather than PublishDate: a draft has no publish date, and this read answers
        // with the caller's drafts as well as the published set, so ordering on a column half the
        // rows leave null would scatter them.
        parameters.set('$orderby', 'createdWhen desc');
        parameters.set('$skip', String(query.pageIndex * query.pageSize));
        parameters.set('$top', String(query.pageSize + 1));

        const url = `${this.relativeContentItemsUrl}?${parameters.toString()}`;
        const result = await this.apiBroker.GetAsync(url);
        const rows = result.data as ContentItem[];

        return {
            items: rows.slice(0, query.pageSize),
            pageIndex: query.pageIndex,
            pageSize: query.pageSize,
            hasNextPage: rows.length > query.pageSize
        };
    }
}

export default ContentItemBroker;
