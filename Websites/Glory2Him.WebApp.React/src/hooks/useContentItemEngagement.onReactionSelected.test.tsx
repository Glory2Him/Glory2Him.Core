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
import { createAuthState, setLoading, signInAs, signOut } from '../tests/testAuth';

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

// Answers, all at once, every summary read still held open, and lets what follows run.
const answerEveryHeldRead = async (
    reads: Settle<ContentItemReactionSummary[]>[],
    summaries: ContentItemReactionSummary[]): Promise<void> => {
    await act(async () => {
        reads.splice(0).forEach((read) => read.resolve(summaries));
    });
};

// What a card shows, in the view's names: the reaction marked, if any, and each count.
const showing = (
    viewerReactionLabel: string | undefined,
    counts: ReadonlyArray<[string, number]>) => ({
    viewerReactionLabel,
    reactionSummary: counts.map(([name, count]) => ({
        label: name,
        glyph: reactionNamed(name).unicodeEmoji,
        count
    }))
});

// The test environment treats a PageTransitionEvent as a plain Event and drops `persisted`
// from its constructor, so it is set on the event itself.
const dispatchPageTransition = (type: 'pagehide' | 'pageshow', persisted: boolean): void => {
    const event = new Event(type);
    Object.defineProperty(event, 'persisted', { value: persisted });
    act(() => { window.dispatchEvent(event); });
};

// The page going into the back-forward cache and coming back out of it.
const goIntoTheCacheAndBack = (): void => {
    dispatchPageTransition('pagehide', true);
    dispatchPageTransition('pageshow', true);
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

    const shownAs = (render: EngagementRender, contentItemId: string) => {
        const item = shown(render, contentItemId);

        return showing(
            item.viewerReactionLabel,
            (item.reactionSummary ?? []).map((reactionCount) =>
                [reactionCount.label, reactionCount.count] as [string, number]));
    };

    const waitForTheRead = async (render: EngagementRender, contentItemId: string): Promise<void> => {
        await waitFor(() =>
            expect(shown(render, contentItemId).reactionSummary).toBeDefined());
    };

    // Long enough for every promise the hook and the query client chain to run, and for every
    // read that has landed to have been seen, so a change that would follow it has followed.
    const settleEverything = async (): Promise<void> => {
        await act(async () => {
            await new Promise((resolve) => setTimeout(resolve, 50));
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

        // One upsert mutation serves both cards, and its shared state reports only its latest
        // call, B's. Every read here answers each card differently from the overlay it carries,
        // so a card that took a read it must not take, or kept its overlay past the read it
        // must take, shows it.
        it("should end each card's overlay on its own writes when two cards' writes overlap", async () => {
            // given — A settles last
            serverSummaries = {
                'item-a': summaryOf('item-a', [['Joy', 1]], null),
                'item-b': summaryOf('item-b', [['Joy', 1]], null)
            };

            let upserts = holdEachCall(postAssociationAsync);
            let render = renderEngagement([['item-a', 'item-b']]);
            await waitForTheRead(render, 'item-a');
            choose(render, 'item-a', 'Love');
            choose(render, 'item-b', 'Amen');
            await waitFor(() => expect(upserts).toHaveLength(2));
            expect(postAssociationAsync.mock.calls[0][0]).toStrictEqual(requestFor('item-a', 'Love'));

            serverSummaries = {
                'item-a': summaryOf('item-a', [['Joy', 5]], null),
                'item-b': summaryOf('item-b', [['Amen', 3]], 'Amen')
            };

            // when B's write settles while A's is pending
            await act(async () => upserts[1].resolve(createdResult));

            // then
            await waitFor(() =>
                expect(shownAs(render, 'item-b')).toStrictEqual(showing('Amen', [['Amen', 3]])));

            expect(shownAs(render, 'item-a'))
                .toStrictEqual(showing('Love', [['Joy', 1], ['Love', 1]]));

            // when A's write settles
            serverSummaries = {
                'item-a': summaryOf('item-a', [['Love', 4]], 'Love'),
                'item-b': summaryOf('item-b', [['Amen', 3]], 'Amen')
            };

            await act(async () => upserts[0].resolve(createdResult));

            // then
            await waitFor(() =>
                expect(shownAs(render, 'item-a')).toStrictEqual(showing('Love', [['Love', 4]])));

            render.unmount();

            // given — A fails while B is pending
            queryClient.clear();
            postAssociationAsync.mockClear();

            serverSummaries = {
                'item-a': summaryOf('item-a', [['Joy', 1]], null),
                'item-b': summaryOf('item-b', [['Joy', 1]], null)
            };

            upserts = holdEachCall(postAssociationAsync);
            render = renderEngagement([['item-a', 'item-b']]);
            await waitForTheRead(render, 'item-a');
            const reads = holdEachCall(getReactionSummariesAsync);
            choose(render, 'item-a', 'Love');
            choose(render, 'item-b', 'Amen');
            await waitFor(() => expect(upserts).toHaveLength(2));

            // when A's write fails
            await act(async () => upserts[0].reject(new Error('refused')));

            // then A shows the last read at once, before any read lands
            expect(shownAs(render, 'item-a')).toStrictEqual(showing(undefined, [['Joy', 1]]));
            expect(shownAs(render, 'item-b')).toStrictEqual(showing('Amen', [['Joy', 1], ['Amen', 1]]));

            // and B keeps its overlay through the read A's failure sent
            await answerEveryHeldRead(reads, [
                summaryOf('item-a', [['Joy', 7]], null),
                summaryOf('item-b', [['Joy', 9]], null)
            ]);

            await waitFor(() =>
                expect(shownAs(render, 'item-a')).toStrictEqual(showing(undefined, [['Joy', 7]])));

            expect(shownAs(render, 'item-b')).toStrictEqual(showing('Amen', [['Joy', 1], ['Amen', 1]]));

            // when B's write settles and its own re-read lands
            await act(async () => upserts[1].resolve(createdResult));
            await waitFor(() => expect(reads.length).toBeGreaterThan(0));

            await answerEveryHeldRead(reads, [
                summaryOf('item-a', [['Joy', 7]], null),
                summaryOf('item-b', [['Amen', 3]], 'Amen')
            ]);

            // then
            await waitFor(() =>
                expect(shownAs(render, 'item-b')).toStrictEqual(showing('Amen', [['Amen', 3]])));
        });

        // Every read after the first is held open, so what the item shows once a write fails is
        // the last read and nothing laid over it. The failure is the app's global handler's to
        // announce, and this harness has none, so any toast would be the hook's own.
        it('should take the overlay away when a reaction write fails', async () => {
            const failures: ReadonlyArray<{
                choices: ReadonlyArray<string>;
                failTheWrite: (upserts: Settle<AssociationSuggestionResult>[]) => Promise<void>;
            }> = [
                {
                    // the only write
                    choices: ['Love'],
                    failTheWrite: async (upserts) => {
                        await act(async () => upserts[0].reject(new Error('refused')));
                    }
                },
                {
                    // the first of two
                    choices: ['Love', 'Amen'],
                    failTheWrite: async (upserts) => {
                        await act(async () => upserts[0].reject(new Error('refused')));
                    }
                },
                {
                    // the later of two
                    choices: ['Love', 'Amen'],
                    failTheWrite: async (upserts) => {
                        await act(async () => upserts[0].resolve(createdResult));
                        await waitFor(() => expect(upserts).toHaveLength(2));
                        await act(async () => upserts[1].reject(new Error('refused')));
                    }
                }
            ];

            for (const failure of failures) {
                // given
                queryClient.clear();
                serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], 'Joy') };
                getReactionSummariesAsync.mockImplementation(answerFromTheServer);
                const upserts = holdEachCall(postAssociationAsync);
                const render = renderEngagement();
                await waitForTheRead(render, 'item-1');
                holdEachCall(getReactionSummariesAsync);

                for (const choice of failure.choices) {
                    choose(render, 'item-1', choice);
                }

                await waitFor(() => expect(upserts.length).toBeGreaterThan(0));

                // when
                await failure.failTheWrite(upserts);
                await settleEverything();

                // then
                expect(shownAs(render, 'item-1')).toStrictEqual(showing('Joy', [['Joy', 2]]));
                expect(toastError).not.toHaveBeenCalled();
                expect(toastSuccess).not.toHaveBeenCalled();

                render.unmount();
            }
        });

        // Every read is held open, so which read lands, and when, is the test's to decide. Item
        // 1 starts at Joy 2, held. Each read before the one the hook waits on answers it
        // differently from its overlay, so taking that read would show; the read that ends the
        // overlay answers it as the server finally holds it.
        it('should drop the overlay only when the re-read after the latest write lands', async () => {
            type Scenario = {
                pages: ReadonlyArray<ReadonlyArray<string>>;
                expectedOverlay: ReturnType<typeof showing>;
                final: ContentItemReactionSummary;
                landTheEarlierReadsAndSettleTheLatestWrite: (
                    render: EngagementRender,
                    upserts: Settle<AssociationSuggestionResult>[],
                    reads: Settle<ContentItemReactionSummary[]>[]) => Promise<void>;
            };

            const startingSummary = summaryOf('item-1', [['Joy', 2]], 'Joy');
            const earlierAnswer = summaryOf('item-1', [['Moved', 4]], null);
            const loveOverlay = showing('Love', [['Joy', 1], ['Love', 1]]);
            const finalAnswer = summaryOf('item-1', [['Love', 5]], 'Love');

            const scenarios: Record<string, Scenario> = {
                'a read in flight when the reader chose': {
                    pages: [['item-1']],
                    expectedOverlay: loveOverlay,
                    final: finalAnswer,
                    landTheEarlierReadsAndSettleTheLatestWrite: async (render, upserts, reads) => {
                        // the read was sent before the choice, and lands after it
                        await answerEveryHeldRead(reads, [earlierAnswer]);
                        await settleEverything();
                        expect(shownAs(render, 'item-1')).toStrictEqual(loveOverlay);

                        await act(async () => upserts[0].resolve(createdResult));
                    }
                },
                "another card's read": {
                    pages: [['item-1', 'item-2']],
                    expectedOverlay: loveOverlay,
                    final: finalAnswer,
                    landTheEarlierReadsAndSettleTheLatestWrite: async (render, upserts, reads) => {
                        choose(render, 'item-2', 'Amen');
                        await waitFor(() => expect(upserts).toHaveLength(2));

                        await act(async () => upserts[1].resolve(createdResult));
                        await waitFor(() => expect(reads.length).toBeGreaterThan(0));

                        await answerEveryHeldRead(reads, [
                            earlierAnswer,
                            summaryOf('item-2', [['Amen', 1]], 'Amen')
                        ]);

                        await waitFor(() => expect(shownAs(render, 'item-2'))
                            .toStrictEqual(showing('Amen', [['Amen', 1]])));

                        expect(shownAs(render, 'item-1')).toStrictEqual(loveOverlay);

                        await act(async () => upserts[0].resolve(createdResult));
                    }
                },
                'a refresh that landed before the write settled': {
                    pages: [['item-1']],
                    expectedOverlay: loveOverlay,
                    final: finalAnswer,
                    landTheEarlierReadsAndSettleTheLatestWrite: async (render, upserts, reads) => {
                        // the write's own refresh is sent as it settles, before its call
                        // resolves to the hook
                        await act(async () => upserts[0].resolve(createdResult));
                        await waitFor(() => expect(reads.length).toBeGreaterThan(0));

                        await act(async () => reads.shift()!.resolve([earlierAnswer]));
                        await settleEverything();
                        expect(shownAs(render, 'item-1')).toStrictEqual(loveOverlay);
                    }
                },
                "an earlier write's read": {
                    pages: [['item-1']],
                    expectedOverlay: showing('Amen', [['Joy', 1], ['Amen', 1]]),
                    final: summaryOf('item-1', [['Amen', 5]], 'Amen'),
                    landTheEarlierReadsAndSettleTheLatestWrite: async (render, upserts, reads) => {
                        choose(render, 'item-1', 'Amen');

                        await act(async () => upserts[0].resolve(createdResult));
                        await waitFor(() => expect(upserts).toHaveLength(2));
                        await waitFor(() => expect(reads.length).toBeGreaterThan(0));
                        await answerEveryHeldRead(reads, [earlierAnswer]);
                        await settleEverything();

                        expect(shownAs(render, 'item-1'))
                            .toStrictEqual(showing('Amen', [['Joy', 1], ['Amen', 1]]));

                        await act(async () => upserts[1].resolve(createdResult));
                    }
                },
                'an unchanged answer': {
                    pages: [['item-1']],
                    expectedOverlay: loveOverlay,
                    final: startingSummary,
                    landTheEarlierReadsAndSettleTheLatestWrite: async (_render, upserts) => {
                        await act(async () => upserts[0].resolve(createdResult));
                    }
                }
            };

            for (const [name, scenario] of Object.entries(scenarios)) {
                // given
                queryClient.clear();

                serverSummaries = {
                    'item-1': startingSummary,
                    'item-2': summaryOf('item-2', [['Joy', 1]], null)
                };

                getReactionSummariesAsync.mockImplementation(answerFromTheServer);
                const upserts = holdEachCall(postAssociationAsync);
                const render = renderEngagement(scenario.pages);
                await waitForTheRead(render, 'item-1');
                const reads = holdEachCall(getReactionSummariesAsync);

                if (name === 'a read in flight when the reader chose') {
                    act(() => { void queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] }); });
                    await waitFor(() => expect(reads).toHaveLength(1));
                }

                choose(render, 'item-1', 'Love');
                await waitFor(() => expect(upserts).toHaveLength(1));

                // when
                await scenario.landTheEarlierReadsAndSettleTheLatestWrite(render, upserts, reads);
                await settleEverything();

                // then, before the re-read the hook waits on lands
                expect(shownAs(render, 'item-1')).toStrictEqual(scenario.expectedOverlay);

                // when it lands
                await waitFor(() => expect(reads.length).toBeGreaterThan(0));

                await answerEveryHeldRead(reads, [
                    scenario.final,
                    summaryOf('item-2', [['Amen', 1]], 'Amen')
                ]);

                // then
                await waitFor(() => expect(shownAs(render, 'item-1')).toStrictEqual(showing(
                    scenario.final.viewerReactionName ?? undefined,
                    scenario.final.reactions.map((reaction) =>
                        [reaction.name, reaction.count] as [string, number]))));

                render.unmount();
            }
        });

        // Each case holds every write open, and checks at the broker that a waiting write is not
        // made while the one before it is pending, and is made once that one resolves. Item 1
        // starts with Love held.
        it("should send an item's next reaction write only once the one before it settles", async () => {
            const loveHeld = summaryOf('item-1', [['Love', 3]], 'Love');

            const startWithLoveHeld = async () => {
                queryClient.clear();
                vi.clearAllMocks();
                serverSummaries = { 'item-1': loveHeld };
                getReactionSummariesAsync.mockImplementation(answerFromTheServer);
                const upserts = holdEachCall(postAssociationAsync);
                const withdrawals = holdEachCall(deleteAssociationPairAsync);
                const render = renderEngagement();
                await waitForTheRead(render, 'item-1');

                return { render, upserts, withdrawals };
            };

            // a withdrawal chosen while a change is pending
            let { render, upserts, withdrawals } = await startWithLoveHeld();
            choose(render, 'item-1', 'Joy');
            choose(render, 'item-1', 'Joy');
            await waitFor(() => expect(upserts).toHaveLength(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            await settleEverything();
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();

            await act(async () => upserts[0].resolve(createdResult));

            await waitFor(() => expect(deleteAssociationPairAsync).toHaveBeenCalledTimes(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            render.unmount();

            // a change chosen while a withdrawal is pending
            ({ render, upserts, withdrawals } = await startWithLoveHeld());
            choose(render, 'item-1', 'Love');
            choose(render, 'item-1', 'Joy');
            await waitFor(() => expect(withdrawals).toHaveLength(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Love'));
            await settleEverything();
            expect(postAssociationAsync).not.toHaveBeenCalled();

            await act(async () => withdrawals[0].resolve(undefined));

            await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            render.unmount();

            // two writes waiting
            ({ render, upserts, withdrawals } = await startWithLoveHeld());
            choose(render, 'item-1', 'Joy');
            choose(render, 'item-1', 'Joy');
            choose(render, 'item-1', 'Amen');
            await waitFor(() => expect(upserts).toHaveLength(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            await settleEverything();
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();

            await act(async () => upserts[0].resolve(createdResult));

            await waitFor(() => expect(deleteAssociationPairAsync).toHaveBeenCalledTimes(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            await settleEverything();
            expect(postAssociationAsync).toHaveBeenCalledTimes(1);

            await act(async () => withdrawals[0].resolve(undefined));

            await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(2));
            expect(postAssociationAsync).toHaveBeenLastCalledWith(requestFor('item-1', 'Amen'));
            render.unmount();

            // a change chosen once the page is restored from the back-forward cache, while the
            // write pending as it went in is still pending
            ({ render, upserts, withdrawals } = await startWithLoveHeld());
            choose(render, 'item-1', 'Love');
            choose(render, 'item-1', 'Joy');
            await waitFor(() => expect(withdrawals).toHaveLength(1));

            goIntoTheCacheAndBack();

            expect(shownAs(render, 'item-1')).toStrictEqual(showing('Love', [['Love', 3]]));
            choose(render, 'item-1', 'Amen');
            await settleEverything();
            expect(postAssociationAsync).not.toHaveBeenCalled();

            await act(async () => withdrawals[0].resolve(undefined));

            await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(1));
            expect(postAssociationAsync).toHaveBeenCalledWith(requestFor('item-1', 'Amen'));
            await settleEverything();
            expect(postAssociationAsync).not.toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
            render.unmount();
        });

        // Item 1 starts with Love held, and the change to Joy fails with writes waiting behind
        // it. Every read after the first is held open, so the item shows the last read.
        it('should drop the reaction writes that wait behind one that fails', async () => {
            const waitingChoicesByCase: ReadonlyArray<ReadonlyArray<string>> = [
                ['Joy'],
                ['Amen', 'Amen']
            ];

            for (const waitingChoices of waitingChoicesByCase) {
                // given
                queryClient.clear();
                vi.clearAllMocks();
                serverSummaries = { 'item-1': summaryOf('item-1', [['Love', 3]], 'Love') };
                getReactionSummariesAsync.mockImplementation(answerFromTheServer);
                const upserts = holdEachCall(postAssociationAsync);
                holdEachCall(deleteAssociationPairAsync);
                const render = renderEngagement();
                await waitForTheRead(render, 'item-1');
                holdEachCall(getReactionSummariesAsync);

                choose(render, 'item-1', 'Joy');

                for (const waitingChoice of waitingChoices) {
                    choose(render, 'item-1', waitingChoice);
                }

                await waitFor(() => expect(upserts).toHaveLength(1));

                // when
                await act(async () => upserts[0].reject(new Error('refused')));
                await settleEverything();

                // then
                expect(postAssociationAsync).toHaveBeenCalledTimes(1);
                expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
                expect(shownAs(render, 'item-1')).toStrictEqual(showing('Love', [['Love', 3]]));

                // when the reader chooses again
                choose(render, 'item-1', 'Joy');

                // then it is sent at once
                await waitFor(() => expect(postAssociationAsync).toHaveBeenCalledTimes(2));
                expect(postAssociationAsync).toHaveBeenLastCalledWith(requestFor('item-1', 'Joy'));
                expect(deleteAssociationPairAsync).not.toHaveBeenCalled();

                render.unmount();
            }
        });

        // The reader moving to another page of the app unmounts the page's hook. Its queue goes
        // on: the page's own writes are still sent in their turn.
        it('should still send a waiting reaction write after the reader moves to another page of the app', async () => {
            // given
            serverSummaries = { 'item-1': summaryOf('item-1', [['Love', 3]], 'Love') };
            const upserts = holdEachCall(postAssociationAsync);
            const render = renderEngagement();
            await waitForTheRead(render, 'item-1');
            choose(render, 'item-1', 'Joy');
            choose(render, 'item-1', 'Joy');
            await waitFor(() => expect(upserts).toHaveLength(1));

            // when
            render.unmount();
            await act(async () => upserts[0].resolve(createdResult));

            // then
            await waitFor(() => expect(deleteAssociationPairAsync).toHaveBeenCalledTimes(1));
            expect(deleteAssociationPairAsync).toHaveBeenCalledWith(requestFor('item-1', 'Joy'));
        });

        // Item 1 starts with Love held, and the reader chooses Joy, then Joy again. The page goes
        // into the cache while the change to Joy is pending and the withdrawal of Joy waits.
        // Every read after the first is held open, so an item with no overlay shows the last read.
        it('should drop the waiting reaction writes when the page goes into the back-forward cache', async () => {
            const startWithAWriteWaiting = async (contentItemIdPages: ReadonlyArray<ReadonlyArray<string>>) => {
                queryClient.clear();
                vi.clearAllMocks();

                serverSummaries = {
                    'item-1': summaryOf('item-1', [['Love', 3]], 'Love'),
                    'item-2': summaryOf('item-2', [['Amen', 2]], 'Amen')
                };

                getReactionSummariesAsync.mockImplementation(answerFromTheServer);
                const upserts = holdEachCall(postAssociationAsync);
                holdEachCall(deleteAssociationPairAsync);
                const render = renderEngagement(contentItemIdPages);
                await waitForTheRead(render, 'item-1');
                holdEachCall(getReactionSummariesAsync);
                choose(render, 'item-1', 'Joy');
                choose(render, 'item-1', 'Joy');
                await waitFor(() => expect(upserts).toHaveLength(1));

                return { render, upserts };
            };

            // given — the hook mounted, and a second item's change to Moved pending with nothing
            // waiting behind it
            let { render, upserts } = await startWithAWriteWaiting([['item-1', 'item-2']]);
            choose(render, 'item-2', 'Moved');
            await waitFor(() => expect(upserts).toHaveLength(2));

            // when
            goIntoTheCacheAndBack();

            // then
            expect(shownAs(render, 'item-1')).toStrictEqual(showing('Love', [['Love', 3]]));
            expect(shownAs(render, 'item-2')).toStrictEqual(showing('Moved', [['Amen', 1], ['Moved', 1]]));

            // when
            await act(async () => upserts[0].resolve(createdResult));
            await settleEverything();

            // then
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
            render.unmount();

            // given — the hook unmounted, as when the reader moves to another page of the app
            ({ render, upserts } = await startWithAWriteWaiting([['item-1']]));
            render.unmount();

            // when
            goIntoTheCacheAndBack();
            await act(async () => upserts[0].resolve(createdResult));
            await settleEverything();

            // then
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
        });
    });

    describe('a reader who is not signed in', () => {
        // The overlay stands only between a press and the read that follows the item's write,
        // and this choice writes nothing: a hook that laid it before its guard would show Love
        // marked, and nothing would ever take it away.
        it('should send a signed-out reader to sign in and write nothing', async () => {
            // given
            signOut(authState);
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], null) };
            const render = renderEngagement([['item-1']], '/posts?q=grace#item-1');
            await waitForTheRead(render, 'item-1');

            // when
            choose(render, 'item-1', 'Love');
            await settleEverything();

            // then
            expect(render.result.current.location.pathname).toBe('/Account/Login');

            expect(new URLSearchParams(render.result.current.location.search).get('returnUrl'))
                .toBe('/posts?q=grace#item-1');

            expect(postAssociationAsync).not.toHaveBeenCalled();
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
            expect(shownAs(render, 'item-1')).toStrictEqual(showing(undefined, [['Joy', 2]]));
        });

        // A reader not yet known has no summaries read, so the last read said nothing: the item
        // is shown as the page handed it.
        it("should do nothing while the reader's sign-in state is still unknown", async () => {
            // given
            setLoading(authState);
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], null) };
            const render = renderEngagement([['item-1']], '/posts?q=grace#item-1');

            // when
            choose(render, 'item-1', 'Love');
            await settleEverything();

            // then
            expect(render.result.current.location.pathname).toBe('/posts');
            expect(postAssociationAsync).not.toHaveBeenCalled();
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
            expect(shown(render, 'item-1')).toStrictEqual(cardFor('item-1'));
        });

        // A failed read of the current user leaves no user and is no longer loading: the reader
        // is not yet known, not signed out, and the app's global handler has announced it.
        it("should do nothing when the reader's sign-in state could not be read", async () => {
            // given
            authState.data = undefined;
            authState.isLoading = false;
            serverSummaries = { 'item-1': summaryOf('item-1', [['Joy', 2]], null) };
            const render = renderEngagement([['item-1']], '/posts?q=grace#item-1');

            // when
            choose(render, 'item-1', 'Love');
            await settleEverything();

            // then
            expect(render.result.current.location.pathname).toBe('/posts');
            expect(postAssociationAsync).not.toHaveBeenCalled();
            expect(deleteAssociationPairAsync).not.toHaveBeenCalled();
            expect(shown(render, 'item-1')).toStrictEqual(cardFor('item-1'));
        });
    });
});
