import { useEffect, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';
import { invalidateContentItemSettingReads } from './contentItemSettingService';

export const liveUpdateService = {
    useLiveUpdates: (): void => {
        // One broker for as long as the hook is mounted. A broker made on every render, as the
        // app's other hooks make theirs, would hand the effect a new one each time, and the
        // effect would stop the connection and start it again on every render of its holder.
        const [liveUpdateBroker] = useState(() => new LiveUpdateBroker());
        const queryClient = useQueryClient();

        useEffect(() => {
            void liveUpdateBroker.startLiveUpdatesAsync(
                {
                    onLiveUpdate: () => {
                        invalidateContentItemSettingReads(queryClient);
                        queryClient.invalidateQueries({ queryKey: ['ReactionSummaries'] });
                    },
                    onReconnected: () => undefined,
                    onClosed: () => undefined
                },
                () => null);

            return () => {
                void liveUpdateBroker.stopLiveUpdatesAsync();
            };
        }, [liveUpdateBroker, queryClient]);
    }
};
