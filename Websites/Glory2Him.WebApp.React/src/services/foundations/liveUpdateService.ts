import { useEffect } from 'react';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';

export const liveUpdateService = {
    useLiveUpdates: (): void => {
        const liveUpdateBroker = new LiveUpdateBroker();

        useEffect(() => {
            void liveUpdateBroker.startLiveUpdatesAsync(
                {
                    onLiveUpdate: () => undefined,
                    onReconnected: () => undefined,
                    onClosed: () => undefined
                },
                () => null);

            return () => {
                void liveUpdateBroker.stopLiveUpdatesAsync();
            };
        }, [liveUpdateBroker]);
    }
};
