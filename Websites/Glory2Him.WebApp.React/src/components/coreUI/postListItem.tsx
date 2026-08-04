import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { formatDate } from './dateFormats';

// Reusable horizontal post row (Blogzine "Post-List" item). Renders a single PostView.
export interface PostListItemProps {
    post: PostView;
}

export function PostListItem({ post }: PostListItemProps) {
    const postHref = `/Post-Single/${post.slug}`;

    return (
        <div className="card mb-4">
            <div className="row g-0">
                <div className="col-md-4">
                    <img className="rounded-3 h-100 object-fit-cover" src={post.imageUrl} alt={post.title} />
                </div>
                <div className="col-md-8">
                    <div className="card-body h-100 d-flex flex-column">
                        <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-2 align-self-start`}>
                            {post.category}
                        </Link>
                        <h3 className="card-title">
                            <Link to={postHref} className="btn-link text-reset stretched-link fw-bold">{post.title}</Link>
                        </h3>
                        <p className="card-text">{post.excerpt}</p>
                        <ul className="nav nav-divider align-items-center mt-auto">
                            <li className="nav-item">
                                <div className="nav-link">
                                    <div className="avatar avatar-xs">
                                        <img className="avatar-img rounded-circle" src={post.authorImageUrl} alt={post.authorName} />
                                    </div>
                                </div>
                            </li>
                            <li className="nav-item">by <span className="text-reset btn-link">{post.authorName}</span></li>
                            <li className="nav-item">{formatDate(post.publishedDate)}</li>
                            <li className="nav-item">{post.readMinutes} min read</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
}
