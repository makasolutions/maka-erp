using System.Text.Json;
using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Product — definición pura del bien/servicio — spec §2.6.
/// ⚠ NO contiene Price (→ PriceListItem), Stock (→ Inventory) ni Sku (→ ProductVariation).
/// Cada producto tiene al menos 1 ProductVariation (la default para Simple/Service).
/// </summary>
public sealed class Product : AggregateRoot<Guid>, ISoftDeletable
{
    // Identificación
    public string   Name             { get; private set; } = default!;
    public string   Slug             { get; private set; } = default!;
    public string?  ShortDescription { get; private set; }
    public string?  Description      { get; private set; }
    public string?  TechnicalSpecs   { get; private set; }
    public JsonDocument? Specs       { get; private set; }  // JSONB

    // Clasificación
    public Guid?    BrandId          { get; private set; }
    public Guid?    TaxRateId        { get; private set; }
    public Guid?    ShippingClassId  { get; private set; }

    // Tipo y estado
    public ProductType   Type        { get; private set; }
    public ProductStatus Status      { get; private set; }

    // Envío
    public bool     IsVirtual        { get; private set; }
    public bool     IsDownloadable   { get; private set; }
    public decimal? Weight           { get; private set; }
    public WeightUnit    WeightUnit  { get; private set; }
    public decimal? DimensionLength  { get; private set; }
    public decimal? DimensionWidth   { get; private set; }
    public decimal? DimensionHeight  { get; private set; }
    public DimensionUnit DimensionUnit { get; private set; }

    // SEO
    public string?  SeoTitle         { get; private set; }
    public string?  SeoDescription   { get; private set; }
    public string?  SeoKeywords      { get; private set; }

    // Catálogo global
    public Guid?    OwnerId          { get; private set; }
    public bool     IsPublic         { get; private set; }

    public int?     WooCommerceId    { get; private set; }

    public DateTime  CreatedAtUtc    { get; private set; }
    public DateTime? UpdatedAtUtc    { get; private set; }

    // ISoftDeletable
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    // Colecciones
    public ICollection<ProductImage>      Images            { get; private set; } = new List<ProductImage>();
    public ICollection<ProductCode>       Codes             { get; private set; } = new List<ProductCode>();
    public ICollection<ProductTag>        Tags              { get; private set; } = new List<ProductTag>();
    public ICollection<ProductCategory>   ProductCategories { get; private set; } = new List<ProductCategory>();
    public ICollection<ProductAttribute>  Attributes        { get; private set; } = new List<ProductAttribute>();
    public ICollection<ProductVariation>  Variations        { get; private set; } = new List<ProductVariation>();
    public ICollection<ProductBundleItem> BundleItems       { get; private set; } = new List<ProductBundleItem>();

    private Product() { }

    /// <summary>
    /// Crea un producto; para Simple/Service genera la variación default
    /// con Sku = "{slug}-default" (spec §7 VARIACIÓN DEFAULT).
    /// </summary>
    public static Product Create(
        string name,
        string slug,
        ProductType type,
        Guid? brandId = null,
        Guid? taxRateId = null,
        Guid? shippingClassId = null,
        string? shortDescription = null,
        Guid? ownerId = null,
        string? defaultSku = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var product = new Product
        {
            Id               = Guid.CreateVersion7(),
            Name             = name.Trim(),
            Slug             = slug.ToLowerInvariant().Trim(),
            Type             = type,
            Status           = ProductStatus.Draft,
            BrandId          = brandId,
            TaxRateId        = taxRateId,
            ShippingClassId  = shippingClassId,
            ShortDescription = shortDescription?.Trim(),
            OwnerId          = ownerId,
            IsPublic         = false,
            WeightUnit       = WeightUnit.KG,
            DimensionUnit    = DimensionUnit.CM,
            CreatedAtUtc     = DateTime.UtcNow,
        };

        if (type is ProductType.Simple or ProductType.Service)
        {
            var sku = string.IsNullOrWhiteSpace(defaultSku) ? $"{product.Slug}-default" : defaultSku;
            product.Variations.Add(ProductVariation.Create(product.Id, sku, isDefault: true));
        }

        return product;
    }

    public void UpdateDetails(
        string name,
        string? shortDescription,
        string? description,
        string? technicalSpecs,
        Guid? brandId,
        Guid? taxRateId,
        Guid? shippingClassId,
        bool isVirtual,
        bool isDownloadable,
        bool isPublic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name             = name.Trim();
        ShortDescription = shortDescription?.Trim();
        Description      = description;
        TechnicalSpecs   = technicalSpecs;
        BrandId          = brandId;
        TaxRateId        = taxRateId;
        ShippingClassId  = shippingClassId;
        IsVirtual        = isVirtual;
        IsDownloadable   = isDownloadable;
        IsPublic         = isPublic;
        UpdatedAtUtc     = DateTime.UtcNow;
    }

    public void UpdateSpecs(JsonDocument? specs)
    {
        Specs        = specs;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateSeo(string? title, string? description, string? keywords)
    {
        SeoTitle       = title;
        SeoDescription = description;
        SeoKeywords    = keywords;
        UpdatedAtUtc   = DateTime.UtcNow;
    }

    public void UpdateShipping(
        decimal? weight, WeightUnit weightUnit,
        decimal? length, decimal? width, decimal? height, DimensionUnit dimUnit)
    {
        Weight          = weight;
        WeightUnit      = weightUnit;
        DimensionLength = length;
        DimensionWidth  = width;
        DimensionHeight = height;
        DimensionUnit   = dimUnit;
        UpdatedAtUtc    = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Active) return;
        Status       = ProductStatus.Active;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == ProductStatus.Archived) return;
        Status       = ProductStatus.Archived;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted    = false;
        DeletedOnUtc = null;
        DeletedBy    = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SyncWooCommerce(int wooCommerceId) => WooCommerceId = wooCommerceId;
}
