import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';
import { LiveUpdateListeners, LiveUpdateType } from '../../models/foundations/liveUpdates/liveUpdate';
import { invalidateContentItemSettingReads } from './contentItemSettingService';

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
                onClosed: () => undefined
            };

            const startAsync = async (): Promise<void> => {
                try {
                    await liveUpdateBroker.startLiveUpdatesAsync(listeners, () => null);
                    makeEveryCoveredReadStale();
                } catch {
                    window.setTimeout(() => void startAsync(), 0);
                }
            };

            void startAsync();

            return () => {
                void liveUpdateBroker.stopLiveUpdatesAsync();
            };
        }, [liveUpdateBroker, queryClient]);
    }
};
