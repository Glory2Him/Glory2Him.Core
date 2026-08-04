import { Link } from 'react-router-dom';
import { reference, singleVerseText, tags } from '../../../data/sampleScripture';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// A single verse on its own, laid out like Post Single Minimal — one narrow column, no
// sidebar, nothing between the reader and the words. The full chapter is one link away.
export const BibleReferencePartialSample = () => {
    useDocumentTitle(`${reference} — Sample — Glory 2 Him`);

    return (
        <SampleShell title="Bible Reference - Partial" sourceFile="post-single-3.html">
            <section className="py-5">
                <div className="container">
                    <div className="row justify-content-center">
                        <div className="col-lg-7">
                            <h1 className="h2 mb-3">{reference}</h1>

                            <p className="lead">{singleVerseText}</p>

                            <div className="text-center my-4">
                                <Link
                                    to="/SamplePages/BibleReferences/BibleReference-Full-Chapter"
                                    className="btn-link">
                                    Show Full Chapter
                                </Link>
                            </div>

                            <hr className="my-4" />

                            <div className="d-flex flex-wrap align-items-center gap-2">
                                <span className="fw-bold me-1">Tags:</span>
                                {tags.map((tag) => (
                                    <Link
                                        key={tag}
                                        to={`/Tag?name=${encodeURIComponent(tag)}`}
                                        className="btn btn-sm btn-outline-secondary mb-0">
                                        {tag}
                                    </Link>
                                ))}
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </SampleShell>
    );
};
