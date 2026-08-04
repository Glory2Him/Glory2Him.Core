import { Link } from 'react-router-dom';
import { BlogSidebar } from '../../components/coreUI/blogSidebar';
import MegaMenuComponent from '../../components/coreUI/megaMenu';
import { Pagination } from '../../components/coreUI/pagination';
import { PostCard } from '../../components/coreUI/postCard';
import { PostOverlayCard } from '../../components/coreUI/postOverlayCard';
import { Spinner } from '../../components/coreUI/spinner';
import { useDocumentTitle } from '../useDocumentTitle';
import { SampleShell } from './shared/sampleShell';
import { useSamplePosts } from './shared/useSamplePosts';

// The Blogzine "Lifestyle" category treatment: a featured hero, the category grid and the
// topic pill row from its mega-menu panel — plus the mega menu itself, demonstrated live in
// the strip below so the dropdown behaviour can be seen rather than just described.

const topics: ReadonlyArray<string> =
    ['Faith', 'Hope', 'Prayer', 'Scripture', 'Testimony', 'Worship', 'Community'];

export const LifestyleSample = () => {
    useDocumentTitle('Lifestyle — Sample — Glory 2 Him');

    const { lead, isLoading, isError, fill, take } = useSamplePosts();

    return (
        <SampleShell title="Lifestyle" sourceFile="categories.html + mega menu">
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
                    {/* The mega menu is a nav-item, so it needs a navbar around it to open
                        correctly. */}
                    <nav className="navbar navbar-expand-lg border-bottom py-0">
                        <div className="container">
                            <span className="small text-body-secondary me-3">Mega menu demo:</span>
                            <ul className="navbar-nav">
                                <MegaMenuComponent title="Lifestyle" posts={take(4)} topics={[...topics]} />
                            </ul>
                        </div>
                    </nav>

                    <section className="pt-4">
                        <div className="container">
                            <PostOverlayCard post={lead} titleCssClass="display-6" />
                        </div>
                    </section>

                    <section className="py-5">
                        <div className="container">
                            <div className="d-flex flex-wrap align-items-center justify-content-between mb-4">
                                <h2 className="h4 mb-0">In Lifestyle</h2>
                                <ul className="list-inline mb-0">
                                    {topics.map((topic) => (
                                        <li className="list-inline-item" key={topic}>
                                            <Link
                                                to={`/Tag?name=${encodeURIComponent(topic)}`}
                                                className="btn btn-sm btn-primary-soft">
                                                {topic}
                                            </Link>
                                        </li>
                                    ))}
                                </ul>
                            </div>

                            <div className="row g-4">
                                <div className="col-lg-8">
                                    <div className="row g-4">
                                        {fill(6).map((post, index) => (
                                            <div className="col-sm-6" key={`${post.id}-${index}`}>
                                                <PostCard post={post} showExcerpt={true} />
                                            </div>
                                        ))}
                                    </div>

                                    <div className="mt-4">
                                        <Pagination currentPage={1} totalPages={3} variant="Rounded" />
                                    </div>
                                </div>

                                <div className="col-lg-4">
                                    <BlogSidebar trendingPosts={take(4)} topics={topics} />
                                </div>
                            </div>
                        </div>
                    </section>
                </>
            )}
        </SampleShell>
    );
};
