import { useEffect, useState } from 'react';
import LiveUpdateBroker from '../../brokers/hubBroker.liveUpdates';

export const liveUpdateService = {
    useLiveUpdates: (): void => {
        // One broker for as long as the hook is mounted. A broker made on every render, as the
        // app's other hooks make theirs, would hand the effect a new one each time, and the
        // effect would stop the connection and start it again on every render of its holder.
        const [liveUpdateBroker] = useState(() => new LiveUpdateBroker());

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
