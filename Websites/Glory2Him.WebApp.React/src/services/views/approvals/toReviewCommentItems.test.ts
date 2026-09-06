import { describe, expect, it } from 'vitest';
import { toReviewCommentItem, toReviewCommentItems } from './toReviewCommentItems';
import { ApprovalCommentType } from '../../../models/foundations/approvals/approvalComment';
import { ApprovalComment } from '../../../models/foundations/approvals/approvalComment';
import { ReviewerDisplayName } from '../../../models/foundations/approvals/approval';

// THE PROJECTION THE THREAD RENDERS FROM. Nothing above it can catch a mistake here: the panel
// tests hand the panel finished items, and the page test mocks the service that produces them —
// so the one place the wire row becomes a rendered row had no coverage at all until this file.

const approvalComment = (overrides: Partial<ApprovalComment> = {}): ApprovalComment => ({
    id: 'comment-1',
    approvalId: 'approval-1',
    comment: 'Is this quote actually Moody?',
    commentType: ApprovalCommentType.Question,
    isResolved: false,
    createdBy: 'user-john',
    createdWhen: '2026-08-27T09:00:00Z',
    updatedBy: 'user-john',
    updatedWhen: '2026-08-27T09:00:00Z',
    isDeleted: false,
    ...overrides
});

const names: ReadonlyArray<ReviewerDisplayName> = [
    { userId: 'user-john', displayName: 'John Mensah', userName: 'jmensah' }
];

describe('toReviewCommentItems', () => {
    it('should carry the row across and resolve its author from the names read', () => {
        // when
        const item = toReviewCommentItem(approvalComment(), names);

        // then
        expect(item).toEqual({
            id: 'comment-1',
            approvalId: 'approval-1',
            comment: 'Is this quote actually Moody?',
            commentType: ApprovalCommentType.Question,
            isResolved: false,

            // CreatedBy is the author — the audit name, not a reviewer column
            authorId: 'user-john',
            authorDisplayName: 'John Mensah',
            authorUserName: 'jmensah',
            createdWhen: '2026-08-27T09:00:00Z',
            updatedWhen: '2026-08-27T09:00:00Z'
        });
    });

    // §16.7.4 leaves an id that names nobody OUT of its answer rather than erroring, so one
    // departed account must not break a whole thread.
    it('should fall back for an author the names read did not answer for', () => {
        // when
        const item = toReviewCommentItem(
            approvalComment({ createdBy: 'user-gone' }), names);

        // then
        expect(item.authorDisplayName).toBe('Unknown author');

        // empty rather than absent: the field is required, and the view draws no brackets for it
        expect(item.authorUserName).toBe('');
    });

    it('should fall back when no names were supplied at all', () => {
        // when
        const item = toReviewCommentItem(approvalComment());

        // then
        expect(item.authorDisplayName).toBe('Unknown author');
        expect(item.authorUserName).toBe('');
    });

    // The read already filters these server-side, but this projection is also handed collections
    // a test or a doc page composed by hand — and a withdrawn comment must never render,
    // whichever route it arrived by.
    it('should drop soft-deleted rows', () => {
        // given
        const collection = [
            approvalComment({ id: 'live' }),
            approvalComment({ id: 'withdrawn', isDeleted: true })
        ];

        // when
        const items = toReviewCommentItems(collection, names);

        // then
        expect(items.map((item) => item.id)).toEqual(['live']);
    });

    it('should project an empty collection to an empty one', () => {
        expect(toReviewCommentItems([], names)).toEqual([]);
    });
});
