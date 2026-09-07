import { useId } from 'react';
import { formatDateTime } from '../coreUI/dateFormats';

import {
    ApprovalCommentType,
    ReviewCommentEvents,
    ReviewCommentItem
} from '../../models/components/approvals/reviewCommentItem';

// THE READ FACE OF ONE ROW: who said it, what kind of thing it is, when, the words, whether it
// is settled, and — for whoever wrote it — the two ways to change that.
//
// A PURE TEMPLATE. Every gate was decided by ReviewCommentPanel and arrives as a boolean, so
// this file contains no role name and no ownership comparison: two places deciding the same
// question is two places to disagree.
export interface ReviewCommentViewPanelProps
    extends Pick<
        ReviewCommentEvents, 'onRemoved' | 'onRemoveRequested' | 'onResolvedChanged'> {
    reviewComment: ReviewCommentItem;

    // Whether Edit and Delete render. The author's alone — no role widens amending or
    // withdrawing somebody else's words (§12.3.1 rule 5).
    showsAmendActions?: boolean;

    // Whether the settled tick renders. A QUESTION only, and only for the author or the publisher
    // tier — ReviewCommentPanel owns both halves of that.
    showsResolveControl?: boolean;

    isSubmitting?: boolean;

    onEditClick?: () => void;
}

// text-bg-* rather than bg-*, like every other badge in this codebase (postTypeBadge, articleCard,
// userMenu, and contentItemSettingsViewPanel's scope chip, which is the closest sibling). The
// pairing utility sets a foreground MATCHED to the background instead of leaning on .badge's
// default, so the chip's contrast follows a theme change rather than depending on one.
const chipFor = (commentType: ApprovalCommentType): { label: string; cssClass: string } =>
    commentType === ApprovalCommentType.Question
        ? { label: 'Question', cssClass: 'text-bg-primary' }
        : { label: 'Comment', cssClass: 'text-bg-secondary' };

// The stored timestamp is ISO 8601 off the row. An unparseable one renders as nothing rather
// than as "Invalid Date" — a broken date must not be the loudest thing on a row.
const writtenWhen = (isoDate: string): string => {
    const parsed = new Date(isoDate);

    return Number.isNaN(parsed.getTime()) ? '' : formatDateTime(parsed);
};

export function ReviewCommentViewPanel({
    reviewComment,
    showsAmendActions = false,
    showsResolveControl = false,
    isSubmitting = false,
    onEditClick,
    onRemoveRequested,
    onRemoved,
    onResolvedChanged
}: ReviewCommentViewPanelProps) {
    const resolveFieldId = useId();
    const chip = chipFor(reviewComment.commentType);
    const timestamp = writtenWhen(reviewComment.createdWhen);

    // WHICH ROW, not just whose. One author answering two questions on a round produced two Edit
    // buttons and two Delete buttons with identical accessible names, so a screen-reader user
    // tabbing the thread could not tell which comment they were about to change or remove. The
    // timestamp is what separates two rows by the same person; it falls back to the name alone
    // when the date will not parse, which is the only case where there is nothing to add.
    const rowName = timestamp.length > 0
        ? `by ${reviewComment.authorDisplayName}, ${timestamp}`
        : `by ${reviewComment.authorDisplayName}`;

    // DELETE GOES THROUGH THE CONSUMER when one is listening, because the confirmation is page
    // chrome this panel cannot place. onRemoved alone is honoured for the surface that genuinely
    // has nothing to ask — a doc page, a test — and never in addition, or one click would raise
    // both the request and the removal.
    const requestRemoval = () => {
        if (onRemoveRequested != null) {
            onRemoveRequested(reviewComment);

            return;
        }

        onRemoved?.(reviewComment);
    };

    return (
        <article className="g2h-review-comment-row py-3">
            <div className="d-flex flex-wrap align-items-start justify-content-between gap-2">
                <div className="d-flex flex-wrap align-items-center gap-2">
                    <span className="fw-bold">{reviewComment.authorDisplayName}</span>

                    {/* The username disambiguates: two accounts can share a display name, and on
                        a thread that is the difference between reading the submitter's answer and
                        somebody else's. Muted, because it is the tiebreak rather than the name.

                        Every account has one, so this is not an optional field — the only reason
                        it can be empty is an author whose account has since been deleted, and
                        that row has no display name either. */}
                    {reviewComment.authorUserName.length > 0 && (
                        <span className="small text-body-secondary">
                            ({reviewComment.authorUserName})
                        </span>
                    )}

                    <span className={`badge ${chip.cssClass}`}>{chip.label}</span>
                </div>

                {timestamp.length > 0 && (
                    <time
                        className="small text-body-secondary"
                        dateTime={reviewComment.createdWhen}>
                        {timestamp}
                    </time>
                )}
            </div>

            {/* pre-wrap so the paragraphs somebody typed survive. The words are rendered as TEXT
                and never as markup — this thread is written by anybody who can contribute. */}
            <p className="g2h-review-comment-text mb-2 mt-2">{reviewComment.comment}</p>

            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
                <div>
                    {showsResolveControl && (
                        <div className="form-check mb-0">
                            <input
                                className="form-check-input"
                                type="checkbox"
                                id={resolveFieldId}
                                checked={reviewComment.isResolved}
                                disabled={isSubmitting}
                                onChange={(event) =>
                                    onResolvedChanged?.(reviewComment, event.target.checked)} />

                            {/* Both halves are inside the label, so the muted phrase is part of
                                what a screen reader announces and part of what a click targets —
                                it explains the tick rather than decorating it. */}
                            <label className="form-check-label" htmlFor={resolveFieldId}>
                                Is resolved
                                <span className="text-body-secondary">
                                    {' '}&mdash; I got my answer
                                </span>
                            </label>
                        </div>
                    )}
                </div>

                {showsAmendActions && (
                    <div className="d-flex gap-2">
                        <button
                            type="button"
                            className="btn btn-sm btn-outline-secondary mb-0"
                            disabled={isSubmitting}
                            aria-label={`Edit comment ${rowName}`}
                            onClick={() => onEditClick?.()}>
                            <i className="bi bi-pencil me-1" aria-hidden="true"></i>Edit
                        </button>

                        <button
                            type="button"
                            className="btn btn-sm btn-outline-danger mb-0"
                            disabled={isSubmitting}
                            aria-label={`Delete comment ${rowName}`}
                            onClick={requestRemoval}>
                            <i className="bi bi-trash me-1" aria-hidden="true"></i>Delete
                        </button>
                    </div>
                )}
            </div>
        </article>
    );
}
