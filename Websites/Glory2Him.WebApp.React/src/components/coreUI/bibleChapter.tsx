import { Fragment } from 'react';
import { BibleSection } from '../../models/coreUI/bibleSection';
import './coreUI.css';

// A chapter of scripture: the reference as the heading, share links alongside, then each section
// under its own sub-heading with the verse numbers set small and raised inline against flowing
// text. The caller supplies the text.
export interface BibleChapterProps {
    reference: string;
    sections?: ReadonlyArray<BibleSection>;
    showShareLinks?: boolean;
}

export function BibleChapter({ reference, sections = [], showShareLinks = true }: BibleChapterProps) {
    return (
        <>
            <div className="d-flex justify-content-between align-items-start mb-3">
                <h2 className="mb-0">{reference}</h2>

                {showShareLinks && (
                    <ul className="nav text-white-force flex-nowrap">
                        <li className="nav-item">
                            <a className="nav-link icon-sm rounded-circle me-2 p-0 bg-facebook" href="#"
                                aria-label="Share on Facebook">
                                <i className="fab fa-facebook-square align-middle"></i>
                            </a>
                        </li>
                        <li className="nav-item">
                            <a className="nav-link icon-sm rounded-circle p-0 bg-twitter" href="#"
                                aria-label="Share on Twitter">
                                <i className="fab fa-twitter-square align-middle"></i>
                            </a>
                        </li>
                    </ul>
                )}
            </div>

            {sections.map((section, sectionIndex) => (
                <Fragment key={sectionIndex}>
                    {section.heading != null && section.heading.trim().length > 0 && (
                        <h3 className="h5 mt-4 mb-2">{section.heading}</h3>
                    )}

                    <p className="g2h-chapter-text">
                        {section.verses.map((verse) => (
                            <Fragment key={verse.number}>
                                <sup className="g2h-verse-number">{verse.number}</sup>
                                {/* Trailing space keeps verses from running together when they
                                    flow as one block. */}
                                <span className="g2h-verse">{verse.text}</span>{' '}
                            </Fragment>
                        ))}
                    </p>
                </Fragment>
            ))}
        </>
    );
}
