import { useEffect, useRef } from "react";

// Initializes GLightbox (window.GLightbox, loaded globally in index.html) for the elements the
// current page renders. Mirrors the Blazor project's wwwroot/assets/js/functions.js, section
// "12 GLIGHTBOX": every element carrying data-glightbox joins the gallery, opening and closing
// with a fade. Runs after the page's DOM is committed and destroys the instance on unmount so
// SPA navigation re-binds against fresh elements instead of stale ones.
export const useGLightbox = (options?: GLightboxOptions): void => {
    const optionsRef = useRef(options);
    optionsRef.current = options;

    useEffect(() => {
        if (typeof window.GLightbox !== "function") {
            return;
        }

        const instance = window.GLightbox({
            selector: "*[data-glightbox]",
            openEffect: "fade",
            closeEffect: "fade",
            ...optionsRef.current
        });

        return () => instance.destroy();
    }, []);
};
