// vitest/config gives us the same defineConfig as vite, but with the
// `test` option added. Everything below still configures Vite the same
// way it did before.
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    optimizeDeps: {
        exclude: ['maplibre-gl'],
    },
    server: {
        proxy: {
            '/api': {
                target: 'http://localhost:5213',
                changeOrigin: true,
            },
        },
    },
    test: {
        environment: 'jsdom',
        setupFiles: ['./src/setupTests.ts'],
    },
})
