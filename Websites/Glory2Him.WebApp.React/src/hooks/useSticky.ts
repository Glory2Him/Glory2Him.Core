import { useEffect } from "react";

// Activates sticky-js (window.Sticky, loaded globally in index.html) for the current page.
// Mirrors the Blazor project's wwwroot/assets/js/functions.js, section "06 STICKY BAR": the
// template marks sticky sidebars/bars with the data-sticky attribute and hands the selector to
// Sticky. The instance is destroyed on unmount so SPA navigation re-binds against the elements
// the next page renders.
export const useSticky = (selector: string = "[data-sticky]"): void => {
    useEffect(() => {
        if (!window.Sticky || !document.querySelector(selector)) {
            return;
        }

        const instance = new window.Sticky(selector);

        return () => instance.destroy();
    }, [selector]);
};
