// Reusable avatar. Renders the user's profile image when one is set, otherwise a deterministic
// initials circle (colour derived from the name).

// A calm, readable palette; the name selects one deterministically so a user always gets
// the same colour.
const palette = [
    '#2163e8', '#0cbc87', '#d6293e', '#f7c32e',
    '#4f42b5', '#0d6efd', '#20c997', '#fd7e14',
];

export interface AvatarProps {
    name: string;
    imageUrl?: string;
    sizePx?: number;
    sizeCssClass?: string;

    // A GLYPH INSTEAD OF INITIALS, for an identity that is not a person — the AI reviewer of
    // design §8.6.2 is the first. Initials are a stand-in for a face, and "BE" over a name-hashed
    // colour would present an automated identity as one more colleague in the list. The glyph
    // sits on the neutral theme surface rather than a palette colour for the same reason: the
    // palette is how people are told apart, and this is not one of them.
    iconCssClass?: string;

    // DECORATIVE, for the case where the name is already written beside the avatar. Without it
    // every such row announces the person twice — role="img" with the display name as its
    // accessible name, then the same name as text — and inside a <button> that duplicate lands
    // in the button's own accessible name too. The circle carries no information the adjacent
    // text does not, so it is hidden from assistive technology rather than repeated.
    isDecorative?: boolean;
}

function computeInitials(name: string): string {
    const trimmed = name.trim();

    if (trimmed.length === 0) {
        return '?';
    }

    const parts = trimmed.split(/[ \-_.]+/).filter((part) => part.length > 0);

    if (parts.length >= 2) {
        return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    }

    const single = parts[0];

    return (single.length >= 2 ? single.slice(0, 2) : single.slice(0, 1)).toUpperCase();
}

function computeBackgroundColor(name: string): string {
    const key = name.trim().toLowerCase();

    // Stable, framework-independent hash so the colour never shifts between runs.
    // Matches the Blazor implementation: 32-bit int overflow arithmetic via Math.imul.
    let hash = 17;

    for (let index = 0; index < key.length; index++) {
        hash = (Math.imul(hash, 31) + key.charCodeAt(index)) | 0;
    }

    return palette[Math.abs(hash) % palette.length];
}

export function Avatar({
    name,
    imageUrl,
    sizePx = 40,
    sizeCssClass = '',
    iconCssClass,
    isDecorative = false
}: AvatarProps) {
    const fontSizePx = Math.max(10, Math.trunc(sizePx * 0.42));

    // Either a labelled image or hidden outright — never an unlabelled one, and never a
    // half-measure that drops the label but keeps the role.
    const semantics = isDecorative
        ? { 'aria-hidden': true }
        : { role: 'img', 'aria-label': name, title: name };

    return (
        <div className={`avatar ${sizeCssClass}`} style={{ width: `${sizePx}px`, height: `${sizePx}px` }}>
            {iconCssClass != null && iconCssClass.length > 0 ? (
                <span
                    className={
                        'avatar-img rounded-circle d-inline-flex align-items-center '
                        + 'justify-content-center'}
                    style={{
                        width: `${sizePx}px`,
                        height: `${sizePx}px`,

                        // Theme tokens, never literal greys — but NOT the secondary PAIR, which
                        // is what this started as. The Blogzine theme redefines
                        // --bs-secondary-color to #d0d4d9 in :root, so the glyph landed at
                        // 1.32:1 on its own #f0f1f3 disc and the avatar was simply missing in
                        // light mode. Measured in the running app with the theme loaded, body
                        // colour on the secondary surface gives 5.82:1 light and 4.48:1 dark —
                        // both clear WCAG 1.4.11's 3:1 for a meaningful graphic. (Dark is the
                        // narrower of the two because the theme resolves --bs-body-color to
                        // #a1a1a8 there, not to the #dee2e6 the palette block first declares.)
                        backgroundColor: 'var(--bs-secondary-bg)',
                        color: 'var(--bs-body-color)',
                        fontSize: `${fontSizePx}px`,
                    }}
                    {...semantics}>
                    <i className={`bi ${iconCssClass}`} aria-hidden="true"></i>
                </span>
            ) : imageUrl != null && imageUrl.trim().length > 0 ? (
                <img
                    className="avatar-img rounded-circle"
                    style={{ width: `${sizePx}px`, height: `${sizePx}px`, objectFit: 'cover' }}
                    src={imageUrl}
                    alt={isDecorative ? '' : name} />
            ) : (
                <span
                    className="avatar-img rounded-circle d-inline-flex align-items-center justify-content-center text-white fw-bold"
                    style={{
                        width: `${sizePx}px`,
                        height: `${sizePx}px`,
                        backgroundColor: computeBackgroundColor(name),
                        fontSize: `${fontSizePx}px`,
                    }}
                    {...semantics}>
                    {computeInitials(name)}
                </span>
            )}
        </div>
    );
}
