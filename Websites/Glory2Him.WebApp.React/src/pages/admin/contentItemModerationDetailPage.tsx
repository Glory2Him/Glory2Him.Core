import { useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { toastSuccess } from '../../brokers/toastBroker.success';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { ContentItemPanel } from '../../components/contentItems/contentItemPanel';
import { ContentItemEditPanel } from '../../components/contentItems/contentItemEditPanel';
import { ReviewPanel } from '../../components/approvals/reviewPanel';
import { Spinner } from '../../components/coreUI/spinner';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { EntityTypeName } from '../../models/foundations/approvals/approval';
import { useApprovalRound } from '../../hooks/useApprovalRound';

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

import {
    toContentItemFormItem,
    toContentItemModifyRequest
} from '../../services/views/contentItems/toContentItemFormItem';

import { toastError } from '../../brokers/toastBroker.error';
import { useAuth } from '../../components/securitys/authProvider';
import { approvalService } from '../../services/foundations/approvalService';
import { extractApiErrorMessage } from './apiErrorMessage';

import {
    ApprovalDecision,
    ApprovalStatus as ReviewVote,
    ReviewerCandidateItem
} from '../../models/components/approvals/approvalReviewItem';

import {
    ContentItemFormItem,
    ContentItemValidationIssues
} from '../../models/components/contentItems/contentItemFormItem';

import {
    toContentItemApiFailure
} from '../../services/views/contentItems/toContentItemApiFailure';

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

    // MODERATING IS WHAT EDITING MEANS HERE. showModerationSection puts the card's one action
    // under the moderation tier and labels it Edit, and taking it opens the editor in place
    // rather than navigating — this page IS the destination, so there is nowhere for it to go.
    //
    // It is also the only way a draft advances. §9.2 rule 3's carve-out lets the owner or the
    // publishing tier move Draft ↔ Submitted through a modify, and until somebody does the
    // round cannot open at all — which is exactly what the draft block reason beside it says.
    const [isEditing, setIsEditing] = useState(false);
    const [validationIssues, setValidationIssues] =
        useState<ContentItemValidationIssues | undefined>();

    const modifyContentItem = contentItemService.useModifyContentItem();
    const removeContentItem = contentItemService.useRemoveContentItem();

    // A TAKEDOWN LEAVES NOWHERE TO STAND. The row this page is about is gone, so staying on
    // its address would show a removed item; the moderator goes back to the queue they came
    // from — filtered as they left it, or the bare queue when they arrived by a pasted link.
    //
    // The panel confirms before it ever raises this, so there is no second prompt here.
    const removeContentItemAsync = async () => {
        if (contentItem == null) {
            return;
        }

        try {
            await removeContentItem.mutateAsync({ contentItemId: contentItem.id });
            goBack();
        } catch (error) {
            const failure = toContentItemApiFailure(
                error, 'We could not remove this post right now. Please try again later.');

            toastError(failure.message);
        }
    };

    // The API is the authority on what an item must carry, so nothing is pre-judged here: the
    // edit goes, and whatever comes back marks up the form the moderator is looking at.
    const saveChangesAsync = async (formItem: ContentItemFormItem) => {
        if (contentItem == null) {
            return;
        }

        setValidationIssues(undefined);

        try {
            await modifyContentItem.mutateAsync(
                toContentItemModifyRequest(contentItem, formItem));

            setIsEditing(false);
        } catch (error) {
            const failure = toContentItemApiFailure(
                error, 'We could not save this post right now. Please try again later.');

            setValidationIssues(failure.validationIssues);
            toastError(failure.message);
        }
    };

    // THE ROUND, off the approval endpoints. A refusal is an answer here: a post with no
    // approval row and a caller outside the moderation tier both leave the verdict undefined,
    // and the panel reads that as "no verdict" — the round shown read-only rather than a
    // decision surface nobody is entitled to.
    const {
        approvalVerdict,
        approvalReviewCollection,
        approvalReviews,
        requestedReviewerCollection,
        reviewerCandidateCollection,
        isLoading: isRoundLoading
    } = useApprovalRound(EntityTypeName.ContentItem, contentItemId);

    // ── THE WRITES, events in, requests out. ──────────────────────────────────────
    //
    // The panel decides nothing beyond what its own gates read; the server is the authority,
    // and a refusal from it is an ANSWER (§14.5) — HR-2, a reviewer who has spent their vote, a
    // bypass the policy shut — so each handler shows the reason it was given rather than a
    // generic failure. Nothing is optimistic: every write invalidates the round on success and
    // the panel repaints off the reads.
    const { user } = useAuth();
    const castApprovalReview = approvalService.useCastApprovalReview();
    const decideApproval = approvalService.useDecideApproval();
    const resetApproval = approvalService.useResetApproval();
    const requestReview = approvalService.useRequestReview();
    const withdrawReviewRequest = approvalService.useWithdrawReviewRequest();

    // The viewer's standing review, if any: a changed vote amends THAT row (§7.7 rule 1), and
    // the projection the panel renders does not carry what an amend has to send back.
    const viewerStandingReview = approvalReviews.find(
        (review) => review.createdBy === (user?.userId ?? '') && review.isDeleted !== true);

    const castVoteAsync = async (vote: ReviewVote) => {
        if (approvalVerdict == null) {
            return;
        }

        try {
            await castApprovalReview.mutateAsync({
                approvalId: approvalVerdict.approvalId,
                vote,
                standingReview: viewerStandingReview
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'Your review could not be recorded. Please try again.'));
        }
    };

    const decideAsync = async (
        decision: ApprovalDecision,
        isBypassRequested: boolean,
        bypassReason: string) => {
        try {
            await decideApproval.mutateAsync({
                entityType: EntityTypeName.ContentItem,
                entityId: contentItemId,
                decision,
                isBypassRequested,
                bypassReason
            });

            toastSuccess(decision === ApprovalDecision.Approve
                ? 'The post has been approved.'
                : 'The post has been rejected.');
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'The decision could not be applied. Please try again.'));
        }
    };

    const resetAsync = async () => {
        try {
            await resetApproval.mutateAsync({
                entityType: EntityTypeName.ContentItem,
                entityId: contentItemId
            });

            toastSuccess(
                'The approval has been reset. The post is back with the reviewers, and its '
                + 'recorded reviews have been dismissed.');
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'The approval could not be reset. Please try again.'));
        }
    };

    const requestReviewAsync = async (candidate: ReviewerCandidateItem) => {
        try {
            await requestReview.mutateAsync({
                entityType: EntityTypeName.ContentItem,
                entityId: contentItemId,
                requestedUserId: candidate.userId
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, `${candidate.displayName} could not be asked to review this post.`));
        }
    };

    const withdrawReviewRequestAsync = async (candidate: ReviewerCandidateItem) => {
        try {
            await withdrawReviewRequest.mutateAsync({
                entityType: EntityTypeName.ContentItem,
                entityId: contentItemId,
                requestedUserId: candidate.userId
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, `The request to ${candidate.displayName} could not be withdrawn.`));
        }
    };

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
                            {isEditing ? (
                                /* showEditSection here is the EDITOR's own surface switch, not
                                   the read card's. It is off on the card below so that card's
                                   one action is the moderation Edit rather than the owner's;
                                   on the editor it must be on, because the form panel refuses
                                   mode="edit" back to read without it. */
                                <ContentItemEditPanel
                                    contentItem={toContentItemFormItem(contentItem)}
                                    showEditSection
                                    showApprovalStatusRibbon
                                    validationIssues={validationIssues}
                                    isSubmitting={modifyContentItem.isPending}
                                    onModified={saveChangesAsync}
                                    onRemoved={removeContentItemAsync}
                                    onCancelled={() => {
                                        setValidationIssues(undefined);
                                        setIsEditing(false);
                                    }}
                                    contentItemSettingCollection={contentItemSettings ?? []} />
                            ) : (
                                <ContentItemPanel
                                    contentItem={searchItem}
                                    showModerationSection
                                    showApprovalStatusRibbon
                                    showApprovalStatus={false}
                                    showContentExpanded
                                    showTagSection={false}
                                    showBibleReferenceSection={false}
                                    onModerateClick={() => setIsEditing(true)}
                                    contentItemSettingCollection={contentItemSettings ?? []} />
                            )}

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

                                THE ROUND IS READ, not invented: the verdict, the votes cast,
                                who is still being waited on, and who else may be asked all come
                                off the approval endpoints. The panel does no fetching of its own
                                — props in, events out — so the assembling is this page's job,
                                done once in useApprovalRound.

                                THE WRITES go back out through the handlers above: a vote is
                                a review row, a decision is the round's, a request is an
                                invitation. Each invalidates the round, so what the panel shows
                                next is what the server holds, not what the click assumed. */}
                            <ReviewPanel
                                entityType="ContentItem"
                                contentType={ContentType[contentItem.contentType] ?? ''}
                                entityOwnerId={contentItem.createdBy}
                                approvalStatus={contentItem.approvalStatus}
                                approvalVerdict={approvalVerdict}
                                approvalReviewCollection={approvalReviewCollection}
                                requestedReviewerCollection={requestedReviewerCollection}
                                reviewerCandidateCollection={reviewerCandidateCollection}
                                isLoading={isRoundLoading}
                                onReviewStatusChanged={(vote) => void castVoteAsync(vote)}
                                onApprovalStatusChanged={(decision, isBypassRequested, bypassReason) =>
                                    void decideAsync(decision, isBypassRequested, bypassReason)}
                                onApprovalReset={() => void resetAsync()}
                                onReviewRequested={(candidate) => void requestReviewAsync(candidate)}
                                onReviewRequestWithdrawn={(candidate) =>
                                    void withdrawReviewRequestAsync(candidate)}
                                showBorder />
                        </div>
                    </div>
                </>
            )}
        </>
    );
};
