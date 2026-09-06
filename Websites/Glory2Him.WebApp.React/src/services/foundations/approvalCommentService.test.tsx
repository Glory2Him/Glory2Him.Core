import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { approvalCommentService } from './approvalCommentService';
import { ApprovalCommentType } from '../../models/foundations/approvals/approvalComment';

// THE THREAD'S WRITES, mocked at the BROKER so the requests this service composes are what gets
// asserted. The derivation below is the single most consequential line in the family — it decides
// whether a comment blocks an approval — and until this file nothing exercised it: the page test
// mocks this service away wholesale.
const getApprovalCommentsAsync = vi.fn();
const postApprovalCommentAsync = vi.fn();
const putApprovalCommentAsync = vi.fn();
const deleteApprovalCommentByIdAsync = vi.fn();
const postApprovalCommentResolveAsync = vi.fn();

vi.mock('../../brokers/apiBroker.approvalComments', () => ({
    default: class {
        GetApprovalCommentsAsync = getApprovalCommentsAsync;
        PostApprovalCommentAsync = postApprovalCommentAsync;
        PutApprovalCommentAsync = putApprovalCommentAsync;
        DeleteApprovalCommentByIdAsync = deleteApprovalCommentByIdAsync;
        PostApprovalCommentResolveAsync = postApprovalCommentResolveAsync;
    }
}));

describe('approvalCommentService', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        getApprovalCommentsAsync.mockResolvedValue([]);
        postApprovalCommentAsync.mockResolvedValue({ approvalId: 'approval-1' });
        putApprovalCommentAsync.mockResolvedValue({ approvalId: 'approval-1' });
        deleteApprovalCommentByIdAsync.mockResolvedValue({ approvalId: 'approval-1' });
        postApprovalCommentResolveAsync.mockResolvedValue({ approvalId: 'approval-1' });

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    describe('reading the thread', () => {
        it('should ask for the thread of the approval it was given', async () => {
            // when
            const { result } = renderHook(
                () => approvalCommentService.useGetApprovalComments('approval-1'),
                { wrapper });

            // then
            await waitFor(() => expect(result.current.isSuccess).toBe(true));
            expect(getApprovalCommentsAsync).toHaveBeenCalledWith('approval-1');
        });

        // An ungated read would ask the server for `approvalId eq  and isDeleted eq false` — a
        // malformed filter, refused, and refused SILENTLY because this read suppresses its toast.
        it('should ask for nothing before an approval id arrives', () => {
            // when
            renderHook(
                () => approvalCommentService.useGetApprovalComments(''),
                { wrapper });

            // then
            expect(getApprovalCommentsAsync).not.toHaveBeenCalled();
        });

        it('should ask for nothing when the caller disabled it', () => {
            // when
            renderHook(
                () => approvalCommentService.useGetApprovalComments('approval-1', false),
                { wrapper });

            // then
            expect(getApprovalCommentsAsync).not.toHaveBeenCalled();
        });
    });

    describe('adding', () => {
        // §7.8's own sentence, and the ONE place the client states it: an ask is born outstanding
        // and holds the approval shut, a remark is born settled and never blocks. The server now
        // refuses the settled ask outright, so this is the client agreeing with the gate rather
        // than the client being the gate.
        it.each([
            [ApprovalCommentType.Question, false],
            [ApprovalCommentType.Comment, true]
        ])('should give type %s the birth resolution %s', async (commentType, isResolved) => {
            // given
            const { result } = renderHook(
                () => approvalCommentService.useAddApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalId: 'approval-1',
                    comment: 'anything',
                    commentType
                });
            });

            // then
            expect(postApprovalCommentAsync).toHaveBeenCalledWith(
                expect.objectContaining({ commentType, isResolved }));
        });

        // The foundation refuses an empty Guid and never mints one, so the id is the client's.
        it('should mint an id for the new row', async () => {
            // given
            const { result } = renderHook(
                () => approvalCommentService.useAddApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalId: 'approval-1',
                    comment: 'anything',
                    commentType: ApprovalCommentType.Comment
                });
            });

            // then
            const sent = postApprovalCommentAsync.mock.calls[0][0];
            expect(sent.id).toEqual(expect.any(String));
            expect(sent.id.length).toBeGreaterThan(0);
            expect(sent.isDeleted).toBe(false);
        });
    });

    describe('what a write refreshes', () => {
        // Every write moves three things: the thread, the verdict's unresolved count and its block
        // reasons, and — for a FIRST comment — the round's name set, which has never carried that
        // author before. Missing the last one rendered the row that just landed as "Unknown
        // author" until something else happened to refetch.
        it.each([
            ['ApprovalComments'],
            ['ApprovalVerdict'],
            ['ReviewerDisplayNames']
        ])('should invalidate %s after a comment is added', async (queryKey) => {
            // given
            const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');

            const { result } = renderHook(
                () => approvalCommentService.useAddApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalId: 'approval-1',
                    comment: 'anything',
                    commentType: ApprovalCommentType.Comment
                });
            });

            // then
            expect(invalidateQueries).toHaveBeenCalledWith(
                expect.objectContaining({
                    queryKey: expect.arrayContaining([queryKey])
                }));
        });

        it('should key the thread invalidation on the approval the row belongs to', async () => {
            // given
            const invalidateQueries = vi.spyOn(queryClient, 'invalidateQueries');

            const { result } = renderHook(
                () => approvalCommentService.useResolveApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalCommentId: 'comment-1',
                    isResolved: true
                });
            });

            // then: taken off the RESPONSE, since the caller only ever holds the comment's id
            expect(invalidateQueries).toHaveBeenCalledWith(
                { queryKey: ['ApprovalComments', 'approval-1'] });
        });
    });

    describe('the other writes', () => {
        it('should send the whole row on a modify', async () => {
            // given
            const storedRow = {
                id: 'comment-1',
                approvalId: 'approval-1',
                comment: 'Amended.',
                commentType: ApprovalCommentType.Comment,
                isResolved: true,
                createdBy: 'user-john',
                createdWhen: '2026-08-27T09:00:00Z',
                updatedBy: 'user-john',
                updatedWhen: '2026-08-27T09:00:00Z',
                isDeleted: false
            };

            const { result } = renderHook(
                () => approvalCommentService.useModifyApprovalComment(), { wrapper });

            // when
            await act(async () => { await result.current.mutateAsync(storedRow); });

            // then
            expect(putApprovalCommentAsync).toHaveBeenCalledWith(storedRow);
        });

        it('should soft delete with the reason it was given', async () => {
            // given
            const { result } = renderHook(
                () => approvalCommentService.useRemoveApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalCommentId: 'comment-1',
                    deletionReason: 'Withdrawn by the author'
                });
            });

            // then
            expect(deleteApprovalCommentByIdAsync).toHaveBeenCalledWith(
                'comment-1', 'Withdrawn by the author');
        });

        // The transition is symmetric by design: a comment settled prematurely must be able to
        // block again.
        it.each([[true], [false]])('should resolve in the %s direction', async (isResolved) => {
            // given
            const { result } = renderHook(
                () => approvalCommentService.useResolveApprovalComment(), { wrapper });

            // when
            await act(async () => {
                await result.current.mutateAsync({
                    approvalCommentId: 'comment-1',
                    isResolved
                });
            });

            // then
            expect(postApprovalCommentResolveAsync)
                .toHaveBeenCalledWith('comment-1', isResolved);
        });
    });
});
