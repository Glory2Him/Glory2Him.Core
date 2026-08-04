import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { PostLargeCard } from '../../../components/coreUI/postLargeCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine index-5.html: the classic blog roll — one wide post after another down the main
// column, with the sidebar alongside.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Blog Classic', isActive: true },
];

export const HomeBlogClassicSample = () => {
    useDocumentTitle('Blog Classic — Sample — Glory 2 Him');

    const { posts, isLoading, isError, take } = useSamplePosts();

    return (
        <SampleShell title="Blog Classic" sourceFile="index-5.html">
            <HeroBanner title="Blog Classic" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : posts.length === 0 ? (
                        <div className="alert alert-info" role="alert">
                            No posts have been published yet.
                        </div>
                    ) : (
                        <div className="row g-4">
                            <div className="col-lg-8">
                                <div className="vstack gap-4">
                                    {posts.map((post) => (
                                        <PostLargeCard post={post} key={post.id} />
                                    ))}
                                </div>

                                <div className="mt-5">
                                    <Pagination currentPage={1} totalPages={3} />
                                </div>
                            </div>

                            <div className="col-lg-4">
                                <BlogSidebar trendingPosts={take(4)} />
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
