# CHECKPOINT HANDOFF — Maka Omni-Commerce
# Fecha: Mayo/Junio 2026
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
- Frontend: React 19 + Vite + TanStack Query + Syncfusion Blazor React 33.1.44
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
- `IMPLEMENTATION_PROMPT.md` (prompt maestro para arrancar implementación)
- `MODEL_ROUTING_RULES.md` (reglas de selección de modelo)

### Módulo Catalog — estado actual ⚠️ EN TRANSICIÓN
**El módulo está siendo reconstruido desde cero.**

Estado antes de la limpieza (lo que había):
- Entidades viejas: Brand, Category, Product (con Sku, Price Money, Stock int)
- Migraciones viejas en: `src/Host/FSH.Starter.Migrations.PostgreSQL/Catalog/`
- 3 páginas frontend funcionando: /catalog/brands, /catalog/categories, /catalog/products
- Datos: todos de prueba, ninguno real importante

**Decisiones de arquitectura tomadas en la sesión anterior:**

DECISIÓN 1 — Modelo de tenancy para Catalog:
```
→ Opción elegida: Schema completo, runtime tenant-aislado (Opción 1)
→ OwnerId existe en las tablas como Guid? (preparado para SaaS futuro)
→ Runtime opera tenant-aislado por Finbuckle como siempre
→ La capa global canónica (null = visible a todos) se activa
  en una fase posterior cuando haya 2+ tenants reales
→ NO implementar IGlobalEntity ni opt-out del filtro Finbuckle ahora
```

DECISIÓN 2 — Blast radius:
```
→ Romper limpiamente y reconstruir
→ Todos los datos son de prueba — se pueden borrar
→ Las páginas frontend se rompen temporalmente — aceptable
→ Reconstruir con el modelo correcto del spec
```

---

## 4. TAREA ACTIVA — FASE C1 DEL MÓDULO CATALOG

### Lo que se estaba ejecutando cuando se agotaron los tokens:

Claude Code acababa de recibir estas instrucciones y hizo check-in.
**Verifica exactamente en qué Paso está** con los comandos de §6.

### Plan completo de Fase C1 (los 7 pasos):

```
Paso 1 — Limpiar módulo Catalog (backend)
  a. DROP de tablas en PostgreSQL:
     DROP TABLE IF EXISTS catalog."ProductImages" CASCADE;
     DROP TABLE IF EXISTS catalog."Products" CASCADE;
     DROP TABLE IF EXISTS catalog."Categories" CASCADE;
     DROP TABLE IF EXISTS catalog."Brands" CASCADE;
     (verificar nombres reales con \dt catalog.*)
  b. Eliminar archivos de migración en:
     src/Host/FSH.Starter.Migrations.PostgreSQL/Catalog/
  c. Eliminar archivos viejos del Domain/:
     Product.cs, Money.cs, Brand.cs, Category.cs, ProductImage.cs
     y Features/, Contracts/ del módulo Catalog
  d. dotnet build → verificar que compila (habrá errores frontend — esperado)

Paso 2 — Reconstruir Domain/ según el spec
  Orden estricto de creación de entidades:
  1. Brand
  2. Category (árbol recursivo con ParentId)
  3. TaxRate
  4. ShippingClass
  5. CatalogAttribute + CatalogAttributeValue
  6. Product (SIN Price, SIN Stock, SIN Sku)
  7. ProductVariation (con Sku ÚNICO global)
  8. ProductImage, ProductCode, ProductTag,
     ProductCategory, ProductAttribute, ProductBundleItem
  9. PriceList + PriceListItem + PriceListItemHistory
  10. TenantProduct + TenantProductImage
  11. SupplierBrand + SupplierProduct

  REGLAS para cada entidad:
  - Heredar de AggregateRoot (con domain events) o BaseEntity
  - Usar ISoftDeletable donde el spec lo indica
  - IHasTenant de FSH — NO campo TenantId manual
  - OwnerId es Guid? normal (no afecta Finbuckle)
  - Constructor privado vacío para EF Core
  - Todos los setters: private set

Paso 3 — Configuraciones EF Core en Data/Configurations/
  Índices obligatorios:
  - Brand: Slug UNIQUE
  - ProductVariation: Sku UNIQUE (global, sin filtro de tenant)
  - ProductCode: (VariationId + CodeType + Code) UNIQUE
  - Category: (Slug + ParentId) UNIQUE

Paso 4 — Migración única inicial
  dotnet ef migrations add InitialCatalogV2 \
    --project src/Host/FSH.Starter.Migrations.PostgreSQL \
    --startup-project src/Host/FSH.Starter.Api \
    --context CatalogDbContext \
    --output-dir Catalog
  Luego: dotnet run --project src/Host/FSH.Starter.DbMigrator

Paso 5 — Seed mínimo
  - 3 TaxRates: IVA 19% (default), IVA 5%, Exento 0%
  - 5 Brands: Sony, DJI, Godox, Nanlite, Blackmagic
  - Árbol básico de categorías (4 ramas)
  - NO productos todavía

Paso 6 — Features mínimas para compilar
  Solo para que el sistema no explote:
  - GetBrands (query + endpoint)
  - GetCategories (query + endpoint, árbol)
  Nada más — el resto en Fase C2

Paso 7 — Frontend mínimo para compilar
  - Actualizar ProductDto en clients/dashboard/src/api/catalog.ts
    (quitar sku/price/stock, alinear con el nuevo modelo)
  - Actualizar columnas en products.tsx (sin SKU/precio/stock)
  - Las páginas muestran menos datos temporalmente — está bien
  - dotnet build + npm run build → ambos deben pasar sin errores

  QA Checklist completo al final del Paso 7.
```

### Reglas de ejecución:
```
- Un commit por cada Paso completado y verificado
- dotnet build limpio antes de cada commit
- Si hay más de 5 errores nuevos en un paso: PARAR y reportar
- Frontend puede tener warnings temporales pero NO errores de build
```

---

## 5. CONTEXTO DE FASES FUTURAS (para referencia, no implementar ahora)

```
Fase C2: Features backend completas de Brands
Fase C3: Features backend completas de Categories
Fase C4: Features backend completas de Products (la más compleja)
Fase C5: Frontend completo (una página a la vez, QA antes de cada una)

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

# 3. ¿Qué archivos existen actualmente en el Domain del Catalog?
Get-ChildItem src/Modules/Catalog -Recurse -Filter "*.cs" | Select-Object Name

# 4. ¿Qué migraciones del Catalog existen?
Get-ChildItem src/Host/FSH.Starter.Migrations.PostgreSQL/Catalog -Filter "*.cs" 2>$null

# 5. ¿Compila el backend ahora mismo?
dotnet build src/FSH.Starter.slnx 2>&1 | tail -5

# 6. ¿Compila el frontend?
cd clients/dashboard && npm run build 2>&1 | tail -5

# 7. ¿Qué tablas existen en el schema catalog de PostgreSQL?
# (ejecutar en Adminer http://localhost:8081 o con psql)
# SELECT table_name FROM information_schema.tables
# WHERE table_schema = 'catalog';
```

Con esos resultados, identifica exactamente en qué Paso de la Fase C1 estás
y continúa desde ahí.

---

## 7. INSTRUCCIÓN DE CONTINUACIÓN

Una vez verificado el estado, responde con este formato:

```
[QA] Verificación del estado actual:
- Rama: [resultado]
- Últimos commits: [lista]
- Archivos en Domain/: [lista]
- Migraciones Catalog: [lista o "ninguna"]
- Build backend: [✅ limpio / ❌ N errores]
- Build frontend: [✅ limpio / ❌ N errores]
- Tablas en schema catalog: [lista o "ninguna"]

Estado: estoy en el Paso [N] de la Fase C1.
[Si Paso 1 completado: confirmar qué se limpió]
[Si nada hecho: "Listo para iniciar Paso 1"]

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

❌ NUNCA AutoMapper — mapeo manual con extension methods

❌ NUNCA Repository Pattern genérico — DbContext directo en handlers

❌ NUNCA MudBlazor — Syncfusion únicamente

❌ NUNCA strings hardcodeados en JSX — siempre t() con traducciones ES+EN

❌ NUNCA colores hex/rgb en CSS — siempre var(--color-*)

❌ NUNCA commitear con errores en Console o Network del browser

✅ OwnerId = Guid? en entidades del Catalog (preparado para SaaS, no activo)

✅ ISoftDeletable en Brand, Category, Product, ProductVariation

✅ AsNoTracking() en TODAS las queries de lectura

✅ QA Checklist completo antes de cada commit de feature
```

---

*Checkpoint generado: Mayo/Junio 2026*
*Sesión anterior terminó por límite de contexto (~1M tokens)*
*Continuar en nueva sesión con /clear activo*
