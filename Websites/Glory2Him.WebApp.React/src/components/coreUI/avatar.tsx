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

export function Avatar({ name, imageUrl, sizePx = 40, sizeCssClass = '' }: AvatarProps) {
    const fontSizePx = Math.max(10, Math.trunc(sizePx * 0.42));

    return (
        <div className={`avatar ${sizeCssClass}`} style={{ width: `${sizePx}px`, height: `${sizePx}px` }}>
            {imageUrl != null && imageUrl.trim().length > 0 ? (
                <img
                    className="avatar-img rounded-circle"
                    style={{ width: `${sizePx}px`, height: `${sizePx}px`, objectFit: 'cover' }}
                    src={imageUrl}
                    alt={name} />
            ) : (
                <span
                    className="avatar-img rounded-circle d-inline-flex align-items-center justify-content-center text-white fw-bold"
                    style={{
                        width: `${sizePx}px`,
                        height: `${sizePx}px`,
                        backgroundColor: computeBackgroundColor(name),
                        fontSize: `${fontSizePx}px`,
                    }}
                    role="img"
                    aria-label={name}
                    title={name}>
                    {computeInitials(name)}
                </span>
            )}
        </div>
    );
}
