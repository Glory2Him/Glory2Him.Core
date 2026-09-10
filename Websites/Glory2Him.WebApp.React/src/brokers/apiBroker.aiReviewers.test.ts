import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import AIReviewerBroker from './apiBroker.aiReviewers';
import { EntityTypeName } from '../models/foundations/approvals/approval';

// Berean's own resource (design §8.6.2). What matters here is the ADDRESS: api/AIReviewers is a
// resource in its own right, NOT a sub-resource of the approval round — the endpoints moved off
// api/Approvals/{entityType}/{entityId}/AIReviewer when the AI workflow got its own controller,
// and this suite is what pins them there.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);
const postAsync = vi.mocked(axios.post);
const deleteAsync = vi.mocked(axios.delete);

const requestedUrl = (): string =>
    decodeURIComponent(getAsync.mock.calls[0][0] as string);

describe('AIReviewerBroker', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: {} } as never);
        postAsync.mockResolvedValue({ data: {} } as never);
        deleteAsync.mockResolvedValue({ data: {} } as never);
    });

    /// ONE RESOURCE, THREE VERBS, and no reviewer id anywhere in the address. A person's review
    /// request is named by the account it was addressed to; Berean is not a role-bearing
    /// identity and has no account to name, so the ENTITY is the whole key and the verb is what
    /// says whether to read, assign or withdraw.
    it('should read, assign and withdraw the AI reviewer at one entity-keyed address',
        async () => {
            // when
            const broker = new AIReviewerBroker();
            await broker.GetAIReviewerStatusAsync(EntityTypeName.ContentItem, 'item-1');
            await broker.PostAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');
            await broker.DeleteAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');

            // then: the member NAME, so a network log says what the request is about
            const aiReviewerUrl = '/api/aiReviewers/ContentItem/item-1';

            expect(requestedUrl()).toBe(aiReviewerUrl);
            expect(postAsync.mock.calls[0][0]).toBe(aiReviewerUrl);
            expect(deleteAsync.mock.calls[0][0]).toBe(aiReviewerUrl);
        });

    /// THE ROUND IS NOT IN THE ADDRESS ANY MORE. The old form hung Berean off the approval
    /// round's own resource, which is what gave one controller two contracts — so this asserts
    /// the absence rather than trusting the string above to notice a half-done move.
    it('should not address the AI reviewer through the approval round', async () => {
        // when
        const broker = new AIReviewerBroker();
        await broker.GetAIReviewerStatusAsync(EntityTypeName.ContentItem, 'item-1');
        await broker.PostAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');
        await broker.DeleteAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        for (const url of [
            requestedUrl(),
            postAsync.mock.calls[0][0] as string,
            deleteAsync.mock.calls[0][0] as string
        ]) {
            expect(url.toLowerCase()).not.toContain('/api/approvals');
        }
    });

    /// THE UPSERT CARRIES NOTHING. Absent creates a pending row, a completed one resets to
    /// pending, a still-pending one is a no-op — the entity key is the whole request, and there
    /// is no parameter that could vary it. So the body is the empty object the host binds
    /// nothing from, exactly as the round's reset and decision send.
    it('should assign the AI reviewer with an empty body', async () => {
        // when
        await new AIReviewerBroker().PostAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        expect(postAsync.mock.calls[0][1]).toEqual({});
    });

    /// NOTHING STANDING ANSWERS 204, and axios materialises that by handing the default
    /// transform an empty body — which it cannot parse as JSON and gives back VERBATIM. So the
    /// broker's guard has to recognise the empty string as well as the absent ones: `?? null`
    /// alone resolved '', quietly contradicting the nullable this method promises and leaving a
    /// falsy value a consumer would have to know to test for.
    it.each([
        '',
        undefined,
        null
    ])('should answer nothing when the withdrawal removed nothing (%j)', async (emptyBody) => {
        // given
        deleteAsync.mockResolvedValue({ data: emptyBody } as never);

        // when
        const assignment = await new AIReviewerBroker()
            .DeleteAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        expect(assignment).toBeNull();
    });

    /// ...and the row itself when one WAS removed, so the guard above narrows the empty answers
    /// without swallowing the real one.
    it('should answer with the assignment the withdrawal removed', async () => {
        // given
        const removedAssignment = {
            id: '22222222-2222-2222-2222-222222222222',
            approvalId: 'approval-1',
            isAIReviewCompleted: false,
            isAIReviewCommentsPresent: false,
            isDeleted: true
        };

        deleteAsync.mockResolvedValue({ data: removedAssignment } as never);

        // when
        const assignment = await new AIReviewerBroker()
            .DeleteAIReviewerAsync(EntityTypeName.ContentItem, 'item-1');

        // then
        expect(assignment).toEqual(removedAssignment);
    });
});
