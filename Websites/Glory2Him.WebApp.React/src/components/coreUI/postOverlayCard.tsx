import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { formatDate } from './dateFormats';

// Post card whose title and byline sit over the image behind a bottom gradient (Blogzine
// "card-overlay-bottom card-img-scale"). Renders a single PostView.
export interface PostOverlayCardProps {
    post: PostView;

    // A Bootstrap heading utility ("h4", "h6", "display-6", …) so the same card can headline a
    // hero or sit in a four-across grid without changing its markup.
    titleCssClass?: string;
}

export function PostOverlayCard({ post, titleCssClass = 'h5' }: PostOverlayCardProps) {
    return (
        <div className="card card-overlay-bottom card-img-scale h-100">
            <img className="card-img" src={post.imageUrl} alt={post.title} />

            <div className="card-img-overlay d-flex align-items-center p-3 p-sm-4">
                <div className="w-100 mt-auto">
                    <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-2`}>{post.category}</Link>

                    {/* Sizing rides on a Bootstrap heading utility class rather than swapping the
                        tag, so the heading level stays consistent wherever the card is reused. */}
                    <h3 className={`text-white ${titleCssClass}`}>
                        <Link to={`/Post-Single/${post.slug}`} className="btn-link stretched-link text-reset fw-bold">
                            {post.title}
                        </Link>
                    </h3>

                    <ul className="nav nav-divider text-white-force align-items-center small mb-0">
                        <li className="nav-item">by {post.authorName}</li>
                        <li className="nav-item">{formatDate(post.publishedDate)}</li>
                    </ul>
                </div>
            </div>
        </div>
    );
}
