using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

public sealed class Category : AggregateRoot<Guid>, ISoftDeletable
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsVisible { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted = false;
        DeletedOnUtc = null;
        DeletedBy = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private Category() { }

    public static Category Create(
        string code,
        string name,
        string? description,
        Guid? parentCategoryId,
        string? imageUrl = null,
        bool isActive = true,
        bool isVisible = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Category
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Slug = Slugify(name),
            Description = description?.Trim(),
            ImageUrl = imageUrl?.Trim(),
            ParentCategoryId = parentCategoryId,
            IsActive = isActive,
            IsVisible = isVisible,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Update(
        string code,
        string name,
        string? description,
        Guid? parentCategoryId,
        string? imageUrl,
        bool isActive,
        bool isVisible)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (parentCategoryId == Id)
        {
            throw new InvalidOperationException("A category cannot be its own parent.");
        }

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Slug = Slugify(name);
        Description = description?.Trim();
        ImageUrl = imageUrl?.Trim();
        ParentCategoryId = parentCategoryId;
        IsActive = isActive;
        IsVisible = isVisible;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string Slugify(string value)
    {
        var trimmed = value.Trim();
#pragma warning disable CA1308 // slug is canonical lowercase, not security-sensitive
        var lower = trimmed.ToLowerInvariant();
#pragma warning restore CA1308
        var chars = lower.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var collapsed = new string(chars).Trim('-');
        while (collapsed.Contains("--", StringComparison.Ordinal))
        {
            collapsed = collapsed.Replace("--", "-", StringComparison.Ordinal);
        }
        return collapsed;
    }
}
