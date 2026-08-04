import { useEffect, useState } from 'react';
import { BibleSection } from '../../models/coreUI/bibleSection';
import { ScriptureTranslation } from '../../models/coreUI/scriptureTranslation';
import { BibleChapter } from './bibleChapter';
import './coreUI.css';

// A chapter reader in the manner of bible.com: a bar carrying the chapter and the translation, a
// toggle that splits the page into two translations side by side, and a chevron on each side to
// step to the neighbouring chapter.
//
// The caller hands over the chapter count, the translations, and a lookup for the text of any
// (chapter, translation) pair. The component keeps its own chapter state (seeded from the
// `chapter` prop, as the Blazor original did) and raises onChapterChange so a caller can put the
// chapter in the URL.
export interface ScriptureReaderProps {
    book: string;
    chapterCount?: number;
    chapter?: number;
    onChapterChange?: (chapter: number) => void;
    translations?: ReadonlyArray<ScriptureTranslation>;

    // Returns null when that chapter has no text in that translation, which the reader shows
    // as a plain note rather than an empty column.
    sectionsFor: (chapter: number, translationCode: string) => ReadonlyArray<BibleSection> | null;
}

export function ScriptureReader({
    book,
    chapterCount = 1,
    chapter: chapterProp = 1,
    onChapterChange,
    translations = [],
    sectionsFor,
}: ScriptureReaderProps) {
    const [chapter, setChapter] = useState(chapterProp);
    const [primaryCode, setPrimaryCode] = useState('');
    const [secondaryCode, setSecondaryCode] = useState('');
    const [isParallel, setIsParallel] = useState(false);

    // A caller that owns the chapter (e.g. via the URL) stays in charge: when the prop moves,
    // the local state follows it.
    useEffect(() => {
        setChapter(chapterProp);
    }, [chapterProp]);

    // Both sides start on the same translation, as bible.com does — the reader picks a second
    // one after splitting, rather than the split choosing one for them.
    const effectivePrimaryCode =
        translations.some((translation) => translation.code === primaryCode)
            ? primaryCode
            : translations[0]?.code ?? '';

    const effectiveSecondaryCode =
        translations.some((translation) => translation.code === secondaryCode)
            ? secondaryCode
            : effectivePrimaryCode;

    const translationName = (code: string) =>
        translations.find((translation) => translation.code === code)?.name ?? code;

    const previousTitle = chapter <= 1 ? 'No earlier chapter' : `${book} ${chapter - 1}`;
    const nextTitle = chapter >= chapterCount ? 'No later chapter' : `${book} ${chapter + 1}`;

    const goToChapter = (target: number) => {
        if (target < 1 || target > chapterCount || target === chapter) {
            return;
        }

        setChapter(target);
        onChapterChange?.(target);
    };

    const renderChapter = (translationCode: string) => {
        const sections = sectionsFor(chapter, translationCode);

        if (sections == null) {
            return (
                <>
                    <h2 className="mb-3">{book} {chapter}</h2>

                    <div className="alert alert-info mb-0" role="alert">
                        {book} {chapter} is not available in {translationName(translationCode)} yet.
                    </div>
                </>
            );
        }

        return (
            <BibleChapter
                reference={`${book} ${chapter}`}
                sections={sections}
                showShareLinks={false} />
        );
    };

    return (
        <>
            <div className="g2h-scripture-bar d-flex flex-wrap align-items-center gap-3 pb-3 mb-4 border-bottom">
                <select
                    className="form-select g2h-scripture-select"
                    value={chapter}
                    onChange={(event) => {
                        const parsed = Number.parseInt(event.target.value, 10);

                        if (!Number.isNaN(parsed)) {
                            goToChapter(parsed);
                        }
                    }}
                    aria-label="Chapter">
                    {Array.from({ length: chapterCount }, (_, index) => index + 1).map((number) => (
                        <option key={number} value={number}>{book} {number}</option>
                    ))}
                </select>

                <select
                    className="form-select g2h-scripture-select"
                    value={effectivePrimaryCode}
                    onChange={(event) => setPrimaryCode(event.target.value)}
                    aria-label="Translation">
                    {translations.map((translation) => (
                        <option key={translation.code} value={translation.code}>{translation.name}</option>
                    ))}
                </select>

                {isParallel && (
                    <select
                        className="form-select g2h-scripture-select"
                        value={effectiveSecondaryCode}
                        onChange={(event) => setSecondaryCode(event.target.value)}
                        aria-label="Second translation">
                        {translations.map((translation) => (
                            <option key={translation.code} value={translation.code}>{translation.name}</option>
                        ))}
                    </select>
                )}

                <button
                    type="button"
                    className="btn btn-link p-0 mb-0 d-flex align-items-center"
                    onClick={() => setIsParallel(!isParallel)}
                    aria-pressed={isParallel}>
                    <i className="bi bi-layout-split me-2"></i>{isParallel ? 'Exit Parallel Mode' : 'Parallel'}
                </button>
            </div>

            {/* The chevrons sit in the flow either side of the text rather than floating over
                it — the reading column is narrow enough here that an overlaid control would land
                on the words. */}
            <div className="d-flex align-items-center gap-2 gap-lg-3">
                <button
                    type="button"
                    className="btn btn-outline-secondary rounded-circle g2h-scripture-step mb-0"
                    onClick={() => goToChapter(chapter - 1)}
                    disabled={chapter <= 1}
                    title={previousTitle}
                    aria-label={previousTitle}>
                    <i className="bi bi-chevron-left"></i>
                </button>

                <div className="row g-4 flex-grow-1">
                    <div className={isParallel ? 'col-md-6' : 'col-12'}>
                        {renderChapter(effectivePrimaryCode)}
                    </div>

                    {isParallel && (
                        <div className="col-md-6">
                            {renderChapter(effectiveSecondaryCode)}
                        </div>
                    )}
                </div>

                <button
                    type="button"
                    className="btn btn-outline-secondary rounded-circle g2h-scripture-step mb-0"
                    onClick={() => goToChapter(chapter + 1)}
                    disabled={chapter >= chapterCount}
                    title={nextTitle}
                    aria-label={nextTitle}>
                    <i className="bi bi-chevron-right"></i>
                </button>
            </div>
        </>
    );
}
