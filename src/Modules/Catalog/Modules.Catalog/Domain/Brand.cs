using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Brand / marca — spec §2.1.
/// OwnerId = null  → canónico global (visible a todos los tenants).
/// OwnerId = Guid  → override de un tenant (preparado para fase multi-tenant futura).
/// Runtime siempre tenant-aislado por el shadow TenantId que añade BaseDbContext.
/// </summary>
public sealed class Brand : AggregateRoot<Guid>, ISoftDeletable
{
    public string  Name            { get; private set; } = default!;
    public string  Slug            { get; private set; } = default!;  // único global (filtrado IsDeleted=false)
    public string? Description     { get; private set; }
    public string? LogoUrl         { get; private set; }
    public string? WebsiteUrl      { get; private set; }
    public string? CountryOfOrigin { get; private set; }  // ISO-2: "JP", "CN", "CO"

    public Guid?   OwnerId         { get; private set; }  // null = canónico global
    public bool    IsActive        { get; private set; }

    public int?    WooCommerceId   { get; private set; }

    public DateTime  CreatedAtUtc  { get; private set; }
    public DateTime? UpdatedAtUtc  { get; private set; }

    // ISoftDeletable — populated by the auditing interceptor on dbContext.Remove().
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    private Brand() { }

    public static Brand Create(
        string name,
        string slug,
        string? description = null,
        string? logoUrl = null,
        string? websiteUrl = null,
        string? countryOfOrigin = null,
        Guid? ownerId = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Brand
        {
            Id              = Guid.CreateVersion7(),
            Name            = name.Trim(),
            Slug            = slug.ToLowerInvariant().Trim(),
            Description     = description?.Trim(),
            LogoUrl         = logoUrl?.Trim(),
            WebsiteUrl      = websiteUrl?.Trim(),
            CountryOfOrigin = countryOfOrigin?.Trim().ToUpperInvariant(),
            OwnerId         = ownerId,
            IsActive        = isActive,
            CreatedAtUtc    = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string slug,
        string? description,
        string? logoUrl,
        string? websiteUrl,
        string? countryOfOrigin,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Name            = name.Trim();
        Slug            = slug.ToLowerInvariant().Trim();
        Description     = description?.Trim();
        LogoUrl         = logoUrl?.Trim();
        WebsiteUrl      = websiteUrl?.Trim();
        CountryOfOrigin = countryOfOrigin?.Trim().ToUpperInvariant();
        IsActive        = isActive;
        UpdatedAtUtc    = DateTime.UtcNow;
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
