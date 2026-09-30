import {
    ContentItemReactionCount,
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

export const toChosenReactionSummary = (
    reactionSummary: ReadonlyArray<ContentItemReactionCount> | undefined,
    heldReactionLabel: string | undefined,
    chosenReactionLabel: string | undefined,
    options: ReadonlyArray<ContentItemReactionOption>
): ReadonlyArray<ContentItemReactionCount> => {
    const summary = reactionSummary ?? [];

    const movedSummary = summary.map((reactionCount) => ({
        ...reactionCount,
        count: reactionCount.count
            - (reactionCount.label === heldReactionLabel ? 1 : 0)
            + (reactionCount.label === chosenReactionLabel ? 1 : 0)
    })).filter((reactionCount) => reactionCount.count > 0);

    const addedSummary = summary.some((reactionCount) => reactionCount.label === chosenReactionLabel)
        ? []
        : options
            .filter((option) => option.label === chosenReactionLabel)
            .map((option) => ({ label: option.label, glyph: option.glyph, count: 1 }));

    const rankOf = (label: string | undefined): number =>
        options.findIndex((option) => option.label === label);

    const addedAt = movedSummary
        .filter((reactionCount) => rankOf(reactionCount.label) < rankOf(chosenReactionLabel))
        .length;

    return [
        ...movedSummary.slice(0, addedAt),
        ...addedSummary,
        ...movedSummary.slice(addedAt)
    ];
};
