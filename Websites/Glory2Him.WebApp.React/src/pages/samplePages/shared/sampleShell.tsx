import { ReactNode } from 'react';
import { Link } from 'react-router-dom';

// Shared chrome for every sample page: a slim strip naming the demo and linking back to the
// sample index, then the demo itself. The strip is deliberately plain so it never competes with
// the layout being demonstrated. Ported from the Blazor SampleShellComponent.

export interface SampleShellProps {
    title: string;

    // The Blogzine file this layout was ported from, shown so the demo can be traced back to
    // its source when comparing against the original template.
    sourceFile?: string;
    children?: ReactNode;
}

export function SampleShell({ title, sourceFile, children }: SampleShellProps) {
    return (
        <>
            <div className="bg-body-tertiary border-bottom py-2">
                <div className="container d-flex flex-wrap align-items-center justify-content-between gap-2">
                    <Link className="btn btn-sm btn-outline-secondary" to="/SamplePages">
                        <i className="bi bi-arrow-left me-1"></i>Back to Sample Pages
                    </Link>

                    <div className="d-flex align-items-center gap-2 small text-body-secondary">
                        <span className="badge text-bg-secondary">Sample</span>
                        <span className="fw-semibold text-body">{title}</span>
                        {sourceFile != null && sourceFile.trim() !== '' && (
                            <span className="d-none d-sm-inline">— Blogzine <code>{sourceFile}</code></span>
                        )}
                    </div>
                </div>
            </div>

            {children}
        </>
    );
}
