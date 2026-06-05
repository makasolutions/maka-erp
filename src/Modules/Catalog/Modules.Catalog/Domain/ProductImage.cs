using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>ProductImage — spec §2.8.</summary>
public sealed class ProductImage : BaseEntity<Guid>
{
    public Guid    ProductId { get; private set; }
    public string  Url       { get; private set; } = default!;
    public string? AltText   { get; private set; }
    public bool    IsPrimary { get; private set; }
    public int     SortOrder { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private ProductImage() { }

    public static ProductImage Create(Guid productId, string url, string? altText = null, bool isPrimary = false, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new ProductImage
        {
            Id           = Guid.CreateVersion7(),
            ProductId    = productId,
            Url          = url.Trim(),
            AltText      = altText,
            IsPrimary    = isPrimary,
            SortOrder    = sortOrder,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}
