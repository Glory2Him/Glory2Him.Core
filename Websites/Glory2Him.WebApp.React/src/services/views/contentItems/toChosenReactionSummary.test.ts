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
});
