// Minimal ambient typings for the globally loaded ApexCharts bundle
// (index.html loads /assets/vendor/apexcharts/apexcharts.min.js).
// Only the surface the coreUI Chart component touches is declared.

interface ApexChartsInstance {
    render(): Promise<void>;
    destroy(): void;
}

interface ApexChartsConstructor {
    new (element: HTMLElement, options: Record<string, unknown>): ApexChartsInstance;
}

interface Window {
    ApexCharts?: ApexChartsConstructor;
}

// Ambient declarations for the Blogzine vendor libraries loaded globally in index.html
// (tiny-slider, sticky-js, isotope, imagesLoaded, glightbox, plyr, vanilla-lazyload).
// Only the surface the hooks in src/hooks actually touch is declared — these are not
// full typings for the libraries.

interface TinySliderInstance {
    destroy(): void;
}

interface TinySliderResponsiveEntry {
    items?: number;
}

interface TinySliderOptions {
    container?: HTMLElement;
    mode?: string;
    axis?: string;
    gutter?: number | string;
    edgePadding?: number | string;
    speed?: number | string;
    autoWidth?: boolean;
    controls?: boolean;
    nav?: boolean;
    autoplay?: boolean;
    autoplayTimeout?: number | string;
    autoplayHoverPause?: boolean;
    autoplayButton?: boolean;
    autoplayButtonOutput?: boolean;
    controlsPosition?: string;
    navContainer?: HTMLElement | false;
    navPosition?: string;
    autoplayPosition?: string;
    controlsText?: string[];
    loop?: boolean;
    rewind?: boolean;
    autoHeight?: boolean;
    fixedWidth?: boolean | number;
    touch?: boolean;
    mouseDrag?: boolean;
    arrowKeys?: boolean;
    items?: number | string;
    textDirection?: string;
    lazyload?: boolean;
    lazyloadSelector?: string;
    responsive?: Record<number, TinySliderResponsiveEntry>;
}

interface IsotopeInstance {
    layout(): void;
    arrange(options: { filter: string }): void;
    destroy(): void;
}

interface IsotopeOptions {
    itemSelector?: string;
    layoutMode?: string;
    transitionDuration?: string;
}

interface IsotopeConstructor {
    new (element: HTMLElement, options: IsotopeOptions): IsotopeInstance;
}

interface ImagesLoadedInstance {
    on(event: string, listener: () => void): ImagesLoadedInstance;
    off(event: string, listener: () => void): ImagesLoadedInstance;
}

interface GLightboxInstance {
    destroy(): void;
}

interface GLightboxOptions {
    selector?: string;
    openEffect?: string;
    closeEffect?: string;
}

interface StickyInstance {
    destroy(): void;
}

interface StickyConstructor {
    new (selector: string): StickyInstance;
}

interface LazyLoadInstance {
    update(): void;
    destroy(): void;
}

interface LazyLoadConstructor {
    new (options?: Record<string, unknown>): LazyLoadInstance;
}

interface PlyrInstance {
    destroy(): void;
}

interface PlyrOptions {
    captions?: { active?: boolean };
}

interface PlyrConstructor {
    new (target: HTMLElement, options?: PlyrOptions): PlyrInstance;
    setup(selector: string, options?: PlyrOptions): PlyrInstance[];
}

interface Window {
    tns?: (options: TinySliderOptions) => TinySliderInstance;
    Isotope?: IsotopeConstructor;
    imagesLoaded?: (element: HTMLElement) => ImagesLoadedInstance;
    GLightbox?: (options?: GLightboxOptions) => GLightboxInstance;
    Sticky?: StickyConstructor;
    LazyLoad?: LazyLoadConstructor;
    Plyr?: PlyrConstructor;
}
