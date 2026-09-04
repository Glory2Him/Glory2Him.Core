import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ApprovalSettingBroker from './apiBroker.approvalSettings';

import {
    ApprovalSetting,
    EntityType,
    toApprovalSettingAddRequest
} from '../models/foundations/approvalSettings/approvalSetting';

// THE ADDRESSES api/ApprovalSettings ACTUALLY ANSWERS ON. None of these are visible on screen,
// and each of them fails quietly rather than loudly when it drifts: a closed row travelling back
// into the list, an amend landing on a route that does not exist, a deletion reason lost on its
// way to the audit trail.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);
const postAsync = vi.mocked(axios.post);
const putAsync = vi.mocked(axios.put);
const deleteAsync = vi.mocked(axios.delete);

const requestedUrl = (
    mock: { mock: { calls: unknown[][] } },
    callIndex = 0): string =>
    decodeURIComponent(mock.mock.calls[callIndex][0] as string);

const approvalSetting: ApprovalSetting = {
    id: '11111111-1111-1111-1111-111111111111',
    entityType: EntityType.ContentItem,
    contentType: null,
    isPersonal: null,
    requireApprovals: true,
    requiredNumberOfApprovals: 1,
    autoApproveIfAllApprovalRequirementsMet: false,
    allowSelfApproval: false,
    blockOnReject: true,
    blockOnZeroApprovalScore: false,
    requireReapprovalOnChange: true,
    requireReviewCommentResolutionBeforeApprovals: true,
    doNotAllowBypassingSettings: false,
    createdBy: 'admin',
    createdWhen: '2026-09-01T09:00:00.000+00:00',
    updatedBy: 'admin',
    updatedWhen: '2026-09-01T09:00:00.000+00:00',
    isDeleted: false
};

describe('ApprovalSettingBroker', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
        postAsync.mockResolvedValue({ data: approvalSetting } as never);
        putAsync.mockResolvedValue({ data: approvalSetting } as never);
        deleteAsync.mockResolvedValue({ data: approvalSetting } as never);
    });

    describe('reading the set', () => {
        // A SOFT-DELETED ROW IS NOT A POLICY. The delete keeps the row for its audit trail, so
        // it comes back from an unfiltered read and would sit in the table looking live.
        it('should leave closed rows behind', async () => {
            // when
            await new ApprovalSettingBroker().GetApprovalSettingsAsync();

            // then
            expect(requestedUrl(getAsync)).toContain('$filter=isDeleted eq false');
        });

        // The evaluation resolves the most specific row that applies, so the entity-type
        // defaults have to sit above the content-type rows that override them.
        it('should order the defaults above the rows that override them', async () => {
            // when
            await new ApprovalSettingBroker().GetApprovalSettingsAsync();

            // then
            expect(requestedUrl(getAsync)).toContain('$orderby=entityType,contentType');
        });
    });

    describe('creating one', () => {
        /// Asserted as the exact body: a stray `createdWhen: ''` is refused in model binding
        /// before any service sees the row, and that is precisely the regression.
        it('should post the scope and the policy and nothing about who or when', async () => {
            // given
            const addRequest = toApprovalSettingAddRequest(approvalSetting);

            // when
            await new ApprovalSettingBroker().AddApprovalSettingAsync(addRequest);

            // then
            expect(postAsync.mock.calls[0][0]).toBe('/api/approvalsettings');
            expect(postAsync.mock.calls[0][1]).toEqual(addRequest);

            for (const auditField of ['createdBy', 'createdWhen', 'updatedBy', 'updatedWhen']) {
                expect(postAsync.mock.calls[0][1]).not.toHaveProperty(auditField);
            }
        });
    });

    // THE EXPOSER ROUTES ON THE BODY'S Id, not on a path segment — a PUT to
    // /api/approvalsettings/{id} is a route that does not exist.
    describe('amending one', () => {
        it('should put to the collection rather than to the row', async () => {
            // when
            await new ApprovalSettingBroker().UpdateApprovalSettingAsync(approvalSetting);

            // then
            expect(requestedUrl(putAsync)).toBe('/api/approvalsettings');
            expect(putAsync.mock.calls[0][1]).toBe(approvalSetting);
        });

        it('should send the audit fields the foundation checks against storage', async () => {
            // when
            await new ApprovalSettingBroker().UpdateApprovalSettingAsync(approvalSetting);

            // then
            expect(putAsync.mock.calls[0][1]).toEqual(
                expect.objectContaining({
                    id: approvalSetting.id,
                    createdBy: 'admin',
                    createdWhen: '2026-09-01T09:00:00.000+00:00'
                }));
        });
    });

    describe('closing one', () => {
        it('should carry a stated reason into the audit trail', async () => {
            // when
            await new ApprovalSettingBroker().RemoveApprovalSettingByIdAsync(
                approvalSetting.id, 'Superseded by the testimony policy');

            // then
            expect(requestedUrl(deleteAsync)).toBe(
                `/api/approvalsettings/${approvalSetting.id}`
                + '?deletionReason=Superseded by the testimony policy');
        });

        // An empty reason is no reason: sending deletionReason= says one was given and lost.
        it('should ask plainly when no reason was given', async () => {
            // when
            await new ApprovalSettingBroker().RemoveApprovalSettingByIdAsync(
                approvalSetting.id, '   ');

            // then
            expect(requestedUrl(deleteAsync))
                .toBe(`/api/approvalsettings/${approvalSetting.id}`);
        });
    });
});
