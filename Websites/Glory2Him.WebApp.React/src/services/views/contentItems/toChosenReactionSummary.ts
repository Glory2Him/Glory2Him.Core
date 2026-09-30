import {
    ContentItemReactionCount,
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

export const toChosenReactionSummary = (
    reactionSummary: ReadonlyArray<ContentItemReactionCount> | undefined,
    heldReactionLabel: string | undefined,
    chosenReactionLabel: string | undefined,
    options: ReadonlyArray<ContentItemReactionOption>
): ReadonlyArray<ContentItemReactionCount> => [];
