import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The SPA talks to the API through this proxy, so the API needs no CORS configuration.
// Override the target with ASTROLAB_API_URL (e.g. http://localhost:8080 for the Docker container).
const apiTarget = process.env.ASTROLAB_API_URL ?? 'http://localhost:5279';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/openapi': { target: apiTarget, changeOrigin: true },
    },
  },
  preview: {
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/openapi': { target: apiTarget, changeOrigin: true },
    },
  },
});
