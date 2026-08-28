import { KeyboardEvent, useState } from 'react';
import './coreUI.css';

// A box that turns what you type into tags: press Enter and the word joins the list above as a
// pill, each carrying a red cross to take it back off again.
// The parent owns the tag list; only the in-progress draft is local state.
//
// Note for callers: this must not sit inside a <form> that has a submit button, or the browser
// will submit the form on Enter before the tag is ever added.
export interface TagInputProps {
    tags?: ReadonlyArray<string>;
    onTagsChange?: (tags: ReadonlyArray<string>) => void;
    placeholder?: string;
    ariaLabel?: string;
    tagCssClass?: string;

    // Decoration for each pill: a literal prefix ('#' for hashtags) or a leading icon
    // (bi-book for bible references). Neither is part of the stored value.
    tagPrefix?: string;
    tagIconCssClass?: string;
}

export function TagInput({
    tags = [],
    onTagsChange,
    placeholder = 'Type a tag and press Enter',
    ariaLabel = 'Add a tag',
    tagCssClass = 'btn-success-soft',
    tagPrefix = '',
    tagIconCssClass,
}: TagInputProps) {
    const [draft, setDraft] = useState('');

    const onKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
        if (event.key !== 'Enter') {
            return;
        }

        // A leading hash is how people write tags, but it is not part of the tag itself.
        const tag = draft.trim().replace(/^#+/, '');

        setDraft('');

        const alreadyListed = tags.some(
            (listed) => listed.toLowerCase() === tag.toLowerCase());

        if (tag.length === 0 || alreadyListed) {
            return;
        }

        onTagsChange?.([...tags, tag]);
    };

    const removeTag = (tag: string) =>
        onTagsChange?.(tags.filter((listed) => listed !== tag));

    return (
        <>
            {tags.length > 0 && (
                <div className="d-flex flex-wrap gap-2 mb-2">
                    {tags.map((tag) => (
                        <span key={tag} className={`btn ${tagCssClass} g2h-tag-pill mb-0`}>
                            {tagIconCssClass != null && (
                                <i className={`bi ${tagIconCssClass} me-1`} aria-hidden="true"></i>
                            )}
                            {tagPrefix}{tag}

                            <button
                                type="button"
                                className="g2h-tag-remove ms-1"
                                title="Remove tag"
                                aria-label={`Remove ${tag}`}
                                onClick={() => removeTag(tag)}>
                                <i className="bi bi-x-lg" aria-hidden="true"></i>
                            </button>
                        </span>
                    ))}
                </div>
            )}

            <input
                className="form-control"
                type="text"
                placeholder={placeholder}
                value={draft}
                onChange={(event) => setDraft(event.target.value)}
                onKeyDown={onKeyDown}
                aria-label={ariaLabel} />
        </>
    );
}
