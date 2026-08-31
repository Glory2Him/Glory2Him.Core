import { useQuery } from '@tanstack/react-query';
import ReactionBroker from '../../brokers/apiBroker.reactions';
import { Reaction } from '../../models/foundations/reactions/reaction';

export const reactionService = {
    // The choices behind every Like control. One vocabulary for the whole site, so a long
    // staleTime is right: a new reaction being approved is a rare event, and the next page load
    // picks it up.
    useGetApprovedReactions: () => {
        const reactionBroker = new ReactionBroker();

        return useQuery<Reaction[]>({
            queryKey: ['ReactionsGetApproved'],
            queryFn: async () => await reactionBroker.GetApprovedReactionsAsync(),
            staleTime: 5 * 60 * 1000
        });
    }
};
