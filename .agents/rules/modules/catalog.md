> ⚠️ **SPEC HISTÓRICA v1** — la implementación divergió deliberadamente; el as-built canónico
> está en **CLAUDE.md §16 + código**. Útil como visión/razonamiento, **NO como receta**.
> La relación comercial (proveedor↔comprador) NO se modela aquí: ver el diseño de
> **Convenios/Marketplace** (`.agents/rules/modules/agreements-marketplace.md`).

# CATALOG_MODULE_SPEC.md
# Especificación del Módulo de Catálogo — Maka Omni-Commerce
# Versión: 2.0 | Mayo 2026
# Leer COMPLETO antes de escribir cualquier código.

---

## INSTRUCCIÓN PARA CLAUDE CODE

Este documento es la especificación de diseño del módulo Catalog.
Antes de implementar CUALQUIER cosa:
1. Lee este documento completo
2. Lee `.agents/rules/database.md`
3. Lee `.agents/rules/api-conventions.md`
4. Usa `.agents/skills/add-entity/SKILL.md` para cada entidad
5. Usa `.agents/skills/create-migration/SKILL.md` para las migraciones
6. Plan Mode obligatorio → proponer y esperar aprobación de Juan antes de escribir código

El módulo `Maka.Modules.Catalog` ya existe con datos y migraciones previas.
Los datos actuales NO son importantes y se pueden borrar.
Las migraciones existentes se deben revisar antes de crear nuevas
para evitar conflictos con el snapshot de EF Core.

---

## 1. PRINCIPIOS DE DISEÑO — LEER PRIMERO

### 1.1 Separación de responsabilidades

```
PRODUCTO   = definición del bien o servicio
             Qué es, cómo se llama, qué atributos tiene, imágenes.
             NUNCA precio. NUNCA stock.

PRECIO     = estado que cambia en el tiempo → módulo Catalog (PriceList)
             Cuánto cuesta, para qué segmento de cliente, en qué fecha.
             Histórico automático: cada cambio genera un nuevo registro.

STOCK      = estado que cambia en el tiempo → módulo Inventory
             Cuántas unidades hay, en qué bodega, público o privado.
             Trazabilidad: cada movimiento es un registro inmutable.
```

### 1.2 Variación por defecto (DefaultSku)

Todo producto, incluso el `Simple`, tiene internamente una variación que actúa
como la "unidad de stock" del producto. Esto unifica el modelo:

```
Producto Simple   → 1 variación (la default, sin atributos)
Producto Variable → N variaciones (cada combinación de atributos)
Producto Bundle   → N items (referencias a otras variaciones)
Producto Service  → 1 variación default (sin stock físico)
```

Las tablas de Inventario y PriceListItem siempre referencian `VariationId`,
nunca `ProductId` directamente. Esto simplifica las consultas y unifica el modelo.

### 1.3 Catálogo en dos capas

```
CAPA 1 — Canónico global (OwnerId = null)
  Solo super admins de Maka pueden crear/editar.
  Disponible para todos los tenants como template.

CAPA 2 — Override por tenant (OwnerId = TenantId)
  Cada tenant puede clonar un producto canónico y personalizarlo.
  Solo guarda los campos que difieren del canónico.
  Si un campo de override es null → se usa el valor canónico.
```

### 1.4 Histórico de precios

```
PriceList + PriceListItem = precio vigente actual
PriceListItemHistory      = cada cambio de precio genera un registro histórico
  → timestamp, usuario que hizo el cambio, precio anterior, precio nuevo, motivo
```

### 1.5 Códigos como tabla de equivalencias

`ProductCode` es la tabla central de todos los identificadores de un producto/variación:
- SKU interno (único)
- EAN, UPC, ISBN, GTIN (universales)
- CódigoFabricante, CódigoProveedor (para match en importación masiva de precios)

El flujo de actualización masiva de precios por proveedor:
```
CSV del proveedor → columna "código proveedor" 
  → buscar en ProductCode donde CodeType = "SupplierCode" AND SupplierId = X
  → hacer match con el ProductCode.VariationId
  → crear nueva versión de PriceListItem con precio propuesto
  → esperar aprobación del operador antes de activar
```

---

## 2. MODELO DE DATOS

### 2.1 Brand (Marcas)

```csharp
public class Brand : AggregateRoot, ISoftDeletable
{
    public string   Name              { get; private set; }  // "Sony", "DJI", "Canon"
    public string   Slug              { get; private set; }  // "sony" — único global, URL-friendly
    public string?  Description       { get; private set; }
    public string?  LogoUrl           { get; private set; }
    public string?  WebsiteUrl        { get; private set; }
    public string?  CountryOfOrigin   { get; private set; }  // "JP", "CN", "CO"

    // SaaS / Global catalog
    public Guid?    OwnerId           { get; private set; }  // null = canónico global
    public bool     IsActive          { get; private set; }
    public bool     IsDeleted         { get; private set; }  // ISoftDeletable

    // WooCommerce sync
    public int?     WooCommerceId     { get; private set; }
}
```

**Índices:** `Slug UNIQUE`, `OwnerId`, `IsActive + IsDeleted`

---

### 2.2 Category (Categorías — árbol recursivo)

```csharp
public class Category : AggregateRoot, ISoftDeletable
{
    public Guid?    ParentId          { get; private set; }  // null = raíz
    public string   Name              { get; private set; }
    public string   Slug              { get; private set; }  // único por nivel del árbol
    public string?  Description       { get; private set; }
    public string?  ImageUrl          { get; private set; }
    public int      SortOrder         { get; private set; }

    public Guid?    OwnerId           { get; private set; }
    public bool     IsActive          { get; private set; }
    public bool     IsDeleted         { get; private set; }

    public int?     WooCommerceId     { get; private set; }

    // Navigation
    public Category?              Parent   { get; private set; }
    public ICollection<Category>  Children { get; private set; }
}
```

**Índices:** `ParentId`, `OwnerId`, `(Slug + ParentId) UNIQUE`

---

### 2.3 TaxRate (Impuestos configurables)

```csharp
public class TaxRate : BaseEntity
{
    public string   Name          { get; private set; }  // "IVA 19%", "IVA 5%", "Exento"
    public decimal  Rate          { get; private set; }  // 0.19, 0.05, 0.00
    public string?  Description   { get; private set; }
    public bool     IsDefault     { get; private set; }
    public bool     IsActive      { get; private set; }
    public Guid?    OwnerId       { get; private set; }  // null = global
}
```

**Seed inicial:** IVA 19% (default), IVA 5%, Exento 0%

---

### 2.4 ShippingClass (Clases de envío)

```csharp
public class ShippingClass : BaseEntity
{
    public string   Name          { get; private set; }  // "Normal", "Frágil", "Sobredimensionado"
    public string?  Description   { get; private set; }
    public Guid?    OwnerId       { get; private set; }
}
```

---

### 2.5 Attribute y AttributeValue (Atributos para variaciones)

```csharp
public class CatalogAttribute : BaseEntity  // ⚠️ Prefijo Catalog para evitar conflicto con System.Attribute
{
    public string         Name                 { get; private set; }  // "Color", "Talla", "Material"
    public string         Slug                 { get; private set; }
    public AttributeType  Type                 { get; private set; }  // Text | Color | Image | Select
    public bool           IsVisibleOnProduct   { get; private set; }
    public bool           IsUsedForVariations  { get; private set; }
    public int            SortOrder            { get; private set; }
    public Guid?          OwnerId              { get; private set; }

    public ICollection<CatalogAttributeValue> Values { get; private set; }
}

public class CatalogAttributeValue : BaseEntity
{
    public Guid    AttributeId  { get; private set; }
    public string  Value        { get; private set; }  // "Rojo", "XL", "Algodón"
    public string? ColorCode    { get; private set; }  // solo para Type = Color (#FF0000)
    public string? ImageUrl     { get; private set; }  // solo para Type = Image
    public int     SortOrder    { get; private set; }
}

public enum AttributeType { Text, Color, Image, Select }
```

---

### 2.6 Product (Producto — definición pura, SIN precio ni stock)

```csharp
public class Product : AggregateRoot, ISoftDeletable
{
    // ── IDENTIFICACIÓN ──────────────────────────────────────────────────────
    public string   Name              { get; private set; }
    public string   Slug              { get; private set; }  // único global, auto-generado
    public string?  ShortDescription  { get; private set; }  // texto plano, máx 500 chars
    public string?  Description       { get; private set; }  // HTML enriquecido, máx 50.000 chars
    public string?  TechnicalSpecs    { get; private set; }  // HTML enriquecido, máx 10.000 chars
    public JsonDocument? Specs        { get; private set; }  // JSONB — specs estructuradas para filtros avanzados
                                                              // Ej: {"sensor":"12MP","video":"4K120fps"}

    // ── CLASIFICACIÓN ────────────────────────────────────────────────────────
    public Guid?    BrandId           { get; private set; }
    public Guid?    TaxRateId         { get; private set; }
    public Guid?    ShippingClassId   { get; private set; }
    // Categorías: relación N:N via ProductCategory

    // ── TIPO Y ESTADO ────────────────────────────────────────────────────────
    public ProductType    Type        { get; private set; }
    //  Simple    → 1 variación default, sin atributos visibles
    //  Variable  → N variaciones (combinaciones de atributos)
    //  Bundle    → combo de otras variaciones de otros productos
    //  Service   → sin stock físico (talleres, alquiler estudios, etc.)

    public ProductStatus  Status      { get; private set; }
    //  Draft     → solo visible para admins
    //  Active    → visible en catálogo
    //  Archived  → oculto, no se puede comprar

    // ── ENVÍO ────────────────────────────────────────────────────────────────
    public bool     IsVirtual         { get; private set; }   // sin envío (servicios, digitales)
    public bool     IsDownloadable    { get; private set; }
    public decimal? Weight            { get; private set; }
    public WeightUnit    WeightUnit   { get; private set; }   // KG | G | LB | OZ
    public decimal? DimensionLength   { get; private set; }
    public decimal? DimensionWidth    { get; private set; }
    public decimal? DimensionHeight   { get; private set; }
    public DimensionUnit DimensionUnit{ get; private set; }  // CM | M | IN

    // ── SEO ──────────────────────────────────────────────────────────────────
    public string?  SeoTitle          { get; private set; }   // ≤ 60 chars
    public string?  SeoDescription    { get; private set; }   // 135-139 chars
    public string?  SeoKeywords       { get; private set; }

    // ── CATÁLOGO GLOBAL ──────────────────────────────────────────────────────
    public Guid?    OwnerId           { get; private set; }   // null = canónico global
    public bool     IsPublic          { get; private set; }   // visible a otros tenants para dropshipping
    public bool     IsDeleted         { get; private set; }

    // ── WooCommerce sync ─────────────────────────────────────────────────────
    public int?     WooCommerceId     { get; private set; }

    // ── COLECCIONES ──────────────────────────────────────────────────────────
    public ICollection<ProductImage>      Images           { get; private set; }
    public ICollection<ProductCode>       Codes            { get; private set; }
    public ICollection<ProductTag>        Tags             { get; private set; }
    public ICollection<ProductCategory>   ProductCategories{ get; private set; }
    public ICollection<ProductAttribute>  Attributes       { get; private set; }
    public ICollection<ProductVariation>  Variations       { get; private set; }
    public ICollection<ProductBundleItem> BundleItems      { get; private set; }
    // Precios → módulo Catalog (PriceList/PriceListItem)
    // Stock   → módulo Inventory (StockEntry/StockMovement)
}

public enum ProductType    { Simple, Variable, Bundle, Service }
public enum ProductStatus  { Draft, Active, Archived }
public enum WeightUnit     { KG, G, LB, OZ }
public enum DimensionUnit  { CM, M, IN }
```

**⚠️ IMPORTANTE:** No hay campos `Price`, `SalePrice`, `Stock`, `ManageStock` directamente en Product.
Estos viven en `PriceListItem` y en el módulo de Inventario respectivamente.

---

### 2.7 ProductVariation (SKU individual — unidad de precio e inventario)

```csharp
public class ProductVariation : BaseEntity, ISoftDeletable
{
    public Guid    ProductId        { get; private set; }

    // Identificación
    public string  Sku              { get; private set; }   // ÚNICO global — requerido
    public string? Description      { get; private set; }   // "Rojo XL", "128GB Negro"
    public bool    IsDefault        { get; private set; }   // true = variación base del producto Simple
    public bool    IsActive         { get; private set; }
    public bool    IsDeleted        { get; private set; }

    // Física propia (override del padre, null = hereda del Product)
    public decimal? Weight          { get; private set; }
    public WeightUnit? WeightUnit   { get; private set; }
    public decimal? DimensionLength { get; private set; }
    public decimal? DimensionWidth  { get; private set; }
    public decimal? DimensionHeight { get; private set; }

    // Imagen específica de esta variación
    public string?  ImageUrl        { get; private set; }

    // Inventario — configuración (el stock real está en módulo Inventory)
    public bool     ManageStock     { get; private set; }
    public bool     AllowBackorders { get; private set; }
    public bool     SoldIndividually{ get; private set; }   // limitar a 1 por compra
    public int?     LowStockThreshold { get; private set; } // alerta cuando stock ≤ N
    public bool     IsVirtual       { get; private set; }   // sin envío

    // WooCommerce sync
    public int?     WooCommerceId   { get; private set; }

    // Combinación de atributos que define esta variación
    public ICollection<CatalogAttributeValue> AttributeValues { get; private set; }

    // Códigos propios de la variación
    public ICollection<ProductCode> Codes { get; private set; }
}
```

**Regla de creación:** Al crear un producto Simple o Service, crear automáticamente
una `ProductVariation` con `IsDefault = true` y `Sku = "{producto.Slug}-default"`.

**Índices:** `Sku UNIQUE`, `ProductId + IsDefault`, `ProductId + IsDeleted`

---

### 2.8 ProductImage

```csharp
public class ProductImage : BaseEntity
{
    public Guid    ProductId    { get; private set; }
    public string  Url          { get; private set; }
    public string? AltText      { get; private set; }
    public bool    IsPrimary    { get; private set; }
    public int     SortOrder    { get; private set; }
}
```

---

### 2.9 ProductCode (Tabla central de equivalencias de códigos)

```csharp
public class ProductCode : BaseEntity
{
    public Guid    ProductId        { get; private set; }
    public Guid    VariationId      { get; private set; }  // SIEMPRE referencia a una variación
                                                            // (incluyendo la variación default del Simple)
    public Guid?   SupplierId       { get; private set; }  // FK a Suppliers (módulo futuro)
                                                            // null = código universal (SKU, EAN, etc.)
                                                            // non-null = código específico de ese proveedor

    public string  CodeType         { get; private set; }  // "SKU" | "EAN" | "UPC" | "ISBN" |
                                                            // "GTIN" | "PartNumber" |
                                                            // "ManufacturerCode" | "SupplierCode"
                                                            // Extensible: el usuario puede agregar tipos
    public string  Code             { get; private set; }  // el valor alfanumérico
    public bool    IsPrimary        { get; private set; }  // el código principal visible en UI
}
```

**Índices:** `(VariationId + CodeType + Code) UNIQUE`, `(SupplierId + Code)` para match en importación masiva

**Uso en importación masiva de precios:**
```
1. Cargar CSV del proveedor con columnas: [CódigoProveedor, PrecioNuevo]
2. Para cada fila: buscar ProductCode WHERE CodeType = 'SupplierCode' AND SupplierId = X AND Code = fila.CódigoProveedor
3. Si hay match: proponer actualización de PriceListItem para esa VariationId
4. Mostrar al operador: lista de cambios propuestos (producto, variación, precio anterior, precio nuevo)
5. Operador aprueba/rechaza individualmente o en bloque
6. Solo al aprobar: crear nueva entrada en PriceListItem y registrar en PriceListItemHistory
```

---

### 2.10 ProductTag (Etiquetas)

```csharp
public class ProductTag : BaseEntity
{
    public Guid    ProductId  { get; private set; }
    public string  Name       { get; private set; }   // "Nuevo", "Oferta", "Exclusivo"
    public string? Color      { get; private set; }   // hex color para la etiqueta en UI
}
```

---

### 2.11 ProductCategory (Relación N:N)

```csharp
public class ProductCategory : BaseEntity
{
    public Guid ProductId    { get; private set; }
    public Guid CategoryId   { get; private set; }
    public bool IsPrimary    { get; private set; }   // categoría principal del producto
}
```

---

### 2.12 ProductAttribute (Atributos asignados al producto)

```csharp
public class ProductAttribute : BaseEntity
{
    public Guid   ProductId             { get; private set; }
    public Guid   AttributeId           { get; private set; }
    public bool   IsUsedForVariations   { get; private set; }
    public bool   IsVisibleOnProduct    { get; private set; }
    public int    SortOrder             { get; private set; }

    public ICollection<CatalogAttributeValue> SelectedValues { get; private set; }
}
```

---

### 2.13 ProductBundleItem (Items de un combo)

```csharp
public class ProductBundleItem : BaseEntity
{
    public Guid    ProductId           { get; private set; }  // el producto Bundle
    public Guid    ItemVariationId     { get; private set; }  // variación incluida en el combo
    public int     Quantity            { get; private set; }
    public decimal? DiscountPercent   { get; private set; }   // descuento específico de este item
    public decimal? DiscountFixed     { get; private set; }
    public bool    IsOptional         { get; private set; }   // el cliente puede quitar este item
    public int     SortOrder          { get; private set; }
}

// Configuración del bundle completo va en un campo JSONB del producto
// o en una entidad ProductBundleConfig si se necesita más estructura:
// DiscountPercent global, DiscountFixed global, ShowIndividualPrices
```

---

### 2.14 PriceList y PriceListItem (Listas de precios con histórico)

```csharp
public class PriceList : BaseEntity
{
    public string   Name              { get; private set; }  // "Lista Mayorista 2026-Q1"
    public string?  Description       { get; private set; }
    public string   CustomerSegment   { get; private set; }  // "retail" | "wholesale" | "vip" | "b2b" | "dropshipping"
                                                              // Se compara con claim "price_segment" del JWT del cliente
    public DateTime ValidFrom         { get; private set; }
    public DateTime? ValidTo          { get; private set; }  // null = lista activa actualmente
    public bool     IsActive          { get; private set; }
    public Guid?    OwnerId           { get; private set; }  // null = global | TenantId = del tenant

    public ICollection<PriceListItem> Items { get; private set; }
}

public class PriceListItem : BaseEntity
{
    public Guid     PriceListId       { get; private set; }
    public Guid     VariationId       { get; private set; }  // SIEMPRE referencia variación (nunca ProductId directo)
    public decimal  Price             { get; private set; }  // precio en COP
    public decimal? MinQuantity       { get; private set; }  // precio aplica a partir de X unidades
    public DateTime CreatedAt         { get; private set; }
    public string   CreatedByUserId   { get; private set; }  // auditoría de quién puso el precio

    public ICollection<PriceListItemHistory> History { get; private set; }
}

// ─────────────────────────────────────────────────
// HISTÓRICO DE PRECIOS — inmutable, nunca se edita
// ─────────────────────────────────────────────────
public class PriceListItemHistory : BaseEntity
{
    public Guid     PriceListItemId   { get; private set; }
    public Guid     VariationId       { get; private set; }
    public decimal  OldPrice          { get; private set; }
    public decimal  NewPrice          { get; private set; }
    public DateTime ChangedAt         { get; private set; }
    public string   ChangedByUserId   { get; private set; }
    public string?  ChangeReason      { get; private set; }  // "Importación masiva proveedor Sony", "Ajuste manual"
    public string?  SourceReference   { get; private set; }  // referencia al CSV o documento fuente
}
```

**Regla de negocio — lista activa:**
- Solo una lista activa por `CustomerSegment + OwnerId` en un momento dado.
- Al crear nueva lista para el mismo segmento → cerrar automáticamente la anterior (`ValidTo = DateTime.UtcNow`).
- Precio efectivo = buscar `PriceListItem` donde `PriceList.ValidFrom ≤ hoy` Y `(PriceList.ValidTo IS NULL OR ValidTo > hoy)` Y `PriceList.CustomerSegment = claim del JWT`.

---

### 2.15 TenantProduct (Override por tenant — Capa 2)

```csharp
public class TenantProduct : BaseEntity
{
    public Guid     CanonicalProductId     { get; private set; }  // referencia al producto canónico

    // Overrides de texto — null = usar valor del canónico
    public string?  NameOverride           { get; private set; }
    public string?  ShortDescriptionOverride { get; private set; }
    public string?  DescriptionOverride    { get; private set; }
    public string?  TechnicalSpecsOverride { get; private set; }
    public string?  SeoTitleOverride       { get; private set; }
    public string?  SeoDescriptionOverride { get; private set; }

    // Precios del tenant para dropshipping
    public decimal? DropshippingPrice      { get; private set; }  // precio que cobra a otros tenants
    public decimal? DropshippingMinQty     { get; private set; }  // mínimo para dropshipping

    // Visibilidad
    public bool     IsActive               { get; private set; }
    public bool     IsPublic               { get; private set; }  // visible a otros tenants

    // TenantId viene de Finbuckle (BaseEntity)
    public ICollection<TenantProductImage> Images { get; private set; }
}

public class TenantProductImage : BaseEntity
{
    public Guid    TenantProductId  { get; private set; }
    public string  Url              { get; private set; }
    public string? AltText          { get; private set; }
    public bool    IsPrimary        { get; private set; }
    public int     SortOrder        { get; private set; }
}
```

---

### 2.16 SupplierBrand y SupplierProduct (diseño para módulo futuro de Compras)

```csharp
// Estas tablas se crean ahora para no migrar después
// La lógica de proveedores se implementa en el módulo Purchasing

public class SupplierBrand : BaseEntity
{
    public Guid     SupplierId                  { get; private set; }  // FK futura a Suppliers
    public Guid     BrandId                     { get; private set; }
    public bool     IsExclusiveDistributor      { get; private set; }  // ej: DZOFilm Colombia
    public string?  Notes                       { get; private set; }
}

public class SupplierProduct : BaseEntity
{
    public Guid     SupplierId                  { get; private set; }
    public Guid     ProductId                   { get; private set; }
    public Guid     VariationId                 { get; private set; }
    public decimal? CostPrice                   { get; private set; }  // precio de costo
    public string?  CostCurrency                { get; private set; }  // "USD" | "COP" | "EUR"
    public bool     IsPreferredSupplier         { get; private set; }
    public DateTime? LastPriceUpdate            { get; private set; }
    // El código del proveedor va en ProductCode.CodeType = "SupplierCode" AND SupplierId = X
}
```

---

## 3. DIAGRAMA DE RELACIONES

```
Brand (1)──────────────────────── (*) Product
Category (*) ──[ProductCategory]── (*) Product
TaxRate (1)──────────────────────── (*) Product
ShippingClass (1)────────────────── (*) Product

Product (1) ──── (*) ProductImage
Product (1) ──── (*) ProductTag
Product (1) ──── (*) ProductCategory
Product (1) ──── (*) ProductAttribute ──── (*) CatalogAttributeValue
Product (1) ──── (*) ProductVariation ◄──── todos los precios e inventario
                        │
                        └──── (*) ProductCode (SKU, EAN, SupplierCode...)
                        └──── (*) CatalogAttributeValue (define la variación)
Product (1) ──── (*) ProductBundleItem ──── (1) ProductVariation [item del combo]

CatalogAttribute (1) ──── (*) CatalogAttributeValue

PriceList (1) ──── (*) PriceListItem ──── (1) ProductVariation
                         │
                         └──── (*) PriceListItemHistory [inmutable]

Product (1) ──── (*) TenantProduct [override por tenant]

Brand (1)    ──── (*) SupplierBrand
ProductVariation (1) ──── (*) SupplierProduct
ProductVariation (1) ──── (*) ProductCode [SupplierCode para match en importación]

[ StockEntry / StockMovement → módulo Inventory, referencia VariationId ]
```

---

## 4. PLAN DE MIGRACIONES

### Prerequisito: revisar el estado actual

```bash
# Antes de crear migraciones nuevas, revisar qué existe
dotnet ef migrations list \
  --project src/Modules/Catalog/Maka.Modules.Catalog \
  --startup-project src/Host/FSH.Starter.Api \
  --context CatalogDbContext
```

### Estrategia: drop y recrear (los datos actuales no importan)

```bash
# 1. Bajar tablas del catálogo
dotnet ef database drop --force \
  --project src/Modules/Catalog/Maka.Modules.Catalog \
  --startup-project src/Host/FSH.Starter.Api \
  --context CatalogDbContext

# 2. Eliminar migraciones existentes del catálogo
# Borrar todos los archivos en Data/Migrations/ del módulo Catalog

# 3. Crear migraciones nuevas en orden
```

### Orden de migraciones

```bash
# Migración 1 — Entidades base (sin dependencias entre sí)
dotnet ef migrations add AddCatalogBase \
  --project src/Modules/Catalog/Maka.Modules.Catalog \
  --startup-project src/Host/FSH.Starter.Api \
  --context CatalogDbContext \
  --output-dir Data/Migrations
# Incluye: Brand, Category, TaxRate, ShippingClass, CatalogAttribute, CatalogAttributeValue

# Migración 2 — Producto y sus colecciones
dotnet ef migrations add AddProduct \
  ...
# Incluye: Product, ProductImage, ProductTag, ProductCategory,
#          ProductAttribute, ProductVariation, ProductCode, ProductBundleItem

# Migración 3 — Precios con histórico
dotnet ef migrations add AddPricing \
  ...
# Incluye: PriceList, PriceListItem, PriceListItemHistory

# Migración 4 — Overrides por tenant
dotnet ef migrations add AddTenantOverrides \
  ...
# Incluye: TenantProduct, TenantProductImage

# Migración 5 — Relaciones con proveedores (esquema para módulo futuro)
dotnet ef migrations add AddSupplierRelations \
  ...
# Incluye: SupplierBrand, SupplierProduct

# Aplicar todas las migraciones
dotnet run --project src/Host/FSH.Starter.DbMigrator
```

---

## 5. FEATURES BACKEND — VERTICAL SLICE

Por cada feature: `Contracts/v1/{Area}/{Feature}/` + `Features/v1/{Area}/{Feature}/`

### 5.1 Brands

| Feature | Tipo | Notas |
|---|---|---|
| `CreateBrand` | Command | Validar slug único. Auto-generar slug del nombre |
| `UpdateBrand` | Command | Regenerar slug si cambia el nombre (y no está publicado) |
| `DeleteBrand` | Command | Soft delete. Verificar que no tenga productos activos |
| `GetBrandById` | Query | Detalle + conteo de productos |
| `GetBrands` | Query | Paginado, filtros: nombre, estado, OwnerId |

### 5.2 Categories

| Feature | Tipo | Notas |
|---|---|---|
| `CreateCategory` | Command | Con soporte ParentId para árbol |
| `UpdateCategory` | Command | Incluyendo mover de rama |
| `DeleteCategory` | Command | Verificar sin productos activos ni subcategorías |
| `GetCategoryTree` | Query | Árbol completo cargado de una sola vez (recursivo) |
| `GetCategories` | Query | Lista plana paginada |

### 5.3 Products

| Feature | Tipo | Notas |
|---|---|---|
| `CreateProduct` | Command | Crea producto + variación default automática |
| `UpdateProduct` | Command | Solo campos propios, no colecciones |
| `DeleteProduct` | Command | Soft delete |
| `PublishProduct` | Command | Draft → Active (valida campos requeridos) |
| `ArchiveProduct` | Command | Active → Archived |
| `GetProductById` | Query | Detalle completo con todas las colecciones resueltas |
| `GetProducts` | Query | Lista paginada con filtros avanzados |
| `GetProductBySlug` | Query | Para URLs amigables |

### 5.4 Variations

| Feature | Tipo | Notas |
|---|---|---|
| `AddVariation` | Command | Valida combinación única de AttributeValues |
| `UpdateVariation` | Command | Peso, imagen, ManageStock, etc. |
| `DeleteVariation` | Command | Soft delete. No eliminar si tiene stock o pedidos |
| `GenerateVariations` | Command | Auto-genera todas las combinaciones de atributos del producto |

### 5.5 ProductCodes

| Feature | Tipo | Notas |
|---|---|---|
| `AddProductCode` | Command | Agregar código a una variación |
| `RemoveProductCode` | Command | Eliminar código |
| `GetProductCodes` | Query | Todos los códigos de una variación |

### 5.6 Price Lists

| Feature | Tipo | Notas |
|---|---|---|
| `CreatePriceList` | Command | Cierra automáticamente la lista anterior del mismo segmento |
| `AddPriceListItem` | Command | Agrega precio para una variación + registra en History |
| `UpdatePriceListItem` | Command | Actualiza precio + registra cambio en History obligatoriamente |
| `BulkUpdatePricesFromCsv` | Command | Importación masiva. Genera propuestas pendientes de aprobación |
| `ApprovePriceProposal` | Command | Aprueba propuestas generadas por BulkUpdate |
| `RejectPriceProposal` | Command | Rechaza propuestas |
| `GetActivePriceList` | Query | Lista vigente para un segmento |
| `GetEffectivePrice` | Query | Precio resuelto para una variación + segmento + fecha |
| `GetPriceHistory` | Query | Histórico de cambios de precio de una variación |

### 5.7 Tenant Overrides

| Feature | Tipo | Notas |
|---|---|---|
| `CloneProductToTenant` | Command | Copia canónico al catálogo del tenant |
| `UpdateTenantProduct` | Command | Actualiza solo los campos override |
| `GetResolvedProduct` | Query | Producto resuelto = canónico + overrides del tenant |

---

## 6. FEATURES FRONTEND — DASHBOARD

Usando wrappers Maka*, i18n obligatorio, tokens CSS del tema.
Migrar a MakaGrid **una página a la vez**, verificar carga en browser antes de pasar a la siguiente.

### 6.1 Páginas

| Ruta | Descripción |
|---|---|
| `/catalog/brands` | MakaGrid con CRUD. InlineCreate desde productos |
| `/catalog/categories` | Vista árbol + lista. Drag para reordenar |
| `/catalog/attributes` | CRUD de atributos y sus valores |
| `/catalog/products` | MakaGrid con filtros: marca, categoría, tipo, estado |
| `/catalog/products/new` | Formulario multi-tab (ver §6.2) |
| `/catalog/products/:id` | Edición del producto |
| `/catalog/price-lists` | Gestión de listas de precios |
| `/catalog/price-lists/:id` | Items de la lista + historial de cambios |

### 6.2 ProductFormPage — Estructura de tabs

```
Tab 1: General
  Nombre, Slug (auto + editable), Descripción corta, Descripción (editor HTML),
  Marca (SfDropDownList + InlineCreate), Tipo de producto, Estado

Tab 2: Clasificación
  Árbol de categorías con checkboxes multi-selección (marcar categoría principal),
  Etiquetas, Clase de envío

Tab 3: Atributos y Variaciones
  Agregar atributos al producto + marcar cuáles son para variaciones,
  Botón "Generar variaciones" (auto-genera todas las combinaciones),
  Tabla de variaciones: SKU, imagen, peso, ManageStock, IsActive

Tab 4: Precios
  (No hay precio en el producto — explicar al usuario)
  Acceso directo a Listas de precios → filtrado por este producto
  Precio efectivo actual por segmento (lectura)

Tab 5: Inventario
  (El stock real está en módulo Inventario)
  Configuración: ManageStock, AllowBackorders, SoldIndividually,
  LowStockThreshold, IsVirtual, IsDownloadable
  Vista de stock actual por bodega (lectura, desde Inventory API)

Tab 6: Envío
  Peso + unidad, Dimensiones (L×A×H) + unidad, Clase de envío

Tab 7: Códigos
  Lista dinámica: tipo (SKU, EAN, UPC, SupplierCode...) + valor + proveedor
  Botón agregar código, botón eliminar

Tab 8: Imágenes
  Galería con drag-and-drop, marcar imagen principal

Tab 9: SEO
  Meta título (contador ≤60), Meta descripción (contador 135-139),
  Vista previa del snippet en Google

Tab 10: Especificaciones técnicas
  Editor HTML enriquecido (TechnicalSpecs),
  Editor JSON para Specs estructuradas (con validación de JSON)

Tab 11: Historial
  EntityAuditSection — historial de cambios del producto
```

---

## 7. REGLAS DE NEGOCIO CRÍTICAS

```
SLUG:
  Auto-generado del nombre:
    text.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase()
        .replace(/[^a-z0-9]+/g,'-').replace(/^-+|-+$/g,'')
  Único globalmente en el espacio de nombres del catálogo.
  "canción" → "cancion" (NO "canci-n")
  Inmutable una vez que el producto está Active.

VARIACIÓN DEFAULT:
  Al crear cualquier producto (Simple, Service):
    → crear automáticamente ProductVariation con IsDefault=true
    → Sku = slug-del-producto + "-default" (o el SKU que ingrese el usuario)
  Todo en PriceListItem y en Inventory referencia VariationId, nunca ProductId.

UNICIDAD DE SKU:
  El SKU de una variación es único globalmente (no solo por producto).
  La validación debe ser a nivel de base de datos (UNIQUE constraint).

CÓDIGOS PROVEEDOR:
  ProductCode donde CodeType='SupplierCode' y SupplierId=X
  es el código que ese proveedor usa para ese producto/variación.
  Puede haber múltiples SupplierCodes por variación (uno por proveedor).
  Se usa para el match en BulkUpdatePricesFromCsv.

LISTAS DE PRECIOS:
  Solo una lista activa (ValidTo IS NULL) por CustomerSegment + OwnerId.
  Al crear nueva lista del mismo segmento → cerrar la anterior automáticamente.
  Toda actualización de PriceListItem genera un registro en PriceListItemHistory.
  Los registros de History son INMUTABLES — nunca se editan ni eliminan.

BUNDLE:
  Los items del bundle descuentan stock de sus variaciones individuales.
  El precio = suma de precios efectivos de items − descuento del bundle.

SOFT DELETE — restricciones:
  Brand: no eliminar si tiene productos activos asociados.
  Category: no eliminar si tiene productos activos o subcategorías.
  ProductVariation: no eliminar si tiene stock > 0 o aparece en pedidos.
  → Lanzar CustomException con mensaje claro, nunca proceder silenciosamente.

PRODUCTO CANÓNICO (OwnerId = null):
  Solo Permissions.Catalog.Products.ManageGlobal puede crear/editar.
  Los tenants solo pueden ejecutar CloneProductToTenant.

TENANT OVERRIDE:
  Campo con valor null = heredar del canónico.
  El tenant nunca puede modificar el OwnerId del canónico.
  El precio del tenant se gestiona en PriceList del tenant, no en TenantProduct.
```

---

## 8. PERMISOS

```
Permissions.Catalog.Brands.View
Permissions.Catalog.Brands.Create
Permissions.Catalog.Brands.Update
Permissions.Catalog.Brands.Delete

Permissions.Catalog.Categories.View
Permissions.Catalog.Categories.Create
Permissions.Catalog.Categories.Update
Permissions.Catalog.Categories.Delete

Permissions.Catalog.Products.View
Permissions.Catalog.Products.Create
Permissions.Catalog.Products.Update
Permissions.Catalog.Products.Delete
Permissions.Catalog.Products.Publish
Permissions.Catalog.Products.ManageGlobal   ← solo super admins de Maka

Permissions.Catalog.PriceLists.View
Permissions.Catalog.PriceLists.Manage
Permissions.Catalog.PriceLists.ApproveBulk  ← aprobar importaciones masivas

Permissions.Catalog.Attributes.Manage
```

---

## 9. TRADUCCIONES REQUERIDAS

En `public/locales/es/catalog.json` y `en/catalog.json`:

```json
{
  "brands": { "title", "create", "edit", "delete", "noResults" },
  "categories": { "title", "create", "edit", "delete", "tree", "noResults" },
  "products": {
    "title", "create", "edit", "delete", "publish", "archive",
    "tabs": {
      "general", "classification", "attributes", "prices",
      "inventory", "shipping", "codes", "images", "seo",
      "specs", "history"
    }
  },
  "variations": { "title", "add", "generate", "sku", "default" },
  "codes": { "title", "add", "types": { "sku", "ean", "upc", "isbn", "gtin", "partNumber", "manufacturerCode", "supplierCode" } },
  "priceLists": { "title", "create", "segments", "validFrom", "validTo", "history", "bulkImport", "approve", "reject" },
  "inventory": { "manageStock", "allowBackorders", "soldIndividually", "virtual", "lowStockThreshold" },
  "status": { "draft", "active", "archived" },
  "types": { "simple", "variable", "bundle", "service" }
}
```

---

## 10. INTEGRACIÓN WooCommerce (preparar ahora, implementar en Fase INT)

Campos `WooCommerceId` (int?) en: Product, ProductVariation, Category, Brand,
CatalogAttribute, CatalogAttributeValue.

Mapeo de referencia:

| Maka | WooCommerce |
|---|---|
| Product | wp_posts (post_type='product') |
| ProductVariation | wp_posts (post_type='product_variation') |
| ProductCode (SKU) | _sku postmeta |
| PriceListItem | _price, _regular_price, _sale_price postmeta |
| Category | product_cat taxonomy |
| Brand | product_brand o custom taxonomy |
| CatalogAttribute | pa_{attribute} taxonomy |
| CatalogAttributeValue | term en pa_{attribute} |
| TaxRate | tax class |

---

## 11. SEÑALES DE ALERTA — PARAR Y PREGUNTAR

```
⚠️  Una migración DROP o ALTER a tabla con datos → mostrar SQL antes de aplicar
⚠️  Se está creando una variación sin SKU único verificado en BD
⚠️  Un PriceListItem se actualiza SIN crear registro en PriceListItemHistory
⚠️  Se intenta asignar OwnerId = null desde un tenant no super-admin
⚠️  Dos variaciones tienen la misma combinación de AttributeValues en el mismo producto
⚠️  Se intenta eliminar Brand/Category con productos activos
⚠️  PriceList.ValidFrom > PriceList.ValidTo
⚠️  Un producto de tipo Variable no tiene variaciones activas
⚠️  Un producto de tipo Simple no tiene su variación default
```

---

*Versión: 2.0 | Módulo: Catalog | Mayo 2026*
*Fuente de verdad para el módulo de Catálogo.*
*Guardar en: `/src/Modules/Catalog/CATALOG_MODULE_SPEC.md` o en `.agents/rules/modules/catalog.md`*

---

## 12. DISEÑO ANTICIPADO — MÓDULO INVENTORY (StockLedger / Kárdex)

> ⚠️ Estas tablas pertenecen al módulo Inventory, NO al módulo Catalog.
> Se documenta aquí para que el diseño del Catalog sea compatible
> y no requiera migraciones posteriores en las FKs.

### 12.1 StockEntry (Stock actual por variación, bodega y tenant)

```csharp
// src/Modules/Inventory/Domain/StockEntry.cs
public class StockEntry : BaseEntity
{
    public Guid   VariationId       { get; private set; }  // FK a ProductVariation (Catalog)
    public Guid   WarehouseId       { get; private set; }  // FK a Warehouse (Inventory)
    public int    QuantityOnHand    { get; private set; }  // stock físico total
    public int    QuantityReserved  { get; private set; }  // reservado por pedidos pendientes
    public int    QuantityPublic    { get; private set; }  // visible a otros tenants (dropshipping)
    // QuantityAvailable = QuantityOnHand - QuantityReserved (calculado, no almacenado)
    // TenantId viene de Finbuckle (BaseEntity)
}
```

### 12.2 StockMovement (Kárdex — inmutable, trazabilidad completa)

```csharp
// src/Modules/Inventory/Domain/StockMovement.cs
public class StockMovement : BaseEntity
{
    public Guid     VariationId           { get; private set; }  // FK a ProductVariation
    public Guid     WarehouseId           { get; private set; }
    public MovementType TransactionType   { get; private set; }
    //  Purchase       → entrada por compra
    //  Sale           → salida por venta
    //  Return         → entrada por devolución del cliente
    //  Adjustment     → ajuste manual (conteo físico)
    //  Transfer       → traslado entre bodegas
    //  Import         → entrada por importación internacional
    //  Warranty       → salida por garantía
    //  Sample         → salida por muestra/demo
    //  Write-off      → baja/merma

    public int      QuantityChange        { get; private set; }  // positivo = entrada, negativo = salida
    public int      StockSnapshot         { get; private set; }  // stock total DESPUÉS del movimiento
    public string?  ReferenceDocument     { get; private set; }  // "PO-2026-001", "ORD-2026-0042"
    public string?  Notes                 { get; private set; }
    // CreatedBy, CreatedAt, TenantId heredados de BaseEntity (INMUTABLE — nunca se edita)
}

public enum MovementType
{
    Purchase, Sale, Return, Adjustment, Transfer,
    Import, Warranty, Sample, WriteOff
}
```

**Relación con Catalog:**
- `StockMovement.VariationId` y `StockEntry.VariationId` referencian `ProductVariation.Id` del módulo Catalog.
- El módulo Catalog **no** tiene FKs hacia Inventory — la dependencia es unidireccional: Inventory → Catalog.
- La comunicación entre módulos es por eventos: `ProductVariation` publicará `VariationDeletedEvent` → Inventory verifica que no tenga stock antes de permitir el soft delete.

---

## 13. DISEÑO ANTICIPADO — CLAIMS-BASED PRICE ROUTING

Cuando el módulo CRM implemente la entidad Contact/Customer, el JWT del cliente
inyectará el claim de segmento de precio. Diseño de referencia:

```
Claim name: "price_segment"  (o "http://schemas.makasolutions.com/identity/claims/price-category")
Valores:    "retail" | "wholesale" | "vip" | "b2b" | "dropshipping"
Default:    "retail" (si el claim no existe en el JWT)
```

**Implementación de referencia en el handler de precio efectivo:**

```csharp
// GetEffectivePrice/GetEffectivePriceQueryHandler.cs
public async ValueTask<EffectivePriceDto> Handle(
    GetEffectivePriceQuery query, CancellationToken ct)
{
    // Obtener el segmento del claim del JWT del usuario actual
    var segment = _currentUser.GetClaim("price_segment") ?? "retail";
    var now = DateTimeOffset.UtcNow;

    var price = await _db.PriceListItems
        .Include(i => i.PriceList)
        .Where(i => i.VariationId == query.VariationId
                 && i.PriceList.CustomerSegment == segment
                 && i.PriceList.IsActive
                 && i.PriceList.ValidFrom <= now
                 && (i.PriceList.ValidTo == null || i.PriceList.ValidTo >= now))
        .OrderByDescending(i => i.PriceList.ValidFrom)
        .AsNoTracking()
        .FirstOrDefaultAsync(ct);

    // Fallback: si no hay lista para el segmento del usuario → buscar "retail"
    if (price is null && segment != "retail")
    {
        price = await _db.PriceListItems
            .Include(i => i.PriceList)
            .Where(i => i.VariationId == query.VariationId
                     && i.PriceList.CustomerSegment == "retail"
                     && i.PriceList.IsActive
                     && i.PriceList.ValidFrom <= now
                     && (i.PriceList.ValidTo == null || i.PriceList.ValidTo >= now))
            .OrderByDescending(i => i.PriceList.ValidFrom)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    return price is null
        ? throw new NotFoundException($"No active price found for variation {query.VariationId}")
        : new EffectivePriceDto(price.Price, price.PriceList.CustomerSegment, price.PriceList.Name);
}
```

**Nota para implementación futura:** El claim `price_segment` se asigna en el módulo CRM
al perfil del cliente/contacto. Por ahora, cualquier usuario sin este claim recibe el segmento `"retail"`.

---

## 14. ESTRATEGIA DE SEED (CatalogDbSeeder)

Claude Code debe implementar un `CatalogDbSeeder` que se ejecute en el `DbMigrator`.
El seed crea datos de prueba realistas basados en Tecnoimportaciones:

### 14.1 Taxes (Seed global — TenantId = null)

```
IVA 19% (default = true)
IVA 5%
Exento 0%
```

### 14.2 Brands (Seed global — OwnerId = null)

```
Sony          → slug: "sony"          → CountryOfOrigin: "JP"
DJI           → slug: "dji"           → CountryOfOrigin: "CN"
Godox         → slug: "godox"         → CountryOfOrigin: "CN"
Nanlite       → slug: "nanlite"       → CountryOfOrigin: "CN"
Blackmagic    → slug: "blackmagic-design" → CountryOfOrigin: "AU"
```

### 14.3 Categories (Seed global — árbol de 2 niveles)

```
Cámaras y Video
  └── Cámaras Cinema
  └── Cámaras Mirrorless
  └── Accesorios para Cámara
Iluminación
  └── Luz LED
  └── Flash
  └── Accesorios de Iluminación
Drones
  └── Drones Profesionales
  └── Accesorios para Drones
Audio
  └── Micrófonos
  └── Grabadoras
```

### 14.4 Attributes (Seed global)

```
Color         → Type: Color   → IsUsedForVariations: true
Capacidad     → Type: Select  → IsUsedForVariations: true
               → Values: "64GB", "128GB", "256GB", "512GB", "1TB"
```

### 14.5 Products de prueba (5 productos — OwnerId = null, canónicos)

```
1. Sony FX3
   Tipo: Simple | Categoría: Cámaras Cinema | Marca: Sony
   → 1 variación default (IsDefault=true)
   → Specs JSONB: {"sensor":"12.1MP Full-Frame","video":"4K 120fps","stabilization":"Active SteadyShot"}
   → Código EAN: 4548736130951

2. DJI Mavic 3 Pro
   Tipo: Variable | Categoría: Drones Profesionales | Marca: DJI
   → Variaciones: "DJI Mavic 3 Pro Solo" / "DJI Mavic 3 Pro Fly More Combo"
   → Specs JSONB: {"camera":"Hasselblad L-Format","flightTime":"43min","maxRange":"15km"}

3. Godox SL150W II
   Tipo: Simple | Categoría: Luz LED | Marca: Godox
   → 1 variación default

4. Nanlite FS-200B
   Tipo: Simple | Categoría: Luz LED | Marca: Nanlite
   → 1 variación default

5. Kit de Inicio Filmación  ← BUNDLE
   Tipo: Bundle | Sin categoría específica
   → Items: Sony FX3 (1ud) + Godox SL150W II (2ud) + Nanlite FS-200B (1ud)
   → Descuento del bundle: 5%
```

### 14.6 PriceLists de prueba (para tenant "root")

```
Lista Retail General    → CustomerSegment: "retail"    → ValidFrom: hoy → ValidTo: null
Lista Mayorista         → CustomerSegment: "wholesale" → ValidFrom: hoy → ValidTo: null
Lista VIP               → CustomerSegment: "vip"       → ValidFrom: hoy → ValidTo: null
```

Con precios de ejemplo en COP para los 5 productos anteriores.

---

## 15. ACTUALIZACIÓN DE ProductPriceHistory — campos faltantes

Agregar a `PriceListItemHistory` los campos de precio de oferta:

```csharp
public class PriceListItemHistory : BaseEntity
{
    public Guid     PriceListItemId   { get; private set; }
    public Guid     VariationId       { get; private set; }

    // Precio normal
    public decimal  OldPrice          { get; private set; }
    public decimal  NewPrice          { get; private set; }

    // Precio de oferta (puede ser null si no aplica)
    public decimal? OldSalePrice      { get; private set; }
    public decimal? NewSalePrice      { get; private set; }
    public DateTime? OldSalePriceFrom { get; private set; }
    public DateTime? NewSalePriceFrom { get; private set; }
    public DateTime? OldSalePriceTo   { get; private set; }
    public DateTime? NewSalePriceTo   { get; private set; }

    // Auditoría
    public DateTime ChangedAt         { get; private set; }
    public string   ChangedByUserId   { get; private set; }
    public string?  ChangeReason      { get; private set; }
    public string?  SourceReference   { get; private set; }  // nombre del CSV o documento
}
```

**Nota:** `SalePrice` y sus fechas van en `PriceListItem`, no en `Product`.
El precio de oferta es simplemente una entrada en la lista de precios con
`SalePrice + SalePriceFrom + SalePriceTo` — cuando la fecha actual está dentro
del rango, el precio efectivo es `SalePrice`; fuera del rango, es `Price`.

---

## 16. COMANDO DE ACTUALIZACIÓN MASIVA DE PRECIOS DESDE PROVEEDOR

Referencia de implementación para `BulkUpdatePricesFromCsvCommand`:

```csharp
// Contracts/v1/Prices/BulkUpdatePrices/BulkUpdatePricesFromCsvCommand.cs
public record BulkUpdatePricesFromCsvCommand(
    Guid        SupplierId,
    Guid        PriceListId,
    string      CsvContent,       // contenido del CSV en base64 o string
    string      SupplierCodeColumn,  // nombre de la columna de código del proveedor en el CSV
    string      PriceColumn,         // nombre de la columna de precio en el CSV
    string?     ChangeReason
) : ICommand<BulkUpdatePricesResult>;

public record BulkUpdatePricesResult(
    int     MatchedCount,     // cuántos códigos encontraron match
    int     NotFoundCount,    // cuántos códigos no encontraron match
    int     ProposedCount,    // propuestas creadas pendientes de aprobación
    List<string> NotFoundCodes  // lista de códigos que no se encontraron (para reporte)
);
```

**Flujo del handler:**
```
1. Parsear CSV → lista de (SupplierCode, NewPrice)
2. Para cada fila:
   → buscar ProductCode WHERE CodeType='SupplierCode' AND SupplierId=X AND Code=fila.SupplierCode
   → si no hay match: agregar a NotFoundCodes
   → si hay match: crear PriceBulkProposal (pendiente de aprobación)
3. Retornar BulkUpdatePricesResult con resumen
4. El operador aprueba/rechaza desde la UI
5. Al aprobar: actualizar PriceListItem + insertar en PriceListItemHistory (transacción atómica)
```

**Nota:** Usar `IDbContextTransaction` para garantizar atomicidad:
el `PriceListItem` y su `PriceListItemHistory` se guardan juntos o ninguno.

---

*Versión: 2.1 | Módulo: Catalog | Mayo 2026*
*Complementado con: StockLedger (Inventory), Claims-based pricing, Seed strategy, BulkUpdatePrices*
