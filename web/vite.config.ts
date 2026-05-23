import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig({
  plugins: [react()],
  base: "./",
  define: {
    "process.env.NODE_ENV": JSON.stringify("production"),
    process: JSON.stringify({ env: { NODE_ENV: "production" } }),
  },
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        "ksp-solar-map": resolve(__dirname, "src/mount.tsx"),
        "planet-texture-lab": resolve(__dirname, "src/dev/planet-texture-lab-entry.tsx"),
      },
      output: {
        entryFileNames: "assets/[name].js",
        chunkFileNames: "assets/[name]-[hash].js",
        assetFileNames: "assets/[name][extname]",
      },
    },
  },
  server: {
    port: 5173,
    proxy: {
      "/api": {
        target: "http://127.0.0.1:8750",
        changeOrigin: true,
      },
      "/assets/bodies": {
        target: "http://127.0.0.1:8750",
        changeOrigin: true,
      },
    },
  },
});
