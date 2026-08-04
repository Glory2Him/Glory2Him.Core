import { PageHeader } from '../components/coreUI/pageHeader';
import { PostCard } from '../components/coreUI/postCard';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// The journal grid (Blazor Categories.razor) — every post as a card, three across.
export const Categories = () => {
    useDocumentTitle('Journal — Glory 2 Him');

    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    return (
        <>
            <PageHeader title="Our Journal" />

            <section className="pt-4 pb-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5">
                            <div className="spinner-border text-primary" role="status">
                                <span className="visually-hidden">Loading...</span>
                            </div>
                        </div>
                    ) : isError ? (
                        <div className="alert alert-danger text-center mb-0" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : posts.length === 0 ? (
                        <div className="alert alert-info text-center mb-0" role="alert">
                            No posts have been published yet.
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
