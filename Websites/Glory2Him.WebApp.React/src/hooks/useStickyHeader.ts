import { RefObject, useEffect, useRef } from "react";

// Replicates the Blogzine sticky header from the Blazor project's
// wwwroot/assets/js/functions.js, section "03 STICKY HEADER": past 400px of scroll the
// .navbar-sticky header gains .navbar-sticky-on (the template's CSS then fixes it to the top)
// and a spacer div holding the header's original height keeps the page from jumping.
// functions.js only ran on DOMContentLoaded, before React has rendered — this hook owns the
// behavior, attached to the <header> element via the returned ref, and removes both the
// listener and the spacer on unmount.
export const useStickyHeader = <T extends HTMLElement = HTMLElement>(
): RefObject<T | null> => {
    const headerRef = useRef<T>(null);

    useEffect(() => {
        const stickyNav = headerRef.current;

        if (!stickyNav) {
            return;
        }

        const stickyHeight = stickyNav.offsetHeight;
        const stickySpace = document.createElement("div");
        stickySpace.id = "sticky-space";
        stickyNav.insertAdjacentElement("afterend", stickySpace);

        const onScroll = () => {
            const scrollTop =
                window.pageYOffset || document.documentElement.scrollTop;

            if (scrollTop >= 400) {
                stickySpace.classList.add("active");
                stickySpace.style.height = `${stickyHeight}px`;
                stickyNav.classList.add("navbar-sticky-on");
            } else {
                stickySpace.classList.remove("active");
                stickySpace.style.height = "0px";
                stickyNav.classList.remove("navbar-sticky-on");
            }
        };

        document.addEventListener("scroll", onScroll);

        return () => {
            document.removeEventListener("scroll", onScroll);
            stickySpace.remove();
        };
    }, []);

    return headerRef;
};
