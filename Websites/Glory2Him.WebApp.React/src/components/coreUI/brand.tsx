import { CSSProperties, ReactElement } from "react";
import "./brand.css";

// The Glory 2 Him site brand, ported from the Blazor BrandComponent. The banner artwork
// carries the wordmark on its own, so the compact icon + text lockup is what renders wherever
// the banner will not fit (narrow screens, the sticky navbar). Sizes come from brand.css
// unless a caller overrides them. Presentational only (ts-ui-001).
export type BrandVariant =
    // Banner on large screens, icon + wordmark below the lg breakpoint.
    | "responsive"

    // The banner artwork on its own.
    | "banner"

    // The square icon followed by the wordmark as text.
    | "compact"

    // The wordmark on its own, no icon — for placing over the header's own background art.
    | "text";

type BrandComponentProps = {
    variant?: BrandVariant,

    // Left undefined the component's own stylesheet sizes the mark, which is what the header
    // wants so the brand shrinks with the sticky navbar. Set it where a fixed size reads better.
    bannerHeightPx?: number,

    iconSizePx?: number,

    // Overrides the wordmark size directly. Useful for the text variant, which has no icon to
    // derive a balanced size from.
    nameFontSizePx?: number,

    // The "2" is the brand's colour accent on light backgrounds; a caller over its own artwork
    // (the header photo) wants the whole wordmark in one flat colour instead.
    accentTwo?: boolean
}

export default function BrandComponent({
    variant = "responsive",
    bannerHeightPx,
    iconSizePx,
    nameFontSizePx,
    accentTwo = true
}: BrandComponentProps): ReactElement {
    const twoSpanClass = accentTwo ? "text-primary" : "";

    const bannerStyle: CSSProperties | undefined =
        bannerHeightPx === undefined ? undefined : { height: `${bannerHeightPx}px` };

    const iconStyle: CSSProperties | undefined =
        iconSizePx === undefined
            ? undefined
            : { width: `${iconSizePx}px`, height: `${iconSizePx}px` };

    // Without an explicit size, the wordmark is sized off the icon so an icon+text lockup
    // stays balanced at any scale.
    const nameFontSize =
        nameFontSizePx
        ?? (iconSizePx === undefined
            ? undefined
            : Math.max(14, Math.floor(iconSizePx * 0.62)));

    const nameStyle: CSSProperties | undefined =
        nameFontSize === undefined ? undefined : { fontSize: `${nameFontSize}px` };

    return (
        <>
            {(variant === "banner" || variant === "responsive") && (
                <img
                    className={`g2h-brand-banner ${variant === "responsive" ? "d-none d-lg-block" : ""}`}
                    style={bannerStyle}
                    src="/assets/images/glory2him-banner.png"
                    alt="Glory 2 Him" />
            )}

            {(variant === "compact" || variant === "responsive") && (
                <span className={`g2h-brand-compact d-inline-flex align-items-center ${variant === "responsive" ? "d-lg-none" : ""}`}>
                    <img className="g2h-brand-icon rounded" style={iconStyle}
                        src="/assets/images/glory2him-icon.png" alt="" />

                    <span className="g2h-brand-name ms-2 fw-bold lh-1" style={nameStyle}>
                        Glory <span className={twoSpanClass}>2</span> Him
                    </span>
                </span>
            )}

            {variant === "text" && (
                <span className="g2h-brand-name fw-bold lh-1" style={nameStyle}>
                    Glory <span className={twoSpanClass}>2</span> Him
                </span>
            )}
        </>
    );
}
