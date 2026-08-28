import { ReactNode } from 'react';
import { Breadcrumb } from '../../../../components/coreUI/breadcrumb';
import { Card } from '../../../../components/coreUI/card';
import { BreadcrumbItem } from '../../../../models/coreUI/breadcrumbItem';

// Shared chrome for the component reference pages. These are documentation rather than layout
// demos, so they sit in the admin shell with the sidebar still on the left — you read across
// several components in one sitting — instead of full width like the Blogzine ports.

export interface ComponentDocProps {
    name: string;
    filePath: string;
    summary: ReactNode;
    children?: ReactNode;
}

export function ComponentDoc({ name, filePath, summary, children }: ComponentDocProps) {
    const crumbs: ReadonlyArray<BreadcrumbItem> = [
        { title: 'Sample Pages', href: '/SamplePages' },
        { title: 'Components', href: '/SamplePages' },
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

export function LiveDemo({ title = 'Live', children }: LiveDemoProps) {
    return (
        <Card
            cssClass="mb-3"
            headerContent={
                <span className="d-flex align-items-center">
                    <i className="bi bi-play-circle me-2"></i>{title}
                </span>
            }>
            {children}
        </Card>
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
            <table className="table table-sm align-middle">
                <thead>
                    <tr>
                        <th scope="col">Prop</th>
                        <th scope="col">Type</th>
                        <th scope="col">Default</th>
                        <th scope="col">Description</th>
                    </tr>
                </thead>
                <tbody>
                    {rows.map((row) => (
                        <tr key={row.name}>
                            <td className="text-nowrap"><code>{row.name}</code></td>
                            <td className="text-nowrap"><small>{row.type}</small></td>
                            <td className="text-nowrap">
                                <small>{row.defaultValue ?? '—'}</small>
                            </td>
                            <td>{row.description}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
