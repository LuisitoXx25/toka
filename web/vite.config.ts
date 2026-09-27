import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// The browser only ever calls its own origin (/api). In development this proxy adds the API key on the
// server side, like nginx does in Docker, so the key never reaches the bundle.
// API_URL and API_KEY are read from the shell or web/.env.local; they have no VITE_ prefix on purpose,
// which keeps Vite from embedding them in client code.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  return {
    plugins: [react()],
    build: { sourcemap: false },
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: env.API_URL ?? 'http://localhost:8080',
          changeOrigin: true,
          headers: { 'X-Api-Key': env.API_KEY ?? '' },
        },
      },
    },
  }
})
