import { useMemo } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemListPanel } from '../../components/contentItems/contentItemListPanel';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { useContentItemEngagement } from '../../hooks/useContentItemEngagement';

import {
    buildContentItemFeedNavigation
} from '../../services/views/contentItems/contentItemFeedNavigation';

import {
    toContentItemSearchCriteria,
    toContentItemSearchParams
} from '../../services/views/contentItems/contentItemSearchCriteriaUrl';

import { contentItemService } from '../../services/foundations/contentItemService';
import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';

import {
    ContentItemSearchCriteria,
    contentItemSearchApprovalStatusMembers
} from '../../models/components/contentItems/contentItemSearchItem';

import {
    toContentItemSearchItem
} from '../../services/views/contentItems/toContentItemSearchItem';

import { useDocumentTitle } from '../useDocumentTitle';

// THE POSTS LIST at /Admin/Posts: the same family the public feed renders, in the admin shell,
// over every content item the caller's tier may see — at EVERY status by default, because a
// moderator's question changes by the hour ("what is waiting", "what did we reject last
// week") and the status boxes are how they ask it. The FOUNDATION still decides which rows
// this caller's roles actually reach (§14.5), so no box can widen the list past that.
//
// The four statuses start ticked and the reader's selection replaces them; the same four are
// handed to the read below, so the boxes always describe the results. "Approved items with
// unapproved associations" is still not askable — associations have no HTTP exposer (#318) —
// and the issue records that gap rather than a client-side approximation.
//
// Every card wears its status badge, which on this page is the entire point of the projection's
// approvalStatus member.
const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Posts', href: '/Admin/Posts', isActive: true },
];

export function ContentItemModerationPage() {
    useDocumentTitle('Posts — Admin — Glory 2 Him');

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
    } = contentItemService.useSearchContentItems(criteria, {
        scope: 'caller',
        defaultApprovalStatuses: contentItemSearchApprovalStatusMembers
    });

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

    // EVERY WAY INTO AN ITEM FROM HERE stays in the admin area — the title, the read-more,
    // the comment link and Moderate alike. This is the builder's detail parameter doing the job
    // it exists for: one destination for the whole card, so a moderator cannot fall out of the
    // queue by clicking the heading instead of the pencil.
    const feedNavigation = buildContentItemFeedNavigation(
        navigate, location, (item) => `/Admin/Posts/${item.id}`);

    const { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withViewerReactions } =
        useContentItemEngagement();

    // MODERATE STAYS IN THE ADMIN AREA. It leads to the item's admin address, never to the
    // public /posts/{id}: a moderator who steps into a post from here is still working the
    // queue, and the public route would swap the chrome out from under them and lose the
    // filtered page they were part-way through.
    //
    // The intent still rides in state for the surface #350 will build there. The origin rides
    // with it, so the way back is the queue as they left it rather than a guess at history.
    const moderateContentItem = (item: { id: string }) =>
        navigate(`/Admin/Posts/${item.id}`, {
            state: { from: `${location.pathname}${location.search}`, moderate: true }
        });

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Posts</h1>
                <Breadcrumb items={crumbs} />
            </div>

            {/* NO CARD AROUND THE PANEL. Every row the panel renders is already a card, so a
                card around them is chrome inside chrome: a second border, and card-body padding
                on both sides that narrows every quote and title for nothing. The public feeds
                (home, /posts, /myposts) render this same panel bare, and the queue is the same
                family in the admin shell — not a different one that needs framing. */}
            {isError ? (
                <div className="alert alert-danger" role="alert">
                    We could not load the moderation queue right now. Please try again later.
                </div>
            ) : (
                <ContentItemListPanel
                    ariaLabel="Posts awaiting moderation"
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
                    showModerationSection
                    showApprovalStatus

                    // Every status starts ticked, matching the read above; a moderator
                    // working the drafts alone unticks the other three, and the list
                    // narrows to exactly what the boxes say.
                    showApprovalStatusSearchOptions
                    searchApprovalDraftSelected
                    searchApprovalSubmittedSelected
                    searchApprovalApprovedSelected
                    searchApprovalRejectedSelected
                    onModerateClick={moderateContentItem}
                    emptyText="No posts matched that search."
                    {...feedNavigation} />
            )}
        </>
    );
}
