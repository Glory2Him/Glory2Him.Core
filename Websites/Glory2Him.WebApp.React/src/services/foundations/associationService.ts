import { useMutation } from '@tanstack/react-query';
import { AssociationRequest } from '../../models/foundations/associations/associationRequest';
import { AssociationSuggestionResult } from '../../models/foundations/associations/associationSuggestionResult';

export const associationService = {
    useUpsertAssociation: () => {
        return useMutation<AssociationSuggestionResult, unknown, AssociationRequest>({
            mutationFn: async () => {
                throw new Error('Not implemented');
            }
        });
    }
};
