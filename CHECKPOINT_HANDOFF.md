# CHECKPOINT HANDOFF — Maka Omni-Commerce
# Fecha: Junio 2026
# Para: nueva sesión de Claude Code
# Propósito: retomar el trabajo exactamente donde quedó

---

## TL;DR — QUÉ HACER AL LEER ESTE ARCHIVO

1. Lee las secciones en orden
2. Lee los archivos del proyecto listados en §1
3. Verifica el estado real con los comandos de §6
4. Continúa con la tarea pendiente de §7
5. NO preguntes nada que ya esté respondido aquí

---

## 1. ARCHIVOS DE CONTEXTO — LEER ANTES DE TOCAR CÓDIGO

```
CLAUDE.md                                    ← reglas globales (raíz del proyecto)
AGENTS.md                                    ← convenciones FSH
.agents/rules/database.md
.agents/rules/api-conventions.md
.agents/rules/frontend/dashboard.md
.agents/rules/model-routing.md               ← cuándo usar Sonnet/Opus/Haiku
.agents/rules/modules/catalog.md             ← spec completo del módulo Catalog
.agents/rules/modules/parties.md             ← spec completo del módulo Parties
.agents/skills/add-module-page/SKILL.md      ← receta para páginas con MakaGrid
.agents/skills/add-syncfusion-grid/SKILL.md
.agents/skills/add-translation/SKILL.md
```

---

## 2. CONTEXTO DEL PROYECTO

**Producto:** Maka Omni-Commerce Ecosystem — ERP + CRM + WhatsApp Omnicanal
**Empresa:** Maka Solutions SAS / Tecnoimportaciones (Bogotá + Medellín)
**Stack:**
- Backend: .NET 10 / C# 14 / FSH (FullStackHero) boilerplate / PostgreSQL
- Frontend: React 19 + Vite + TanStack Query + Syncfusion React 33.1.44
- Infra: Docker (PostgreSQL 17, Redis, RabbitMQ) — todo en maka_erp_dev
- Auth: Finbuckle MultiTenant + ASP.NET Identity + JWT
- Bus: MassTransit 8.5.7 (Apache 2.0 — NO actualizar a v9, es comercial)

**Repo:** `C:\jann\maka-erp` / rama `develop`
**Dos clientes React:**
- `clients/admin/` → consola super-admin (gestión de tenants, billing, usuarios root)
- `clients/dashboard/` → app operativa diaria (todo el ERP, corre en puerto 5174)

---

## 3. LO QUE YA ESTÁ HECHO

### Infraestructura y ambiente ✅
- Docker Compose con PostgreSQL 17, Redis, RabbitMQ, Adminer
- .NET 10 SDK, dotnet-ef 10.0.8, Node 24, npm
- Syncfusion React 33.1.44 instalado con licencia activa
- i18next configurado con namespaces ES + EN
- Sistema de localización por tenant (timezone, moneda, idioma, formato)
- Reglas de model routing documentadas

### Frontend — dashboard ✅
- MudBlazor eliminado completamente
- 5 wrappers Maka* creados: MakaGrid, MakaChart, MakaKanban, MakaPivot, MakaScheduler
- Página `/maka-components` de prueba (DEV ONLY)
- Módulo de Auditoría mejorado: filtros dinámicos, diff antes/después, EntityAuditSection
- Settings → Localización (timezone, moneda, idioma por tenant)
- Settings → Apariencia (tema light/dark, acento, tipografía — ya existía en FSH)
- i18n en todas las páginas existentes (0 strings hardcodeados)
- Sistema de temas con tokens CSS (var(--color-*) en todo)

### Specs de módulos ✅
- `CATALOG_MODULE_SPEC.md` v2.1 (en `.agents/rules/modules/catalog.md`)
- `PARTIES_MODULE_SPEC.md` v1.1 (en `.agents/rules/modules/parties.md`)
- `MODEL_ROUTING_RULES.md` (reglas de selección de modelo)

### ✅ FASE C1 — COMPLETADA AL 100%

| Paso | Descripción | Commit |
|------|-------------|--------|
| 1 | Eliminar entidades, migraciones y features v1 | `05423d7b` |
| 2 | Reconstruir Domain/ con 18 entidades v2 | `3ea6208c` |
| 3 | Configuraciones EF Core v2 (índices únicos, relaciones) | `d5343814` |
| 4 | Migración `InitialCatalogV2` aplicada en BD | `e32788c4` |
| 5 | Seed: TaxRates + ShippingClasses + Brands + Categories | `cf10c893` |
| 6 | Features mínimas: GetBrands + GetCategories (query+handler+endpoint+validator) | `1d3f8cc9` |
| 7 | Frontend alineado con v2 (catalog.ts, pages compilando sin errores) | `304d4fbf` |

### ✅ FASE C2 — COMPLETADA AL 100%

**Objetivo:** CRUD completo de Brands en backend + frontend brands.tsx v2.

| Paso | Descripción | Commit |
|------|-------------|--------|
| C2.1 | GetBrandById — GET /api/v1/catalog/brands/{id} | `e4ad6a94` |
| C2.2 | CreateBrand — POST /api/v1/catalog/brands (slug auto-gen, unicidad, validación) | `e4ad6a94` |
| C2.3 | UpdateBrand — PUT /api/v1/catalog/brands/{id} | `e4ad6a94` |
| C2.4 | DeleteBrand — DELETE /api/v1/catalog/brands/{id} (soft, bloquea si tiene productos) | `e4ad6a94` |
| C2.5 | RestoreBrand — POST /api/v1/catalog/brands/{id}/restore | `e4ad6a94` |
| C2.6 | ListTrashedBrands — GET /api/v1/catalog/brands/trash (paginado, IgnoreQueryFilters) | `e4ad6a94` |
| C2.7 | Frontend brands.tsx — eliminar v1 (code, isVisible), agregar websiteUrl/countryOfOrigin, panel papelera | `34d79e17` |

**Estado de endpoints activos:**
- `GET    /api/v1/catalog/brands`              → PagedResponse<BrandDto>
- `GET    /api/v1/catalog/brands/{id}`         → BrandDetailDto
- `POST   /api/v1/catalog/brands`              → 201 Guid
- `PUT    /api/v1/catalog/brands/{id}`         → 200 Guid
- `DELETE /api/v1/catalog/brands/{id}`         → 204
- `POST   /api/v1/catalog/brands/{id}/restore` → 200 Guid
- `GET    /api/v1/catalog/brands/trash`        → PagedResponse<TrashedBrandDto>
- `GET    /api/v1/catalog/categories`          → IReadOnlyList<CategoryDto> árbol

---

## 4. TAREA ACTIVA — FASE C3: FEATURES BACKEND COMPLETAS DE CATEGORIES

### Objetivo
Implementar el CRUD completo de Categories en el backend. Al final de C3:
- Todos los endpoints de Category funcionan y están en Scalar
- La página `categories.tsx` del dashboard opera completamente
- Tests de los nuevos handlers pasando

### Plan completo Fase C3 (orden de implementación)

```
Paso C3.1 — GetCategoryById
  Query + Handler + Endpoint: GET /api/v1/catalog/categories/{id}
  Retorna: CategoryDetailDto o 404 NotFoundException

Paso C3.2 — CreateCategory
  Command + Validator + Handler + Endpoint: POST /api/v1/catalog/categories
  Input:
    name (required, max 200)
    slug (auto-gen si no se envía, único global, lowercase)
    description? (max 2000)
    imageUrl? (max 500, URL válida)
    parentId? (Guid? — si se envía, debe existir y no estar borrado)
    sortOrder (default 0)
    isActive (default true)
  Retorna: 201 Created con Guid

Paso C3.3 — UpdateCategory
  Command + Validator + Handler + Endpoint: PUT /api/v1/catalog/categories/{id}
  Mismos campos que Create + categoryId en ruta
  Validación: slug único excluyendo el propio id
  Retorna: 200 con Guid

Paso C3.4 — DeleteCategory (soft delete)
  Command + Handler + Endpoint: DELETE /api/v1/catalog/categories/{id}
  No se puede eliminar si tiene sub-categorías activas O productos activos
  Retorna: 204 No Content

Paso C3.5 — RestoreCategory
  Command + Handler + Endpoint: POST /api/v1/catalog/categories/{id}/restore
  Revierte soft delete. Falla si no está borrada.
  Retorna: 200 con Guid

Paso C3.6 — ListTrashedCategories
  Query + Validator + Handler + Endpoint: GET /api/v1/catalog/categories/trash
  Paginado. Filtra WHERE IsDeleted = true (IgnoreQueryFilters)
  Retorna: PagedResponse<TrashedCategoryDto>
```

### Reglas de implementación para C3

```
- Estructura de archivos: src/Modules/Catalog/Modules.Catalog/Features/v1/Categories/{Feature}/
- Contracts en: src/Modules/Catalog/Modules.Catalog.Contracts/v1/Categories/{Feature}/
- Registrar cada endpoint nuevo en CatalogModule.MapEndpoints()
- NO AutoMapper — mapeo manual Category → CategoryDto con método estático
  (crear src/Modules/Catalog/Modules.Catalog/Extensions/CategoryExtensions.cs)
- Handlers: DbContext directo, AsNoTracking() en queries de lectura
- DeleteCategory: verificar sub-categorías Y productos ANTES de borrar
- ListTrashedCategories: requiere .IgnoreQueryFilters()
- Reusar BuildSlug de CreateBrandCommandHandler (mover a helper o duplicar)
- Permiso por endpoint:
  GetCategoryById  → CatalogPermissions.Categories.View
  CreateCategory   → CatalogPermissions.Categories.Create
  UpdateCategory   → CatalogPermissions.Categories.Update
  DeleteCategory   → CatalogPermissions.Categories.Delete
  RestoreCategory  → CatalogPermissions.Categories.Restore
  ListTrashed      → CatalogPermissions.Categories.View
```

### Paso C3.7 — Frontend categories.tsx (al final, una vez que el backend compila)

```
- Quitar campo "Code" del formulario categories.tsx (ya no existe en v2)
- Conectar createCategory / updateCategory / deleteCategory con los nuevos endpoints
- Agregar panel de papelera igual al de brands.tsx
- QA: crear → editar → eliminar → restaurar → confirmar en DevTools Network tab
- Light mode + Dark mode antes de commit
```

---

## 5. CONTEXTO DE FASES FUTURAS (para referencia, no implementar ahora)

```
Fase C4: Features backend completas de Products
  (la más compleja — variaciones, códigos, imágenes, price lists)

Fase C5: Frontend completo (una página a la vez, QA antes de cada una)
  Orden: brands (✅ C2) → categories (C3) → products (C4)

Después de Catalog completo:
→ Módulo Parties (spec en .agents/rules/modules/parties.md)
  P1: Geography + catálogos fiscales
  P2: Party base + TenantAccount + maestros contables
  P3: BusinessPartner (clientes, proveedores, crédito)
  P4: Supplier Agreements (convenios)
```

---

## 6. COMANDOS DE VERIFICACIÓN — EJECUTAR PRIMERO

Antes de cualquier acción, ejecuta estos comandos y reporta resultados:

```bash
# 1. ¿En qué rama estás?
git branch --show-current

# 2. ¿Qué se hizo en los últimos commits?
git log --oneline -10

# 3. ¿Qué features existen en Categories?
find src/Modules/Catalog/Modules.Catalog/Features/v1/Categories -name "*.cs" | sort

# 4. ¿Qué contracts existen en Categories?
find src/Modules/Catalog/Modules.Catalog.Contracts/v1/Categories -name "*.cs" | sort

# 5. ¿Compila el backend?
dotnet build src/FSH.Starter.slnx 2>&1 | tail -4

# 6. ¿Compila el frontend?
cd clients/dashboard && npm run build 2>&1 | tail -3
```

Con esos resultados, confirma que C2 está completa e inicia C3.1.

---

## 7. INSTRUCCIÓN DE CONTINUACIÓN

```
[QA] Verificación del estado actual:
- Rama: [resultado]
- Últimos commits: [lista]
- Features en Categories/: [lista de carpetas/archivos]
- Build backend: [✅ limpio / ❌ N errores]
- Build frontend: [✅ limpio / ❌ N errores]

Estado: Fase C2 completa — inicio Fase C3 (CRUD completo de Categories)
Continúo con: C3.1 GetCategoryById
```

---

## 8. REGLAS CRÍTICAS QUE NO DEBEN OLVIDARSE

```
❌ NUNCA poner Price, Stock o Sku directamente en la entidad Product
   → Price va en PriceListItem (referencia VariationId)
   → Stock va en módulo Inventory (futuro)
   → Sku va en ProductVariation (única por variación)

❌ NUNCA campo TenantId manual en entidades — lo maneja Finbuckle

❌ NUNCA AutoMapper — mapeo manual con extension methods estáticos en
   src/Modules/Catalog/Modules.Catalog/Extensions/

❌ NUNCA Repository Pattern genérico — DbContext directo en handlers

❌ NUNCA MudBlazor — Syncfusion únicamente

❌ NUNCA strings hardcodeados en JSX — siempre t() con traducciones ES+EN

❌ NUNCA colores hex/rgb en CSS — siempre var(--color-*)

❌ NUNCA commitear con errores en Console o Network del browser

✅ OwnerId = Guid? en entidades del Catalog (preparado para SaaS, no activo)

✅ ISoftDeletable en Brand, Category, Product, ProductVariation

✅ AsNoTracking() en TODAS las queries de lectura

✅ IgnoreQueryFilters() en queries de trash (soft-deleted records)

✅ CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.X)
   — CustomException NO tiene overload (string, statusCode) directo;
     siempre pasar IEnumerable<string> como segundo argumento.

✅ ICommand<Guid> para todos los commands (no ICommand sin tipo de retorno)

✅ QA Checklist completo antes de cada commit de feature:
   backend: build → endpoint en Scalar → curl de prueba → respuesta 2xx
   frontend: build → browser → flujo completo → 0 errores Console/Network
```

---

## 9. ESTRUCTURA DE ARCHIVOS DE REFERENCIA (estado tras C2)

```
src/Modules/Catalog/
├── Modules.Catalog.Contracts/
│   ├── Authorization/CatalogPermissions.cs
│   ├── CatalogContractsMarker.cs
│   └── v1/
│       ├── Brands/
│       │   ├── GetBrands/       BrandDto.cs + GetBrandsQuery.cs
│       │   ├── GetBrandById/    BrandDetailDto.cs + GetBrandByIdQuery.cs
│       │   ├── CreateBrand/     CreateBrandCommand.cs
│       │   ├── UpdateBrand/     UpdateBrandCommand.cs
│       │   ├── DeleteBrand/     DeleteBrandCommand.cs
│       │   ├── RestoreBrand/    RestoreBrandCommand.cs
│       │   └── ListTrashedBrands/ ListTrashedBrandsQuery.cs + TrashedBrandDto.cs
│       ├── Categories/
│       │   └── GetCategories/   CategoryDto.cs + GetCategoriesQuery.cs
│       └── GetCatalogHealthQuery.cs
│
└── Modules.Catalog/
    ├── CatalogModule.cs          ← MapEndpoints (todos los Brands registrados)
    ├── Data/
    │   ├── CatalogDbContext.cs
    │   ├── CatalogDbInitializer.cs
    │   └── Configurations/       11 archivos
    ├── Domain/                   18 entidades v2
    └── Features/v1/
        ├── Brands/
        │   ├── GetBrands/
        │   ├── GetBrandById/
        │   ├── CreateBrand/      Handler + Validator + Endpoint
        │   ├── UpdateBrand/      Handler + Validator + Endpoint
        │   ├── DeleteBrand/      Handler + Endpoint
        │   ├── RestoreBrand/     Handler + Endpoint
        │   └── ListTrashedBrands/ Handler + Validator + Endpoint
        ├── Categories/
        │   └── GetCategories/
        └── GetCatalogHealthQueryHandler.cs
```

---

*Checkpoint actualizado: Junio 2026*
*Fase C2 completada al 100% — próxima: Fase C3 (CRUD completo de Categories)*
