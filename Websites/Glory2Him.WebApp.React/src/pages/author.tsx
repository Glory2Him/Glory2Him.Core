import { PostListItem } from '../components/coreUI/postListItem';
import { postService } from '../services/foundations/postService';
import { withParsedDates } from './postDates';
import { useDocumentTitle } from './useDocumentTitle';

// Author profile with the author's posts listed beneath it (Blazor Author.razor).
export const Author = () => {
    useDocumentTitle('Author — Glory 2 Him');

    const { data, isLoading, isError } = postService.useGetPosts({});
    const posts = data == null ? [] : withParsedDates(data.items);

    return (
        <>
            {/* Author profile START */}
            <section className="pt-4 pb-3">
                <div className="container">
                    <div className="row">
                        <div className="col-lg-10 mx-auto text-center">
                            <div className="avatar avatar-xl mb-3">
                                <img
                                    className="avatar-img rounded-circle"
                                    src="/assets/images/avatar/01.jpg"
                                    alt="Author" />
                            </div>
                            <h1 className="h3">Joan Wallace</h1>
                            <p className="mb-0">
                                Writer at Glory 2 Him. Sharing stories of faith, hope, and the
                                good news of Jesus Christ.
                            </p>
                        </div>
                    </div>
                </div>
            </section>
            {/* Author profile END */}

            {/* Author posts START */}
            <section className="pt-2 pb-5">
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
                            This author has not published yet.
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
            {/* Author posts END */}
        </>
    );
};
