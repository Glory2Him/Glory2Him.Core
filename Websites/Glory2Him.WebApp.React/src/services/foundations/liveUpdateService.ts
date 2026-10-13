import { useEffect, useState } from 'react';
import { QueryClient, useQueryClient } from '@tanstack/react-query';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';

import {
    LiveUpdate,
    LiveUpdateListeners,
    LiveUpdateType
} from '../../models/foundations/liveUpdates/liveUpdate';

// SignalR's documented default reconnection delays, then the last of them again without end, so
// a connection that will not come back is never given up while the tab is open. The hook waits
// on it before starting a failed connection again, and SignalR on it before reconnecting.
const retryDelaysInMilliseconds = [0, 2000, 10000, 30000];

const nextRetryDelayInMilliseconds = (previousRetryCount: number): number =>
    retryDelaysInMilliseconds[Math.min(previousRetryCount, retryDelaysInMilliseconds.length - 1)];

// The five families a settings write makes stale (contentItemSettingService.ts), named here
// because a foundation service calls no other service (tsr-services-009). A family a settings
// write comes to make stale is not made stale by a message until it is named here too.
const settingReadFamilies = [
    'ContentItemSettingsGetEffective',
    'ContentItemSettingsGetAll',
    'ContentItemSettingsGetById',
    'ContentItemSettingsGetAvailableForContribution',
    'ContentItemSettingsGetDefaults'
];

const makeSettingReadsStale = (queryClient: QueryClient): void => {
    for (const settingReadFamily of settingReadFamilies) {
        queryClient.invalidateQueries({ queryKey: [settingReadFamily] });
    }
};

// A changed setting can change what any item renders under, and a summary answers its host's
// ShowReactions inside its count (§ARC16.8), so every setting read and every summary read.
const makeSettingMessageReadsStale = (queryClient: QueryClient): void => {
    makeSettingReadsStale(queryClient);
    queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
};

// Only the pages of summaries holding the version whose counts may have moved. A summary read
// is keyed ['ReactionSummaries', <readerId>, <that page's ids>], so the page is the third
// element. A message that names no item holds no page's id, and makes nothing stale.
const makeCountMessageReadsStale = (queryClient: QueryClient, contentItemId: string | null): void => {
    queryClient.invalidateQueries({
        queryKey: ['ReactionSummaries'],
        predicate: query =>
            (query.queryKey[2] as ReadonlyArray<string | null>).includes(contentItemId)
    });
};

// A type the page does not know changes nothing: a host newer than the page may send one.
const makeStaleWhatTheMessageCovers = (queryClient: QueryClient, liveUpdate: LiveUpdate): void => {
    if (liveUpdate.type === LiveUpdateType.ContentItemSettingChanged) {
        makeSettingMessageReadsStale(queryClient);
    } else if (liveUpdate.type === LiveUpdateType.ContentItemReactionsChanged) {
        makeCountMessageReadsStale(queryClient, liveUpdate.contentItemId);
    }
};

// Every read any message could have covered, since a connection that was away may have missed
// a message of either type: every setting read, and every page of summaries.
const makeEveryCoveredReadStale = (queryClient: QueryClient): void => {
    makeSettingReadsStale(queryClient);
    queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
};

export const liveUpdateService = {
    // The tab's one live connection, for every reader (§UI20.10 rule 1). It reads no sign-in
    // state, because every reader's connection is the same (§SEC14.8 rule 2), and it tells the
    // reader nothing about the connection (§UI20.10 rule 4).
    useLiveUpdates: (): void => {
        // One broker for as long as the hook is mounted. A broker made on every render, as the
        // app's other hooks make theirs, would hand the effect a new one each time, and the
        // effect would stop the connection and start it again on every render of its holder.
        const [liveUpdateBroker] = useState(() => new LiveUpdateBroker());
        const queryClient = useQueryClient();

        useEffect(() => {
            let isMounted = true;
            let isFirstStart = true;
            let previousRetryCount = 0;
            let startAgainTimeoutId: number | undefined;

            // Once unmounted it starts nothing: not on the close its own stop causes, nor on a
            // start that stop cuts short, both of which reach here as any other failure would.
            const startAgainLater = () => {
                if (isMounted === false) {
                    return;
                }

                startAgainTimeoutId = window.setTimeout(
                    () => void startAsync(),
                    nextRetryDelayInMilliseconds(previousRetryCount));

                previousRetryCount++;
            };

            const listeners: LiveUpdateListeners = {
                onLiveUpdate: liveUpdate => makeStaleWhatTheMessageCovers(queryClient, liveUpdate),
                onReconnected: () => makeEveryCoveredReadStale(queryClient),
                onClosed: startAgainLater
            };

            // A first start that succeeds at once makes nothing stale, because the pages it
            // serves have just read. Every later start brings back a connection that was away.
            // A start that succeeds begins the schedule afresh for the next failure, as
            // SignalR's own reconnection does.
            const startAsync = async (): Promise<void> => {
                const isComingBack = isFirstStart === false;
                isFirstStart = false;

                try {
                    await liveUpdateBroker.startLiveUpdatesAsync(
                        listeners,
                        nextRetryDelayInMilliseconds);

                    previousRetryCount = 0;

                    if (isComingBack) {
                        makeEveryCoveredReadStale(queryClient);
                    }
                } catch {
                    startAgainLater();
                }
            };

            void startAsync();

            // A stop that fails is let go, as every failure of the connection is, with nothing
            // announced (§UI20.10 rule 4): the tab is leaving the connection either way.
            return () => {
                isMounted = false;
                window.clearTimeout(startAgainTimeoutId);
                liveUpdateBroker.stopLiveUpdatesAsync().catch(() => undefined);
            };
        }, [liveUpdateBroker, queryClient]);
    }
};
