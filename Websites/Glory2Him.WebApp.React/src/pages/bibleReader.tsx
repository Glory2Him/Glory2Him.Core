import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { BibleReader as YouVersionBibleReader } from '@youversion/platform-react-ui';
import * as sampleScripture from '../data/sampleScripture';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';

// The whole chapter, read through the YouVersion Platform SDK's BibleReader: chapter and
// version pickers plus font settings on the toolbar, licensed scripture in the body.
// Content display only — no YouVersion sign-in and no auth-gated features (highlights,
// notes) are enabled.
//
// Without props this is the /BibleReferences/BibleReader default (John 14, NIV, sample
// tags); the /BibleReferences/:reference route supplies a parsed bible.com-style chapter
// reference instead (JHN.3, GEN.1.NIV, ...).
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
    const isDefaultChapter = book === 'JHN' && chapter === '14';

    useEffect(() => {
        document.title = isDefaultChapter
            ? `${sampleScripture.chapterReference} — Glory 2 Him`
            : `${book}.${chapter} — Glory 2 Him`;
    }, [book, chapter, isDefaultChapter]);

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-10">
                        {!isLoading && (
                            isAvailable
                                ? (
                                    <YouVersionBibleReader.Root
                                        key={`${book}.${chapter}-${versionId}`}
                                        defaultBook={book}
                                        defaultChapter={chapter}
                                        defaultVersionId={versionId}>
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
