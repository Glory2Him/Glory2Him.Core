import { ReactElement, useId, useState } from "react";
import { Outlet } from "react-router-dom";
import NavMenu from "../navigations/navMenu";
import "./sidebarLayout.css";

// Shared shell for the authenticated areas (dashboard, admin, account profile), ported from
// the Blazor SidebarLayout: CoreUI-style left sidebar menu, restyled in Blogzine/Bootstrap.
// Use it as a react-router layout route nested under Root — its children render in the Outlet:
//   { element: <SidebarLayout />, children: [{ path: "Dashboard", element: <Dashboard /> }] }
//
// THE MENU FOLDS TO A RAIL. An admin surface is a working surface — a moderation queue, a
// settings table, a post being read in full — and the menu is navigation, not part of the work.
// Folded, its quarter of the row goes to the content and it keeps only its icons, each named by
// a tooltip: every destination is still one click away, so folding costs reach rather than
// buying width with it.
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
                    {/* Sidebar. Folded it is col-auto — as wide as the toggle needs and no
                        wider — so the content column takes the rest through col rather than a
                        second fixed width that would have to be kept in step with this one. */}
                    <aside className={isMenuShown ? "col-lg-3" : "col-auto"}>
                        <div className="card card-body border p-3">
                            <div
                                className={`d-flex ${isMenuShown
                                    ? "justify-content-end mb-2"
                                    : "justify-content-center"}`}>
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
                            </div>

                            {/* The SAME menu either way — folded it renders as the icon rail,
                                so nothing is dropped from it and no destination goes missing
                                with the words. A group cannot open inside a rail, so its icon
                                asks for the menu back instead. */}
                            <div id={menuId}>
                                <NavMenu
                                    isCollapsed={isMenuShown === false}
                                    onExpandRequested={() => {
                                        setIsMenuShown(true);
                                        writeMenuPreference(true);
                                    }} />
                            </div>
                        </div>
                    </aside>

                    {/* Content */}
                    <div className={isMenuShown ? "col-lg-9" : "col"}>
                        <Outlet />
                    </div>
                </div>
            </div>
        </section>
    );
}
