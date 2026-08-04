import { BlogSidebar } from '../../../components/coreUI/blogSidebar';
import { HeroBanner } from '../../../components/coreUI/heroBanner';
import { ReviewRating } from '../../../components/coreUI/reviewRating';
import { Spinner } from '../../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../../models/coreUI/breadcrumbItem';
import { ReviewCriterion } from '../../../models/coreUI/reviewCriterion';
import { useDocumentTitle } from '../../useDocumentTitle';
import { SampleArticleBody } from '../shared/sampleArticleBody';
import { SampleShell } from '../shared/sampleShell';
import { useSamplePosts } from '../shared/useSamplePosts';

// Blogzine post-single-5.html: the review layout — article beside a scorecard, verdict at
// the end.

const crumbs: ReadonlyArray<BreadcrumbItem> = [
    { title: 'Review', isActive: true },
];

const criteria: ReadonlyArray<ReviewCriterion> = [
    { label: 'Writing', score: 4.8 },
    { label: 'Pacing', score: 4.1 },
    { label: 'Depth', score: 4.6 },
    { label: 'Value', score: 4.4 },
];

export const PostSingleReviewSample = () => {
    useDocumentTitle('Post Single Review — Sample — Glory 2 Him');

    const { lead, isLoading, isError, take } = useSamplePosts();

    return (
        <SampleShell title="Post Single Review" sourceFile="post-single-5.html">
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
                    <HeroBanner title={lead.title} imageUrl={lead.imageUrl} crumbs={crumbs} />

                    <section className="py-5">
                        <div className="container">
                            <div className="row g-4">
                                <div className="col-lg-8">
                                    <SampleArticleBody post={lead} />

                                    <div className="alert alert-primary mt-4" role="alert">
                                        <h3 className="h5 alert-heading">The verdict</h3>
                                        <p className="mb-0">
                                            Worth your time. It rewards a slow read, and the closing chapter
                                            lands harder than the opening promises.
                                        </p>
                                    </div>
                                </div>

                                <div className="col-lg-4">
                                    <div className="mb-4">
                                        <ReviewRating
                                            overallScore={4.5}
                                            summary="Excellent — a rare, generous book."
                                            criteria={criteria} />
                                    </div>

                                    <BlogSidebar trendingPosts={take(3)} showAbout={false} />
                                </div>
                            </div>
                        </div>
                    </section>
                </>
            )}
        </SampleShell>
    );
};
