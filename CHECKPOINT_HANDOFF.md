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

**Objetivo:** limpiar el módulo Catalog v1, reconstruir con modelo v2, compilar limpio.

| Paso | Descripción | Commit |
|------|-------------|--------|
| 1 | Eliminar entidades, migraciones y features v1 | `05423d7b` |
| 2 | Reconstruir Domain/ con 18 entidades v2 | `3ea6208c` |
| 3 | Configuraciones EF Core v2 (índices únicos, relaciones) | `d5343814` |
| 4 | Migración `InitialCatalogV2` aplicada en BD | `e32788c4` |
| 5 | Seed: TaxRates + ShippingClasses + Brands + Categories | `cf10c893` |
| 6 | Features mínimas: GetBrands + GetCategories (query+handler+endpoint+validator) | `1d3f8cc9` |
| 7 | Frontend alineado con v2 (catalog.ts, pages compilando sin errores) | `304d4fbf` |

**Estado de la BD (schema `catalog`, 23 tablas):**
- `root`, `acme`, `globex`: 3 TaxRates + 3 ShippingClasses cada uno
- `acme`, `globex`: 5 Brands (Sony, DJI, Godox, Nanlite, Blackmagic) + 16 Categories

**Endpoints activos en el backend:**
- `GET /api/v1/catalog/brands` → `PagedResponse<BrandDto>` (permisos: Catalog.Brands.View)
- `GET /api/v1/catalog/categories` → `IReadOnlyList<CategoryDto>` árbol (permisos: Catalog.Categories.View)

**Estado frontend:**
- `clients/dashboard/src/api/catalog.ts` → tipos v2. Campos v1 (`sku`, `price`, `stock`, `code`,
  `isVisible`) marcados `@deprecated` y opcionales para compat con páginas existentes.
- `brands.tsx`, `categories.tsx`, `products.tsx`, `product-detail.tsx` → compilan sin errores TS.
- Las páginas funcionan con los endpoints que existen; los que faltan (CRUD) retornan 404 hasta C2/C3/C4.

---

## 4. TAREA ACTIVA — FASE C2: FEATURES BACKEND COMPLETAS DE BRANDS

### Objetivo
Implementar el CRUD completo de Brands en el backend. Al final de C2:
- Todos los endpoints de Brand funcionan y están en Scalar
- Las páginas `brands.tsx` del dashboard operan completamente
- Tests de los nuevos handlers pasando

### Plan completo Fase C2 (orden de implementación)

```
Paso C2.1 — GetBrandById
  Query + Handler + Endpoint: GET /api/v1/catalog/brands/{id}
  Retorna: BrandDto o 404 NotFoundException

Paso C2.2 — CreateBrand
  Command + Validator + Handler + Endpoint: POST /api/v1/catalog/brands
  Input:
    name (required, max 200)
    slug (auto-generado si no se envía, único global, lowercase, URL-safe)
    description? (max 2000)
    logoUrl? (max 500, debe ser URL válida si se envía)
    websiteUrl? (max 500)
    countryOfOrigin? (exactamente 2 chars uppercase ISO-2)
    isActive (default true)
  Validación extra: slug debe ser único en la BD (excluyendo IsDeleted=true)
  Retorna: 201 Created con el Guid del brand nuevo
  Domain event: ninguno en C2 (se agrega en C5 para sync WooCommerce)

Paso C2.3 — UpdateBrand
  Command + Validator + Handler + Endpoint: PUT /api/v1/catalog/brands/{id}
  Mismos campos que Create + brandId en ruta
  Validación: slug único excluyendo el propio id
  Retorna: 200 con Guid

Paso C2.4 — DeleteBrand (soft delete)
  Command + Handler + Endpoint: DELETE /api/v1/catalog/brands/{id}
  Usa ISoftDeletable — setea IsDeleted=true, DeletedOnUtc, DeletedBy
  No se puede eliminar si tiene Products asociados (verificar antes de borrar)
  Retorna: 204 No Content

Paso C2.5 — RestoreBrand
  Command + Handler + Endpoint: POST /api/v1/catalog/brands/{id}/restore
  Revierte soft delete. Falla con NotFoundException si no está borrada.
  Retorna: 200 con Guid

Paso C2.6 — ListTrashedBrands
  Query + Validator + Handler + Endpoint: GET /api/v1/catalog/brands/trash
  Paginado. Filtra WHERE IsDeleted = true (IgnoreQueryFilters necesario)
  Retorna: PagedResponse<BrandDto>
```

### Reglas de implementación para C2

```
- Estructura de archivos: src/Modules/Catalog/Modules.Catalog/Features/v1/Brands/{Feature}/
- Contracts en: src/Modules/Catalog/Modules.Catalog.Contracts/v1/Brands/{Feature}/
- Registrar cada endpoint nuevo en CatalogModule.MapEndpoints()
- NO AutoMapper — mapeo manual Brand → BrandDto con método estático
  (crear src/Modules/Catalog/Modules.Catalog/Extensions/BrandExtensions.cs)
- Handlers: DbContext directo, AsNoTracking() en queries de lectura
- DeleteBrand: verificar Products ANTES de borrar
  → si tiene products activos: lanzar CustomException(400)
- ListTrashedBrands: requiere .IgnoreQueryFilters() para ver registros soft-deleted
- Permiso por endpoint:
  GetBrandById   → CatalogPermissions.Brands.View
  CreateBrand    → CatalogPermissions.Brands.Create
  UpdateBrand    → CatalogPermissions.Brands.Update
  DeleteBrand    → CatalogPermissions.Brands.Delete
  RestoreBrand   → CatalogPermissions.Brands.Restore
  ListTrashed    → CatalogPermissions.Brands.View
```

### Convención de slug

```csharp
// Generar slug a partir del nombre si no se envía
slug = string.IsNullOrWhiteSpace(input.Slug)
    ? input.Name.ToLowerInvariant()
              .Normalize(NormalizationForm.FormD)
              // quitar diacríticos
              .Replace(" ", "-")
              // quitar chars no ASCII-safe
              .Trim('-')
    : input.Slug.ToLowerInvariant().Trim();

// Verificar unicidad (excluyendo soft-deleted):
bool slugExists = await db.Brands
    .AsNoTracking()
    .Where(b => !b.IsDeleted && b.Slug == slug && b.Id != excludeId)
    .AnyAsync(ct);
if (slugExists) throw new CustomException("Slug ya existe", statusCode: HttpStatusCode.Conflict);
```

### Paso C2.7 — Frontend brands.tsx (al final, una vez que el backend compila)

```
- Actualizar createBrand y updateBrand en catalog.ts para enviar slug auto-generado
- Quitar campo "Code" del formulario de brands.tsx (ya no existe en v2)
- Agregar campos opcionales: websiteUrl, countryOfOrigin (si se quieren en C2)
- Verificar que trash/restore funciona en la UI
- QA: crear → editar → eliminar → restaurar → confirmar en DevTools Network tab
- Light mode + Dark mode antes de commit
```

---

## 5. CONTEXTO DE FASES FUTURAS (para referencia, no implementar ahora)

```
Fase C3: Features backend completas de Categories
  (CRUD + árbol recursivo + reordenamiento + move subtree)

Fase C4: Features backend completas de Products
  (la más compleja — variaciones, códigos, imágenes, price lists)

Fase C5: Frontend completo (una página a la vez, QA antes de cada una)
  Orden: brands → categories → products

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

# 3. ¿Qué features existen en Brands?
find src/Modules/Catalog/Modules.Catalog/Features/v1/Brands -name "*.cs" | sort

# 4. ¿Qué contracts existen en Brands?
find src/Modules/Catalog/Modules.Catalog.Contracts/v1/Brands -name "*.cs" | sort

# 5. ¿Compila el backend?
dotnet build src/FSH.Starter.slnx 2>&1 | tail -4

# 6. ¿Compila el frontend?
cd clients/dashboard && npm run build 2>&1 | tail -3

# 7. ¿Cuántos brands hay en la BD?
# (en Adminer http://localhost:8081 o docker exec)
# SELECT "TenantId", COUNT(*) FROM catalog."Brands" GROUP BY "TenantId";

# 8. ¿Los endpoints de brands están en Scalar?
# Abrir https://localhost:7030/scalar y buscar "catalog/brands"
```

Con esos resultados, identifica exactamente en qué Paso de C2 estás y continúa.

---

## 7. INSTRUCCIÓN DE CONTINUACIÓN

Una vez verificado el estado, responde con este formato:

```
[QA] Verificación del estado actual:
- Rama: [resultado]
- Últimos commits: [lista]
- Features en Brands/: [lista de carpetas/archivos]
- Build backend: [✅ limpio / ❌ N errores]
- Build frontend: [✅ limpio / ❌ N errores]
- Endpoints Brands en Scalar: [✅ visible / ❌ no encontrado]

Estado: [Fase C1 completa / estoy en el Paso C2.N de Fase C2]
Continúo con: [descripción de lo que sigue]
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

✅ QA Checklist completo antes de cada commit de feature:
   backend: build → endpoint en Scalar → curl de prueba → respuesta 2xx
   frontend: build → browser → flujo completo → 0 errores Console/Network
```

---

## 9. ESTRUCTURA DE ARCHIVOS DE REFERENCIA (estado actual)

```
src/Modules/Catalog/
├── Modules.Catalog.Contracts/
│   ├── Authorization/CatalogPermissions.cs  ← permisos ya definidos para Brands/Categories/Products
│   ├── CatalogContractsMarker.cs
│   └── v1/
│       ├── Brands/
│       │   └── GetBrands/
│       │       ├── BrandDto.cs
│       │       └── GetBrandsQuery.cs
│       ├── Categories/
│       │   └── GetCategories/
│       │       ├── CategoryDto.cs
│       │       └── GetCategoriesQuery.cs
│       └── GetCatalogHealthQuery.cs
│
└── Modules.Catalog/
    ├── CatalogModule.cs                     ← MapEndpoints aquí
    ├── Data/
    │   ├── CatalogDbContext.cs
    │   ├── CatalogDbInitializer.cs          ← SeedAsync ya implementado
    │   └── Configurations/                  ← 11 archivos de config EF
    ├── Domain/                              ← 18 entidades v2
    │   ├── Brand.cs
    │   ├── Category.cs
    │   ├── TaxRate.cs, ShippingClass.cs
    │   ├── CatalogAttribute.cs, CatalogJoins.cs
    │   ├── Product.cs, ProductVariation.cs
    │   ├── ProductCode.cs, ProductImage.cs, ProductTag.cs
    │   ├── ProductCategory.cs, ProductAttribute.cs, ProductBundleItem.cs
    │   ├── PriceList.cs, PriceListItem.cs, PriceListItemHistory.cs
    │   ├── TenantProduct.cs, SupplierRelations.cs
    │   └── ShippingClass.cs
    └── Features/v1/
        ├── Brands/
        │   └── GetBrands/
        │       ├── GetBrandsQueryHandler.cs
        │       ├── GetBrandsQueryValidator.cs
        │       └── GetBrandsEndpoint.cs
        ├── Categories/
        │   └── GetCategories/
        │       ├── GetCategoriesQueryHandler.cs
        │       └── GetCategoriesEndpoint.cs
        └── GetCatalogHealthQueryHandler.cs
```

---

*Checkpoint generado: Junio 2026*
*Sesión anterior terminó por límite de contexto (~1M tokens)*
*Fase C1 completada al 100% — próxima: Fase C2 (CRUD completo de Brands)*
