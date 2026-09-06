import { useId, useState } from 'react';
import { CommentTypeRadioGroup } from './commentTypeRadioGroup';

import {
    ApprovalCommentType,
    ReviewCommentEvents,
    ReviewCommentItem
} from '../../models/components/approvals/reviewCommentItem';

// THE EDIT FACE OF ONE ROW — the same row with its words and its type made editable, and Save /
// Cancel beneath them.
//
// NOTHING IS PERSISTED UNTIL SAVE, and CANCEL RESTORES THE ROW VERBATIM. That is not undo logic
// here: the draft is local state seeded from the item, and cancelling simply closes the face, so
// the view template re-renders from an item this panel never touched. There is nothing to put
// back because nothing was taken.
//
// A PURE PRESENTATION COMPONENT. The gate that decides whether this face is reachable at all is
// ReviewCommentPanel's ownership check; by the time this renders, the reader is the author.
export interface ReviewCommentEditPanelProps extends Pick<ReviewCommentEvents, 'onModified'> {
    reviewComment: ReviewCommentItem;
    isSubmitting?: boolean;
    maxLength?: number;
    onCancelled?: () => void;
}

export function ReviewCommentEditPanel({
    reviewComment,
    isSubmitting = false,

    // The same cap the add face and the foundation keep. Stated rather than shared through a
    // constant because it is the column's, and the two faces reaching for one another for it
    // would be the coupling this family avoids.
    maxLength = 1000,
    onModified,
    onCancelled
}: ReviewCommentEditPanelProps) {
    const [draft, setDraft] = useState(reviewComment.comment);

    const [commentType, setCommentType] =
        useState<ApprovalCommentType>(reviewComment.commentType);

    const draftFieldId = useId();

    // Blank is refused for the same reason the add face refuses it: an outstanding comment with
    // no text holds an approval shut while saying nothing (§7.8), and the foundation refuses it
    // too — this only spares the round trip.
    const canSave = draft.trim().length > 0 && isSubmitting === false;

    // THE WHOLE ROW goes back, with only the two edited fields moved. An amend is a PUT of the
    // row that was read: the foundation pins CreatedBy, CreatedWhen, ApprovalId and UpdatedWhen
    // against storage before it accepts the write, so a fresh object carrying only the new words
    // is refused.
    //
    // isResolved is NOT touched here. Whether a comment is settled is a different decision with a
    // different tier (§14.7 rule 5), and moving it under cover of an edit would let an author
    // clear a §8.5 gate through a control that says nothing about gates.
    const save = () => {
        if (canSave === false) {
            return;
        }

        onModified?.({
            ...reviewComment,
            comment: draft.trim(),
            commentType
        });
    };

    return (
        <article className="g2h-review-comment-row py-3">
            <label className="form-label small fw-semibold" htmlFor={draftFieldId}>
                Edit your comment
            </label>

            <textarea
                id={draftFieldId}
                className="form-control mb-2"
                rows={3}
                value={draft}
                maxLength={maxLength}
                disabled={isSubmitting}
                onChange={(event) => setDraft(event.target.value)}></textarea>

            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2">
                <CommentTypeRadioGroup
                    value={commentType}
                    onChange={setCommentType}
                    disabled={isSubmitting} />

                <div className="d-flex gap-2">
                    <button
                        type="button"
                        className="btn btn-outline-secondary mb-0"
                        disabled={isSubmitting}
                        onClick={() => onCancelled?.()}>
                        Cancel
                    </button>

                    <button
                        type="button"
                        className="btn btn-primary mb-0"
                        disabled={canSave === false}
                        onClick={save}>
                        Save
                    </button>
                </div>
            </div>
        </article>
    );
}
