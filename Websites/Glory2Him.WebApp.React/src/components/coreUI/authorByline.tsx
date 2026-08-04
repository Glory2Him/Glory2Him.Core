import { Link } from 'react-router-dom';
import { Avatar } from './avatar';
import { formatDate } from './dateFormats';

// Horizontal article byline: the author's face and name, then the article's date, reading time and
// engagement counts running along beside them. Wraps onto a second line on narrow screens rather
// than squeezing.
export interface AuthorBylineProps {
    authorName: string;
    authorRole?: string;
    authorImageUrl?: string;
    avatarSizePx?: number;

    // Each figure is optional: undefined leaves it out rather than rendering a zero.
    publishedDate?: Date;
    readMinutes?: number;
    reactions?: number;
    comments?: number;
    views?: number;
    cssClass?: string;
}

export function AuthorByline({
    authorName,
    authorRole,
    authorImageUrl,
    avatarSizePx = 44,
    publishedDate,
    readMinutes,
    reactions,
    comments,
    views,
    cssClass = '',
}: AuthorBylineProps) {
    return (
        <div className={`d-flex flex-wrap align-items-center gap-3 ${cssClass}`}>
            <div className="d-flex align-items-center">
                <Avatar name={authorName} imageUrl={authorImageUrl} sizePx={avatarSizePx} />

                <span className="ms-2">
                    <Link to="/Author" className="fw-bold text-reset btn-link d-block lh-1">{authorName}</Link>

                    {authorRole != null && authorRole.trim().length > 0 && (
                        <span className="small">{authorRole}</span>
                    )}
                </span>
            </div>

            <ul className="nav nav-divider align-items-center mb-0">
                {publishedDate != null && (
                    <li className="nav-item">{formatDate(publishedDate)}</li>
                )}

                {readMinutes != null && (
                    <li className="nav-item">
                        <i className="bi bi-clock-fill me-1"></i>{readMinutes} min read
                    </li>
                )}

                {reactions != null && (
                    <li className="nav-item"><i className="far fa-heart me-1"></i>{reactions} reactions</li>
                )}

                {comments != null && (
                    <li className="nav-item"><i className="far fa-comment me-1"></i>{comments} comments</li>
                )}

                {views != null && (
                    <li className="nav-item"><i className="far fa-eye me-1"></i>{views} Views</li>
                )}
            </ul>
        </div>
    );
}
