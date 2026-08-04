import { Link } from 'react-router-dom';
import { EngagementMeta } from './engagementMeta';
import { TagPillList } from './tagPillList';

// Listing card: image with the category badge over it, then title, excerpt, the post's hashtags
// and bible references, and the byline.
export interface ArticleCardProps {
    title: string;
    href?: string;
    excerpt?: string;
    imageUrl?: string;
    category?: string;
    categoryBadgeCss?: string;
    authorName?: string;
    authorImageUrl?: string;
    publishedDate?: Date;
    tags?: ReadonlyArray<string>;
    bibleReferences?: ReadonlyArray<string>;
    reactions?: number;
    comments?: number;
}

export function ArticleCard({
    title,
    href = '#',
    excerpt,
    imageUrl = '',
    category = '',
    categoryBadgeCss = 'text-bg-primary',
    authorName = '',
    authorImageUrl,
    publishedDate,
    tags = [],
    bibleReferences = [],
    reactions,
    comments,
}: ArticleCardProps) {
    return (
        <div className="card h-100">
            <div className="position-relative">
                <img className="card-img" src={imageUrl} alt={title} />

                <div className="card-img-overlay d-flex align-items-start flex-column p-3">
                    <div className="w-100 mt-auto">
                        <Link to="/Categories" className={`badge ${categoryBadgeCss} mb-2`}>
                            <i className="fas fa-circle me-2 small fw-bold"></i>{category}
                        </Link>
                    </div>
                </div>
            </div>

            <div className="card-body px-0 pt-3 pb-0 d-flex flex-column">
                <h4 className="card-title mt-0 mb-2">
                    <Link to={href} className="btn-link text-reset fw-bold">{title}</Link>
                </h4>

                {excerpt != null && excerpt.trim().length > 0 && (
                    <p className="card-text mb-2">{excerpt}</p>
                )}

                {(tags.length > 0 || bibleReferences.length > 0) && (
                    <TagPillList tags={tags} bibleReferences={bibleReferences} />
                )}

                <div className="mt-auto">
                    <EngagementMeta
                        authorName={authorName}
                        authorImageUrl={authorImageUrl}
                        publishedDate={publishedDate}
                        reactions={reactions}
                        comments={comments} />
                </div>
            </div>
        </div>
    );
}
