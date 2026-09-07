import { useOnlineStatus } from "../../hooks/useOnlineStatus";

// The installed PWA's precached shell still renders with the network off, which would
// otherwise look indistinguishable from a live page. This makes the offline state explicit
// rather than leaving the reader to guess why nothing updates.
export default function OfflineBanner() {
    const isOnline = useOnlineStatus();

    if (isOnline) {
        return null;
    }

    return (
        <div className="alert alert-warning rounded-0 mb-0 text-center py-2" role="status">
            <i className="bi bi-wifi-off me-2" aria-hidden="true"></i>
            You&apos;re offline. Showing what&apos;s already been loaded.
        </div>
    );
}
