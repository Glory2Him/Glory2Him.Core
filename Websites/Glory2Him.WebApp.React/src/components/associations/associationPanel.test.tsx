import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AssociationPanel } from './associationPanel';
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

// signInAs mints userId 'user-1', which is what CreatedBy carries for that reader's own items.
const ViewerId = 'user-1';
const OtherId = 'another-user';

const itemWith = (
    value: string,
    approvalStatus: ApprovalStatus,
    createdBy: string
): AssociationItem => ({ value, createdBy, approvalStatus });

const approvedItem = (value: string, createdBy = OtherId): AssociationItem =>
    itemWith(value, ApprovalStatus.Approved, createdBy);

const submittedItem = (value: string, createdBy = ViewerId): AssociationItem =>
    itemWith(value, ApprovalStatus.Submitted, createdBy);

const chipOf = (value: string): Element | null =>
    screen.getByText(value).closest('span.g2h-association-chip');

describe('AssociationPanel', () => {
    beforeEach(() => {
        signOut(authState);
    });

    describe('chips', () => {
        it('should render the title and a chip per item', () => {
            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[approvedItem('faith'), approvedItem('hope')]} />);

            // then
            expect(screen.getByRole('heading', { name: 'Tags' })).toBeInTheDocument();
            expect(screen.getByText('faith')).toBeInTheDocument();
            expect(screen.getByText('hope')).toBeInTheDocument();
        });

        it('should prefix each chip and link it when a href builder is supplied', () => {
            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    chipPrefixText="#"
                    chipHrefFor={(item) => `/Search?q=${item.value}`}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.getByRole('link', { name: '#faith' }))
                .toHaveAttribute('href', '/Search?q=faith');
        });

        it('should render a button and raise chipOnClick when no href builder is supplied', async () => {
            // given
            const chipOnClick = vi.fn();
            const item = approvedItem('faith');

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    chipOnClick={chipOnClick}
                    associationCollection={[item]} />);

            await userEvent.click(screen.getByRole('button', { name: 'faith' }));

            // then
            expect(chipOnClick).toHaveBeenCalledTimes(1);
            expect(chipOnClick).toHaveBeenCalledWith(item);
        });

        it('should render the empty text when nothing is visible', () => {
            // when
            renderWithAuth(<AssociationPanel title="Tags" emptyText="No tags yet." />);

            // then
            expect(screen.getByText('No tags yet.')).toBeInTheDocument();
        });

        it('should render the loading text instead of the empty text while loading', () => {
            // when
            renderWithAuth(
                <AssociationPanel title="Tags" isLoading={true} emptyText="No tags yet." />);

            // then
            expect(screen.getByText('Loading…')).toBeInTheDocument();
            expect(screen.queryByText('No tags yet.')).not.toBeInTheDocument();
        });

        it('should keep the caller theme class on the chip so it follows light and dark mode', () => {
            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    chipCssClass="btn-primary-soft"
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(chipOf('faith')).toHaveClass('btn-primary-soft');
        });
    });

    describe('status icons', () => {
        it('should show the approved icon on an approved chip', () => {
            // when
            renderWithAuth(
                <AssociationPanel
                    title="References"
                    approvedIconCssClass="bi-book"
                    associationCollection={[approvedItem('Romans 3:23')]} />);

            // then
            expect(chipOf('Romans 3:23')?.querySelector('i.bi-book')).toBeInTheDocument();
            expect(chipOf('Romans 3:23')?.querySelector('i.bi-hourglass-split')).toBeNull();
        });

        it('should show the pending icon on a submitted chip in place of the approved one', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="References"
                    approvedIconCssClass="bi-book"
                    associationCollection={[submittedItem('Romans 3:23')]} />);

            // then
            expect(chipOf('Romans 3:23')?.querySelector('i.bi-hourglass-split')).toBeInTheDocument();
            expect(chipOf('Romans 3:23')?.querySelector('i.bi-book')).toBeNull();
        });

        it('should show the pending icon on a draft chip too', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[itemWith('hope', ApprovalStatus.Draft, ViewerId)]} />);

            // then
            expect(chipOf('hope')?.querySelector('i.bi-hourglass-split')).toBeInTheDocument();
            expect(chipOf('hope')).toHaveClass('g2h-association-chip-pending');
        });

        it('should fall back to the flat chip icon when no status icon is set', () => {
            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    chipIconCssClass="bi-tag"
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(chipOf('faith')?.querySelector('i.bi-tag')).toBeInTheDocument();
        });
    });

    describe('unapproved visibility', () => {
        it('should hide an unapproved item from a reader who cannot act on it', () => {
            // given
            signInAs(authState, ['Members']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.queryByText('hope')).not.toBeInTheDocument();
        });

        it('should hide an unapproved item from an anonymous reader', () => {
            // given
            signOut(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.queryByText('hope')).not.toBeInTheDocument();
        });

        it('should show an unapproved item to its owner', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel title="Tags" associationCollection={[submittedItem('hope')]} />);

            // then
            expect(screen.getByText('hope')).toBeInTheDocument();
        });

        it('should show a submitted item to a moderator so they can decide on it', () => {
            // given
            signInAs(authState, ['Reviewers']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    moderationRoles="Reviewers"
                    removeRoles="[OWNER]"
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.getByText('hope')).toBeInTheDocument();
        });

        it('should never render a removed item, whatever the role', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        { value: 'gone', createdBy: ViewerId, isDeleted: true },
                        {
                            value: 'also-gone',
                            createdBy: ViewerId,
                            approvalStatus: ApprovalStatus.Approved,
                            isDeleted: true
                        }
                    ]} />);

            // then: removal outranks approval — a taken-down row is gone even to an administrator
            expect(screen.queryByText('gone')).not.toBeInTheDocument();
            expect(screen.queryByText('also-gone')).not.toBeInTheDocument();
        });

        it('should not render a removed item even with the filter turned off', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        { value: 'gone', createdBy: ViewerId, isDeleted: true }
                    ]} />);

            // then
            expect(screen.queryByText('gone')).not.toBeInTheDocument();
        });

        it('should hide a draft from the publishing tier — it was never put forward', () => {
            // given
            signInAs(authState, ['Publishers']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[
                        itemWith('drafted', ApprovalStatus.Draft, OtherId)
                    ]} />);

            // then: a draft is its author's alone, and administrators' by viewAllRoles
            expect(screen.queryByText('drafted')).not.toBeInTheDocument();
        });

        it('should let the widest grant see a draft nobody else can', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        itemWith('drafted', ApprovalStatus.Draft, OtherId)
                    ]} />);

            // then
            expect(screen.getByText('drafted')).toBeInTheDocument();
        });

        it('should hide a refusal from the publishing tier — it has already been judged', () => {
            // given
            signInAs(authState, ['Publishers']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[
                        itemWith('refused', ApprovalStatus.Rejected, OtherId),
                        itemWith('dismissed', ApprovalStatus.Dismissed, OtherId)
                    ]} />);

            // then: moderation stops at submissions, so a settled refusal falls to viewAllRoles
            expect(screen.queryByText('refused')).not.toBeInTheDocument();
            expect(screen.queryByText('dismissed')).not.toBeInTheDocument();
        });

        it('should let the widest grant see a refusal and act on it', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[
                        itemWith('refused', ApprovalStatus.Rejected, OtherId),
                        itemWith('dismissed', ApprovalStatus.Dismissed, OtherId)
                    ]} />);

            // then
            expect(screen.getByText('refused')).toBeInTheDocument();
            expect(screen.getByText('dismissed')).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Remove refused' })).toBeInTheDocument();
        });

        it('should not let a reviewer see a draft or a refusal, only what awaits them', () => {
            // given
            signInAs(authState, ['Reviewers']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        itemWith('drafted', ApprovalStatus.Draft, OtherId),
                        itemWith('refused', ApprovalStatus.Rejected, OtherId),
                        itemWith('waiting', ApprovalStatus.Submitted, OtherId)
                    ]} />);

            // then: a reviewer decides submissions — a draft is not yet theirs, a refusal no
            // longer is
            expect(screen.queryByText('drafted')).not.toBeInTheDocument();
            expect(screen.queryByText('refused')).not.toBeInTheDocument();
            expect(screen.getByText('waiting')).toBeInTheDocument();
        });

        it('should apply the gates even on a moderation surface', () => {
            // given: switching the ACTIONS on must not widen who can SEE things
            signInAs(authState, ['Members']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.queryByText('hope')).not.toBeInTheDocument();
        });
    });

    describe('delete', () => {
        it('should not render the delete control when deleting is off', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={false}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.queryByRole('button', { name: /Remove faith/ })).not.toBeInTheDocument();
        });

        it('should not render the delete control for an anonymous reader', () => {
            // given
            signOut(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.queryByRole('button', { name: /Remove faith/ })).not.toBeInTheDocument();
        });

        it('should let the owner withdraw their own submitted item', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope')]} />);

            // then
            expect(screen.getByRole('button', { name: 'Remove hope' })).toBeInTheDocument();
        });

        it('should let the owner withdraw their own draft item', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[itemWith('hope', ApprovalStatus.Draft, ViewerId)]} />);

            // then
            expect(screen.getByRole('button', { name: 'Remove hope' })).toBeInTheDocument();
        });

        it('should hide a rejected item from its own contributor entirely', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[
                        itemWith('hope', ApprovalStatus.Rejected, ViewerId)
                    ]} />);

            // then: a refusal is not the contributor's to keep revisiting — no chip, no action
            expect(screen.queryByText('hope')).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Remove hope/ })).not.toBeInTheDocument();
        });

        it('should not let the owner withdraw their own item once it is approved', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[approvedItem('faith', ViewerId)]} />);

            // then
            expect(screen.queryByRole('button', { name: /Remove faith/ })).not.toBeInTheDocument();
        });

        it('should let an administrator remove an approved item they do not own', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.getByRole('button', { name: 'Remove faith' })).toBeInTheDocument();
        });

        it('should not let a signed-in reader remove an item they neither own nor moderate', () => {
            // given
            signInAs(authState, ['Members']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.queryByRole('button', { name: /Remove faith/ })).not.toBeInTheDocument();
        });

        it('should let any authenticated reader remove when the role list is empty', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    removeRoles=""
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.getByRole('button', { name: 'Remove faith' })).toBeInTheDocument();
        });

        it('should raise onRemove with the item and honour a custom tooltip', async () => {
            // given
            signInAs(authState, ['Administrators']);
            const onRemove = vi.fn();
            const item = approvedItem('faith');

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    removeTooltip="Withdraw"
                    onRemove={onRemove}
                    associationCollection={[item]} />);

            await userEvent.click(screen.getByRole('button', { name: 'Withdraw faith' }));

            // then
            expect(onRemove).toHaveBeenCalledTimes(1);
            expect(onRemove).toHaveBeenCalledWith(item);
        });
    });

    describe('approve and deny', () => {
        it('should offer both decisions to a moderator on someone else\'s submission', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.getByRole('button', { name: 'Approve hope' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Reject hope' })).toBeInTheDocument();
        });

        it('should offer all three actions together on someone else\'s submission', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then: Remove destroys the row, Reject records a refusal — a moderator wants both
            expect(screen.getByRole('button', { name: 'Remove hope' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Reject hope' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Approve hope' })).toBeInTheDocument();
        });

        it('should order the three actions by escalating consequence', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            const chip = screen.getByText('hope').closest('span.g2h-association-chip');

            const labels = Array.from(chip?.querySelectorAll('button') ?? [])
                .map((button) => button.getAttribute('aria-label'));

            expect(labels).toEqual(['Remove hope', 'Reject hope', 'Approve hope']);
        });

        it('should raise onReject separately from onRemove', async () => {
            // given
            signInAs(authState, ['Administrators']);
            const onRemove = vi.fn();
            const onReject = vi.fn();
            const item = submittedItem('hope', OtherId);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    onRemove={onRemove}
                    onReject={onReject}
                    associationCollection={[item]} />);

            await userEvent.click(screen.getByRole('button', { name: 'Reject hope' }));

            // then: rejecting is a verdict, not a deletion
            expect(onReject).toHaveBeenCalledTimes(1);
            expect(onReject).toHaveBeenCalledWith(item);
            expect(onRemove).not.toHaveBeenCalled();
        });

        it('should raise onApprove and onReject with the item', async () => {
            // given
            signInAs(authState, ['Administrators']);
            const onApprove = vi.fn();
            const onReject = vi.fn();
            const item = submittedItem('hope', OtherId);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    onApprove={onApprove}
                    onReject={onReject}
                    associationCollection={[item]} />);

            await userEvent.click(screen.getByRole('button', { name: 'Approve hope' }));
            await userEvent.click(screen.getByRole('button', { name: 'Reject hope' }));

            // then
            expect(onApprove).toHaveBeenCalledTimes(1);
            expect(onApprove).toHaveBeenCalledWith(item);
            expect(onReject).toHaveBeenCalledTimes(1);
            expect(onReject).toHaveBeenCalledWith(item);
        });

        it('should give the owner a delete rather than a decision on their own submission', () => {
            // given: an administrator who also happens to be the contributor
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', ViewerId)]} />);

            // then: nobody waves through their own submission, role or not
            expect(screen.queryByRole('button', { name: /Approve hope/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Reject hope/ })).not.toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Remove hope' })).toBeInTheDocument();
        });

        it('should not offer a decision to a reader without a moderation role', () => {
            // given
            signInAs(authState, ['Members']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.queryByRole('button', { name: /Approve hope/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Reject hope/ })).not.toBeInTheDocument();
        });

        it('should not offer a decision on an already approved item', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[approvedItem('faith')]} />);

            // then
            expect(screen.queryByRole('button', { name: /Approve faith/ })).not.toBeInTheDocument();
        });

        it('should not offer a decision when moderation is switched off', () => {
            // given
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={false}
                    associationCollection={[submittedItem('hope', OtherId)]} />);

            // then
            expect(screen.queryByRole('button', { name: /Approve hope/ })).not.toBeInTheDocument();
        });

        it('should still let the owner withdraw their own item with moderation switched off', () => {
            // given: a panel that takes suggestions but hands the decision to a back office
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showModerationActions={true}
                    associationCollection={[submittedItem('hope', ViewerId)]} />);

            // then: the two gates are independent — no decision offered, withdrawal intact
            expect(screen.getByRole('button', { name: 'Remove hope' })).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Approve hope/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Reject hope/ })).not.toBeInTheDocument();
        });

        it('should offer an administrator nothing at all in the default read-only posture', () => {
            // given: actions off, which is the default — the panel renders but cannot be acted on
            signInAs(authState, ['Administrators']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        submittedItem('hope', OtherId),
                        approvedItem('faith')
                    ]} />);

            // then: both chips are visible to an administrator, neither carries a button
            expect(screen.getByText('hope')).toBeInTheDocument();
            expect(screen.getByText('faith')).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Approve hope/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Reject hope/ })).not.toBeInTheDocument();
            expect(screen.queryByRole('button', { name: /Remove/ })).not.toBeInTheDocument();
        });

        it('should still let a contributor withdraw their own unapproved item with actions off', () => {
            // given: the one carve-out — someone tidying their own suggestions before a verdict
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    associationCollection={[
                        submittedItem('mine', ViewerId),
                        itemWith('my-draft', ApprovalStatus.Draft, ViewerId),
                        approvedItem('mine-approved', ViewerId)
                    ]} />);

            // then
            expect(screen.getByRole('button', { name: 'Remove mine' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Remove my-draft' })).toBeInTheDocument();

            // …but only while it is still unapproved
            expect(screen.queryByRole('button', { name: /Remove mine-approved/ }))
                .not.toBeInTheDocument();
        });
    });

    describe('add', () => {
        it('should render the add box for an authenticated reader', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    suggestTitle="Suggest a tag"
                    addPlaceholderText="Start typing a tag…" />);

            // then
            expect(screen.getByPlaceholderText('Start typing a tag…')).toBeInTheDocument();
            expect(screen.getByText('Suggest a tag')).toBeInTheDocument();
        });

        it('should replace the add box with a login link carrying the return url', () => {
            // given
            signOut(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    loginButtonText="Login to suggest a tag" />);

            // then
            expect(screen.queryByPlaceholderText('Start typing a tag…')).not.toBeInTheDocument();

            expect(screen.getByRole('link', { name: /Login to suggest a tag/ }))
                .toHaveAttribute('href', '/Account/Login?returnUrl=%2FSecured%2FPage');
        });

        it('should raise loginButtonOnClick instead of linking when a handler is supplied', async () => {
            // given
            signOut(authState);
            const loginButtonOnClick = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    loginButtonText="Sign in"
                    loginButtonOnClick={loginButtonOnClick} />);

            await userEvent.click(screen.getByRole('button', { name: /Sign in/ }));

            // then
            expect(loginButtonOnClick).toHaveBeenCalledTimes(1);
            expect(screen.queryByRole('link', { name: /Sign in/ })).not.toBeInTheDocument();
        });

        it('should hide the add box entirely from a signed-in reader lacking the add role', () => {
            // given
            signInAs(authState, ['Members']);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addRoles="Administrators"
                    addPlaceholderText="Start typing a tag…"
                    loginButtonText="Login to suggest a tag" />);

            // then: already signed in, so no login prompt either
            expect(screen.queryByPlaceholderText('Start typing a tag…')).not.toBeInTheDocument();
            expect(screen.queryByText(/Login to suggest/)).not.toBeInTheDocument();
        });

        it('should raise onAdd with the normalized value on Enter and clear the box', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    normalizeAddedValue={(rawValue) => rawValue.trim().replace(/^#+/, '')}
                    onAdd={onAdd} />);

            const input = screen.getByPlaceholderText('Start typing a tag…');
            await userEvent.type(input, '  #faith  {Enter}');

            // then
            expect(onAdd).toHaveBeenCalledTimes(1);
            expect(onAdd).toHaveBeenCalledWith('faith');
            expect(input).toHaveValue('');
        });

        it('should refuse a duplicate of an existing item, whatever its casing', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    associationCollection={[approvedItem('Faith')]}
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'), 'faith{Enter}');

            // then
            expect(onAdd).not.toHaveBeenCalled();
        });

        it('should refuse an empty suggestion', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'), '   {Enter}');

            // then
            expect(onAdd).not.toHaveBeenCalled();
        });

        it('should separate a comma-separated box into one call per association', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    onAdd={onAdd} />);

            const input = screen.getByPlaceholderText('Start typing a tag…');
            await userEvent.type(input, 'faith, healing{Enter}');

            // then
            expect(onAdd).toHaveBeenCalledTimes(2);
            expect(onAdd).toHaveBeenNthCalledWith(1, 'faith');
            expect(onAdd).toHaveBeenNthCalledWith(2, 'healing');
            expect(input).toHaveValue('');
        });

        it('should separate on a semicolon and leave the words inside a value alone', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'),
                'grace and faith; love{Enter}');

            // then: "and" is part of the association, not a separator
            expect(onAdd).toHaveBeenCalledTimes(2);
            expect(onAdd).toHaveBeenNthCalledWith(1, 'grace and faith');
            expect(onAdd).toHaveBeenNthCalledWith(2, 'love');
        });

        it('should drop the blanks and the repeats and still add the rest', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'),
                'faith, , Faith; healing,{Enter}');

            // then
            expect(onAdd).toHaveBeenCalledTimes(2);
            expect(onAdd).toHaveBeenNthCalledWith(1, 'faith');
            expect(onAdd).toHaveBeenNthCalledWith(2, 'healing');
        });

        it('should refuse the value already listed without costing the others', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    associationCollection={[approvedItem('Faith')]}
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'), 'faith, healing{Enter}');

            // then
            expect(onAdd).toHaveBeenCalledTimes(1);
            expect(onAdd).toHaveBeenCalledWith('healing');
        });

        it('should normalize each separated value rather than the box as a whole', async () => {
            // given
            signInAs(authState);
            const onAdd = vi.fn();

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…"
                    normalizeAddedValue={(rawValue) => rawValue.trim().replace(/^#+/, '')}
                    onAdd={onAdd} />);

            await userEvent.type(
                screen.getByPlaceholderText('Start typing a tag…'), '  #faith ; #hope  {Enter}');

            // then
            expect(onAdd).toHaveBeenNthCalledWith(1, 'faith');
            expect(onAdd).toHaveBeenNthCalledWith(2, 'hope');
        });

        it('should offer no add button beside the box — Enter is the way in', () => {
            // given
            signInAs(authState);

            // when
            renderWithAuth(
                <AssociationPanel
                    title="Tags"
                    showAdd={true}
                    addPlaceholderText="Start typing a tag…" />);

            // then
            expect(screen.getByPlaceholderText('Start typing a tag…')).toBeInTheDocument();
            expect(screen.queryByRole('button', { name: 'Add' })).not.toBeInTheDocument();
        });
    });

    describe('border', () => {
        it('should carry the class that neutralises the theme\'s section padding', () => {
            // when
            const { container } = renderWithAuth(<AssociationPanel title="Tags" />);

            // then: the Blogzine theme pads every bare <section> by 3.5rem/2.8rem, which is
            // page-band spacing and wrong for a panel inside a card or a sidebar
            expect(container.querySelector('section')).toHaveClass('g2h-association-panel');
        });

        it('should keep the neutralising class when bordered', () => {
            // when
            const { container } = renderWithAuth(
                <AssociationPanel title="Tags" showBorder={true} />);

            // then
            const panel = container.querySelector('section');
            expect(panel).toHaveClass('g2h-association-panel');
            expect(panel).toHaveClass('p-3');
        });

        it('should leave the panel unbordered by default', () => {
            // when
            const { container } = renderWithAuth(<AssociationPanel title="Tags" />);

            // then
            expect(container.querySelector('section')).not.toHaveClass('border');
        });

        it('should surround the panel with a border when asked', () => {
            // when
            const { container } = renderWithAuth(
                <AssociationPanel title="Tags" showBorder={true} />);

            // then
            expect(container.querySelector('section')).toHaveClass('border');
        });
    });
});
