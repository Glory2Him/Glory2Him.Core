import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Posts } from './posts';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentItemSetting } from '../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';

import {
    ContentItemPage
} from '../models/foundations/contentItems/contentItemSearchQuery';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

// What the PAGE owns is everything the family does not: which read feeds it (the scope and the
// pins), the paging over that read, the projection of its rows, the criteria in the URL and the
// redirects. The service is mocked at its own boundary so each is asserted directly.
const fetchNextPage = vi.fn();
let searchedCriteria: ContentItemSearchCriteria | null = null;
let searchedOptions: Record<string, unknown> | null = null;
let pages: ContentItemPage[] = [];
let isLoading = false;
let isError = false;
let hasNextPage = false;
let isFetchingNextPage = false;

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: (
            criteria: ContentItemSearchCriteria,
            options: Record<string, unknown>) => {
            searchedCriteria = criteria;
            searchedOptions = options;

            return {
                data: { pages },
                isLoading,
                isError,
                hasNextPage,
                isFetchingNextPage,
                fetchNextPage
            };
        }
    }
}));

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: settings })
    }
}));

const settingFor = (
    contentType: ContentType,
    contentTypeName: string,
    overrides: Partial<ContentItemSetting> = {}): ContentItemSetting => ({
        id: `setting-${contentType}`,
        contentType,
        contentItemId: null,
        contentTypeName,
        contentTypeDescription: contentTypeName,
        contentTypeIconCssClass: 'bi-quote',
        sortOrder: contentType,
        hasTitle: contentType !== ContentType.Quote,
        hasAuthor: true,
        isAvailableAsGeneralUserContribution: true,
        tagsAllowed: true,
        showTags: true,
        reactionsAllowed: true,
        showReactions: true,
        linksAllowed: false,
        showLinks: false,
        attachmentsAllowed: false,
        showAttachments: false,
        commentsAllowed: true,
        showComments: true,
        bibleReferenceAllowed: true,
        showBibleReferences: true,
        limitReactionsToLoveOnly: false,
        createdBy: 'system-seed',
        createdWhen: '2026-01-01T00:00:00Z',
        updatedBy: 'system-seed',
        updatedWhen: '2026-01-01T00:00:00Z',
        deletedBy: null,
        deletedWhen: null,
        isDeleted: false,
        deletionReason: null,
        ...overrides
    });

const settings: ContentItemSetting[] = [
    settingFor(ContentType.Quote, 'Quote'),
    settingFor(ContentType.Devotional, 'Devotional')
];

const contentItemFor = (overrides: Partial<ContentItem> = {}): ContentItem => ({
    id: 'devotional-1',
    contentType: ContentType.Devotional,
    title: 'Grace for the ordinary Tuesday',
    author: 'Miriam Vale',
    content: 'Grace is not a one-time event but the daily air the believer breathes.',
    shareabilityBasis: ShareabilityBasis.Owned,
    sharePermission: null,
    contentHash: 'hash-1',
    groupId: 'group-1',
    version: 1,
    publishDate: '2026-07-03T00:00:00Z',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'account-1',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'account-1',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null,
    ...overrides
});

const onePage = (items: ContentItem[]): ContentItemPage[] =>
    [{ items, pageIndex: 0, pageSize: 8, hasNextPage: false }];

const renderPosts = (initialUrl = '/posts') =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <Posts />
        </MemoryRouter>);

describe('Posts', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        searchedCriteria = null;
        searchedOptions = null;
        pages = onePage([contentItemFor()]);
        isLoading = false;
        isError = false;
        hasNextPage = false;
        isFetchingNextPage = false;
    });

    it('should render the journal and a way into the contribution form', () => {
        // when
        renderPosts();

        // then
        expect(screen.getByRole('heading', { name: 'The journal', level: 1 }))
            .toBeInTheDocument();

        expect(screen.getByRole('link', { name: /Share what He has done/ }))
            .toHaveAttribute('href', '/posts/contribute');
    });

    // The caller-scoped read is what separates this surface from the home feed.
    it('should feed the panel from the caller-scoped read', () => {
        // when
        renderPosts();

        // then
        expect(searchedOptions).toEqual(expect.objectContaining({ scope: 'caller' }));
    });

    it('should project each row onto the card the panel renders', () => {
        // when
        renderPosts();

        // then: the title is an EVENT, not a link — the page owns the redirect
        expect(screen.getByRole('button', { name: 'Grace for the ordinary Tuesday' }))
            .toBeInTheDocument();

        expect(screen.getByRole('button', { name: /Author/ }))
            .toHaveTextContent('Miriam Vale');
    });

    // A card carries no figure it does not have: comments and reactions are association reads
    // the host does not expose yet (#318).
    it('should claim no engagement figures the api cannot answer', () => {
        // when
        renderPosts();

        // then
        expect(screen.queryByText(/comments/)).not.toBeInTheDocument();
    });

    // Giving a reaction is an association too, and a surface that cannot persist one must not
    // appear to accept one — no reactionOptions and no onReactionSelected are passed.
    it('should offer no Like it could not persist', () => {
        // given
        pages = onePage([contentItemFor({
            id: 'quote-1',
            contentType: ContentType.Quote,
            title: null,
            content: 'Character is what you are in the dark.'
        })]);

        // when
        renderPosts();

        // then
        expect(screen.getByText(new RegExp('Character is what you are')))
            .toBeInTheDocument();

        expect(screen.queryByRole('button', { name: /Like/ })).not.toBeInTheDocument();
    });

    it('should read the criteria off the url so a shared link lands on the results', () => {
        // when
        renderPosts('/posts?q=grace&type=Devotional&author=Vale');

        // then
        expect(searchedCriteria).toEqual({
            query: 'grace',
            contentType: ContentType.Devotional,
            author: 'Vale',
            submittedBy: null,
            tag: null
        });
    });

    // The url carries the member NAME, not the number: a link reading ?type=Devotional survives
    // somebody reading it.
    it('should put what was searched for back into the url', async () => {
        // given
        renderPosts();

        // when
        await userEvent.type(screen.getByRole('searchbox'), 'grace');
        await userEvent.click(screen.getByRole('button', { name: 'Advanced search options' }));

        await userEvent.selectOptions(
            screen.getByLabelText('Category'), String(ContentType.Devotional));

        await userEvent.click(screen.getByRole('button', { name: /Search/ }));

        // then
        await waitFor(() => expect(searchedCriteria).toEqual({
            query: 'grace',
            contentType: ContentType.Devotional,
            author: '',
            submittedBy: null,
            tag: null
        }));
    });

    // The clicked filters commit into the URL — id and name both — so a narrowed list is
    // shareable and the back button un-narrows it.
    it('should read a clicked submitted-by filter back off the url', () => {
        // when
        renderPosts('/posts?by=account-1&byName=Joan');

        // then
        expect(searchedCriteria).toEqual(
            expect.objectContaining({
                submittedBy: { id: 'account-1', name: 'Joan' }
            }));

        expect(screen.getByRole('button', { name: /Submitted by Joan/ }))
            .toBeInTheDocument();
    });

    it('should ignore a content type the url does not actually name', () => {
        // when
        renderPosts('/posts?type=NotAContentType');

        // then
        expect(searchedCriteria?.contentType).toBeNull();
    });

    it('should accumulate the pages rather than showing only the last', () => {
        // given
        pages = [
            {
                items: [contentItemFor()],
                pageIndex: 0,
                pageSize: 8,
                hasNextPage: true
            },
            {
                items: [contentItemFor({ id: 'devotional-2', title: 'When the answer is wait' })],
                pageIndex: 1,
                pageSize: 8,
                hasNextPage: false
            }
        ];

        // when
        renderPosts();

        // then
        expect(screen.getByRole('button', { name: 'Grace for the ordinary Tuesday' }))
            .toBeInTheDocument();

        expect(screen.getByRole('button', { name: 'When the answer is wait' }))
            .toBeInTheDocument();
    });

    it('should hand the next page request straight to the query', async () => {
        // given
        vi.stubGlobal('IntersectionObserver', undefined);
        hasNextPage = true;

        renderPosts();

        // when
        await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

        // then
        expect(fetchNextPage).toHaveBeenCalledTimes(1);

        vi.unstubAllGlobals();
    });

    it('should say so rather than showing an empty journal when the read fails', () => {
        // given
        isError = true;
        pages = [];

        // when
        renderPosts();

        // then
        expect(screen.getByRole('alert')).toHaveTextContent(/could not load the journal/);
        expect(screen.queryByRole('searchbox')).not.toBeInTheDocument();
    });
});
