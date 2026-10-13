import { ReactNode } from 'react';
import { QueryClient, QueryClientProvider, QueryKey, useQuery } from '@tanstack/react-query';
import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { liveUpdateService } from './liveUpdateService';
import type LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';
import { EntityType } from '../../models/foundations/approvalSettings/approvalSetting';

import {
    LiveUpdate,
    LiveUpdateListeners,
    LiveUpdateType
} from '../../models/foundations/liveUpdates/liveUpdate';

const startLiveUpdatesAsync = vi.fn<LiveUpdateBroker['startLiveUpdatesAsync']>();
const stopLiveUpdatesAsync = vi.fn<LiveUpdateBroker['stopLiveUpdatesAsync']>();

vi.mock('../../brokers/hubBroker.liveUpdates', () => ({
    default: class {
        startLiveUpdatesAsync = startLiveUpdatesAsync;
        stopLiveUpdatesAsync = stopLiveUpdatesAsync;
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

describe('liveUpdateService.useLiveUpdates', () => {
    let queryClient: QueryClient;

    const wrapper = ({ children }: { children: ReactNode }) => (
        <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
    );

    // The listeners the hook handed its latest start, which is what the broker calls.
    const theListeners = (): LiveUpdateListeners => startLiveUpdatesAsync.mock.lastCall![0];

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

    beforeEach(() => {
        vi.resetAllMocks();
        startLiveUpdatesAsync.mockResolvedValue(undefined);
        stopLiveUpdatesAsync.mockResolvedValue(undefined);

        queryClient = new QueryClient({
            defaultOptions: { queries: { retry: false } }
        });
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
});
