import { useMemo } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { toastSuccess } from '../../brokers/toastBroker.success';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { ContentItemPanel } from '../../components/contentItems/contentItemPanel';
import { ReviewPanel } from '../../components/approvals/reviewPanel';
import { Spinner } from '../../components/coreUI/spinner';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    BibleReferenceAssociationPanel
} from '../../components/associations/bibleReferenceAssociationPanel';

import {
    TagAssociationPanel
} from '../../components/associations/tagAssociationPanel';
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

    // The association WRITES arrive with #318; until then the boxes answer honestly rather than
    // silently dropping what a moderator typed. Same posture as /myposts/{id}.
    const suggestTag = () => toastSuccess('Suggesting tags is coming soon.');

    const suggestBibleReference = () =>
        toastSuccess('Suggesting bible references is coming soon.');

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
            ) : isError || contentItem == null || searchItem == null ? (
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

                    {/* WHAT IS BEING JUDGED on the left, WHO IS JUDGING IT on the right. The
                        item and the facts attached to it read as one column, and the round —
                        who has voted, why approval is blocked, the decision itself — stands
                        beside them rather than under a scroll. */}
                    <div className="row g-4">
                        <div className="col-lg-7">
                            {/* THE MODERATED FACE OF THE CARD. showModerationSection says this
                                surface IS moderation, so the card offers no Edit of its own; the
                                ribbon names the status in the corner; and the content is uncut,
                                because a moderator rules on what is actually there rather than
                                on a truncation.

                                The pill beside the type chip is OFF against the ribbon — one
                                card saying "Draft" twice reads as two different facts about the
                                row — and the in-card tag and reference sections are off because
                                those two panels render in full below, and the same facts must
                                not appear twice on one screen. Both are the calls /myposts/{id}
                                makes for the same pairing. */}
                            <ContentItemPanel
                                contentItem={searchItem}
                                showModerationSection
                                showApprovalStatusRibbon
                                showApprovalStatus={false}
                                showContentExpanded
                                showTagSection={false}
                                showBibleReferenceSection={false}
                                contentItemSettingCollection={contentItemSettings ?? []} />

                            {/* BELOW THE ITEM, not beside it: tags and references are facts
                                ABOUT the thing being judged, so they belong in its column. The
                                collections are honestly empty until #318 gives associations an
                                exposer — the item's id is here off the URL, which is where that
                                read keys in. */}
                            <TagAssociationPanel
                                associationCollection={[]}
                                onAdd={suggestTag}
                                showBorder
                                cssClass="mt-4" />

                            <BibleReferenceAssociationPanel
                                associationCollection={[]}
                                onAdd={suggestBibleReference}
                                showBorder
                                cssClass="mt-4" />
                        </div>

                        <div className="col-lg-5">
                            {/* THE ROUND. entityOwnerId and approvalStatus come off the stored
                                item, so the panel's own gates — nobody reviews their own
                                submission, a terminal round is frozen — decide against the real
                                row rather than against anything this page invents.

                                contentType is the enum MEMBER NAME, never the setting's
                                editable ContentTypeName: it is what §18.6 composes
                                ContentItem-{Type}-Reviewers from, and a renamed type must not
                                silently shed its role names.

                                THE ROUND ITSELF IS NOT READ YET. Reviews, requests, candidates
                                and the per-caller verdict all need the approval exposers (#350),
                                so the collections are empty and no verdict is passed — the panel
                                then shows the status and no decision controls, which is honest,
                                rather than a decision surface over data nobody fetched. */}
                            <ReviewPanel
                                entityType="ContentItem"
                                contentType={ContentType[contentItem.contentType] ?? ''}
                                entityOwnerId={contentItem.createdBy}
                                approvalStatus={contentItem.approvalStatus}
                                approvalReviewCollection={[]}
                                showBorder />
                        </div>
                    </div>
                </>
            )}
        </>
    );
};
