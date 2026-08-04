import { PageHeader } from '../components/coreUI/pageHeader';
import { PostCard } from '../components/coreUI/postCard';
import { Spinner } from '../components/coreUI/spinner';
import { useIsotopeGrid } from '../hooks/useIsotopeGrid';
import { PostView } from '../models/coreUI/postView';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// Filterable masonry journal (Blogzine post-grid-masonry-filter.html). Blazor leaned on the
// vendor isotope init running at DOMContentLoaded; here useIsotopeGrid owns that lifecycle.

// isotope filters on a CSS class per category (e.g. "Faith" -> "faith-category").
const categoryClass = (category: string): string =>
    category.toLowerCase().split(' ').join('-') + '-category';

// The grid lives in its own component so useIsotopeGrid's effect runs when the loaded posts
// mount — on the page component it would fire once against the spinner and never again.
const MasonryGrid = ({ posts }: { posts: ReadonlyArray<PostView> }) => {
    const gridRef = useIsotopeGrid<HTMLDivElement>('.grid-menu');

    const categories = [...new Map(
        posts.map((post) => [post.category.toLowerCase(), post.category]),
    ).values()].sort((left, right) => left.localeCompare(right));

    return (
        <>
            {/* Filter pills (isotope) */}
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

            {/* Masonry grid */}
            <div
                ref={gridRef}
                className="row filter-container overflow-hidden"
                data-isotope='{"layoutMode": "masonry"}'>
                {posts.map((post) => (
                    <div
                        className={`col-sm-6 col-lg-4 grid-item ${categoryClass(post.category)}`}
                        key={post.id}>
                        <PostCard post={post} showExcerpt={true} />
                    </div>
                ))}
            </div>
        </>
    );
};

export const JournalMasonry = () => {
    useDocumentTitle('Journal — masonry — Glory 2 Him');

    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    return (
        <>
            <PageHeader title="Journal — masonry filter" />

            <section className="pt-4 pb-5">
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
                        <MasonryGrid posts={posts} />
                    )}
                </div>
            </section>
        </>
    );
};
