import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { contentItemService } from './contentItemService';
import { ContentItem } from '../../models/foundations/contentItems/contentItem';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ApprovalStatus,
    ShareabilityBasis
} from '../../models/components/contentItems/contentItemFormItem';

// WHAT A WRITE INVALIDATES is this service's own business, and nothing above it can catch a
// mistake here: every page test mocks this module, so a mutation that saved and told no cache
// would pass all of them while the reader watched their change revert on screen.
const putContentItemAsync = vi.fn();

vi.mock('../../brokers/apiBroker.contentItems', () => ({
    default: class {
        PutContentItemAsync = putContentItemAsync;
    }
}));

const quote: ContentItem = {
    id: 'quote-1',
    contentType: ContentType.Quote,
    title: null,
    author: 'D. L. Moody',
    content: 'Character is what you are in the dark.',
    shareabilityBasis: ShareabilityBasis.PublicDomain,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: null,
    isPublished: false,
    approvalStatus: ApprovalStatus.Submitted,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'user-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'user-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
};

describe('contentItemService.useModifyContentItem', () => {
    let queryClient: QueryClient;
    let invalidated: Array<ReadonlyArray<unknown>>;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    beforeEach(() => {
        vi.clearAllMocks();
        putContentItemAsync.mockResolvedValue(quote);
        invalidated = [];

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });

        vi.spyOn(queryClient, 'invalidateQueries').mockImplementation((filters) => {
            invalidated.push((filters?.queryKey ?? []) as ReadonlyArray<unknown>);

            return Promise.resolve();
        });
    });

    const modifyAsync = async () => {
        const { result } = renderHook(
            () => contentItemService.useModifyContentItem(), { wrapper });

        await result.current.mutateAsync(quote);
        await waitFor(() => expect(invalidated.length).toBeGreaterThan(0));
    };

    /// The row the editor just closed over. Without this the read surface re-renders from the
    /// cached copy and shows the change reverting — saved, and the page says otherwise.
    it('should invalidate the item it just wrote', async () => {
        // when
        await modifyAsync();

        // then
        expect(invalidated).toContainEqual(['ContentItemsGetById', 'quote-1']);
    });

    /// Matched by PREFIX rather than reconstructed: the search key carries the criteria, and a
    /// moderation queue filtered one way and a journal filtered another both hold this row.
    it('should invalidate every feed and queue holding it', async () => {
        // when
        await modifyAsync();

        // then
        expect(invalidated).toContainEqual(['ContentItemsSearch']);
    });

    /// THE ROUND MOVES WITH THE ROW. A modify carries the Draft <-> Submitted carve-out, so a
    /// verdict fetched before it is answering about a status that no longer holds.
    it('should invalidate the approval round the row is judged by', async () => {
        // when
        await modifyAsync();

        // then
        expect(invalidated).toContainEqual(['ApprovalVerdict']);
        expect(invalidated).toContainEqual(['ApprovalReviews']);
        expect(invalidated).toContainEqual(['ReviewerCandidates']);
        expect(invalidated).toContainEqual(['ReviewRequests']);
    });

    /// A write that FAILED has changed nothing, so telling every cache to refetch would be
    /// noise — and would paper over the failure by making the page look busy.
    it('should invalidate nothing when the write fails', async () => {
        // given
        putContentItemAsync.mockRejectedValue(new Error('refused'));

        const { result } = renderHook(
            () => contentItemService.useModifyContentItem(), { wrapper });

        // when
        await expect(result.current.mutateAsync(quote)).rejects.toThrow('refused');

        // then
        expect(invalidated).toEqual([]);
    });
});
