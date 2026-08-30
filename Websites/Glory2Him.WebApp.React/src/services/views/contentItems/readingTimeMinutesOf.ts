// Words per minute for silent reading of ordinary prose. The research spread is wide (roughly
// 200-260 for non-technical text); 200 is the low end deliberately, because a reading time that
// under-promises is a kindness and one that over-promises is a small lie.
const wordsPerMinute = 200;

// HOW LONG THIS WILL TAKE TO READ, from the content and nothing else.
//
// Computed rather than stored: it is a pure function of the text, so a stored copy would be one
// more field to keep in step with an edit, and it would be wrong the moment somebody amended a
// paragraph. ContentItemPanel takes it as a PROP rather than working it out itself — the panel is
// pure presentation, and a consumer rendering a list wants to compute this once per item rather
// than have every panel do it on every render.
//
// NEVER ZERO. A one-line quote still takes a moment, and "0 min read" reads as a defect rather
// than as a short piece — so anything with words in it rounds up to one minute, and only genuinely
// empty content returns 0 for the caller to leave out.
export const readingTimeMinutesOf = (content: string | undefined): number => {
    const wordCount = (content ?? '')
        .split(/\s+/)
        .filter((word) => word.length > 0)
        .length;

    if (wordCount === 0) {
        return 0;
    }

    return Math.max(1, Math.round(wordCount / wordsPerMinute));
};
