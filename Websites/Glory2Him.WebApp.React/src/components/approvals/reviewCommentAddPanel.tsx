import { useEffect, useId, useState } from 'react';
import { CommentTypeRadioGroup } from './commentTypeRadioGroup';

import {
    ApprovalCommentType,
    ReviewCommentEvents
} from '../../models/components/approvals/reviewCommentItem';

// THE TOP OF THE THREAD: one box, one choice, two buttons.
//
// The choice is the whole reason this face is not just a textarea. §7.8 draws a line between a
// remark, which asks for nothing and never blocks, and an ask, which holds the approval shut
// until it is settled — and until the reader states which they are writing, nothing downstream
// can tell. The radios are that statement.
//
// A PURE PRESENTATION COMPONENT like every face in this family. It owns the DRAFT — the words
// and the choice between one save and the next — and nothing else: the consumer persists, and
// what the thread then shows is the collection it re-read.
export interface ReviewCommentAddPanelProps
    extends Pick<ReviewCommentEvents, 'onSave' | 'onClear'> {
    // Stamped onto the draft, so the consumer never has to pair the words back up with a round.
    approvalId: string;

    // Which radio the box opens on, and what Clear returns it to.
    defaultType?: ApprovalCommentType;

    isSubmitting?: boolean;

    placeholderText?: string;
    maxLength?: number;
}

export function ReviewCommentAddPanel({
    approvalId,
    defaultType = ApprovalCommentType.Comment,
    isSubmitting = false,
    placeholderText = 'Write a comment or ask a question…',

    // The foundation caps the text at 1000 characters and the column agrees with it, so the box
    // refuses what the server would rather than composing a save that comes back a 400.
    maxLength = 1000,
    onSave,
    onClear
}: ReviewCommentAddPanelProps) {
    const [draft, setDraft] = useState('');
    const [commentType, setCommentType] = useState<ApprovalCommentType>(defaultType);
    const draftFieldId = useId();

    // A changed defaultType prop overrules a choice the reader has not committed — the same
    // identity-keyed reset the content item form engine keeps for its draft. Not a reset of the
    // WORDS: those are the reader's, and dropping them because a prop moved would lose work.
    useEffect(() => {
        setCommentType(defaultType);
    }, [defaultType]);

    // BLANK IS REFUSED, and not merely as tidiness: an outstanding comment with no text holds an
    // approval shut while saying nothing, which is exactly what §7.8 requires the text cap and
    // the required rule to prevent. The foundation refuses it too — this only spares the round
    // trip.
    const canSave = draft.trim().length > 0 && isSubmitting === false;

    const clear = () => {
        setDraft('');
        setCommentType(defaultType);
        onClear?.();
    };

    // THE BOX CLEARS ON A COMMITTED SAVE, and only then. onSave may hand back a promise, and if
    // it does this waits: a refusal leaves the words exactly where the reader typed them.
    //
    // Clearing first was the obvious shape and the wrong one. The draft IS the payload here —
    // unlike the association panel this borrowed from, where the value is a short tag and the
    // chip appears optimistically — so a round that closed a moment ago, or a sanction applied
    // mid-session, cost the moderator a comment they then had to write again from memory. The
    // edit face never had that behaviour, which made the same user lose work on one face and not
    // the other.
    //
    // The TYPE goes back to the default with it: the next thing written is a fresh decision, not
    // a continuation of the last one.
    const save = async () => {
        if (canSave === false) {
            return;
        }

        const draftToSave = { approvalId, comment: draft.trim(), commentType };

        try {
            await onSave?.(draftToSave);
        } catch {
            // The CONSUMER reports it — this panel has no toast and no idea what went wrong. All
            // that is decided here is that the words stay.
            return;
        }

        setDraft('');
        setCommentType(defaultType);
    };

    return (
        <div className="g2h-review-comment-add mb-4">
            <textarea
                id={draftFieldId}
                className="form-control mb-2"
                rows={3}
                value={draft}
                maxLength={maxLength}
                placeholder={placeholderText}
                aria-label={placeholderText}
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
                        onClick={clear}>
                        Clear
                    </button>

                    <button
                        type="button"
                        className="btn btn-primary mb-0"
                        disabled={canSave === false}
                        onClick={() => void save()}>
                        Save
                    </button>
                </div>
            </div>
        </div>
    );
}
