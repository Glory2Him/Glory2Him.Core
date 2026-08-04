import { useParams } from 'react-router-dom';
import { useVersionAbbreviations } from '../hooks/useVersionAbbreviations';
import { parseBibleReference } from '../services/views/bibleReferences/parseBibleReference';
import { BibleReference } from './bibleReference';
import { BibleReader } from './bibleReader';
import { NotFound } from './notFound';

// The bible.com-style deep-link route (/BibleReferences/:reference): a reference with a
// verse shows the Bible Card, a chapter-only reference opens the Bible Reader —
//
//   /BibleReferences/JHN.3.16.NIV     → card, John 3:16, NIV
//   /BibleReferences/JHN.3.16-17.NIV  → card, John 3:16-17
//   /BibleReferences/JHN.3.16.17.NIV  → card, John 3:16-17 (bible.com's dotted range form)
//   /BibleReferences/JHN.3.NIV        → reader, John 3
//
// An unparseable segment is a Not Found, same as any other unknown URL.
export function BibleReferenceView() {
    const { reference } = useParams();
    const { versionIdFor } = useVersionAbbreviations();
    const parsed = reference !== undefined ? parseBibleReference(reference) : null;

    if (parsed === null) {
        return <NotFound />;
    }

    // The parser resolves the well-known abbreviations on its own; the catalogue covers the
    // rest once it loads, so a URL naming any translation opens in that translation.
    const versionId = parsed.versionAbbreviation != null
        ? versionIdFor(parsed.versionAbbreviation)
        : parsed.versionId;

    if (parsed.verseRange !== null) {
        const chapterSuffix = parsed.versionAbbreviation != null
            ? `.${parsed.versionAbbreviation}`
            : '';

        return (
            <BibleReference
                reference={parsed.usfmPassage}
                versionId={versionId}
                chapterHref={`/BibleReferences/${parsed.book}.${parsed.chapter}${chapterSuffix}`} />
        );
    }

    return (
        <BibleReader
            book={parsed.book}
            chapter={parsed.chapter}
            versionId={parsed.versionId} />
    );
}
