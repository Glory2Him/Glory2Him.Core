import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { PostCard } from '../../../components/coreUI/postCard';
import { PostLargeCard } from '../../../components/coreUI/postLargeCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-large-and-grid.html: a wide lead story, then the rest as a regular grid.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

export const PostMixedLargeThenGridSample = () => {
    useDocumentTitle('Post Mixed Large Then Grid — Sample — Glory 2 Him');

    const { lead, afterLead, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Mixed Large Then Grid" sourceFile="post-large-and-grid.html">
            <HeroBanner title="Large post, then grid" crumbs={crumbs} />

            <section className="py-5">
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
                            <div className="mb-5">
                                <PostLargeCard post={lead} titleCssClass="h2" />
                            </div>

                            <div className="row g-4">
                                {afterLead.map((post) => (
                                    <div className="col-sm-6 col-lg-4" key={post.id}>
                                        <PostCard post={post} showExcerpt={true} />
                                    </div>
                                ))}
                            </div>

                            <div className="mt-5">
                                <Pagination currentPage={1} totalPages={2} variant="PrevNext" />
                            </div>
                        </>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
