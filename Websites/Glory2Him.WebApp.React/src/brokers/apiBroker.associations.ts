import ApiBroker from './apiBroker';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../models/foundations/associations/associationSuggestionResult';

class AssociationBroker {
    relativeAssociationsUrl = '/api/associations';
    private apiBroker: ApiBroker = new ApiBroker();

    // The server answers 201 on Created and 200 on every other outcome, both with the result as
    // the body; PostAsync resolves both.
    async PostAssociationAsync(association: AssociationRequest): Promise<AssociationSuggestionResult> {
        const result = await this.apiBroker.PostAsync(this.relativeAssociationsUrl, association);

        return result.data as AssociationSuggestionResult;
    }
}

export default AssociationBroker;
