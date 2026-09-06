import { useEffect, useRef } from 'react';

// THE FRESHNESS CHANNEL (design §20.6.1). The panel never fetches or subscribes — that section
// puts freshness on the consumer — and until now no consumer did anything about it: a vote cast
// elsewhere, a comment resolved elsewhere, or an auto-approval firing was invisible to an open,
// focused tab until it was reloaded. SignalR over the EventHighway facts the workflow already
// publishes (§10.17) is the design's intended channel; this hook is the documented first cut —
// polling — kept in its own file so a later SignalR channel replaces only this one.
//
// POLLING ONLY RUNS WHILE THE TAB IS VISIBLE. A hidden tab gains nothing from a background
// fetch; it only spends the moderator's data and the server's time on a screen nobody is
// reading. Coming back to a hidden tab, or coming back online, may have missed a change the poll
// would otherwise have sat on for up to a full interval — both refresh immediately instead of
// waiting the interval out.
//
// REFETCH ON RECONNECT IS NOT OPTIONAL (§20.6.1): a message missed while the network was down
// must not leave a stale panel showing a decision control for a round that has since closed.
// TanStack Query already refetches on the browser's 'online' event by default, but that is an
// inherited library default rather than a stated contract — the 'online' listener here restates
// it explicitly, for the round as a whole, so the requirement is pinned by this hook's own test
// rather than by an unread default.
export const useApprovalRoundChanges = (
    entityId: string,
    refresh: () => void,
    intervalMs = 15 * 1000
): void => {
    // A ref, not a dependency: refresh is re-created on every render of the caller (it closes
    // over query state), and re-running the effect on every one of those renders would tear
    // down and rebuild the interval and the listeners for no reason.
    const refreshRef = useRef(refresh);
    refreshRef.current = refresh;

    useEffect(() => {
        if (entityId.length === 0) {
            return;
        }

        const refreshIfVisible = () => {
            if (document.visibilityState === 'visible') {
                refreshRef.current();
            }
        };

        const intervalId = window.setInterval(refreshIfVisible, intervalMs);
        document.addEventListener('visibilitychange', refreshIfVisible);
        window.addEventListener('online', refreshIfVisible);

        return () => {
            window.clearInterval(intervalId);
            document.removeEventListener('visibilitychange', refreshIfVisible);
            window.removeEventListener('online', refreshIfVisible);
        };
    }, [entityId, intervalMs]);
};
