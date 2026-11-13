import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import { fileURLToPath, URL } from "url";

export default defineConfig({
    plugins: [vue()],
    resolve: {
        alias: {
            "@": fileURLToPath(new URL("./src", import.meta.url)),
            "@a": fileURLToPath(new URL("./src/api", import.meta.url)),
            "@c": fileURLToPath(new URL("./src/components", import.meta.url)),
            "@v": fileURLToPath(new URL("./src/views", import.meta.url)),
        },
    },
    build: {
        outDir: "build",
        emptyOutDir: true,
        sourcemap: true,
    },
    server: {
        proxy: {
            "/api": { target: "http://localhost:5400", secure: false },
            "/mcp": { target: "http://localhost:5400", secure: false },
            "/hubs": { target: "http://localhost:5400", secure: false, ws: true },
        },
        port: 3004,
        strictPort: true,
        host: "0.0.0.0",
    },
});
