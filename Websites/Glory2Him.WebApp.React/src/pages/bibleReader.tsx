import { useEffect, useRef } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BibleReader as YouVersionBibleReader } from '@youversion/platform-react-ui';
import * as sampleScripture from '../data/sampleScripture';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { useVersionAbbreviations } from '../hooks/useVersionAbbreviations';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';

// The whole chapter, read through the YouVersion Platform SDK's BibleReader: chapter and
// version pickers plus font settings on the toolbar, licensed scripture in the body.
// Content display only — no YouVersion sign-in and no auth-gated features (highlights,
// notes) are enabled.
//
// The reader is URL-driven: the route (/BibleReferences/JHN.14.NIV) is the source of truth,
// and every in-reader navigation — chapter arrows, book/chapter picker, version picker —
// pushes the matching URL, so refresh keeps the reader where it was and every position is a
// shareable link. Book selection alone doesn't navigate (the picker is mid-flow until a
// chapter is chosen); it is tracked in a ref and committed when the chapter lands.
//
// Wider than the single-verse page (col-lg-10, not 7) so the reader has room to breathe.
type BibleReaderParameters = {
    book?: string,
    chapter?: string,
    versionId?: number
}

export function BibleReader({
    book = 'JHN',
    chapter = '14',
    versionId = youVersionVersions.niv,
}: BibleReaderParameters) {
    const { isLoading, isAvailable } = useYouVersionAvailability();
    const { abbreviationFor } = useVersionAbbreviations();
    const navigate = useNavigate();
    const isDefaultChapter = book === 'JHN' && chapter === '14';

    const referenceUrl = (newBook: string, newChapter: string, newVersionId: number) => {
        const abbreviation = abbreviationFor(newVersionId);
        const versionSuffix = abbreviation != null ? `.${abbreviation}` : '';

        return `/BibleReferences/${newBook}.${newChapter}${versionSuffix}`;
    };

    // The book the picker has selected but not yet committed with a chapter.
    const pendingBookRef = useRef(book);
    pendingBookRef.current = book;

    useEffect(() => {
        document.title = isDefaultChapter
            ? `${sampleScripture.chapterReference} — Glory 2 Him`
            : `${book}.${chapter} — Glory 2 Him`;
    }, [book, chapter, isDefaultChapter]);

    const onBookChange = (newBook: string) => {
        pendingBookRef.current = newBook;
    };

    const onChapterChange = (newChapter: string) => {
        navigate(referenceUrl(pendingBookRef.current, newChapter, versionId));
    };

    const onVersionChange = (newVersionId: number) => {
        navigate(referenceUrl(book, chapter, newVersionId));
    };

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-10">
                        {!isLoading && (
                            isAvailable
                                ? (
                                    <YouVersionBibleReader.Root
                                        book={book}
                                        chapter={chapter}
                                        versionId={versionId}
                                        onBookChange={onBookChange}
                                        onChapterChange={onChapterChange}
                                        onVersionChange={onVersionChange}>
                                        <YouVersionBibleReader.Toolbar />
                                        <YouVersionBibleReader.Content />
                                    </YouVersionBibleReader.Root>
                                )
                                : <YouVersionUnavailableMessage />
                        )}

                        {/* The verse link and tag row belong to the curated John 14 page; an
                            arbitrary deep-linked chapter has no companion verse or tags. */}
                        {isDefaultChapter && (
                            <>
                                <div className="text-center my-4">
                                    <Link to="/BibleReferences" className="btn-link">
                                        Show {sampleScripture.reference} on its own
                                    </Link>
                                </div>

                                <hr className="my-4" />

                                <div className="d-flex flex-wrap align-items-center gap-2">
                                    <span className="fw-bold me-1">Tags:</span>
                                    {sampleScripture.tags.map((tag) => (
                                        <Link
                                            key={tag}
                                            to={`/Search?q=${encodeURIComponent(tag)}`}
                                            className="btn btn-sm btn-outline-secondary mb-0">{tag}</Link>
                                    ))}
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
