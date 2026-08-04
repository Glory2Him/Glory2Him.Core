import { PageHeader } from '../components/coreUI/pageHeader';
import { PostListItem } from '../components/coreUI/postListItem';
import { Spinner } from '../components/coreUI/spinner';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// Post list layout (Blogzine post-list.html) — reuses the shared posts query + PostListItem.
export const PostList = () => {
    useDocumentTitle('Post list — Glory 2 Him');

    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    return (
        <>
            <PageHeader title="Post list" />

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
                        <div className="row">
                            <div className="col-lg-10 mx-auto">
                                {posts.map((post) => (
                                    <PostListItem key={post.id} post={post} />
                                ))}
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </>
    );
};
