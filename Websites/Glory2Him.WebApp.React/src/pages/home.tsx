import { useMemo } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../components/contentItems/contentItemSearchPanel';
import { SharingPanel } from '../components/contentItems/sharingPanel';
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

    const loadedContentItems = useMemo(
        () => (data?.pages ?? []).flatMap((page) => page.items),
        [data]);

    // Defaults PLUS the overrides of exactly the items on screen — the rows the PROJECTION
    // resolves each item's winner from, so a quote whose comments are switched off by its own
    // override row renders that way here, not just on its detail page.
    const { data: contentItemSettings } =
        contentItemSettingService.useGetEffectiveSettingsFor(
            loadedContentItems.map((item) => item.id));

    // Each element leaves here SELF-CONTAINED — the item and its winning setting together —
    // so the panels consult no collection, and updating one item is one element swapped.
    const contentItems = useMemo(
        () => loadedContentItems.map(
            (item) => toContentItemSearchItem(item, contentItemSettings ?? [])),
        [loadedContentItems, contentItemSettings]);

    const search = (searched: ContentItemSearchCriteria) =>
        setSearchParams(toContentItemSearchParams(searched));

    const feedNavigation = buildContentItemFeedNavigation(navigate, location);

    const { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withViewerReactions } =
        useContentItemEngagement();

    // Edit renders only for the item's own submitter and Moderate only for the moderation
    // tier — ContentItemPanel decides both from the signed-in identity, so this page
    // just says where each leads. The moderation detail surface is #350's work; until it
    // exists Moderate leads to the item with the intent in state, as Edit does.
    const editContentItem = (item: { id: string }) =>
        navigate(`/posts/${item.id}`, {
            state: { from: `${location.pathname}${location.search}`, edit: true }
        });

    const moderateContentItem = (item: { id: string }) =>
        navigate(`/posts/${item.id}`, {
            state: { from: `${location.pathname}${location.search}`, moderate: true }
        });

    return (
        <>
            <VerseOfTheDay verse={verseOfTheDay} href="/BibleReferences" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="row">
                        <div className="col-12">
                            {/* The invitation, sized by its container: full-width here, so it
                                wears its banner face. The panel raises onSubmit; this page says
                                where contributing happens. */}
                            <SharingPanel
                                onSubmit={() => navigate('/posts/contribute', {
                                    state: { from: `${location.pathname}${location.search}` }
                                })} />

                            {isError ? (
                                <div className="alert alert-danger" role="alert">
                                    We could not load the journal right now. Please try again
                                    later.
                                </div>
                            ) : (
                                <ContentItemSearchPanel
                                    ariaLabel="The journal"
                                    contentItemCollection={withViewerReactions(contentItems)}
                                    categorySettingCollection={contentItemSettings ?? []}
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
                                    onEditClick={editContentItem}
                                    onModerateClick={moderateContentItem}
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
