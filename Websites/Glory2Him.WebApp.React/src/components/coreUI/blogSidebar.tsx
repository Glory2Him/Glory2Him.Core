import { Link } from 'react-router-dom';
import { PostView } from '../../models/coreUI/postView';
import { Avatar } from './avatar';
import { Card } from './card';
import { formatDate } from './dateFormats';
import { Newsletter } from './newsletter';

// The Blogzine blog sidebar: an about-the-author card, a numbered "trending" list, a topic pill
// cloud and the newsletter sign-up. Posts are supplied by the parent.
export interface BlogSidebarProps {
    trendingPosts?: ReadonlyArray<PostView>;
    topics?: ReadonlyArray<string>;
    showAbout?: boolean;
    showNewsletter?: boolean;
}

const defaultTopics = ['Faith', 'Hope', 'Prayer', 'Scripture', 'Testimony', 'Worship'];

export function BlogSidebar({
    trendingPosts = [],
    topics = defaultTopics,
    showAbout = true,
    showNewsletter = true,
}: BlogSidebarProps) {
    return (
        <div className="row g-4">
            {showAbout && (
                <div className="col-12">
                    <Card cssClass="border" headerContent="About">
                        <div className="text-center">
                            <Avatar name="Glory 2 Him" sizePx={72} sizeCssClass="mx-auto mb-3" />
                            <h6 className="mb-1">Glory 2 Him</h6>
                            <p className="small text-body-secondary mb-0">
                                Stories, reflections and encouragement — all glory to Him.
                            </p>
                        </div>
                    </Card>
                </div>
            )}

            {trendingPosts.length > 0 && (
                <div className="col-12">
                    <Card cssClass="border" headerContent="Trending">
                        {trendingPosts.map((post, index) => (
                            <div key={post.slug} className="d-flex mb-3 position-relative">
                                <span className="h4 text-body-secondary opacity-50 me-3 mb-0">
                                    {String(index + 1).padStart(2, '0')}
                                </span>
                                <div>
                                    <h6 className="mb-1">
                                        <Link
                                            to={`/Post-Single/${post.slug}`}
                                            className="btn-link text-reset stretched-link">{post.title}</Link>
                                    </h6>
                                    <small className="text-body-secondary">
                                        {formatDate(post.publishedDate)}
                                    </small>
                                </div>
                            </div>
                        ))}
                    </Card>
                </div>
            )}

            {topics.length > 0 && (
                <div className="col-12">
                    <Card cssClass="border" headerContent="Topics">
                        <ul className="list-inline mb-0">
                            {topics.map((topic) => (
                                <li key={topic} className="list-inline-item mb-2">
                                    <Link
                                        to={`/Tag?name=${encodeURIComponent(topic)}`}
                                        className="btn btn-sm btn-outline-secondary">{topic}</Link>
                                </li>
                            ))}
                        </ul>
                    </Card>
                </div>
            )}

            {showNewsletter && (
                <div className="col-12">
                    <Newsletter
                        heading="Weekly encouragement"
                        subheading="A short note of hope in your inbox." />
                </div>
            )}
        </div>
    );
}
