import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { PostCard } from '../../../components/coreUI/postCard';
import { PostListItem } from '../../../components/coreUI/postListItem';
import { PostOverlayCard } from '../../../components/coreUI/postOverlayCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine index-3.html: a dense magazine front page — full-bleed hero, a four-across strip,
// then a two-column main + sidebar split.
export const HomeMagazineSample = () => {
    useDocumentTitle('Magazine — Sample — Glory 2 Him');

    const { lead, isLoading, isError, fill, take } = useSamplePosts();

    return (
        <SampleShell title="Magazine" sourceFile="index-3.html">
            {isLoading ? (
                <div className="text-center py-5"><Spinner /></div>
            ) : isError ? (
                <div className="alert alert-danger m-4" role="alert">
                    We could not load posts right now. Please try again later.
                </div>
            ) : lead == null ? (
                <div className="alert alert-info m-4" role="alert">
                    No posts have been published yet.
                </div>
            ) : (
                <>
                    <section className="pt-4">
                        <div className="container">
                            <div className="row g-4">
                                <div className="col-12">
                                    <PostOverlayCard post={lead} titleCssClass="display-6" />
                                </div>
                            </div>
                        </div>
                    </section>

                    <section className="py-5">
                        <div className="container">
                            <div className="row g-4 mb-5">
                                {fill(4).map((post, index) => (
                                    <div className="col-sm-6 col-lg-3" key={`strip-${post.id}-${index}`}>
                                        <PostCard post={post} />
                                    </div>
                                ))}
                            </div>

                            <div className="row g-4">
                                <div className="col-lg-8">
                                    <h2 className="h4 mb-4">Editor's picks</h2>

                                    <div className="row g-4">
                                        {fill(4).map((post, index) => (
                                            <div className="col-md-6" key={`picks-${post.id}-${index}`}>
                                                <PostCard post={post} showExcerpt={true} />
                                            </div>
                                        ))}
                                    </div>

                                    <hr className="my-4" />

                                    {take(3).map((post) => (
                                        <PostListItem post={post} key={`list-${post.id}`} />
                                    ))}
                                </div>

                                <div className="col-lg-4">
                                    <BlogSidebar trendingPosts={take(4)} />
                                </div>
                            </div>
                        </div>
                    </section>
                </>
            )}
        </SampleShell>
    );
};
