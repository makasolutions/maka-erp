import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const apiBase = env.VITE_API_BASE_URL ?? "https://localhost:7030";

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
    server: {
      port: 5174,
      strictPort: true,
      proxy: {
        // SSE stream — this rule MUST come before the generic /api rule so
        // Vite applies the socket-close handler only to long-lived SSE
        // connections.  Without it Vite's http-proxy keeps the backend
        // connection open after the browser tab is closed or hard-refreshed,
        // because http-proxy does not destroy the proxyReq when the client
        // socket closes for streaming responses.  Over time these "zombie"
        // backend connections accumulate, preventing ASP.NET Core's
        // HttpContext.RequestAborted from firing and keeping
        // SseConnectionManager entries alive indefinitely.
        "/api/v1/sse": {
          target: apiBase,
          changeOrigin: true,
          secure: false,
          configure: (proxy) => {
            proxy.on("proxyReq", (proxyReq, req) => {
              req.socket.on("close", () => proxyReq.destroy());
            });
          },
        },
        // ws: true forwards the WebSocket upgrade used by SignalR's hub
        // transport at /api/v1/realtime/hub. Without it the negotiate
        // succeeds over HTTP but the WS upgrade falls into Vite's own
        // dev server, so the chat status stalls on "CONNECTING" while
        // SignalR retries forever.
        "/api": { target: apiBase, changeOrigin: true, secure: false, ws: true },
        "/openapi": { target: apiBase, changeOrigin: true, secure: false },
        "/scalar": { target: apiBase, changeOrigin: true, secure: false },
        // Health probes live at the root (not under /api). Without this the
        // dashboard's /system/health page 404s in dev because Vite serves
        // the request itself instead of proxying to the API.
        "/health": { target: apiBase, changeOrigin: true, secure: false },
        // Dev-only: local storage presigned upload receiver (PUT /local-upload/{token}).
        // Only active when Storage:Provider = "local" on the backend.
        "/local-upload": { target: apiBase, changeOrigin: true, secure: false },
        // Local storage serves public files at /tenants/... (server-relative publicUrl).
        // Proxy them so product/brand images render in dev.
        "/tenants": { target: apiBase, changeOrigin: true, secure: false },
      },
    },
  };
});
