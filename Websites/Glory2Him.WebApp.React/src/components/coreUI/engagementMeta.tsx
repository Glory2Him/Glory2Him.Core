import { Avatar } from './avatar';
import { formatDate } from './dateFormats';

// Divider-separated byline: avatar, author, date and whichever engagement counts are supplied.
// An omitted count is left out entirely rather than shown as a zero, so a card only ever claims
// numbers it actually has.
export interface EngagementMetaProps {
    authorName?: string;
    authorImageUrl?: string;
    showAuthor?: boolean;
    publishedDate?: Date;
    readMinutes?: number;
    reactions?: number;
    comments?: number;
    tagCount?: number;
    referenceCount?: number;
    views?: number;
    cssClass?: string;
}

export function EngagementMeta({
    authorName = '',
    authorImageUrl,
    showAuthor = true,
    publishedDate,
    readMinutes,
    reactions,
    comments,
    tagCount,
    referenceCount,
    views,
    cssClass = 'mb-0',
}: EngagementMetaProps) {
    return (
        <ul className={`nav nav-divider align-items-center small ${cssClass}`}>
            {showAuthor && (
                <li className="nav-item">
                    <div className="nav-link ps-0">
                        <div className="d-flex align-items-center position-relative">
                            <Avatar name={authorName} imageUrl={authorImageUrl} sizePx={24} />
                            <span className="ms-2">by {authorName}</span>
                        </div>
                    </div>
                </li>
            )}

            {publishedDate != null && (
                <li className="nav-item">{formatDate(publishedDate)}</li>
            )}

            {readMinutes != null && (
                <li className="nav-item">{readMinutes} min read</li>
            )}

            {reactions != null && (
                <li className="nav-item"><i className="far fa-heart me-1"></i>{reactions}</li>
            )}

            {comments != null && (
                <li className="nav-item"><i className="far fa-comment me-1"></i>{comments}</li>
            )}

            {tagCount != null && (
                <li className="nav-item"><i className="fas fa-tag me-1"></i>{tagCount}</li>
            )}

            {referenceCount != null && (
                <li className="nav-item"><i className="bi bi-book me-1"></i>{referenceCount}</li>
            )}

            {views != null && (
                <li className="nav-item"><i className="far fa-eye me-1"></i>{views} Views</li>
            )}
        </ul>
    );
}
