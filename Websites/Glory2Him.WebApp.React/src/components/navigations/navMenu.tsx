import { Fragment, ReactElement, ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { hasChildren, NavItem } from "../../models/views/navigations/navItem";
import { navMenuProvider } from "../../services/views/navigations/navMenuProvider";
import { SecuredComponent } from "../securitys/securedComponents";
import "./navMenu.css";

// CoreUI-style left sidebar menu, ported from the Blazor NavMenu: restyled with
// Blogzine/Bootstrap classes and Bootstrap Icons. The menu tree comes from navMenuProvider,
// narrowed to the area being viewed; role/auth gating goes through SecuredComponent (which
// reads the same roles Blazor's AuthorizeView did). Top-level items render as a section
// heading; deeper items that still have children render as a collapsible nav-group.
// Collapsing uses Bootstrap's own data-bs-toggle collapse (bootstrap.bundle.min.js delegates
// at the document level, so it reaches React-rendered DOM).
type NavMenuProps = {
    // Left undefined the menu follows the page being viewed; set it to pin the sidebar to a
    // fixed list.
    items?: NavItem[]

    // THE RAIL. Folded, the menu keeps every item it had but shows each as its icon alone,
    // named by a tooltip. It is the same tree under the same gates — nothing is dropped, so
    // folding the menu never quietly takes a destination away.
    isCollapsed?: boolean

    // What a GROUP does in the rail. A rail cannot nest — there is no room beside an icon for
    // the children hanging off it — so a group asks for the menu back instead of pretending to
    // open in place. Without a handler a group icon is inert rather than misleading.
    onExpandRequested?: () => void
}

// Bootstrap needs a DOM id to collapse against, and it has to survive a re-render so an open
// group stays open — derive it from the item's place in the tree rather than a random id.
const slugify = (value: string): string =>
    value
        .split("")
        .map((character) => /[a-zA-Z0-9]/.test(character)
            ? character.toLowerCase()
            : "-")
        .join("");

const getGroupId = (item: NavItem): string =>
    "nav-group-" + slugify(item.href.length > 0 ? item.href : item.title);

const containsActiveLeaf = (item: NavItem, currentPath: string): boolean => {
    if (hasChildren(item)) {
        return item.children!.some((child) => containsActiveLeaf(child, currentPath));
    }

    if (!item.href || item.href.trim().length === 0) {
        return false;
    }

    const href = item.href.replace(/^\/+|\/+$/g, "").toLowerCase();

    return item.exactMatch
        ? currentPath === href
        : currentPath.startsWith(href);
};

export default function NavMenu({
    items,
    isCollapsed = false,
    onExpandRequested
}: NavMenuProps): ReactElement {
    const location = useLocation();

    const relativePath = location.pathname.replace(/^\/+/, "");

    const currentPath = relativePath
        .split("?")[0]
        .replace(/^\/+|\/+$/g, "")
        .toLowerCase();

    const visibleItems = items ?? navMenuProvider.getNavMenu(relativePath);

    // ONE ITEM IN THE RAIL: its icon, and its title as the tooltip — the title is the only
    // thing naming it now, so it is both the tooltip and the accessible name rather than
    // decoration over an unlabelled control.
    const renderRailNode = (item: NavItem): ReactNode => {
        if (hasChildren(item)) {
            const groupId = getGroupId(item);

            return (
                <li className="nav-item" key={groupId}>
                    <button
                        type="button"
                        className="nav-link g2h-nav-rail-link"
                        title={item.title}
                        aria-label={`${item.title} — expand the menu`}
                        onClick={onExpandRequested}>
                        <i className={`bi ${item.icon}`} aria-hidden="true"></i>
                    </button>
                </li>
            );
        }

        return (
            <li className="nav-item" key={item.href}>
                <NavLink
                    className={({ isActive }) =>
                        `nav-link g2h-nav-rail-link${isActive ? " active" : ""}`}
                    to={`/${item.href}`}
                    end={item.exactMatch === true}
                    title={item.title}
                    aria-label={item.title}>
                    <i className={`bi ${item.icon}`} aria-hidden="true"></i>
                </NavLink>
            </li>
        );
    };

    // A group renders expanded when the page being viewed lives somewhere inside it, so
    // landing deep in the tree never leaves you looking at a collapsed menu.
    const renderNode = (item: NavItem): ReactNode => {
        if (isCollapsed) {
            return renderRailNode(item);
        }

        if (hasChildren(item)) {
            const groupId = getGroupId(item);
            const isOpen = containsActiveLeaf(item, currentPath);

            return (
                <li className="nav-item nav-group" key={groupId}>
                    <a className={`nav-link nav-group-toggle d-flex align-items-center ${isOpen ? "" : "collapsed"}`}
                        data-bs-toggle="collapse"
                        href={`#${groupId}`}
                        role="button"
                        aria-controls={groupId}
                        aria-expanded={isOpen ? "true" : "false"}>
                        <i className={`bi ${item.icon} me-2`}></i>{item.title}
                        <i className="bi bi-chevron-down ms-auto g2h-nav-chevron"></i>
                    </a>

                    <ul className={`nav flex-column nav-group-items collapse ${isOpen ? "show" : ""}`}
                        id={groupId}>
                        {item.children!.map((child) => renderNode(child))}
                    </ul>
                </li>
            );
        }

        return (
            <li className="nav-item" key={item.href}>
                <NavLink
                    className={({ isActive }) =>
                        `nav-link d-flex align-items-center${isActive ? " active" : ""}`}
                    to={`/${item.href}`}
                    end={item.exactMatch === true}>
                    <i className={`bi ${item.icon} me-2`}></i>{item.title}
                </NavLink>
            </li>
        );
    };

    const renderSection = (item: NavItem): ReactNode => {
        if (hasChildren(item)) {
            // A heading is a WORD, and the rail has no room for one. The grouping it carries is
            // kept as a rule between the icons rather than lost outright.
            if (isCollapsed) {
                return (
                    <>
                        <li className="nav-item w-100" aria-hidden="true">
                            <hr className="my-2" />
                        </li>

                        {item.children!.map((child) => renderNode(child))}
                    </>
                );
            }

            return (
                <>
                    <li className="nav-item mt-3">
                        <span className="nav-title text-uppercase small fw-bold text-body-secondary px-3">
                            <i className={`bi ${item.icon} me-1`}></i>{item.title}
                        </span>
                    </li>

                    {item.children!.map((child) => renderNode(child))}
                </>
            );
        }

        return renderNode(item);
    };

    const renderGatedSection = (item: NavItem): ReactNode => {
        const key = item.href.length > 0 ? item.href : item.title;

        if (item.roles && item.roles.length > 0) {
            return (
                <SecuredComponent allowedRoles={item.roles} key={key}>
                    <>{renderSection(item)}</>
                </SecuredComponent>
            );
        }

        if (item.requiresAuth) {
            return (
                <SecuredComponent key={key}>
                    <>{renderSection(item)}</>
                </SecuredComponent>
            );
        }

        return <Fragment key={key}>{renderSection(item)}</Fragment>;
    };

    return (
        <nav className={`sidebar-nav${isCollapsed ? " g2h-nav-rail" : ""}`}>
            <ul className={`nav flex-column${isCollapsed ? " align-items-center" : ""}`}>
                {visibleItems.map((item) => renderGatedSection(item))}
            </ul>
        </nav>
    );
}
