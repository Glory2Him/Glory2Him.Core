import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { Avatar } from './avatar';
import { formatDate } from './dateFormats';

// Wide lead story: image on one side, headline and excerpt on the other (Blogzine
// post-large-and-grid). Flips to image-on-top on narrow screens.
export interface PostLargeCardProps {
    post: PostView;
    titleCssClass?: string;

    // Stacked below md so the image never shrinks to a sliver on a phone.
    imageFirst?: boolean;
}

export function PostLargeCard({ post, titleCssClass = 'h4', imageFirst = true }: PostLargeCardProps) {
    const imageColumnCssClass = imageFirst ? 'col-md-5' : 'col-md-5 order-md-2';
    const bodyColumnCssClass = imageFirst ? 'col-md-7' : 'col-md-7 order-md-1';

    return (
        <div className="card border h-100">
            <div className="row g-0 h-100">
                <div className={imageColumnCssClass}>
                    <img
                        className="w-100 h-100 rounded-start"
                        src={post.imageUrl}
                        alt={post.title}
                        style={{ objectFit: 'cover', minHeight: '220px' }} />
                </div>

                <div className={bodyColumnCssClass}>
                    <div className="card-body h-100 d-flex flex-column">
                        <div>
                            <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-2`}>{post.category}</Link>

                            <h2 className={titleCssClass}>
                                <Link
                                    to={`/Post-Single/${post.slug}`}
                                    className="btn-link text-reset stretched-link fw-bold">{post.title}</Link>
                            </h2>

                            <p className="card-text">{post.excerpt}</p>
                        </div>

                        <ul className="nav nav-divider align-items-center small mt-auto mb-0">
                            <li className="nav-item">
                                <div className="nav-link ps-0">
                                    <Avatar name={post.authorName} imageUrl={post.authorImageUrl} sizePx={28} />
                                </div>
                            </li>
                            <li className="nav-item">by {post.authorName}</li>
                            <li className="nav-item">{formatDate(post.publishedDate)}</li>
                            <li className="nav-item">{post.readMinutes} min read</li>
                        </ul>
                    </div>
                </div>
            </div>
        </div>
    );
}
