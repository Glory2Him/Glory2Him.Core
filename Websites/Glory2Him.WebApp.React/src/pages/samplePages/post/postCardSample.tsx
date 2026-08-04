import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { PostCard } from '../../../components/coreUI/postCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-cards.html: the plain card treatment at three sizes, so the same component
// can be compared across column widths.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Post cards', isActive: true },
];

export const PostCardSample = () => {
    useDocumentTitle('Post Card — Sample — Glory 2 Him');

    const { isLoading, isError, fill, take } = useSamplePosts();

    return (
        <SampleShell title="Post Card" sourceFile="post-cards.html">
            <HeroBanner title="Post cards" crumbs={crumbs} />

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
                            <h2 className="h5 mb-3">Two across, with excerpt</h2>
                            <div className="row g-4 mb-5">
                                {take(2).map((post) => (
                                    <div className="col-md-6" key={`two-${post.id}`}>
                                        <PostCard post={post} showExcerpt={true} />
                                    </div>
                                ))}
                            </div>

                            <h2 className="h5 mb-3">Three across</h2>
                            <div className="row g-4 mb-5">
                                {take(3).map((post) => (
                                    <div className="col-sm-6 col-lg-4" key={`three-${post.id}`}>
                                        <PostCard post={post} />
                                    </div>
                                ))}
                            </div>

                            <h2 className="h5 mb-3">Four across</h2>
                            <div className="row g-4">
                                {fill(4).map((post, index) => (
                                    <div className="col-sm-6 col-lg-3" key={`four-${post.id}-${index}`}>
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
