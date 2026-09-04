import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ApprovalBroker from './apiBroker.approvals';
import { EntityTypeName } from '../models/foundations/approvals/approval';
import { ApprovalDecision } from '../models/components/approvals/approvalReviewItem';

// The approval round's reads. What matters here is the ADDRESSES: two hosts answer them, one
// keyed by entity and one by approval, and the reviewer names ride a repeated query parameter
// rather than a joined string.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);
const postAsync = vi.mocked(axios.post);
const putAsync = vi.mocked(axios.put);
const deleteAsync = vi.mocked(axios.delete);

const requestedUrl = (): string =>
    decodeURIComponent(getAsync.mock.calls[0][0] as string);

describe('ApprovalBroker', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
        postAsync.mockResolvedValue({ data: {} } as never);
        putAsync.mockResolvedValue({ data: {} } as never);
        deleteAsync.mockResolvedValue({ data: {} } as never);
    });

    it('should ask for the verdict by entity, naming the type rather than numbering it', async () => {
        // when
        await new ApprovalBroker()
            .GetApprovalVerdictAsync(EntityTypeName.ContentItem, 'item-1');

        // then: the member NAME, so a network log says what the request is about
        expect(requestedUrl()).toBe('/api/approvals/ContentItem/item-1/Verdict');
    });

    /// Reviews are keyed by the APPROVAL, not by the entity — which is the whole reason the
    /// verdict has to be read first.
    it('should ask for the reviews by approval, and leave the withdrawn ones behind', async () => {
        // when
        await new ApprovalBroker().GetApprovalReviewsAsync('approval-1');

        // then
        expect(requestedUrl()).toBe(
            '/api/approvalreviews?$filter=approvalId eq approval-1 and isDeleted eq false');
    });

    it('should ask for the candidates and the requests by entity', async () => {
        // when
        await new ApprovalBroker()
            .GetReviewerCandidatesAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        expect(requestedUrl()).toBe('/api/approvals/ContentItem/item-1/ReviewerCandidates');

        // when
        getAsync.mockClear();
        await new ApprovalBroker()
            .GetReviewRequestsAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        expect(requestedUrl()).toBe('/api/approvals/ContentItem/item-1/ReviewRequests');
    });

    /// ONE round trip for the whole round, and the parameter REPEATED rather than joined on a
    /// comma: that is how the host's string[] binder reads a query array, and it is what keeps
    /// an id containing a comma from being read as two.
    it('should ask for every reviewer name in one request', async () => {
        // when
        await new ApprovalBroker()
            .GetReviewerDisplayNamesAsync(['user-john', 'user-mary']);

        // then
        expect(getAsync).toHaveBeenCalledTimes(1);

        expect(requestedUrl()).toBe(
            '/api/approvals/ReviewerDisplayNames?userIds=user-john&userIds=user-mary');
    });

    // ── Writes ────────────────────────────────────────────────────────────────

    /// The audit fields are the SERVER's to stamp, and an empty one is refused in model binding
    /// before any service sees the row — so the vote carries none. Asserted as the exact body,
    /// not a subset, because a stray `createdWhen: ''` is precisely the regression.
    it('should post a vote as a review row carrying no audit fields', async () => {
        // when
        await new ApprovalBroker().PostApprovalReviewAsync({
            id: 'review-1',
            approvalId: 'approval-1',
            statusId: 2,
            comment: '',
            isDeleted: false
        });

        // then: the plain collection, and ONLY what the client is entitled to say
        expect(postAsync.mock.calls[0][0]).toBe('/api/approvalreviews');

        expect(postAsync.mock.calls[0][1]).toEqual({
            id: 'review-1',
            approvalId: 'approval-1',
            statusId: 2,
            comment: '',
            isDeleted: false
        });
    });

    /// A changed vote amends the row that was read: the exposer routes on the body's id, and
    /// the foundation checks the audit fields against storage — so the whole row goes back.
    it('should put a changed vote to the collection with the row it was read as', async () => {
        // given
        const standing = {
            id: 'review-1',
            approvalId: 'approval-1',
            statusId: 2,
            comment: 'as read',
            createdBy: 'user-1',
            createdWhen: '2026-09-01T00:00:00Z',
            updatedBy: 'user-1',
            updatedWhen: '2026-09-01T00:00:00Z',
            isDeleted: false
        };

        // when
        await new ApprovalBroker().PutApprovalReviewAsync({ ...standing, statusId: 3 });

        // then
        expect(putAsync.mock.calls[0][0]).toBe('/api/approvalreviews');
        expect(putAsync.mock.calls[0][1]).toEqual({ ...standing, statusId: 3 });
    });

    it('should post the decision by entity with the bypass and its reason on the query', async () => {
        // when
        await new ApprovalBroker().PostApprovalDecisionAsync(
            EntityTypeName.ContentItem, 'item-1', ApprovalDecision.Approve, true, 'Trusted source');

        // then: the enum by its number, which is what the host binds, and the reason encoded
        expect(decodeURIComponent(postAsync.mock.calls[0][0] as string)).toBe(
            '/api/approvals/ContentItem/item-1/Decision'
                + '?decision=0&isBypassRequested=true&bypassReason=Trusted+source');
    });

    /// An absent reason, not an empty one: the orchestration reads a missing bypassReason as
    /// the ordinary "none supplied", and an empty string is one more blank to recognise.
    it('should leave the reason off a plain decision', async () => {
        // when
        await new ApprovalBroker().PostApprovalDecisionAsync(
            EntityTypeName.ContentItem, 'item-1', ApprovalDecision.Reject, false, '   ');

        // then
        expect(postAsync.mock.calls[0][0]).toBe(
            '/api/approvals/ContentItem/item-1/Decision?decision=1&isBypassRequested=false');
    });

    it('should post and delete a review request by entity, naming who was asked', async () => {
        // when
        const broker = new ApprovalBroker();
        await broker.PostReviewRequestAsync(EntityTypeName.ContentItem, 'item-1', 'user mary');
        await broker.DeleteReviewRequestAsync(EntityTypeName.ContentItem, 'item-1', 'user mary');

        // then: the same address both ways, the id encoded
        expect(postAsync.mock.calls[0][0]).toBe(
            '/api/approvals/ContentItem/item-1/ReviewRequests?requestedUserId=user%20mary');

        expect(deleteAsync.mock.calls[0][0]).toBe(
            '/api/approvals/ContentItem/item-1/ReviewRequests?requestedUserId=user%20mary');
    });
});
