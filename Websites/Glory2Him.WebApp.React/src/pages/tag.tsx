import { Link, useSearchParams } from 'react-router-dom';
import { PageHeader } from '../components/coreUI/pageHeader';
import { PostCard } from '../components/coreUI/postCard';
import { Spinner } from '../components/coreUI/spinner';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// Tag archive (Blogzine tag.html) — posts filtered by tag/category, with a tag cloud.
// Blazor read the tag from ?name= via [SupplyParameterFromQuery]; here useSearchParams does
// the same, and the filtering stays client-side over the full list exactly as TagBase did.
export const Tag = () => {
    const [searchParams] = useSearchParams();
    const tagName = searchParams.get('name');
    const activeTag = tagName == null || tagName.trim().length === 0 ? 'All' : tagName;

    useDocumentTitle(`Tag: ${activeTag} — Glory 2 Him`);

    const { data, isLoading, isError } = postService.useGetPosts({});
    const allPosts = data == null ? [] : withParsedDates(data.items);

    const tags = [...new Map(
        allPosts.map((post) => [post.category.toLowerCase(), post.category]),
    ).values()].sort((left, right) => left.localeCompare(right));

    const posts = tagName == null || tagName.trim().length === 0
        ? allPosts
        : allPosts.filter((post) =>
            post.category.toLowerCase() === tagName.toLowerCase());

    return (
        <>
            <PageHeader title={`# ${activeTag}`} />

            <section className="pt-4 pb-5">
                <div className="container">
                    {/* Tag cloud */}
                    <ul className="list-inline mb-4">
                        <li className="list-inline-item">
                            <Link
                                to="/Tag"
                                className={`btn btn-sm ${tagName == null || tagName.trim().length === 0 ? 'btn-primary' : 'btn-primary-soft'}`}>
                                All
                            </Link>
                        </li>
                        {tags.map((tag) => (
                            <li className="list-inline-item" key={tag}>
                                <Link
                                    to={`/Tag?name=${encodeURIComponent(tag)}`}
                                    className={`btn btn-sm ${tagName != null && tag.toLowerCase() === tagName.toLowerCase() ? 'btn-primary' : 'btn-primary-soft'}`}>
                                    {tag}
                                </Link>
                            </li>
                        ))}
                    </ul>

                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : posts.length === 0 ? (
                        <div className="alert alert-info" role="alert">
                            No posts tagged <strong>{activeTag}</strong>.
                        </div>
                    ) : (
                        <div className="row g-4">
                            {posts.map((post) => (
                                <div className="col-sm-6 col-lg-4" key={post.id}>
                                    <PostCard post={post} />
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </section>
        </>
    );
};
