import { ReactElement } from "react";
import { Outlet } from "react-router-dom";
import NavMenu from "../navigations/navMenu";

// Shared shell for the authenticated areas (dashboard, admin, account profile), ported from
// the Blazor SidebarLayout: CoreUI-style left sidebar menu, restyled in Blogzine/Bootstrap.
// Use it as a react-router layout route nested under Root — its children render in the Outlet:
//   { element: <SidebarLayout />, children: [{ path: "Dashboard", element: <Dashboard /> }] }
export default function SidebarLayout(): ReactElement {
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
                        <Outlet />
                    </div>
                </div>
            </div>
        </section>
    );
}
