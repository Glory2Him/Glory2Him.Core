import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App'

// vite.config.ts's service worker (registerType: 'autoUpdate', clientsClaim/skipWaiting) claims
// an already-open tab the moment a new version activates, without reloading it — left alone, a
// tab open across a deploy keeps running the OLD bundle against the NEW worker, and its next
// code-split chunk request (a filename no longer in the fresh precache) 404s. Reloading once on
// controllerchange picks up the fresh index and chunk names instead, the same fix
// samplePagesRoutes.tsx already applies to a failed lazy-chunk fetch. Guarded through
// sessionStorage, cleared on every fresh load, so a worker that flips repeatedly can't loop the
// page — mirrors that same file's reloadGuardKey pattern.
if ('serviceWorker' in navigator) {
    const swReloadGuardKey = 'g2h-sw-reload';
    sessionStorage.removeItem(swReloadGuardKey);

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (sessionStorage.getItem(swReloadGuardKey) != null) {
            return;
        }

        sessionStorage.setItem(swReloadGuardKey, 'reloading');
        window.location.reload();
    });
}

const root = ReactDOM.createRoot(document.getElementById('root') as HTMLElement);

root.render(
    <React.StrictMode>
        <App />
    </React.StrictMode>
);
