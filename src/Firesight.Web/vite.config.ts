import { defineConfig } from 'vite'
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
})
