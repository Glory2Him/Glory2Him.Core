import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "Glory2Him.WebApp.React";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
    env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://localhost:7059';

// The single source of truth for the installed app's chrome colour — consumed by both the
// manifest below and the injected <meta name="theme-color"> plugin, so the two can't drift.
const themeColor = '#6c5440';

export default defineConfig({
    plugins: [
        plugin(),
        {
            // vite-plugin-pwa injects the <link rel="manifest"> tag itself, but not a
            // theme-color meta tag, so this used to be a second hand-authored literal in
            // index.html that could silently drift from the manifest's own theme_color.
            name: 'inject-pwa-theme-color',
            transformIndexHtml() {
                return [{
                    tag: 'meta',
                    attrs: { name: 'theme-color', content: themeColor },
                    injectTo: 'head',
                }];
            },
        },
        VitePWA({
            // Dev keeps hot reload; the service worker only ships in production builds.
            devOptions: { enabled: false },
            registerType: 'autoUpdate',
            manifest: {
                name: 'Glory 2 Him',
                short_name: 'Glory 2 Him',
                description: 'Glory 2 Him - Sharing the Gospel',
                start_url: '/',
                display: 'standalone',
                background_color: '#ffffff',
                theme_color: themeColor,
                icons: [
                    { src: '/pwa/pwa-192x192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
                    { src: '/pwa/pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
                    { src: '/pwa/pwa-maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
                ],
            },
            workbox: {
                // The SPA's own build output, minus the admin-only sample/reference-doc pages:
                // samplePagesRoutes.tsx already code-splits every demo page — and the coreUI
                // components/helpers currently reachable ONLY from that lazy-loaded tree, which
                // Rollup gives their own chunks too — out of the public bundle, so ordinary
                // visitors never download them. Precaching them here would silently undo that
                // split for every installed visitor.
                //
                // Matched by chunk name, not source path (Workbox precaches Vite's OUTPUT files,
                // which don't carry their source directory), so this list has to be kept in step
                // with samplePagesRoutes.tsx by hand: a new demo page, or a new component used
                // only by one, needs adding here too. After changing either, re-run `npm run
                // build` and check the "precache N entries" summary for a stray chunk whose name
                // doesn't appear in this list.
                globPatterns: ['**/*.{js,css,html,ico,png,svg,webmanifest}'],
                globIgnores: [
                    '**/assets/*{Doc,Sample,Playground}-*.{js,css}',
                    '**/assets/{samplePagesIndex,heroBanner,authCard,podcastCard,postLargeCard,'
                    + 'postOverlayCard,postTypeBadge,sampleShell,sampleFormats,sampleArticleBody,'
                    + 'useSamplePosts,contentItemDemoData,contentItemShapeSamples,'
                    + 'securityContextDemo}-*.js',
                ],
                navigateFallback: '/index.html',
                navigateFallbackDenylist: [
                    /^\/api\//,
                    // Real full-page POST navigations (an OAuth challenge/link-provider form),
                    // not SPA routes — NavigationRoute matches on request.mode alone, not
                    // method, so without this the SPA shell fallback answers the POST instead of
                    // letting it reach the server. See externalLoginPicker.tsx and
                    // account/manage/externalLogins.tsx.
                    /^\/Account\/PerformExternalLogin/,
                    /^\/Account\/Manage\/LinkExternalLogin/,
                ],
                clientsClaim: true,
                skipWaiting: true,
                runtimeCaching: [
                    {
                        // Same-origin urlPattern regexes are matched by Workbox against the
                        // request's FULL href (scheme+host+path) via regExp.exec(url.href), never
                        // just the pathname — a `^`-anchored pattern like /^\/api\// can never
                        // match a string that starts with "https://", so this has to be a
                        // pathname-matching function instead.
                        //
                        // Scoped to the one config read the shell needs at boot, not every
                        // /api/* route: issue #463 explicitly scopes this PR to the shell only
                        // ("do not attempt offline reading of content"), and a blanket /api/*
                        // match would both violate that scope (silently caching post/content
                        // reads) and cache authenticated per-user responses (e.g.
                        // /api/accounts/me) with no cache-clear on logout.
                        urlPattern: ({ url }) => url.pathname === '/api/frontend-configurations',
                        handler: 'NetworkFirst',
                        options: {
                            cacheName: 'api-cache',
                            networkTimeoutSeconds: 10,
                            cacheableResponse: { statuses: [200] },
                            expiration: { maxEntries: 10, maxAgeSeconds: 60 * 60 * 24 },
                        },
                    },
                    {
                        // The Blogzine theme's CSS/JS/vendor assets and Profile-Image reads: same
                        // proxying rules as the dev server (see server.proxy below), served by the
                        // ASP.NET Core host at the same origin in production. Without these cached,
                        // an installed app that opens offline renders the shell unstyled. Matched
                        // on pathname for the same reason as the rule above.
                        urlPattern: ({ url }) => /^\/(assets|Profile-Image)\//.test(url.pathname),
                        handler: 'StaleWhileRevalidate',
                        options: {
                            cacheName: 'host-assets',
                            expiration: { maxEntries: 200, maxAgeSeconds: 60 * 60 * 24 * 30 },
                        },
                    },
                    {
                        urlPattern: /^https:\/\/fonts\.googleapis\.com\//,
                        handler: 'StaleWhileRevalidate',
                        options: { cacheName: 'google-fonts-stylesheets' },
                    },
                    {
                        urlPattern: /^https:\/\/fonts\.gstatic\.com\//,
                        handler: 'CacheFirst',
                        options: {
                            cacheName: 'google-fonts-webfonts',
                            cacheableResponse: { statuses: [0, 200] },
                            expiration: { maxAgeSeconds: 60 * 60 * 24 * 365, maxEntries: 30 },
                        },
                    },
                ],
            },
        }),
    ],
    build: {
        rollupOptions: {
            output: {
                manualChunks: {
                    react: ['react', 'react-dom', 'react-router-dom'],
                    query: ['@tanstack/react-query', '@tanstack/react-query-devtools'],
                    bootstrap: ['react-bootstrap', 'react-toastify'],
                }
            }
        }
    },
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api/*': { target, secure: false },
            '^/assets/*': { target, secure: false },
            '^/Profile-Image/*': { target, secure: false }
        },
        port: 6080,
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    }
})
