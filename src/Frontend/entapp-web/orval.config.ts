import { defineConfig } from "orval";

export default defineConfig({
  entapp: {
    input: {
      // Backend Swagger endpoint'i
      target: "http://localhost:5000/swagger/v1/swagger.json",
    },
    output: {
      // Üretilen dosyaların konumu
      target: "./src/api/generated.ts",
      client: "axios",
      mode: "tags-split",
      schemas: "./src/api/models",
      prettier: true,

      override: {
        mutator: {
          path: "./src/lib/api-client.ts",
          name: "apiClient",
        },
      },
    },
    hooks: {
      afterAllFilesWrite: "prettier --write",
    },
  },
});
