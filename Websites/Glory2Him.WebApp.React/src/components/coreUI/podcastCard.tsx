import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { Button } from './button';

// Podcast episode row: cover art, a round play button, the episode title and its duration
// (Blogzine index-10 / podcast-single). The play button is decorative in these demos — wire
// onPlay up to a real player when one exists.
export interface PodcastCardProps {
    post: PostView;

    // Episode length is presentation detail for the podcast layouts, not something PostView
    // carries — the caller supplies it rather than the domain model growing a field for it.
    duration?: string;
    onPlay?: () => void;
}

export function PodcastCard({ post, duration = '32:10', onPlay }: PodcastCardProps) {
    return (
        <div className="card border h-100">
            <div className="card-body d-flex align-items-center gap-3">
                <div className="position-relative flex-shrink-0">
                    <img
                        className="rounded"
                        src={post.imageUrl}
                        alt={post.title}
                        style={{ width: '80px', height: '80px', objectFit: 'cover' }} />

                    <Button
                        color="primary"
                        cssClass="btn-sm rounded-circle position-absolute top-50 start-50 translate-middle"
                        onClick={onPlay}>
                        <i className="bi bi-play-fill"></i>
                    </Button>
                </div>

                <div className="min-w-0">
                    <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-1`}>{post.category}</Link>

                    <h6 className="mb-1">
                        <Link to={`/Post-Single/${post.slug}`} className="btn-link text-reset">{post.title}</Link>
                    </h6>

                    <ul className="nav nav-divider align-items-center small mb-0">
                        <li className="nav-item">{post.authorName}</li>
                        <li className="nav-item">
                            <i className="bi bi-clock me-1"></i>{duration}
                        </li>
                    </ul>
                </div>
            </div>
        </div>
    );
}
