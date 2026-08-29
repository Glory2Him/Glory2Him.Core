import { useMemo, useState } from 'react';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';
import { FontAwesomeStyle, fontAwesomeIcons } from './fontAwesomeCatalogue';

// Font Awesome is the second of the two icon fonts index.html loads for the whole site:
//   /assets/vendor/font-awesome/css/all.min.css        -> Font Awesome (fas / far / fab)
//   /assets/vendor/bootstrap-icons/bootstrap-icons.css -> Bootstrap Icons (see bootstrapIconsDoc)
// The bundle is Font Awesome Free 5.15.1, which is why every example here uses the v5 style
// classes. fontAwesomeCatalogue.ts lists which of the three fonts carries each icon.

const STYLE_SECTIONS: ReadonlyArray<{ style: FontAwesomeStyle; title: string; lead: string }> = [
    {
        style: 'fas',
        title: 'Solid',
        lead: 'The default weight, and the only style most icons ship in.'
    },
    {
        style: 'far',
        title: 'Regular',
        lead: 'The outlined cut. Free Font Awesome only draws a small subset this way.'
    },
    {
        style: 'fab',
        title: 'Brands',
        lead: 'Logos. These exist only as brands — there is no solid or regular version.'
    }
];

const fontAwesomeUsageSample = `
// Font Awesome ships three styles, selected by the class that precedes the icon name.
// This site bundles Free 5.15.1, so the v5 spellings apply — fas / far / fab. The v6
// "fa-solid" / "fa-regular" / "fa-brands" classes are not in this stylesheet and render nothing.
<i className="fas fa-heart"></i>
<i className="far fa-heart"></i>
<i className="fab fa-github"></i>

// Like any icon font, it is sized and coloured as text.
<i className="fas fa-heart fs-1 text-danger"></i>

// react-fontawesome (already a dependency) is the alternative when the icon needs to be
// a React element rather than a bare <i>, e.g. to pass to a component prop.
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faHeart } from '@fortawesome/free-solid-svg-icons';
<FontAwesomeIcon icon={faHeart} size="2x" />
`;

export const FontAwesomeDoc = () => {
    useDocumentTitle('Font Awesome — Glory 2 Him');

    const [search, setSearch] = useState('');

    // One section per style, each holding every icon that style can draw — you cannot search
    // for an icon whose name you do not already know, so browsing has to work with an empty
    // search box. ~1,600 <i> elements of one font is a cheap grid, no virtualisation needed.
    const sections = useMemo(() => {
        const term = search.trim().toLowerCase();

        return STYLE_SECTIONS.map((section) => ({
            ...section,
            names: fontAwesomeIcons
                .filter((icon) =>
                    icon.styles.includes(section.style) && icon.name.includes(term))
                .map((icon) => icon.name)
        }));
    }, [search]);

    const matchCount = sections.reduce((total, section) => total + section.names.length, 0);
    const totalCount = fontAwesomeIcons.reduce((total, icon) => total + icon.styles.length, 0);

    return (
        <ComponentDoc
            name="Font Awesome"
            filePath="src/pages/samplePages/icons/fontAwesomeDoc.tsx"
            sectionTitle="Icons"
            summary={
                <>
                    <strong>Font Awesome Free 5.15.1</strong> is loaded globally in{' '}
                    <code>index.html</code>, in all three of its styles —{' '}
                    <code>fas</code> (solid), <code>far</code> (regular) and <code>fab</code>{' '}
                    (brands). It is a font, not an image set — apply the classes to any{' '}
                    <code>&lt;i&gt;</code> and size or colour it like text.
                </>
            }>

            <DocSection
                title="Catalogue"
                lead={`Every icon in the bundle — ${fontAwesomeIcons.length} names across ${totalCount} name-and-style combinations, grouped by style. An icon that ships in two styles appears under both. Search narrows every group at once.`}>
                <LiveDemo>
                    <input
                        type="search"
                        className="form-control mb-3"
                        placeholder="Search icon names, e.g. &quot;heart&quot;"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)} />

                    <p className="small text-body-secondary">
                        Showing {matchCount} of {totalCount} icons.
                    </p>

                    {sections.map((section) => (
                        <section key={section.style} className="mb-4">
                            <h3 className="h6 text-body-secondary border-bottom pb-2 mb-2">
                                {section.title}{' '}
                                <code className="fw-normal">{section.style}</code>{' '}
                                <span className="fw-normal">({section.names.length})</span>
                            </h3>
                            <p className="small text-body-secondary">{section.lead}</p>

                            <div className="d-flex flex-wrap gap-3">
                                {section.names.map((name) => (
                                    <div
                                        key={name}
                                        className="text-center border rounded p-2"
                                        style={{ width: '6.5rem' }}
                                        title={`${section.style} fa-${name}`}>
                                        <i className={`${section.style} fa-${name} fs-3`}></i>
                                        <div className="small text-body-secondary text-truncate">
                                            {name}
                                        </div>
                                    </div>
                                ))}
                            </div>

                            {section.names.length === 0 && (
                                <p className="text-body-secondary small mb-0">No matches.</p>
                            )}
                        </section>
                    ))}
                </LiveDemo>
                <CodeSample code={fontAwesomeUsageSample} />
            </DocSection>
        </ComponentDoc>
    );
};
