using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// CatalogAttribute + CatalogAttributeValue — spec §2.5.
/// Prefijo "Catalog" para evitar conflicto con System.Attribute.
/// </summary>
public enum CatalogAttributeType { Text, Color, Image, Select }

public sealed class CatalogAttribute : BaseEntity<Guid>
{
    public string               Name                { get; private set; } = default!;
    public string               Slug                { get; private set; } = default!;
    public CatalogAttributeType Type                { get; private set; }
    public bool                 IsVisibleOnProduct  { get; private set; }
    public bool                 IsUsedForVariations { get; private set; }
    public int                  SortOrder           { get; private set; }
    public Guid?                OwnerId             { get; private set; }

    public int? WooCommerceId { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public ICollection<CatalogAttributeValue> Values { get; private set; } = new List<CatalogAttributeValue>();

    private CatalogAttribute() { }

    public static CatalogAttribute Create(
        string name,
        string slug,
        CatalogAttributeType type,
        bool isVisibleOnProduct = true,
        bool isUsedForVariations = false,
        int sortOrder = 0,
        Guid? ownerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        return new CatalogAttribute
        {
            Id                  = Guid.CreateVersion7(),
            Name                = name.Trim(),
            Slug                = slug.ToLowerInvariant().Trim(),
            Type                = type,
            IsVisibleOnProduct  = isVisibleOnProduct,
            IsUsedForVariations = isUsedForVariations,
            SortOrder           = sortOrder,
            OwnerId             = ownerId,
            CreatedAtUtc        = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        CatalogAttributeType type,
        bool isVisibleOnProduct,
        bool isUsedForVariations,
        int sortOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name                = name.Trim();
        Type                = type;
        IsVisibleOnProduct  = isVisibleOnProduct;
        IsUsedForVariations = isUsedForVariations;
        SortOrder           = sortOrder;
        UpdatedAtUtc        = DateTime.UtcNow;
    }

    public void SyncWooCommerce(int wooCommerceId) => WooCommerceId = wooCommerceId;
}

public sealed class CatalogAttributeValue : BaseEntity<Guid>
{
    public Guid    AttributeId { get; private set; }
    public string  Value       { get; private set; } = default!;  // "Rojo", "XL"
    public string? ColorCode   { get; private set; }  // solo Type=Color (#FF0000)
    public string? ImageUrl    { get; private set; }  // solo Type=Image
    public int     SortOrder   { get; private set; }

    public int? WooCommerceId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private CatalogAttributeValue() { }

    public static CatalogAttributeValue Create(
        Guid attributeId,
        string value,
        string? colorCode = null,
        string? imageUrl = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new CatalogAttributeValue
        {
            Id           = Guid.CreateVersion7(),
            AttributeId  = attributeId,
            Value        = value.Trim(),
            ColorCode    = colorCode,
            ImageUrl     = imageUrl,
            SortOrder    = sortOrder,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void SyncWooCommerce(int wooCommerceId) => WooCommerceId = wooCommerceId;
}
