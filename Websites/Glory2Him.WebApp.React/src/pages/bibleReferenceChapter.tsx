import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import { ScriptureReader } from '../components/coreUI/scriptureReader';
import * as sampleScripture from '../data/sampleScripture';

// The whole chapter, read through the scripture reader: chapter and translation on a bar above the
// text, a toggle for two translations side by side, and a step button either side for the
// neighbouring chapters.
//
// Wider than the single-verse page (col-lg-10, not 7) because parallel mode puts two reading
// columns between the step buttons.
export function BibleReferenceChapter() {
    useEffect(() => {
        document.title = `${sampleScripture.chapterReference} — Glory 2 Him`;
    }, []);

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-10">
                        <ScriptureReader
                            book={sampleScripture.chapterBook}
                            chapterCount={sampleScripture.chapterCount}
                            chapter={sampleScripture.chapterNumber}
                            translations={sampleScripture.translations}
                            sectionsFor={sampleScripture.chapterFor} />

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
