import { Link } from 'react-router-dom';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';

// Full-width photographic page banner with the title and a dotted breadcrumb sitting over a dark
// gradient (Blogzine "card-overlay-bottom h-300").
export interface HeroBannerProps {
    title: string;
    imageUrl?: string;

    // "Home" is always rendered first, so callers supply only the trail after it.
    crumbs?: ReadonlyArray<BreadcrumbItem>;
}

export function HeroBanner({
    title,
    imageUrl = 'assets/images/blog/16by9/big/01.jpg',
    crumbs = [],
}: HeroBannerProps) {
    return (
        <section className="pt-4">
            <div className="container">
                <div className="row">
                    <div className="col-12">
                        <div
                            className="card card-overlay-bottom h-300 overflow-hidden text-center"
                            style={{
                                backgroundImage: `url(${imageUrl})`,
                                backgroundPosition: 'center center',
                                backgroundSize: 'cover',
                            }}>
                            <div className="card-img-overlay d-flex align-items-center p-3 pb-4 px-sm-5">
                                <div className="col-12 mt-auto d-md-flex justify-content-between align-items-center">
                                    <h1 className="text-white display-5 mb-0">{title}</h1>

                                    {crumbs.length > 0 && (
                                        <nav className="d-flex justify-content-center" aria-label="breadcrumb">
                                            <ol className="breadcrumb breadcrumb-dots mb-0">
                                                <li className="breadcrumb-item">
                                                    <Link to="/"><i className="bi bi-house me-1"></i>Home</Link>
                                                </li>
                                                {crumbs.map((crumb) => (
                                                    <li
                                                        key={crumb.title}
                                                        className={`breadcrumb-item ${crumb.isActive === true ? 'active' : ''}`}>
                                                        {crumb.isActive === true
                                                            ? crumb.title
                                                            : <Link to={crumb.href ?? '#'}>{crumb.title}</Link>}
                                                    </li>
                                                ))}
                                            </ol>
                                        </nav>
                                    )}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
