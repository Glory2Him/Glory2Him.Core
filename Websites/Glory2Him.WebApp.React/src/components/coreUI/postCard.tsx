import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { formatDate } from './dateFormats';

// Reusable blog post card (Blogzine "card-grid" item). Renders a single PostView.
export interface PostCardProps {
    post: PostView;
    showExcerpt?: boolean;
}

export function PostCard({ post, showExcerpt = false }: PostCardProps) {
    const postHref = `/Post-Single/${post.slug}`;

    return (
        <div className="card h-100">
            <div className="position-relative">
                <img className="card-img" src={post.imageUrl} alt={post.title} />
                <div className="card-img-overlay d-flex align-items-start flex-column p-3">
                    <div className="w-100 align-items-center text-start">
                        <Link to="/Categories" className={`badge ${post.categoryBadgeCss}`}>{post.category}</Link>
                    </div>
                </div>
            </div>
            <div className="card-body px-3 pt-3">
                <h4 className="card-title">
                    <Link to={postHref} className="btn-link text-reset stretched-link fw-bold">{post.title}</Link>
                </h4>
                {showExcerpt && (
                    <p className="card-text">{post.excerpt}</p>
                )}
                <ul className="nav nav-divider align-items-center">
                    <li className="nav-item">
                        <div className="nav-link">
                            <div className="avatar avatar-xs">
                                <img className="avatar-img rounded-circle" src={post.authorImageUrl} alt={post.authorName} />
                            </div>
                        </div>
                    </li>
                    <li className="nav-item">
                        by <span className="text-reset btn-link">{post.authorName}</span>
                    </li>
                    <li className="nav-item">{formatDate(post.publishedDate)}</li>
                </ul>
            </div>
        </div>
    );
}
