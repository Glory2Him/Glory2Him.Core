import { useEffect, useMemo, useState } from 'react';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// The template ships two icon fonts (see index.html), loaded once for the whole site:
//   /assets/vendor/font-awesome/css/all.min.css     -> Font Awesome (fa-*)
//   /assets/vendor/bootstrap-icons/bootstrap-icons.css -> Bootstrap Icons (bi-*)
// Every nav icon in navMenuProvider.ts uses bi-*, since the CoreUI cil-* set that ships
// with the demo shell is not loaded here.

const BOOTSTRAP_ICONS_MANIFEST = '/assets/vendor/bootstrap-icons/bootstrap-icons.css';
const BOOTSTRAP_ICONS_NAMES_URL = '/assets/vendor/bootstrap-icons/bootstrap-icons.json';
const MAX_UNFILTERED_RESULTS = 150;

const bootstrapUsageSample = `
// Bootstrap Icons is a font: any element can carry the class, sized and coloured like text.
<i className="bi bi-stars"></i>
<i className="bi bi-stars fs-1"></i>
<i className="bi bi-stars fs-1 text-warning"></i>

// It is what the sidebar uses for every nav entry — see navMenuProvider.ts.
{ title: "Lifestyle", icon: "bi-stars", href: "SamplePages/Lifestyle" }
`;

const fontAwesomeUsageSample = `
// Font Awesome ships three styles, selected by the class that precedes the icon name.
<i className="fa-solid fa-heart"></i>
<i className="fa-regular fa-heart"></i>
<i className="fa-brands fa-github"></i>

// react-fontawesome (already a dependency) is the alternative when the icon needs to be
// a React element rather than a bare <i>, e.g. to pass to a component prop.
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faHeart } from '@fortawesome/free-solid-svg-icons';
<FontAwesomeIcon icon={faHeart} size="2x" />
`;

// A representative slice, not the full catalogue — Font Awesome Free ships well over a
// thousand icons across three styles, and only free-solid-svg-icons is installed as a
// package. The full browsable set is at fontawesome.com/search.
const fontAwesomeSample: ReadonlyArray<[string, string]> = [
    ['fa-solid fa-heart', 'heart'], ['fa-solid fa-star', 'star'],
    ['fa-solid fa-house', 'house'], ['fa-solid fa-user', 'user'],
    ['fa-solid fa-envelope', 'envelope'], ['fa-solid fa-magnifying-glass', 'search'],
    ['fa-solid fa-book-bible', 'bible'], ['fa-solid fa-church', 'church'],
    ['fa-solid fa-calendar-days', 'calendar'], ['fa-solid fa-bell', 'bell'],
    ['fa-solid fa-gear', 'gear'], ['fa-solid fa-circle-check', 'check'],
    ['fa-regular fa-heart', 'heart (regular)'], ['fa-regular fa-star', 'star (regular)'],
    ['fa-regular fa-envelope', 'envelope (regular)'], ['fa-regular fa-calendar', 'calendar (regular)'],
    ['fa-brands fa-facebook', 'facebook'], ['fa-brands fa-instagram', 'instagram'],
    ['fa-brands fa-youtube', 'youtube'], ['fa-brands fa-github', 'github'],
];

export const IconsDoc = () => {
    useDocumentTitle('Icons — Glory 2 Him');

    const [bootstrapIconNames, setBootstrapIconNames] = useState<ReadonlyArray<string>>([]);
    const [loadError, setLoadError] = useState(false);
    const [search, setSearch] = useState('');

    useEffect(() => {
        let cancelled = false;

        fetch(BOOTSTRAP_ICONS_NAMES_URL)
            .then((response) => response.json())
            .then((manifest: Record<string, number>) => {
                if (!cancelled) {
                    setBootstrapIconNames(Object.keys(manifest).sort());
                }
            })
            .catch(() => {
                if (!cancelled) {
                    setLoadError(true);
                }
            });

        return () => { cancelled = true; };
    }, []);

    const filteredNames = useMemo(
        () => bootstrapIconNames.filter((name) => name.includes(search.trim().toLowerCase())),
        [bootstrapIconNames, search]);

    const visibleNames = search.trim() === ''
        ? filteredNames.slice(0, MAX_UNFILTERED_RESULTS)
        : filteredNames;

    return (
        <ComponentDoc
            name="Icons"
            filePath="src/pages/samplePages/icons/iconsDoc.tsx"
            sectionTitle="Icons"
            summary={
                <>
                    Two icon fonts ship with the site, both loaded globally in{' '}
                    <code>index.html</code>: <strong>Bootstrap Icons</strong> (<code>bi-*</code>,
                    used throughout the admin sidebar) and <strong>Font Awesome</strong>{' '}
                    (<code>fa-solid</code> / <code>fa-regular</code> / <code>fa-brands</code>).
                    Both are icon fonts — apply the class to any <code>&lt;i&gt;</code> and size
                    or colour it like text.
                </>
            }>

            <DocSection
                title="Bootstrap Icons"
                lead={
                    <>
                        The full set — {bootstrapIconNames.length || '…'} icons, read live from{' '}
                        <code>{BOOTSTRAP_ICONS_MANIFEST}</code>. Search narrows the grid; without a
                        search term only the first {MAX_UNFILTERED_RESULTS} are shown.
                    </>
                }>
                <LiveDemo>
                    <input
                        type="search"
                        className="form-control mb-3"
                        placeholder="Search icon names, e.g. &quot;star&quot;"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)} />

                    {loadError && (
                        <p className="text-danger small">
                            Could not load the Bootstrap Icons manifest.
                        </p>
                    )}

                    {!loadError && (
                        <div className="d-flex flex-wrap gap-3">
                            {visibleNames.map((name) => (
                                <div
                                    key={name}
                                    className="text-center border rounded p-2"
                                    style={{ width: '6.5rem' }}
                                    title={`bi bi-${name}`}>
                                    <i className={`bi bi-${name} fs-3`}></i>
                                    <div className="small text-body-secondary text-truncate">
                                        {name}
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {!loadError && filteredNames.length > visibleNames.length && (
                        <p className="small text-body-secondary mt-2 mb-0">
                            {filteredNames.length - visibleNames.length} more — refine the search
                            to see them.
                        </p>
                    )}
                </LiveDemo>
                <CodeSample code={bootstrapUsageSample} />
            </DocSection>

            <DocSection
                title="Font Awesome"
                lead={
                    <>
                        A representative sample across the three styles — the free set has well
                        over a thousand icons. Browse the rest at{' '}
                        <a href="https://fontawesome.com/search" target="_blank" rel="noreferrer">
                            fontawesome.com/search
                        </a>.
                    </>
                }>
                <LiveDemo>
                    <div className="d-flex flex-wrap gap-3">
                        {fontAwesomeSample.map(([cssClass, label]) => (
                            <div
                                key={cssClass}
                                className="text-center border rounded p-2"
                                style={{ width: '6.5rem' }}
                                title={cssClass}>
                                <i className={`${cssClass} fs-3`}></i>
                                <div className="small text-body-secondary text-truncate">
                                    {label}
                                </div>
                            </div>
                        ))}
                    </div>
                </LiveDemo>
                <CodeSample code={fontAwesomeUsageSample} />
            </DocSection>
        </ComponentDoc>
    );
};
