import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { TagAssociationPanel } from './tagAssociationPanel';
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

const tag = (value: string): AssociationItem => ({
    value,
    createdBy: 'someone-else',
    approvalStatus: ApprovalStatus.Approved
});

describe('TagAssociationPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    it('should render the post-detail tag panel from defaults alone', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <TagAssociationPanel associationCollection={[tag('creation'), tag('science')]} />);

        // then
        expect(screen.getByRole('heading', { name: 'Tags' })).toBeInTheDocument();
        expect(screen.getByText('Suggest a tag')).toBeInTheDocument();

        expect(screen.getByText(
            'Think a tag is missing? Suggest one and help others find this post.'))
            .toBeInTheDocument();

        expect(screen.getByPlaceholderText('Start typing a tag…')).toBeInTheDocument();
    });

    it('should hash-prefix each chip and link it to the search', () => {
        // when
        renderWithAuth(<TagAssociationPanel associationCollection={[tag('creation')]} />);

        // then
        const chipLink = screen.getByRole('link', { name: '#creation' });
        expect(chipLink).toHaveAttribute('href', '/Search?q=creation');
    });

    it('should strip a leading hash from a suggestion before raising it', async () => {
        // given
        signInAs(authState);
        const onAdd = vi.fn();

        // when
        renderWithAuth(<TagAssociationPanel onAdd={onAdd} />);

        await userEvent.type(
            screen.getByPlaceholderText('Start typing a tag…'), '#miracles{Enter}');

        // then
        expect(onAdd).toHaveBeenCalledTimes(1);
        expect(onAdd).toHaveBeenCalledWith('miracles');
    });

    it('should separate a tag list and strip the hash from each of them', async () => {
        // given
        signInAs(authState);
        const onAdd = vi.fn();

        // when
        renderWithAuth(<TagAssociationPanel onAdd={onAdd} />);

        await userEvent.type(
            screen.getByPlaceholderText('Start typing a tag…'), '#faith, #healing{Enter}');

        // then
        expect(onAdd).toHaveBeenCalledTimes(2);
        expect(onAdd).toHaveBeenNthCalledWith(1, 'faith');
        expect(onAdd).toHaveBeenNthCalledWith(2, 'healing');
    });

    it('should thread the remove and decision hooks through to the panel', async () => {
        // given
        signInAs(authState, ['Administrators']);
        const onRemove = vi.fn();
        const onApprove = vi.fn();
        const onReject = vi.fn();

        // when
        renderWithAuth(
            <TagAssociationPanel
                showModerationActions={true}
                onRemove={onRemove}
                onApprove={onApprove}
                onReject={onReject}
                associationCollection={[{
                    value: 'test',
                    createdBy: 'another-user',
                    approvalStatus: ApprovalStatus.Submitted
                }]} />);

        await userEvent.click(screen.getByRole('button', { name: 'Remove test' }));
        await userEvent.click(screen.getByRole('button', { name: 'Reject test' }));
        await userEvent.click(screen.getByRole('button', { name: 'Approve test' }));

        // then
        expect(onRemove).toHaveBeenCalledWith(expect.objectContaining({ value: 'test' }));
        expect(onReject).toHaveBeenCalledWith(expect.objectContaining({ value: 'test' }));
        expect(onApprove).toHaveBeenCalledWith(expect.objectContaining({ value: 'test' }));
    });

    it('should offer the tag-specific login prompt to an anonymous reader', () => {
        // given
        signOut(authState);

        // when
        renderWithAuth(<TagAssociationPanel />);

        // then
        expect(screen.getByRole('link', { name: /Login to suggest a tag/ })).toBeInTheDocument();
        expect(screen.queryByPlaceholderText('Start typing a tag…')).not.toBeInTheDocument();
    });

    it('should let a caller override a default without giving up the rest', () => {
        // when
        renderWithAuth(
            <TagAssociationPanel title="Topics" associationCollection={[tag('creation')]} />);

        // then: the title is the caller's, the hash prefix is still the component's
        expect(screen.getByRole('heading', { name: 'Topics' })).toBeInTheDocument();
        expect(screen.getByRole('link', { name: '#creation' })).toBeInTheDocument();
    });

    it('should show the owner their own pending tag with a way to withdraw it', () => {
        // given
        signInAs(authState);

        // when
        renderWithAuth(
            <TagAssociationPanel
                associationCollection={[{
                    value: 'test',
                    createdBy: 'user-1',
                    approvalStatus: ApprovalStatus.Submitted
                }]} />);

        // then
        const chip = screen.getByRole('link', { name: '#test' })
            .closest('span.g2h-association-chip');

        expect(chip).toHaveClass('g2h-association-chip-pending');
        expect(chip?.querySelector('i.bi-hourglass-split')).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Remove test' })).toBeInTheDocument();
    });

    it('should offer a moderator both decisions on a tag somebody else suggested', () => {
        // given
        signInAs(authState, ['Administrators']);

        // when
        renderWithAuth(
            <TagAssociationPanel
                showModerationActions={true}
                associationCollection={[{
                    value: 'test',
                    createdBy: 'another-user',
                    approvalStatus: ApprovalStatus.Submitted
                }]} />);

        // then
        expect(screen.getByRole('button', { name: 'Approve test' })).toBeInTheDocument();
        expect(screen.getByRole('button', { name: 'Reject test' })).toBeInTheDocument();
    });
});
