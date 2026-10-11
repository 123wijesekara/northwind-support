import { loadEnv } from 'vite';
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// The dev server proxies /api to the backend so the browser only ever talks to one origin.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: env.VITE_API_PROXY_TARGET ?? 'https://localhost:7043',
          changeOrigin: true,
          // The backend uses the ASP.NET development certificate, which is self-signed.
          secure: false,
        },
      },
    },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: './src/test/setup.ts',
      css: false,
      // Windows: Vitest 4 rewrote its pools. `poolOptions` is gone, so the
      // equivalent settings live at the top level now.
      pool: 'threads',
      maxWorkers: 1,
      // Keep isolation on: shared JSDOM between files leaks renders into later tests.
      isolate: true,
      testTimeout: 20_000,
      hookTimeout: 30_000,
    },
  };
});