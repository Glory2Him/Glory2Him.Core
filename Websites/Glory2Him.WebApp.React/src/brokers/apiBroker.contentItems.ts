import {
    ContentItem,
    ContentItemAddRequest
} from '../models/foundations/contentItems/contentItem';

import ApiBroker from './apiBroker';

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
}

export default ContentItemBroker;
