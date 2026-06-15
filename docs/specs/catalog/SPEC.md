# SPEC · Módulo Catalog — Maka ERP   (AS-BUILT)

> **Estado:** As-built (refleja el código en `develop` @ `3214a0ef`, jun-2026).
> **Metodología:** Spec-Driven + Harness · **Origen:** lectura del código del repo (no inferido de memoria).
> **Constitución:** `docs/specs/_constitution.md` (índice maestro). `_TEMPLATE.md` aún no existe — las
> invariantes de §1 son las heredadas del índice maestro §4. As-built canónico complementario: `CLAUDE.md §16` + código.
> **Fidelidad:** cada afirmación se rastrea a `archivo:línea`/tipo/test real. Donde falte evidencia se
> escribe explícitamente "no encontrado en código".

---

## 0. Cómo leer

Este es un documento **as-built**: describe lo que Catalog **hace hoy**, no lo que debería hacer. La
sección "Tasks" de un spec greenfield se reemplaza por **§6 Estado actual, huecos y deuda técnica**. Los
requirements (§4) están en notación EARS pero redactan el **comportamiento observado**, cada uno con su
referencia al handler/dominio que lo implementa. El valor del documento está en §6/§7/§8 (huecos,
cobertura, decisiones) tanto como en la descripción.

---

## 1. Constitución aplicable

| Invariante | Estado | Evidencia |
|---|---|---|
| **INV-2** — solo PEPS/promedio/identificación específica; UEPS no existe (Catalog no valora pero no debe introducir el concepto) | **N/A — CUMPLE** | Catalog no valora inventario; **0** ocurrencias de `ueps`/`lifo` en `src/Modules/Catalog`. |
| **INV-3** — append-only donde haya ledgers; nada se sobrescribe, todo se versiona | **CUMPLE (parcial por alcance)** | `PriceListItem.ChangePrice` graba `PriceListItemHistory` inmutable (`Domain/PriceListItemHistory.cs`; test `CatalogPricingTests / PriceListTests.ChangePrice_Should_RecordImmutableHistoryEntry`). **Pero** `Agreement` es mutable in-place y solo se congela en `Terminado` (`Domain/Agreement.cs:124-139`) — sin ledger de versiones (la redefinición de Convenios ya lo registra como capa nueva en `docs/design/convenios-marketplace.draft.md`). |
| **INV-8** — integración por eventos vía Outbox; prohibido `IEventBus` directo | **CUMPLE (trivial) + HUECO** | **0** usos de `IEventBus` **y 0** de `IOutboxStore` **y 0** integration events en `Catalog.Contracts`. Catalog **no publica nada**: cumple la prohibición pero la malla de eventos del producto (ProductPublished, PriceChanged → contabilidad/inventario futuros) **está ausente** aquí. La protege `Architecture.Tests / EventingArchitectureTests` (commit `3214a0ef`). |
| **INV-9** — multitenant: `OwnerId Guid?` preparado; tenant restaurado en scope fresco al consumir eventos | **CUMPLE (preparado, no activado)** | `Product.OwnerId Guid?` + `Product.IsPublic` existen (`Domain/Product.cs:47-48`). Lectura cross-tenant del plano global vía `IGlobalCatalogReader.RunAsync` → scope hijo fijado al tenant `global` (`Data/GlobalCatalogReader.cs`). Catalog **no consume** integration events (no aplica la cláusula de restauración en consumidor). |
| **INV-10** — convenciones FSH (`FSH.Modules.Catalog`; `Guid.CreateVersion7()`; `base.OnModelCreating` al final; UI con `t()` + grids Syncfusion InlineCreate) | **CUMPLE** | Namespace `FSH.Modules.Catalog` (`CatalogModule.cs`). `Guid.CreateVersion7()` **36 usos / `Guid.NewGuid()` 0** en `Domain/`. `base.OnModelCreating(modelBuilder)` al final (`Data/CatalogDbContext.cs:59`). UI: `pages/catalog/*.tsx` usan `t()` e InlineCreate (`CreateableCombobox` en product-form, ver §6). |
| **Decisión Catalog tomada** — `Price` en `PriceListItem`→`VariationId`; Stock diferido a Inventory; `SKU` en `ProductVariation`; `OwnerId` preparado no activado | **CUMPLE (con fuga de concepto stock)** | `PriceListItem.Price` por `VariationId` (`Domain/PriceListItem.cs:14-15`). `ProductVariation.Sku` "ÚNICO global" (`Domain/ProductVariation.cs:14`). `OwnerId` nullable presente. ⚠️ **Fuga:** `ProductVariation` lleva flags de política de stock (`ManageStock`, `AllowBackorders`, `LowStockThreshold`, `SoldIndividually`, `Domain/ProductVariation.cs:29-32`) y existe el permiso `Catalog.Products.AdjustStock` (`CatalogPermissions.cs`) **sin handler que lo use** — ver §6. No hay **cantidades** de stock (eso sí difiere a Inventory). |

---

## 2. Glosario (términos reales del dominio)

| Término | Tipo real | Qué es |
|---|---|---|
| **Product** | `Domain/Product.cs` (`AggregateRoot<Guid>, ISoftDeletable`) | Definición pura del producto (sin precio ni stock). Tipos: Simple/Variable/Bundle/Service. |
| **ProductVariation** | `Domain/ProductVariation.cs` | SKU individual — unidad de precio (vía PriceListItem) e inventario futuro. Lleva el `Sku` único global. |
| **Brand / Category** | `Domain/Brand.cs`, `Category.cs` (árbol recursivo) | Marcas; categorías con taxonomía Google (`GoogleCategoryId/RootGoogleCategoryId/FullPath`). |
| **CatalogAttribute / CatalogAttributeValue** | `Domain/CatalogAttribute.cs` | Atributos para variaciones (Text/Color/Image/Select). Prefijo `Catalog` para evitar choque con `System.Attribute`. |
| **PriceList / PriceListItem / PriceListItemHistory** | `Domain/PriceList.cs`, `PriceListItem.cs`, `PriceListItemHistory.cs` | Listas de precio por segmento/campaña; ítem por `VariationId`; histórico inmutable de cambios. |
| **PartyPriceList** | `Domain/PartyPriceList.cs` | Asignación de lista de precios a un tercero (cliente/proveedor). |
| **PriceBulkProposal** | `Domain/PriceBulkProposal.cs` | Propuesta de cambio masivo de precios (CSV) con aprobación. |
| **TaxRate / ShippingClass** | `Domain/TaxRate.cs`, `ShippingClass.cs` | Impuestos (0/5/19%) y clases de envío. |
| **TenantProduct / TenantProductImage** | `Domain/TenantProduct.cs` | Override por tenant del catálogo (capa 2 del catálogo global). |
| **Industry / IndustryCategory / TenantIndustry** | `Domain/Industry.cs` | Industrias del marketplace; mapeo a raíces Google; industrias activas por tenant. |
| **CatalogAlias** | `Domain/CatalogAlias.cs` | Sinónimos para búsqueda fuzzy (pg_trgm) de categorías/marcas en el plano global. |
| **MarketplaceAttributeRequirement** | `Domain/MarketplaceAttributeRequirement.cs` | Atributos exigidos por marketplace destino (ML/Falabella/…). |
| **SupplierBrand / SupplierCategory / SupplierProduct** | `Domain/SupplierRelations.cs` | Mapeo proveedor ↔ catálogo. |
| **Agreement / AgreementRule** | `Domain/Agreement.cs` | Convenio comercial tenant-local con políticas + reglas de elegibilidad. |
| **ScorecardKpi / SupplierScorecard / ScorecardCriterion** | `Domain/SupplierScorecard.cs` | Evaluación ponderada periódica de un proveedor/distribuidor. |

---

## 3. Alcance as-built

### 3.1 Qué hace hoy (capacidades reales)
1. **Catálogo de productos**: CRUD de productos simples/variables/bundle/servicio, variaciones con SKU,
   imágenes, códigos (EAN/UPC/SKU), tags, atributos, bundles. Papelera (soft-delete + restore). Specs JSONB.
2. **Clasificación**: marcas, categorías (árbol + taxonomía Google), atributos para variaciones.
3. **Precios**: listas por segmento, ítems por variación con histórico inmutable, propuestas masivas CSV
   con aprobación, asignación de listas a terceros, campañas con vigencia.
4. **Impuestos/envío**: tasas de impuesto y clases de envío.
5. **Marketplace / catálogo global**: publicación de productos al tenant `global`, adopción cross-tenant,
   búsqueda fuzzy (pg_trgm + unaccent + alias), industrias y taxonomía, requisitos por marketplace.
6. **Relación con proveedores** (cohabita en Catalog): convenios (`Agreements`) con motor de evaluación de
   elegibilidad, scorecards ponderados de proveedor (`Scorecards`), mapeo proveedor↔catálogo (`Suppliers`).

### 3.2 Qué NO hace / está diferido
- **Stock / inventario** (cantidades, movimientos, valoración) → **Inventory** (no iniciado). Catalog solo
  guarda flags de política (`ManageStock`…) y un permiso `AdjustStock` huérfano.
- **No publica integration events** → la malla evento-driven hacia contabilidad/inventario/OMS no existe aún.
- **`OwnerId`/marketplace por-owner no activado** (preparado, no hay flujo que lo escriba como gate real).
- **Fase G** (bodega/stock global compartido, tarjeta dropshipping) → no iniciado.

### 3.3 Superficie (conteos reales)
- **37 tipos de dominio** en **28 archivos** (`Domain/*.cs`).
- **126 endpoints** (`.Map{Get,Post,Put,Delete}`), **125 con `.RequirePermission`** (delta = 1, ver §6).
- **17 enums** en `Contracts/Enums`.
- **18 configuraciones EF**, **13 migraciones** (`Catalog/`).
- **23 áreas de feature** en `Features/v1/`.
- **90 tests unitarios** (10 Domain + 7 Validators) en `Catalog.Tests`; **0** integration tests de negocio.
- **Agregados raíz**: `Product` es el único `AggregateRoot<Guid>`; el resto son `BaseEntity<Guid>`
  (incluidos `Agreement`, `PriceList`, `SupplierScorecard` — ver smell en §6).

---

## 4. Requirements (EARS) — derivados del comportamiento actual

### CAP-01 · Productos y variaciones
- **REQ-01.1** CUANDO se crea un producto Simple o Service EL SISTEMA DEBERÁ generar una variación por
  defecto automáticamente. *(`Product.Create`; test `ProductTests.Create_Should_GenerateDefaultVariation_When_SimpleOrService`)*
- **REQ-01.2** CUANDO se crea un producto Variable o Bundle EL SISTEMA DEBERÁ NO generar variación por
  defecto. *(test `Create_Should_NotGenerateDefaultVariation_When_VariableOrBundle`)*
- **REQ-01.3** EL SISTEMA DEBERÁ crear todo producto en estado `Draft`. *(test `Create_Should_StartAsDraft`)*
- **REQ-01.4** CUANDO se publica un producto EL SISTEMA DEBERÁ pasarlo a `Active`; CUANDO se archiva,
  a `Archived`. *(tests `Publish_Should_SetStatusActive`, `Archive_Should_SetStatusArchived`)*
- **REQ-01.5** EL SISTEMA DEBERÁ almacenar el `Sku` de variación en mayúsculas y trim, y rechazar SKU en
  blanco. *(`ProductVariation.Create`; tests `Create_Should_UppercaseAndTrimSku`, `Create_Should_ThrowOnBlankSku`)*
- **REQ-01.6** SI un producto se elimina ENTONCES EL SISTEMA DEBERÁ hacer soft-delete (recuperable vía
  Restore). *(`ISoftDeletable`; `Features/v1/Products/*`)*
- **REQ-01.7** DONDE el producto tenga `Specs` EL SISTEMA DEBERÁ persistirlas como JSONB. *(`Product.Specs : JsonDocument`)*

### CAP-02 · Precios, listas, propuestas, campañas
- **REQ-02.1** EL SISTEMA DEBERÁ crear listas de precio normalizando el segmento e iniciándolas activas.
  *(test `PriceListTests.Create_Should_NormalizeSegmentAndStartActive`)*
- **REQ-02.2** CUANDO cambia el precio de un ítem EL SISTEMA DEBERÁ grabar una entrada **inmutable** de
  histórico y acumularla. *(tests `ChangePrice_Should_RecordImmutableHistoryEntry`, `..._OnMultipleChanges`)* — **INV-3**.
- **REQ-02.3** SI el precio es negativo ENTONCES EL SISTEMA DEBERÁ rechazarlo. *(test `Create_Should_ThrowOnNegativePrice`)*
- **REQ-02.4** EL SISTEMA DEBERÁ derivar la base con/sin redondeo respetando IVA y nunca producir sugerido
  negativo. *(tests `CatalogPricingTests.DeriveBase_*`, `RoundSuggested_Should_NeverGoNegative`)*
- **REQ-02.5** MIENTRAS un ítem tenga override manual EL SISTEMA DEBERÁ respetarlo al aplicar derivados.
  *(test `ApplyDerived_Should_RespectManualOverride`)*
- **REQ-02.6** CUANDO se crea una propuesta masiva EL SISTEMA DEBERÁ iniciarla `Pending`; aprobar/rechazar
  fija decididor; aprobar es no-op si ya decidida. *(tests `Create_Should_StartPending`, `Approve_Should_*`, `Reject_Should_SetRejected`)*
- **REQ-02.7** CUANDO se crea una campaña EL SISTEMA DEBERÁ iniciarla `Scheduled` y al terminar
  desactivarla, recordando el precio base pre-campaña. *(tests `Create_Campaign_Should_StartScheduled`, `Transition_ToEnded_Should_Deactivate`, `SnapshotPreCampaign_Should_RememberBasePrice`)*

### CAP-03 · Categorías, marcas, atributos
- **REQ-03.1** EL SISTEMA DEBERÁ crear categorías/marcas con slug en minúsculas y trim de nombre.
  *(tests `PriceList...Create_Should_LowercaseSlug_AndKeepFlags`, `Brand...Create_Should_TrimName_AndKeepColor`)*
- **REQ-03.2** SI el nombre está en blanco ENTONCES EL SISTEMA DEBERÁ rechazarlo. *(tests `Create_Should_Throw_OnBlankName`)*
- **REQ-03.3** EL SISTEMA DEBERÁ validar el código de color de un valor de atributo y rechazar valor vacío.
  *(tests `AttributeDomainTests.AddAttributeValue_Should_ValidateColorCode`, `..._RejectEmptyValue`)*
- **REQ-03.4** SI un atributo se usa para variaciones ENTONCES EL SISTEMA DEBERÁ exigir valores al
  asignarlo al producto. *(test `SetProductAttributes_Should_RequireValues_WhenUsedForVariations`)*

### CAP-04 · Marketplace / catálogo global
- **REQ-04.1** EL SISTEMA DEBERÁ leer el catálogo del tenant `global` desde cualquier tenant vía un scope
  hijo (`IGlobalCatalogReader.RunAsync`), sin modificar `BaseDbContext`. *(`Data/GlobalCatalogReader.cs`)*
- **REQ-04.2** CUANDO se busca categoría/marca/producto EL SISTEMA DEBERÁ aplicar similitud fuzzy
  (pg_trgm) + unaccent + alias. *(`Features/v1/GlobalCatalog/*`; migración `Catalog_FuzzySearch`)*
- **REQ-04.3** DONDE exista una sugerencia global EL SISTEMA DEBERÁ permitir **adoptarla** editando
  nombre/slug y recreando ancestros. *(`Features/v1/GlobalCatalog/*`, 14 endpoints)*
- **REQ-04.4** DONDE un marketplace destino exija atributos EL SISTEMA DEBERÁ modelarlos como
  `MarketplaceAttributeRequirement`. *(`Domain/MarketplaceAttributeRequirement.cs`; test `MarketplaceRequirementTests`)*

### CAP-05 · Relación con proveedores (convenios, scorecards) — *cohabita en Catalog*
- **REQ-05.1** EL SISTEMA DEBERÁ crear convenios en `Borrador`, inmutables en `Terminado`.
  *(`Domain/Agreement.cs:124-139`)* — sin test (ver §7).
- **REQ-05.2** CUANDO se evalúa un convenio contra un distribuidor EL SISTEMA DEBERÁ resolver sus reglas
  leyendo el `Party` vía `Parties.Contracts` (`GetPartyByIdQuery` por `IMediator`) y el último scorecard.
  *(`Features/v1/Agreements/AgreementFeatures.cs:282,306,313-320`)* — sin test.
- **REQ-05.3** EL SISTEMA DEBERÁ calcular `WeightedScore = Σ(score·peso)/Σ(peso)` (1–5) → `Grade` A–F,
  inmutable en `Cerrado`. *(`Domain/SupplierScorecard.cs`)* — sin test.

> Nota: CAP-05 no tiene cobertura de dominio (a diferencia de CAP-01–04). Es además el candidato a
> extracción del §6/§8.

---

## 5. Design as-built (el modelo REAL)

### 5.1 Entidades y campos (extractos verificados)
- **`Product`** (`AggregateRoot<Guid>, ISoftDeletable`): `Name, Slug, ShortDescription, Description,
  TechnicalSpecs, Specs(JsonDocument/JSONB), BrandId?, TaxRateId?, ShippingClassId?, Type(ProductType),
  Status(ProductStatus), IsVirtual, IsDownloadable, Weight?, WeightUnit, Dimension{L,W,H}?, DimensionUnit,
  Seo{Title,Description,Keywords}?, OwnerId Guid?, IsPublic, WooCommerceId?, CreatedAtUtc, UpdatedAtUtc?,
  IsDeleted, DeletedOnUtc?, DeletedBy?`.
- **`ProductVariation`** (`BaseEntity<Guid>`): `ProductId, Sku(único global), Description?, IsDefault,
  IsActive, Weight?/Unit, Dimension{L,W,H}?, ImageUrl?, ManageStock, AllowBackorders, SoldIndividually,
  LowStockThreshold?, IsVirtual, WooCommerceId?, CreatedAtUtc, UpdatedAtUtc?, IsDeleted/DeletedOnUtc/DeletedBy,
  AttributeValues[], Codes[]`.
- **Auditoría:** **24** archivos de `Domain` usan `CreatedAtUtc`/`UpdatedAtUtc` **manuales** (`DateTime`);
  **0** implementan `IAuditableEntity` — diverge de la firma framework `CreatedOnUtc/LastModifiedOnUtc`
  (`DateTimeOffset`). Ver §6.

**Inventario de tipos (37) por concern** (para §6/§8):
| Concern | Tipos |
|---|---|
| Producto-núcleo (10) | Product, ProductVariation, ProductImage, ProductCode, ProductTag, ProductCategory, ProductAttribute, ProductAttributeValue, ProductBundleItem, VariationAttributeValue |
| Clasificación (5) | Brand, Category, CategoryAttribute, CatalogAttribute, CatalogAttributeValue |
| Precios (5) | PriceList, PriceListItem, PriceListItemHistory, PartyPriceList, PriceBulkProposal |
| Impuesto/envío (2) | TaxRate, ShippingClass |
| Override tenant (2) | TenantProduct, TenantProductImage |
| **Marketplace/global (5)** | Industry, IndustryCategory, TenantIndustry, MarketplaceAttributeRequirement, CatalogAlias |
| **Relación proveedor (8)** | SupplierBrand, SupplierCategory, SupplierProduct, Agreement, AgreementRule, ScorecardKpi, SupplierScorecard, ScorecardCriterion |

### 5.2 Relaciones y agregados
`Product` (raíz) → variaciones, imágenes, códigos, tags, categorías (N:N vía `ProductCategory`), atributos.
`PriceList` → `PriceListItem` (por `VariationId`) → `PriceListItemHistory` (append-only). El resto son
agregados independientes con FK por `Guid` (sin navegación cross-módulo: las referencias a Party son por
`Guid` + consulta `IMediator`).

### 5.3 Eventos
- **Emitidos:** **ninguno** (0 `IIntegrationEvent` en `Catalog.Contracts`, 0 `AddDomainEvent` con
  publicación). Catalog es un **leaf**: **0** módulos consumen `FSH.Modules.Catalog.Contracts`.
- **Consumidos:** ninguno como integration event. Llamada **síncrona de lectura** a Parties
  (`GetPartyByIdQuery` vía `IMediator`) en el motor de convenios — patrón permitido para reads.
- **Outbox:** N/A (no publica). Cumple INV-8 por ausencia de violación.

### 5.4 Persistencia
- `CatalogDbContext` (`Data/CatalogDbContext.cs`): schema `catalog`; extensiones `pg_trgm` + `unaccent`
  (`:53-56`); `ApplyConfigurationsFromAssembly`; `base.OnModelCreating` **al final** (`:59`).
- **18 configuraciones** EF (`Data/Configurations/*.cs`). **13 migraciones** (`Catalog/`), de
  `InitialCatalogV2` (jun-04) a `Catalog_FuzzySearch` (jun-09): pricing, category-attributes, marketplace
  requirements, campaigns, taxonomía Google, party-price-lists, supplier-categories, agreements,
  supplier-evaluation, fuzzy-search.

### 5.5 Endpoints (por área — verbo/handler en el código; 126 total, 125 con permiso)
| Área | Endpoints | Grupo de ruta |
|---|---|---|
| Products | 14 | `api/v1/catalog/products` |
| Variations | 6 | `.../{productId}/variations` |
| Brands | 7 · Categories 7 · Attributes 8 | `catalog/{brands,categories}` |
| PriceLists 6 · Prices 2 · PriceProposals 4 · Campaigns 4 · PartyPriceLists 3 | 19 | `catalog/{price-lists,party-price-lists}` |
| ProductImages 4 · ProductCodes 3 · ProductTags 2 · Bundles 3 | 12 | bajo products |
| TaxRates 4 · ShippingClasses 4 · TenantProducts 3 | 11 | catalog |
| **GlobalCatalog 14 · Marketplace 4** | 18 | `catalog/global` |
| **Suppliers 5 · Agreements 7 · Scorecards 12** | 24 | `catalog/{suppliers,agreements,scorecard-kpis,supplier-scorecards}` |

Permisos: 8 recursos (`Catalog.{Brands, Categories, Products, PriceLists, Attributes, Agreements,
Scorecards, Settings}`) con acciones `View/Create/Update/Delete/Restore` + específicas (`Publish, Archive,
AdjustStock, Manage, ApproveBulk`).

---

## 6. Estado actual, huecos y deuda técnica

### Implementado / parcial / ausente por capacidad
| Capacidad | Estado |
|---|---|
| CAP-01 Productos/variaciones | ✅ Completo (CRUD, papelera, default-variation, tests de dominio) |
| CAP-02 Precios/listas/propuestas/campañas | ✅ Completo con histórico inmutable y tests |
| CAP-03 Categorías/marcas/atributos | ✅ Completo con tests |
| CAP-04 Marketplace/global/fuzzy | ✅ Operando (publica/adopta/busca) — **sin tests de handler** |
| CAP-05 Convenios/scorecards/suppliers | ✅ Backend+UI operando — **0 tests, candidato a extracción** |
| TaxRates/ShippingClasses | ⚠️ Backend + selects; **sin página de administración propia** (consumidos como dropdown) |
| Stock | ❌ Diferido a Inventory (correcto) |

### Discrepancias contra la constitución (§1)
1. **Fuga del concepto stock (vs decisión "Catalog NO maneja stock").** `ProductVariation` lleva
   `ManageStock/AllowBackorders/SoldIndividually/LowStockThreshold` (`Domain/ProductVariation.cs:29-32`) y
   existe el permiso `Catalog.Products.AdjustStock` **referenciado solo en `CatalogPermissions.cs`** (0
   handlers lo usan) — **permiso huérfano**. Severidad media: son flags de política WooCommerce-parity, no
   cantidades; pero el `AdjustStock` debería vivir en Inventory.
2. **Convenciones de auditoría divergentes (no es INV explícito, pero rompe la firma framework).** 24
   entidades usan `CreatedAtUtc/UpdatedAtUtc` `DateTime` manuales en vez de `IAuditableEntity`
   (`CreatedOnUtc/LastModifiedOnUtc`, `DateTimeOffset`). Mismo hallazgo M6 del baseline Fase A. Severidad baja.
3. **INV-3 parcial:** `Agreement` no versiona (mutable→congelado), a diferencia de `PriceListItemHistory`.
   Por-diseño-hoy; la redefinición de Convenios lo aborda. Severidad: registrada, no bug.
4. **Malla de eventos ausente (INV-8 cumplido por vacío).** Catalog no emite `ProductPublished`/
   `PriceChanged`/`ProductAdoptedToGlobal` → cuando exista Inventory/OMS/Accounting **habrá que añadir**
   esos integration events por Outbox. Hoy es hueco, no violación.

### Endpoint sin permiso (delta 126 vs 125)
El barrido por-archivo **no halló ningún endpoint de datos sin `.RequirePermission`**; el delta de 1
proviene del conteo bruto de `.Map*` y corresponde con alta probabilidad al **probe de health**
(`GetCatalogHealthQueryHandler` + `AddHealthChecks`, `CatalogModule.cs:129-132`), no a un endpoint de
negocio. **Verificación pendiente:** confirmar que no es un endpoint de datos (set 3 de QA).

### Mega-módulo — conteo real y candidatos a extracción (SIN extraer)
**Catalog = 37 tipos / 126 endpoints / 18 EF configs.** De esos, **13 tipos (~35%)** y **~42 endpoints**
no son catálogo-núcleo sino **relación comercial / marketplace**:
- **Bounded context candidato "SupplierRelations / Convenios" (8 tipos, ~24 endpoints):** `Agreement`,
  `AgreementRule`, `SupplierScorecard`, `ScorecardKpi`, `ScorecardCriterion`, `SupplierBrand`,
  `SupplierCategory`, `SupplierProduct`. Razón: es la **relación proveedor↔comprador**, depende de
  `Parties.Contracts`, y ya tiene una redefinición cross-tenant pendiente
  (`docs/design/convenios-marketplace.draft.md`). Es el corte más limpio.
- **Bounded context candidato "Marketplace / catálogo global" (5 tipos, ~18 endpoints):** `Industry`,
  `IndustryCategory`, `TenantIndustry`, `MarketplaceAttributeRequirement`, `CatalogAlias` + `GlobalCatalog`.
  Razón: opera sobre el tenant `global` con su propio reader; conceptualmente "compartir/descubrir catálogo"
  ≠ "definir mi catálogo". Corte más discutible (comparte entidades núcleo como Category/Brand).
- **Catalog-núcleo a conservar (~24 tipos):** producto, variaciones, clasificación, precios, impuestos,
  override por tenant.

> **No se extrae nada en este documento.** La decisión va a §8 y depende de la resolución del draft de
> Convenios (Fase B) y de la compuerta ADR-0001 (topología de outbox antes del backbone).

### TODOs / code smells (archivo:línea)
- **0 `TODO`/`FIXME`/`NotImplementedException`** en `src/Modules/Catalog` (lo incompleto es de alcance, no
  de código a medias).
- **Smell — agregados sin raíz:** `Agreement`, `PriceList`, `SupplierScorecard` son `BaseEntity<Guid>` con
  colecciones hijas (`AgreementRule`, `PriceListItem`, `ScorecardCriterion`) que deberían colgar de un
  `AggregateRoot` para encapsular invariantes; hoy solo `Product` es raíz (`Domain/*.cs`). Severidad media.
- **Smell — permiso huérfano** `AdjustStock` (arriba).
- **Archivos `Features` grandes:** `Agreements/AgreementFeatures.cs` agrupa 7 endpoints + motor de
  evaluación en un solo archivo (vs el patrón VSA de un slice por carpeta usado en Products). Severidad baja.

---

## 7. Harness (verificación existente)

### 7.1 Requirement → test
| REQ | Test existente | Cobertura |
|---|---|---|
| 01.1/01.2 | `ProductTests.Create_Should_(Not)GenerateDefaultVariation_*` | ✅ |
| 01.3/01.4 | `ProductTests.Create_Should_StartAsDraft / Publish_* / Archive_*` | ✅ |
| 01.5 | `ProductVariationTests.Create_Should_UppercaseAndTrimSku / ThrowOnBlankSku` | ✅ |
| 02.2 | `PriceListTests.ChangePrice_Should_RecordImmutableHistoryEntry / _OnMultipleChanges` | ✅ |
| 02.4/02.5 | `CatalogPricingTests.DeriveBase_* / ApplyDerived_Should_RespectManualOverride` | ✅ |
| 02.6 | `PriceProposal…Create_Should_StartPending / Approve_* / Reject_*` | ✅ |
| 02.7 | `CampaignTests.Create_Campaign_* / Transition_ToEnded_* / SnapshotPreCampaign_*` | ✅ |
| 03.3/03.4 | `AttributeDomainTests.*` | ✅ |
| 04.4 | `MarketplaceRequirementTests` | ✅ (dominio) |
| **04.1/04.2/04.3** (global reader, fuzzy, adopt) | — | ❌ **sin test** (lógica de handler/SQL crudo) |
| **05.1/05.2/05.3** (convenios, evaluación, scorecard) | — | ❌ **sin test** (dominio **y** motor) |
| Todos los **handlers/endpoints** (126) | — | ❌ **sin integration tests** de negocio |

### 7.2 Architecture.Tests que protegen el módulo
- `EventingArchitectureTests` — prohíbe `IEventBus` en módulos (protege INV-8). ✅
- `HandlerValidatorPairingTests` — cada command/paginated-query con validator (Catalog incluido). ✅
- `ModuleArchitectureTests` / `LayerDependencyTests` / `ContractsPurityTests` / `TenantIsolationTests` /
  `NamespaceConventionsTests` — límites, pureza de Contracts, aislamiento de tenant, naming. ✅ (49→51 global).

### 7.3 Definition of Done para "documentado y verde" (tests que faltan)
1. **Integration tests de los flujos núcleo** (crear→publicar→precio→adoptar global) — hoy **0**.
2. **Tests de dominio de CAP-05** (`Agreement.ChangeStatus` inmutable, `SupplierScorecard.Recompute`,
   motor `EvaluateAgreement`).
3. **Test del `GlobalCatalogReader`** (lectura cross-tenant correcta + aislamiento).
4. **Test de regresión** del permiso huérfano `AdjustStock` (o su retiro hacia Inventory).
5. **Multitenant**: confirmar que adopción global y publicación no fugan entre tenants (set 3 QA).

---

## 8. Decisiones abiertas
1. **Descomposición del mega-módulo** (la grande). ¿Extraer `SupplierRelations/Convenios` (8 tipos, corte
   limpio, ya con redefinición pendiente) y/o `Marketplace` (5 tipos, corte discutible)? Ligada a: el draft
   de Convenios (Fase B) y a ADR-0001 (topología de outbox antes del backbone). **Recomendación:** decidir
   la extracción de `Convenios` **junto con** su redefinición cross-tenant, no antes; `Marketplace` puede
   quedarse en Catalog hasta que duela.
2. **Permiso `AdjustStock` + flags de stock**: ¿se quedan en Catalog (WooCommerce-parity) o migran a
   Inventory cuando exista? Decidir antes de Inventory.
3. **Auditoría**: ¿unificar las 24 entidades a `IAuditableEntity` o aceptar `CreatedAtUtc` manual como
   convención Catalog? (afecta consistencia transversal — M6).
4. **Eventos a emitir**: qué integration events necesita Catalog para el backbone (`ProductPublished`,
   `PriceChanged`, `ProductAdoptedToGlobal`) — definir junto con Inventory/OMS, no especular ahora.
5. **Agregados sin raíz**: ¿promover `Agreement`/`PriceList`/`SupplierScorecard` a `AggregateRoot`?

---

## 9. Trazabilidad
| REQ | Diseño (§5) | Test (§7) |
|---|---|---|
| 01.1–01.7 | `Domain/Product.cs`, `ProductVariation.cs` | ✅ (dominio) |
| 02.1–02.7 | `Domain/PriceList*.cs`, `PriceBulkProposal.cs`, `CatalogPricing.cs` | ✅ (dominio) |
| 03.1–03.4 | `Domain/{Brand,Category,CatalogAttribute}.cs` | ✅ (dominio) |
| 04.1–04.4 | `Data/GlobalCatalogReader.cs`, `Features/v1/GlobalCatalog/*`, `Domain/MarketplaceAttributeRequirement.cs` | ⚠️ solo 04.4 |
| 05.1–05.3 | `Domain/Agreement.cs`, `SupplierScorecard.cs`, `Features/v1/Agreements/AgreementFeatures.cs` | ❌ sin test |
| (capa endpoint, 126) | `Features/v1/**/*Endpoint.cs` | ❌ sin integration test |

**REQ sin diseño claro:** ninguno (todo REQ rastrea a un tipo/handler). **REQ sin test:** 04.1, 04.2, 04.3,
05.1, 05.2, 05.3 + toda la capa de endpoints.

---

*Documento as-built. Cero archivos de código modificados. Para el porqué de las decisiones de diseño ver
`CLAUDE.md §16`, `docs/design/convenios-marketplace.draft.md` y `docs/adr/`.*
