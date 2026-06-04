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

### ✅ FASE C1 — COMPLETADA

| Paso | Descripción | Commit |
|------|-------------|--------|
| 1 | Eliminar entidades, migraciones y features v1 | `05423d7b` |
| 2 | Reconstruir Domain/ con 18 entidades v2 | `3ea6208c` |
| 3 | Configuraciones EF Core v2 (índices únicos, relaciones) | `d5343814` |
| 4 | Migración `InitialCatalogV2` aplicada en BD | `e32788c4` |
| 5 | Seed: TaxRates + ShippingClasses + Brands + Categories | `cf10c893` |
| 6 | Features mínimas: GetBrands + GetCategories | `1d3f8cc9` |
| 7 | Frontend alineado con v2 | `304d4fbf` |

### ✅ FASE C2 — COMPLETADA

CRUD completo de Brands: backend + frontend brands.tsx v2. Commit `e4ad6a94` + `34d79e17`

Endpoints activos:
- `GET/POST /api/v1/catalog/brands`
- `GET/PUT/DELETE /api/v1/catalog/brands/{id}`
- `POST /api/v1/catalog/brands/{id}/restore`
- `GET /api/v1/catalog/brands/trash`

### ✅ FASE C3 — COMPLETADA

CRUD completo de Categories: backend + frontend categories.tsx v2. Commit `8e9c62aa`

Endpoints activos:
- `GET/POST /api/v1/catalog/categories`
- `GET/PUT/DELETE /api/v1/catalog/categories/{id}`
- `POST /api/v1/catalog/categories/{id}/restore`
- `GET /api/v1/catalog/categories/trash`

### ✅ FASE C4 — COMPLETADA

CRUD core de Products (9 endpoints). Commit `ddf7944c`

Nota técnica: `ProductType`, `ProductStatus`, `WeightUnit`, `DimensionUnit` viven en
`src/Modules/Catalog/Modules.Catalog.Contracts/Enums/ProductEnums.cs`
(movidos del Domain para respetar module boundaries — el Domain los importa vía using).

Endpoints activos:
- `GET /api/v1/catalog/products/trash`
- `GET /api/v1/catalog/products`
- `POST /api/v1/catalog/products` (crea variación default para Simple/Service)
- `GET /api/v1/catalog/products/{id}` (incluye Images, Variations, Categories, Tags)
- `PUT /api/v1/catalog/products/{id}`
- `DELETE /api/v1/catalog/products/{id}` (soft)
- `POST /api/v1/catalog/products/{id}/restore`
- `POST /api/v1/catalog/products/{id}/publish` (valida variación activa)
- `POST /api/v1/catalog/products/{id}/archive`

### ✅ FASE C5 — COMPLETADA

Frontend products.tsx reescrito para v2. Commit `949393f9`
- Grid server-side con filtros: nombre, marca, tipo (Simple/Variable/Bundle/Service), estado (Draft/Active/Archived)
- Panel de papelera + restore
- Diálogos: crear, editar, eliminar, publicar, archivar, restaurar
- 0 campos v1 (sin SKU, precio, stock, isVisible en Product)
- Permisos: publish y archive agregados a permissions.ts

---

## 4. TAREA ACTIVA — FASE C6: VARIATIONS BACKEND

### Objetivo
Implementar el CRUD de ProductVariations. Al final de C6:
- Un operador puede agregar/editar/eliminar variaciones a un producto Variable
- Los productos Simple/Service muestran su variación default en el formulario
- Todos los endpoints de Variation funcionan y están en Scalar

### Plan C6 — 5 endpoints

```
C6.1 — GetVariationsByProduct
  GET /api/v1/catalog/products/{productId}/variations
  Retorna: IReadOnlyList<VariationDto> (todas las variaciones del producto, incluidas soft-deleted con flag)

C6.2 — AddVariation
  POST /api/v1/catalog/products/{productId}/variations
  Input: Sku (required, único global), Description?, IsDefault (si true, quita IsDefault a la anterior),
         IsActive (default true), Weight?, WeightUnit?, ImageUrl?, ManageStock (default true),
         AllowBackorders, SoldIndividually, LowStockThreshold?, IsVirtual
  Retorna: 201 con Guid

C6.3 — UpdateVariation
  PUT /api/v1/catalog/products/{productId}/variations/{id}
  Mismos campos que Add (excepto Sku — inmutable después de crear según spec §7)
  Retorna: 200 con Guid

C6.4 — DeleteVariation (soft)
  DELETE /api/v1/catalog/products/{productId}/variations/{id}
  Bloquear si IsDefault = true (el producto Simple necesita al menos 1 variación)
  Retorna: 204

C6.5 — RestoreVariation
  POST /api/v1/catalog/products/{productId}/variations/{id}/restore
  Retorna: 200 con Guid
```

### DTOs necesarios (Contracts)

```csharp
// VariationDto — para GetVariationsByProduct
record VariationDto(
  Guid    Id,
  Guid    ProductId,
  string  Sku,
  string? Description,
  bool    IsDefault,
  bool    IsActive,
  bool    IsDeleted,
  decimal? Weight,
  string? WeightUnit,
  string? ImageUrl,
  bool    ManageStock,
  bool    AllowBackorders,
  bool    SoldIndividually,
  int?    LowStockThreshold,
  bool    IsVirtual,
  int?    WooCommerceId,
  DateTime  CreatedAtUtc,
  DateTime? UpdatedAtUtc);

// AddVariationCommand — Contracts
record AddVariationCommand(
  Guid    ProductId,
  string  Sku,
  string? Description,
  bool    IsDefault = false,
  bool    IsActive = true,
  decimal? Weight = null,
  string?  WeightUnit = null,
  string?  ImageUrl = null,
  bool     ManageStock = true,
  bool     AllowBackorders = false,
  bool     SoldIndividually = false,
  int?     LowStockThreshold = null,
  bool     IsVirtual = false) : ICommand<Guid>;

// UpdateVariationCommand
record UpdateVariationCommand(
  Guid    ProductId,
  Guid    Id,
  string? Description,
  bool    IsActive,
  decimal? Weight,
  string?  WeightUnit,
  string?  ImageUrl,
  bool     ManageStock,
  bool     AllowBackorders,
  bool     SoldIndividually,
  int?     LowStockThreshold,
  bool     IsVirtual) : ICommand<Guid>;
```

### Reglas clave

```
- Rutas anidadas bajo /products/{productId}/variations
- SKU: inmutable después de crear — no incluir en UpdateVariation
- SKU: único global — verificar con IgnoreQueryFilters() para capturar también soft-deleted
- AddVariation: verificar que el productId existe y no está deleted
- DeleteVariation: bloquear si IsDefault = true (usar CustomException 400)
- Permiso: CatalogPermissions.Products.Update para Add/Update/Delete/Restore
- Permiso: CatalogPermissions.Products.View para Get
- Registrar endpoints en CatalogModule con MapGroup anidado:
  var variations = products.MapGroup("/{productId:guid}/variations")
```

---

## 5. CONTEXTO DE FASES FUTURAS

```
Fase C7: ProductCodes backend
  - GET/POST/DELETE /api/v1/catalog/products/{id}/variations/{varId}/codes
  - CodeType: SKU | EAN | UPC | ISBN | GTIN | PartNumber | ManufacturerCode | SupplierCode

Fase C8: PriceLists backend
  - CRUD de PriceList + PriceListItem + GetEffectivePrice
  - CustomerSegment: retail | wholesale | vip | b2b | dropshipping

Fase C9: Frontend Variations (en-línea en products.tsx) + ProductCodes UI

Después de Catalog completo:
→ Módulo Parties (spec en .agents/rules/modules/parties.md)
  P1: Geography + catálogos fiscales
  P2: Party base + TenantAccount + maestros contables
  P3: BusinessPartner (clientes, proveedores, crédito)
  P4: Supplier Agreements (convenios)
```

---

## 6. COMANDOS DE VERIFICACIÓN — EJECUTAR PRIMERO

```bash
# 1. ¿En qué rama estás?
git branch --show-current

# 2. ¿Qué se hizo en los últimos commits?
git log --oneline -10

# 3. ¿Qué features existen en Variations?
find src/Modules/Catalog/Modules.Catalog/Features/v1/Variations -name "*.cs" 2>/dev/null | sort

# 4. ¿Compila el backend?
dotnet build src/FSH.Starter.slnx 2>&1 | grep -E ": error CS" | grep -v MSB302 | head -10

# 5. ¿Compila el frontend?
cd clients/dashboard && npx tsc --noEmit 2>&1 | head -10
```

---

## 7. INSTRUCCIÓN DE CONTINUACIÓN

```
[QA] Verificar estado actual con §6
Estado: Fase C5 completa — inicio Fase C6 (Variations CRUD)
Continúo con: C6.1 GetVariationsByProduct → C6.2 AddVariation → C6.3-C6.5
```

---

## 8. REGLAS CRÍTICAS QUE NO DEBEN OLVIDARSE

```
❌ NUNCA campo TenantId manual en entidades — lo maneja Finbuckle
❌ NUNCA AutoMapper — mapeo manual con extension methods estáticos
❌ NUNCA Repository Pattern genérico — DbContext directo en handlers
❌ NUNCA MudBlazor — Syncfusion únicamente
❌ NUNCA strings hardcodeados en JSX — siempre t() con traducciones ES+EN
❌ NUNCA colores hex/rgb en CSS — siempre var(--color-*)
❌ NUNCA commitear con errores en Console o Network del browser

✅ Enums de Products viven en Contracts/Enums/ProductEnums.cs (NO en Domain)
   Domain los importa via: using FSH.Modules.Catalog.Contracts.Enums;

✅ ISoftDeletable en Brand, Category, Product, ProductVariation

✅ AsNoTracking() en TODAS las queries de lectura

✅ IgnoreQueryFilters() en queries de trash (soft-deleted records)

✅ SKU de variación: ÚNICO GLOBAL — verificar con IgnoreQueryFilters()
   para capturar también SKUs de variaciones soft-deleted

✅ CustomException(message, Enumerable.Empty<string>(), HttpStatusCode.X)

✅ ICommand<Guid> para todos los commands

✅ Variaciones anidadas bajo productos:
   MapGroup("/{productId:guid}/variations") dentro del group de products

✅ QA Checklist completo antes de cada commit:
   backend: build → endpoint en Scalar → curl de prueba → respuesta 2xx
   frontend: build → browser → flujo completo → 0 errores Console/Network
```

---

## 9. ESTRUCTURA DE ARCHIVOS (estado tras C5)

```
src/Modules/Catalog/
├── Modules.Catalog.Contracts/
│   ├── Authorization/CatalogPermissions.cs   ← Products: Publish + Archive agregados
│   ├── Enums/ProductEnums.cs                 ← ProductType, ProductStatus, WeightUnit, DimensionUnit
│   ├── CatalogContractsMarker.cs
│   └── v1/
│       ├── Brands/       (CRUD completo)
│       ├── Categories/   (CRUD completo)
│       └── Products/     (CRUD core completo — faltan Variations, Codes, PriceLists)
│
└── Modules.Catalog/
    ├── CatalogModule.cs          ← brands + categories + products mapeados
    ├── Data/
    │   ├── CatalogDbContext.cs
    │   ├── CatalogDbInitializer.cs
    │   └── Configurations/       11 archivos
    ├── Domain/                   18 entidades v2
    ├── Extensions/SlugHelper.cs
    └── Features/v1/
        ├── Brands/       (CRUD completo)
        ├── Categories/   (CRUD completo)
        └── Products/     (CRUD core completo — faltan Variations, Codes)

clients/dashboard/src/
├── auth/permissions.ts           ← products: publish + archive agregados
├── api/catalog.ts                ← ProductDto v2, publishProduct, archiveProduct
└── pages/catalog/
    ├── brands.tsx      ✅ v2
    ├── categories.tsx  ✅ v2
    └── products.tsx    ✅ v2 (server-side grid, filtros tipo/estado, trash panel)
```

---

*Checkpoint actualizado: Junio 2026*
*Fases C1-C5 completadas — próxima: Fase C6 (Variations CRUD)*
