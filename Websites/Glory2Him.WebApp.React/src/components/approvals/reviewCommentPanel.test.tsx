import { render, screen, within } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import userEvent from '@testing-library/user-event';
import { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '../securitys/authProvider';
import { ReviewCommentPanel } from './reviewCommentPanel';
import { createAuthState, signInAs, signOut } from '../../tests/testAuth';

import {
    ApprovalCommentType,
    ReviewCommentItem
} from '../../models/components/approvals/reviewCommentItem';

// THE REVIEW THREAD, all four faces. Every fact the panel shows comes from the collection it is
// handed and the identity it is rendered under, so each test varies exactly one of those two.
//
// The auth double is here because most of what this panel decides is an identity question:
// amending is the author's alone, and settling is the author's or the publisher tier's. Render
// gates only — the foundation re-decides add, modify, remove and resolve against the stored row
// (§14.6), which is why no test here asserts a server outcome.
const authState = createAuthState();

vi.mock('../../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const renderPanel = (ui: ReactElement) =>
    render(
        <MemoryRouter initialEntries={['/Admin/Posts/item-1']}>
            <AuthProvider>{ui}</AuthProvider>
        </MemoryRouter>);

const approvalId = 'approval-1';

// signInAs stands the viewer up as user-1, so a row authored by that id is "mine" and anything
// else is somebody else's.
const viewerId = 'user-1';
const otherAuthorId = 'somebody-else';

const comment = (overrides: Partial<ReviewCommentItem> = {}): ReviewCommentItem => ({
    id: 'comment-1',
    approvalId,
    comment: 'I think it works — being moved to tears is exactly the feeling this captures.',
    commentType: ApprovalCommentType.Comment,
    isResolved: true,
    authorId: otherAuthorId,
    authorDisplayName: 'Susan',
    authorUserName: 'susan',
    createdWhen: '2026-08-26T09:15:00Z',
    ...overrides
});

const question = (overrides: Partial<ReviewCommentItem> = {}): ReviewCommentItem =>
    comment({
        id: 'comment-2',
        comment: 'Does a crying face read as "moved" or as sadness?',
        commentType: ApprovalCommentType.Question,
        isResolved: false,
        authorDisplayName: 'John',
        authorUserName: 'john',
        createdWhen: '2026-08-27T14:40:00Z',
        ...overrides
    });

const resolveTick = (): HTMLInputElement =>
    screen.getByRole('checkbox', { name: /Is resolved/ }) as HTMLInputElement;

describe('ReviewCommentPanel', () => {
    beforeEach(() => {
        signInAs(authState, []);
    });

    describe('the thread', () => {
        it('should render newest first whatever order the consumer hands it over in', () => {
            // given: the older row FIRST, which is what a consumer that forwarded an OData
            // response unsorted would do
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment(), question()]} />);

            // then
            const rendered = screen.getAllByRole('article');

            expect(within(rendered[0]).getByText('John')).toBeInTheDocument();
            expect(within(rendered[1]).getByText('Susan')).toBeInTheDocument();
        });

        it('should name the author, chip the type and print the date AND the time', () => {
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[question()]} />);

            // Scoped to the ROW: the add box's own radio is labelled "Question" too, and an
            // unscoped query would pass on the control rather than on the chip.
            const row = screen.getByRole('article');

            expect(within(row).getByText('John')).toBeInTheDocument();
            expect(within(row).getByText('(john)')).toBeInTheDocument();
            expect(within(row).getByText('Question')).toBeInTheDocument();

            // The time is half the claim: the screenshot omits it and the requirement does not.
            // Matched loosely on purpose — formatDateTime renders in the RUNNER's timezone, so
            // pinning the hour would make this test fail in another one.
            expect(screen.getByText(/Aug 27, 2026 at \d{1,2}:\d{2} (AM|PM)/))
                .toBeInTheDocument();
        });

        it('should render the display name alone when nothing resolved a username', () => {
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorUserName: undefined })]} />);

            expect(screen.getByText('Susan')).toBeInTheDocument();
            expect(screen.queryByText('()')).not.toBeInTheDocument();
        });

        it('should say so rather than render an empty list when nothing has been said', () => {
            renderPanel(<ReviewCommentPanel approvalId={approvalId} reviewComments={[]} />);

            expect(screen.getByText('Nothing has been said about this submission yet.'))
                .toBeInTheDocument();
        });
    });

    describe('adding', () => {
        it('should save the words, the chosen type and the approval id', async () => {
            const saved = vi.fn();

            renderPanel(<ReviewCommentPanel approvalId={approvalId} onSave={saved} />);

            await userEvent.type(
                screen.getByPlaceholderText('Write a comment or ask a question…'),
                'Is this quote actually Moody?');

            await userEvent.click(screen.getByRole('radio', { name: 'Question' }));
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            expect(saved).toHaveBeenCalledWith({
                approvalId,
                comment: 'Is this quote actually Moody?',
                commentType: ApprovalCommentType.Question
            });
        });

        it('should open on defaultType and put Clear back to it', async () => {
            const cleared = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    defaultType={ApprovalCommentType.Question}
                    onClear={cleared} />);

            expect(screen.getByRole('radio', { name: 'Question' })).toBeChecked();

            // when: the reader changes their mind about both halves
            const box = screen.getByPlaceholderText('Write a comment or ask a question…');
            await userEvent.type(box, 'never mind');
            await userEvent.click(screen.getByRole('radio', { name: 'Comment' }));
            await userEvent.click(screen.getByRole('button', { name: 'Clear' }));

            // then: the box is empty and the radios are back on defaultType, not on Comment
            expect(box).toHaveValue('');
            expect(screen.getByRole('radio', { name: 'Question' })).toBeChecked();
            expect(cleared).toHaveBeenCalledOnce();
        });

        it('should refuse a blank comment', async () => {
            const saved = vi.fn();

            renderPanel(<ReviewCommentPanel approvalId={approvalId} onSave={saved} />);

            await userEvent.type(
                screen.getByPlaceholderText('Write a comment or ask a question…'), '   ');

            expect(screen.getByRole('button', { name: 'Save' })).toBeDisabled();
            expect(saved).not.toHaveBeenCalled();
        });

        it('should clear the box once a save has been raised', async () => {
            renderPanel(<ReviewCommentPanel approvalId={approvalId} onSave={vi.fn()} />);

            const box = screen.getByPlaceholderText('Write a comment or ask a question…');
            await userEvent.type(box, 'a remark');
            await userEvent.click(screen.getByRole('button', { name: 'Save' }));

            expect(box).toHaveValue('');
        });

        it('should offer no box to a signed-out reader, and say why', () => {
            signOut(authState);

            renderPanel(<ReviewCommentPanel approvalId={approvalId} />);

            expect(screen.queryByPlaceholderText('Write a comment or ask a question…'))
                .not.toBeInTheDocument();

            expect(screen.getByText('Sign in to comment on this submission.'))
                .toBeInTheDocument();
        });

        it('should offer no box against a global ReadOnly sanction', () => {
            signInAs(authState, ['Publishers', 'ReadOnly']);

            renderPanel(<ReviewCommentPanel approvalId={approvalId} />);

            expect(screen.queryByPlaceholderText('Write a comment or ask a question…'))
                .not.toBeInTheDocument();

            expect(screen.getByText(/read-only/)).toBeInTheDocument();
        });

        it('should offer no box when there is no approval round to speak on', () => {
            renderPanel(<ReviewCommentPanel approvalId="" />);

            expect(screen.queryByPlaceholderText('Write a comment or ask a question…'))
                .not.toBeInTheDocument();

            expect(screen.getByText(/no approval round/)).toBeInTheDocument();
        });
    });

    describe('the resolve tick', () => {
        it('should render on a question for its author', () => {
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[question({ authorId: viewerId })]} />);

            expect(resolveTick()).toBeInTheDocument();
            expect(resolveTick().checked).toBe(false);
        });

        it('should never render on a comment, whoever is looking', () => {
            signInAs(authState, ['Administrators']);

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId })]} />);

            expect(screen.queryByRole('checkbox', { name: /Is resolved/ }))
                .not.toBeInTheDocument();
        });

        it.each([
            ['Administrators'],
            ['Publishers'],
            ['ContentItem-Publishers'],
            ['ContentItem-Quote-Publishers']
        ])('should render on somebody else\'s question for %s', (role) => {
            signInAs(authState, [role]);

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    entityType="ContentItem"
                    contentType="Quote"
                    reviewComments={[question()]} />);

            expect(resolveTick()).toBeInTheDocument();
        });

        it.each([
            ['Reviewers'],
            ['ContentItem-Reviewers'],
            ['ContentItem-Quote-Reviewers'],
            ['ContentItem-Devotional-Publishers']
        ])('should not render on somebody else\'s question for %s', (role) => {
            signInAs(authState, [role]);

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    entityType="ContentItem"
                    contentType="Quote"
                    reviewComments={[question()]} />);

            expect(screen.queryByRole('checkbox', { name: /Is resolved/ }))
                .not.toBeInTheDocument();
        });

        it.each([
            ['ReadOnly'],
            ['ContentItem-ReadOnly'],
            ['ContentItem-Quote-ReadOnly']
        ])('should withhold the tick from a publisher sanctioned by %s', (block) => {
            // the sanction outranks the grant (§18.6 rule 2), at every scope the entity composes
            signInAs(authState, ['Administrators', block]);

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    entityType="ContentItem"
                    contentType="Quote"
                    reviewComments={[question()]} />);

            expect(screen.queryByRole('checkbox', { name: /Is resolved/ }))
                .not.toBeInTheDocument();
        });

        it('should raise both directions off one control', async () => {
            const resolved = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[question({ authorId: viewerId })]}
                    onResolvedChanged={resolved} />);

            await userEvent.click(resolveTick());

            expect(resolved).toHaveBeenCalledWith(
                expect.objectContaining({ id: 'comment-2' }), true);

            // and: the panel does NOT flip the tick itself — the row it renders is the
            // consumer's, so an un-persisted click must not look like a settled comment
            expect(resolveTick().checked).toBe(false);
        });

        it('should raise the unsettling direction off a settled question', async () => {
            const resolved = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[question({ authorId: viewerId, isResolved: true })]}
                    onResolvedChanged={resolved} />);

            expect(resolveTick().checked).toBe(true);
            await userEvent.click(resolveTick());

            expect(resolved).toHaveBeenCalledWith(
                expect.objectContaining({ id: 'comment-2' }), false);
        });
    });

    describe('editing and withdrawing', () => {
        it('should offer Edit and Delete to the author alone', () => {
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId }), question()]} />);

            expect(screen.getAllByRole('button', { name: /^Edit comment by/ }))
                .toHaveLength(1);

            expect(screen.getByRole('button', { name: 'Edit comment by Susan' }))
                .toBeInTheDocument();
        });

        it('should offer neither to an administrator on somebody else\'s row', () => {
            // no role widens amending or withdrawing another person's words (§12.3.1 rule 5)
            signInAs(authState, ['Administrators']);

            renderPanel(
                <ReviewCommentPanel approvalId={approvalId} reviewComments={[question()]} />);

            expect(screen.queryByRole('button', { name: /^Edit comment by/ }))
                .not.toBeInTheDocument();

            expect(screen.queryByRole('button', { name: /^Delete comment by/ }))
                .not.toBeInTheDocument();
        });

        it('should swap the row for the edit template and commit the changed row', async () => {
            const modified = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId })]}
                    onModified={modified} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Edit comment by Susan' }));

            const editor = screen.getByLabelText('Edit your comment');
            await userEvent.clear(editor);
            await userEvent.type(editor, 'Rewritten.');

            // Everything from here is scoped to the ROW being edited: the add box carries the
            // same radio pair and the same Save label, so an unscoped query would drive the
            // wrong face and pass for the wrong reason.
            const row = screen.getByRole('article');
            await userEvent.click(within(row).getByRole('radio', { name: 'Question' }));
            await userEvent.click(within(row).getByRole('button', { name: 'Save' }));

            expect(modified).toHaveBeenCalledWith(expect.objectContaining({
                id: 'comment-1',
                comment: 'Rewritten.',
                commentType: ApprovalCommentType.Question,

                // untouched: settling is a different decision with a different tier
                isResolved: true
            }));
        });

        it('should restore the row exactly as it was when the edit is cancelled', async () => {
            const modified = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId })]}
                    onModified={modified} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Edit comment by Susan' }));

            const editor = screen.getByLabelText('Edit your comment');
            await userEvent.clear(editor);
            await userEvent.type(editor, 'Something else entirely.');
            await userEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            expect(screen.getByText(/being moved to tears/)).toBeInTheDocument();
            expect(screen.queryByText('Something else entirely.')).not.toBeInTheDocument();
            expect(modified).not.toHaveBeenCalled();
        });

        it('should raise the removal as an INTENT so the consumer can ask first', async () => {
            const removeRequested = vi.fn();
            const removed = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId })]}
                    onRemoveRequested={removeRequested}
                    onRemoved={removed} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Delete comment by Susan' }));

            expect(removeRequested).toHaveBeenCalledWith(
                expect.objectContaining({ id: 'comment-1' }));

            // and NOT both: one click must never be a request AND a removal
            expect(removed).not.toHaveBeenCalled();
        });

        it('should remove directly for a surface with nothing to ask', async () => {
            const removed = vi.fn();

            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[comment({ authorId: viewerId })]}
                    onRemoved={removed} />);

            await userEvent.click(
                screen.getByRole('button', { name: 'Delete comment by Susan' }));

            expect(removed).toHaveBeenCalledWith(
                expect.objectContaining({ id: 'comment-1' }));
        });
    });

    describe('the collection states', () => {
        it('should hold the thread back behind a spinner rather than emptying it', () => {
            renderPanel(
                <ReviewCommentPanel approvalId={approvalId} reviewComments={[]} isLoading />);

            expect(screen.getByText('Loading…')).toBeInTheDocument();

            expect(screen.queryByText('Nothing has been said about this submission yet.'))
                .not.toBeInTheDocument();
        });

        it('should freeze every control while the consumer is persisting', () => {
            renderPanel(
                <ReviewCommentPanel
                    approvalId={approvalId}
                    reviewComments={[question({ authorId: viewerId })]}
                    isSubmitting />);

            expect(screen.getByRole('button', { name: 'Clear' })).toBeDisabled();
            expect(screen.getByRole('button', { name: /^Edit comment by/ })).toBeDisabled();
            expect(screen.getByRole('button', { name: /^Delete comment by/ })).toBeDisabled();
            expect(resolveTick()).toBeDisabled();
        });

        it('should offer a load-more button where IntersectionObserver is unavailable', async () => {
            const loadMore = vi.fn();
            const observer = globalThis.IntersectionObserver;

            // @ts-expect-error — deliberately removing the API to take the fallback path
            delete globalThis.IntersectionObserver;

            try {
                renderPanel(
                    <ReviewCommentPanel
                        approvalId={approvalId}
                        reviewComments={[comment()]}
                        hasMore
                        onLoadMore={loadMore} />);

                await userEvent.click(screen.getByRole('button', { name: 'Load more' }));
                expect(loadMore).toHaveBeenCalledOnce();
            } finally {
                globalThis.IntersectionObserver = observer;
            }
        });
    });
});
