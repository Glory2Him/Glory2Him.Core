import { describe, expect, it } from 'vitest';
import { ApprovalStatus } from '../../../models/components/approvals/approvalReviewItem';

import {
    ApprovalReview,
    ApprovalReviewRequest,
    ReviewerCandidate,
    ReviewerDisplayName
} from '../../../models/foundations/approvals/approval';

import {
    toApprovalReviewItem,
    toRequestedReviewerItem,
    toReviewerCandidateItem
} from './toReviewPanelItems';

// THE NAME RESOLVER'S OUTPUT (§16.7.4). One read names everybody the round involves — cast
// reviewers and outstanding invitations alike — which is what lets a review row and a request row
// be labelled from the same place instead of from two.
const resolvedNames: ReadonlyArray<ReviewerDisplayName> = [
    { userId: 'user-mary', displayName: 'Mary Adeyemi', userName: 'mary.a' },
    { userId: 'user-john', displayName: 'John', userName: 'john.b' }
];

const review = (createdBy: string): ApprovalReview => ({
    id: 'review-1',
    approvalId: 'approval-1',
    statusId: ApprovalStatus.Approved,
    comment: '',
    createdBy,
    createdWhen: '2026-09-06T00:00:00Z',
    updatedBy: createdBy,
    updatedWhen: '2026-09-06T00:00:00Z',
    isDeleted: false
});

describe('toReviewPanelItems', () => {
    describe('a review', () => {
        it('should carry the username off the resolver', () => {
            // when
            const item = toApprovalReviewItem(review('user-mary'), resolvedNames);

            // then
            expect(item.reviewerDisplayName).toBe('Mary Adeyemi');
            expect(item.reviewerUserName).toBe('mary.a');
        });

        /// An id naming no account resolves to NEITHER field. The display name has always had an
        /// honest fallback here; the username has none to give, and inventing one would label a
        /// departed reviewer as somebody.
        it('should leave the username absent when the id names no account', () => {
            // when
            const item = toApprovalReviewItem(review('user-departed'), resolvedNames);

            // then
            expect(item.reviewerDisplayName).toBe('Unknown reviewer');
            expect(item.reviewerUserName).toBeUndefined();
        });

        it('should leave the username absent when no resolver output was supplied', () => {
            // when
            const item = toApprovalReviewItem(review('user-mary'));

            // then
            expect(item.reviewerUserName).toBeUndefined();
        });
    });

    /// A REQUEST ROW IS HALF DENORMALISED, deliberately. §16.7.4 keeps the write-time display
    /// name on the row and refuses to add a second denormalised field beside it, so the two
    /// halves of the label come from two places — and this is where they are put back together.
    describe('a request', () => {
        const request: ApprovalReviewRequest = {
            id: 'request-1',
            approvalId: 'approval-1',
            requestedUserId: 'user-mary',
            requestedUserDisplayName: 'Mary A.',
            isDeleted: false
        };

        it('should keep the row own display name and take the username from the resolver', () => {
            // when
            const item = toRequestedReviewerItem(request, resolvedNames);

            // then: the ROW's name, not the resolver's — it is the record of who was asked
            expect(item.displayName).toBe('Mary A.');
            expect(item.userName).toBe('mary.a');
        });

        it('should render the row alone when the resolver names nobody', () => {
            // when
            const item = toRequestedReviewerItem(request);

            // then
            expect(item.displayName).toBe('Mary A.');
            expect(item.userName).toBeUndefined();
        });
    });

    describe('a candidate', () => {
        it('should carry the username straight across', () => {
            // given
            const candidate: ReviewerCandidate = {
                userId: 'user-paul',
                displayName: 'Paul Nkemdirim',
                userName: 'paul.n'
            };

            // when
            const item = toReviewerCandidateItem(candidate);

            // then
            expect(item).toEqual({
                userId: 'user-paul',
                displayName: 'Paul Nkemdirim',
                userName: 'paul.n'
            });
        });
    });
});
