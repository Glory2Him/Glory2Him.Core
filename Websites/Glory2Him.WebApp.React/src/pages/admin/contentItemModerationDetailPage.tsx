import { useCallback, useMemo, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { toastSuccess } from '../../brokers/toastBroker.success';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { ContentItemPanel } from '../../components/contentItems/contentItemPanel';
import { ContentItemEditPanel } from '../../components/contentItems/contentItemEditPanel';
import { ReviewPanel } from '../../components/approvals/reviewPanel';
import { ReviewCommentPanel } from '../../components/approvals/reviewCommentPanel';
import { ConfirmDialog } from '../../components/coreUI/confirmDialog';

import {
    ApprovalCommentType,
    ReviewCommentDraft,
    ReviewCommentItem
} from '../../models/components/approvals/reviewCommentItem';

import {
    approvalCommentService
} from '../../services/foundations/approvalCommentService';

import {
    ContentItemSettingsPanel
} from '../../components/contentItemSettings/contentItemSettingsPanel';

import {
    ContentItemSetting
} from '../../models/foundations/contentItemSettings/contentItemSetting';
import { Spinner } from '../../components/coreUI/spinner';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../../models/components/approvals/approvalReviewItem';
import { EntityTypeName } from '../../models/foundations/approvals/approval';
import { useApprovalRound } from '../../hooks/useApprovalRound';
import { useApprovalRoundChanges } from '../../hooks/useApprovalRoundChanges';

// WHERE THE §8.6.2 FEATURE SWITCH WILL BE READ, and it is named for it: IsAIReviewerOffered is
// one of the ApprovalSetting fields #354 still has to add, so there is no policy row to resolve
// yet and this constant stands in the place that resolved value will occupy. When the setting
// lands this becomes a read off the resolved ApprovalSetting, and nothing else on this page
// moves.
//
// ITS SIBLING IS NOT THIS PAGE'S CONCERN. §8.6.2 puts the vote behind a second, child switch —
// IsAIAllowedToVote — which decides whether Berean casts an ApprovalReview alongside the comment
// it always files. That is read where the round is decided, not where the reviewer is offered:
// a Berean that may be asked but may not vote is offered from here identically.
//
// It is a constant rather than a hidden true so that the fail-closed posture is one edit away
// while the backend is unbuilt: the AI reviewer is OFFERED here, but nothing it is offered for
// exists — picking it raises the seam below and writes nothing anywhere.
const isAIReviewerOffered = true;

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
    BereanAIReviewer,
    BereanAIReviewerUserId,
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

    const {
        data: contentItem,
        isLoading,
        isError,
        refetch: refetchContentItem
    } = contentItemService.useGetContentItemById(contentItemId, contentItemId.length > 0);

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

    // ── THE SETTINGS THAT GOVERN THIS ITEM ────────────────────────────────────────
    //
    // Read from the SAME collection the card and the heading already resolve against, so the
    // sidebar cannot disagree with the item beside it about which row is in force.
    //
    // SAVING ALWAYS WRITES AN OVERRIDE. The panel stamps the item's id onto the row and empties
    // the id when the form was seeded from the type default, and the service turns that into a
    // POST or a PUT — narrowing one item never re-shapes every item of its type.
    const createOrUpdateOverride =
        contentItemSettingService.useCreateOrUpdateContentItemSettingOverride();

    const hardRemoveContentItemSetting =
        contentItemSettingService.useHardRemoveContentItemSetting();

    // REMOVING AN OVERRIDE IS PERMANENT and it changes how the item renders for every visitor,
    // so it is confirmed before it is sent. The row is held here between the click and the
    // answer — the panel raises which row it means, and this page is what asks.
    const [overrideToRemove, setOverrideToRemove] = useState<ContentItemSetting | null>(null);

    const saveContentItemSettingAsync = async (contentItemSetting: ContentItemSetting) => {
        try {
            await createOrUpdateOverride.mutateAsync(contentItemSetting);
            toastSuccess('Content settings saved.');
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'We could not save these content settings. Please try again later.'));
        }
    };

    const removeContentItemSettingOverrideAsync = async () => {
        if (overrideToRemove == null) {
            return;
        }

        try {
            await hardRemoveContentItemSetting.mutateAsync(overrideToRemove.id);
            toastSuccess('Content settings override removed.');
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'We could not remove this override. Please try again later.'));
        } finally {
            setOverrideToRemove(null);
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
        isLoading: isRoundLoading,

        // The thread, on the same chain: a comment names the approval it hangs off and nothing
        // about the post being judged, so it waits on the verdict's id like the reviews do. The
        // RAW rows ride along beside the projection because an amend is a PUT of the row that
        // was read — the foundation pins four fields against storage.
        reviewCommentCollection,
        reviewComments,
        areReviewCommentsLoading,

        refresh: refreshApprovalRound
    } = useApprovalRound(EntityTypeName.ContentItem, contentItemId);

    // THE FRESHNESS CHANNEL (design §20.6.1). The round can move under this open tab — another
    // reviewer votes, a comment resolves, an auto-approval fires — and none of that arrives
    // through a write this page made, so nothing above already invalidates it. Polling is the
    // first cut; see useApprovalRoundChanges for why and what it does on reconnect.
    //
    // THE ITEM IS PART OF THE ROUND HERE, even though it is not one of the approval reads. The
    // panel's open-or-closed gates get their status from the STORED ITEM rather than from the
    // verdict — approvalStatus is its own prop precisely so a read-only viewer, who gets no
    // verdict at all, still sees one — so a decision landing elsewhere moves the item's row and
    // nothing else. Refreshing the round alone would repaint the votes while leaving the vote
    // and decision controls live on a round that has already closed, which is the one outcome
    // §20.6.1 names. The writes above already invalidate this read for the same reason.
    const refreshModerationView = useCallback(
        async () => {
            await Promise.all([
                refreshApprovalRound(),
                refetchContentItem({ cancelRefetch: false })
            ]);
        },
        [refreshApprovalRound, refetchContentItem]);

    useApprovalRoundChanges(contentItemId, refreshModerationView);

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
    // DISMISSED IS NOT STANDING. A dismissal writes only StatusId, never IsDeleted, so a
    // dismissed row still comes back on the reviews read — and treating it as the viewer's
    // standing review aims the amend at a row the foundation refuses (§7.7 rule 7: a dismissed
    // review is closed, and the reviewer files a NEW one). The filtered unique index leaves the
    // slot free for exactly that.
    //
    // Reachable the moment a round is reset: every active review is dismissed, the round goes
    // back to the same reviewers, and without this each of them is refused every vote they try
    // to cast on it, permanently.
    const viewerStandingReview = approvalReviews.find(
        (review) =>
            review.createdBy === (user?.userId ?? '')
                && review.isDeleted !== true
                && review.statusId !== ApprovalStatus.Dismissed);

    // WHETHER BEREAN HAS BEEN ASKED, and the only place that answer lives. Every other
    // invitation on this round is a row the server holds and this page reads back; this one has
    // no row to read, so it is state — un-persisted, gone on reload, and never written anywhere.
    const [isAIReviewerRequested, setIsAIReviewerRequested] = useState(false);

    // THE AI-REVIEW SEAM (design §8.6.2, issue #354). Assigning Berean is meant to publish an
    // assignment fact that an AI-review process consumes and answers on the round.
    //
    // ON THIS PAGE THAT ANSWER IS COMMENTS, and only comments. §8.6.2 rules ContentItem out of
    // confidence scoring deliberately — a score judges a PAIRING, and a content item is not one —
    // so the threshold rules that produce a Berean vote are unreachable here whatever
    // IsAIAllowedToVote resolves to. What Berean has to say about a content item arrives as
    // ApprovalComments under its system identity, and a human decides.
    //
    // NONE OF IT EXISTS YET. §13.4 is explicit that no AI broker or content-analysis service is
    // in code today, and §8.6.2's open rulings are unanswered. So this deliberately writes
    // NOTHING: it does not post a review request, because Berean has no account for one to name,
    // and it does not fake a pending row, because a "Requested" chip against a request nobody
    // holds is the panel lying about the round.
    //
    // It says so instead — and it SHOWS the assignment, which is the half that is this page's
    // to finish. The invitation is held HERE, in page state, and merged into the collection the
    // panel renders (see requestedReviewerCollectionWithAIReviewer below): the front end is then
    // complete on its own terms, and what remains for the backend is an endpoint for this
    // handler to call.
    //
    // AN EARLIER VERSION OF THIS COMMENT REFUSED TO SHOW IT, on the ground that a "Requested"
    // chip against a request nobody holds is the panel lying about the round. The reasoning was
    // sound and the conclusion was not: it left the one UI state the feature is made of
    // unreachable and unreviewable, so nobody could tell a finished surface from an unfinished
    // one. What makes the compromise honest is that the invitation is LOCAL and says so — it
    // lives for this page visit, it is written nowhere, and a reload has it gone. Nothing is
    // read back as though the server held it.
    const requestAIReviewAsync = (candidate: ReviewerCandidateItem): void => {
        setIsAIReviewerRequested(true);

        toastSuccess(
            `${candidate.displayName} cannot review yet — the AI review service is still to be `
            + 'built. Nothing has been requested.');
    };

    // THE AI REVIEWER LEAVES BY THE SAME DOOR IT CAME IN. ReviewPanel deliberately routes a
    // withdrawal through the ONE callback — an invitation under Requested is withdrawable and
    // that is the only thing a click there can mean, Berean included — so the split is made
    // here, where the two invitations are actually different things: a person's is a row the
    // server holds, and Berean's is this page's state.
    const withdrawAIReviewRequest = (): void => {
        setIsAIReviewerRequested(false);

        toastSuccess(
            `${BereanAIReviewer.displayName} is no longer down to review this post.`);
    };

    const isAIReviewerCandidate = (candidate: ReviewerCandidateItem): boolean =>
        candidate.userId === BereanAIReviewerUserId;

    // WHAT THE PANEL IS HANDED: the round's real invitations, plus Berean's when it is standing.
    // The merge is here rather than in the panel because the panel is presentation and this is a
    // fact about the round — and rather than in useApprovalRound, which assembles what the SERVER
    // holds and must not be taught to invent a row.
    //
    // The panel needs nothing else to render it: an AI reviewer in this collection already draws
    // with its tagline where a username sits, wears the Requested chip, drops out of Suggestions,
    // counts against the invitation cap and offers the withdraw a click there means. That is the
    // whole reason the seam is worth finishing this way — the front end was one collection short
    // of complete, not one component short.
    const requestedReviewerCollectionWithAIReviewer:
        ReadonlyArray<ReviewerCandidateItem> = useMemo(
            () => isAIReviewerRequested === false
                ? requestedReviewerCollection

                // Filtered, not appended blindly: the day the backend lands, Berean arrives in
                // the server's own collection and this state can still be standing from the
                // click that put it there. Two Bereans in one list is the bug that would follow.
                : [
                    ...requestedReviewerCollection.filter(
                        (candidate) => isAIReviewerCandidate(candidate) === false),
                    BereanAIReviewer
                ],
            [isAIReviewerRequested, requestedReviewerCollection]);

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
        if (isAIReviewerCandidate(candidate)) {
            withdrawAIReviewRequest();

            return;
        }

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

    // ── THE REVIEW THREAD ─────────────────────────────────────────────────────────
    //
    // The conversation the round is actually made of. The panel decides nothing beyond what its
    // own gates render — the foundation re-decides every write against the stored row (§14.6) —
    // and every write here invalidates the thread AND the verdict, because an outstanding
    // comment is one of the block reasons ReviewPanel prints in the column beside it.
    const addReviewComment = approvalCommentService.useAddApprovalComment();
    const modifyReviewComment = approvalCommentService.useModifyApprovalComment();
    const removeReviewComment = approvalCommentService.useRemoveApprovalComment();
    const resolveReviewComment = approvalCommentService.useResolveApprovalComment();

    // WITHDRAWING A COMMENT CANNOT BE UNDONE from any surface the site offers, so it is confirmed
    // before it is sent. The row is held here between the click and the answer — the panel raises
    // which row it means, and this page is what asks.
    const [commentToRemove, setCommentToRemove] = useState<ReviewCommentItem | null>(null);

    // RETHROWN AFTER THE TOAST, and that is the contract rather than an oversight. The add face
    // waits on this promise before it clears the box, so a handler that swallowed its failure
    // would report the error AND throw the reader's words away — the exact pairing that made a
    // refused save cost a moderator their whole comment.
    //
    // It is also passed to the panel BY REFERENCE rather than wrapped in `void (...)` like the
    // handlers below it. Voiding it would discard the promise the panel needs to await, undoing
    // the fix from the other end and leaving the rejection unhandled besides.
    const saveReviewCommentAsync = async (draft: ReviewCommentDraft) => {
        try {
            await addReviewComment.mutateAsync(draft);
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'Your comment could not be saved. Please try again.'));

            throw error;
        }
    };

    // THE STORED ROW is what goes back, with only the edited fields moved onto it. The projection
    // the panel renders carries no audit values, and the foundation compares CreatedBy,
    // CreatedWhen, ApprovalId and UpdatedWhen against storage before it accepts the write — so a
    // PUT composed from the projection alone would be refused.
    const modifyReviewCommentAsync = async (item: ReviewCommentItem) => {
        const storedComment = reviewComments.find(
            (reviewComment) => reviewComment.id === item.id);

        // NEVER SILENTLY. The results panel has already closed the editor by the time this runs,
        // so returning quietly would look exactly like a save that landed while the amendment was
        // dropped on the floor.
        //
        // NOT COVERED BY A UI TEST, and that is a statement about the branch rather than an
        // omission. The results panel closes its editor the moment a row leaves the collection,
        // so a poll landing between opening the editor and pressing Save takes the Save button
        // with it — which leaves only a same-tick race to reach this, and nothing a rendered test
        // can stage. It stays because a silent return is the wrong answer whether or not a test
        // can prove it.
        if (storedComment == null) {
            toastError(
                'This comment is no longer on the thread, so your change was not saved. '
                    + 'It may have been removed while you were editing.');

            return;
        }

        // RETYPING IS THE BIRTH RULE AGAIN (§20.6.3): a Question is outstanding and holds the
        // approval shut, a Comment is settled and never blocks. The add face derives IsResolved
        // from the type for exactly that reason, and an edit that moves the type has to move the
        // flag with it — a remark retyped as a question that kept its settled tick is a question
        // already answered, which is the one pairing the amend gate refuses outright, so the save
        // came back a flat refusal and only the words could ever be changed.
        //
        // The flag is left ALONE where the type did not move, and that is the whole of why this
        // is a transition rather than a derivation: a settled ask may be edited by its author
        // (the gate says so), and re-deriving would silently re-open it, undoing a resolution
        // that answers to the publisher tier.
        const isCommentTypeChanged = item.commentType !== storedComment.commentType;

        try {
            await modifyReviewComment.mutateAsync({
                ...storedComment,
                comment: item.comment,
                commentType: item.commentType,

                isResolved: isCommentTypeChanged
                    ? item.commentType === ApprovalCommentType.Comment
                    : storedComment.isResolved
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'Your comment could not be saved. Please try again.'));
        }
    };

    // SOFT removal, never the hard one: the words stop being part of the conversation and the
    // record of them stays. It also LEAVES THE BLOCK — §8.5 counts comments where IsDeleted is
    // false && IsResolved is false — so withdrawing an outstanding question unblocks the round,
    // which is why the verdict is re-read with it.
    const removeReviewCommentAsync = async () => {
        if (commentToRemove == null) {
            return;
        }

        try {
            await removeReviewComment.mutateAsync({
                approvalCommentId: commentToRemove.id,
                deletionReason: 'Withdrawn by the author'
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'This comment could not be removed. Please try again.'));
        } finally {
            setCommentToRemove(null);
        }
    };

    // Both directions ride one call: a comment settled prematurely must be able to block again
    // (§14.7 rule 5), and the flag is always sent because the endpoint binds it [BindRequired].
    const resolveReviewCommentAsync = async (
        item: ReviewCommentItem,
        isResolved: boolean) => {
        try {
            await resolveReviewComment.mutateAsync({
                approvalCommentId: item.id,
                isResolved
            });
        } catch (error) {
            toastError(extractApiErrorMessage(
                error, 'This comment could not be updated. Please try again.'));
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

                            {/* THE CONVERSATION, under the facts and in the same column as the
                                thing being discussed. It belongs here rather than beside the
                                round because a thread is about the SUBMISSION — its wording, its
                                references, whether a claim checks out — and because the right
                                column is a decision surface that must stay readable at a glance
                                while a thread grows without limit.

                                approvalId comes off the verdict, which is the only read that
                                turns this post into the round its comments hang off. Absent —
                                a post with no approval row, or a caller the verdict refused
                                (§14.5 rule 1) — the panel says so instead of offering a box that
                                cannot post.

                                contentType is the enum MEMBER NAME, never the setting's editable
                                ContentTypeName: it is what §18.6 composes
                                ContentItem-{Type}-Publishers from, and a renamed type must not
                                silently shed its role names.

                                The DELETE confirmation is this page's, not the panel's — the
                                panel raises which row the reader means and the page asks the
                                question, the same split the settings override removal makes. */}
                            <ReviewCommentPanel
                                approvalId={approvalVerdict?.approvalId ?? ''}
                                reviewComments={reviewCommentCollection}
                                entityType="ContentItem"
                                contentType={ContentType[contentItem.contentType] ?? ''}
                                isLoading={areReviewCommentsLoading}
                                isSubmitting={addReviewComment.isPending
                                    || modifyReviewComment.isPending
                                    || removeReviewComment.isPending
                                    || resolveReviewComment.isPending}
                                onSave={saveReviewCommentAsync}
                                onModified={(item) => void modifyReviewCommentAsync(item)}
                                onRemoveRequested={setCommentToRemove}
                                onResolvedChanged={(item, isResolved) =>
                                    void resolveReviewCommentAsync(item, isResolved)}
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
                                requestedReviewerCollection={
                                    requestedReviewerCollectionWithAIReviewer}
                                reviewerCandidateCollection={reviewerCandidateCollection}
                                isLoading={isRoundLoading}
                                onReviewStatusChanged={(vote) => void castVoteAsync(vote)}
                                onApprovalStatusChanged={(decision, isBypassRequested, bypassReason) =>
                                    void decideAsync(decision, isBypassRequested, bypassReason)}
                                onApprovalReset={() => void resetAsync()}
                                onReviewRequested={(candidate) => void requestReviewAsync(candidate)}
                                onReviewRequestWithdrawn={(candidate) =>
                                    void withdrawReviewRequestAsync(candidate)}
                                aiReviewerCandidate={
                                    isAIReviewerOffered ? BereanAIReviewer : undefined}
                                onAIReviewerRequested={(candidate) =>
                                    requestAIReviewAsync(candidate)}
                                showBorder />

                            {/* BENEATH THE ROUND, in the same column: the round is about THIS
                                submission and the settings are about how the item is presented
                                for good, so the decision reads first and the standing policy
                                below it.

                                The collection is the one already fetched above — the panel
                                resolves §6.4 from it exactly as the card does, so no read is
                                added by showing it. */}
                            <ContentItemSettingsPanel
                                contentItemId={contentItem.id}
                                contentType={contentItem.contentType}
                                contentItemSettingCollection={contentItemSettings ?? []}
                                isSubmitting={createOrUpdateOverride.isPending
                                    || hardRemoveContentItemSetting.isPending}
                                onModified={(setting) =>
                                    void saveContentItemSettingAsync(setting)}
                                onOverrideRemoved={setOverrideToRemove}
                                showBorder
                                cssClass="mt-4" />
                        </div>
                    </div>

                    {/* The default title is already "Are you sure?", which is the question this
                        one has to ask. The message says what is lost rather than what happens:
                        a soft delete is invisible to every caller afterwards, so "cannot be
                        undone" is the honest description of it from here. */}
                    <ConfirmDialog
                        visible={commentToRemove != null}
                        message={'This comment will be removed from the review thread. '
                            + 'This action cannot be undone.'}
                        confirmText="Delete"
                        onConfirm={() => void removeReviewCommentAsync()}
                        onCancel={() => setCommentToRemove(null)} />

                    <ConfirmDialog
                        visible={overrideToRemove != null}
                        title="Remove override?"
                        message={'This content item will go back to its content type defaults. '
                            + 'The override is deleted permanently and cannot be recovered.'}
                        confirmText="Remove Override"
                        onConfirm={() => void removeContentItemSettingOverrideAsync()}
                        onCancel={() => setOverrideToRemove(null)} />
                </>
            )}
        </>
    );
};
