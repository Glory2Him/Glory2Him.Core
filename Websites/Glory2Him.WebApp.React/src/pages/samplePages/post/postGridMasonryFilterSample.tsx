import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { PostCard } from '../../../components/coreUI/postCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { useIsotopeGrid } from '../../../hooks/useIsotopeGrid';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { PostView } from '../../../models/coreUI/postView';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-grid-masonry-filter.html: masonry plus the category filter pills isotope
// drives.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'All posts', isActive: true },
];

// isotope filters on a CSS class per category (e.g. "Faith" -> "faith-category").
const categoryClass = (category: string): string =>
    category.toLowerCase().split(' ').join('-') + '-category';

// The grid lives in its own component so useIsotopeGrid's effect runs when the loaded posts
// mount — on the page component it would fire once against the spinner and never again.
const FilteredMasonryGrid = ({ posts }: { posts: ReadonlyArray<PostView> }) => {
    const gridRef = useIsotopeGrid<HTMLDivElement>('.grid-menu');

    const categories = [...new Map(
        posts.map((post) => [post.category.toLowerCase(), post.category]),
    ).values()].sort((left, right) => left.localeCompare(right));

    return (
        <>
            <div className="row">
                <div className="col-12">
                    <div className="grid-menu" data-target=".filter-container">
                        <ul className="nav nav-pills justify-content-start mb-3">
                            <li className="nav-item">
                                <span className="nav-link disabled ps-0">Show me:</span>
                            </li>
                            <li className="nav-item">
                                <a data-filter="*" className="nav-link active">All posts</a>
                            </li>
                            {categories.map((category) => (
                                <li className="nav-item" key={category}>
                                    <a data-filter={`.${categoryClass(category)}`} className="nav-link">
                                        {category}
                                    </a>
                                </li>
                            ))}
                        </ul>
                    </div>
                </div>
            </div>

            <div
                ref={gridRef}
                className="row filter-container overflow-hidden"
                data-isotope='{"layoutMode": "masonry"}'>
                {posts.map((post) => (
                    <div
                        className={`col-sm-6 col-lg-4 grid-item mb-4 ${categoryClass(post.category)}`}
                        key={post.id}>
                        <PostCard post={post} showExcerpt={true} />
                    </div>
                ))}
            </div>
        </>
    );
};

export const PostGridMasonryFilterSample = () => {
    useDocumentTitle('Post Grid Masonry Filter — Sample — Glory 2 Him');

    const { posts, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Grid Masonry Filter" sourceFile="post-grid-masonry-filter.html">
            <HeroBanner title="Post grid masonry filter" crumbs={crumbs} />

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
                        <FilteredMasonryGrid posts={posts} />
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
