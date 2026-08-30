import { useQuery } from '@tanstack/react-query';
import axios from 'axios';
import ContributorBroker from '../../brokers/apiBroker.contributors';
import { ContributorSummary } from '../../models/foundations/contributors/contributorSummary';

export const contributorService = {
    // The byline identity behind a ContentItem's CreatedBy.
    //
    // A 404 IS AN ANSWER, NOT A FAILURE, and it is swallowed into null here rather than left to
    // reject. The global QueryCache toasts "An unknown error has occurred, please refresh the
    // page" on every query error and offers no per-query opt-out — so an item whose contributor's
    // account is gone would greet a reader with an error popup over an article that rendered
    // perfectly well. Null is what the caller wants anyway: no byline.
    //
    // Anything else still throws. A 500 or a dropped connection IS a failure, and the reader
    // should be told the page is not showing them everything.
    useGetContributorById: (userId: string, enabled = true) => {
        const contributorBroker = new ContributorBroker();

        return useQuery<ContributorSummary | null>({
            queryKey: ['ContributorsGetById', userId],

            queryFn: async () => {
                try {
                    return await contributorBroker.GetContributorByIdAsync(userId);
                } catch (error) {
                    if (axios.isAxiosError(error) && error.response?.status === 404) {
                        return null;
                    }

                    throw error;
                }
            },

            enabled: enabled && userId.length > 0,

            // A display name and an avatar change rarely, and every contribution on a page asks
            // for the same handful of people. Five minutes keeps a list of items from re-asking
            // for each of them on every render pass.
            staleTime: 5 * 60 * 1000
        });
    }
};
