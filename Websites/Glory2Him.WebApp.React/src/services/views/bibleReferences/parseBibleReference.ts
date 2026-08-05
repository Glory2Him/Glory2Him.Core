import { resolveVersionId } from '../../../models/youVersion/youVersionVersions';

// Parses the bible.com-style reference segment used by our /BibleReferences/:reference
// route into the USFM passage the YouVersion SDK components consume:
//
//   JHN.3          → the whole chapter (Bible Reader)
//   JHN.3.16       → a single verse (Bible Card)
//   JHN.3.16-17    → a verse range (Bible Card)
//   JHN.3.16.17    → bible.com's dotted range form, normalized to 16-17 (Bible Card)
//   ...NIV suffix  → optional version abbreviation (JHN.3.16.NIV), resolved to a version id
//
// USFM book codes are three characters (JHN, GEN, 1JN, SNG...); chapters and verses are
// numeric. Anything that does not fit returns null so the router can show Not Found.
export interface ParsedBibleReference {
    book: string;
    chapter: string;
    verseRange: string | null;
    versionId: number;
    versionAbbreviation: string | null;

    // "JHN.3" for a chapter, "JHN.3.16" / "JHN.3.16-17" for verses — what the SDK takes.
    usfmPassage: string;
}

const bookPattern = /^[0-9]?[A-Z]{2,3}$/;
const numberPattern = /^[0-9]+$/;
const versePattern = /^[0-9]+(-[0-9]+)?$/;
const abbreviationPattern = /^[A-Z][A-Z0-9]{1,7}$/;

export function parseBibleReference(referenceSegment: string): ParsedBibleReference | null {
    const segments = decodeURIComponent(referenceSegment).toUpperCase().split('.');

    if (segments.length < 2 || segments.length > 5) {
        return null;
    }

    // A trailing alphabetic segment is the version abbreviation (JHN.3.16.NIV).
    let versionAbbreviation: string | null = null;
    const lastSegment = segments[segments.length - 1];

    if (!versePattern.test(lastSegment)) {
        if (!abbreviationPattern.test(lastSegment)) {
            return null;
        }

        versionAbbreviation = lastSegment;
        segments.pop();
    }

    const [book, chapter, verse, verseEnd, ...rest] = segments;

    if (rest.length > 0 || !book || !chapter) {
        return null;
    }

    if (!bookPattern.test(book) || !numberPattern.test(chapter)) {
        return null;
    }

    let verseRange: string | null = null;

    if (verse !== undefined) {
        if (!versePattern.test(verse)) {
            return null;
        }

        verseRange = verse;

        // bible.com's dotted range form: JHN.3.16.17 means verses 16-17.
        if (verseEnd !== undefined) {
            if (!numberPattern.test(verseEnd) || verse.includes('-')) {
                return null;
            }

            verseRange = `${verse}-${verseEnd}`;
        }
    } else if (verseEnd !== undefined) {
        return null;
    }

    return {
        book,
        chapter,
        verseRange,
        versionId: resolveVersionId(versionAbbreviation ?? undefined),
        versionAbbreviation,
        usfmPassage: verseRange ? `${book}.${chapter}.${verseRange}` : `${book}.${chapter}`,
    };
}
