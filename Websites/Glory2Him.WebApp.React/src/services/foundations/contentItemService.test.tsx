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

import {
    defaultContentItemSearchApprovalStatuses,
    emptyContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

// WHAT A WRITE INVALIDATES is this service's own business, and nothing above it can catch a
// mistake here: every page test mocks this module, so a mutation that saved and told no cache
// would pass all of them while the reader watched their change revert on screen.
const putContentItemAsync = vi.fn();
const deleteContentItemByIdAsync = vi.fn();
const searchContentItemsAsync = vi.fn();

vi.mock('../../brokers/apiBroker.contentItems', () => ({
    default: class {
        PutContentItemAsync = putContentItemAsync;
        DeleteContentItemByIdAsync = deleteContentItemByIdAsync;
        SearchContentItemsAsync = searchContentItemsAsync;
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
        deleteContentItemByIdAsync.mockResolvedValue(quote);
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

    /// A TAKEDOWN LEAVES THE SAME CACHES STALE as an edit: the row is gone from every feed and
    /// queue that held it, and the round it was judged by is answering about something no
    /// longer there.
    it('should invalidate the item, its feeds and its round on a takedown', async () => {
        // given
        const { result } = renderHook(
            () => contentItemService.useRemoveContentItem(), { wrapper });

        // when
        await result.current.mutateAsync({ contentItemId: 'quote-1' });
        await waitFor(() => expect(invalidated.length).toBeGreaterThan(0));

        // then
        expect(invalidated).toContainEqual(['ContentItemsGetById', 'quote-1']);
        expect(invalidated).toContainEqual(['ContentItemsSearch']);
        expect(invalidated).toContainEqual(['ApprovalVerdict']);
    });
});


// THE STATUSES THE READ ACTUALLY ASKS FOR, which is where the search bar's checkbox group
// lands. The reader's choice where they made one; the surface's DEFAULTS where they did not —
// the same four the page hands its panel, so the boxes and the results always agree.
describe('contentItemService.useSearchContentItems approval statuses', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    const searchWith = async (
        approvalStatuses: ReadonlyArray<ApprovalStatus>,
        defaultApprovalStatuses: ReadonlyArray<ApprovalStatus> | null) => {

        renderHook(
            () => contentItemService.useSearchContentItems(
                { ...emptyContentItemSearchCriteria, approvalStatuses },
                defaultApprovalStatuses == null ? {} : { defaultApprovalStatuses }),
            { wrapper });

        await waitFor(() => expect(searchContentItemsAsync).toHaveBeenCalled());

        return searchContentItemsAsync.mock.calls[0][0].approvalStatuses;
    };

    beforeEach(() => {
        vi.clearAllMocks();

        searchContentItemsAsync.mockResolvedValue({
            items: [],
            pageIndex: 0,
            hasNextPage: false
        });

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    it('should ask for the ticked statuses', async () => {
        // given
        const ticked = [ApprovalStatus.Draft, ApprovalStatus.Rejected];

        // when
        const asked = await searchWith(ticked, null);

        // then
        expect(asked).toEqual(ticked);
    });

    // Nothing ticked is "not chosen", not "no status" — the surface's defaults are read.
    it('should ask for the surface defaults where nothing is ticked', async () => {
        // when
        const asked = await searchWith([], [ApprovalStatus.Draft, ApprovalStatus.Submitted]);

        // then
        expect(asked).toEqual([ApprovalStatus.Draft, ApprovalStatus.Submitted]);
    });

    // A page that names no defaults reads the decided rows — the panel's own resting flags —
    // so a journal that hides the boxes never receives a draft because nothing was ticked.
    it('should ask for the decided statuses where the surface named no defaults', async () => {
        // when
        const asked = await searchWith([], null);

        // then
        expect(asked).toEqual(defaultContentItemSearchApprovalStatuses);
        expect(asked).toEqual([ApprovalStatus.Approved, ApprovalStatus.Rejected]);
    });

    // The choice REPLACES the defaults; it is not intersected with them. A surface that
    // starts at the decided rows and whose reader ticks Draft is asked for the drafts.
    it('should let the ticked statuses replace the defaults outright', async () => {
        // when
        const asked = await searchWith(
            [ApprovalStatus.Draft],
            [ApprovalStatus.Approved, ApprovalStatus.Rejected]);

        // then
        expect(asked).toEqual([ApprovalStatus.Draft]);
    });
});
