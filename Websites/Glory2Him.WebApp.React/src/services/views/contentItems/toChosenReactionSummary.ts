import {
    ContentItemReactionCount,
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

// The counts a card shows between a reader's choice and the read that follows the item's latest
// write (UI/Views/ChosenReactionSummary.md §1). The chosen reaction goes up by one and the held
// one down by one, wherever the held one has an entry; an entry at nought leaves; an added entry
// takes its place in the options' order, the vocabulary's. A new summary, never an edited one.
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

    const isRankedBeforeChosen = (label: string): boolean =>
        rankOf(label) >= 0 && rankOf(label) < rankOf(chosenReactionLabel);

    const addedAt = movedSummary.reduce(
        (place, reactionCount, index) => isRankedBeforeChosen(reactionCount.label) ? index + 1 : place,
        0);

    return [
        ...movedSummary.slice(0, addedAt),
        ...addedSummary,
        ...movedSummary.slice(addedAt)
    ];
};
