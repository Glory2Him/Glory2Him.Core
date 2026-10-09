import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter, useLocation } from 'react-router-dom';
import { act, renderHook, RenderHookResult, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useContentItemEngagement } from './useContentItemEngagement';
import { AuthProvider } from '../components/securitys/authProvider';
import { ApprovalStatus } from '../models/components/associations/associationItem';
import { ContentItemSearchItem } from '../models/components/contentItems/contentItemSearchItem';
import { EntityType } from '../models/foundations/approvalSettings/approvalSetting';
import { AssociationRequest } from '../models/foundations/associations/associationRequest';
import { ContentItemReactionSummary } from '../models/foundations/associations/contentItemReactionSummary';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';
import { Reaction } from '../models/foundations/reactions/reaction';
import { createAuthState, signInAs } from '../tests/testAuth';

import {
    AssociationSuggestionResult,
    AssociationSuggestionStatus
} from '../models/foundations/associations/associationSuggestionResult';

// WHAT A CHOICE WRITES, AND WHAT THE CARD SHOWS UNTIL THE SERVER ANSWERS, is this hook's own
// business. Its #738 tests replace associationService wholesale, which models no mutation, no
// promise per call and no read that lands, so this suite mocks the BROKER beneath the real
// service instead, over a real QueryClient: what the tests meet is TanStack Query's own
// behaviour, and each write is observed where it leaves.
const postAssociationAsync = vi.fn<(association: AssociationRequest) => Promise<AssociationSuggestionResult>>();
const deleteAssociationPairAsync = vi.fn<(association: AssociationRequest) => Promise<void>>();

const getReactionSummariesAsync =
    vi.fn<(contentItemIds: ReadonlyArray<string>) => Promise<ContentItemReactionSummary[]>>();

vi.mock('../brokers/apiBroker.associations', () => ({
    default: class {
        PostAssociationAsync = postAssociationAsync;
        DeleteAssociationPairAsync = deleteAssociationPairAsync;
        GetReactionSummariesAsync = getReactionSummariesAsync;
    }
}));

const authState = createAuthState();

vi.mock('../services/foundations/accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

vi.mock('../brokers/toastBroker.error', () => ({
    toastError: vi.fn()
}));

vi.mock('../brokers/toastBroker.success', () => ({
    toastSuccess: vi.fn()
}));

const { toastError } = await import('../brokers/toastBroker.error');
const { toastSuccess } = await import('../brokers/toastBroker.success');

const reactionFor = (name: string, unicodeEmoji: string): Reaction => ({
    id: `reaction-${name.toLowerCase()}`,
    name,
    unicodeEmoji,
    isPublished: true,
    approvalStatus: ApprovalStatus.Approved,
    isDeleted: false
});

// The vocabulary's order is neither a summary's order nor the alphabet's, so counts placed by
// either would be told apart from counts placed by the options.
const vocabulary: ReadonlyArray<Reaction> = [
    reactionFor('Joy', '😊'),
    reactionFor('Love', '❤️'),
    reactionFor('Amen', '🙏'),
    reactionFor('Moved', '🥹')
];

vi.mock('../services/foundations/reactionService', () => ({
    reactionService: {
        useGetApprovedReactions: () => ({ data: vocabulary })
    }
}));

const reactionNamed = (name: string): Reaction =>
    vocabulary.find((reaction) => reaction.name === name)!;

// One item's summary as the server sends it: each reaction given and its count, in the order
// listed, and the one the reader holds, if any.
const summaryOf = (
    contentItemId: string,
    counts: ReadonlyArray<[string, number]>,
    viewerReactionName: string | null): ContentItemReactionSummary => ({
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

// What the server answers each summary read with, by item, until a test holds a read open.
let serverSummaries: Record<string, ContentItemReactionSummary> = {};

const answerFromTheServer = async (contentItemIds: ReadonlyArray<string>) =>
    contentItemIds.flatMap((contentItemId) =>
        serverSummaries[contentItemId] === undefined ? [] : [serverSummaries[contentItemId]]);

type Settle<T> = { resolve: (value: T) => void; reject: (reason: unknown) => void };

// Holds every call of a broker method open, in call order, so each can be settled when the test
// says.
const holdEachCall = <T,>(brokerMethod: { mockImplementation: (implementation: () => Promise<T>) => unknown }) => {
    const calls: Settle<T>[] = [];

    brokerMethod.mockImplementation(() => new Promise<T>((resolve, reject) => {
        calls.push({ resolve, reject });
    }));

    return calls;
};

const createdResult: AssociationSuggestionResult = {
    status: AssociationSuggestionStatus.Created,
    associationId: 'association-1'
};

const requestFor = (contentItemId: string, reactionName: string): AssociationRequest => ({
    entityAType: EntityType.ContentItem,
    entityAKeyId: contentItemId,
    entityBType: EntityType.Reaction,
    entityBKeyId: reactionNamed(reactionName).id
});

const cardFor = (id: string): ContentItemSearchItem => ({
    id,
    contentType: ContentType.Quote,
    content: `The words of ${id}.`
});

type Engagement = {
    engagement: ReturnType<typeof useContentItemEngagement>;
    location: ReturnType<typeof useLocation>;
};

type EngagementRender = RenderHookResult<Engagement, unknown>;

describe('useContentItemEngagement.onReactionSelected', () => {
    let queryClient: QueryClient;

    const renderEngagement = (
        contentItemIdPages: ReadonlyArray<ReadonlyArray<string>> = [['item-1']],
        initialEntry = '/posts'): EngagementRender => {
        const wrapper = ({ children }: { children: ReactNode }) => (
            <QueryClientProvider client={queryClient}>
                <MemoryRouter initialEntries={[initialEntry]}>
                    <AuthProvider>{children}</AuthProvider>
                </MemoryRouter>
            </QueryClientProvider>
        );

        return renderHook(
            () => ({
                engagement: useContentItemEngagement(contentItemIdPages),
                location: useLocation()
            }),
            { wrapper });
    };

    // The item as the page hands it to the card: through withReactions.
    const shown = (render: EngagementRender, contentItemId: string): ContentItemSearchItem =>
        render.result.current.engagement.withReactions([cardFor(contentItemId)])[0];

    const choose = (render: EngagementRender, contentItemId: string, reactionName: string): void => {
        const option = render.result.current.engagement.reactionOptions
            .find((reactionOption) => reactionOption.label === reactionName)!;

        act(() => render.result.current.engagement.onReactionSelected(
            shown(render, contentItemId), option));
    };

    const waitForTheRead = async (render: EngagementRender, contentItemId: string): Promise<void> => {
        await waitFor(() =>
            expect(shown(render, contentItemId).reactionSummary).toBeDefined());
    };

    // Lets every promise the hook and the query client chain run, and any timer they set.
    const settleEverything = async (): Promise<void> => {
        await act(async () => {
            await new Promise((resolve) => setTimeout(resolve, 0));
        });
    };

    beforeEach(() => {
        vi.clearAllMocks();
        signInAs(authState);
        serverSummaries = {};
        getReactionSummariesAsync.mockImplementation(answerFromTheServer);
        postAssociationAsync.mockImplementation(() => new Promise(() => undefined));
        deleteAssociationPairAsync.mockImplementation(() => new Promise(() => undefined));

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    afterEach(() => {
        queryClient.clear();
    });

    describe('a signed-in reader', () => {
        it('should give the reaction a reader chooses', async () => {
            // given
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], null) };
            const render = renderEngagement();
            await waitForTheRead(render, 'item-1');

            // when
            choose(render, 'item-1', 'Love');

            // then
            await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Love'));
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
        });

        // The server changes the reaction a reader holds when it is given another: the page
        // asks for the new one and never withdraws the old one first.
        it('should change the reaction a reader holds to the one they choose', async () => {
            // given
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], 'Joy') };
            const render = renderEngagement();
            await waitForTheRead(render, 'item-1');

            // when
            choose(render, 'item-1', 'Love');

            // then
            await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Love'));
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
        });

        it('should withdraw the reaction a reader chooses again', async () => {
            // given
            serverSummaries = { 'item-1': summaryOf('item-1', [['Love', 3]], 'Love') };
            const render = renderEngagement();
            await waitForTheRead(render, 'item-1');

            // when
            choose(render, 'item-1', 'Love');

            // then
            await waitFor(() => expect(deleteAssociationPairAsync).toHaveBeenCalledTimes(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Love'));
            await settleEverything();
            expect(postAssociationAsync).not.toHaveBeenCalled();
        });

        // Every write here stays pending, so what the item shows is the overlay alone. Each
        // summary lists its reactions out of the vocabulary's order, so a reaction added where
        // the summary or the alphabet would put it is told apart from one the options place.
        it('should mark the choice and move the counts at once', async () => {
            const choices: ReadonlyArray<{
                summary: ContentItemReactionSummary;
                chosen: string;
                expectedViewerReactionLabel: string | undefined;
                expectedReactionSummary: ReadonlyArray<{ label: string; glyph: string; count: number }>;
            }> = [
                {
                    // give
                    summary: summaryOf('item-1', [['Love', 3]], null),
                    chosen: 'Joy',
                    expectedViewerReactionLabel: 'Joy',
                    expectedReactionSummary: [
                        { label: 'Joy', glyph: '😊', count: 1 },
                        { label: 'Love', glyph: '❤️', count: 3 }
                    ]
                },
                {
                    // change
                    summary: summaryOf('item-1', [['Love', 3], ['Joy', 2]], 'Joy'),
                    chosen: 'Amen',
                    expectedViewerReactionLabel: 'Amen',
                    expectedReactionSummary: [
                        { label: 'Love', glyph: '❤️', count: 3 },
                        { label: 'Joy', glyph: '😊', count: 1 },
                        { label: 'Amen', glyph: '🙏', count: 1 }
                    ]
                },
                {
                    // withdraw
                    summary: summaryOf('item-1', [['Love', 3], ['Joy', 1]], 'Joy'),
                    chosen: 'Joy',
                    expectedViewerReactionLabel: undefined,
                    expectedReactionSummary: [{ label: 'Love', glyph: '❤️', count: 3 }]
                }
            ];

            for (const choice of choices) {
                // given
                queryClient.clear();
                serverSummaries = { 'item-1': choice.summary };
                const render = renderEngagement();
                await waitForTheRead(render, 'item-1');

                // when
                choose(render, 'item-1', choice.chosen);

                // then
                const item = shown(render, 'item-1');
                expect(item.viewerReactionLabel).toBe(choice.expectedViewerReactionLabel);
                expect(item.reactionSummary).toStrictEqual(choice.expectedReactionSummary);

                render.unmount();
            }
        });

        // Read against the last read instead, the second press would give Love again, or show
        // Joy back at 2 beside a Love of 1: a press of Love from Joy moves both.
        it('should read a second press against the overlay the first one left', async () => {
            // given
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], 'Joy') };
            const upserts = holdEachCall(postAssociationAsync);
            const render = renderEngagement();
            await waitForTheRead(render, 'item-1');

            // when
            choose(render, 'item-1', 'Love');
            choose(render, 'item-1', 'Love');

            // then, while the first write is still pending
            await waitFor(() => expect(upserts).toHaveLength(1));
            const item = shown(render, 'item-1');
            expect(item.viewerReactionLabel).toBeUndefined();
            expect(item.reactionSummary).toStrictEqual([{ label: 'Joy', glyph: '😊', count: 1 }]);

            // and once it succeeds
            await act(async () => upserts[0].resolve(createdResult));

            await waitFor(() => expect(deleteAssociationPairAsync).toHaveBeenCalledTimes(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Love'));
            expect(postAssociationAsync).toHaveBeenCalledTimes(1);
        });
    });
});
