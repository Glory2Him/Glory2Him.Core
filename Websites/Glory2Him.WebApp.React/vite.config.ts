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

export default defineConfig({
    plugins: [
        plugin(),
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
                theme_color: '#6c5440',
                icons: [
                    { src: '/pwa/pwa-192x192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
                    { src: '/pwa/pwa-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
                    { src: '/pwa/pwa-maskable-512x512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
                ],
            },
            workbox: {
                // The SPA's own build output only; the Blogzine host assets below are not part
                // of it and are cached at runtime instead.
                globPatterns: ['**/*.{js,css,html,ico,png,svg,webmanifest}'],
                navigateFallback: '/index.html',
                navigateFallbackDenylist: [/^\/api\//],
                clientsClaim: true,
                skipWaiting: true,
                runtimeCaching: [
                    {
                        // /api/* must never resolve from a stale cache while the network is up.
                        urlPattern: /^\/api\//,
                        handler: 'NetworkFirst',
                        options: {
                            cacheName: 'api-cache',
                            networkTimeoutSeconds: 10,
                            cacheableResponse: { statuses: [0, 200] },
                        },
                    },
                    {
                        // The Blogzine theme's CSS/JS/vendor assets and Profile-Image reads: same
                        // proxying rules as the dev server (see server.proxy below), served by the
                        // ASP.NET Core host at the same origin in production. Without these cached,
                        // an installed app that opens offline renders the shell unstyled.
                        urlPattern: /^\/(assets|Profile-Image)\//,
                        handler: 'StaleWhileRevalidate',
                        options: { cacheName: 'host-assets' },
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
