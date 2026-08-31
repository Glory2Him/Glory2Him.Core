import { useMemo } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../../components/contentItems/contentItemSearchPanel';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Card } from '../../components/coreUI/card';
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
    ApprovalStatus,
    ContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

import {
    toContentItemSearchItem
} from '../../services/views/contentItems/toContentItemSearchItem';

import { useDocumentTitle } from '../useDocumentTitle';

// THE MODERATION QUEUE at /Admin/Posts: the same family the public feed renders, in the admin
// shell, narrowed to what needs a moderator — every Draft and Submitted content item the
// caller's tier may see. The PAGE pins the statuses; the FOUNDATION still decides which rows
// this caller's roles actually reach (§14.5), so the pin only ever narrows.
//
// THE THIRD CLAUSE IS MISSING, deliberately: "approved items with unapproved associations"
// cannot be asked yet — associations have no HTTP exposer (#318) — so this queue is Draft +
// Submitted until that read exists, and the issue records the gap rather than a client-side
// approximation pretending to cover it.
//
// Every card wears its status badge, which on this page is the entire point of the projection's
// approvalStatus member.
const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Posts', href: '/Admin/Posts', isActive: true },
];

const moderatedStatuses: ReadonlyArray<ApprovalStatus> =
    [ApprovalStatus.Draft, ApprovalStatus.Submitted];

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
        approvalStatuses: moderatedStatuses
    });

    const { data: contentItemSettings } = contentItemSettingService.useGetDefaults();

    const contentItems = useMemo(
        () => (data?.pages ?? [])
            .flatMap((page) => page.items)
            .map(toContentItemSearchItem),
        [data]);

    const search = (searched: ContentItemSearchCriteria) =>
        setSearchParams(toContentItemSearchParams(searched));

    // The detail destination is /posts/{id} FOR NOW — the moderation detail surface is #350's
    // work, and this builder's parameter is where it plugs in when it exists.
    const feedNavigation = buildContentItemFeedNavigation(navigate, location);

    const { reactionOptions, onReactionSelected, onShareClick, onSaveClick, withViewerReactions } =
        useContentItemEngagement();

    // The moderation detail surface is #350's work; until it exists Edit leads to the item.
    const editContentItem = (item: { id: string }) =>
        navigate(`/posts/${item.id}`, {
            state: { from: `${location.pathname}${location.search}`, edit: true }
        });

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Posts awaiting moderation</h1>
                <Breadcrumb items={crumbs} />
            </div>

            <Card>
                {isError ? (
                    <div className="alert alert-danger mb-0" role="alert">
                        We could not load the moderation queue right now. Please try again later.
                    </div>
                ) : (
                    <ContentItemSearchPanel
                        ariaLabel="Posts awaiting moderation"
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
                        onEditClick={editContentItem}
                        emptyText="Nothing is waiting for moderation. Well done."
                        {...feedNavigation} />
                )}
            </Card>
        </>
    );
}
