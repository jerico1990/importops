import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const proxyTarget = env.VITE_API_PROXY_TARGET || "http://localhost:5090";

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src")
      }
    },
    server: {
      port: 5091,
      proxy: {
        "/api": {
          target: proxyTarget,
          changeOrigin: true,
          secure: false
        },
        "/health": {
          target: proxyTarget,
          changeOrigin: true,
          secure: false
        }
      }
    }
  };
});
