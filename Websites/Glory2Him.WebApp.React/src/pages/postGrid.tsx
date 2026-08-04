import { PageHeader } from '../components/coreUI/pageHeader';
import { PostCard } from '../components/coreUI/postCard';
import { Spinner } from '../components/coreUI/spinner';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// Post grid layout (Blogzine post-grid.html) — reuses the shared posts query + PostCard.
export const PostGrid = () => {
    useDocumentTitle('Post grid — Glory 2 Him');

    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    return (
        <>
            <PageHeader title="Post grid" />

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
