// vitest/config gives us the same defineConfig as vite, but with the
// `test` option added. Everything below still configures Vite the same
// way it did before.
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'
import { viteStaticCopy } from 'vite-plugin-static-copy'

export default defineConfig({
    plugins: [
        react(),
        // maplibre-gl's worker script (imported directly in WildfireMap.tsx)
        // has its own internal `import ... from "./maplibre-gl-shared.mjs"`.
        // That's a plain string in the raw file, not something Vite's bundler
        // sees or rewrites, so the sibling file has to exist under that exact
        // unhashed name next to the worker in the build output for the
        // worker's own import to resolve at runtime. Copying straight from
        // node_modules keeps it in sync with whatever maplibre-gl version is
        // actually installed.
        viteStaticCopy({
            targets: [
                {
                    src: 'node_modules/maplibre-gl/dist/maplibre-gl-shared.mjs',
                    dest: 'assets',
                    rename: { stripBase: true },
                },
            ],
        }),
    ],
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
