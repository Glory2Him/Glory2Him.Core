import { useMemo } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../components/contentItems/contentItemSearchPanel';
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

import { useDocumentTitle } from './useDocumentTitle';

// EVERY CONTRIBUTION THE CALLER MAY SEE — the collection `/posts/{id}` and `/posts/contribute`
// are members of. The list itself is the ContentItemSearchPanel family; this page's whole job is
// to decide which read feeds it, page that read, project its rows and own the redirects.
//
// THE CALLER-SCOPED READ, which is what separates this surface from the home feed: it widens
// with whoever is asking — the §14.1 canonical set for a visitor, plus the caller's own rows at
// every status, plus everything a review role covers — and the FOUNDATION decides all of that
// against the stored row. Nothing here filters, and nothing here could be made to leak a draft
// by a role change elsewhere.
//
// THE CRITERIA LIVE IN THE URL, so the header's search, a shared link and the back button all
// land with the results already showing.
export function Posts() {
    useDocumentTitle('The journal — Glory 2 Him');

    const navigate = useNavigate();
    const location = useLocation();
    const [searchParams, setSearchParams] = useSearchParams();

    // Memoized on the URL itself: the criteria are part of the query key, and a fresh object on
    // every render would restart the scroll on every render.
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
    } = contentItemService.useSearchContentItems(criteria, { scope: 'caller' });

    // Rendering an item needs its type's name, icon and facet pairs, which is a different
    // question from which types are open to contribution — so the defaults are read, the same
    // way /posts/{id} reads them.
    // The ACCUMULATED list — react-query keeps the pages.
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
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row">
                    <div className="col-12">
                        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
                            <h1 className="h2 mb-0">The journal</h1>

                            <Link to="/posts/contribute" className="btn btn-primary mb-0">
                                <i className="bi bi-pencil-square me-1" aria-hidden="true"></i>
                                Share what He has done
                            </Link>
                        </div>

                        {isError ? (
                            <div className="alert alert-danger" role="alert">
                                We could not load the journal right now. Please try again later.
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

                        {/* The engagement handlers are the shared THIN wiring: the controls
                            render and respond, and the writes behind them arrive with #318 —
                            see useContentItemEngagement. */}
                    </div>
                </div>
            </div>
        </section>
    );
}
