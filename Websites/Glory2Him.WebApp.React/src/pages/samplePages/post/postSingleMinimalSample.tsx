import { Link } from 'react-router-dom';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { formatLongDate } from '../shared/sampleFormats';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-single-3.html: one narrow column, no sidebar — nothing between the reader
// and the words.
export const PostSingleMinimalSample = () => {
    useDocumentTitle('Post Single Minimal — Sample — Glory 2 Him');

    const { lead, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Single Minimal" sourceFile="post-single-3.html">
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
                            <div className="col-lg-7">
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
                </section>
            )}
        </SampleShell>
    );
};
