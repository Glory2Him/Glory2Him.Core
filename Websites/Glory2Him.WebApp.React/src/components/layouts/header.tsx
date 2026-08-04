import { ReactElement } from "react";
import { Link } from "react-router-dom";
import { useAuth } from "../securitys/authProvider";
import { SecuredComponent } from "../securitys/securedComponents";
import { accountService } from "../../services/foundations/accountService";
import securityPoints from "../../securityMatrix";
import { useStickyHeader } from "../../hooks/useStickyHeader";
import { Theme, useTheme } from "../../hooks/useTheme";
import BrandComponent from "../coreUI/brand";
import UserMenuComponent from "./userMenu";
import "./header.css";

// Blogzine header chrome, ported from the Blazor HeaderComponent: top bar (accessibility +
// dark-mode + social), logo navbar with the section nav, and the right-hand search /
// user-menu / offcanvas controls. The textless Glory2Him sunset-over-mountains photo sits
// behind the whole header as a single full-bleed background (see header.css); navbar-dark
// switches every nav-link, icon and the wordmark to white so they stay readable over it, and
// the accessibility buttons ride a light pill for the same reason. The template's
// DOMContentLoaded wiring (sticky header, theme switch, font sizing) never sees this
// React-rendered DOM, so those behaviors live in useStickyHeader/useTheme/onChange instead.

// The identity name is the raw username ("admin"), which reads as a typo next to the other
// top-bar links — capitalize it for display only.
const toDisplayName = (userName: string | undefined): string =>
    !userName || userName.trim().length === 0
        ? ""
        : userName[0].toUpperCase() + userName.slice(1);

// The template's font-size accessibility switch (functions.js, section "14 FONT SIZE"):
// font-sm/font-lg classes on <html> scale the whole page.
const onFontSizeChange = (fontSizeId: string): void => {
    const doc = document.documentElement;

    if (fontSizeId === "font-sm") {
        doc.classList.remove("font-lg");
        doc.classList.add("font-sm");
    } else if (fontSizeId === "font-default") {
        doc.classList.remove("font-sm", "font-lg");
    } else if (fontSizeId === "font-lg") {
        doc.classList.remove("font-sm");
        doc.classList.add("font-lg");
    }
};

export default function HeaderComponent(): ReactElement {
    const { isAuthenticated, user } = useAuth();
    const logout = accountService.useLogout();
    const headerRef = useStickyHeader<HTMLElement>();
    const { theme, setTheme } = useTheme();

    const themeItemClass = (itemTheme: Theme): string =>
        `dropdown-item d-flex align-items-center${theme === itemTheme ? " active" : ""}`;

    return (
        <header ref={headerRef} className="navbar-dark navbar-sticky header-static g2h-header-photo">
            <div className="navbar-top d-none d-lg-block small">
                <div className="container">
                    <div className="d-md-flex justify-content-between align-items-center my-2">
                        {/* Top bar left */}
                        <ul className="nav align-items-center">
                            <li className="nav-item">
                                <Link className="nav-link ps-0" to="/About-Us">About</Link>
                            </li>
                            <li className="nav-item">
                                <Link className="nav-link" to="/Contact-Us">Contact</Link>
                            </li>
                            {isAuthenticated ? (
                                <>
                                    <SecuredComponent allowedRoles={securityPoints.admin.view}>
                                        <li className="nav-item">
                                            <Link className="nav-link" to="/Admin/Users">Admin</Link>
                                        </li>
                                    </SecuredComponent>
                                    {/* Always shown (not just for administrators): it separates
                                        About/Contact from the signed-in user's own links, whether
                                        that's just the profile + logout, or the admin link too. */}
                                    <li className="nav-item d-flex align-items-center">
                                        <span className="text-white-50" aria-hidden="true">|</span>
                                    </li>
                                    <li className="nav-item">
                                        <Link className="nav-link d-flex align-items-center" to="/Account/Manage">
                                            <i className="bi bi-person me-1"></i>
                                            {toDisplayName(user?.userName)}
                                        </Link>
                                    </li>
                                    <li className="nav-item">
                                        {/* The button itself is a fully reset, invisible click target
                                            (no border/background/padding of its own — .btn's padding and
                                            line-height fought with .nav-link's, leaving the text baseline
                                            slightly higher than the sibling <a> links). The actual nav-link
                                            styling lives on the inner span instead, which renders with the
                                            exact same box as "About"/"Contact"/"Admin". */}
                                        <button type="button" onClick={() => logout.mutate()}
                                            className="p-0 border-0 bg-transparent">
                                            <span className="nav-link d-flex align-items-center">
                                                <i className="bi bi-box-arrow-right me-1"></i>Logout
                                            </span>
                                        </button>
                                    </li>
                                </>
                            ) : (
                                <>
                                    <li className="nav-item d-flex align-items-center">
                                        <span className="text-white-50" aria-hidden="true">|</span>
                                    </li>
                                    <li className="nav-item">
                                        <Link className="nav-link" to="/Account/Login">Login / Join</Link>
                                    </li>
                                </>
                            )}
                        </ul>
                        {/* Top bar right */}
                        <div className="d-flex align-items-center">
                            {/* Font size accessibility START */}
                            <div className="btn-group me-3 g2h-header-pill" role="group" aria-label="font size changer">
                                <input type="radio" className="btn-check" name="fntradio" id="font-sm"
                                    onChange={() => onFontSizeChange("font-sm")} />
                                <label className="btn btn-xs btn-outline-primary mb-0" htmlFor="font-sm">A-</label>

                                <input type="radio" className="btn-check" name="fntradio" id="font-default" defaultChecked
                                    onChange={() => onFontSizeChange("font-default")} />
                                <label className="btn btn-xs btn-outline-primary mb-0" htmlFor="font-default">A</label>

                                <input type="radio" className="btn-check" name="fntradio" id="font-lg"
                                    onChange={() => onFontSizeChange("font-lg")} />
                                <label className="btn btn-xs btn-outline-primary mb-0" htmlFor="font-lg">A+</label>
                            </div>

                            {/* Dark mode options START */}
                            <div className="nav-item dropdown mx-2">
                                <button className="modeswitch" id="bd-theme" type="button" aria-expanded="false" data-bs-toggle="dropdown" data-bs-display="static">
                                    <svg className="theme-icon-active"><use href="#"></use></svg>
                                </button>
                                <ul className="dropdown-menu min-w-auto dropdown-menu-end" aria-labelledby="bd-theme">
                                    <li className="mb-1">
                                        <button type="button" className={themeItemClass("light")}
                                            data-bs-theme-value="light" onClick={() => setTheme("light")}>
                                            <svg width="16" height="16" fill="currentColor" className="bi bi-brightness-high-fill fa-fw mode-switch me-1" viewBox="0 0 16 16">
                                                <path d="M12 8a4 4 0 1 1-8 0 4 4 0 0 1 8 0zM8 0a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 0zm0 13a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-1 0v-2A.5.5 0 0 1 8 13zm8-5a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2a.5.5 0 0 1 .5.5zM3 8a.5.5 0 0 1-.5.5h-2a.5.5 0 0 1 0-1h2A.5.5 0 0 1 3 8zm10.657-5.657a.5.5 0 0 1 0 .707l-1.414 1.415a.5.5 0 1 1-.707-.708l1.414-1.414a.5.5 0 0 1 .707 0zm-9.193 9.193a.5.5 0 0 1 0 .707L3.05 13.657a.5.5 0 0 1-.707-.707l1.414-1.414a.5.5 0 0 1 .707 0zm9.193 2.121a.5.5 0 0 1-.707 0l-1.414-1.414a.5.5 0 0 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .707zM4.464 4.465a.5.5 0 0 1-.707 0L2.343 3.05a.5.5 0 1 1 .707-.707l1.414 1.414a.5.5 0 0 1 0 .708z" />
                                                <use href="#"></use>
                                            </svg>Light
                                        </button>
                                    </li>
                                    <li className="mb-1">
                                        <button type="button" className={themeItemClass("dark")}
                                            data-bs-theme-value="dark" onClick={() => setTheme("dark")}>
                                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-moon-stars-fill fa-fw mode-switch me-1" viewBox="0 0 16 16">
                                                <path d="M6 .278a.768.768 0 0 1 .08.858 7.208 7.208 0 0 0-.878 3.46c0 4.021 3.278 7.277 7.318 7.277.527 0 1.04-.055 1.533-.16a.787.787 0 0 1 .81.316.733.733 0 0 1-.031.893A8.349 8.349 0 0 1 8.344 16C3.734 16 0 12.286 0 7.71 0 4.266 2.114 1.312 5.124.06A.752.752 0 0 1 6 .278z" />
                                                <path d="M10.794 3.148a.217.217 0 0 1 .412 0l.387 1.162c.173.518.579.924 1.097 1.097l1.162.387a.217.217 0 0 1 0 .412l-1.162.387a1.734 1.734 0 0 0-1.097 1.097l-.387 1.162a.217.217 0 0 1-.412 0l-.387-1.162A1.734 1.734 0 0 0 9.31 6.593l-1.162-.387a.217.217 0 0 1 0-.412l1.162-.387a1.734 1.734 0 0 0 1.097-1.097l.387-1.162zM13.863.099a.145.145 0 0 1 .274 0l.258.774c.115.346.386.617.732.732l.774.258a.145.145 0 0 1 0 .274l-.774.258a1.156 1.156 0 0 0-.732.732l-.258.774a.145.145 0 0 1-.274 0l-.258-.774a1.156 1.156 0 0 0-.732-.732l-.774-.258a.145.145 0 0 1 0-.274l.774-.258c.346-.115.617-.386.732-.732L13.863.1z" />
                                                <use href="#"></use>
                                            </svg>Dark
                                        </button>
                                    </li>
                                    <li>
                                        <button type="button" className={themeItemClass("auto")}
                                            data-bs-theme-value="auto" onClick={() => setTheme("auto")}>
                                            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="bi bi-circle-half fa-fw mode-switch me-1" viewBox="0 0 16 16">
                                                <path d="M8 15A7 7 0 1 0 8 1v14zm0 1A8 8 0 1 1 8 0a8 8 0 0 1 0 16z" />
                                                <use href="#"></use>
                                            </svg>Auto
                                        </button>
                                    </li>
                                </ul>
                            </div>
                            {/* Dark mode options END */}

                            <ul className="nav">
                                <li className="nav-item">
                                    <a className="nav-link px-2 fs-5" href="#"><i className="fab fa-facebook-square"></i></a>
                                </li>
                                <li className="nav-item">
                                    <a className="nav-link px-2 fs-5" href="#"><i className="fab fa-twitter-square"></i></a>
                                </li>
                                <li className="nav-item">
                                    <a className="nav-link px-2 fs-5" href="#"><i className="fab fa-linkedin"></i></a>
                                </li>
                                <li className="nav-item">
                                    <a className="nav-link px-2 fs-5" href="#"><i className="fab fa-youtube-square"></i></a>
                                </li>
                            </ul>
                        </div>
                    </div>
                    {/* Divider */}
                    <div className="border-bottom border-2 border-primary opacity-1"></div>
                </div>
            </div>

            {/* Logo Nav START */}
            {/* navbar-dark repeats here (in addition to <header>): Bootstrap's .navbar rule resets
                the brand/link color custom properties for its own subtree, so the header's
                navbar-dark alone doesn't reach the brand text or the nav links nested inside
                .navbar. */}
            <nav className="navbar navbar-dark navbar-expand-lg">
                <div className="container">
                    {/* Logo START */}
                    <Link className="navbar-brand" to="/">
                        <BrandComponent variant="text" accentTwo={false} />
                    </Link>
                    {/* Logo END */}

                    {/* Section nav START */}
                    {/* The three sections of the site, centred between the wordmark and the search
                        button. mx-auto is what centres them: it takes the space either side, so the
                        group sits in the middle of what is left rather than butting up against the
                        brand. Hidden below lg — the wordmark, the Search button and the icons
                        already fill a phone width, and the offcanvas menu covers navigation there. */}
                    <ul className="navbar-nav flex-row flex-nowrap align-items-center mx-auto d-none d-lg-flex g2h-section-nav">
                        <li className="nav-item">
                            <Link className="nav-link" to="/">Posts</Link>
                        </li>

                        <li className="nav-item" aria-hidden="true">
                            <span className="nav-link px-2 opacity-50">|</span>
                        </li>

                        {/* Spans, not links: these two sections have no route yet, and an anchor
                            that goes nowhere is worse than plain text. Swap each for a Link when
                            they do. */}
                        <li className="nav-item">
                            <span className="nav-link g2h-section-pending" title="Coming soon">Series</span>
                        </li>

                        <li className="nav-item" aria-hidden="true">
                            <span className="nav-link px-2 opacity-50">|</span>
                        </li>

                        <li className="nav-item">
                            <span className="nav-link g2h-section-pending" title="Coming soon">The Gospel</span>
                        </li>
                    </ul>
                    {/* Section nav END */}

                    {/* Nav right START */}
                    <div className="nav flex-nowrap align-items-center">
                        {/* Search — replaces the magnifier that used to drop a small search form
                            down here: that posted to Search-Result, and having two search entry
                            points in one header meant two different result pages. This one just
                            goes to /Search. */}
                        <div className="nav-item me-2">
                            <Link to="/Search" className="btn btn-success mb-0 d-flex align-items-center">
                                <i className="bi bi-search me-2"></i>Search
                            </Link>
                        </div>
                        {/* Notifications + profile menu (CoreUI-style) */}
                        <UserMenuComponent />
                        {/* Offcanvas menu toggler */}
                        <div className="nav-item">
                            <a className="nav-link p-0" data-bs-toggle="offcanvas" href="#offcanvasMenu" role="button" aria-controls="offcanvasMenu">
                                <i className="bi bi-text-right rtl-flip fs-2" data-bs-target="#offcanvasMenu"> </i>
                            </a>
                        </div>
                    </div>
                    {/* Nav right END */}
                </div>
            </nav>
            {/* Logo Nav END */}
        </header>
    );
}
