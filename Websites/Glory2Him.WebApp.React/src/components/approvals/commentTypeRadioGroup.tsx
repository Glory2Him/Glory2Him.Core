import { useId } from 'react';

import {
    ApprovalCommentType
} from '../../models/components/approvals/reviewCommentItem';

// THE ONE CONTROL THAT SAYS WHAT A COMMENT IS — a remark or an ask (§7.8).
//
// It exists as its own component because BOTH faces of the thread need it and it must mean the
// same thing on each: the add panel asks the question at birth, the edit panel lets the author
// correct their answer. Two copies of the markup drifted the moment either label, either id
// scheme or the group's accessible name changed, and nothing compared them — the panel's own
// tests had to scope their queries by row precisely because the two were indistinguishable in
// the DOM.
//
// A RADIOGROUP RATHER THAN TWO LOOSE RADIOS: a screen reader announces "Comment, 1 of 2" and the
// arrow keys move between them, which is what the pair actually means.
export interface CommentTypeRadioGroupProps {
    value: ApprovalCommentType;
    onChange: (commentType: ApprovalCommentType) => void;
    disabled?: boolean;

    // Every instance mints its own name, so two of these on one screen — the add box above an
    // open editor — are two independent groups rather than one with four options.
    groupLabel?: string;
}

const options: ReadonlyArray<{ value: ApprovalCommentType; label: string }> = [
    { value: ApprovalCommentType.Comment, label: 'Comment' },
    { value: ApprovalCommentType.Question, label: 'Question' }
];

export function CommentTypeRadioGroup({
    value,
    onChange,
    disabled = false,
    groupLabel = 'Comment type'
}: CommentTypeRadioGroupProps) {
    const radioGroupName = useId();

    return (
        <div
            className="d-flex align-items-center gap-3"
            role="radiogroup"
            aria-label={groupLabel}>

            {options.map((option) => (
                <div className="form-check mb-0" key={option.label}>
                    <input
                        className="form-check-input"
                        type="radio"
                        name={radioGroupName}
                        id={`${radioGroupName}-${option.label}`}
                        checked={value === option.value}
                        disabled={disabled}
                        onChange={() => onChange(option.value)} />

                    <label
                        className="form-check-label"
                        htmlFor={`${radioGroupName}-${option.label}`}>
                        {option.label}
                    </label>
                </div>
            ))}
        </div>
    );
}
