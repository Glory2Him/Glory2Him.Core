import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ApprovalCommentBroker from './apiBroker.approvalComments';
import { ApprovalCommentType } from '../models/foundations/approvals/approvalComment';

// THE THREAD'S ADDRESSES. What matters here is the URLs that actually leave: every consumer above
// this file mocks the service wholesale, so a malformed filter or a dropped query parameter would
// satisfy every other suite in the repo while failing against the real host.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);
const postAsync = vi.mocked(axios.post);
const putAsync = vi.mocked(axios.put);
const deleteAsync = vi.mocked(axios.delete);

const requestedGetUrl = (): string =>
    decodeURIComponent(getAsync.mock.calls[0][0] as string);

describe('ApprovalCommentBroker', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
        postAsync.mockResolvedValue({ data: {} } as never);
        putAsync.mockResolvedValue({ data: {} } as never);
        deleteAsync.mockResolvedValue({ data: {} } as never);
    });

    // Keyed by the APPROVAL, not the entity — which is the whole reason the verdict has to be
    // read first — and withdrawn rows are left behind server-side rather than filtered here.
    it('should ask for the thread by approval, and leave the withdrawn ones behind', async () => {
        // when
        await new ApprovalCommentBroker().GetApprovalCommentsAsync('approval-1');

        // then
        expect(requestedGetUrl()).toBe(
            '/api/approvalcomments?$filter=approvalId eq approval-1 and isDeleted eq false');
    });

    it('should post a comment as a body, with the id the caller minted', async () => {
        // when
        await new ApprovalCommentBroker().PostApprovalCommentAsync({
            id: 'comment-1',
            approvalId: 'approval-1',
            comment: 'Is this quote actually Moody?',
            commentType: ApprovalCommentType.Question,
            isResolved: false,
            isDeleted: false
        });

        // then: no audit fields — the server stamps those from the caller's own identity, and a
        // client has nothing true to put there
        expect(postAsync.mock.calls[0][0]).toBe('/api/approvalcomments');

        expect(postAsync.mock.calls[0][1]).toEqual({
            id: 'comment-1',
            approvalId: 'approval-1',
            comment: 'Is this quote actually Moody?',
            commentType: ApprovalCommentType.Question,
            isResolved: false,
            isDeleted: false
        });
    });

    it('should put the whole row, since four fields are pinned against storage', async () => {
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

        // when
        await new ApprovalCommentBroker().PutApprovalCommentAsync(storedRow);

        // then: the audit values travel, because the foundation compares them before it accepts
        expect(putAsync.mock.calls[0][0]).toBe('/api/approvalcomments');
        expect(putAsync.mock.calls[0][1]).toEqual(storedRow);
    });

    // SOFT removal, never DELETE {id}/Hard: a withdrawn comment leaves the §8.5 block and the
    // record of what was said stays.
    it('should soft delete with the reason on the query string', async () => {
        // when
        await new ApprovalCommentBroker()
            .DeleteApprovalCommentByIdAsync('comment-1', 'Withdrawn by the author');

        // then
        expect(decodeURIComponent(deleteAsync.mock.calls[0][0] as string)).toBe(
            '/api/approvalcomments/comment-1?deletionReason=Withdrawn by the author');
    });

    // The endpoint binds the flag [BindRequired] precisely because an absent bool would bind to
    // false with a valid model state and quietly UN-resolve a settled comment.
    it.each([[true], [false]])(
        'should always send the resolution flag, including %s', async (isResolved) => {
            // when
            await new ApprovalCommentBroker()
                .PostApprovalCommentResolveAsync('comment-1', isResolved);

            // then
            expect(postAsync.mock.calls[0][0]).toBe(
                `/api/approvalcomments/comment-1/Resolve?isResolved=${String(isResolved)}`);
        });
});
