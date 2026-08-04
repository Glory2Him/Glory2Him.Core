import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { PostListItem } from '../../../components/coreUI/postListItem';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-list.html: horizontal rows down the main column, sidebar alongside.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

export const PostListSample = () => {
    useDocumentTitle('Post List — Sample — Glory 2 Him');

    const { posts, isLoading, isError, take } = useSamplePosts();

    return (
        <SampleShell title="Post List" sourceFile="post-list.html">
            <HeroBanner title="Post list" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : (
                        <div className="row g-4">
                            <div className="col-lg-8">
                                {posts.map((post) => (
                                    <PostListItem post={post} key={post.id} />
                                ))}

                                <div className="mt-4">
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
