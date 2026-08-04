import { RefObject, useEffect, useRef } from "react";

// Initializes tiny-slider (window.tns, loaded globally in index.html) on the element the
// returned ref is attached to. The Blogzine template drives every slider option from data-*
// attributes on the .tiny-slider-inner element (see the Blazor project's
// wwwroot/assets/js/functions.js, section "04 TINY SLIDER") — this hook reads exactly the
// same attributes so ported markup behaves identically, and explicit options win over them.
// The instance is destroyed on unmount so SPA navigation never leaks sliders.
export const useTinySlider = <T extends HTMLElement = HTMLDivElement>(
    options?: Partial<TinySliderOptions>
): RefObject<T | null> => {
    const containerRef = useRef<T>(null);
    const optionsRef = useRef(options);
    optionsRef.current = options;

    useEffect(() => {
        const slider = containerRef.current;

        if (!slider || typeof window.tns !== "function") {
            return;
        }

        const attribute = (name: string): string | null =>
            slider.getAttribute(name);

        const sliderItems = attribute("data-items") ?? 4;
        const sliderItemsXl = attribute("data-items-xl") ?? Number(sliderItems);
        const sliderItemsLg = attribute("data-items-lg") ?? Number(sliderItemsXl);
        const sliderItemsMd = attribute("data-items-md") ?? Number(sliderItemsLg);
        const sliderItemsSm = attribute("data-items-sm") ?? Number(sliderItemsMd);
        const sliderItemsXs = attribute("data-items-xs") ?? Number(sliderItemsSm);

        const navContainer =
            document.querySelector<HTMLElement>(".custom-thumb") ?? undefined;

        const isRtl =
            document.getElementsByTagName("html")[0].getAttribute("dir") === "rtl";

        const dataDrivenOptions: TinySliderOptions = {
            container: slider,
            mode: attribute("data-mode") ?? "carousel",
            axis: attribute("data-axis") ?? "horizontal",
            gutter: attribute("data-gutter") ?? 30,
            edgePadding: attribute("data-edge") ?? 0,
            speed: attribute("data-speed") ?? 500,
            autoWidth: attribute("data-autowidth") === "true",
            controls: attribute("data-arrow") !== "false",
            nav: attribute("data-dots") !== "false",
            autoplay: attribute("data-autoplay") !== "false",
            autoplayTimeout: attribute("data-autoplaytime") ?? 4000,
            autoplayHoverPause: attribute("data-hoverpause") === "true",
            autoplayButton: false,
            autoplayButtonOutput: false,
            navContainer,
            controlsText: [
                '<i class="fas fa-chevron-left"></i>',
                '<i class="fas fa-chevron-right"></i>'
            ],
            loop: attribute("data-loop") !== "false",
            rewind: attribute("data-rewind") === "true",
            autoHeight: attribute("data-autoheight") === "true",
            fixedWidth: attribute("data-fixedwidth") === "true",
            touch: attribute("data-touch") !== "false",
            mouseDrag: attribute("data-drag") !== "false",
            arrowKeys: true,
            items: sliderItems,
            textDirection: isRtl ? "rtl" : undefined,
            lazyload: true,
            lazyloadSelector: ".lazy",
            responsive: {
                0: { items: Number(sliderItemsXs) },
                576: { items: Number(sliderItemsSm) },
                768: { items: Number(sliderItemsMd) },
                992: { items: Number(sliderItemsLg) },
                1200: { items: Number(sliderItemsXl) }
            }
        };

        const instance = window.tns({
            ...dataDrivenOptions,
            ...optionsRef.current
        });

        return () => instance.destroy();
    }, []);

    return containerRef;
};
