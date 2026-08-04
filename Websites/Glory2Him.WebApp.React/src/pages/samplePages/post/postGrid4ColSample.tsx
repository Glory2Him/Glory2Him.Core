import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { PostCard } from '../../../components/coreUI/postCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-grid-4-col.html: the same grid at four across, so cards drop their excerpt.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

export const PostGrid4ColSample = () => {
    useDocumentTitle('Post Grid 4 Col — Sample — Glory 2 Him');

    const { isLoading, isError, fill } = useSamplePosts();

    return (
        <SampleShell title="Post Grid 4 Col" sourceFile="post-grid-4-col.html">
            <HeroBanner title="Post grid 4 column" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : (
                        <>
                            <div className="row g-4">
                                {fill(12).map((post, index) => (
                                    <div className="col-sm-6 col-lg-3" key={`${post.id}-${index}`}>
                                        <PostCard post={post} />
                                    </div>
                                ))}
                            </div>

                            <div className="mt-5">
                                <Pagination currentPage={2} totalPages={4} />
                            </div>
                        </>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
