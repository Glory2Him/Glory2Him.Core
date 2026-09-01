import './contentItems.css';

// The invitation to contribute, as a panel: icon, title, description and the way in. A pure
// presentation component — it raises onSubmit and the PAGE decides where that leads, which for
// every shipped consumer is /posts/contribute.
//
// IT ADAPTS TO ITS CONTAINER, not the viewport, via a CSS container query — the same panel sits
// full-width above a feed and in a sidebar column, and only its own width says which it is.
// Wide, the description and the button share a row with the button on the right; narrow, they
// stack. The icon lives INLINE in the heading, so a narrow title wraps naturally to underneath
// it, and the button's text never wraps however tight things get.
export interface SharingPanelProps {
    // The css to render the icon with — any icon-font class the page's stylesheets carry.
    iconCss?: string;

    title?: string;
    description?: string;
    buttonText?: string;

    // Raised when the button is pressed. The consumer navigates — /posts/contribute on every
    // page that ships one.
    onSubmit?: () => void;

    cssClass?: string;
}

export function SharingPanel({
    iconCss = 'bi bi-pencil-square',
    title = 'Have something to share?',
    description = 'A quote, a story, a testimony, or a verse that carried you through — if it '
        + 'might encourage someone else, we would love to read it.',
    buttonText = 'Submit a contribution',
    onSubmit,
    cssClass = 'mb-4'
}: SharingPanelProps) {
    return (
        <section
            className={`g2h-sharing-panel border rounded-3 bg-body p-3 p-lg-4 ${cssClass}`}
            aria-label={title}>

            <h3 className="h4 mb-2">
                <i className={`${iconCss} text-primary me-2`} aria-hidden="true"></i>
                {title}
            </h3>

            <div className="g2h-sharing-panel-body">
                <p className="mb-0">{description}</p>

                <button
                    type="button"
                    className="btn btn-primary-soft fw-bold text-nowrap mb-0"
                    onClick={() => onSubmit?.()}>
                    {buttonText}
                    <i className="bi bi-arrow-right ms-2" aria-hidden="true"></i>
                </button>
            </div>
        </section>
    );
}
