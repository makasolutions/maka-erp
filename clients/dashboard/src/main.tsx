// ── i18n must be the very first import so the instance is ready before React mounts ──
import "@/i18n";

// ── Syncfusion CSS (loaded before our globals so design tokens override) ──────
import "@syncfusion/ej2-base/styles/bootstrap5.css";
import "@syncfusion/ej2-react-grids/styles/bootstrap5.css";
import "@syncfusion/ej2-react-kanban/styles/bootstrap5.css";
// ej2-react-charts: canvas-based, no separate CSS needed
import "@syncfusion/ej2-react-inputs/styles/bootstrap5.css";
import "@syncfusion/ej2-react-popups/styles/bootstrap5.css";
import "@syncfusion/ej2-react-navigations/styles/bootstrap5.css";
import "@syncfusion/ej2-react-dropdowns/styles/bootstrap5.css";
import "@syncfusion/ej2-react-notifications/styles/bootstrap5.css";
import "@syncfusion/ej2-react-calendars/styles/bootstrap5.css";
import "@syncfusion/ej2-react-pivotview/styles/bootstrap5.css";
import "@syncfusion/ej2-react-schedule/styles/bootstrap5.css";
import "@syncfusion/ej2-react-richtexteditor/styles/bootstrap5.css";

// ── Syncfusion Spanish CLDR (month/day names for calendar components) ────────
import "@/lib/syncfusion-cldr";

import { registerLicense } from "@syncfusion/ej2-base";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "@/App";
import { installImpersonationFromHash } from "@/auth/impersonation-handoff";
import { loadRuntimeConfig } from "@/env";
import "@/styles/globals.css";

// Register Syncfusion license — loaded from .env.local (gitignored), never hardcoded
const sfLicense = import.meta.env.VITE_SYNCFUSION_LICENSE as string | undefined;
if (sfLicense) {
  registerLicense(sfLicense);
}

// Runtime config must resolve before React mounts so env.apiBase reads
// inside components see the right value on first paint.
await loadRuntimeConfig();

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element '#root' not found");
}

// Cross-app impersonation handoff — must run BEFORE createRoot so the
// installed token is visible to AuthProvider on first paint. See the
// helper docstring for the why.
installImpersonationFromHash();

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
