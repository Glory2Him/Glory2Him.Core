import { describe, expect, it } from 'vitest';
import { readingTimeMinutesOf } from './readingTimeMinutesOf';

const wordsOf = (count: number): string =>
    Array.from({ length: count }, () => 'word').join(' ');

describe('readingTimeMinutesOf', () => {
    it('should count a minute for every two hundred words', () => {
        // when
        const actualMinutes = readingTimeMinutesOf(wordsOf(600));

        // then
        expect(actualMinutes).toBe(3);
    });

    it('should round to the nearest minute rather than truncating', () => {
        // when: 500 words is two and a half minutes
        const actualMinutes = readingTimeMinutesOf(wordsOf(500));

        // then
        expect(actualMinutes).toBe(3);
    });

    it('should never report a piece with words in it as taking no time', () => {
        // when: a one-line quote still takes a moment, and "0 min read" reads as a defect
        const actualMinutes = readingTimeMinutesOf('He is faithful.');

        // then
        expect(actualMinutes).toBe(1);
    });

    it('should count words rather than whitespace', () => {
        // when: paragraph breaks and runs of spaces are how a textarea arrives
        const actualMinutes = readingTimeMinutesOf('  one\n\n  two   three \n four  ');

        // then
        expect(actualMinutes).toBe(1);
    });

    it('should return nothing to report for content with no words', () => {
        // when
        const actualMinutes = readingTimeMinutesOf('   \n  ');

        // then: zero is the caller's cue to leave the figure out, not to render "0 min read"
        expect(actualMinutes).toBe(0);
    });

    it('should treat absent content as nothing to report', () => {
        // when
        const actualMinutes = readingTimeMinutesOf(undefined);

        // then
        expect(actualMinutes).toBe(0);
    });
});
