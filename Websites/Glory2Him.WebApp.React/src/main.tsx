import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'

// vite.config.ts's service worker (registerType: 'autoUpdate', clientsClaim/skipWaiting) claims
// an already-open tab the moment a new version activates, without reloading it — left alone, a
// tab open across a deploy keeps running the OLD bundle against the NEW worker, and its next
// code-split chunk request (a filename no longer in the fresh precache) 404s. Reloading once on
// controllerchange picks up the fresh index and chunk names instead, the same fix
// samplePagesRoutes.tsx already applies to a failed lazy-chunk fetch.
//
// clientsClaim also fires controllerchange the moment a visitor's VERY FIRST service worker
// finishes installing — there is no prior bundle to be stale against, so reloading then would
// just interrupt a first-time visitor for no reason. hadController, captured before any claim
// can happen, tells the two cases apart: only a page that already had a controller is a
// same-tab update landing on top of an older one.
//
// The sessionStorage guard is deliberately never cleared: it caps auto-reloads at ONE per tab
// session (sessionStorage outlives a reload and is only reset when the tab closes), so a worker
// that keeps flipping can page-load Whac-A-Mole with itself once and then stop, rather than
// looping — clearing it on every load (as an earlier version of this code did) would have reset
// the guard right back to "eligible" on the very page the reload just landed on.
//
// try/catch because this runs before root.render, outside the React tree and any error
// boundary — sessionStorage throwing here (a sandboxed iframe with no allow-same-origin, some
// private-browsing configurations) would otherwise abort the whole boot with a blank page,
// rather than just losing this one PWA-update convenience.
try {
    if ('serviceWorker' in navigator) {
        const swReloadGuardKey = 'g2h-sw-reload';
        const hadController = Boolean(navigator.serviceWorker.controller);

        navigator.serviceWorker.addEventListener('controllerchange', () => {
            if (!hadController || sessionStorage.getItem(swReloadGuardKey) != null) {
                return;
            }

            try {
                sessionStorage.setItem(swReloadGuardKey, 'reloading');
            } catch {
                // Best effort — still reload below even if the guard itself can't be persisted.
            }

            window.location.reload();
        });
    }
} catch {
    // No PWA-update reload this session; the app still boots normally below.
}

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);

root.render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);
