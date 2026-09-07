import { useOnlineStatus } from "../../hooks/useOnlineStatus";

// The installed PWA's precached shell still renders with the network off, which would
// otherwise look indistinguishable from a live page. This makes the offline state explicit
// rather than leaving the reader to guess why nothing updates.
//
// Pinned to the viewport rather than left in normal flow: HeaderComponent goes
// position: fixed on its own past 400px of scroll (useStickyHeader.ts), and an in-flow banner
// placed before it would already be scrolled out of view by the time a reader deep in an
// article loses connectivity. The z-index sits one above the header's own sticky z-index
// (1020, set in header.css) so the banner paints over it rather than underneath.
export default function OfflineBanner() {
    const isOnline = useOnlineStatus();

    if (isOnline) {
        return null;
    }

    return (
        <div
            className="alert alert-warning rounded-0 mb-0 text-center py-2 sticky-top"
            style={{ zIndex: 1030 }}
            role="status">
            <i className="bi bi-wifi-off me-2" aria-hidden="true"></i>
            You&apos;re offline. Showing what&apos;s already been loaded.
        </div>
    );
}
