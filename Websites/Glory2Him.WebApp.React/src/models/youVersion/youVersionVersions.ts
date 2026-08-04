// YouVersion Platform Bible version ids. The default version a component opens with —
// readers can still switch via the version picker. Which versions the app key may serve
// is governed by its licensing in the YouVersion Platform portal.
export const youVersionVersions = {
    niv: 111,
} as const;

// Well-known YouVersion version ids by the abbreviation used in bible.com URLs
// (https://www.bible.com/bible/111/JHN.3.16.NIV — the trailing segment). Unknown
// abbreviations fall back to NIV rather than failing the whole page.
const versionIdsByAbbreviation: Record<string, number> = {
    NIV: 111,
    KJV: 1,
    ESV: 59,
    NLT: 116,
    NKJV: 114,
    MSG: 97,
    AMP: 1588,
    CSB: 1713,
};

export const resolveVersionId = (abbreviation: string | undefined): number => {
    if (!abbreviation) {
        return youVersionVersions.niv;
    }

    return versionIdsByAbbreviation[abbreviation.toUpperCase()] ?? youVersionVersions.niv;
};
