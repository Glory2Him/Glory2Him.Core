import { ReactElement } from 'react';
import { MemoryRouter } from 'react-router-dom';
import { fireEvent, screen } from '@testing-library/react';
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
            expect(screen.getByText('Requested')).toBeInTheDocument();
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
            expect(screen.getAllByText('Requested')).toHaveLength(1);
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
            expect(screen.getByRole('button', { name: /Mary/ })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: /Paul/ })).toBeInTheDocument();
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
            expect(screen.getByRole('button', { name: /Mary/ })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Paul/ })).not.toBeInTheDocument();

            // when — by username
            await userEvent.clear(filterBox);
            await userEvent.type(filterBox, 'paul.p');

            // then
            expect(screen.getByRole('button', { name: /Paul/ })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Mary/ })).not.toBeInTheDocument();
        });

        /// The picker deliberately STAYS OPEN after a pick: assigning several reviewers is one
        /// task, and closing after each would make the common case four trips through the cog.
        it('should raise onReviewRequested and keep the picker open', async () => {
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
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));

            // then
            expect(onReviewRequested).toHaveBeenCalledTimes(1);
            expect(onReviewRequested).toHaveBeenCalledWith(mary);
            expect(screen.getByRole('button', { name: /Paul/ })).toBeInTheDocument();
        });

        /// Unassigning happens ONLY through the picker's Requested section (§7.9 rule 5). There
        /// is no inline control on the row, so the one route is the one place the rule lives.
        it('should withdraw a request when its Requested row is picked', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequestWithdrawn = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]}
                    reviewerCandidateCollection={[mary, paul]}
                    onReviewRequestWithdrawn={onReviewRequestWithdrawn} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));

            // then
            expect(onReviewRequestWithdrawn).toHaveBeenCalledTimes(1);
            expect(onReviewRequestWithdrawn).toHaveBeenCalledWith(mary);
        });

        /// A requested person is NOT filtered out of the picker - they move into the Requested
        /// section. Filtering them away would leave a searcher wondering why the person is
        /// missing, which is the question the ticks exist to answer.
        it('should list a requested person once, in the Requested section', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]}
                    reviewerCandidateCollection={[mary, paul]} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            const maryRows = screen.getAllByRole('button', { name: /Mary/ });
            expect(maryRows).toHaveLength(1);

            // ticked, which is how the picker says "already assigned"
            expect(maryRows[0]).toHaveAttribute('aria-pressed', 'true');
            expect(maryRows[0]).toBeEnabled();
            expect(screen.getByText('Everyone else')).toBeInTheDocument();
        });

        /// Somebody who has already voted stays listed, ticked and INERT. A cast verdict is
        /// theirs (§8.6.1 owner-only), so there is no unassign to offer - but hiding them would
        /// make a search for them come back empty.
        it('should list a voter as ticked and refuse to act on a click', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();
            const onReviewRequestWithdrawn = vi.fn();

            const voted: ApprovalReviewItem = {
                reviewerUserId: mary.userId,
                reviewerDisplayName: mary.displayName,
                vote: ApprovalStatus.Approved
            };

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[voted]}
                    reviewerCandidateCollection={[mary, paul]}
                    onReviewRequested={onReviewRequested}
                    onReviewRequestWithdrawn={onReviewRequestWithdrawn} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            const maryRow = screen.getByRole('button', { name: /Mary/ });
            await userEvent.click(maryRow);

            // then
            expect(maryRow).toBeDisabled();
            expect(maryRow).toHaveAttribute('aria-pressed', 'true');
            expect(onReviewRequested).not.toHaveBeenCalled();
            expect(onReviewRequestWithdrawn).not.toHaveBeenCalled();
        });

        it('should not offer the cog to a roleless reader', () => {
            // given
            signInAs(authState, ['Users']);

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]} />);

            // then — the row still renders; only the way to change it is withheld
            expect(screen.getByText('Mary')).toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Request a review' }))
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

        /// approvalStatus is the canonical prop that freezes this panel. A consumer refreshing
        /// after a decision can hand over the new status with a verdict fetched a moment earlier,
        /// and painting block reasons over a settled round tells the reader it is still waiting.
        it('should not render block reasons once the round is no longer submitted', () => {
            // given
            signInAs(authState, ['Publisher']);

            // when - a terminal status alongside a verdict that still says blocked
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Approved}
                    approvalVerdict={blockedVerdict({
                        blockReasons: [
                            { code: 1, message: 'At least 3 approving review(s) is required.' }
                        ]
                    })} />);

            // then
            expect(screen.queryByText('At least 3 approving review(s) is required.'))
                .not.toBeInTheDocument();

            expect(screen.queryByText('Approval is blocked')).not.toBeInTheDocument();
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

        /// A selection made under the bypass must not outlive the tick that made it possible.
        /// Without this the Submit button stays on screen offering an Approve the server will
        /// refuse, which is the one outcome guaranteed to look like a bug.
        it('should clear a bypass approve selection when the bypass is unticked', async () => {
            // given
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({ isBypassAllowedForCurrentUser: true })} />);

            await userEvent.click(screen.getByRole('checkbox'));
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));
            await userEvent.click(screen.getByRole('button', { name: /Approve this item/ }));

            // when
            await userEvent.click(screen.getByRole('checkbox'));

            // then
            expect(screen.getByRole('button', { name: 'Set approval status' }))
                .toBeInTheDocument();

            expect(screen.queryByRole('button', { name: 'Submit' })).not.toBeInTheDocument();
        });

        /// canApprove is the SERVER's per-caller answer and the panel must read it verbatim: it
        /// already folds the approval conditions, HR-2 self-approval, and the reviewer whose own
        /// review carried the round over the line - none of which a browser can compute.
        ///
        /// Every other fixture ties canApprove to isBlocked, so a panel that ignored the field
        /// and re-derived it from the block set would leave the suite green. This one holds
        /// isBlocked false and canApprove false together, which no reason code can produce, so it
        /// fails the moment the field stops being read.
        it('should refuse approve on the verdict word even when nothing is blocking', async () => {
            // given
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith({
                        isBlocked: false,
                        blockReasons: [],
                        canApprove: false
                    })} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Set approval status' }));

            // then - no bypass is on offer either, so the verdict is the only thing that could
            // have disabled this
            expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();

            expect(screen.getByRole('button', { name: /Approve this item/ })).toBeDisabled();

            expect(screen.getByRole('button', { name: /Reject this item/ })).toBeEnabled();
        });

        /// A reason code can stay put while what it SAYS changes - "at least 3 approving
        /// review(s)" becomes "at least 2" as votes land. Consent was given against the sentence
        /// the publisher read, not against its code.
        it('should reset the bypass tick when a block reason is reworded', async () => {
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
                    approvalVerdict={blockedVerdict({
                        isBypassAllowedForCurrentUser: true,
                        blockReasons: [
                            { code: 1, message: 'At least 3 approving review(s) is required.' }
                        ]
                    })} />);

            await userEvent.click(screen.getByRole('checkbox'));
            expect(screen.getByRole('checkbox')).toBeChecked();

            // when - same code, fewer approvals still needed
            rerender(panelWith(blockedVerdict({
                isBypassAllowedForCurrentUser: true,
                blockReasons: [
                    { code: 1, message: 'At least 1 approving review(s) is required.' }
                ]
            })));

            // then
            expect(screen.getByRole('checkbox')).not.toBeChecked();
        });

        /// The case the reason codes cannot catch. Two different approvals blocked for the SAME
        /// reasons produce an identical signature in every other field, so without approvalId a
        /// consumer swapping items without remounting keeps the previous item's tick, its typed
        /// justification and its pending decision over a repainted panel. Submitting then writes
        /// one item's justification onto another's permanent record, and the server cannot catch
        /// it — a bypass reason is free text it only checks for being non-blank.
        it('should reset the bypass tick when the panel moves to another approval', async () => {
            // given
            signInAs(authState, ['Publisher']);
            const onApprovalStatusChanged = vi.fn();

            const reasonBoxQuery = () =>
                screen.queryByRole('textbox', {
                    name: 'Reason for bypassing the approval requirements'
                });

            const panelWith = (verdict: ApprovalVerdictItem): ReactElement => (
                <MemoryRouter>
                    <AuthProvider>
                        <ReviewPanel
                            entityType="ContentItem"
                            approvalStatus={ApprovalStatus.Submitted}
                            approvalVerdict={verdict}
                            onApprovalStatusChanged={onApprovalStatusChanged} />
                    </AuthProvider>
                </MemoryRouter>
            );

            const { rerender } = renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={blockedVerdict({
                        approvalId: 'approval-a',
                        isBypassAllowedForCurrentUser: true
                    })}
                    onApprovalStatusChanged={onApprovalStatusChanged} />);

            await userEvent.click(screen.getByRole('checkbox'));
            await userEvent.type(reasonBoxQuery()!, 'Trustees approved this out of band');
            expect(screen.getByRole('checkbox')).toBeChecked();

            // when — the consumer points the panel at a DIFFERENT approval that happens to be
            // blocked for exactly the same reasons
            rerender(panelWith(blockedVerdict({
                approvalId: 'approval-b',
                isBypassAllowedForCurrentUser: true
            })));

            // then — nothing of the first item's consent survives onto the second
            expect(screen.getByRole('checkbox')).not.toBeChecked();
            expect(reasonBoxQuery()).not.toBeInTheDocument();
            expect(onApprovalStatusChanged).not.toHaveBeenCalled();
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

    describe('out-of-scope rows', () => {
        /// This panel shows the round AS IT STANDS: approved, rejected, and pending. A dismissed
        /// verdict describes content that has since changed (§9.5) - it is kept as evidence, not
        /// as a standing opinion - so it is excluded outright rather than badged. Rendering it
        /// would invite a publisher to count an opinion nobody currently holds.
        it('should exclude a dismissed review entirely', () => {
            // given
            signInAs(authState, ['Publisher']);

            const dismissed: ApprovalReviewItem = {
                reviewerUserId: 'user-jane',
                reviewerDisplayName: 'Jane',
                vote: ApprovalStatus.Dismissed
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[dismissed, johnApproved]}
                    voteRoles="" />);

            // then
            expect(screen.queryByText('Jane')).not.toBeInTheDocument();
            expect(screen.queryByText('Dismissed')).not.toBeInTheDocument();
            expect(screen.getByText('John')).toBeInTheDocument();
            expect(rowNames()).toEqual(['John']);
        });

        /// A withdrawn review keeps its row, and a withdrawn opinion is no opinion.
        it('should exclude a soft-deleted review entirely', () => {
            // given
            signInAs(authState, ['Publisher']);

            const withdrawn: ApprovalReviewItem = {
                reviewerUserId: 'user-jane',
                reviewerDisplayName: 'Jane',
                vote: ApprovalStatus.Approved,
                isDeleted: true
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[withdrawn, johnApproved]}
                    voteRoles="" />);

            // then
            expect(screen.queryByText('Jane')).not.toBeInTheDocument();
            expect(rowNames()).toEqual(['John']);
        });

        /// §7.7 rule 7: the reviewer files a NEW review once theirs has been dismissed, and rule
        /// 2a forbids amending the dismissed row. Excluding it gives them the placeholder rather
        /// than a dropdown labelled with a verdict they can no longer amend.
        it('should offer the vote placeholder to a viewer whose own review was dismissed', () => {
            // given
            signInAs(authState, ['Reviewer']);

            const viewerDismissed: ApprovalReviewItem = {
                reviewerUserId: ViewerId,
                reviewerDisplayName: 'Tester',
                vote: ApprovalStatus.Dismissed
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[viewerDismissed]} />);

            // then
            expect(screen.getByRole('button', { name: 'Vote...' })).toBeInTheDocument();
            expect(rowNames()).toEqual(['Tester']);
        });

        /// Dismissal is per-review, so one being dismissed must not disturb another.
        it('should keep the standing votes when one review is dismissed', () => {
            // given
            signInAs(authState, ['Reviewer']);

            const janeDismissed: ApprovalReviewItem = {
                reviewerUserId: 'user-jane',
                reviewerDisplayName: 'Jane',
                vote: ApprovalStatus.Dismissed
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[janeDismissed, viewerRejected, johnApproved]} />);

            // then
            expect(rowNames()).toEqual(['Tester', 'John']);
            expect(screen.getByRole('button', { name: 'Rejected' })).toBeInTheDocument();
            expect(screen.getByText('Approved')).toBeInTheDocument();
        });

        /// A vote SUPERSEDES an outstanding request. Once somebody answers, the invitation is
        /// spent (§7.9 rule 6 retires it server-side) and showing both would list one person
        /// twice - once as an answer and once as a question.
        it('should drop a request once its target has voted', () => {
            // given
            signInAs(authState, ['Reviewer']);

            const johnRequested: ReviewerCandidateItem = {
                userId: johnApproved.reviewerUserId,
                displayName: 'John'
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]}
                    requestedReviewerCollection={[johnRequested, mary]} />);

            // then
            expect(rowNames()).toEqual(['Tester', 'John', 'Mary']);
            expect(screen.getByText('Approved')).toBeInTheDocument();
            expect(screen.getAllByText('Requested')).toHaveLength(1);
        });

        /// Votes and outstanding requests share one alphabetical list, viewer first - a reader
        /// asks "where does this round stand?" per person, so a name keeps its place whether or
        /// not the answer has arrived.
        it('should sort requests and votes into one alphabetical list', () => {
            // given
            signInAs(authState, ['Reviewer']);

            const bill: ReviewerCandidateItem = {
                userId: 'user-bill',
                displayName: 'BillWoodNHS'
            };

            // when
            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[viewerRejected, johnApproved, susanApproved]}
                    requestedReviewerCollection={[bill]} />);

            // then
            expect(rowNames()).toEqual(['Tester', 'BillWoodNHS', 'John', 'Susan']);
        });
    });

    describe('picker sections and the request cap', () => {
        /// The panel does no ranking of its own - who is worth asking depends on history it
        /// cannot see - so suggestions and their reasons come from the consumer.
        it('should render supplied suggestions with their reason', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            const suggested: ReviewerCandidateItem = {
                userId: 'user-christo',
                displayName: 'Christo du Toit',
                userName: 'cjdutoit',
                suggestionReason: 'Recently reviewed this type'
            };

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    suggestedReviewerCollection={[suggested]}
                    reviewerCandidateCollection={[mary]} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(screen.getByText('Suggestions')).toBeInTheDocument();
            expect(screen.getByText('Recently reviewed this type')).toBeInTheDocument();
        });

        /// A consumer ranks suggestions from history the panel cannot see, so it may well
        /// suggest somebody who has voted since. The tick has to mean the same thing in every
        /// section: already answered, nothing to do here.
        it('should render a suggested candidate who has already voted as inert', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();

            const suggested: ReviewerCandidateItem = {
                userId: 'user-john',
                displayName: 'John',
                userName: 'john.b'
            };

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]}
                    suggestedReviewerCollection={[suggested]}
                    onReviewRequested={onReviewRequested} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            const row = screen.getByRole('button', { name: /john\.b/ });

            // then
            expect(row).toBeDisabled();
            expect(row).toHaveAccessibleName(/has already reviewed/);

            await userEvent.click(row);
            expect(onReviewRequested).not.toHaveBeenCalled();
        });

        /// "Everyone else" is meant literally. A consumer ranks its suggestions out of the same
        /// candidates read, so the same person arrives in both collections - and rendering them
        /// twice in one open picker makes the second row look like a different person.
        it('should not repeat a suggested person under everyone else', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    suggestedReviewerCollection={[mary]}
                    reviewerCandidateCollection={[mary, paul]} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(screen.getAllByRole('button', { name: /mary\.m/ })).toHaveLength(1);
            expect(screen.getAllByRole('button', { name: /paul\.p/ })).toHaveLength(1);
        });

        /// A request whose target has since voted is spent - rule 6 retires it server-side - but
        /// when that retirement fails or the panel is a few seconds stale it lingers in the
        /// collection. Counting it against the cap would refuse new invitations on behalf of
        /// somebody who has already answered, with nothing on screen saying why.
        it('should not count an answered request against the cap', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();

            const votedInvitee: ReviewerCandidateItem = {
                userId: 'user-john',
                displayName: 'John',
                userName: 'john.b'
            };

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]}
                    requestedReviewerCollection={[votedInvitee, mary]}
                    reviewerCandidateCollection={[paul]}
                    maxReviewerRequests={2}
                    onReviewRequested={onReviewRequested} />);

            // when - two rows are in the collection and the cap is two, but only Mary is
            // actually being waited on
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            await userEvent.click(screen.getByRole('button', { name: /paul\.p/ }));

            // then
            expect(onReviewRequested).toHaveBeenCalledTimes(1);
        });

        /// The case the first inert test does not reach. A person can be both invited AND
        /// answered - rule 6 normally retires the invitation, but a failed retirement or a stale
        /// panel leaves it standing - and the server refuses to withdraw an answered invitation
        /// (rule 5). A clickable row there is the one click in the panel that round-trips into an
        /// error.
        it('should render a requested candidate who has already voted as inert', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequestWithdrawn = vi.fn();

            const invitedAndAnswered: ReviewerCandidateItem = {
                userId: 'user-john',
                displayName: 'John',
                userName: 'john.b'
            };

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalReviewCollection={[johnApproved]}
                    requestedReviewerCollection={[invitedAndAnswered]}
                    onReviewRequestWithdrawn={onReviewRequestWithdrawn} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));
            const row = screen.getByRole('button', { name: /john\.b/ });

            // then
            expect(row).toBeDisabled();

            await userEvent.click(row);
            expect(onReviewRequestWithdrawn).not.toHaveBeenCalled();
        });

        /// Suggestions win the tie. A consumer ranking suggestions out of its own request list can
        /// hand the same person to both collections, and two rows read as two people.
        it('should not repeat a suggested person under requested', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    suggestedReviewerCollection={[mary]}
                    requestedReviewerCollection={[mary]} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(screen.getAllByRole('button', { name: /mary\.m/ })).toHaveLength(1);
        });

        it('should name the cap in the picker heading', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary]}
                    maxReviewerRequests={3} />);

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // then
            expect(screen.getByText('Request up to 3 reviewers')).toBeInTheDocument();
        });

        /// At the cap, NEW invitations stop but withdrawals must not - otherwise reaching the
        /// limit would trap the round with no way to free a slot.
        it('should stop new requests at the cap while leaving withdrawal available', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();
            const onReviewRequestWithdrawn = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    requestedReviewerCollection={[mary]}
                    reviewerCandidateCollection={[mary, paul]}
                    maxReviewerRequests={1}
                    onReviewRequested={onReviewRequested}
                    onReviewRequestWithdrawn={onReviewRequestWithdrawn} />);

            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // when: Paul is un-requested and the cap is reached
            const paulRow = screen.getByRole('button', { name: /Paul/ });
            await userEvent.click(paulRow);

            // then
            expect(paulRow).toBeDisabled();
            expect(onReviewRequested).not.toHaveBeenCalled();

            // and the standing request can still be withdrawn to free the slot
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));
            expect(onReviewRequestWithdrawn).toHaveBeenCalledWith(mary);
        });
    });

    // All three menus are the panel's own rather than Bootstrap's, so none of the dismissal that
    // `data-bs-toggle` brings for free applies. Opened by keyboard, the only way out used to be
    // finding the trigger and clicking it a second time.
    //
    // Driven through the three of them rather than one, because the whole point of the shared
    // hook is that they cannot drift apart.
    describe('menu dismissal and labelling', () => {
        const openVoteMenu = async (): Promise<HTMLElement> => {
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted} />);

            const trigger = screen.getByRole('button', { name: 'Vote...' });
            await userEvent.click(trigger);

            return trigger;
        };

        const openPicker = async (): Promise<HTMLElement> => {
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]} />);

            const trigger = screen.getByRole('button', { name: 'Request a review' });
            await userEvent.click(trigger);

            return trigger;
        };

        const openDecisionMenu = async (): Promise<HTMLElement> => {
            signInAs(authState, ['Publisher']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    approvalVerdict={verdictWith()} />);

            const trigger = screen.getByRole('button', { name: 'Set approval status' });
            await userEvent.click(trigger);

            return trigger;
        };

        const menus: Array<[string, () => Promise<HTMLElement>, string]> = [
            ['the vote menu', openVoteMenu, 'Approved'],
            ['the reviewer picker', openPicker, 'Mary'],
            ['the decision menu', openDecisionMenu, 'Approve']
        ];

        it.each(menus)(
            'should dismiss %s on Escape and hand focus back to its trigger',
            async (_name, open, itemName) => {
                // given
                const trigger = await open();
                expect(screen.getByRole('button', { name: new RegExp(itemName) }))
                    .toBeInTheDocument();

                // when
                await userEvent.keyboard('{Escape}');

                // then
                expect(screen.queryByRole('button', { name: new RegExp(itemName) }))
                    .not.toBeInTheDocument();

                // the whole point: a keyboard user is put back where they were, not stranded
                expect(trigger).toHaveFocus();
                expect(trigger).toHaveAttribute('aria-expanded', 'false');
            });

        it.each(menus)(
            'should dismiss %s on a click outside it',
            async (_name, open, itemName) => {
                // given
                const trigger = await open();

                // when: somewhere that is neither the menu nor its trigger.
                //
                // fireEvent.mouseDown rather than userEvent.click, and that is the whole
                // difference between this test working and not. userEvent.click on a
                // non-focusable <h4> blurs the active element ITSELF, after the hook's effect has
                // run — so document.activeElement is <body> by the time the assertion below runs
                // no matter what the hook did. Written with userEvent this test passes even when
                // the outside-click path is changed to returnFocus: true, i.e. it cannot fail.
                // mousedown moves no focus, so what is asserted is the hook's behaviour alone.
                fireEvent.mouseDown(screen.getByRole('heading', { name: 'Approval Reviews' }));

                // then
                expect(screen.queryByRole('button', { name: new RegExp(itemName) }))
                    .not.toBeInTheDocument();

                // and focus is NOT dragged back — the user chose to go elsewhere, and yanking
                // it to the trigger would undo their own click
                expect(trigger).not.toHaveFocus();
            });

        // The one dismissal route that existed BEFORE this PR, and the invariant the hook's
        // containerRef exists to protect: the trigger lives inside the container, so its mousedown
        // is never "outside" and cannot race the toggle into reopening what it just closed. Point
        // containerRef at the menu instead and this is the test that goes red.
        it.each(menus)(
            'should dismiss %s when its own trigger is clicked again',
            async (_name, open, itemName) => {
                // given
                const trigger = await open();

                // when
                await userEvent.click(trigger);

                // then
                expect(screen.queryByRole('button', { name: new RegExp(itemName) }))
                    .not.toBeInTheDocument();

                expect(trigger).toHaveAttribute('aria-expanded', 'false');
            });

        it.each(menus)(
            'should label %s by the trigger that opened it',
            async (_name, open) => {
                // given
                const trigger = await open();

                // when
                const menu = document.querySelector('.dropdown-menu.show');

                // then: without this a screen-reader user landing in one of three menus has no
                // way to tell which
                expect(menu).not.toBeNull();
                expect(trigger.id).not.toBe('');
                expect(menu).toHaveAttribute('aria-labelledby', trigger.id);
                expect(trigger).toHaveAttribute('aria-controls', menu?.id);

                // asserted OPEN as well as closed. Pinning only the closed state let a trigger
                // hard-coded to aria-expanded={false} pass the whole suite.
                expect(trigger).toHaveAttribute('aria-expanded', 'true');

                // NOT a menu, and it must not say it is. aria-haspopup="true" is defined as
                // synonymous with "menu", which would promise roles and arrow keys none of these
                // three have — see useDismissableMenu. Absence here is the assertion.
                expect(trigger).not.toHaveAttribute('aria-haspopup');
            });

        it.each(menus)(
            'should drop aria-controls from the %s trigger once it is closed',
            async (_name, open) => {
                // given: a trigger pointing aria-controls at an element that is no longer in the
                // document is a dangling reference, so the attribute has to go with the menu
                const trigger = await open();
                expect(trigger).toHaveAttribute('aria-controls');

                // when
                await userEvent.keyboard('{Escape}');

                // then
                expect(trigger).not.toHaveAttribute('aria-controls');
            });

        it('should move focus to the first control in the reviewer picker when it opens', async () => {
            // given, when: the picker's first control is the filter box, which is also where
            // somebody opening it wants to be
            await openPicker();

            // then: WHICH element, not merely "something inside". Asserting containment let a
            // hook focusing the LAST control pass — landing the picker on its final candidate
            // row instead of the filter box somebody opened it to type in.
            expect(screen.getByRole('textbox', { name: /Filter by name/ })).toHaveFocus();
        });

        it.each([
            ['the vote menu', openVoteMenu],
            ['the decision menu', openDecisionMenu]
        ] as Array<[string, () => Promise<HTMLElement>]>)(
            'should move focus to the menu container, not the first item, when %s opens',
            async (_name, open) => {
                // given, when: focusing the first item would make a stray second Enter on the
                // trigger fall through to that item's action — casting a vote or picking a
                // decision with no visible menu the user noticed opening. See #370.
                await open();
                const item = open === openVoteMenu
                    ? screen.getByRole('button', { name: /Approved/ })
                    : screen.getByRole('button', { name: /Approve this item/ });
                const menu = item.closest('.dropdown-menu');
                expect(menu).not.toBeNull();

                // then
                expect(menu as HTMLElement).toHaveFocus();
            });

        it('should not cast a vote when Enter is pressed right after the vote menu opens', async () => {
            // given: a second Enter on the trigger is the habitual response to a menu that opened
            // below the fold or was otherwise not noticed. Before #370, that Enter landed on the
            // first item and cast a vote nobody meant to cast.
            signInAs(authState, ['Reviewer']);
            const onReviewStatusChanged = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    onReviewStatusChanged={onReviewStatusChanged} />);

            await userEvent.click(screen.getByRole('button', { name: 'Vote...' }));

            // when
            await userEvent.keyboard('{Enter}');

            // then
            expect(onReviewStatusChanged).not.toHaveBeenCalled();
        });

        it('should hand focus back to the trigger after a vote is cast', async () => {
            // given
            const trigger = await openVoteMenu();

            // when
            await userEvent.click(screen.getByRole('button', { name: /Approved/ }));

            // then: choosing is not leaving, so focus stays on the control
            expect(trigger).toHaveFocus();
        });

        it('should hand focus back to the trigger after a decision is chosen', async () => {
            // given
            const trigger = await openDecisionMenu();

            // when
            await userEvent.click(screen.getByRole('button', { name: /^Approve this item/ }));

            // then
            expect(trigger).toHaveFocus();
        });

        // The picker deliberately stays open after each pick — assigning several reviewers is one
        // task — so its dismissal must not have quietly turned that into one round trip per name.
        it('should keep the picker open after a candidate is requested', async () => {
            // given
            signInAs(authState, ['Reviewer']);
            const onReviewRequested = vi.fn();

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]}
                    onReviewRequested={onReviewRequested} />);

            await userEvent.click(screen.getByRole('button', { name: 'Request a review' }));

            // when
            await userEvent.click(screen.getByRole('button', { name: /Mary/ }));

            // then
            expect(onReviewRequested).toHaveBeenCalledWith(mary);
            expect(screen.getByRole('button', { name: /Paul/ })).toBeInTheDocument();
        });

        // Keyboard activation of a <button> fires keydown/click and no mousedown, so the
        // cross-menu dismissal that mousedown gives the outside-click test above never runs for
        // a keyboard user. Without the focusout close, tabbing out of a menu leaves it painted
        // over the page with aria-expanded still "true".
        it.each(menus)(
            'should close %s and drop aria-expanded when focus tabs out of it',
            async (_name, open, itemName) => {
                // given
                const trigger = await open();
                expect(screen.getByRole('button', { name: new RegExp(itemName) }))
                    .toBeInTheDocument();

                // when: focus leaves the container for something outside it entirely. Fired
                // directly rather than via .focus()/tab, because jsdom does not reliably
                // populate relatedTarget on the focusout it raises for a plain focus() call —
                // this is what a real browser sends when Tab carries focus past the menu.
                const outside = document.createElement('input');
                document.body.appendChild(outside);
                const container = trigger.closest('.dropdown') as HTMLElement;
                fireEvent.focusOut(container, { relatedTarget: outside });

                // then
                expect(screen.queryByRole('button', { name: new RegExp(itemName) }))
                    .not.toBeInTheDocument();
                expect(trigger).toHaveAttribute('aria-expanded', 'false');

                // and focus is not dragged back — the user has already moved on
                expect(trigger).not.toHaveFocus();

                document.body.removeChild(outside);
            });

        it('should not leave a menu open for a later Escape to stale-close after focus moves on', async () => {
            // given: the picker's filter box is a plain textbox that gets no special treatment
            // from the hook — it stands in for "the user has since moved to unrelated UI"
            const trigger = await openPicker();
            const filterBox = screen.getByRole('textbox', { name: /Filter by name/ });
            expect(filterBox).toHaveFocus();

            // when: focus leaves the picker entirely for something outside it
            document.body.focus();
            const outsideInput = document.createElement('input');
            document.body.appendChild(outsideInput);
            outsideInput.focus();
            await userEvent.type(outsideInput, 'wor');

            // and: Escape now fires — meant for this field/browser autofill, not the picker,
            // which the fix requires to already be closed and unarmed
            await userEvent.keyboard('{Escape}');

            // then: the outside field keeps its text and its focus, and the picker did not
            // reopen or steal focus back to its trigger
            expect(outsideInput).toHaveValue('wor');
            expect(outsideInput).toHaveFocus();
            expect(trigger).not.toHaveFocus();

            document.body.removeChild(outsideInput);
        });

        // Cross-menu dismissal used to come only from the mousedown listener, which keyboard
        // activation never fires — so opening a second menu with Enter left the first one open,
        // and a single Escape then closed both, with the LAST effect to run (hook-declaration
        // order) winning the focus regardless of which menu the user was actually in.
        it('should not allow two menus open at once when both are opened via the keyboard', async () => {
            // given
            signInAs(authState, ['Reviewer']);

            renderWithAuth(
                <ReviewPanel
                    entityType="ContentItem"
                    approvalStatus={ApprovalStatus.Submitted}
                    reviewerCandidateCollection={[mary, paul]} />);

            const cog = screen.getByRole('button', { name: 'Request a review' });
            const voteTrigger = screen.getByRole('button', { name: 'Vote...' });

            // when: keyboard activation, not a click — no mousedown is fired by either
            cog.focus();
            await userEvent.keyboard('{Enter}');
            expect(document.querySelectorAll('.dropdown-menu.show')).toHaveLength(1);

            voteTrigger.focus();
            await userEvent.keyboard('{Enter}');

            // then: only the second menu is open
            expect(document.querySelectorAll('.dropdown-menu.show')).toHaveLength(1);
            expect(cog).toHaveAttribute('aria-expanded', 'false');
            expect(voteTrigger).toHaveAttribute('aria-expanded', 'true');

            // and a single Escape closes the one actually open, returning focus to IT
            await userEvent.keyboard('{Escape}');

            expect(document.querySelectorAll('.dropdown-menu.show')).toHaveLength(0);
            expect(voteTrigger).toHaveFocus();
        });
    });
});
