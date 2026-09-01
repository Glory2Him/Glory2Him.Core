import { ReactNode } from 'react';
import { Breadcrumb } from '../../../../components/coreUI/breadcrumb';
import { BreadcrumbItem } from '../../../../models/coreUI/breadcrumbItem';

// Shared chrome for the component reference pages. These are documentation rather than layout
// demos, so they sit in the admin shell with the sidebar still on the left — you read across
// several components in one sitting — instead of full width like the Blogzine ports.

export interface ComponentDocProps {
    name: string;
    filePath: string;
    summary: ReactNode;
    children?: ReactNode;
    sectionTitle?: string;
    sectionHref?: string;
}

export function ComponentDoc({
    name, filePath, summary, children,
    sectionTitle = 'Components', sectionHref = '/SamplePages'
}: ComponentDocProps) {
    const crumbs: ReadonlyArray<BreadcrumbItem> = [
        { title: 'Sample Pages', href: '/SamplePages' },
        { title: sectionTitle, href: sectionHref },
        { title: name, isActive: true }
    ];

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">{name}</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <p className="text-body-secondary mb-1">{summary}</p>
            <p className="small text-body-secondary">
                <i className="bi bi-file-earmark-code me-1"></i><code>{filePath}</code>
            </p>

            {children}
        </>
    );
}

// A section of the page: a heading, optional lead-in, and whatever it documents.
export interface DocSectionProps {
    title: string;
    lead?: ReactNode;
    children?: ReactNode;
}

// A <div> rather than a <section> on purpose: the Blogzine theme gives every bare section
// 3.5rem of top and 2.8rem of bottom padding, which stacked into roughly 100px of dead space
// between each heading here. The heading levels carry the document structure instead.
export function DocSection({ title, lead, children }: DocSectionProps) {
    return (
        <div className="mb-4">
            <h2 className="h5 mb-2">{title}</h2>
            {lead != null && <p className="text-body-secondary">{lead}</p>}
            {children}
        </div>
    );
}

// Code is rendered from a plain string rather than as JSX so it can be copied verbatim.
// overflow-auto keeps a long line inside its own scroller instead of widening the page.
export interface CodeSampleProps {
    code: string;
    caption?: string;
}

export function CodeSample({ code, caption }: CodeSampleProps) {
    return (
        <figure className="mb-3">
            <pre className="bg-body-tertiary border rounded p-3 mb-0 overflow-auto">
                <code>{code.trim()}</code>
            </pre>
            {caption != null && (
                <figcaption className="small text-body-secondary mt-1">{caption}</figcaption>
            )}
        </figure>
    );
}

// The component running for real, boxed so it is obvious where the demo starts and stops.
export interface LiveDemoProps {
    title?: string;
    children?: ReactNode;
}

// NOT the shared Card: the theme pads a card body generously, and a demo living inside one
// loses real estate on both sides — the component under demonstration should get the width
// the page has.
export function LiveDemo({ title = 'Live', children }: LiveDemoProps) {
    return (
        <div className="mb-3">
            <p className="small fw-semibold text-body-secondary mb-2">
                <i className="bi bi-play-circle me-2"></i>{title}
            </p>

            {children}
        </div>
    );
}

export interface ComponentPropRow {
    name: string;
    type: string;
    defaultValue?: string;
    description: ReactNode;
}

export interface PropsTableProps {
    rows: ReadonlyArray<ComponentPropRow>;
}

export function PropsTable({ rows }: PropsTableProps) {
    return (
        <div className="table-responsive">
            {/* FIXED layout, or the browser hands the width to whichever column carries
                the longest unbreakable token and the description ends up in a sliver —
                tall rows and a table wider than the page. The description keeps the
                width; names and types may break mid-token when they must. */}
            <table
                className="table table-sm align-middle"
                style={{ tableLayout: 'fixed' }}>
                <thead>
                    <tr>
                        <th scope="col" style={{ width: '22%' }}>Prop</th>
                        <th scope="col" style={{ width: '20%' }}>Type</th>
                        <th scope="col" style={{ width: '9%' }}>Default</th>
                        <th scope="col">Description</th>
                    </tr>
                </thead>
                <tbody>
                    {/* Nothing here refuses to wrap: a nowrap name or type cell forces
                        the table wider than the page, pushes the description off screen
                        and stretches every row — the exact opposite of a reference table. */}
                    {rows.map((row) => (
                        <tr key={row.name}>
                            <td className="text-break"><code>{row.name}</code></td>
                            <td className="text-break"><small>{row.type}</small></td>
                            <td><small>{row.defaultValue ?? '—'}</small></td>
                            <td>{row.description}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

// A LIVE CONTROL BOARD for a demo: one switch per boolean prop, restyled the way the admin
// Features table reads — flip a switch and the demo beside it re-renders with the prop
// changed, so what a prop DOES is seen rather than read about.
export interface DemoToggle {
    name: string;
    label: string;
    value: boolean;
    onChange: (value: boolean) => void;
}

export interface DemoControlsProps {
    title?: string;
    toggles: ReadonlyArray<DemoToggle>;
}

export function DemoControls({ title = 'Controls', toggles }: DemoControlsProps) {
    return (
        <div className="border rounded-3 p-3 mb-3">
            <p className="small text-uppercase fw-bold text-body-secondary mb-2">{title}</p>

            <div className="row g-2">
                {toggles.map((toggle) => (
                    <div className="col-12 col-md-6 col-xl-4" key={toggle.name}>
                        <div className="form-check form-switch mb-0">
                            <input
                                className="form-check-input"
                                type="checkbox"
                                role="switch"
                                id={`demo-toggle-${toggle.name}`}
                                checked={toggle.value}
                                onChange={(event) => toggle.onChange(event.target.checked)} />

                            <label
                                className="form-check-label"
                                htmlFor={`demo-toggle-${toggle.name}`}>
                                {toggle.label}
                            </label>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

// A LIVE RADIO BOARD, the DemoControls' sibling for a prop that is one choice from a small
// closed set rather than a boolean — pick an option and the demo re-renders under it.
export interface DemoRadioOption {
    key: string;
    label: string;
}

export interface DemoRadioGroupProps {
    title: string;
    name: string;
    options: ReadonlyArray<DemoRadioOption>;
    selectedKey: string;
    onChange: (key: string) => void;
}

export function DemoRadioGroup({
    title,
    name,
    options,
    selectedKey,
    onChange
}: DemoRadioGroupProps) {
    return (
        <div className="border rounded-3 p-3 mb-3">
            <p className="small text-uppercase fw-bold text-body-secondary mb-2">{title}</p>

            <div className="row g-2">
                {options.map((option) => (
                    <div className="col-12 col-md-6 col-xl-4" key={option.key}>
                        <div className="form-check mb-0">
                            <input
                                className="form-check-input"
                                type="radio"
                                name={name}
                                id={`${name}-${option.key}`}
                                checked={selectedKey === option.key}
                                onChange={() => onChange(option.key)} />

                            <label
                                className="form-check-label"
                                htmlFor={`${name}-${option.key}`}>
                                {option.label}
                            </label>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

// A LIVE NUMBER BOX, the DemoControls' sibling for a numeric prop — type a value and the
// demo re-renders under it. An emptied or unparseable box falls back to the default rather
// than handing the component NaN.
export interface DemoNumberInputProps {
    label: string;
    name: string;
    value: number;
    defaultValue: number;
    onChange: (value: number) => void;
}

export function DemoNumberInput({
    label,
    name,
    value,
    defaultValue,
    onChange
}: DemoNumberInputProps) {
    return (
        <div className="border rounded-3 p-3 mb-3">
            <label
                className="small text-uppercase fw-bold text-body-secondary mb-2 d-block"
                htmlFor={`demo-number-${name}`}>
                {label}
            </label>

            <input
                className="form-control"
                style={{ maxWidth: '10rem' }}
                type="number"
                id={`demo-number-${name}`}
                value={value}
                min={1}
                onChange={(event) => {
                    const parsed = Number(event.target.value);

                    onChange(Number.isFinite(parsed) && parsed > 0
                        ? parsed
                        : defaultValue);
                }} />
        </div>
    );
}
