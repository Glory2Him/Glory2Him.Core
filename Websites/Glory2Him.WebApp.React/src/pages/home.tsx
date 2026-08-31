import { useMemo } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../components/contentItems/contentItemSearchPanel';
import { VerseOfTheDay } from '../components/coreUI/verseOfTheDay';
import { useContentItemEngagement } from '../hooks/useContentItemEngagement';

import {
    buildContentItemFeedNavigation
} from '../services/views/contentItems/contentItemFeedNavigation';

import {
    toContentItemSearchCriteria,
    toContentItemSearchParams
} from '../services/views/contentItems/contentItemSearchCriteriaUrl';

import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

import {
    toContentItemSearchItem
} from '../services/views/contentItems/toContentItemSearchItem';

import { verseOfTheDay } from './sampleContent';
import { useDocumentTitle } from './useDocumentTitle';

// THE PUBLIC HOME PAGE: the verse of the day, then what has actually been contributed — the
// ContentItemSearchPanel family over the PUBLIC read, replacing the Blogzine sample feed that
// stood here.
//
// GET api/ContentItems/Public IS THE POINT of this page's wiring. It is caller-INDEPENDENT by
// construction — exactly the §14.1 canonical set: approved, published, past its publish date —
// so a privileged visitor sees what an anonymous one does, and no role change anywhere can leak
// a draft onto the front page. The caller-widened surfaces are /posts, /MyPosts and
// /Admin/Posts; the front door deliberately is not one.
//
// The criteria live in the URL, so the header's search and a shared link land with the results
// already showing.
export const Home = () => {
    useDocumentTitle('Glory 2 Him — Sharing the Gospel');

    const navigate = useNavigate();
    const location = useLocation();
    const [searchParams, setSearchParams] = useSearchParams();

    const criteria = useMemo(
        () => toContentItemSearchCriteria(searchParams),
        [searchParams]);

    const {
        data,
        isLoading,
        isError,
        isFetchingNextPage,
        hasNextPage,
        fetchNextPage
    } = contentItemService.useSearchContentItems(criteria, { scope: 'public' });

    const { data: contentItemSettings } = contentItemSettingService.useGetDefaults();

    const contentItems = useMemo(
        () => (data?.pages ?? [])
            .flatMap((page) => page.items)
            .map(toContentItemSearchItem),
        [data]);

    const search = (searched: ContentItemSearchCriteria) =>
        setSearchParams(toContentItemSearchParams(searched));

    const feedNavigation = buildContentItemFeedNavigation(navigate, location);

    const { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withViewerReactions } =
        useContentItemEngagement();

    return (
        <>
            <VerseOfTheDay verse={verseOfTheDay} href="/BibleReferences" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="row">
                        <div className="col-12">
                            <div className="d-flex flex-wrap justify-content-end align-items-center gap-2 mb-4">
                                <Link to="/posts/contribute" className="btn btn-primary mb-0">
                                    <i className="bi bi-pencil-square me-1" aria-hidden="true"></i>
                                    Share what He has done
                                </Link>
                            </div>

                            {isError ? (
                                <div className="alert alert-danger" role="alert">
                                    We could not load the journal right now. Please try again
                                    later.
                                </div>
                            ) : (
                                <ContentItemSearchPanel
                                    ariaLabel="The journal"
                                    contentItemCollection={withViewerReactions(contentItems)}
                                    contentItemSettingCollection={contentItemSettings ?? []}
                                    criteria={criteria}
                                    onSearch={search}
                                    isLoading={isLoading}
                                    isLoadingMore={isFetchingNextPage}
                                    hasMore={hasNextPage}
                                    onLoadMore={fetchNextPage}
                                    reactionOptions={reactionOptions}
                                    onReactionSelected={onReactionSelected}
                                    onShareClick={onShareClick}
                                    onSaveClick={onSaveClick}
                                    emptyText={
                                        'Nothing matched that search. Try clearing the advanced '
                                        + 'options.'}
                                    {...feedNavigation} />
                            )}

                            {/* The engagement handlers are the shared THIN wiring: the
                                controls render and respond, and the writes behind them
                                arrive with #318 — see useContentItemEngagement. */}
                        </div>
                    </div>
                </div>
            </section>
        </>
    );
};
