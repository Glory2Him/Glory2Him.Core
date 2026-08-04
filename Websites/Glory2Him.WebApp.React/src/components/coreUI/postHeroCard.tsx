import { Link } from 'react-router-dom';
import { Avatar } from './avatar';
import { formatDate } from './dateFormats';

// Background-image hero card with the story sitting over a bottom gradient, optionally flagged
// with the featured star. Sizing rides on the theme's card-grid-lg / card-grid-sm classes, which
// set a fixed height under a `.card-grid` ancestor — so no h-100 here, it would override that
// height with a percentage of an auto-height column and collapse the card.
export interface PostHeroCardProps {
    title: string;
    href?: string;
    excerpt?: string;
    showExcerpt?: boolean;
    category?: string;
    categoryBadgeCss?: string;
    imageUrl?: string;
    authorName?: string;
    publishedDate?: Date;
    isFeatured?: boolean;

    // card-grid-lg for a half-page lead, card-grid-sm for the tiles beside it.
    sizeCssClass?: string;
    titleCssClass?: string;
    reactions?: number;
    comments?: number;
    tagCount?: number;
    referenceCount?: number;

    // Puts the engagement counts on their own row beneath the byline. The narrow hero tiles
    // need this; the full-width lead does not.
    splitMeta?: boolean;

    // The lead card shows the author's face beside their name; the smaller tiles have no room
    // for it and use the name alone.
    showAuthorAvatar?: boolean;
    authorImageUrl?: string;
}

export function PostHeroCard({
    title,
    href = '#',
    excerpt,
    showExcerpt = true,
    category = '',
    categoryBadgeCss = 'text-bg-primary',
    imageUrl = '',
    authorName = '',
    publishedDate,
    isFeatured = false,
    sizeCssClass = 'card-grid-lg',
    titleCssClass = 'h1',
    reactions,
    comments,
    tagCount,
    referenceCount,
    splitMeta = false,
    showAuthorAvatar = false,
    authorImageUrl,
}: PostHeroCardProps) {
    const hasCounts =
        reactions != null || comments != null || tagCount != null || referenceCount != null;

    const counts = (
        <>
            {reactions != null && (
                <li className="nav-item"><i className="far fa-heart me-1"></i>{reactions}</li>
            )}
            {comments != null && (
                <li className="nav-item"><i className="far fa-comment me-1"></i>{comments}</li>
            )}
            {tagCount != null && (
                <li className="nav-item"><i className="bi bi-tag me-1"></i>{tagCount}</li>
            )}
            {referenceCount != null && (
                <li className="nav-item"><i className="bi bi-book me-1"></i>{referenceCount}</li>
            )}
        </>
    );

    return (
        <div
            className={`card card-overlay-bottom ${sizeCssClass} card-bg-scale`}
            style={{
                backgroundImage: `url(${imageUrl})`,
                backgroundPosition: 'center center',
                backgroundSize: 'cover',
            }}>

            {isFeatured && (
                <span className="card-featured" title="Featured post">
                    <i className="fas fa-star"></i>
                </span>
            )}

            <div className="card-img-overlay d-flex align-items-center p-3 p-sm-4">
                <div className="w-100 mt-auto">
                    <Link to="/Categories" className={`badge ${categoryBadgeCss} mb-2`}>
                        <i className="fas fa-circle me-2 small fw-bold"></i>{category}
                    </Link>

                    <h2 className={`text-white ${titleCssClass}`}>
                        <Link to={href} className="btn-link stretched-link text-reset">{title}</Link>
                    </h2>

                    {showExcerpt && excerpt != null && excerpt.trim().length > 0 && (
                        <p className="text-white">{excerpt}</p>
                    )}

                    {/* On a narrow tile the byline and the counts will not fit on one line, so
                        splitMeta drops the counts onto a second row instead of letting them
                        overflow the card. */}
                    <ul className="nav nav-divider text-white-force align-items-center small mb-0 d-none d-sm-flex">
                        <li className="nav-item">
                            {showAuthorAvatar ? (
                                <div className="d-flex align-items-center text-white">
                                    <Avatar name={authorName} imageUrl={authorImageUrl} sizePx={32} />
                                    <span className="ms-2">by {authorName}</span>
                                </div>
                            ) : (
                                <>by {authorName}</>
                            )}
                        </li>
                        {publishedDate != null && (
                            <li className="nav-item">{formatDate(publishedDate)}</li>
                        )}

                        {!splitMeta && counts}
                    </ul>

                    {splitMeta && hasCounts && (
                        <ul className="nav nav-divider text-white-force align-items-center small mb-0 mt-1 d-none d-sm-flex">
                            {counts}
                        </ul>
                    )}
                </div>
            </div>
        </div>
    );
}
