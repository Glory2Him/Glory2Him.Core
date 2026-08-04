import { Link } from 'react-router-dom';
import { Button } from '../../../components/coreUI/button';
import { PostTypeBadge } from '../../../components/coreUI/postTypeBadge';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { formatLongDate } from '../shared/sampleFormats';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-single-6.html: a video sits where the hero image usually would. The frame is
// a placeholder — no third-party player is embedded in these demos.
export const PostSingleVideoSample = () => {
    useDocumentTitle('Post Single Video — Sample — Glory 2 Him');

    const { lead, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Single Video" sourceFile="post-single-6.html">
            {isLoading ? (
                <div className="text-center py-5"><Spinner /></div>
            ) : isError ? (
                <div className="alert alert-danger m-4" role="alert">
                    We could not load posts right now. Please try again later.
                </div>
            ) : lead == null ? (
                <div className="alert alert-info m-4" role="alert">
                    No posts have been published yet.
                </div>
            ) : (
                <section className="py-5">
                    <div className="container">
                        <div className="row justify-content-center">
                            <div className="col-lg-9">
                                <div className="position-relative rounded overflow-hidden mb-4">
                                    <img
                                        className="w-100"
                                        src={lead.imageUrl}
                                        alt={lead.title}
                                        style={{ maxHeight: '460px', objectFit: 'cover' }} />

                                    <div
                                        className="position-absolute top-0 start-0 w-100 h-100 d-flex align-items-center justify-content-center"
                                        style={{ background: 'rgba(10,12,20,0.45)' }}>
                                        <Button color="light" cssClass="rounded-circle p-3 lh-1">
                                            <i className="bi bi-play-fill fs-1"></i>
                                        </Button>
                                    </div>

                                    <span className="position-absolute top-0 end-0 m-3">
                                        <PostTypeBadge type="Video" />
                                    </span>
                                </div>

                                <Link to="/Categories" className={`badge ${lead.categoryBadgeCss} mb-3`}>
                                    {lead.category}
                                </Link>

                                <h1 className="h2 mb-3">{lead.title}</h1>

                                <ul className="nav nav-divider align-items-center small mb-4">
                                    <li className="nav-item">by {lead.authorName}</li>
                                    <li className="nav-item">{formatLongDate(lead.publishedDate)}</li>
                                    <li className="nav-item">12:04 watch</li>
                                </ul>

                                <SampleArticleBody post={lead} />
                            </div>
                        </div>
                    </div>
                </section>
            )}
        </SampleShell>
    );
};
