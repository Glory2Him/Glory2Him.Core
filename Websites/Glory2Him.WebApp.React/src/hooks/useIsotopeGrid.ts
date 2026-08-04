import { RefObject, useEffect, useRef } from "react";

// Initializes an Isotope masonry/grid layout (window.Isotope + window.imagesLoaded, loaded
// globally in index.html) on the element the returned ref is attached to. Mirrors the Blazor
// project's wwwroot/assets/js/functions.js, section "13 ISOTOPE": the layout mode comes from
// the element's own data-isotope JSON attribute, items match .grid-item, and the layout is
// recomputed as each image loads. When filterSelector is supplied the matching .grid-menu's
// li a entries become filter buttons driving data-filter values, exactly as the template does.
// Everything is torn down on unmount so SPA navigation never leaks instances or listeners.
export const useIsotopeGrid = <T extends HTMLElement = HTMLDivElement>(
    filterSelector?: string
): RefObject<T | null> => {
    const gridRef = useRef<T>(null);

    useEffect(() => {
        const grid = gridRef.current;

        if (!grid || !window.Isotope || !window.imagesLoaded) {
            return;
        }

        const isotopeData = grid.getAttribute("data-isotope");

        const layoutMode: string | undefined = isotopeData
            ? (JSON.parse(isotopeData) as { layoutMode?: string }).layoutMode
            : undefined;

        const instance = new window.Isotope(grid, {
            itemSelector: ".grid-item",
            transitionDuration: filterSelector ? "0.7s" : undefined,
            layoutMode
        });

        const relayout = () => instance.layout();

        const imagesLoadedInstance =
            window.imagesLoaded(grid).on("progress", relayout);

        const menuItems: HTMLElement[] = filterSelector
            ? Array.from(document.querySelectorAll<HTMLElement>(
                `${filterSelector} li a`))
            : [];

        const menuItemListeners = menuItems.map((menuItem) => {
            const listener = () => {
                const filterValue = menuItem.getAttribute("data-filter") ?? "*";
                instance.arrange({ filter: filterValue });
                menuItems.forEach((control) => control.classList.remove("active"));
                menuItem.classList.add("active");
            };

            menuItem.addEventListener("click", listener);

            return { menuItem, listener };
        });

        return () => {
            menuItemListeners.forEach(({ menuItem, listener }) =>
                menuItem.removeEventListener("click", listener));

            imagesLoadedInstance.off("progress", relayout);
            instance.destroy();
        };
    }, [filterSelector]);

    return gridRef;
};
