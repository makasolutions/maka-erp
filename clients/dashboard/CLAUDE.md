# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Context

This is the React dashboard for the Maka ERP platform. It communicates with `FSH.Starter.Api` (runs on `http://localhost:5030`). The Vite dev server runs on port 5174 and proxies `/api`, `/health`, `/openapi`, `/scalar` to the API. See root `CLAUDE.md` for the full platform context.

## Commands

```bash
npm run dev          # Vite dev server on port 5174 (reads .env.local)
npm run build        # tsc -b && vite build (must pass before committing)
npm run lint         # ESLint (typescript-eslint + react-hooks + jsx-a11y)
npm run preview      # Preview production build on port 4174
npm run test:e2e     # Playwright end-to-end tests
npm run test:e2e:ui  # Playwright with interactive UI
```

**Build is the gate.** Always run `npm run build` after changes — the dashboard uses `tsc -b` strict mode and a type error here is a real error.

## Architecture

### Runtime config

`public/config.json` is loaded at boot by `src/env.ts` via `loadRuntimeConfig()` (called in `main.tsx` before React mounts). This sets `env.apiBase` and `env.defaultTenant`. In dev, `VITE_API_BASE_URL` in `.env.local` controls the Vite proxy target (must point to `http://localhost:5030`).

### Auth flow

`src/auth/auth-context.tsx` → `AuthProvider` wraps the app. JWT and refresh token live in `src/auth/token-store.ts` (sessionStorage). `src/lib/api-client.ts` (`apiFetch`) automatically injects `Authorization` and `tenant` headers; on 401 it deduplicates a single refresh call then retries. Root-tenant logins send `X-FSH-App: admin`; all other tenants send `X-FSH-App: dashboard`.

### Data fetching

All API calls go through `apiFetch` (never raw `fetch`). TanStack Query v5 manages caching, deduplication, and background refetch. Query keys follow `[module, resource, params]` convention (e.g. `["catalog", "products", { search, page }]`). Mutations call `queryClient.invalidateQueries` on success.

### Design system

Tailwind v4 with CSS custom properties (`var(--color-*)`) in oklch colour space. **Never hardcode colours** — always use the tokens defined in `src/styles/globals.css`. Token categories: `--color-primary`, `--color-destructive`, `--color-success`, `--color-warning`, `--color-muted`, `--color-border`, `--color-ring`, `--color-card`, `--color-accent`.

### Component layers

| Layer | Path | Purpose |
|---|---|---|
| Primitives | `src/components/ui/` | Radix-based: Button, Input, Dialog, DropdownMenu, Badge, etc. |
| Entity shell | `src/components/list/` | Reusable list-page building blocks (see below) |
| Layout | `src/components/layout/` | AppShell, Sidebar, Topbar, MobileNav |
| Maka wrappers | `src/components/maka/` | Syncfusion components pre-configured for COP/Bogotá/es |
| Pages | `src/pages/` | One file per page; dialogs live in the same file |

### Entity shell pattern

Every list page reuses components from `src/components/list/entity-shell.tsx`:
- `EntityPageHeader` — icon tile + title + count + action buttons
- `EntityListCard` / `EntityListHeader` / `EntityListRow` — CSS-Grid table (no `<table>` element)
- `EntityMobileCard` — touch-friendly card for `< md`
- `EntityEmpty` / `EntityListLoading` / `EntityPager`
- `EntityStatusBadge` — tone-tinted pill (success/warning/danger/info)

Editor state in each page uses a discriminated union: `{ mode: "closed" } | { mode: "create" } | { mode: "edit"; item: T } | ...`. All dialogs for that page live in the same file.

### Syncfusion

Licensed via `VITE_SYNCFUSION_LICENSE` in `.env.local` (gitignored). CSS is imported in `main.tsx` using the `bootstrap5` theme. Syncfusion components are wrapped in `src/components/maka/` — always use `MakaGrid`, `MakaChart`, etc. instead of raw Syncfusion components. The wrappers set locale (`es`), formatting (COP), and Maka design tokens.

### SSE / real-time

`src/sse/sse-context.tsx` manages the SSE connection to `/api/v1/sse/stream`. Components subscribe via `useSse()`. Events drive cache invalidation and notification badges — do not poll when an SSE event is available.

### i18n

`src/i18n.ts` initialises i18next with `i18next-http-backend` (lazy-loads from `public/locales/{lang}/{ns}.json`), `i18next-browser-languagedetector`, fallback `es`. Import `useTranslation` from `react-i18next`; pick the namespace matching the page module (`catalog`, `inventory`, `orders`, `crm`, `common`, `settings`). New translatable strings go in the appropriate namespace JSON first.

## Key constraints (from root CLAUDE.md)

- **Syncfusion only** — MudBlazor is purged; no other UI component libraries.
- MassTransit v8.5.7 Apache 2.0 — do not upgrade to v9.
- Real-time uses SSE, **not** SignalR.
- API docs use Scalar, **not** Swagger.
- New Maka module pages follow the entity-shell pattern above.

## REGLAS PROHIBIDAS — Dashboard

### Componentes Syncfusion obligatorios

🚫 **PROHIBIDO:** crear o modificar componentes UI complejos sin usar los wrappers `Maka*` de `src/components/maka/`.

| Necesitas | Usar |
|---|---|
| Tabla / listado de datos | `MakaGrid` |
| Gráfica / chart | `MakaChart` |
| Tablero Kanban | `MakaKanban` |
| Tabla pivote / analytics | `MakaPivot` |
| Calendario / agenda | `MakaScheduler` |
| Input de texto | `SfTextBox` |
| Select / dropdown | `SfDropDownList` |
| Date picker | `SfDatePicker` |
| Botones, badges, cards, avatars, skeleton | Componentes Tailwind nativos existentes en `src/components/ui/` — **no reemplazar** |

### i18n obligatorio — traducciones completas

🚫 **PROHIBIDO:** agregar texto visible al usuario sin `t()` de i18next y sin crear la key en **todos** los idiomas configurados.

**Proceso obligatorio** para cualquier texto nuevo:

1. Identificar el namespace: `common`, `catalog`, `inventory`, `orders`, `crm`, `settings`, `billing`, `logistics`, `warranties`, `imports`, `whatsapp`, `hr`
2. Agregar la key en español → `public/locales/es/[namespace].json`
3. Agregar la traducción en inglés → `public/locales/en/[namespace].json`
4. Si existen más carpetas en `public/locales/` → agregar en **todas** sin excepción
5. Recién entonces usar `t('namespace:key')` en el componente

Reglas adicionales:
- NUNCA dejar una key sin traducción en algún idioma configurado
- NUNCA usar strings de texto directos en JSX/TSX, aunque sea "solo temporal"
- NUNCA usar el namespace `common` para strings específicos de un módulo
- Si no se conoce la traducción exacta en inglés, usar una aproximación razonable y agregar `// TODO: review translation`

### Compatibilidad con sistema de temas

🚫 **PROHIBIDO:** crear componentes que no respeten el sistema de temas del dashboard.

**1. Nunca hardcodear colores.** Usar siempre tokens CSS de `src/styles/globals.css`:

```css
/* Texto */
var(--color-foreground)            var(--color-muted-foreground)

/* Superficies */
var(--color-background)            var(--color-card)
var(--color-popover)               var(--color-muted)

/* Bordes */
var(--color-border)                var(--color-input)
var(--color-ring)

/* Acento (cambia según preferencia del usuario) */
var(--color-primary)               var(--color-primary-foreground)
var(--color-accent)

/* Semánticos */
var(--color-destructive)           var(--color-success)
var(--color-warning)               var(--color-info)

/* Charts */
var(--color-chart-1) … var(--color-chart-5)   var(--color-saffron)
```

**2. Nunca usar clases Tailwind de color fijas:** `bg-white`, `text-black`, `bg-gray-100`, `text-gray-600`, `border-gray-200`, etc. Usar las clases semánticas existentes: `text-foreground`, `text-muted-foreground`, `bg-card`, `border-border`, etc.

**3. Syncfusion usa canvas** — no lee CSS vars automáticamente. Resolver colores en `useEffect`/mount:
```ts
const color = getComputedStyle(document.documentElement)
  .getPropertyValue('--color-chart-1').trim();
```
Patrón de referencia: `src/components/maka/MakaChart.tsx` — seguirlo exactamente.

**4. Verificación obligatoria antes de commit:** el componente debe verse correctamente en:
- Tema Light **y** Dark (toggle con el selector de tema en Topbar)
- Con al menos 2 acentos diferentes (rose, indigo, emerald, etc.)

**5. Syncfusion + dark mode:** si un componente Syncfusion no respeta dark mode, agregar CSS override:
```css
[data-theme="dark"] .e-grid { ... }
[data-theme="dark"] .e-kanban { ... }
[data-theme="dark"] .e-schedule { ... }
```

## DEUDA TÉCNICA CONOCIDA

### FILE UPLOAD — storage no configurado
- **Síntoma:** uploads fallan con error CORS local:// o similar
- **Afecta:** `/files` (Mis Archivos), `/settings/profile` (foto de usuario)
- **Causa probable:** MinIO fue eliminado del docker-compose. El módulo Files del backend no tiene storage configurado.
- **Impacto:** bajo en esta fase (no es funcionalidad core)
- **Resolver en:** auditoría del módulo Files
- **NO intentar corregir hasta que se audite el módulo completo**
