import { useEffect, useState } from "react";

// Tracks the browser's connectivity so the installed PWA can show a clear offline state
// instead of letting cached pages silently look live. navigator.onLine seeds the initial
// value; the online/offline events keep it current without polling.
export const useOnlineStatus = (): boolean => {
    const [isOnline, setIsOnline] = useState(navigator.onLine);

    useEffect(() => {
        const onOnline = () => setIsOnline(true);
        const onOffline = () => setIsOnline(false);

        window.addEventListener("online", onOnline);
        window.addEventListener("offline", onOffline);

        return () => {
            window.removeEventListener("online", onOnline);
            window.removeEventListener("offline", onOffline);
        };
    }, []);

    return isOnline;
};
