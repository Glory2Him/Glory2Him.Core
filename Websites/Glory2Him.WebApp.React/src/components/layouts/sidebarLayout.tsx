import { ReactElement, useId, useState } from "react";
import { Outlet } from "react-router-dom";
import NavMenu from "../navigations/navMenu";
import "./sidebarLayout.css";

// Shared shell for the authenticated areas (dashboard, admin, account profile), ported from
// the Blazor SidebarLayout: CoreUI-style left sidebar menu, restyled in Blogzine/Bootstrap.
// Use it as a react-router layout route nested under Root — its children render in the Outlet:
//   { element: <SidebarLayout />, children: [{ path: "Dashboard", element: <Dashboard /> }] }
//
// THE MENU FOLDS AWAY ENTIRELY. An admin surface is a working surface — a moderation queue, a
// settings table, a post being read in full — and the menu is navigation, not part of the work.
// Folded, its whole column goes: no rail, no gutter, nothing left standing in the content's
// way. Either the menu is there in full or it is not there at all.
//
// THE CONTROL LIVES IN THE CONTENT, not in the menu it folds. A control inside the panel would
// go with it, and then there would be nothing left to press — which is why it sits at the head
// of the content column instead, beside the page's own title, where it is in the same place
// whichever state the menu is in.
//
// THE CHOICE IS REMEMBERED, per browser. Someone who folds the menu to read a long post is
// still reading it after the next navigation, and a preference that reset on every refresh
// would be worse than not offering one. localStorage is guarded both ways — a private window,
// blocked site data, or a browser that throws on access all fall back to the menu shown, which
// is the safe default because it is the one that can be changed from.
const menuPreferenceKey = "g2h-sidebar-menu";

const readMenuPreference = (): boolean => {
    try {
        return window.localStorage.getItem(menuPreferenceKey) !== "collapsed";
    } catch {
        return true;
    }
};

const writeMenuPreference = (isShown: boolean): void => {
    try {
        window.localStorage.setItem(menuPreferenceKey, isShown ? "shown" : "collapsed");
    } catch {
        // A viewer who cannot store the preference still gets the toggle for this session.
    }
};

export default function SidebarLayout(): ReactElement {
    const [isMenuShown, setIsMenuShown] = useState(readMenuPreference);
    const menuId = useId();

    const toggleMenu = () => {
        const shown = isMenuShown === false;

        setIsMenuShown(shown);
        writeMenuPreference(shown);
    };

    const toggleLabel = isMenuShown ? "Collapse the menu" : "Expand the menu";

    return (
        <section className="py-4">
            <div className="container">
                <div className="row g-4">
                    {/* Sidebar. Absent entirely when folded — not narrowed, not hidden — so the
                        content column has the whole row rather than the row minus a stub. */}
                    {isMenuShown && (
                        <aside className="col-lg-3" id={menuId}>
                            <div className="card card-body border p-3">
                                <NavMenu />
                            </div>
                        </aside>
                    )}

                    {/* Content */}
                    <div className={isMenuShown ? "col-lg-9" : "col-12"}>
                        {/* The toggle leads the page rather than sitting above it: align-items
                            start keeps it on the title's line, and the Outlet takes the rest of
                            the width through flex-grow. min-width-0 is what stops a wide child
                            — a table, a long unbroken title — from pushing the button off. */}
                        <div className="d-flex align-items-start gap-3">
                            <button
                                type="button"
                                className="btn btn-link p-0 text-body g2h-sidebar-toggle"
                                onClick={toggleMenu}
                                aria-controls={menuId}
                                aria-expanded={isMenuShown}
                                aria-label={toggleLabel}
                                title={toggleLabel}>
                                <i className="bi bi-list" aria-hidden="true"></i>
                            </button>

                            <div className="flex-grow-1 g2h-sidebar-content">
                                <Outlet />
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
