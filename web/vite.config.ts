import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { resolve } from "path";

export default defineConfig({
  plugins: [react()],
  base: "./",
  build: {
    outDir: "dist",
    emptyOutDir: true,
    lib: {
      entry: resolve(__dirname, "src/mount.tsx"),
      formats: ["es"],
      fileName: "ksp-solar-map",
    },
    rollupOptions: {
      output: {
        entryFileNames: "assets/ksp-solar-map.js",
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
    },
  },
});
