/// <reference types="vitest/globals" />
import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
  test: {
    globals: true,
    include: ["src/**/*.{test,spec}.ts"],
    coverage: {
      reporter: ["text", "json", "html"],
      include: ["src/lib/**/*.ts", "src/stores/**/*.ts"],
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});
