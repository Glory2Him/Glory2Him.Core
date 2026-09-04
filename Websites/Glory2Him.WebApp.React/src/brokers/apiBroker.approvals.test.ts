import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ApprovalBroker from './apiBroker.approvals';
import { EntityTypeName } from '../models/foundations/approvals/approval';

// The approval round's reads. What matters here is the ADDRESSES: two hosts answer them, one
// keyed by entity and one by approval, and the reviewer names ride a repeated query parameter
// rather than a joined string.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);

const requestedUrl = (): string =>
    decodeURIComponent(getAsync.mock.calls[0][0] as string);

describe('ApprovalBroker', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
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
});
