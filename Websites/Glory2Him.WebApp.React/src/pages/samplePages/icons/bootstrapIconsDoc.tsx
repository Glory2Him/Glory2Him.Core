import { useEffect, useMemo, useState } from 'react';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// Bootstrap Icons is one of the two icon fonts index.html loads for the whole site:
//   /assets/vendor/bootstrap-icons/bootstrap-icons.css -> Bootstrap Icons (bi-*)
//   /assets/vendor/font-awesome/css/all.min.css        -> Font Awesome (see fontAwesomeDoc)
// Every nav icon in navMenuProvider.ts uses bi-*, since the CoreUI cil-* set that ships
// with the demo shell is not loaded here.

const BOOTSTRAP_ICONS_MANIFEST = '/assets/vendor/bootstrap-icons/bootstrap-icons.css';
const BOOTSTRAP_ICONS_NAMES_URL = '/assets/vendor/bootstrap-icons/bootstrap-icons.json';

const bootstrapUsageSample = `
// Bootstrap Icons is a font: any element can carry the class, sized and coloured like text.
<i className="bi bi-stars"></i>
<i className="bi bi-stars fs-1"></i>
<i className="bi bi-stars fs-1 text-warning"></i>

// It is what the sidebar uses for every nav entry — see navMenuProvider.ts.
{ title: "Lifestyle", icon: "bi-stars", href: "SamplePages/Lifestyle" }
`;

export const BootstrapIconsDoc = () => {
    useDocumentTitle('Bootstrap Icons — Glory 2 Him');

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

    // The whole set renders at once: you cannot search for an icon whose name you do not
    // already know, so browsing has to work with an empty search box. It is ~2,000 <i>
    // elements of one font — a cheap grid, no virtualisation needed.
    const filteredNames = useMemo(
        () => bootstrapIconNames.filter((name) => name.includes(search.trim().toLowerCase())),
        [bootstrapIconNames, search]);

    return (
        <ComponentDoc
            name="Bootstrap Icons"
            filePath="src/pages/samplePages/icons/bootstrapIconsDoc.tsx"
            sectionTitle="Icons"
            summary={
                <>
                    <strong>Bootstrap Icons</strong> (<code>bi-*</code>) is the icon font used
                    throughout the admin sidebar, loaded globally in <code>index.html</code>.
                    It is a font, not an image set — apply the class to any{' '}
                    <code>&lt;i&gt;</code> and size or colour it like text.
                </>
            }>

            <DocSection
                title="Catalogue"
                lead={
                    <>
                        Every icon in the set — {bootstrapIconNames.length || '…'} of them, read
                        live from <code>{BOOTSTRAP_ICONS_MANIFEST}</code>. Search narrows the
                        grid by name.
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
                        <>
                            <p className="small text-body-secondary">
                                Showing {filteredNames.length} of {bootstrapIconNames.length}{' '}
                                icons.
                            </p>

                            <div className="d-flex flex-wrap gap-3">
                                {filteredNames.map((name) => (
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

                            {bootstrapIconNames.length > 0 && filteredNames.length === 0 && (
                                <p className="text-body-secondary small mb-0">No matches.</p>
                            )}
                        </>
                    )}
                </LiveDemo>
                <CodeSample code={bootstrapUsageSample} />
            </DocSection>
        </ComponentDoc>
    );
};
