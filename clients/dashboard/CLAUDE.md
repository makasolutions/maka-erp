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

### Formato de moneda — SIEMPRE `formatMoney`

🚫 **PROHIBIDO:** formatear dinero con `Intl.NumberFormat(..., { style: "currency" })` ad-hoc o
con un helper local por archivo.

**Regla de la casa (única fuente):** usar `formatMoney(amount, currency?)` de
`@/lib/list-helpers`. Salida canónica:

- Símbolo **`$` primero**, miles con **`.`**, decimales con **`,`** (agrupación es-CO).
- `formatMoney(12540000)` → `$12.540.000`
- `formatMoney(12540000, "COP")` → `$12.540.000 COP` (el código ISO va **al final**)
- `formatMoney(1299, "USD")` → `$1.299,00 USD`
- **Decimales:** COP (y montos sin código) → **0**; otras monedas → **2** (no redondear centavos extranjeros).
- `formatMoney(x, code, { code: false })` oculta el código en pantallas de una sola moneda.

En grids Syncfusion usar `formatCOP` / `makaCurrencyColumn` de `@/components/maka` (mismo
estilo). En charts seguir `MakaChart.tsx`. Nunca reintroducir un formateador de moneda local.

### Filtros de lista — input/select con ✕ que limpia

🚫 **PROHIBIDO:** crear un input de filtro de texto a mano (`<Input>` suelto) sin la ✕ de limpiar.

- Texto → **`MakaFilterInput`** de `@/components/maka` (input h-8 + ✕ que resetea; hereda el
  comportamiento para **todos** los filtros del sitio).
- Dropdown/select → **`Combobox`** con `clearable` (su ✕ limpia sin abrir el menú).
- Campo de filtro (label + control alineados) → envolver en **`MakaFilterField`** (da nombre
  accesible vía `role="group"` + `aria-labelledby`).
- El panel de filtros/KPIs es **`MakaGridFilters`** (tabs ARIA correctas).

Todo filtro nuevo DEBE reutilizar estos componentes; no reimplementar la lógica de limpiar.

### Formularios en popups — rejilla de 12 columnas

🚫 **PROHIBIDO:** maquetar un formulario de diálogo con grids ad-hoc (`grid-cols-2`,
`grid-cols-[160px_1fr]`, etc.).

- Envolver los campos en **`FormGrid`** de `@/components/list` (rejilla responsive de 12 cols;
  una sola columna en móvil, 12 desde `sm`).
- Dar `span` a cada `Field` (Bootstrap-style). Convención de la casa:
  **código → 4, nombre → 8, slug → 6, descripción → 12**, la mayoría de los demás → **6**.
  Bloques que no son `Field` (switches, secciones) usan `col-span-1 sm:col-span-N` literal.
- Las clases de span son estáticas en `field.tsx` (`SPAN_CLASS`) para que el JIT de Tailwind las
  detecte — **nunca** construir `sm:col-span-${n}` dinámicamente.

**Ancho del diálogo — prop `size` de `DialogContent`:**
- `size="form"` para **forms multi-campo de 2 columnas** → 90vw, cap `sm:max-w-[2048px]` (~2K),
  full-width en móvil, `max-h-[92vh]` con scroll interno. Es el ancho por defecto de todo
  editor con varios campos.
- `size="default"` (o sin prop) para **diálogos de confirmación** (eliminar, impersonar, revocar),
  **forms de 1 campo** y **formularios sensibles angostos por convención** (cambio de contraseña /
  2FA en `settings/security`): NO se ensanchan a 90vw.

Referencia: `pages/catalog/{brands,categories,products}.tsx`, `identity/{users,roles,groups}.tsx`,
`tickets/tickets.tsx`.

