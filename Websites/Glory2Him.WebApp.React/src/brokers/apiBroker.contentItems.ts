import {
    ContentItem,
    ContentItemAddRequest
} from '../models/foundations/contentItems/contentItem';

import {
    ContentItemPage,
    ContentItemSearchQuery
} from '../models/foundations/contentItems/contentItemSearchQuery';

import { ApprovalStatus } from '../models/components/associations/associationItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import ApiBroker from './apiBroker';

// ApprovalStatus is a const object rather than an enum, so it has no reverse mapping the way
// ContentType does — the member names $filter parses are stated here instead. This is a wire
// contract: OData parses the NAME while the JSON body carries the number.
const approvalStatusMemberNames: Readonly<Record<number, string>> = {
    [ApprovalStatus.Draft]: 'Draft',
    [ApprovalStatus.Submitted]: 'Submitted',
    [ApprovalStatus.Approved]: 'Approved',
    [ApprovalStatus.Rejected]: 'Rejected',
    [ApprovalStatus.Dismissed]: 'Dismissed'
};

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
    // WHICH ROUTE answers is the query's `scope`, because it is the PAGE's decision what a
    // surface shows. 'public' is caller-independent by construction (§14.1 canonical set only) —
    // the home feed builds on it so no role change elsewhere can leak a draft there. 'caller'
    // widens with whoever asks — their own rows, everything a review role covers — which is what
    // "my posts" and the moderation queue are made of. Either way the FOUNDATION decides
    // visibility against the stored row; the filters below only ever narrow within it.
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

        // Exact, not contains: an account id is an identity, and half of one identifies nobody.
        if (query.submittedById != null && query.submittedById.trim().length > 0) {
            filters.push(`createdBy eq ${toODataLiteral(query.submittedById.trim())}`);
        }

        // An or-chain of member names rather than `in`, so the clause stays inside the OData
        // grammar every version of the host's parser accepts. This only ever NARROWS: asking the
        // public route for drafts intersects to nothing rather than leaking anything.
        if (query.approvalStatuses != null && query.approvalStatuses.length > 0) {
            const statusClauses = query.approvalStatuses
                .map((status) => `approvalStatus eq '${approvalStatusMemberNames[status]}'`)
                .join(' or ');

            filters.push(`(${statusClauses})`);
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

        // The route the scope names — see the note above.
        const routeUrl = query.scope === 'public'
            ? `${this.relativeContentItemsUrl}/Public`
            : this.relativeContentItemsUrl;

        const url = `${routeUrl}?${parameters.toString()}`;
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
