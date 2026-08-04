import { ReactElement } from 'react';
import { Outlet } from 'react-router-dom';
import { Breadcrumb } from '../coreUI/breadcrumb';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import NavMenu from '../navigations/navMenu';
import ManageNavMenu from './manageNavMenu';

// Ported from Blazor's Account/Shared/ManageLayout.razor: the account-management pages render
// inside the shared sidebar shell (Account-area NavMenu on the left), with the "Manage your
// account" heading + breadcrumb above the page body. The ManageNavMenu offers the in-area
// pill navigation between the converted flows.
const manageCrumbs: BreadcrumbItem[] = [
    { title: 'My Account', href: 'Account/Manage', isActive: true },
];

export default function ManageLayout(): ReactElement {
    return (
        <section className="py-4">
            <div className="container">
                <div className="row g-4">
                    {/* Sidebar */}
                    <aside className="col-lg-3">
                        <div className="card card-body border p-3">
                            <NavMenu />
                        </div>
                    </aside>

                    {/* Content */}
                    <div className="col-lg-9">
                        <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                            <h1 className="h3 mb-0">Manage your account</h1>
                            <Breadcrumb items={manageCrumbs} />
                        </div>
                        <hr />

                        <div className="row">
                            <div className="col-md-3 mb-3">
                                <ManageNavMenu />
                            </div>
                            <div className="col-md-9">
                                <Outlet />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
