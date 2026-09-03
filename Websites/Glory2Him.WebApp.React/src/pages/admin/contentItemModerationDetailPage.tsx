import { useMemo } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { ContentItemPanel } from '../../components/contentItems/contentItemPanel';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { contentItemService } from '../../services/foundations/contentItemService';
import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';
import { contributorService } from '../../services/foundations/contributorService';
import { useDocumentTitle } from '../useDocumentTitle';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../../services/views/contentItems/resolveContentItemSetting';

import {
    toContentItemSearchItem
} from '../../services/views/contentItems/toContentItemSearchItem';

// ONE ITEM FROM THE MODERATION QUEUE, at /Admin/Posts/{id}. The queue's Moderate leads HERE
// rather than to /posts/{id}: a moderator who steps into an item is still working the admin
// area, and the public route drops them out of it — different chrome, no way back to the
// filtered queue they were part-way through, and the sidebar gone from under them.
//
// IT READS THE SAME ITEM THE PUBLIC PAGE DOES, deliberately. The moderation surface proper —
// the review panel, the decision controls, the association verdicts — is #350's work; until it
// lands this page is the item in the admin shell, which is the address those controls will be
// added to rather than a second one to migrate off later.
const moderationRoute = '/Admin/Posts';

export const ContentItemModerationDetailPage = () => {
    const { contentItemId = '' } = useParams();
    const navigate = useNavigate();
    const location = useLocation();

    const { data: contentItem, isLoading, isError } =
        contentItemService.useGetContentItemById(contentItemId, contentItemId.length > 0);

    // Defaults plus THIS item's own override — §6.4 resolution needs the specific row in hand
    // to prefer it, exactly as the queue's cards do.
    const { data: contentItemSettings } =
        contentItemSettingService.useGetEffectiveSettingsFor(
            contentItemId.length > 0 ? [contentItemId] : []);

    // WHO SUBMITTED IT: the item carries CreatedBy, an account id, so the byline takes a second
    // read. Rendered when it arrives rather than waited on — an item under moderation must not
    // hang on its byline.
    const { data: contributor } = contributorService.useGetContributorById(
        contentItem?.createdBy ?? '');

    const searchItem = useMemo(
        () => contentItem == null
            ? undefined
            : {
                ...toContentItemSearchItem(contentItem, contentItemSettings ?? []),
                submittedByName: contributor?.displayName,
                submittedByImageUrl: contributor?.imageUrl ?? undefined
            },
        [contentItem, contentItemSettings, contributor]);

    // The same resolver the panel asks, against the same rows: a type whose effective setting
    // carries no title must not have one shouted as the heading while the panel hides it.
    const headingSetting = useMemo(
        () => contentItem == null
            ? undefined
            : resolveContentItemSetting(
                contentItemSettings ?? [], contentItem.contentType, contentItem.id),
        [contentItemSettings, contentItem]);

    const showsTitle =
        headingSetting?.hasTitle ?? (contentItem?.title ?? '').length > 0;

    const heading =
        contentItem == null
            ? 'Post'
            : showsTitle && (contentItem.title ?? '').length > 0
                ? contentItem.title ?? ''
                : contentTypeNameOf(
                    contentItemSettings ?? [], contentItem.contentType, contentItem.id);

    useDocumentTitle(
        contentItem == null
            ? 'Post — Admin — Glory 2 Him'
            : `${heading} — Admin — Glory 2 Him`);

    const crumbs: BreadcrumbItem[] = [
        { title: 'Admin' },
        { title: 'Posts', href: moderationRoute },
        { title: heading, isActive: true },
    ];

    // The queue as the moderator left it, filters and page and all. Absent when the page was
    // reached without going through the queue — a pasted link, a refresh — and the bare queue
    // is the honest fallback.
    const backRoute =
        (location.state as { from?: string } | null)?.from ?? moderationRoute;

    const goBack = () => navigate(backRoute);

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">{heading}</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : isError || searchItem == null ? (
                <>
                    <div className="alert alert-danger" role="alert">
                        We could not load this post right now. It may have been removed, or it
                        may not be yours to moderate.
                    </div>

                    <Button color="secondary" onClick={goBack}>
                        <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                        Back to Posts
                    </Button>
                </>
            ) : (
                <>
                    <div className="d-flex justify-content-end mb-3">
                        <Button color="secondary" onClick={goBack}>
                            <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                            Back to Posts
                        </Button>
                    </div>

                    {/* The whole item, uncut: a moderator rules on what is actually there, so
                        there is no read-more to leave half of it unread. */}
                    <ContentItemPanel
                        contentItem={searchItem}
                        showContentExpanded
                        showApprovalStatus />
                </>
            )}
        </>
    );
};
