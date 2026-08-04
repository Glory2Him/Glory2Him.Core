import { useEffect } from "react";

// Wires the static .back-top button that lives in index.html. Mirrors the Blazor project's
// wwwroot/assets/js/functions.js, section "09 BACK TO TOP": the button fades in
// (.back-top-show) once the page has scrolled 800px, and clicking it smooth-scrolls back to
// the top. functions.js only ran on DOMContentLoaded, which never re-fires in an SPA — this
// hook owns the behavior instead, mounted once in the root layout.
export const useBackToTop = (): void => {
    useEffect(() => {
        const backButton = document.querySelector<HTMLElement>(".back-top");

        if (!backButton) {
            return;
        }

        const onScroll = () => {
            if (window.scrollY >= 800) {
                backButton.classList.add("back-top-show");
            } else {
                backButton.classList.remove("back-top-show");
            }
        };

        const onClick = () => window.scrollTo({
            top: 0,
            behavior: "smooth"
        });

        window.addEventListener("scroll", onScroll);
        backButton.addEventListener("click", onClick);

        return () => {
            window.removeEventListener("scroll", onScroll);
            backButton.removeEventListener("click", onClick);
        };
    }, []);
};
