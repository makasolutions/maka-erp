namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Explicit join: ProductVariation ↔ CatalogAttributeValue.
/// The set of attribute values that defines a variation (e.g. "Rojo" + "XL").
/// Explicit (not an implicit Dictionary join) so BaseDbContext's per-entity
/// tenant-isolation pass can apply IsMultiTenant() to a real CLR type.
/// </summary>
public sealed class VariationAttributeValue
{
    public Guid VariationId      { get; set; }
    public Guid AttributeValueId { get; set; }
}

/// <summary>
/// Explicit join: ProductAttribute ↔ CatalogAttributeValue.
/// The values selected for an attribute assigned to a product.
/// </summary>
public sealed class ProductAttributeValue
{
    public Guid ProductAttributeId { get; set; }
    public Guid AttributeValueId   { get; set; }
}
