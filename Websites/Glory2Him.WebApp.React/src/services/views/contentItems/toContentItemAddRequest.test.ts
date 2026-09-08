import { describe, expect, it } from 'vitest';
import { ContentType } from '../../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ContentItemFormItem,
    ShareabilityBasis
} from '../../../models/components/contentItems/contentItemFormItem';

import { toContentItemAddRequest } from './toContentItemAddRequest';

const formItemWith = (
    overrides: Partial<ContentItemFormItem> = {}
): ContentItemFormItem => ({
    contentType: ContentType.Story,
    title: 'He carried me',
    author: 'A. Pilgrim',
    content: 'The whole story, as it happened.',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    sharePermission: '',
    approvalStatus: ApprovalStatus.Submitted,
    ...overrides
});

describe('toContentItemAddRequest', () => {
    it('should carry the members the API accepts from a caller', () => {
        // when
        const addRequest = toContentItemAddRequest(formItemWith());

        // then
        expect(addRequest).toEqual({
            contentType: ContentType.Story,
            title: 'He carried me',
            author: 'A. Pilgrim',
            content: 'The whole story, as it happened.',
            shareabilityBasis: ShareabilityBasis.PublicDomain,
            sharePermission: null,
            approvalStatus: ApprovalStatus.Submitted
        });
    });

    // THE BUG THIS PAIR EXISTS FOR: the status used to be left off the request altogether, so a
    // contribution filed as Submitted landed as a Draft — the server composes the row from what
    // it is sent, and what it was never sent it defaults.
    it('should carry a contribution filed for review as Submitted', () => {
        // when
        const addRequest = toContentItemAddRequest(
            formItemWith({ approvalStatus: ApprovalStatus.Submitted }));

        // then
        expect(addRequest.approvalStatus).toBe(ApprovalStatus.Submitted);
    });

    it('should carry a contribution filed as work in progress as Draft', () => {
        // when
        const addRequest = toContentItemAddRequest(
            formItemWith({ approvalStatus: ApprovalStatus.Draft }));

        // then
        expect(addRequest.approvalStatus).toBe(ApprovalStatus.Draft);
    });

    it('should ask for review where the form carried no status at all', () => {
        // given: a projection from a surface that rendered no "Submit as" row
        // when
        const addRequest = toContentItemAddRequest(
            formItemWith({ approvalStatus: undefined }));

        // then
        expect(addRequest.approvalStatus).toBe(ApprovalStatus.Submitted);
    });

    it('should send an empty optional field as null rather than as ""', () => {
        // when
        const addRequest = toContentItemAddRequest(formItemWith({
            title: '   ',
            author: '',
            sharePermission: ' By email from the author '
        }));

        // then
        expect(addRequest.title).toBeNull();
        expect(addRequest.author).toBeNull();
        expect(addRequest.sharePermission).toBe('By email from the author');
    });
});
