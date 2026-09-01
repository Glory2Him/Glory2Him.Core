import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { BibleReferenceAssociationPanel } from './bibleReferenceAssociationPanel';
import {
    ApprovalStatus,
    AssociationItem
} from '../../models/components/associations/associationItem';
import {
    createAuthState,
    renderWithAuth,
    signInAs,
    signOut
} from '../../tests/testAuth';

const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const reference = (value: string): AssociationItem => ({
    value,
    createdBy: 'someone-else',
    approvalStatus: ApprovalStatus.Approved
});

describe('BibleReferenceAssociationPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    it('should render the post-detail reference panel from defaults alone', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel
                associationCollection={[reference('Joshua 10:8, 12-13')]} />);

        // then
        expect(screen.getByRole('heading', { name: 'Bible references' })).toBeInTheDocument();
        expect(screen.getByText('Suggest a bible reference')).toBeInTheDocument();
        expect(screen.getByText('Know a matching verse? Suggest it below.')).toBeInTheDocument();
        expect(screen.getByPlaceholderText('e.g. Romans 3:23…')).toBeInTheDocument();
    });

    it('should address each chip as the deep-link route parses it, not as it reads', () => {
        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel associationCollection={[reference('Romans 3:23')]} />);

        // then: the label is the citation, the href is its USFM form
        const chipLink = screen.getByRole('link', { name: /Romans 3:23/ });
        expect(chipLink).toHaveAttribute('href', '/BibleReferences/ROM.3.23');
    });

    it('should keep the reference as typed, hash and all, unlike a tag', async () => {
        // given
        signInAs(authState);
        const onAdd = vi.fn();

        // when
        renderWithAuth(<BibleReferenceAssociationPanel onAdd={onAdd} />);

        await userEvent.type(
            screen.getByPlaceholderText('e.g. Romans 3:23…'), '  2 Kings 20:9-11  {Enter}');

        // then: trimmed, but nothing stripped from the front
        expect(onAdd).toHaveBeenCalledTimes(1);
        expect(onAdd).toHaveBeenCalledWith('2 Kings 20:9-11');
    });

    it('should offer the reference-specific login prompt to an anonymous reader', () => {
        // given
        signOut(authState);

        // when
        renderWithAuth(<BibleReferenceAssociationPanel />);

        // then
        expect(screen.getByRole('link', { name: /Login to suggest a bible reference/ }))
            .toBeInTheDocument();

        expect(screen.queryByPlaceholderText('e.g. Romans 3:23…')).not.toBeInTheDocument();
    });

    it('should carry the book icon once approved and the hourglass while it waits', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel
                associationCollection={[
                    reference('Romans 3:23'),
                    {
                        value: '2 Kings 20:9-11',
                        createdBy: 'user-1',
                        approvalStatus: ApprovalStatus.Submitted
                    }
                ]} />);

        // then
        const approvedChip = screen.getByRole('link', { name: /Romans 3:23/ })
            .closest('span.g2h-association-chip');

        const pendingChip = screen.getByRole('link', { name: /2 Kings 20:9-11/ })
            .closest('span.g2h-association-chip');

        expect(approvedChip?.querySelector('i.bi-book')).toBeInTheDocument();
        expect(pendingChip?.querySelector('i.bi-hourglass-split')).toBeInTheDocument();
        expect(pendingChip?.querySelector('i.bi-book')).toBeNull();
    });

    it('should let a caller override a default without giving up the rest', () => {
        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel
                title="Passages"
                associationCollection={[reference('Romans 3:23')]} />);

        // then: the title is the caller's, the USFM href is still the component's
        expect(screen.getByRole('heading', { name: 'Passages' })).toBeInTheDocument();

        expect(screen.getByRole('link', { name: /Romans 3:23/ }))
            .toHaveAttribute('href', '/BibleReferences/ROM.3.23');
    });

    it('should let a BibleReference-scoped moderator decide without holding the global role', () => {
        // given
        signInAs(authState, ['BibleReference-Publishers']);

        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel
                showModerationActions={true}
                associationCollection={[{
                    value: 'Romans 3:23',
                    createdBy: 'another-user',
                    approvalStatus: ApprovalStatus.Submitted
                }]} />);

        // then
        expect(screen.getByRole('button', { name: 'Approve Romans 3:23' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Reject Romans 3:23' })).toBeInTheDocument();
    });

    it('should refuse a moderator scoped to a different entity type', () => {
        // given
        signInAs(authState, ['Tag-Publishers']);

        // when
        renderWithAuth(
            <BibleReferenceAssociationPanel
                showModerationActions={true}
                associationCollection={[{
                    value: 'Romans 3:23',
                    createdBy: 'another-user',
                    approvalStatus: ApprovalStatus.Submitted
                }]} />);

        // then: the reference is not even visible to a moderator this scope doesn't cover
        expect(screen.queryByText(/Romans 3:23/)).not.toBeInTheDocument();
    });
});
