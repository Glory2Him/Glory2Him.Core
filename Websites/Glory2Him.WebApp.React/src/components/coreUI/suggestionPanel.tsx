import { KeyboardEvent, useState } from 'react';
import { Link } from 'react-router-dom';
import './coreUI.css';

// A labelled set of pills — the article's tags or its bible references — with a box beneath for
// suggesting another. Anything suggested joins the end of the list carrying an hourglass instead
// of its usual icon, marking it as awaiting approval.
//
// Suggestions live only in this component's own state, so they disappear on refresh or when you
// navigate away. Wiring them to a real store is a separate job.
export interface SuggestionPanelProps {
    heading: string;
    suggestHeading?: string;
    prompt?: string;
    placeholder?: string;
    items?: ReadonlyArray<string>;
    itemCssClass?: string;

    // Left undefined for tags, which are prefixed with a hash instead of carrying an icon.
    itemIconCssClass?: string;
    prefixHash?: boolean;

    // Where an approved pill links to; {0} is the item, URL-escaped.
    hrefFormat?: string;
    onSuggested?: (suggestion: string) => void;
}

export function SuggestionPanel({
    heading,
    suggestHeading = '',
    prompt = '',
    placeholder = '',
    items = [],
    itemCssClass = 'btn-success-soft',
    itemIconCssClass,
    prefixHash = false,
    hrefFormat = 'Tag?name={0}',
    onSuggested,
}: SuggestionPanelProps) {
    const [pendingItems, setPendingItems] = useState<ReadonlyArray<string>>([]);
    const [draft, setDraft] = useState('');

    const displayName = (item: string) => (prefixHash ? `#${item}` : item);

    const buildHref = (item: string) =>
        '/' + hrefFormat.replace('{0}', encodeURIComponent(item)).replace(/^\//, '');

    const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
        if (event.key !== 'Enter') {
            return;
        }

        const suggestion = draft.trim().replace(/^#+/, '');

        setDraft('');

        if (suggestion.length === 0) {
            return;
        }

        // Neither an approved pill nor an already-pending one should be offered twice.
        const alreadyListed =
            items.some((item) => item.toLowerCase() === suggestion.toLowerCase())
                || pendingItems.some((item) => item.toLowerCase() === suggestion.toLowerCase());

        if (alreadyListed) {
            return;
        }

        setPendingItems([...pendingItems, suggestion]);
        onSuggested?.(suggestion);
    };

    // Withdrawing a suggestion is the mirror of making one, and just as local to this
    // component — nothing was stored, so nothing needs unstoring.
    const removeSuggestion = (item: string) =>
        setPendingItems(pendingItems.filter((pending) => pending !== item));

    return (
        <>
            <h4 className="mb-3">{heading}</h4>

            <div className="d-flex flex-wrap gap-2 mb-3">
                {items.map((item) => (
                    <Link key={item} to={buildHref(item)} className={`btn ${itemCssClass} g2h-suggest-pill mb-0`}>
                        {itemIconCssClass != null && itemIconCssClass.trim().length > 0 && (
                            <i className={`bi ${itemIconCssClass} me-1`}></i>
                        )}
                        {displayName(item)}
                    </Link>
                ))}

                {pendingItems.map((item) => (
                    // Only pending pills carry the cross: an approved reference is not the
                    // reader's to withdraw, whereas their own unapproved suggestion is.
                    <span
                        key={item}
                        className={`btn ${itemCssClass} g2h-suggest-pill g2h-suggest-pending mb-0`}
                        title="Pending approval"
                        aria-label={`${displayName(item)} — pending approval`}>
                        <i className="bi bi-hourglass-split me-1"></i>{displayName(item)}

                        <button
                            type="button"
                            className="g2h-suggest-remove ms-1"
                            title="Remove suggestion"
                            aria-label={`Remove ${displayName(item)}`}
                            onClick={() => removeSuggestion(item)}>
                            <i className="bi bi-x-lg" aria-hidden="true"></i>
                        </button>
                    </span>
                ))}
            </div>

            <p className="small text-uppercase fw-bold mb-1">{suggestHeading}</p>
            <p className="small mb-2">{prompt}</p>

            <div className="position-relative mb-2">
                <input
                    className="form-control"
                    type="text"
                    placeholder={placeholder}
                    value={draft}
                    onChange={(event) => setDraft(event.target.value)}
                    onKeyDown={onKeyDown}
                    aria-label={suggestHeading} />
            </div>
        </>
    );
}
