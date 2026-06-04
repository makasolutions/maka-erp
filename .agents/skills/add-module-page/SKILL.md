# SKILL: add-module-page
# Receta para crear una página de módulo completa con MakaGrid, filtros e indicadores
# Versión: 1.0 | Mayo 2026
# Guardar en: .agents/skills/add-module-page/SKILL.md

---

## QUÉ HACE ESTE SKILL

Crea una página de listado completa siguiendo el patrón estándar Maka:
- Header con título, contador y botón de acción
- Pills de filtro rápido
- Barra de búsqueda + filtros avanzados
- Indicadores KPI sobre el grid
- MakaGrid con datos del servidor
- Mobile card para pantallas pequeñas
- i18n completo (ES + EN)
- Compatible con temas light/dark y los 6 acentos

---

## PREREQUISITOS

Antes de ejecutar este skill:
1. El endpoint `GET /api/v1/{module}/{entities}` debe existir y retornar 200
2. El DTO de respuesta debe estar definido en los Contracts
3. Las traducciones base del namespace deben existir en `public/locales/es/`

---

## ESTRUCTURA DE ARCHIVOS A CREAR

```
clients/dashboard/src/pages/{module}/
├── {entities}.tsx          ← página principal (este skill)
└── {entity}-detail.tsx     ← detalle (skill separado)

public/locales/es/{module}.json   ← agregar keys nuevas
public/locales/en/{module}.json   ← agregar keys nuevas
```

---

## TEMPLATE DE PÁGINA

```tsx
// src/pages/{module}/{entities}.tsx
// Reemplazar: {Module}, {Entity}, {EntityDto}, {entities}, {entity}

import { useState, useCallback } from "react";
import { useTranslation } from "react-i18next";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { ColumnModel } from "@syncfusion/ej2-react-grids";
import { MakaGrid } from "@/components/maka";
import { useLocalization } from "@/hooks/use-localization";
import { useAuth } from "@/auth/use-auth";
import { EntityPageHeader } from "@/components/shared/entity-shell";
import { EntityAuditSection } from "@/components/shared/entity-audit-section";
import { api{Entity}Client } from "@/api/{module}";
import type { {EntityDto}, Create{Entity}Dto } from "@/api/{module}.types";

// ── Tipos del estado de editor ──────────────────────────────────────
type EditorState =
  | { mode: "closed" }
  | { mode: "create" }
  | { mode: "edit"; item: {EntityDto} }
  | { mode: "delete"; item: {EntityDto} };

// ── Componente principal ─────────────────────────────────────────────
export function {Entities}Page() {
  const { t } = useTranslation("{module}");
  const { formatCurrency, formatDateTime } = useLocalization();
  const { user } = useAuth();
  const queryClient = useQueryClient();

  // Permisos
  const canCreate = (user?.permissions ?? []).includes("Permissions.{Module}.{Entities}.Create");
  const canEdit   = (user?.permissions ?? []).includes("Permissions.{Module}.{Entities}.Update");
  const canDelete = (user?.permissions ?? []).includes("Permissions.{Module}.{Entities}.Delete");

  // Estado del editor
  const [editor, setEditor] = useState<EditorState>({ mode: "closed" });

  // Filtros
  const [search, setSearch]         = useState("");
  const [statusFilter, setStatus]   = useState<string>("all");
  const [page, setPage]             = useState(1);
  const pageSize = 20;

  // ── Query ──────────────────────────────────────────────────────────
  const query = useQuery({
    queryKey: ["{entities}", { search, statusFilter, page, pageSize }],
    queryFn: () => api{Entity}Client.list({ search, statusFilter, page, pageSize }),
    staleTime: 30_000,
  });

  const items    = query.data?.items ?? [];
  const total    = query.data?.totalCount ?? 0;
  const active   = items.filter(i => i.isActive).length;
  const inactive = items.filter(i => !i.isActive).length;

  // ── Columnas del grid ──────────────────────────────────────────────
  const columns: ColumnModel[] = [
    {
      field: "name",
      headerText: t("{entities}.columns.name"),
      width: "240",
    },
    {
      field: "isActive",
      headerText: t("common:status.label"),
      width: "120",
      template: (row: {EntityDto}) => (
        <span
          className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
          style={{
            background: row.isActive
              ? "var(--color-success-subtle)"
              : "var(--color-bg-tertiary)",
            color: row.isActive
              ? "var(--color-success)"
              : "var(--color-text-secondary)",
          }}
        >
          {row.isActive ? t("common:status.active") : t("common:status.inactive")}
        </span>
      ),
    },
    {
      field: "createdAt",
      headerText: t("common:table.createdAt"),
      width: "160",
      template: (row: {EntityDto}) => (
        <span style={{ color: "var(--color-text-secondary)", fontSize: "0.8rem" }}>
          {formatDateTime(row.createdAt)}
        </span>
      ),
    },
  ];

  // ── Render ─────────────────────────────────────────────────────────
  return (
    <div className="space-y-4 p-6">

      {/* Header */}
      <EntityPageHeader
        title={t("{entities}.title")}
        count={total}
        action={canCreate ? {
          label: t("{entities}.create"),
          onClick: () => setEditor({ mode: "create" }),
        } : undefined}
      />

      {/* Pills de filtro rápido */}
      <div className="flex gap-2 flex-wrap">
        {["all", "active", "inactive"].map(f => (
          <button
            key={f}
            onClick={() => { setStatus(f); setPage(1); }}
            className="px-3 py-1 rounded-full text-sm transition-colors"
            style={{
              background: statusFilter === f
                ? "var(--color-accent)"
                : "var(--color-bg-tertiary)",
              color: statusFilter === f
                ? "white"
                : "var(--color-text-secondary)",
            }}
          >
            {t(`common:status.${f}`)}
          </button>
        ))}
      </div>

      {/* Búsqueda */}
      <div className="flex gap-3 items-center">
        <div className="relative flex-1 max-w-sm">
          <input
            value={search}
            onChange={e => { setSearch(e.target.value); setPage(1); }}
            placeholder={t("{entities}.search")}
            className="w-full pl-9 pr-3 py-2 rounded-lg text-sm"
            style={{
              background: "var(--color-bg-secondary)",
              border: "1px solid var(--color-border-secondary)",
              color: "var(--color-text-primary)",
            }}
          />
        </div>
      </div>

      {/* Indicadores KPI — SIEMPRE presentes */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        {[
          { label: t("common:table.total"),    value: total,    color: "var(--color-text-primary)" },
          { label: t("common:status.active"),  value: active,   color: "var(--color-success)" },
          { label: t("common:status.inactive"),value: inactive, color: "var(--color-text-secondary)" },
        ].map(kpi => (
          <div
            key={kpi.label}
            className="rounded-xl px-4 py-3"
            style={{ background: "var(--color-bg-secondary)" }}
          >
            <div className="text-xs" style={{ color: "var(--color-text-tertiary)" }}>
              {kpi.label}
            </div>
            <div className="text-xl font-bold mt-0.5" style={{ color: kpi.color }}>
              {kpi.value}
            </div>
          </div>
        ))}
      </div>

      {/* Grid */}
      <MakaGrid<{EntityDto}>
        dataSource={items}
        columns={columns}
        isLoading={query.isLoading}
        fileName="{entities}"
        onRowClick={canEdit
          ? (row) => setEditor({ mode: "edit", item: row })
          : undefined}
      />

      {/* Dialogs de CRUD — ver skill add-feature/SKILL.md */}
      {/* Create / Edit / Delete dialogs aquí */}

    </div>
  );
}
```

---

## REGLAS DE APLICACIÓN

```
✅ Los indicadores KPI SIEMPRE están presentes encima del grid
✅ Los pills de filtro siempre incluyen "Todos" como primera opción
✅ El botón de crear solo aparece si el usuario tiene el permiso
✅ El MakaGrid recibe isLoading={query.isLoading} — nunca null
✅ onRowClick solo se pasa si el usuario tiene permiso de editar
✅ Todos los colores usan var(--color-*) — nunca hex ni tailwind de color
✅ Las traducciones existen en ES y EN antes del commit
✅ El search hace debounce de 300ms para no saturar la API
✅ Al cambiar cualquier filtro: resetear page a 1
```

---

## TRADUCCIONES MÍNIMAS REQUERIDAS

```json
// es/{module}.json — agregar estas keys:
{
  "{entities}": {
    "title": "...",
    "create": "Crear ...",
    "search": "Buscar por nombre...",
    "columns": {
      "name": "Nombre",
      "status": "Estado",
      "createdAt": "Creado"
    }
  }
}

// en/{module}.json — mismas keys en inglés
```
