import { describe, expect, it } from 'vitest';
import { parseBibleReference } from './parseBibleReference';

describe('parseBibleReference', () => {
    it('parses a single verse', () => {
        const parsed = parseBibleReference('JHN.3.16');

        expect(parsed).not.toBeNull();
        expect(parsed!.book).toBe('JHN');
        expect(parsed!.chapter).toBe('3');
        expect(parsed!.verseRange).toBe('16');
        expect(parsed!.usfmPassage).toBe('JHN.3.16');
        expect(parsed!.versionAbbreviation).toBeNull();
        expect(parsed!.versionId).toBe(111);
    });

    it('parses a verse with a version abbreviation', () => {
        const parsed = parseBibleReference('JHN.3.16.NIV');

        expect(parsed!.usfmPassage).toBe('JHN.3.16');
        expect(parsed!.versionAbbreviation).toBe('NIV');
        expect(parsed!.versionId).toBe(111);
    });

    it('parses a hyphenated verse range', () => {
        const parsed = parseBibleReference('JHN.3.16-17.ESV');

        expect(parsed!.verseRange).toBe('16-17');
        expect(parsed!.usfmPassage).toBe('JHN.3.16-17');
        expect(parsed!.versionId).toBe(59);
    });

    it("normalizes bible.com's dotted range form", () => {
        const parsed = parseBibleReference('JHN.3.16.17.NIV');

        expect(parsed!.verseRange).toBe('16-17');
        expect(parsed!.usfmPassage).toBe('JHN.3.16-17');
        expect(parsed!.versionAbbreviation).toBe('NIV');
    });

    it('parses a chapter-only reference', () => {
        const parsed = parseBibleReference('JHN.3');

        expect(parsed!.verseRange).toBeNull();
        expect(parsed!.usfmPassage).toBe('JHN.3');
    });

    it('parses a chapter with a version abbreviation', () => {
        const parsed = parseBibleReference('GEN.1.KJV');

        expect(parsed!.verseRange).toBeNull();
        expect(parsed!.usfmPassage).toBe('GEN.1');
        expect(parsed!.versionId).toBe(1);
    });

    it('parses numbered book codes', () => {
        const parsed = parseBibleReference('1JN.4.19');

        expect(parsed!.book).toBe('1JN');
        expect(parsed!.usfmPassage).toBe('1JN.4.19');
    });

    it('is case-insensitive', () => {
        const parsed = parseBibleReference('jhn.3.16.niv');

        expect(parsed!.usfmPassage).toBe('JHN.3.16');
        expect(parsed!.versionAbbreviation).toBe('NIV');
    });

    it('falls back to NIV for unknown version abbreviations', () => {
        const parsed = parseBibleReference('JHN.3.16.XYZ');

        expect(parsed!.versionAbbreviation).toBe('XYZ');
        expect(parsed!.versionId).toBe(111);
    });

    it.each([
        'JHN',
        'JHN.3.16-17.18',
        'JHN.three.16',
        'NOTABOOK.3.16',
        'JHN.3.16.17.18.NIV',
        '',
        'JHN..16',
    ])('rejects malformed reference "%s"', (reference) => {
        expect(parseBibleReference(reference)).toBeNull();
    });
});
