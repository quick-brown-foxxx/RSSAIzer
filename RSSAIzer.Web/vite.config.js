import { defineConfig } from "vite";

export default defineConfig({
  root: ".", // project root
  base: "/build/",
  build: {
    outDir: "wwwroot/build", // final files end up here
    manifest: "manifest.json",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        main: "wwwroot/js/site.js", // entry point
      },
    },
  },
});
