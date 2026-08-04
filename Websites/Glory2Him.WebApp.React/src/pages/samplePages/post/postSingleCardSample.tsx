import { Link } from 'react-router-dom';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { formatLongDate } from '../shared/sampleFormats';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-single-4.html: the article floats in a raised card over a tinted background.
export const PostSingleCardSample = () => {
    useDocumentTitle('Post Single Card — Sample — Glory 2 Him');

    const { lead, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Single Card" sourceFile="post-single-4.html">
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
                <section className="py-5 bg-body-tertiary">
                    <div className="container">
                        <div className="row justify-content-center">
                            <div className="col-lg-9">
                                <div className="card border shadow-sm overflow-hidden">
                                    <img
                                        className="card-img-top"
                                        src={lead.imageUrl}
                                        alt={lead.title}
                                        style={{ maxHeight: '400px', objectFit: 'cover' }} />

                                    <div className="card-body p-4 p-lg-5">
                                        <Link to="/Categories" className={`badge ${lead.categoryBadgeCss} mb-3`}>
                                            {lead.category}
                                        </Link>

                                        <h1 className="h2 mb-3">{lead.title}</h1>

                                        <ul className="nav nav-divider align-items-center small mb-4">
                                            <li className="nav-item">by {lead.authorName}</li>
                                            <li className="nav-item">{formatLongDate(lead.publishedDate)}</li>
                                            <li className="nav-item">{lead.readMinutes} min read</li>
                                        </ul>

                                        <SampleArticleBody post={lead} />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>
            )}
        </SampleShell>
    );
};
