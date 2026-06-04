using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>ProductAttribute — atributos asignados al producto — spec §2.12.</summary>
public sealed class ProductAttribute : BaseEntity<Guid>
{
    public Guid ProductId           { get; private set; }
    public Guid AttributeId         { get; private set; }
    public bool IsUsedForVariations { get; private set; }
    public bool IsVisibleOnProduct  { get; private set; }
    public int  SortOrder           { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Valores seleccionados de este atributo para este producto (join via EF)
    public ICollection<CatalogAttributeValue> SelectedValues { get; private set; } = new List<CatalogAttributeValue>();

    private ProductAttribute() { }

    public static ProductAttribute Create(
        Guid productId,
        Guid attributeId,
        bool isUsedForVariations = false,
        bool isVisibleOnProduct = true,
        int sortOrder = 0)
        => new()
        {
            Id                  = Guid.CreateVersion7(),
            ProductId           = productId,
            AttributeId         = attributeId,
            IsUsedForVariations = isUsedForVariations,
            IsVisibleOnProduct  = isVisibleOnProduct,
            SortOrder           = sortOrder,
            CreatedAtUtc        = DateTime.UtcNow,
        };
}
