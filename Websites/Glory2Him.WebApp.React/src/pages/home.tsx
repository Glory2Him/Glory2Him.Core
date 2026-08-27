import { Link, useLocation } from 'react-router-dom';
import { ArticleCard } from '../components/coreUI/articleCard';
import { ContributionPrompt } from '../components/coreUI/contributionPrompt';
import { PostHeroCard } from '../components/coreUI/postHeroCard';
import { VerseOfTheDay } from '../components/coreUI/verseOfTheDay';
import { useAuth } from '../components/securitys/authProvider';
import {
    categories,
    featured,
    heroTiles,
    latest,
    popularReferences,
    popularTags,
    SamplePost,
    verseOfTheDay,
} from './sampleContent';
import { useDocumentTitle } from './useDocumentTitle';

// The public home page, laid out exactly as the Home Default sample: verse-of-the-day strip, a
// featured lead beside three smaller cards, then the latest posts with their tags and bible
// references against a categories / tags / references sidebar.
//
// The copy still comes from sampleContent — real posts have not been wired in yet. When they are,
// only the data imports above and the hrefs need to change; the markup stays as it is.

// Every hero tile is an h4 with its counts on a second row — the tiles are too narrow to
// carry the byline and the counts on one line.
const SmallHero = ({ post }: { post: SamplePost }) => (
    <PostHeroCard
        title={post.title}
        href="/Post-Single"
        category={post.category}
        categoryBadgeCss={post.categoryBadgeCss}
        imageUrl={post.imageUrl}
        authorName={post.authorName}
        publishedDate={post.publishedDate}
        showExcerpt={false}
        sizeCssClass="card-grid-sm"
        titleCssClass="h4"
        splitMeta={true}
        reactions={post.reactions}
        comments={post.comments}
        tagCount={post.tags.length}
        referenceCount={post.bibleReferences.length} />
);

export const Home = () => {
    useDocumentTitle('Glory 2 Him — Sharing the Gospel');

    const { isAuthenticated } = useAuth();
    const location = useLocation();
    const loginHref = `/Account/Login?returnUrl=${encodeURIComponent(location.pathname)}`;

    return (
        <>
            <VerseOfTheDay verse={verseOfTheDay} href="/Post-Single" />

            <section className="pt-4 pb-0 card-grid">
                <div className="container">
                    <div className="row g-4">
                        <div className="col-lg-6">
                            <PostHeroCard
                                title={featured.title}
                                href="/Post-Single"
                                excerpt={featured.excerpt}
                                category={featured.category}
                                categoryBadgeCss={featured.categoryBadgeCss}
                                imageUrl={featured.imageUrl}
                                authorName={featured.authorName}
                                authorImageUrl={featured.authorImageUrl}
                                showAuthorAvatar={true}
                                publishedDate={featured.publishedDate}
                                isFeatured={true}
                                sizeCssClass="card-grid-lg"
                                titleCssClass="h1"
                                reactions={featured.reactions}
                                comments={featured.comments}
                                tagCount={featured.tags.length}
                                referenceCount={featured.bibleReferences.length} />
                        </div>

                        <div className="col-lg-6">
                            <div className="row g-4">
                                <div className="col-12">
                                    <SmallHero post={heroTiles[0]} />
                                </div>
                                <div className="col-md-6">
                                    <SmallHero post={heroTiles[1]} />
                                </div>
                                <div className="col-md-6">
                                    <SmallHero post={heroTiles[2]} />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </section>

            <section className="position-relative py-5">
                <div className="container">
                    <div className="row g-4">
                        <div className="col-lg-9">
                            <div className="mb-4">
                                <h2 className="m-0">
                                    <i className="bi bi-hourglass-top me-2"></i>Latest posts
                                </h2>
                                <p className="mb-0">
                                    Quotes, stories, devotionals and studies from the community
                                </p>
                            </div>

                            <div className="row gy-4">
                                {latest.map((post) => (
                                    <div className="col-sm-6" key={`${post.slug}-${post.category}`}>
                                        <ArticleCard
                                            title={post.title}
                                            href="/Post-Single"
                                            excerpt={post.excerpt}
                                            imageUrl={post.imageUrl}
                                            category={post.category}
                                            categoryBadgeCss={post.categoryBadgeCss}
                                            authorName={post.authorName}
                                            authorImageUrl={post.authorImageUrl}
                                            publishedDate={post.publishedDate}
                                            tags={post.tags}
                                            bibleReferences={post.bibleReferences}
                                            reactions={post.reactions}
                                            comments={post.comments} />
                                    </div>
                                ))}
                            </div>
                        </div>

                        <div className="col-lg-3">
                            {/* Categories and tags are both ways of asking "show me posts about
                                this", so both go to a search for the word. References go to the
                                passage itself instead — that page shows one fixed verse for now,
                                so its link carries no query. */}
                            <h4 className="mb-3">Categories</h4>
                            <div className="d-flex flex-wrap mb-4" style={{ gap: '8px' }}>
                                {categories.map(([label, buttonCss]) => (
                                    <Link
                                        key={label}
                                        to={`/Search?q=${encodeURIComponent(label)}`}
                                        className={`btn btn-sm ${buttonCss} mb-0`}>
                                        <i className="fas fa-circle me-2 small"></i>{label}
                                    </Link>
                                ))}
                            </div>

                            <h4 className="mb-3">Popular tags</h4>
                            <div className="d-flex flex-wrap" style={{ gap: '8px' }}>
                                {popularTags.map(([label, buttonCss]) => (
                                    <Link
                                        key={label}
                                        to={`/Search?q=${encodeURIComponent(label)}`}
                                        className={`btn btn-sm ${buttonCss} mb-0`}>
                                        {label}
                                    </Link>
                                ))}
                            </div>

                            {/* References flow across and wrap like the tags above them, rather
                                than stacking as full-width rows. */}
                            <h4 className="mt-4 mb-3">Popular references</h4>
                            <div className="d-flex flex-wrap" style={{ gap: '8px' }}>
                                {popularReferences.map((reference) => (
                                    <Link
                                        key={reference}
                                        to="/BibleReferences"
                                        className="btn btn-sm btn-primary-soft mb-0">
                                        <i className="bi bi-book me-2"></i>{reference}
                                    </Link>
                                ))}
                            </div>

                            <ContributionPrompt
                                cssClass="mt-4 mb-0"
                                isAuthenticated={isAuthenticated}
                                loginHref={loginHref} />
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
};
