import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// Standalone Vitest config. It deliberately does not merge vite.config.ts: that file has
// dev-server side effects (exporting an ASP.NET dev certificate, reading key files) that
// must not run when the test runner starts. Only the pieces tests need are repeated here.
export default defineConfig({
    plugins: [react()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    test: {
        environment: 'happy-dom',
        globals: false,
        setupFiles: ['./src/tests/setupTests.ts'],
        include: ['src/**/*.test.{ts,tsx}']
    }
});
