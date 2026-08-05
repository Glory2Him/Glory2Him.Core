// Turns a reference as a person writes it — "Joshua 10:8, 12–13", "2 Kings 20:9–11",
// "1 Timothy 2:5" — into the bible.com-style USFM segment our /BibleReferences/:reference
// route understands, so an editorial reference on a post links to the passage itself.
//
// A reference citing separate verse groups ("Joshua 10:8, 12–13") addresses as the whole
// chapter: one URL can only name one passage, and the chapter holds every verse cited,
// whereas spanning 8–13 would silently include verses the post never quoted.
const usfmByBookName: Record<string, string> = {
    genesis: 'GEN', exodus: 'EXO', leviticus: 'LEV', numbers: 'NUM',
    deuteronomy: 'DEU', joshua: 'JOS', judges: 'JDG', ruth: 'RUT',
    '1samuel': '1SA', '2samuel': '2SA', '1kings': '1KI', '2kings': '2KI',
    '1chronicles': '1CH', '2chronicles': '2CH', ezra: 'EZR', nehemiah: 'NEH',
    esther: 'EST', job: 'JOB', psalm: 'PSA', psalms: 'PSA',
    proverbs: 'PRO', ecclesiastes: 'ECC', songofsongs: 'SNG', songofsolomon: 'SNG',
    isaiah: 'ISA', jeremiah: 'JER', lamentations: 'LAM', ezekiel: 'EZK',
    daniel: 'DAN', hosea: 'HOS', joel: 'JOL', amos: 'AMO',
    obadiah: 'OBA', jonah: 'JON', micah: 'MIC', nahum: 'NAM',
    habakkuk: 'HAB', zephaniah: 'ZEP', haggai: 'HAG', zechariah: 'ZEC',
    malachi: 'MAL',

    matthew: 'MAT', mark: 'MRK', luke: 'LUK', john: 'JHN',
    acts: 'ACT', romans: 'ROM', '1corinthians': '1CO', '2corinthians': '2CO',
    galatians: 'GAL', ephesians: 'EPH', philippians: 'PHP', colossians: 'COL',
    '1thessalonians': '1TH', '2thessalonians': '2TH', '1timothy': '1TI', '2timothy': '2TI',
    titus: 'TIT', philemon: 'PHM', hebrews: 'HEB', james: 'JAS',
    '1peter': '1PE', '2peter': '2PE', '1john': '1JN', '2john': '2JN',
    '3john': '3JN', jude: 'JUD', revelation: 'REV',
};

// "Joshua 10:8, 12–13" → book "Joshua", chapter "10", verses "8, 12–13".
const referencePattern = /^\s*([1-3]?\s*[A-Za-z][A-Za-z\s]*?)\s+([0-9]+)\s*(?::\s*(.+))?\s*$/;

export function toUsfmReference(displayReference: string): string | null {
    const match = referencePattern.exec(displayReference);

    if (match === null) {
        return null;
    }

    const [, bookName, chapter, versePart] = match;
    const book = usfmByBookName[bookName.replace(/\s+/g, '').toLowerCase()];

    if (book === undefined) {
        return null;
    }

    if (versePart === undefined) {
        return `${book}.${chapter}`;
    }

    // En dash, em dash and minus sign all read as ranges in editorial copy.
    const verseGroups = versePart
        .replace(/[‐-―−]/g, '-')
        .split(',')
        .map((group) => group.replace(/\s+/g, ''))
        .filter((group) => group.length > 0);

    if (verseGroups.length !== 1 || !/^[0-9]+(-[0-9]+)?$/.test(verseGroups[0])) {
        return `${book}.${chapter}`;
    }

    return `${book}.${chapter}.${verseGroups[0]}`;
}

// The href a reference pill points at, or the passage search when the reference cannot be
// read — a link that lands somewhere useful beats one that 404s.
export function bibleReferenceHref(displayReference: string): string {
    const usfm = toUsfmReference(displayReference);

    return usfm != null
        ? `/BibleReferences/${usfm}`
        : `/Search?q=${encodeURIComponent(displayReference)}`;
}
