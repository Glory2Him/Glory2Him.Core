import { Link } from 'react-router-dom';
import { ScriptureReader } from '../../../components/coreUI/scriptureReader';
import {
    chapterBook,
    chapterCount,
    chapterFor,
    chapterNumber,
    chapterReference,
    reference,
    tags,
    translations,
} from '../../../data/sampleScripture';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';

// The whole chapter, read through the scripture reader: chapter and translation on a bar
// above the text, a toggle for two translations side by side, and a step button either side
// for the neighbouring chapters.
//
// Wider than the single-verse sample (col-lg-10, not 7) because parallel mode puts two
// reading columns between the step buttons.
export const BibleReferenceFullChapterSample = () => {
    useDocumentTitle(`${chapterReference} — Sample — Glory 2 Him`);

    return (
        <SampleShell title="Bible Reference - Full Chapter" sourceFile="post-single-3.html">
            <section className="py-5">
                <div className="container">
                    <div className="row justify-content-center">
                        <div className="col-lg-10">
                            <ScriptureReader
                                book={chapterBook}
                                chapterCount={chapterCount}
                                chapter={chapterNumber}
                                translations={translations}
                                sectionsFor={chapterFor} />

                            <div className="text-center my-4">
                                <Link
                                    to="/SamplePages/BibleReferences/BibleReference-Single-verse"
                                    className="btn-link">
                                    Show {reference} on its own
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
