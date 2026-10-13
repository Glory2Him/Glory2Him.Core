import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';
import { LiveUpdateListeners, LiveUpdateType } from '../../models/foundations/liveUpdates/liveUpdate';
import { invalidateContentItemSettingReads } from './contentItemSettingService';

// SignalR's documented default reconnection delays, then the last of them again without end, so
// a connection that will not come back is never given up while the tab is open.
const retryDelaysInMilliseconds = [0, 2000, 10000, 30000];

const nextRetryDelayInMilliseconds = (previousRetryCount: number): number =>
    retryDelaysInMilliseconds[Math.min(previousRetryCount, retryDelaysInMilliseconds.length - 1)];

export const liveUpdateService = {
    useLiveUpdates: (): void => {
        // One broker for as long as the hook is mounted. A broker made on every render, as the
        // app's other hooks make theirs, would hand the effect a new one each time, and the
        // effect would stop the connection and start it again on every render of its holder.
        const [liveUpdateBroker] = useState(() => new LiveUpdateBroker());
        const queryClient = useQueryClient();

        useEffect(() => {
            const makeEveryCoveredReadStale = () => {
                invalidateContentItemSettingReads(queryClient);
                queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
            };

            const listeners: LiveUpdateListeners = {
                onLiveUpdate: liveUpdate => {
                    if (liveUpdate.type === LiveUpdateType.ContentItemSettingChanged) {
                        invalidateContentItemSettingReads(queryClient);
                        queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
                    } else {
                        queryClient.invalidateQueries({
                            queryKey: ['ReactionSummaries'],
                            predicate: query =>
                                (query.queryKey[2] as ReadonlyArray<string | null>)
                                    .includes(liveUpdate.contentItemId)
                        });
                    }
                },
                onReconnected: makeEveryCoveredReadStale,
                onClosed: () => startAgainLater()
            };

            let isFirstStart = true;
            let previousRetryCount = 0;

            const startAgainLater = () => {
                window.setTimeout(
                    () => void startAsync(),
                    nextRetryDelayInMilliseconds(previousRetryCount));

                previousRetryCount++;
            };

            const startAsync = async (): Promise<void> => {
                const isComingBack = isFirstStart === false;
                isFirstStart = false;

                try {
                    await liveUpdateBroker.startLiveUpdatesAsync(
                        listeners,
                        nextRetryDelayInMilliseconds);

                    previousRetryCount = 0;

                    if (isComingBack) {
                        makeEveryCoveredReadStale();
                    }
                } catch {
                    startAgainLater();
                }
            };

            void startAsync();

            return () => {
                void liveUpdateBroker.stopLiveUpdatesAsync();
            };
        }, [liveUpdateBroker, queryClient]);
    }
};
