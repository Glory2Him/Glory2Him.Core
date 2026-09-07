import { useEffect, useState } from "react";
import { NETWORK_REACHABLE_EVENT, NETWORK_UNREACHABLE_EVENT } from "../brokers/apiBroker";

// Tracks the browser's connectivity so the installed PWA can show a clear offline state
// instead of letting cached pages silently look live. navigator.onLine seeds the initial
// value; the online/offline events keep it current without polling.
//
// navigator.onLine only reports whether a network adapter is connected, not whether this
// origin is actually reachable — a captive portal or a downed API host both leave it stuck at
// `true`. apiBroker.ts raises NETWORK_REACHABLE_EVENT/NETWORK_UNREACHABLE_EVENT from real
// request outcomes (a response reaching the app at all vs. a request that never got one), which
// catches exactly what the adapter-level signal misses.
export const useOnlineStatus = (): boolean => {
    const [isOnline, setIsOnline] = useState(navigator.onLine);

    useEffect(() => {
        const onOnline = () => setIsOnline(true);
        const onOffline = () => setIsOnline(false);

        window.addEventListener("online", onOnline);
        window.addEventListener("offline", onOffline);
        window.addEventListener(NETWORK_REACHABLE_EVENT, onOnline);
        window.addEventListener(NETWORK_UNREACHABLE_EVENT, onOffline);

        return () => {
            window.removeEventListener("online", onOnline);
            window.removeEventListener("offline", onOffline);
            window.removeEventListener(NETWORK_REACHABLE_EVENT, onOnline);
            window.removeEventListener(NETWORK_UNREACHABLE_EVENT, onOffline);
        };
    }, []);

    return isOnline;
};
