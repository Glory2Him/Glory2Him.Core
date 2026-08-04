import { Link } from 'react-router-dom';
import { Card } from '../../../components/coreUI/card';
import { formatDate } from '../../../components/coreUI/dateFormats';
import { PostCard } from '../../../components/coreUI/postCard';
import { PostLargeCard } from '../../../components/coreUI/postLargeCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine index-8.html: the tech front page — a category strip, a large lead beside a
// compact list, then an even grid.

const topics: ReadonlyArray<string> =
    ['Faith', 'Hope', 'Prayer', 'Scripture', 'Testimony', 'Worship', 'Community'];

export const HomeBlogTechSample = () => {
    useDocumentTitle('Blog Tech — Sample — Glory 2 Him');

    const { lead, afterLead, isLoading, isError, fill } = useSamplePosts();

    return (
        <SampleShell title="Blog Tech" sourceFile="index-8.html">
            <section className="pt-4 pb-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : lead == null ? (
                        <div className="alert alert-info" role="alert">
                            No posts have been published yet.
                        </div>
                    ) : (
                        <>
                            <ul className="list-inline mb-4">
                                {topics.map((topic) => (
                                    <li className="list-inline-item mb-2" key={topic}>
                                        <Link
                                            to={`/Tag?name=${encodeURIComponent(topic)}`}
                                            className="btn btn-sm btn-primary-soft">
                                            {topic}
                                        </Link>
                                    </li>
                                ))}
                            </ul>

                            <div className="row g-4 mb-5">
                                <div className="col-lg-8">
                                    <PostLargeCard post={lead} titleCssClass="h3" />
                                </div>
                                <div className="col-lg-4">
                                    <Card cssClass="border h-100" headerContent="Also this week">
                                        {afterLead.slice(0, 4).map((post) => (
                                            <div className="d-flex gap-3 mb-3 position-relative" key={post.id}>
                                                <img
                                                    className="rounded flex-shrink-0"
                                                    src={post.imageUrl}
                                                    alt={post.title}
                                                    style={{ width: '64px', height: '64px', objectFit: 'cover' }} />
                                                <div>
                                                    <h6 className="mb-1">
                                                        <Link
                                                            to={`/Post-Single/${post.slug}`}
                                                            className="btn-link text-reset stretched-link">
                                                            {post.title}
                                                        </Link>
                                                    </h6>
                                                    <small className="text-body-secondary">
                                                        {formatDate(post.publishedDate)}
                                                    </small>
                                                </div>
                                            </div>
                                        ))}
                                    </Card>
                                </div>
                            </div>

                            <h2 className="h4 mb-4">More from the journal</h2>

                            <div className="row g-4">
                                {fill(8).map((post, index) => (
                                    <div className="col-sm-6 col-lg-3" key={`${post.id}-${index}`}>
                                        <PostCard post={post} />
                                    </div>
                                ))}
                            </div>
                        </>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
