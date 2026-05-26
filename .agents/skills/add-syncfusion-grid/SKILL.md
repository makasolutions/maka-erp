---
name: add-syncfusion-grid
description: Add a MakaGrid table to a dashboard page — API module, TanStack Query hook, MakaGrid with typed columns, i18n, and dark-mode-safe colors. Use for any list/table page in clients/dashboard.
argument-hint: [PageName] [Resource] [namespace]
---

# Add Syncfusion Grid (MakaGrid)

`MakaGrid` is the **only** way to render tabular data in the dashboard. Never use `GridComponent` directly.
Read `.agents/rules/frontend/dashboard.md` first.

## Step 1 — API module (`src/api/{resource}.ts`)

```ts
import { apiFetch } from "@/api/api-fetch";

export type {Resource}Dto = {
  id: string;
  name: string;
  // … mirror the backend DTO
};

export async function get{Resources}(): Promise<{Resource}Dto[]> {
  return apiFetch<{Resource}Dto[]>("/api/v1/{module}/{resources}");
}
```

## Step 2 — TanStack Query hook (inline in the page)

```tsx
const { data = [], isLoading } = useQuery<{Resource}Dto[]>({
  queryKey: ["{resources}"],
  queryFn: get{Resources},
});
```

## Step 3 — Import MakaGrid

```tsx
import { MakaGrid } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { useTranslation } from "react-i18next";
```

## Step 4 — Define columns (always typed, always with i18n headers)

```tsx
const { t } = useTranslation("{namespace}");

const columns: ColumnModel[] = [
  { field: "sku",   headerText: t("{namespace}:{resource}.sku"),  width: 120 },
  { field: "name",  headerText: t("{namespace}:{resource}.name"), minWidth: 200 },
  { field: "price", headerText: t("{namespace}:{resource}.price"), width: 130,
    format: "C0", textAlign: "Right" },
];
```

## Step 5 — Render MakaGrid

```tsx
<MakaGrid
  dataSource={data}
  columns={columns}
  isLoading={isLoading}
  fileName="{resource}-export"
  height="calc(100vh - 200px)"
  onRowClick={(row) => navigate(`/{resources}/${row.id}`)}
/>
```

## Step 6 — Add i18n keys

**es first**, then en. Never skip.

`public/locales/es/{namespace}.json`:
```json
{
  "{resource}": {
    "sku":   "SKU",
    "name":  "Nombre",
    "price": "Precio"
  }
}
```

`public/locales/en/{namespace}.json`:
```json
{
  "{resource}": {
    "sku":   "SKU",
    "name":  "Name",
    "price": "Price"
  }
}
```

## Dark mode

MakaGrid handles dark mode automatically via `[data-theme="dark"] .e-grid { ... }` overrides
in `src/styles/globals.css`. If you add custom cell renderers, use CSS tokens:
- Text: `var(--color-text-primary)`
- Background: `var(--color-bg-primary)`
- Accent: `var(--color-accent)`

## Checklist

- [ ] DTO type defined in `src/api/{resource}.ts`
- [ ] `apiFetch` function returns the correct type
- [ ] `useQuery` with correct `queryKey`
- [ ] All column `headerText` use `t()`
- [ ] Both `es` and `en` locale files updated
- [ ] `MakaGrid` used (not `GridComponent` directly)
- [ ] `isLoading` prop passed
- [ ] `fileName` set for exports
- [ ] Verified in Light and Dark mode
