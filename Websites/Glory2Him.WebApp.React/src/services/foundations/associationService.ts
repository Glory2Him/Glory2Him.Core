import { useMutation } from '@tanstack/react-query';
import AssociationBroker from '../../brokers/apiBroker.associations';
import { AssociationRequest } from '../../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../../models/foundations/associations/associationSuggestionResult';

export const associationService = {
    useUpsertAssociation: () => {
        const associationBroker = new AssociationBroker();

        return useMutation<AssociationSuggestionResult, unknown, AssociationRequest>({
            mutationFn: async (association: AssociationRequest) =>
                await associationBroker.PostAssociationAsync(association)
        });
    }
};
