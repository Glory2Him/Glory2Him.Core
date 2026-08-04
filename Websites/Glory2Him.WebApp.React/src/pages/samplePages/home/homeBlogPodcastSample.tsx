import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { Pagination } from '../../../components/coreUI/pagination';
import { PodcastCard } from '../../../components/coreUI/podcastCard';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine index-10.html: the podcast front page — a featured episode over the hero, then the
// episode list.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Podcast', isActive: true },
];

// Durations are demo dressing rather than stored data, so they are paired here instead of
// being pushed onto PostView.
const durations: ReadonlyArray<string> =
    ['18:42', '26:05', '41:19', '33:57', '12:30', '48:11'];

export const HomeBlogPodcastSample = () => {
    useDocumentTitle('Blog Podcast — Sample — Glory 2 Him');

    const { posts, lead, isLoading, isError, take } = useSamplePosts();

    return (
        <SampleShell title="Blog Podcast" sourceFile="index-10.html">
            <HeroBanner title="The Glory 2 Him Podcast" crumbs={crumbs} />

            <section className="py-5">
                <div className="container">
                    {isLoading ? (
                        <div className="text-center py-5"><Spinner /></div>
                    ) : isError ? (
                        <div className="alert alert-danger" role="alert">
                            We could not load posts right now. Please try again later.
                        </div>
                    ) : lead == null ? (
                        <div className="alert alert-info" role="alert">
                            No episodes have been published yet.
                        </div>
                    ) : (
                        <div className="row g-4">
                            <div className="col-lg-8">
                                <h2 className="h4 mb-4">Latest episodes</h2>

                                <div className="vstack gap-3">
                                    {posts.map((post, index) => (
                                        <PodcastCard
                                            post={post}
                                            duration={durations[index % durations.length]}
                                            key={post.id} />
                                    ))}
                                </div>

                                <div className="mt-5">
                                    <Pagination currentPage={1} totalPages={4} variant="Rounded" />
                                </div>
                            </div>

                            <div className="col-lg-4">
                                <BlogSidebar trendingPosts={take(4)} showAbout={true} />
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </SampleShell>
    );
};
