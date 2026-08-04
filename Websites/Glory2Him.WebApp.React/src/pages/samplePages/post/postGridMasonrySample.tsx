import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { PostCard } from '../../../components/coreUI/postCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { useIsotopeGrid } from '../../../hooks/useIsotopeGrid';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { PostView } from '../../../models/coreUI/postView';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-grid-masonry.html: masonry with no filter bar. Blazor leaned on the vendor
// isotope init running at DOMContentLoaded; here useIsotopeGrid owns that lifecycle.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

// The grid lives in its own component so useIsotopeGrid's effect runs when the loaded posts
// mount — on the page component it would fire once against the spinner and never again.
const MasonryGrid = ({ posts }: { posts: ReadonlyArray<PostView> }) => {
    const gridRef = useIsotopeGrid<HTMLDivElement>();

    return (
        <div
            ref={gridRef}
            className="row filter-container overflow-hidden"
            data-isotope='{"layoutMode": "masonry"}'>
            {posts.map((post, index) => (
                <div className="col-sm-6 col-lg-4 grid-item mb-4" key={`${post.id}-${index}`}>
                    <PostCard post={post} showExcerpt={true} />
                </div>
            ))}
        </div>
    );
};

export const PostGridMasonrySample = () => {
    useDocumentTitle('Post Grid Masonry — Sample — Glory 2 Him');

    const { isLoading, isError, fill } = useSamplePosts();

    return (
        <SampleShell title="Post Grid Masonry" sourceFile="post-grid-masonry.html">
            <HeroBanner title="Post grid masonry" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : (
                        <MasonryGrid posts={fill(9)} />
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
