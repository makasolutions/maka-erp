using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// TenantProduct — override por tenant de un producto canónico (Capa 2) — spec §2.15.
/// Campo null = heredar del canónico. TenantId es shadow (Finbuckle).
/// </summary>
public sealed class TenantProduct : BaseEntity<Guid>
{
    public Guid     CanonicalProductId        { get; private set; }

    // Overrides de texto — null = usar valor del canónico
    public string?  NameOverride              { get; private set; }
    public string?  ShortDescriptionOverride  { get; private set; }
    public string?  DescriptionOverride       { get; private set; }
    public string?  TechnicalSpecsOverride    { get; private set; }
    public string?  SeoTitleOverride          { get; private set; }
    public string?  SeoDescriptionOverride    { get; private set; }

    // Dropshipping
    public decimal? DropshippingPrice         { get; private set; }
    public decimal? DropshippingMinQty        { get; private set; }

    // Visibilidad
    public bool     IsActive                  { get; private set; }
    public bool     IsPublic                  { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public ICollection<TenantProductImage> Images { get; private set; } = new List<TenantProductImage>();

    private TenantProduct() { }

    public static TenantProduct Create(Guid canonicalProductId)
        => new()
        {
            Id                 = Guid.CreateVersion7(),
            CanonicalProductId = canonicalProductId,
            IsActive           = true,
            IsPublic           = false,
            CreatedAtUtc       = DateTime.UtcNow,
        };

    public void UpdateOverrides(
        string? nameOverride,
        string? shortDescriptionOverride,
        string? descriptionOverride,
        string? technicalSpecsOverride,
        string? seoTitleOverride,
        string? seoDescriptionOverride,
        decimal? dropshippingPrice,
        decimal? dropshippingMinQty,
        bool isActive,
        bool isPublic)
    {
        NameOverride             = nameOverride;
        ShortDescriptionOverride = shortDescriptionOverride;
        DescriptionOverride      = descriptionOverride;
        TechnicalSpecsOverride   = technicalSpecsOverride;
        SeoTitleOverride         = seoTitleOverride;
        SeoDescriptionOverride   = seoDescriptionOverride;
        DropshippingPrice        = dropshippingPrice;
        DropshippingMinQty       = dropshippingMinQty;
        IsActive                 = isActive;
        IsPublic                 = isPublic;
        UpdatedAtUtc             = DateTime.UtcNow;
    }
}

/// <summary>TenantProductImage — spec §2.15.</summary>
public sealed class TenantProductImage : BaseEntity<Guid>
{
    public Guid    TenantProductId { get; private set; }
    public string  Url             { get; private set; } = default!;
    public string? AltText         { get; private set; }
    public bool    IsPrimary       { get; private set; }
    public int     SortOrder       { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private TenantProductImage() { }

    public static TenantProductImage Create(Guid tenantProductId, string url, string? altText = null, bool isPrimary = false, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        return new TenantProductImage
        {
            Id              = Guid.CreateVersion7(),
            TenantProductId = tenantProductId,
            Url             = url.Trim(),
            AltText         = altText,
            IsPrimary       = isPrimary,
            SortOrder       = sortOrder,
            CreatedAtUtc    = DateTime.UtcNow,
        };
    }
}
