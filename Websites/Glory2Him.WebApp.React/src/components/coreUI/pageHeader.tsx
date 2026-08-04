import { Link } from 'react-router-dom';

// Reusable interior-page banner: title + breadcrumb.
export interface PageHeaderProps {
    title: string;
    parentTitle?: string;
    parentHref?: string;
}

export function PageHeader({ title, parentTitle, parentHref = '#' }: PageHeaderProps) {
    return (
        <section className="py-4 bg-light">
            <div className="container">
                <div className="row">
                    <div className="col-12 text-center">
                        <h1 className="mb-3">{title}</h1>
                        <nav aria-label="breadcrumb">
                            <ol className="breadcrumb justify-content-center mb-0">
                                <li className="breadcrumb-item"><Link to="/">Home</Link></li>
                                {parentTitle != null && parentTitle.trim().length > 0 && (
                                    <li className="breadcrumb-item"><Link to={parentHref}>{parentTitle}</Link></li>
                                )}
                                <li className="breadcrumb-item active" aria-current="page">{title}</li>
                            </ol>
                        </nav>
                    </div>
                </div>
            </div>
        </section>
    );
}
