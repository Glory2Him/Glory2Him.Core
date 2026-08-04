import { Card } from '../../../components/coreUI/card';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { PostCard } from '../../../components/coreUI/postCard';
import { PostTypeBadge } from '../../../components/coreUI/postTypeBadge';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { PostType } from '../../../models/coreUI/postType';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-types.html: the same card marked up as each kind of post — standard, video,
// audio, gallery and quote.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Post types', isActive: true },
];

const typeCycle: ReadonlyArray<PostType> =
    ['Standard', 'Video', 'Audio', 'Gallery', 'Quote', 'Standard'];

export const PostTypesSample = () => {
    useDocumentTitle('Post Types — Sample — Glory 2 Him');

    const { posts, isLoading, isError } = useSamplePosts();

    return (
        <SampleShell title="Post Types" sourceFile="post-types.html">
            <HeroBanner title="Post types" crumbs={crumbs} />

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
                        <div className="row g-4">
                            {posts.map((post, index) => {
                                const type = typeCycle[index % typeCycle.length];

                                return (
                                    <div className="col-sm-6 col-lg-4" key={post.id}>
                                        <div className="position-relative h-100">
                                            <span
                                                className="position-absolute top-0 end-0 m-3"
                                                style={{ zIndex: 2 }}>
                                                <PostTypeBadge type={type} />
                                            </span>

                                            {type === 'Quote' ? (
                                                <Card cssClass="border h-100 bg-primary-soft">
                                                    <blockquote className="mb-3">
                                                        <i className="bi bi-quote fs-1 text-primary"></i>
                                                        <p className="h5 fst-italic">{post.excerpt}</p>
                                                    </blockquote>
                                                    <footer className="small text-body-secondary">
                                                        — {post.authorName}
                                                    </footer>
                                                </Card>
                                            ) : (
                                                <PostCard post={post} showExcerpt={true} />
                                            )}
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
