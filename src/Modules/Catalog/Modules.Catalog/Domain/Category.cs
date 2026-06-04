using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Category — árbol recursivo — spec §2.2.
/// ParentId = null → categoría raíz. Slug único por nivel (Slug + ParentId).
/// </summary>
public sealed class Category : AggregateRoot<Guid>, ISoftDeletable
{
    public Guid?   ParentId    { get; private set; }  // null = raíz
    public string  Name        { get; private set; } = default!;
    public string  Slug        { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ImageUrl    { get; private set; }
    public int     SortOrder   { get; private set; }

    public Guid?   OwnerId     { get; private set; }
    public bool    IsActive    { get; private set; }

    public int?    WooCommerceId { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    // ISoftDeletable
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    // Navigation — Children only; the parent is reached via ParentId (the tree
    // is assembled in GetCategoryTree). A scalar Parent nav is omitted on purpose.
    public ICollection<Category>  Children { get; private set; } = new List<Category>();

    private Category() { }

    public static Category Create(
        string name,
        string slug,
        Guid? parentId = null,
        string? description = null,
        string? imageUrl = null,
        int sortOrder = 0,
        Guid? ownerId = null,
        bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        return new Category
        {
            Id           = Guid.CreateVersion7(),
            ParentId     = parentId,
            Name         = name.Trim(),
            Slug         = slug.ToLowerInvariant().Trim(),
            Description  = description?.Trim(),
            ImageUrl     = imageUrl?.Trim(),
            SortOrder    = sortOrder,
            OwnerId      = ownerId,
            IsActive     = isActive,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string slug,
        Guid? parentId,
        string? description,
        string? imageUrl,
        int sortOrder,
        bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        Name         = name.Trim();
        Slug         = slug.ToLowerInvariant().Trim();
        ParentId     = parentId;
        Description  = description?.Trim();
        ImageUrl     = imageUrl?.Trim();
        SortOrder    = sortOrder;
        IsActive     = isActive;
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
