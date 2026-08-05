import { describe, expect, it } from 'vitest';
import { bibleReferenceHref, toUsfmReference } from './toUsfmReference';

describe('toUsfmReference', () => {
    it.each([
        ['John 3:16', 'JHN.3.16'],
        ['1 Timothy 2:5', '1TI.2.5'],
        ['2 Kings 20:9-11', '2KI.20.9-11'],
        ['Romans 3:23–24', 'ROM.3.23-24'],
        ['Ephesians 2:8–9', 'EPH.2.8-9'],
        ['Ephesians 6:10–18', 'EPH.6.10-18'],
        ['Song of Songs 2:1', 'SNG.2.1'],
        ['Song of Solomon 2:1', 'SNG.2.1'],
        ['Psalm 23', 'PSA.23'],
        ['Psalms 23:1', 'PSA.23.1'],
        ['Acts 4:12', 'ACT.4.12'],
        ['revelation 22:20', 'REV.22.20'],
    ])('converts "%s" to %s', (display, expected) => {
        expect(toUsfmReference(display)).toBe(expected);
    });

    it('addresses a reference citing separate verse groups as its chapter', () => {
        expect(toUsfmReference('Joshua 10:8, 12–13')).toBe('JOS.10');
    });

    it('keeps a single range intact', () => {
        expect(toUsfmReference('Joshua 10:12–13')).toBe('JOS.10.12-13');
    });

    it.each([
        'Nowhere 3:16',
        'not a reference',
        '',
        '3:16',
    ])('rejects unreadable reference "%s"', (display) => {
        expect(toUsfmReference(display)).toBeNull();
    });
});

describe('bibleReferenceHref', () => {
    it('points a readable reference at the passage', () => {
        expect(bibleReferenceHref('2 Kings 20:9–11')).toBe('/BibleReferences/2KI.20.9-11');
    });

    it('falls back to a search when the reference cannot be read', () => {
        expect(bibleReferenceHref('The bit about the fig tree'))
            .toBe('/Search?q=The%20bit%20about%20the%20fig%20tree');
    });
});
