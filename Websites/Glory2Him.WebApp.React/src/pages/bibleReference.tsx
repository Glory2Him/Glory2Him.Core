import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { BibleCard } from '@youversion/platform-react-ui';
import * as sampleScripture from '../data/sampleScripture';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';

// A single verse (or verse range) on its own — one narrow column, no sidebar, nothing between
// the reader and the words. The full chapter is one link away.
//
// The verse text comes from the YouVersion Platform SDK's BibleCard (licensed scripture, with
// its own title and version picker). Without props this is the /BibleReferences default
// (John 14:6, NIV, sample tags); the /BibleReferences/:reference route supplies a parsed
// bible.com-style reference instead (JHN.3.16, JHN.3.16-17, ...). When no app key is
// configured, the card gives way to an inline "unavailable" message rather than crashing.
type BibleReferenceParameters = {
    reference?: string,
    versionId?: number,
    chapterHref?: string
}

export function BibleReference({
    reference = 'JHN.14.6',
    versionId = youVersionVersions.niv,
    chapterHref = '/BibleReferences/JHN.14.NIV',
}: BibleReferenceParameters) {
    const { isLoading, isAvailable } = useYouVersionAvailability();
    const isDefaultReference = reference === 'JHN.14.6';

    useEffect(() => {
        document.title = isDefaultReference
            ? `${sampleScripture.reference} — Glory 2 Him`
            : `${reference} — Glory 2 Him`;
    }, [reference, isDefaultReference]);

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-7">
                        {/* No page heading: the card carries its own "JOHN 14:6 NIV" title, and the
                            version-picker button lets the reader switch translations. */}
                        {!isLoading && (
                            isAvailable
                                ? <BibleCard
                                    key={`${reference}-${versionId}`}
                                    reference={reference}
                                    defaultVersionId={versionId}
                                    showVersionPicker />
                                : <YouVersionUnavailableMessage />
                        )}

                        <div className="text-center my-4">
                            <Link to={chapterHref} className="btn-link">
                                Show Full Chapter
                            </Link>
                        </div>

                        {/* The tag row belongs to the curated John 14:6 page; an arbitrary
                            deep-linked reference has no editorial tags to show. */}
                        {isDefaultReference && (
                            <>
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
