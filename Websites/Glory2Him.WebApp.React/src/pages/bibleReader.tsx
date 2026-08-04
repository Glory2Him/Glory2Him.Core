import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { BibleReader as YouVersionBibleReader } from '@youversion/platform-react-ui';
import * as sampleScripture from '../data/sampleScripture';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';

// The whole chapter, read through the YouVersion Platform SDK's BibleReader: chapter and
// version pickers plus font settings on the toolbar, licensed scripture in the body. This
// replaces the old sample-text ScriptureReader; the page chrome (links, tags, column width)
// stays as it was. Content display only — no YouVersion sign-in and no auth-gated features
// (highlights, notes) are enabled.
//
// Wider than the single-verse page (col-lg-10, not 7) so the reader has room to breathe.
export function BibleReader() {
    const { isLoading, isAvailable } = useYouVersionAvailability();

    useEffect(() => {
        document.title = `${sampleScripture.chapterReference} — Glory 2 Him`;
    }, []);

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-10">
                        {!isLoading && (
                            isAvailable
                                ? (
                                    <YouVersionBibleReader.Root
                                        defaultBook="JHN"
                                        defaultChapter="14"
                                        defaultVersionId={youVersionVersions.niv}>
                                        <YouVersionBibleReader.Toolbar />
                                        <YouVersionBibleReader.Content />
                                    </YouVersionBibleReader.Root>
                                )
                                : <YouVersionUnavailableMessage />
                        )}

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
                    </div>
                </div>
            </div>
        </section>
    );
}
