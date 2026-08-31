import axios from 'axios';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import ContentItemBroker from './apiBroker.contentItems';
import { ApprovalStatus } from '../models/components/associations/associationItem';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchQuery
} from '../models/foundations/contentItems/contentItemSearchQuery';

// The one broker in the app that composes rather than concatenates, and the composition is
// exactly the kind of thing a mocked page test cannot catch: a malformed $filter is a 400 from
// the host at runtime, with a green suite behind it.
vi.mock('axios');

const getAsync = vi.mocked(axios.get);

const queryFor = (overrides: Partial<ContentItemSearchQuery> = {}): ContentItemSearchQuery => ({
    scope: 'caller',
    submittedById: null,
    approvalStatuses: null,
    searchTerm: '',
    contentType: null,
    author: '',
    pageIndex: 0,
    pageSize: 8,
    ...overrides
});

const rowsOf = (count: number): ContentItem[] =>
    Array.from({ length: count }, (_unused, index) =>
        ({ id: `content-item-${index}` } as ContentItem));

// The URL the broker asked for, with its query string decoded — OData reads far better that way,
// and encodeURIComponent is not what is under test.
const requestedUrl = (): string => decodeURIComponent(getAsync.mock.calls[0][0] as string);

const parameterOf = (name: string): string | null =>
    new URLSearchParams(requestedUrl().split('?')[1] ?? '').get(name);

describe('ContentItemBroker.SearchContentItemsAsync', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        getAsync.mockResolvedValue({ data: [] } as never);
    });

    it('should read the caller-scoped collection rather than the public one', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(queryFor());

        // then: /Public is caller-INDEPENDENT, so it would hide the caller's own drafts and
        // everything a review role covers.
        expect(requestedUrl().split('?')[0]).toBe('/api/contentitems');
    });

    // §14.1 by construction: the public route is caller-independent, so a surface built on it
    // cannot be widened by anybody's roles.
    it('should read the public route when the page scoped itself public', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(queryFor({ scope: 'public' }));

        // then
        expect(requestedUrl().split('?')[0]).toBe('/api/contentitems/Public');
    });

    // Exact, not contains: an account id is an identity, and half of one identifies nobody.
    it('should match the submitter exactly by account id', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ submittedById: 'account-1' }));

        // then
        expect(parameterOf('$filter')).toBe("createdBy eq 'account-1'");
    });

    // The member NAMES, or-chained — the same wire split ContentType has: $filter parses names,
    // JSON bodies carry numbers.
    it('should name the moderation statuses by their members', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ approvalStatuses: [ApprovalStatus.Draft, ApprovalStatus.Submitted] }));

        // then
        expect(parameterOf('$filter')).toBe(
            "(approvalStatus eq 'Draft' or approvalStatus eq 'Submitted')");
    });

    it('should leave the statuses to the read when none were pinned', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ approvalStatuses: [] }));

        // then
        expect(parameterOf('$filter')).toBeNull();
    });

    it('should ask for no filter at all when nothing was searched for', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(queryFor());

        // then
        expect(parameterOf('$filter')).toBeNull();
    });

    it('should match a term against the title, the content and the author', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ searchTerm: 'Grace' }));

        // then: lowercased on both sides, because OData has no case-insensitive contains
        expect(parameterOf('$filter')).toBe(
            "(contains(tolower(title),'grace')"
            + " or contains(tolower(content),'grace')"
            + " or contains(tolower(author),'grace'))");
    });

    // A single quote inside an OData string literal is escaped by doubling it. The term comes
    // from a free-text box, so this is the difference between a working filter and a 400.
    it('should escape a quote in the term rather than breaking the literal', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ searchTerm: "God's work" }));

        // then
        expect(parameterOf('$filter')).toContain("contains(tolower(title),'god''s work')");
    });

    // $filter parses the enum MEMBER NAME while the JSON body carries the number.
    it('should name the content type by its member rather than by its number', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ contentType: ContentType.BibleStudy }));

        // then
        expect(parameterOf('$filter')).toBe("contentType eq 'BibleStudy'");
    });

    it('should filter on the type numbered zero like any other', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ contentType: ContentType.Quote }));

        // then: Quote is 0, which a truthiness check would have dropped
        expect(parameterOf('$filter')).toBe("contentType eq 'Quote'");
    });

    it('should match the author as a substring so a surname is enough', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(queryFor({ author: 'Moody' }));

        // then
        expect(parameterOf('$filter')).toBe("contains(tolower(author),'moody')");
    });

    it('should join every filter it was given', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({
                searchTerm: 'grace',
                contentType: ContentType.Devotional,
                author: 'Vale'
            }));

        // then
        expect(parameterOf('$filter')).toBe(
            "(contains(tolower(title),'grace')"
            + " or contains(tolower(content),'grace')"
            + " or contains(tolower(author),'grace'))"
            + " and contentType eq 'Devotional'"
            + " and contains(tolower(author),'vale')");
    });

    it('should ignore a term that is nothing but whitespace', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ searchTerm: '   ', author: '  ' }));

        // then
        expect(parameterOf('$filter')).toBeNull();
    });

    // CreatedWhen rather than PublishDate: a draft has no publish date, and this read answers with
    // the caller's drafts as well as the published set.
    it('should order by when it was written, newest first', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(queryFor());

        // then
        expect(parameterOf('$orderby')).toBe('createdWhen desc');
    });

    it('should ask for one row beyond the page and skip the pages before it', async () => {
        // when
        await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ pageIndex: 3, pageSize: 8 }));

        // then
        expect(parameterOf('$skip')).toBe('24');
        expect(parameterOf('$top')).toBe('9');
    });

    // The response is a plain array with no total in it, so the extra row is the only thing that
    // separates a full last page from a page with more behind it.
    it('should drop the probe row and say there is another page', async () => {
        // given
        getAsync.mockResolvedValue({ data: rowsOf(9) } as never);

        // when
        const page = await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ pageSize: 8 }));

        // then
        expect(page.items).toHaveLength(8);
        expect(page.hasNextPage).toBe(true);
    });

    it('should say there is nothing behind a page the probe row did not fill', async () => {
        // given
        getAsync.mockResolvedValue({ data: rowsOf(8) } as never);

        // when
        const page = await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ pageSize: 8 }));

        // then
        expect(page.items).toHaveLength(8);
        expect(page.hasNextPage).toBe(false);
    });

    it('should carry the page it answered for back to the caller', async () => {
        // given
        getAsync.mockResolvedValue({ data: rowsOf(2) } as never);

        // when
        const page = await new ContentItemBroker().SearchContentItemsAsync(
            queryFor({ pageIndex: 2, pageSize: 8 }));

        // then: getNextPageParam counts on from this
        expect(page.pageIndex).toBe(2);
        expect(page.pageSize).toBe(8);
    });
});
