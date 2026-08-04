import { PostType } from '../../models/coreUI/postType';

// Small round icon marking what kind of post this is — video, audio, gallery or quote — as used
// on the Blogzine post-types layout.
export interface PostTypeBadgeProps {
    type: PostType;
    sizePx?: number;
}

const iconCssClasses: Record<PostType, string> = {
    Video: 'bi-play-fill',
    Audio: 'bi-mic-fill',
    Gallery: 'bi-images',
    Quote: 'bi-quote',
    Standard: 'bi-file-text-fill',
};

const backgroundCssClasses: Record<PostType, string> = {
    Video: 'text-bg-danger',
    Audio: 'text-bg-success',
    Gallery: 'text-bg-warning',
    Quote: 'text-bg-info',
    Standard: 'text-bg-primary',
};

export function PostTypeBadge({ type, sizePx = 36 }: PostTypeBadgeProps) {
    const label = `${type} post`;

    return (
        <span
            className={`badge rounded-circle ${backgroundCssClasses[type]} d-inline-flex align-items-center justify-content-center`}
            style={{ width: `${sizePx}px`, height: `${sizePx}px` }}
            role="img"
            aria-label={label}
            title={label}>
            <i className={`bi ${iconCssClasses[type]}`}></i>
        </span>
    );
}
