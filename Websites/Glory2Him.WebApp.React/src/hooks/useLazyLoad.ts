import { useEffect } from "react";
import { useLocation } from "react-router-dom";

// Keeps vanilla-lazyload (window.LazyLoad, loaded globally in index.html) watching the images
// the current page renders. Mirrors the Blazor project's wwwroot/assets/js/functions.js,
// section "15 LAZY LOAD" (a single LazyLoad instance over the default .lazy selector) — but
// where the template only ran on DOMContentLoaded, an SPA swaps its DOM on every navigation,
// so one shared instance is updated on mount and again on each route change.
let lazyLoadInstance: LazyLoadInstance | undefined;

export const useLazyLoad = (): void => {
    const location = useLocation();

    useEffect(() => {
        if (!window.LazyLoad) {
            return;
        }

        if (!lazyLoadInstance) {
            lazyLoadInstance = new window.LazyLoad({});
        } else {
            lazyLoadInstance.update();
        }
    }, [location]);
};
