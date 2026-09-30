import {
    ContentItemReactionCount,
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

export const toChosenReactionSummary = (
    reactionSummary: ReadonlyArray<ContentItemReactionCount> | undefined,
    heldReactionLabel: string | undefined,
    chosenReactionLabel: string | undefined,
    options: ReadonlyArray<ContentItemReactionOption>
): ReadonlyArray<ContentItemReactionCount> =>
    (reactionSummary ?? []).map((reactionCount) => ({
        ...reactionCount,
        count: reactionCount.count
            - (reactionCount.label === heldReactionLabel ? 1 : 0)
            + (reactionCount.label === chosenReactionLabel ? 1 : 0)
    })).filter((reactionCount) => reactionCount.count > 0);
