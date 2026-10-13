import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider, QueryKey, useQuery } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { liveUpdateService } from './liveUpdateService';
import type LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';
import { toastError } from '../../brokers/toastBroker.error';
import { toastSuccess } from '../../brokers/toastBroker.success';
import { AuthProvider } from '../../components/securitys/authProvider';
import { EntityType } from '../../models/foundations/approvalSettings/approvalSetting';
import { createAuthState, signInAs, signOut } from '../../tests/testAuth';

import {
    LiveUpdate,
    LiveUpdateListeners,
    LiveUpdateType
} from '../../models/foundations/liveUpdates/liveUpdate';

const startLiveUpdatesAsync = vi.fn<LiveUpdateBroker['startLiveUpdatesAsync']>();
const stopLiveUpdatesAsync = vi.fn<LiveUpdateBroker['stopLiveUpdatesAsync']>();

// Each member hands the hook a promise of its own that settles as the spy's does. The spy
// handles every promise it returns, to record how it settled, so a failure the hook let go of
// unhandled would never be heard if the hook were handed the spy's own promise.
vi.mock('../../brokers/hubBroker.liveUpdates', () => ({
    default: class {
        startLiveUpdatesAsync: LiveUpdateBroker['startLiveUpdatesAsync'] =
            (listeners, nextRetryDelayInMilliseconds) =>
                startLiveUpdatesAsync(listeners, nextRetryDelayInMilliseconds).then(() => undefined);

        stopLiveUpdatesAsync: LiveUpdateBroker['stopLiveUpdatesAsync'] =
            () => stopLiveUpdatesAsync().then(() => undefined);
    }
}));

vi.mock('../../brokers/toastBroker.error', () => ({
    toastError: vi.fn()
}));

vi.mock('../../brokers/toastBroker.success', () => ({
    toastSuccess: vi.fn()
}));

// The reader the app's AuthProvider names, set per test.
const authState = createAuthState();

vi.mock('./accountService', () => ({
    accountService: {
        useGetCurrentUser: () => authState
    }
}));

// THE READS A MESSAGE MAY REACH, each keyed as the hook that makes it keys it. Every one names
// item-1 where its key can, so a read left alone is left alone for its family or its page, never
// because the message named something else.
//
// The five families a settings write already makes stale (contentItemSettingService.ts).
const settingReadKeys: ReadonlyArray<QueryKey> = [
    ['ContentItemSettingsGetEffective', ['item-1', 'item-2']],
    ['ContentItemSettingsGetAll', { page: 1, pageSize: 20 }],
    ['ContentItemSettingsGetById', 'setting-1'],
    ['ContentItemSettingsGetAvailableForContribution'],
    ['ContentItemSettingsGetDefaults']
];

// Keyed as useGetReactionSummaries keys a page: the reader, then the page of ids it asked for. A
// signed-out reader's page is keyed on null.
const summaryReadKeysHoldingTheItem: ReadonlyArray<QueryKey> = [
    ['ReactionSummaries', 'reader-1', ['item-1', 'item-2']],
    ['ReactionSummaries', null, ['item-2', 'item-1']]
];

const summaryReadKeysOfAnotherPage: ReadonlyArray<QueryKey> = [
    ['ReactionSummaries', 'reader-1', ['item-3']]
];

// Reads no message covers (LiveUpdates.md rule 3): the item's own row, and its review round,
// whose third key element names the item as a summary read's page holds it.
const otherReadKeys: ReadonlyArray<QueryKey> = [
    ['ContentItemsGetById', 'item-1'],
    ['ApprovalVerdict', EntityType.ContentItem, 'item-1']
];

const everyCachedReadKey: ReadonlyArray<QueryKey> = [
    ...settingReadKeys,
    ...summaryReadKeysHoldingTheItem,
    ...summaryReadKeysOfAnotherPage,
    ...otherReadKeys
];

const settingMessage: LiveUpdate = {
    type: LiveUpdateType.ContentItemSettingChanged,
    contentItemId: null
};

const countMessageFor = (contentItemId: string | null): LiveUpdate => ({
    type: LiveUpdateType.ContentItemReactionsChanged,
    contentItemId
});

describe('liveUpdateService.useLiveUpdates', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    // The listeners the hook handed its latest start, which is what the broker calls.
    const theListeners = (): LiveUpdateListeners => {
        expect(startLiveUpdatesAsync, 'a started connection').toHaveBeenCalled();

        return startLiveUpdatesAsync.mock.lastCall![0];
    };

    // Cached from another screen: no page is serving them, so a read made stale is marked and
    // not read again.
    const cacheReadsAsync = async (queryKeys: ReadonlyArray<QueryKey>) => {
        for (const queryKey of queryKeys) {
            await queryClient.prefetchQuery({ queryKey, queryFn: async () => [] });
        }
    };

    const staleReadKeys = (queryKeys: ReadonlyArray<QueryKey>) =>
        queryKeys.filter(queryKey => queryClient.getQueryState(queryKey)?.isInvalidated === true);

    // Served on the page, so a read made stale is read again at once. None is among the cached
    // reads, so what is asserted of one never depends on the other.
    const serveActiveReadsAsync = async () => {
        const activeSettingRead = vi.fn().mockResolvedValue([]);
        const activeSummaryRead = vi.fn().mockResolvedValue([]);
        const activeOtherRead = vi.fn().mockResolvedValue([]);

        renderHook(
            () => {
                useQuery({
                    queryKey: ['ContentItemSettingsGetEffective', ['item-9']],
                    queryFn: activeSettingRead
                });

                useQuery({
                    queryKey: ['ReactionSummaries', 'reader-1', ['item-9']],
                    queryFn: activeSummaryRead
                });

                useQuery({
                    queryKey: ['ContentItemsGetById', 'item-9'],
                    queryFn: activeOtherRead
                });
            },
            { wrapper });

        await act(async () => undefined);

        return { activeSettingRead, activeSummaryRead, activeOtherRead };
    };

    // Only the timer a start that is tried again waits on is faked, so no test waits in real time
    // and a start still waiting when a test ends never fires in the next one.
    beforeEach(() => {
        vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
        vi.resetAllMocks();
        startLiveUpdatesAsync.mockResolvedValue(undefined);
        stopLiveUpdatesAsync.mockResolvedValue(undefined);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    // Mounts the hook and lets what its mount set going settle, without moving the clock.
    const mountTheHookAsync = async () => {
        const mountedHook = renderHook(() => liveUpdateService.useLiveUpdates(), { wrapper });
        await act(async () => undefined);

        return mountedHook;
    };

    it('should start one live connection when mounted', async () => {
        // when
        await mountTheHookAsync();

        // then
        expect(startLiveUpdatesAsync).toHaveBeenCalledTimes(1);

        expect(startLiveUpdatesAsync).toHaveBeenCalledWith(
            {
                onLiveUpdate: expect.any(Function),
                onReconnected: expect.any(Function),
                onClosed: expect.any(Function)
            },
            expect.any(Function));
    });

    it('should stop the live connection when unmounted', async () => {
        // given
        const { unmount } = await mountTheHookAsync();

        // when
        unmount();

        // then
        expect(stopLiveUpdatesAsync).toHaveBeenCalledTimes(1);
    });

    // Root renders again on every navigation (useLazyLoad reads the location), and a stop and a
    // start between two renders would lose a message sent in the gap.
    it('should keep its one connection when the component renders again', async () => {
        // given
        const { rerender } = await mountTheHookAsync();

        // when
        rerender();
        rerender();
        await act(async () => undefined);

        // then
        expect(startLiveUpdatesAsync).toHaveBeenCalledTimes(1);
        expect(stopLiveUpdatesAsync).not.toHaveBeenCalled();
    });

    // A summary answers its host's ShowReactions inside its count (§ARC16.8), so a changed
    // setting can change what any summary says, as well as what any setting read answers.
    it('should make every setting read and every summary read stale on a setting message', async () => {
        // given
        await mountTheHookAsync();
        const activeReads = await serveActiveReadsAsync();
        await cacheReadsAsync(everyCachedReadKey);

        // when
        await act(async () => theListeners().onLiveUpdate(settingMessage));

        // then
        expect(staleReadKeys(everyCachedReadKey)).toEqual([
            ...settingReadKeys,
            ...summaryReadKeysHoldingTheItem,
            ...summaryReadKeysOfAnotherPage
        ]);

        expect(activeReads.activeSettingRead).toHaveBeenCalledTimes(2);
        expect(activeReads.activeSummaryRead).toHaveBeenCalledTimes(2);
        expect(activeReads.activeOtherRead).toHaveBeenCalledTimes(1);
    });

    // A count message names the one version whose counts may have moved, so only the pages
    // holding it read again: a reader's page and a signed-out reader's page alike, and nothing
    // else, not another page's summaries, nor the item's other reads, nor any setting read.
    it('should make stale only the summary reads holding the item on a count message', async () => {
        // given
        await mountTheHookAsync();
        await cacheReadsAsync(everyCachedReadKey);

        // when
        await act(async () => theListeners().onLiveUpdate(countMessageFor('item-1')));

        // then
        expect(staleReadKeys(everyCachedReadKey)).toEqual(summaryReadKeysHoldingTheItem);
    });

    // A message may have been sent while the connection was away, so every read a message
    // covers is read again: every setting read, and every page's summaries.
    it.each([
        {
            wayBack: 'SignalR reconnecting',
            arrange: () => undefined,
            comeBackAsync: async () => theListeners().onReconnected()
        },
        {
            wayBack: 'a start that succeeds after an earlier attempt failed',
            arrange: () => startLiveUpdatesAsync.mockRejectedValueOnce(new Error('refused')),
            comeBackAsync: async () => { await vi.advanceTimersByTimeAsync(0); }
        },
        {
            // Only a first start that succeeds at once is spared (rule 5). A connection started
            // again after closing for good was away as long as it was down.
            wayBack: 'a start that succeeds after the connection closed for good',
            arrange: () => undefined,
            comeBackAsync: async () => {
                theListeners().onClosed();
                await vi.advanceTimersByTimeAsync(0);
            }
        }
    ])('should make every covered read stale when the connection comes back, by $wayBack',
        async ({ arrange, comeBackAsync }) => {
            // given
            arrange();
            await mountTheHookAsync();
            await cacheReadsAsync(everyCachedReadKey);

            // when
            await act(comeBackAsync);

            // then
            expect(staleReadKeys(everyCachedReadKey)).toEqual([
                ...settingReadKeys,
                ...summaryReadKeysHoldingTheItem,
                ...summaryReadKeysOfAnotherPage
            ]);
        });

    // The pages a first connection serves have just read, so reading them again would only
    // double every read the app makes when it loads (LiveUpdates.md, Risks).
    it('should make nothing stale when the first start succeeds at once', async () => {
        // given
        const activeReads = await serveActiveReadsAsync();
        await cacheReadsAsync(everyCachedReadKey);

        // when
        await mountTheHookAsync();

        // then
        expect(staleReadKeys(everyCachedReadKey)).toEqual([]);
        expect(activeReads.activeSettingRead).toHaveBeenCalledTimes(1);
        expect(activeReads.activeSummaryRead).toHaveBeenCalledTimes(1);
    });

    const advanceTheClockAsync = async (milliseconds: number) =>
        await act(async () => { await vi.advanceTimersByTimeAsync(milliseconds); });

    // Moves the clock on through each delay in turn, proving the connection is started again
    // once that delay has passed and not a millisecond sooner.
    const expectStartedAgainAfterAsync = async (delaysInMilliseconds: ReadonlyArray<number>) => {
        for (const delay of delaysInMilliseconds) {
            const startsBefore = startLiveUpdatesAsync.mock.calls.length;

            if (delay > 0) {
                await advanceTheClockAsync(delay - 1);

                expect(startLiveUpdatesAsync, `${delay - 1} ms into a ${delay} ms wait`)
                    .toHaveBeenCalledTimes(startsBefore);
            }

            await advanceTheClockAsync(delay > 0 ? 1 : 0);

            expect(startLiveUpdatesAsync, `once a ${delay} ms wait has passed`)
                .toHaveBeenCalledTimes(startsBefore + 1);
        }
    };

    const refused = new Error('refused');

    it.each([
        {
            trigger: 'a start that fails',
            arrangeAsync: async () => {
                startLiveUpdatesAsync.mockRejectedValue(refused);
                await mountTheHookAsync();
            }
        },
        {
            // Started at the second attempt, before it closes, so the schedule the close
            // follows is a fresh one, not the rest of the first.
            trigger: 'a connection that closes for good',
            arrangeAsync: async () => {
                startLiveUpdatesAsync
                    .mockRejectedValueOnce(refused)
                    .mockResolvedValueOnce(undefined)
                    .mockRejectedValue(refused);

                await mountTheHookAsync();
                await advanceTheClockAsync(0);
                act(() => theListeners().onClosed());
            }
        }
    ])('should start the connection again on the schedule, after $trigger', async ({ arrangeAsync }) => {
        // given
        await arrangeAsync();

        // then
        await expectStartedAgainAfterAsync([0, 2000, 10000, 30000, 30000, 30000]);
    });

    // SignalR stops reconnecting at the first null its schedule answers, and closes for good.
    it('should hand SignalR a schedule that never gives up', async () => {
        // given
        await mountTheHookAsync();
        const nextRetryDelayInMilliseconds = startLiveUpdatesAsync.mock.lastCall![1];

        // when
        const retryDelays = [0, 1, 2, 3, 4, 5, 10, 100, 10000].map(nextRetryDelayInMilliseconds);

        // then
        expect(retryDelays).toEqual([0, 2000, 10000, 30000, 30000, 30000, 30000, 30000, 30000]);
    });

    // The two ways the hook could tell anyone of a failure: a toast, or letting the failure go
    // unhandled, which the browser reports as an uncaught error. The app has no handler that
    // turns the second into anything a reader sees, so it is heard here as the test runner
    // hears it, on the process, once the turn it escaped in has ended.
    const failuresLetGoOfAsync = async (failAsync: () => Promise<void>) => {
        const failuresLetGo: unknown[] = [];
        const hear = (reason: unknown) => failuresLetGo.push(reason);
        process.on('unhandledRejection', hear);

        try {
            await failAsync();
            await new Promise(resolve => setImmediate(resolve));
        } finally {
            process.off('unhandledRejection', hear);
        }

        return failuresLetGo;
    };

    it.each([
        {
            failure: 'a start that fails',
            failAsync: async () => {
                startLiveUpdatesAsync.mockRejectedValue(refused);
                await mountTheHookAsync();
                await advanceTheClockAsync(45000);
            }
        },
        {
            failure: 'a connection that is lost',
            failAsync: async () => {
                await mountTheHookAsync();
                startLiveUpdatesAsync.mockRejectedValue(refused);
                act(() => theListeners().onClosed());
                await advanceTheClockAsync(45000);
            }
        },
        {
            failure: 'a stop that fails',
            failAsync: async () => {
                stopLiveUpdatesAsync.mockRejectedValue(refused);
                const { unmount } = await mountTheHookAsync();
                unmount();
            }
        }
    ])('should announce nothing when the connection fails, on $failure', async ({ failAsync }) => {
        // when
        const failuresLetGo = await failuresLetGoOfAsync(failAsync);

        // then
        expect(failuresLetGo).toEqual([]);
        expect(toastError).not.toHaveBeenCalled();
        expect(toastSuccess).not.toHaveBeenCalled();
    });

    // Root unmounts when the root route's error page takes its place, and strict mode unmounts
    // and mounts it again in development. A connection the unmounted hook started again would
    // be one more than the tab's one, with nothing left to stop it.
    it.each([
        {
            // SignalR raises onclose for a stop the app asked for as for any other close.
            way: 'on the close its own stop causes',
            unmountAsync: async () => {
                const { unmount } = await mountTheHookAsync();
                stopLiveUpdatesAsync.mockImplementation(async () => theListeners().onClosed());
                unmount();
            }
        },
        {
            // A stop that lands while the connection is still starting fails that start instead,
            // which is what strict mode's unmount does to the start its mount began.
            way: 'on the start its own stop cuts short',
            unmountAsync: async () => {
                let cutTheStartShort: (reason: unknown) => void = () => undefined;

                startLiveUpdatesAsync.mockReturnValueOnce(
                    new Promise<void>((_, reject) => { cutTheStartShort = reject; }));

                const { unmount } = await mountTheHookAsync();
                unmount();
                cutTheStartShort(new Error('The connection was stopped during negotiation.'));
            }
        },
        {
            way: 'when a start that was waiting on the schedule comes due',
            unmountAsync: async () => {
                startLiveUpdatesAsync.mockRejectedValue(refused);
                const { unmount } = await mountTheHookAsync();
                await advanceTheClockAsync(0);
                unmount();
            }
        },
        {
            way: 'when its stop fails',
            unmountAsync: async () => {
                stopLiveUpdatesAsync.mockRejectedValue(refused);
                const { unmount } = await mountTheHookAsync();
                unmount();
            }
        }
    ])('should start nothing once it has unmounted, $way', async ({ unmountAsync }) => {
        // given
        await unmountAsync();
        const startsAtUnmount = startLiveUpdatesAsync.mock.calls.length;

        // when
        await act(async () => undefined);
        await advanceTheClockAsync(120000);

        // then
        expect(startLiveUpdatesAsync).toHaveBeenCalledTimes(startsAtUnmount);
    });

    // Every reader's connection is the same, and hears the same messages (§SEC14.8 rule 2), so
    // the hook is mounted under each kind of reader the app's own AuthProvider can name.
    it.each([
        { reader: 'a signed-out reader', signIn: () => signOut(authState) },
        { reader: 'a signed-in reader with no role', signIn: () => signInAs(authState, []) },
        { reader: 'an administrator', signIn: () => signInAs(authState, ['Administrators']) }
    ])('should hold the same connection for every reader, signed in or not, for $reader',
        async ({ signIn }) => {
            // given
            signIn();

            const wrapperUnderTheReader = ({ children }: { children: ReactNode }) => (
                <QueryClientProvider client={queryClient}>
                    <AuthProvider>{children}</AuthProvider>
                </QueryClientProvider>
            );

            renderHook(() => liveUpdateService.useLiveUpdates(), { wrapper: wrapperUnderTheReader });
            await act(async () => undefined);
            await cacheReadsAsync(everyCachedReadKey);

            // when
            await act(async () => theListeners().onLiveUpdate(settingMessage));

            // then
            expect(startLiveUpdatesAsync).toHaveBeenCalledTimes(1);

            expect(staleReadKeys(everyCachedReadKey)).toEqual([
                ...settingReadKeys,
                ...summaryReadKeysHoldingTheItem,
                ...summaryReadKeysOfAnotherPage
            ]);
        });
});
