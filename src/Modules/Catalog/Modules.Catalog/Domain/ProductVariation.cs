using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// ProductVariation — unidad de precio e inventario — spec §2.7.
/// Sku ÚNICO GLOBAL (sin filtro de tenant — ver ProductVariationConfiguration).
/// PriceListItem e Inventory referencian VariationId, nunca ProductId.
/// </summary>
public sealed class ProductVariation : BaseEntity<Guid>, ISoftDeletable
{
    public Guid    ProductId   { get; private set; }
    public string  Sku         { get; private set; } = default!;  // ÚNICO global
    public string? Description { get; private set; }
    public bool    IsDefault   { get; private set; }
    public bool    IsActive    { get; private set; }

    // Física propia (null = hereda del Product)
    public decimal?    Weight          { get; private set; }
    public WeightUnit? WeightUnit      { get; private set; }
    public decimal?    DimensionLength { get; private set; }
    public decimal?    DimensionWidth  { get; private set; }
    public decimal?    DimensionHeight { get; private set; }

    public string?     ImageUrl        { get; private set; }

    // Inventario — configuración (stock real en módulo Inventory)
    public bool   ManageStock       { get; private set; }
    public bool   AllowBackorders   { get; private set; }
    public bool   SoldIndividually  { get; private set; }
    public int?   LowStockThreshold { get; private set; }
    public bool   IsVirtual         { get; private set; }

    public int?   WooCommerceId     { get; private set; }

    public DateTime  CreatedAtUtc    { get; private set; }
    public DateTime? UpdatedAtUtc    { get; private set; }

    // ISoftDeletable
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    // Combinación de atributos que define la variación (join via EF)
    public ICollection<CatalogAttributeValue> AttributeValues { get; private set; } = new List<CatalogAttributeValue>();
    public ICollection<ProductCode>           Codes           { get; private set; } = new List<ProductCode>();

    private ProductVariation() { }

    public static ProductVariation Create(
        Guid productId,
        string sku,
        string? description = null,
        bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        return new ProductVariation
        {
            Id           = Guid.CreateVersion7(),
            ProductId    = productId,
            Sku          = sku.Trim().ToUpperInvariant(),
            Description  = description?.Trim(),
            IsDefault    = isDefault,
            IsActive     = true,
            ManageStock  = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(
        string sku,
        string? description,
        bool isActive,
        decimal? weight, WeightUnit? weightUnit,
        decimal? dimLength, decimal? dimWidth, decimal? dimHeight,
        string? imageUrl,
        bool manageStock,
        bool allowBackorders,
        bool soldIndividually,
        int? lowStockThreshold,
        bool isVirtual)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        Sku               = sku.Trim().ToUpperInvariant();
        Description       = description?.Trim();
        IsActive          = isActive;
        Weight            = weight;
        WeightUnit        = weightUnit;
        DimensionLength   = dimLength;
        DimensionWidth    = dimWidth;
        DimensionHeight   = dimHeight;
        ImageUrl          = imageUrl;
        ManageStock       = manageStock;
        AllowBackorders   = allowBackorders;
        SoldIndividually  = soldIndividually;
        LowStockThreshold = lowStockThreshold;
        IsVirtual         = isVirtual;
        UpdatedAtUtc      = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted    = false;
        DeletedOnUtc = null;
        DeletedBy    = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Removes the IsDefault flag when another variation becomes the default.</summary>
    public void ClearDefault()
    {
        if (!IsDefault) return;
        IsDefault    = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SyncWooCommerce(int wooCommerceId) => WooCommerceId = wooCommerceId;
}
