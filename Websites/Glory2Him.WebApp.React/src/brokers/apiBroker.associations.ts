import ApiBroker from './apiBroker';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../models/foundations/associations/associationSuggestionResult';
import { ContentItemReactionSummary } from '../models/foundations/associations/contentItemReactionSummary';

class AssociationBroker {
    relativeAssociationsUrl = '/api/associations';
    private apiBroker: ApiBroker = new ApiBroker();

    // The server answers 201 on Created and 200 on every other outcome, both with the result as
    // the body; PostAsync resolves both.
    async PostAssociationAsync(association: AssociationRequest): Promise<AssociationSuggestionResult> {
        const result = await this.apiBroker.PostAsync(this.relativeAssociationsUrl, association);

        return result.data as AssociationSuggestionResult;
    }

    // Every id it is handed, in one request and in the order handed: bounding and chunking the
    // set are the caller's (§ARC16.8, The set, its bounds).
    async GetReactionSummariesAsync(
        contentItemIds: ReadonlyArray<string>): Promise<ContentItemReactionSummary[]> {
        const parameters = new URLSearchParams();
        contentItemIds.forEach(contentItemId => parameters.append('contentItemIds', contentItemId));

        const url = `${this.relativeAssociationsUrl}/reactionsummaries?${parameters}`;
        const result = await this.apiBroker.GetAsync(url);

        return result.data as ContentItemReactionSummary[];
    }
}

export default AssociationBroker;
