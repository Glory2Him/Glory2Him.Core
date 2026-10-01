import { describe, expect, it } from 'vitest';

import {
    ContentItemReactionCount,
    ContentItemReactionOption
} from '../../../models/components/contentItems/contentItemSearchItem';

import { toChosenReactionSummary } from './toChosenReactionSummary';

const amen: ContentItemReactionOption = { id: 'reaction-amen', label: 'Amen', glyph: '🙏' };
const love: ContentItemReactionOption = { id: 'reaction-love', label: 'Love', glyph: '❤️', isLove: true };
const joy: ContentItemReactionOption = { id: 'reaction-joy', label: 'Joy', glyph: '😄' };
const praying: ContentItemReactionOption = { id: 'reaction-praying', label: 'Praying', glyph: '🙌' };

// The vocabulary's order, which an added entry takes its place in.
const options: ReadonlyArray<ContentItemReactionOption> = [amen, love, joy, praying];

// A reaction the summary can name and the options do not list, as when the options were read
// before the vocabulary changed. Not one of the seeded five.
const hallelujah: ContentItemReactionOption = { id: 'reaction-hallelujah', label: 'Hallelujah', glyph: '🎉' };

const countOf = (option: ContentItemReactionOption, count: number): ContentItemReactionCount => ({
    label: option.label,
    glyph: option.glyph,
    count
});

describe('toChosenReactionSummary', () => {
    it('should move one count from the held reaction to the chosen one', () => {
        // given
        const reactionSummary = [countOf(joy, 2), countOf(love, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, joy.label, love.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(joy, 1), countOf(love, 2)]);
    });

    it('should drop an entry that reaches nought', () => {
        // given
        const reactionSummary = [countOf(amen, 4), countOf(love, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, love.label, undefined, options);

        // then
        expect(chosenSummary).toEqual([countOf(amen, 4)]);
    });

    it('should take one from a count above one', () => {
        // given
        const reactionSummary = [countOf(amen, 4), countOf(love, 3)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, love.label, undefined, options);

        // then
        expect(chosenSummary).toEqual([countOf(amen, 4), countOf(love, 2)]);
    });

    it('should place an added entry before the entries that follow it', () => {
        // given
        const reactionSummary = [countOf(praying, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, undefined, love.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(love, 1), countOf(praying, 1)]);
    });

    it('should place an added entry between the entries around it', () => {
        // given
        const reactionSummary = [countOf(amen, 1), countOf(praying, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, undefined, joy.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(amen, 1), countOf(joy, 1), countOf(praying, 1)]);
    });

    it('should start a summary for an item that has none', () => {
        // given
        const reactionSummary = undefined;

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, undefined, love.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(love, 1)]);
    });

    it('should take nothing from a held reaction that has no entry', () => {
        // given
        const reactionSummary = [countOf(amen, 1), countOf(love, 2)];

        // when
        const changedSummary = toChosenReactionSummary(reactionSummary, joy.label, praying.label, options);
        const withdrawnSummary = toChosenReactionSummary(reactionSummary, joy.label, undefined, options);

        // then
        expect(changedSummary).toEqual([countOf(amen, 1), countOf(love, 2), countOf(praying, 1)]);
        expect(withdrawnSummary).toEqual([countOf(amen, 1), countOf(love, 2)]);
    });

    it('should add an entry for a chosen reaction that has none when the held one has one', () => {
        // given
        const reactionSummary = [countOf(amen, 1), countOf(love, 2), countOf(praying, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, love.label, joy.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(amen, 1), countOf(love, 1), countOf(joy, 1), countOf(praying, 1)]);
    });

    it('should place an added entry among the entries left once the held one leaves', () => {
        // given
        const reactionSummary = [countOf(amen, 1), countOf(love, 1), countOf(praying, 1)];

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, love.label, joy.label, options);

        // then
        expect(chosenSummary).toEqual([countOf(amen, 1), countOf(joy, 1), countOf(praying, 1)]);
    });

    it.each([
        {
            position: 'last',
            reactionSummary: [countOf(amen, 1), countOf(praying, 1), countOf(hallelujah, 1)],
            expectedSummary: [countOf(amen, 1), countOf(joy, 1), countOf(praying, 1), countOf(hallelujah, 1)]
        },
        {
            position: 'first',
            reactionSummary: [countOf(hallelujah, 1), countOf(amen, 1), countOf(praying, 1)],
            expectedSummary: [countOf(hallelujah, 1), countOf(amen, 1), countOf(joy, 1), countOf(praying, 1)]
        }
    ])('should place an added entry among the entries the options rank (unlisted $position)', ({
        reactionSummary,
        expectedSummary
    }) => {
        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, undefined, joy.label, options);

        // then
        expect(chosenSummary).toEqual(expectedSummary);
    });

    it('should never change what it is handed', () => {
        // given
        const reactionSummary = [countOf(joy, 2), countOf(love, 1)];
        const handedOptions = [...options];
        const expectedReactionSummary = structuredClone(reactionSummary);
        const expectedOptions = structuredClone(handedOptions);

        // when
        const chosenSummary = toChosenReactionSummary(reactionSummary, love.label, praying.label, handedOptions);

        // then
        expect(chosenSummary).not.toBe(reactionSummary);
        expect(reactionSummary).toEqual(expectedReactionSummary);
        expect(handedOptions).toEqual(expectedOptions);
    });
});
