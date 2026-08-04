import { Link } from 'react-router-dom';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';

// Reusable breadcrumb trail.
export interface BreadcrumbProps {
    items?: ReadonlyArray<BreadcrumbItem>;
}

export function Breadcrumb({ items = [] }: BreadcrumbProps) {
    return (
        <nav aria-label="breadcrumb">
            <ol className="breadcrumb mb-0">
                <li className="breadcrumb-item"><Link to="/">Home</Link></li>
                {items.map((item) =>
                    item.isActive === true ? (
                        <li key={item.title} className="breadcrumb-item active" aria-current="page">
                            {item.title}
                        </li>
                    ) : (
                        <li key={item.title} className="breadcrumb-item">
                            <Link to={item.href ?? '#'}>{item.title}</Link>
                        </li>
                    ))}
            </ol>
        </nav>
    );
}
