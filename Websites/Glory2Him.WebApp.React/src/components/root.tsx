import { Outlet, ScrollRestoration } from "react-router-dom";
import OffcanvasMenuComponent from "./layouts/offcanvasMenu";
import HeaderComponent from "./layouts/header";
import FooterComponent from "./layouts/footer";
import OfflineBanner from "./coreUI/offlineBanner";
import { InstallPrompt } from "./coreUI/installPrompt";
import { useBackToTop } from "../hooks/useBackToTop";
import { useInstallPrompt } from "../hooks/useInstallPrompt";
import { useLazyLoad } from "../hooks/useLazyLoad";

// The persistent Blogzine chrome around every page, ported from the Blazor MainLayout:
// offcanvas / header / main / footer. The back-to-top button (static in index.html) and the
// lazy-load observer are wired here because the template's DOMContentLoaded init never sees
// SPA-rendered DOM.
export default function Root() {
    useBackToTop();
    useLazyLoad();

    // The offer's own component stays a pure renderer, so the decision of whether to make it —
    // and which platform's wording to use — is taken here and handed down as props.
    const installPrompt = useInstallPrompt();

    return (
        <>
            <OfflineBanner />

            <OffcanvasMenuComponent />

            <HeaderComponent />

            {/* **************** MAIN CONTENT START **************** */}
            <main>
                <Outlet />
            </main>
            {/* **************** MAIN CONTENT END **************** */}

            <FooterComponent />

            <InstallPrompt
                variant={installPrompt.variant}
                onInstall={installPrompt.install}
                onDismiss={installPrompt.dismiss} />

            {/* createBrowserRouter's client-side navigations do not reset scroll position on
                their own — without this, landing on a short page (e.g. Contribute) after
                navigating from further down a long one leaves the viewport wherever it was. */}
            <ScrollRestoration />
        </>
    );
}
