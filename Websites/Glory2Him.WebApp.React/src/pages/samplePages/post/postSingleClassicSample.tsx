import { Link } from 'react-router-dom';
import { Avatar } from '../../../components/coreUI/avatar';
import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { formatLongDate } from '../shared/sampleFormats';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-single-2.html: headline above a standard image, article and sidebar below.
export const PostSingleClassicSample = () => {
    useDocumentTitle('Post Single Classic — Sample — Glory 2 Him');

    const { lead, isLoading, isError, take } = useSamplePosts();

    return (
        <SampleShell title="Post Single Classic" sourceFile="post-single-2.html">
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
                <>
                    <section className="pt-5 pb-4">
                        <div className="container">
                            <div className="row justify-content-center text-center">
                                <div className="col-lg-9">
                                    <Link to="/Categories" className={`badge ${lead.categoryBadgeCss} mb-3`}>
                                        {lead.category}
                                    </Link>
                                    <h1 className="display-6 mb-3">{lead.title}</h1>

                                    <ul className="nav nav-divider align-items-center justify-content-center mb-0">
                                        <li className="nav-item">
                                            <div className="nav-link">
                                                <Avatar
                                                    name={lead.authorName}
                                                    imageUrl={lead.authorImageUrl}
                                                    sizePx={28} />
                                            </div>
                                        </li>
                                        <li className="nav-item">by {lead.authorName}</li>
                                        <li className="nav-item">{formatLongDate(lead.publishedDate)}</li>
                                        <li className="nav-item">{lead.readMinutes} min read</li>
                                    </ul>
                                </div>
                            </div>
                        </div>
                    </section>

                    <section className="pb-5">
                        <div className="container">
                            <img
                                className="rounded w-100 mb-5"
                                src={lead.imageUrl}
                                alt={lead.title}
                                style={{ maxHeight: '480px', objectFit: 'cover' }} />

                            <div className="row g-4">
                                <div className="col-lg-8">
                                    <SampleArticleBody post={lead} />
                                </div>
                                <div className="col-lg-4">
                                    <BlogSidebar trendingPosts={take(4)} />
                                </div>
                            </div>
                        </div>
                    </section>
                </>
            )}
        </SampleShell>
    );
};
