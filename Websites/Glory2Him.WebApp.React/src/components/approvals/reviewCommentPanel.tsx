import { useId, useMemo } from 'react';
import { useAuth } from '../securitys/authProvider';
import { ReviewCommentAddPanel } from './reviewCommentAddPanel';
import { ReviewCommentResultsPanel } from './reviewCommentResultsPanel';

import {
    ApprovalCommentType,
    ReviewCommentEvents,
    ReviewCommentItem,
    ReviewCommentText
} from '../../models/components/approvals/reviewCommentItem';

import './approvals.css';

// THE CONVERSATION A REVIEW ROUND IS ACTUALLY MADE OF. ReviewPanel says an approval is held by
// two unresolved comments; this is where they are read, answered and settled.
//
//   ReviewCommentPanel                 the thread, and who may do what to it
//   ├── ReviewCommentAddPanel          the box, the Comment/Question radios, Clear / Save
//   └── ReviewCommentResultsPanel      the rows, newest first, scrolled rather than paged
//       ├── ReviewCommentViewPanel     READ: author, chip, timestamp, resolve tick, Edit / Delete
//       └── ReviewCommentEditPanel     EDIT: the words and the type, Save / Cancel
//
// This panel owns everything the faces share — the ordering, the ownership gate, the resolve
// tier and the ReadOnly veto — and the faces render what it decides. Built the way
// ContentItemPanel is, and for the same reason: one component resolves the facts, the templates
// render them, and a second surface cannot drift into a different answer.
//
// A PURE PRESENTATION COMPONENT: props in, events out, no fetching and no mutation. The CONSUMER
// owns persistence and owns the confirmation dialog — onSave says what the reader wrote,
// onRemoveRequested says what they want gone, and whether that is a POST or a soft delete is the
// page's business.
//
// FRESHNESS CONTRACT, and this thread has a sharper one than most. Two moderators working the
// same submission is exactly the case this surface exists for, so the consumer MUST keep
// reviewComments moving — approvalCommentService.useGetApprovalComments polls and refetches on
// focus for precisely that reason. Without it this panel honestly shows the world as of the last
// props it was handed.
//
// SECURITY POSTURE. Every gate below decides what to RENDER and nothing more. The foundation
// re-decides add, modify, remove and resolve against the stored row (§14.6): a hidden control is
// a courtesy to the reader, never an authorization boundary.
export interface ReviewCommentPanelProps extends ReviewCommentEvents, ReviewCommentText {
    // The approval every comment on this thread is tied to, stamped onto a save. EMPTY MEANS NO
    // THREAD: an item with no approval row, or a caller the verdict refused (§14.5 rule 1), has
    // nothing to comment on — the panel says so rather than offering a box that cannot post.
    approvalId?: string;

    // The thread as the consumer holds it. Sorted DESCENDING here rather than trusted in the
    // order it arrives: the newest comment leads, and no consumer can hand it over the wrong way
    // round.
    reviewComments?: ReadonlyArray<ReviewCommentItem>;

    // Which radio the box opens on, and what Clear returns it to. Comment by default: most of
    // what is written on a round is an observation, and opening on Question would make every
    // absent-minded save block the approval.
    defaultType?: ApprovalCommentType;

    // Whether the panel is drawn as a bordered card. ON by default, like
    // ContentItemSettingsPanel and unlike the content item family: it stands under other panels
    // in a column rather than in a feed of its own, and an edge is what separates it from
    // whatever sits above.
    showBorder?: boolean;

    // Names the entity under approval so the resolve tier can be composed (§18.6,
    // capability-last and plural): {entityType}-Publishers, and — only when entityType is
    // 'ContentItem' and a contentType is given — the narrower ContentItem-{contentType}-Publishers.
    // NOTHING IS FETCHED from them; the consumer resolves the thread itself.
    entityType?: string;
    contentType?: string;

    // ── The collection's states ───────────────────────────────────────────────
    isLoading?: boolean;
    isLoadingMore?: boolean;
    hasMore?: boolean;
    onLoadMore?: () => void;

    // Freezes every button while the consumer is persisting, so one click is one write.
    isSubmitting?: boolean;

    emptyText?: string;
}

export function ReviewCommentPanel({
    approvalId = '',
    reviewComments = [],
    defaultType = ApprovalCommentType.Comment,
    showBorder = true,
    entityType = 'ContentItem',
    contentType = '',
    isLoading = false,
    isLoadingMore = false,
    hasMore = false,
    onLoadMore,
    isSubmitting = false,
    emptyText = 'Nothing has been said about this submission yet.',
    cssClass = '',
    titleText = 'Review Comments',
    onSave,
    onClear,
    onModified,
    onRemoved,
    onRemoveRequested,
    onResolvedChanged
}: ReviewCommentPanelProps) {
    const { isAuthenticated, user, userRoles } = useAuth();
    const headingId = useId();

    const holdsAnyRole = (roles: ReadonlyArray<string>): boolean =>
        roles.some((role) => userRoles.includes(role));

    // THE GLOBAL SANCTION, and it is the only one that reaches the WORDS. §18.6 rule 3 is
    // explicit that a scoped block does not reach the comment thread — a comment is speech about
    // the content, not a write to it — and ValidateUserIsAllowedToComment asks exactly this one
    // name on every write path. Rendering a wider block here would hide a box the server accepts.
    const isBlockedFromCommenting = userRoles.includes('ReadOnly');

    // THE SANCTION AS THE RESOLUTION SEES IT, which is wider, and deliberately so. Settling a
    // comment clears a RequireReviewCommentResolutionBeforeApprovals block — the one comment
    // field that moves a §8.5 gate — so the scoped names count here where they do not above.
    // That asymmetry is the design's, not this panel's: §18.6 rule 3 records it as the place the
    // thread's exemption strains, and the resolve gate now closes it.
    const isBlockedFromResolving =
        isBlockedFromCommenting
        || holdsAnyRole([
            `${entityType}-ReadOnly`,
            `${entityType}-${contentType}-ReadOnly`
        ]);

    // THE PUBLISHER TIER at every scope this entity composes. Not the review tier: an outstanding
    // comment holds the APPROVAL shut, and the people that block stops are the people who decide
    // the approval. A reviewer answering an ask writes a comment of their own.
    const holdsPublisherTier = holdsAnyRole([
        'Administrators',
        'Publishers',
        `${entityType}-Publishers`,
        `${entityType}-${contentType}-Publishers`
    ]);

    // WHO WROTE IT is an account-id comparison — never a display name, which two accounts can
    // share, and which is exactly why the row carries both.
    const viewerOwns = (item: ReviewCommentItem): boolean =>
        isAuthenticated
        && (item.authorId ?? '').length > 0
        && item.authorId === (user?.userId ?? '');

    // ADDING carries no tier at all: §12.3.1 rule 5 — submitters converse in review threads, so
    // any authenticated caller who is not globally sanctioned may speak. With no approval there
    // is nothing to speak about, so the box is withheld from everyone.
    const mayComment =
        isAuthenticated && isBlockedFromCommenting === false && approvalId.length > 0;

    // AMENDING AND WITHDRAWING are the author's alone. No role widens them — a comment belongs to
    // whoever wrote it, and somebody who needs past an outstanding one settles it rather than
    // editing another person's words (§12.3.1 rule 5).
    const mayAmend = (item: ReviewCommentItem): boolean =>
        isBlockedFromCommenting === false && viewerOwns(item);

    // THE RESOLVE TICK, on a QUESTION only. "Is resolved — I got my answer" is meaningless
    // against a remark that asked for nothing, and the flag on such a row was true from birth.
    const mayResolve = (item: ReviewCommentItem): boolean =>
        item.commentType === ApprovalCommentType.Question
        && isAuthenticated
        && isBlockedFromResolving === false
        && (viewerOwns(item) || holdsPublisherTier);

    // NEWEST FIRST, sorted here rather than trusted. A copy is sorted rather than the prop: the
    // array belongs to the consumer, and sorting it in place would mutate react-query's cache.
    //
    // PARSED TO AN INSTANT, not compared as text. createdWhen is a serialised DateTimeOffset and
    // keeps whatever offset the row was written with, so string comparison orders
    // '2026-08-27T09:00:00+02:00' (07:00Z) ahead of '2026-08-27T08:00:00+00:00' (08:00Z) — an
    // hour the wrong way round. It happens to hold today only because the audit stamp is UtcNow,
    // and nothing in the column, the wire model or a test pins that.
    //
    // An unparseable date sorts LAST rather than throwing NaN through the comparator, where it
    // would make the ordering depend on the engine's sort implementation.
    const orderedComments = useMemo(
        () => {
            const writtenAt = (isoDate: string): number => {
                const parsed = Date.parse(isoDate);

                return Number.isNaN(parsed) ? Number.NEGATIVE_INFINITY : parsed;
            };

            return [...reviewComments].sort((first, second) =>
                writtenAt(second.createdWhen) - writtenAt(first.createdWhen));
        },
        [reviewComments]);

    // ONE ACCESSIBLE NAME, and it is the heading. aria-labelledby outranks aria-label in the
    // accessible-name algorithm, so carrying both — as this did — left an ariaLabel prop that
    // read as configurable in the props table and could never once change what a screen reader
    // announced. The panel always renders its h4, so titleText IS the name: a consumer telling
    // two threads apart on one page sets that.
    const panelCssClass = showBorder
        ? `g2h-review-comment-panel border rounded-3 p-3 p-lg-4 ${cssClass}`
        : `g2h-review-comment-panel ${cssClass}`;

    return (
        <section className={panelCssClass} aria-labelledby={headingId}>

            <h4 className="mb-3" id={headingId}>{titleText}</h4>

            {mayComment && (
                <ReviewCommentAddPanel
                    approvalId={approvalId}
                    defaultType={defaultType}
                    isSubmitting={isSubmitting}
                    onSave={onSave}
                    onClear={onClear} />
            )}

            {/* Signed out, or sanctioned, or standing on an item with no round: the reason is
                said rather than the box silently vanishing, or a reader cannot tell whether the
                thread takes comments at all. */}
            {mayComment === false && (
                <p className="small text-body-secondary mb-3">
                    {approvalId.length === 0
                        ? 'This submission has no approval round, so there is nothing to comment on yet.'
                        : isBlockedFromCommenting
                            ? 'Your account is currently read-only, so you cannot add comments.'
                            : 'Sign in to comment on this submission.'}
                </p>
            )}

            <ReviewCommentResultsPanel
                reviewCommentCollection={orderedComments}
                mayAmend={mayAmend}
                mayResolve={mayResolve}
                isLoading={isLoading}
                isLoadingMore={isLoadingMore}
                hasMore={hasMore}
                onLoadMore={onLoadMore}
                isSubmitting={isSubmitting}
                emptyText={emptyText}
                onModified={onModified}
                onRemoved={onRemoved}
                onRemoveRequested={onRemoveRequested}
                onResolvedChanged={onResolvedChanged} />
        </section>
    );
}
