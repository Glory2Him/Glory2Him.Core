import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { BibleCard } from '@youversion/platform-react-ui';
import * as sampleScripture from '../data/sampleScripture';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';

// A single verse on its own — one narrow column, no sidebar, nothing between the reader and the
// words. The full chapter is one link away.
//
// The verse text now comes from the YouVersion Platform SDK's BibleCard (licensed scripture,
// version picker included) instead of the placeholder sampleScripture text. The page chrome —
// title, links, tags — stays exactly as it was. When no app key is configured, the card gives
// way to an inline "unavailable" message rather than crashing.
export function BibleReference() {
    const { isLoading, isAvailable } = useYouVersionAvailability();

    useEffect(() => {
        document.title = `${sampleScripture.reference} — Glory 2 Him`;
    }, []);

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
                                    reference="JHN.14.6"
                                    defaultVersionId={youVersionVersions.niv}
                                    showVersionPicker />
                                : <YouVersionUnavailableMessage />
                        )}

                        <div className="text-center my-4">
                            <Link to="/BibleReferences/BibleReader" className="btn-link">
                                Show Full Chapter
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
                    </div>
                </div>
            </div>
        </section>
    );
}
