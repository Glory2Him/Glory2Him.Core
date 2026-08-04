import { describe, expect, it } from 'vitest';
import { formatDate, formatDateTime } from './dateFormats';

describe('formatDate', () => {
    it('should format a date as MMM dd, yyyy', () => {
        // given
        const date = new Date(2026, 0, 5);

        // when
        const result = formatDate(date);

        // then
        expect(result).toBe('Jan 05, 2026');
    });

    it('should keep two-digit days unpadded beyond the pad', () => {
        // given
        const date = new Date(2025, 11, 25);

        // when
        const result = formatDate(date);

        // then
        expect(result).toBe('Dec 25, 2025');
    });
});

describe('formatDateTime', () => {
    it('should format midnight as 12:00 AM', () => {
        // given
        const date = new Date(2026, 2, 1, 0, 0);

        // when
        const result = formatDateTime(date);

        // then
        expect(result).toBe('Mar 01, 2026 at 12:00 AM');
    });

    it('should format noon as 12:05 PM', () => {
        // given
        const date = new Date(2026, 5, 15, 12, 5);

        // when
        const result = formatDateTime(date);

        // then
        expect(result).toBe('Jun 15, 2026 at 12:05 PM');
    });

    it('should format a morning time with unpadded hours and padded minutes', () => {
        // given
        const date = new Date(2026, 7, 4, 9, 7);

        // when
        const result = formatDateTime(date);

        // then
        expect(result).toBe('Aug 04, 2026 at 9:07 AM');
    });

    it('should format an afternoon time in twelve hour clock', () => {
        // given
        const date = new Date(2026, 9, 31, 23, 59);

        // when
        const result = formatDateTime(date);

        // then
        expect(result).toBe('Oct 31, 2026 at 11:59 PM');
    });
});
