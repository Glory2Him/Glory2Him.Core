import { useState } from 'react';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Home } from './home';
import { AuthProvider } from '../components/securitys/authProvider';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { ContentItemReactionSummary } from '../models/foundations/associations/contentItemReactionSummary';
import { ContentItem } from '../models/foundations/contentItems/contentItem';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { Reaction } from '../models/foundations/reactions/reaction';
import { ApprovalStatus } from '../models/components/contentItems/contentItemFormItem';
import { ShareabilityBasis } from '../models/components/contentItems/contentItemFormItem';
import { createAuthState, setLoading, signInAs, signOut } from '../tests/testAuth';
import { testContentItemSetting } from '../tests/testContentItemSettings';

import {
    ContentItemPage
} from '../models/foundations/contentItems/contentItemSearchQuery';

// THE FRONT PAGE'S LIKE CONTROL: what the page hands the engagement hook, and what each card
// shows and does with it. The foundation services are mocked as contentItemFeedPages.test.tsx
// mocks them; what a choice writes and when an overlay ends are the hook's own suite's.
const authState = createAuthState();

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// Every page of cards the feed holds, delivered one at a time: the page opens on the first
// `initiallyDelivered` of them, and each Load more delivers the next.
let feedPages: ContentItemPage[] = [];
let initiallyDelivered = 1;

vi.mock('../services/foundations/contentItemService', () => ({
    contentItemSearchPageSize: 8,

    contentItemService: {
        useSearchContentItems: () => {
            const [delivered, setDelivered] = useState(initiallyDelivered);

            return {
                data: { pages: feedPages.slice(0, delivered) },
                isLoading: false,
                isError: false,
                hasNextPage: delivered < feedPages.length,
                isFetchingNextPage: false,
                fetchNextPage: () => setDelivered((count) => count + 1)
            };
        }
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
    reactionFor('Love', '❤️'),
    reactionFor('Amen', '🙏'),
    reactionFor('Joy', '😊')
];

const reactionNamed = (name: string): Reaction =>
    vocabulary.find((reaction) => reaction.name === name)!;

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: vocabulary })
    }
}));

// THE SUMMARIES READ answers only for the ids it is handed, as the real one does, so a card
// whose page the hook was never handed carries no counts. Its writes stay pending for the
// length of the test, so a chosen reaction's overlay stands.
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

const devotionalSetting = testContentItemSetting(ContentType.Devotional, 'Devotional');

vi.mock('../services/foundations/contentItemSettingService', () => ({
    contentItemSettingService: {
        useGetDefaults: () => ({ data: [devotionalSetting] }),
        useGetEffectiveSettingsFor: () => ({ data: [devotionalSetting] })
    }
}));

// Somebody else's devotional: the signed-in account is always user-1.
const devotionalFor = (id: string): ContentItem => ({
    id,
    contentType: ContentType.Devotional,
    title: `Devotional ${id}`,
    author: 'Miriam Vale',
    content: 'Grace is not a one-time event.',
    shareabilityBasis: ShareabilityBasis.Owned,
    sharePermission: null,
    contentHash: `hash-${id}`,
    groupId: `group-${id}`,
    version: 1,
    publishDate: '2026-07-03T00:00:00Z',
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isApprovedByBypass: false,
    approvedByBypassReason: null,
    isDeleted: false,
    createdBy: 'contributor-9',
    createdWhen: '2026-07-01T00:00:00Z',
    updatedBy: 'contributor-9',
    updatedWhen: '2026-07-01T00:00:00Z',
    deletedBy: null,
    deletedWhen: null,
    deletionReason: null
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

const LocationProbe = () => {
    const location = useLocation();

    return <span data-testid="location">{`${location.pathname}${location.search}`}</span>;
};

const landedOn = (): string | null =>
    screen.getByTestId('location').textContent;

const renderHome = (initialUrl = '/') =>
    render(
        <MemoryRouter initialEntries={[initialUrl]}>
            <AuthProvider><Home /></AuthProvider>
            <LocationProbe />
        </MemoryRouter>);

const cardFor = (contentItemId: string): HTMLElement =>
    screen.getAllByRole('article').find((card) =>
        within(card).queryByText(`Devotional ${contentItemId}`) !== null)!;

const reactionCountsOn = (contentItemId: string): HTMLElement =>
    within(cardFor(contentItemId)).getByRole('button', { name: 'Reaction counts' });

describe('Home', () => {
    beforeEach(() => {
        feedPages = [pageOf(0, ['devotional-1', 'devotional-2'])];
        initiallyDelivered = 1;
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

    describe('the Like control', () => {
        it("should show each card's reaction counts on /", () => {
            // given
            serverSummaries = {
                'devotional-1': summaryOf('devotional-1', [['Love', 3], ['Amen', 2]]),
                'devotional-2': summaryOf('devotional-2', [['Joy', 7]])
            };

            // when
            renderHome();

            // then
            expect(handedPages).toEqual([['devotional-1', 'devotional-2']]);
            expect(reactionCountsOn('devotional-1')).toHaveTextContent('5');
            expect(reactionCountsOn('devotional-2')).toHaveTextContent('7');
        });

        // EACH PAGE AS IT WAS DELIVERED: asking for the third page's ids alone is the summaries
        // read's, one query per page it is handed, so the page hands every page it holds and the
        // first two pages' cards keep their counts.
        it('should hand the engagement hook every page delivered on /', async () => {
            // given
            feedPages = [
                pageOf(0, ['devotional-1', 'devotional-2']),
                pageOf(1, ['devotional-3']),
                pageOf(2, ['devotional-4', 'devotional-5'])
            ];

            initiallyDelivered = 2;

            // Without an IntersectionObserver the list offers Load more, a press the test can make.
            vi.stubGlobal('IntersectionObserver', undefined);

            serverSummaries = {
                'devotional-1': summaryOf('devotional-1', [['Love', 1]]),
                'devotional-2': summaryOf('devotional-2', [['Amen', 2]]),
                'devotional-3': summaryOf('devotional-3', [['Joy', 3]]),
                'devotional-4': summaryOf('devotional-4', [['Love', 4]]),
                'devotional-5': summaryOf('devotional-5', [['Amen', 5], ['Joy', 1]])
            };

            renderHome();

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
