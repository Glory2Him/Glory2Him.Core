import { ReactElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '../securitys/authProvider';
import { ReviewPanel } from './reviewPanel';
import {
    ApprovalDecision,
    ApprovalReviewItem,
    ApprovalStatus,
    ApprovalVerdictItem,
    ReviewerCandidateItem
} from '../../models/components/approvals/approvalReviewItem';
import { createAuthState, renderWithAuth, signInAs, signOut } from '../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// signInAs mints userId 'user-1' with displayName 'Tester'.
const ViewerId = 'user-1';

const review = (
    reviewerDisplayName: string,
    reviewerUserId: string,
    vote: ApprovalStatus
): ApprovalReviewItem => ({ reviewerDisplayName, reviewerUserId, vote });

const johnApproved = review('John', 'user-john', ApprovalStatus.Approved);
const susanApproved = review('Susan', 'user-susan', ApprovalStatus.Approved);
const viewerRejected = review('Tester', ViewerId, ApprovalStatus.Rejected);

const mary: ReviewerCandidateItem = {
    userId: 'user-mary',
    displayName: 'Mary',
    userName: 'mary.m'
};

const paul: ReviewerCandidateItem = {
    userId: 'user-paul',
    displayName: 'Paul',
    userName: 'paul.p'
};

const verdictWith = (overrides: Partial<ApprovalVerdictItem> = {}): ApprovalVerdictItem => ({
    approvalId: 'approval-1',
    approvalStatus: ApprovalStatus.Submitted,
    blockReasons: [],
    isBlocked: false,
    isBypassAllowedForCurrentUser: false,
    canApprove: true,
    approvalCount: 2,
    requiredNumberOfApprovals: 3,
    unresolvedApprovalCommentCount: 0,
    ...overrides
});

const blockedVerdict = (overrides: Partial<ApprovalVerdictItem> = {}): ApprovalVerdictItem =>
    verdictWith({
        blockReasons: [{ code: 1, message: 'At least 3 approving review(s) is required by reviewers.' }],
        isBlocked: true,
        canApprove: false,
        ...overrides
    });

const rowNames = (): Array<string | null | undefined> =>
    Array.from(document.querySelectorAll('.g2h-review-row > span:first-child'))
        .map((element) => element.textContent);

const statusPillText = (): string | null | undefined =>
    document.querySelector('.g2h-review-status-pill')?.textContent?.trim();

describe('ReviewPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('reviews', () => {
        it('should render the default titles', () => {
            // when
            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.getByRole('heading', { name: 'Approval Reviews' })).toBeInTheDocument();
            expect(screen.getByRole('heading', { name: 'Review Outcome' })).toBeInTheDocument();
        });

        it('should list the viewer first and everyone else alphabetically', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when — Zoe supplied before Adam to prove sorting, viewer's row last in the input
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[
                        review('Zoe', 'user-zoe', ApprovalStatus.Approved),
                        review('Adam', 'user-adam', ApprovalStatus.Approved),
                        viewerRejected
                    ]} />);

            // then
            expect(rowNames()).toEqual(['Tester', 'Adam', 'Zoe']);
        });

        it('should synthesize a placeholder vote row for an eligible viewer with no vote', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]} />);

            // then
            expect(screen.getByRole('button', { name: 'Vote...' })).toBeInTheDocument();
            expect(rowNames()).toEqual(['Tester', 'John']);
        });

        it('should not offer a vote to the entity owner', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    entityOwnerId={ViewerId}
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]} />);

            // then
            expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
        });

        it('should not offer a vote to an anonymous reader', () => {
            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]} />);

            // then
            expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
        });

        it('should not offer a vote to a reader without a review-tier role', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]} />);

            // then
            expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
        });

        it.each([
            ['ContentItem-Reviewer'],
            ['ContentItem-Blog-Reviewer'],
            ['ContentItem-Publisher'],
            ['Administrators']
        ])('should offer a vote to the scoped role %s', (role: string) => {
            // given
            signInAs(authState, [role]);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    contentType="Blog"
                    approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.getByRole('button', { name: 'Vote...' })).toBeInTheDocument();
        });

        it('should not treat another entity\'s scoped role as eligible here', () => {
            // given
            signInAs(authState, ['Tag-Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
        });

        it('should raise onReviewStatusChanged when the viewer casts a vote', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    onReviewStatusChanged={onReviewStatusChanged} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Vote...' }));
            await userEvent.click(screen.getByRole('button', { name: /I am happy with this item/ }));

            // then
            expect(onReviewStatusChanged).toHaveBeenCalledTimes(1);
            expect(onReviewStatusChanged).toHaveBeenCalledWith(ApprovalStatus.Approved);
        });

        it('should raise onReviewStatusChanged when the viewer changes their vote', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[viewerRejected]}
                    onReviewStatusChanged={onReviewStatusChanged} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Rejected' }));
            await userEvent.click(screen.getByRole('button', { name: /I am happy with this item/ }));

            // then
            expect(onReviewStatusChanged).toHaveBeenCalledWith(ApprovalStatus.Approved);
        });

        it('should not raise onReviewStatusChanged when the viewer re-picks their current vote', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[viewerRejected]}
                    onReviewStatusChanged={onReviewStatusChanged} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Rejected' }));
            await userEvent.click(
                screen.getByRole('button', { name: /I do not think we should approve/ }));

            // then
            expect(onReviewStatusChanged).not.toHaveBeenCalled();
        });

        it('should freeze the viewer\'s vote into a badge once the approval is decided', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Approved}
                    approvalReviewCollection={[viewerRejected]} />);

            // then — the vote is still visible, but no longer a control
            expect(screen.queryByRole('button', { name: 'Rejected' })).not.toBeInTheDocument();
            expect(screen.getByText('Rejected')).toBeInTheDocument();
        });

        it('should render awaiting-review rows for pending review requests', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]}
                    requestedReviewerCollection={[mary]} />);

            // then
            expect(screen.getByText('Mary')).toBeInTheDocument();
            expect(screen.getByText('Awaiting review')).toBeInTheDocument();
        });

        it('should not render the viewer among the requested rows', () => {
            // given — the viewer's own row is already the vote dropdown; one person, one row
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[
                        { userId: ViewerId, displayName: 'Tester' },
                        mary
                    ]} />);

            // then
            expect(rowNames()).toEqual(['Tester', 'Mary']);
            expect(screen.getAllByText('Awaiting review')).toHaveLength(1);
        });

        it('should render the loading text instead of the review rows while loading', () => {
            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    isLoading={true}
                    approvalReviewCollection={[johnApproved]} />);

            // then
            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByText('John')).not.toBeInTheDocument();
        });
    });

    describe('review requests', () => {
        it.each([
            ['Reviewer'],
            ['ContentItem-Reviewer'],
            ['Publisher'],
            ['Administrators']
        ])('should show the request cog to %s', (role: string) => {
            // given
            signInAs(authState, [role]);

            // when
            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.getByRole('button', { name: 'Request a review' })).toBeInTheDocument();
        });

        it('should hide the request cog from a roleless reader and while signed out', () => {
            // given / when — anonymous
            const { unmount } = renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.queryByRole('button', { name: 'Request a review' }))
                .not.toBeInTheDocument();

            unmount();

            // given / when — signed in without a qualifying role
            signInAs(authState, ['Users']);

            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Submitted} />);

            // then
            expect(screen.queryByRole('button', { name: 'Request a review' }))
                .not.toBeInTheDocument();
        });

        it('should hide the request cog once the approval is decided', () => {
            // given
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={ApprovalStatus.Approved} />);

            // then
            expect(screen.queryByRole('button', { name: 'Request a review' }))
                .not.toBeInTheDocument();
        });

        it('should raise onReviewerLookupRequested and list the candidates when opened', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewerLookupRequested = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]}
                    onReviewerLookupRequested={onReviewerLookupRequested} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(onReviewerLookupRequested).toHaveBeenCalledTimes(1);
            expect(screen.getByRole('button', { name: 'Mary' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Paul' })).toBeInTheDocument();
        });

        it('should show the loading text in the picker while candidates load', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    isCandidatesLoading={true} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(screen.getByText('Loading…')).toBeInTheDocument();
        });

        it('should filter the candidates by display name and by username', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]} />);

            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            const filterBox = screen.getByRole('textbox', { name: 'Filter by name' });

            // when — by display name
            await userEvent.type(filterBox, 'mar');

            // then
            expect(screen.getByRole('button', { name: 'Mary' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Paul' })).not.toBeInTheDocument();

            // when — by username
            await userEvent.clear(filterBox);
            await userEvent.type(filterBox, 'paul.p');

            // then
            expect(screen.getByRole('button', { name: 'Paul' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Mary' })).not.toBeInTheDocument();
        });

        it('should raise onReviewRequested with the candidate and close the picker', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]}
                    onReviewRequested={onReviewRequested} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            await userEvent.click(screen.getByRole('button', { name: 'Mary' }));

            // then
            expect(onReviewRequested).toHaveBeenCalledTimes(1);
            expect(onReviewRequested).toHaveBeenCalledWith(mary);
            expect(screen.queryByRole('button', { name: 'Paul' })).not.toBeInTheDocument();
        });

        it('should raise onReviewRequestWithdrawn from a requested row\'s withdraw control', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequestWithdrawn = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]}
                    onReviewRequestWithdrawn={onReviewRequestWithdrawn} />);

            // when
            await userEvent.click(
                screen.getByRole('button', { name: 'Withdraw review request Mary' }));

            // then
            expect(onReviewRequestWithdrawn).toHaveBeenCalledTimes(1);
            expect(onReviewRequestWithdrawn).toHaveBeenCalledWith(mary);
        });

        it('should not offer the withdraw control to a roleless reader', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]} />);

            // then — the row still renders; only the control is withheld
            expect(screen.getByText('Mary')).toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Withdraw review request Mary' }))
                .not.toBeInTheDocument();
        });
    });

    describe('outcome', () => {
        it('should show the blocked panel with every reason message when blocked', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({
                        blockReasons: [
                            { code: 1, message: 'At least 3 approving review(s) is required by reviewers.' },
                            { code: 2, message: 'All review comments must be resolved.' }
                        ]
                    })} />);

            // then
            expect(screen.getByText('Approval is blocked')).toBeInTheDocument();

            expect(screen.getByText('At least 3 approving review(s) is required by reviewers.'))
                .toBeInTheDocument();

            expect(screen.getByText('All review comments must be resolved.')).toBeInTheDocument();
        });

        it('should not show the blocked panel when nothing blocks approval', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith()} />);

            // then
            expect(screen.queryByText('Approval is blocked')).not.toBeInTheDocument();
        });

        it.each([
            [ApprovalStatus.Submitted, 'Awaiting approval'],
            [ApprovalStatus.Draft, 'Awaiting approval'],
            [ApprovalStatus.Approved, 'Approved'],
            [ApprovalStatus.Rejected, 'Rejected'],
            [ApprovalStatus.Dismissed, 'Dismissed']
        ])('should render the status pill for status %s as "%s"', (
            approvalStatus: ApprovalStatus,
            expectedText: string
        ) => {
            // when
            renderWithAuth(
                <ReviewPanel entityType="ContentItem" approvalStatus={approvalStatus} />);

            // then
            expect(statusPillText()).toBe(expectedText);
        });
    });

    describe('bypass', () => {
        it('should offer the bypass checkbox to a decision-tier viewer the verdict allows', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            // then
            expect(screen.getByRole('checkbox')).toBeInTheDocument();
        });

        it('should hide the bypass checkbox when the verdict does not allow this caller to bypass', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: false })} />);

            // then
            expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
        });

        it('should hide the bypass checkbox when nothing blocks approval', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith({ isBypassAllowedForCurrentUser: true })} />);

            // then
            expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
        });

        it('should hide the bypass checkbox from the reviewer tier even when the verdict allows it', () => {
            // given — HR-3: a reviewer never decides, so they are never offered the waiver either
            signInAs(authState, ['Reviewer']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            // then
            expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
        });

        it('should reveal the reason box on tick and clear the reason on untick', async () => {
            // given
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            const reasonBoxQuery = () =>
                screen.queryByRole('textbox', {
                    name: 'Reason for bypassing the approval requirements'
                });

            expect(reasonBoxQuery()).not.toBeInTheDocument();

            // when — tick, type, untick, tick again
            await userEvent.click(screen.getByRole('checkbox'));
            await userEvent.type(reasonBoxQuery()!, 'Launch day exception');
            await userEvent.click(screen.getByRole('checkbox'));

            expect(reasonBoxQuery()).not.toBeInTheDocument();

            await userEvent.click(screen.getByRole('checkbox'));

            // then — the reason did not survive the untick
            expect(reasonBoxQuery()).toHaveValue('');
        });

        it('should reset the bypass tick when the verdict changes underneath it', async () => {
            // given
            signInAs(authState, ['Publisher']);

            const panelWith = (verdict: ApprovalVerdictItem): ReactElement => (
                <MemoryRouter>
                    <AuthProvider>
                        <ReviewPanel
                            entityType="ContentItem"
                            approvalStatus={ApprovalStatus.Submitted}
                            approvalVerdict={verdict} />
                    </AuthProvider>
                </MemoryRouter>
            );

            const { rerender } = renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            await userEvent.click(screen.getByRole('checkbox'));
            expect(screen.getByRole('checkbox')).toBeChecked();

            // when — a new comment arrives: the block reasons change
            rerender(panelWith(blockedVerdict({
                isBypassAllowedForCurrentUser: true,
                blockReasons: [
                    { code: 1, message: 'At least 3 approving review(s) is required by reviewers.' },
                    { code: 2, message: 'All review comments must be resolved.' }
                ]
            })));

            // then — consent given against the old reasons does not carry over
            expect(screen.getByRole('checkbox')).not.toBeChecked();
        });
    });

    describe('decision', () => {
        it.each([
            ['Publisher'],
            ['Admin'],
            ['Administrators'],
            ['ContentItem-Publisher'],
            ['ContentItem-Blog-Publisher']
        ])('should show the set-approval-status dropdown to %s', (role: string) => {
            // given
            signInAs(authState, [role]);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    contentType="Blog"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith()} />);

            // then
            expect(screen.getByRole('button', { name: 'Set approval status' })).toBeInTheDocument();
        });

        it.each([
            ['Reviewer'],
            ['ContentItem-Reviewer'],
            ['Users']
        ])('should hide the set-approval-status dropdown from %s', (role: string) => {
            // given — HR-3: the reviewer tier may never set an ApprovalStatus
            signInAs(authState, [role]);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith()} />);

            // then
            expect(screen.queryByRole('button', { name: 'Set approval status' }))
                .not.toBeInTheDocument();
        });

        it('should hide the set-approval-status dropdown once the approval is decided', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Approved}
                    approvalVerdict={verdictWith({ approvalStatus: ApprovalStatus.Approved })} />);

            // then
            expect(screen.queryByRole('button', { name: 'Set approval status' }))
                .not.toBeInTheDocument();
        });

        it('should disable approve while blocked without a bypass, and keep reject enabled', async () => {
            // given — §12.5.3 rule 13: a direct reject is not gated by the conditions
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));

            // then
            expect(screen.getByRole('button', { name: /Approve this item/ })).toBeDisabled();
            expect(screen.getByRole('button', { name: /Reject this item/ })).toBeEnabled();
        });

        it('should enable approve when the verdict says this caller may approve', async () => {
            // given
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith()} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));

            // then
            expect(screen.getByRole('button', { name: /Approve this item/ })).toBeEnabled();
        });

        it('should enable approve once the bypass is ticked', async () => {
            // given
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            // when
            await userEvent.click(screen.getByRole('checkbox'));
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));

            // then
            expect(screen.getByRole('button', { name: /Approve this item/ })).toBeEnabled();
        });

        it('should submit a plain rejection with no bypass recorded', async () => {
            // given
            signInAs(authState, ['Publisher']);
            const onApprovalStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })}
                    onApprovalStatusChanged={onApprovalStatusChanged} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Reject this item/ }));
            await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

            // then
            expect(onApprovalStatusChanged).toHaveBeenCalledTimes(1);
            expect(onApprovalStatusChanged).toHaveBeenCalledWith(ApprovalDecision.Reject, false, '');
        });

        it('should hold submit on a bypass approve until a reason is given, then send it', async () => {
            // given
            signInAs(authState, ['Publisher']);
            const onApprovalStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })}
                    onApprovalStatusChanged={onApprovalStatusChanged} />);

            // when
            await userEvent.click(screen.getByRole('checkbox'));
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Approve this item/ }));

            // then — no reason yet, so the click must not round-trip into a server 400
            expect(screen.getByRole('button', { name: 'Submit' })).toBeDisabled();

            // when
            await userEvent.type(
                screen.getByRole('textbox', {
                    name: 'Reason for bypassing the approval requirements'
                }),
                'Launch day exception');

            await userEvent.click(screen.getByRole('button', { name: 'Submit' }));

            // then
            expect(onApprovalStatusChanged).toHaveBeenCalledTimes(1);

            expect(onApprovalStatusChanged).toHaveBeenCalledWith(
                ApprovalDecision.Approve, true, 'Launch day exception');
        });
    });

    describe('read-only', () => {
        it('should show reviews and the status pill, and no controls, to a roleless reader', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved, susanApproved]} />);

            // then
            expect(screen.getByText('John')).toBeInTheDocument();
            expect(screen.getByText('Susan')).toBeInTheDocument();
            expect(statusPillText()).toBe('Awaiting approval');
            expect(screen.queryByRole('button', { name: 'Vote...' })).not.toBeInTheDocument();
            expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Set approval status' }))
                .not.toBeInTheDocument();
        });
    });
});
