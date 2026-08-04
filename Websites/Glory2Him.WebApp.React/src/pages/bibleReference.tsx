import { useEffect } from 'react';
import { Link } from 'react-router-dom';
import * as sampleScripture from '../data/sampleScripture';

// A single verse on its own — one narrow column, no sidebar, nothing between the reader and the
// words. The full chapter is one link away.
//
// The passage still comes from sampleScripture, so every reference on the site lands on the same
// verse for now. When references carry their own text, this page takes the reference as a route
// parameter and the markup stays as it is.
export function BibleReference() {
    useEffect(() => {
        document.title = `${sampleScripture.reference} — Glory 2 Him`;
    }, []);

    return (
        <section className="py-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-lg-7">
                        <h1 className="h2 mb-3">{sampleScripture.reference}</h1>

                        <p className="lead">{sampleScripture.singleVerseText}</p>

                        <div className="text-center my-4">
                            <Link to="/BibleReferences/Full-Chapter" className="btn-link">
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
