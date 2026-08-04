import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { PostOverlayCard } from '../../../components/coreUI/postOverlayCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-overlay.html: every card carries its title over the image.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

export const PostOverlaySample = () => {
    useDocumentTitle('Post Overlay — Sample — Glory 2 Him');

    const { lead, isLoading, isError, fill, take } = useSamplePosts();

    return (
        <SampleShell title="Post Overlay" sourceFile="post-overlay.html">
            <HeroBanner title="Post overlay" crumbs={crumbs} />

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
                            <div className="row g-4 mb-4">
                                {take(2).map((post) => (
                                    <div className="col-md-6" key={`lead-${post.id}`}>
                                        <PostOverlayCard post={post} titleCssClass="h4" />
                                    </div>
                                ))}
                            </div>

                            <div className="row g-4">
                                {fill(8).map((post, index) => (
                                    <div className="col-sm-6 col-lg-3" key={`grid-${post.id}-${index}`}>
                                        <PostOverlayCard post={post} titleCssClass="h6" />
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
