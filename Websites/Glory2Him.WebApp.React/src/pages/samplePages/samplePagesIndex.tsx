import { Link } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Card } from '../../components/coreUI/card';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { hasChildren, NavItem } from '../../models/views/navigations/navItem';
import { navMenuProvider } from '../../services/views/navigations/navMenuProvider';
import { useDocumentTitle } from '../useDocumentTitle';

// Catalogue of every layout demo. Sits in the admin shell (SidebarLayout) rather than full
// width, so the sample pages' "Back to Sample Pages" button lands you back in the admin area
// with the navigation still on the left. The tree is read straight from navMenuProvider so
// this page and the sidebar can never drift apart.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Sample Pages', href: 'SamplePages', isActive: true },
];

// Post nests one level deeper (Post Grid), so flatten to the leaves — the catalogue lists
// every reachable demo rather than mirroring the sidebar's nesting.
const flatten = (item: NavItem): NavItem[] => {
    if (!hasChildren(item)) {
        return [item];
    }

    return (item.children ?? []).flatMap(flatten);
};

export const SamplePagesIndex = () => {
    useDocumentTitle('Sample Pages — Glory 2 Him');

    const groups = navMenuProvider.getSamplePagesSection().children ?? [];

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Sample Pages</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <div className="alert alert-info d-flex align-items-start" role="alert">
                <i className="bi bi-info-circle me-2 mt-1"></i>
                <div>
                    Layout demos ported from the Blogzine template, rendered full width inside the Glory 2 Him
                    header and footer. Use the <strong>Back to Sample Pages</strong> button on any demo to
                    return here.
                </div>
            </div>

            <div className="row g-4">
                {groups.map((group) => (
                    <div className="col-md-6 col-xxl-4" key={group.title}>
                        <Card
                            cssClass="h-100"
                            headerContent={
                                <span className="d-flex align-items-center">
                                    <i className={`bi ${group.icon} me-2`}></i>{group.title}
                                </span>
                            }>
                            <ul className="list-unstyled mb-0">
                                {flatten(group).map((leaf) => (
                                    <li className="mb-2 d-flex align-items-center" key={leaf.href}>
                                        <i className="bi bi-dot me-1 opacity-50"></i>
                                        <Link to={`/${leaf.href}`} className="text-reset btn-link">
                                            {leaf.title}
                                        </Link>
                                    </li>
                                ))}
                            </ul>
                        </Card>
                    </div>
                ))}
            </div>
        </>
    );
};
