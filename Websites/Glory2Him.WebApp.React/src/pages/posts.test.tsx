import { useState } from 'react';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Posts } from './posts';
import { AuthProvider } from '../components/securitys/authProvider';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { ContentItemReactionSummary } from '../models/foundations/associations/contentItemReactionSummary';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentItemSetting } from '../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { Reaction } from '../models/foundations/reactions/reaction';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, setLoading, signInAs, signOut } from '../tests/testAuth';

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

// The pages the read holds but has not delivered yet: each next page request delivers the first
// of them.
let undeliveredPages: ContentItemPage[] = [];

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: (
            criteria: ContentItemSearchCriteria,
            options: Record<string, unknown>) => {
            const [, setDeliveries] = useState(0);
            searchedCriteria = criteria;
            searchedOptions = options;

            return {
                data: { pages },
                isLoading,
                isError,
                hasNextPage,
                isFetchingNextPage,
                fetchNextPage: () => {
                    fetchNextPage();

                    if (undeliveredPages.length > 0) {
                        pages = [...pages, undeliveredPages[0]];
                        undeliveredPages = undeliveredPages.slice(1);
                        hasNextPage = undeliveredPages.length > 0;
                        setDeliveries((count) => count + 1);
                    }
                }
            };
        }
    }
}));

// The reader's sign-in state, flipped per test; every test starts signed out.
const authState = createAuthState();

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

const reactionFor = (name: string, unicodeEmoji: string): Reaction => ({
    id: `reaction-${name.toLowerCase()}`,
    name,
    unicodeEmoji,
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
});

const vocabulary: ReadonlyArray<Reaction> = [
    reactionFor('Amen', '👍'),
    reactionFor('Love', '❤️'),
    reactionFor('Joy', '😊')
];

const reactionNamed = (name: string): Reaction =>
    vocabulary.find((reaction) => reaction.name === name)!;

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: vocabulary })
    }
}));

// The engagement hook reads the cards' reaction summaries through a query, which a harness with
// no QueryClientProvider cannot hold, so it is mocked. It answers only for the ids it is handed,
// as the real one does, so a card whose page the hook was never handed carries no counts. Its
// writes are mutations, mocked for the same reason, and a write made through them stays pending
// for the length of the test, so a chosen reaction's overlay stands.
let handedPages: ReadonlyArray<ReadonlyArray<string>> | undefined;
let serverSummaries: Record<string, ContentItemReactionSummary> = {};
const upsertAssociation = vi.fn<(association: AssociationRequest) => Promise<unknown>>();
const removeAssociationByPair = vi.fn<(association: AssociationRequest) => Promise<unknown>>();

vi.mock('../services/foundations/associationService', () => ({
    associationService: {
        useGetReactionSummaries: (contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>) => {
            handedPages = contentItemIdPages;

            return {
                summaries: Object.fromEntries(contentItemIdPages
                    .flat()
                    .filter((contentItemId) => serverSummaries[contentItemId] !== undefined)
                    .map((contentItemId) => [contentItemId, serverSummaries[contentItemId]])),
                isLoading: false,
                isError: false
            };
        },

        useUpsertAssociation: () => ({ mutateAsync: upsertAssociation }),
        useRemoveAssociationByPair: () => ({ mutateAsync: removeAssociationByPair }),
        useReadReactionSummariesAgain: () => () => new Promise(() => undefined)
    }
}));

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: settings }),
        useGetEffectiveSettingsFor: () => ({ data: settings })
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

const LocationProbe = () => {
    const location = useLocation();

    return <span data-testid="location">{`${location.pathname}${location.search}`}</span>;
};

const landedOn = (): string | null =>
    screen.getByTestId('location').textContent;

const renderPosts = (initialUrl = '/posts') =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <AuthProvider><Posts /></AuthProvider>
            <LocationProbe />
        </MemoryRouter>);

// Somebody else's devotional: the signed-in account is always user-1.
const devotionalFor = (id: string): ContentItem =>
    contentItemFor({
        id,
        title: `Devotional ${id}`,
        createdBy: 'contributor-9',
        updatedBy: 'contributor-9'
    });

const pageOf = (pageIndex: number, ids: ReadonlyArray<string>): ContentItemPage => ({
    items: ids.map(devotionalFor),
    pageIndex,
    pageSize: 8,
    hasNextPage: false
});

// One item's summary as the server sends it: each reaction given and its count, and the one
// the reader holds, if any.
const summaryOf = (
    contentItemId: string,
    counts: ReadonlyArray<[string, number]>,
    viewerReactionName: string | null = null): ContentItemReactionSummary => ({
    contentItemId,
    reactions: counts.map(([name, count]) => ({
        reactionId: reactionNamed(name).id,
        name,
        unicodeEmoji: reactionNamed(name).unicodeEmoji,
        count
    })),
    viewerReactionId: viewerReactionName === null ? null : reactionNamed(viewerReactionName).id,
    viewerReactionName
});

const cardFor = (contentItemId: string): HTMLElement =>
    screen.getAllByRole('article').find((card) =>
        within(card).queryByText(`Devotional ${contentItemId}`) !== null)!;

const reactionCountsOn = (contentItemId: string): HTMLElement =>
    within(cardFor(contentItemId)).getByRole('button', { name: 'Reaction counts' });

// One reaction's own count on a card, read from the counts' expanded face, which lists each
// reaction given beside its glyph; the collapsed face shows only their sum.
const reactionCountOn = async (contentItemId: string, reactionName: string): Promise<string> => {
    const reactionCounts = reactionCountsOn(contentItemId);

    if (reactionCounts.getAttribute('aria-expanded') !== 'true') {
        await userEvent.click(reactionCounts);
    }

    const reaction = within(reactionCountsOn(contentItemId)).getByTitle(reactionName);

    return (reaction.textContent ?? '').replace(reactionNamed(reactionName).unicodeEmoji, '').trim();
};

// Opens a card's Like control: the reactions it offers, each pressed or not.
const openLikeOn = async (contentItemId: string): Promise<void> =>
    await userEvent.click(within(cardFor(contentItemId)).getByRole('button', { name: /Like/ }));

const chooseOn = async (contentItemId: string, reactionName: string): Promise<void> => {
    await openLikeOn(contentItemId);
    await userEvent.click(within(cardFor(contentItemId)).getByRole('menuitem', { name: reactionName }));
};

const reactionPairFor = (contentItemId: string, reactionName: string): AssociationRequest => ({
    entityAType: EntityType.ContentItem,
    entityAKeyId: contentItemId,
    entityBType: EntityType.Reaction,
    entityBKeyId: reactionNamed(reactionName).id
});

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
        undeliveredPages = [];
        handedPages = undefined;
        serverSummaries = {};
        upsertAssociation.mockReset();
        upsertAssociation.mockImplementation(() => new Promise(() => undefined));
        removeAssociationByPair.mockReset();
        removeAssociationByPair.mockImplementation(() => new Promise(() => undefined));
        signOut(authState);
    });

    afterEach(() => {
        vi.unstubAllGlobals();
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

    // A card claims no figure it does not have — the comment READS are blocked on #318 — but
    // the way into the comments still renders, uncounted.
    it('should offer the comments control uncounted rather than with an invented figure', () => {
        // when
        renderPosts();

        // then
        expect(screen.getAllByRole('button', { name: 'Comments' }).length)
            .toBeGreaterThan(0);

        expect(screen.queryByText(/\d+ comments/)).not.toBeInTheDocument();
    });

    // The Like control renders with the REAL vocabulary — the page pulls the approved
    // reactions, and the write behind the picker is useContentItemEngagement's, which records
    // the reader's choice (#739).
    it('should offer the Like control fed by the approved vocabulary', async () => {
        // given
        pages = onePage([contentItemFor({
            id: 'quote-1',
            contentType: ContentType.Quote,
            title: null,
            content: 'Character is what you are in the dark.'
        })]);

        renderPosts();

        // when
        await userEvent.click(screen.getByRole('button', { name: /Like/ }));

        // then
        expect(screen.getByRole('menuitem', { name: 'Amen' })).toBeInTheDocument();
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
            tags: [],
            tagMatchMode: 'any',
            bibleReferences: [],
            bibleReferenceMatchMode: 'any',
            shareabilityBasis: null,
            approvalStatuses: []
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
            tags: [],
            tagMatchMode: 'any',
            bibleReferences: [],
            bibleReferenceMatchMode: 'any',
            shareabilityBasis: null,
            approvalStatuses: []
        }));
    });

    // The clicked filters commit into the URL — id and name both — so a narrowed list is
    // shareable and the back button un-narrows it.
    it('should read a clicked submitted-by filter back off the url', async () => {
        // when
        renderPosts('/posts?by=account-1&byName=Joan');

        // then
        expect(searchedCriteria).toEqual(
            expect.objectContaining({
                submittedBy: { id: 'account-1', name: 'Joan' }
            }));

        // The filter lands in its box — the advanced options are where a filter shows now.
        await userEvent.click(
            screen.getByRole('button', { name: 'Advanced search options' }));

        expect(screen.getByLabelText('Submitted by')).toHaveValue('Joan');
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

    describe('the Like control', () => {
        it("should show each card's reaction counts on /posts", () => {
            // given
            pages = [pageOf(0, ['devotional-1', 'devotional-2'])];

            serverSummaries = {
                'devotional-1': summaryOf('devotional-1', [['Love', 3], ['Amen', 2]]),
                'devotional-2': summaryOf('devotional-2', [['Joy', 7]])
            };

            // when
            renderPosts();

            // then
            expect(handedPages).toEqual([['devotional-1', 'devotional-2']]);
            expect(reactionCountsOn('devotional-1')).toHaveTextContent('5');
            expect(reactionCountsOn('devotional-2')).toHaveTextContent('7');
        });

        // EACH PAGE AS IT WAS DELIVERED: asking for the third page's ids alone is the summaries
        // read's, one query per page it is handed, so the page hands every page it holds and the
        // first two pages' cards keep their counts.
        it('should hand the engagement hook every page delivered on /posts', async () => {
            // given
            pages = [pageOf(0, ['devotional-1', 'devotional-2']), pageOf(1, ['devotional-3'])];
            undeliveredPages = [pageOf(2, ['devotional-4', 'devotional-5'])];
            hasNextPage = true;

            // Without an IntersectionObserver the list offers Load more, a press the test can make.
            vi.stubGlobal('IntersectionObserver', undefined);

            serverSummaries = {
                'devotional-1': summaryOf('devotional-1', [['Love', 1]]),
                'devotional-2': summaryOf('devotional-2', [['Amen', 2]]),
                'devotional-3': summaryOf('devotional-3', [['Joy', 3]]),
                'devotional-4': summaryOf('devotional-4', [['Love', 4]]),
                'devotional-5': summaryOf('devotional-5', [['Amen', 5], ['Joy', 1]])
            };

            renderPosts();

            // when
            await userEvent.click(screen.getByRole('button', { name: 'Load more' }));

            // then
            expect(handedPages).toEqual([
                ['devotional-1', 'devotional-2'],
                ['devotional-3'],
                ['devotional-4', 'devotional-5']
            ]);

            expect(reactionCountsOn('devotional-1')).toHaveTextContent('1');
            expect(reactionCountsOn('devotional-2')).toHaveTextContent('2');
            expect(reactionCountsOn('devotional-3')).toHaveTextContent('3');
            expect(reactionCountsOn('devotional-4')).toHaveTextContent('4');
            expect(reactionCountsOn('devotional-5')).toHaveTextContent('6');
        });
    });
});
