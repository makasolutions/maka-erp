using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// ProductBundleItem — item de un combo (producto Bundle) — spec §2.13.
/// Referencia una ItemVariationId (la variación incluida en el combo).
/// </summary>
public sealed class ProductBundleItem : BaseEntity<Guid>
{
    public Guid     ProductId       { get; private set; }  // el producto Bundle
    public Guid     ItemVariationId { get; private set; }  // variación incluida
    public int      Quantity        { get; private set; }
    public decimal? DiscountPercent { get; private set; }
    public decimal? DiscountFixed   { get; private set; }
    public bool     IsOptional      { get; private set; }
    public int      SortOrder       { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private ProductBundleItem() { }

    public static ProductBundleItem Create(
        Guid productId,
        Guid itemVariationId,
        int quantity,
        decimal? discountPercent = null,
        decimal? discountFixed = null,
        bool isOptional = false,
        int sortOrder = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        return new ProductBundleItem
        {
            Id              = Guid.CreateVersion7(),
            ProductId       = productId,
            ItemVariationId = itemVariationId,
            Quantity        = quantity,
            DiscountPercent = discountPercent,
            DiscountFixed   = discountFixed,
            IsOptional      = isOptional,
            SortOrder       = sortOrder,
            CreatedAtUtc    = DateTime.UtcNow,
        };
    }
}
