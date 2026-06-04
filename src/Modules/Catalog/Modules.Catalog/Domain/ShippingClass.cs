using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// ShippingClass — spec §2.4. Ej: "Normal", "Frágil", "Sobredimensionado".
/// </summary>
public sealed class ShippingClass : BaseEntity<Guid>
{
    public string  Name        { get; private set; } = default!;
    public string? Description { get; private set; }
    public Guid?   OwnerId     { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ShippingClass() { }

    public static ShippingClass Create(string name, string? description = null, Guid? ownerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ShippingClass
        {
            Id           = Guid.CreateVersion7(),
            Name         = name.Trim(),
            Description  = description?.Trim(),
            OwnerId      = ownerId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name         = name.Trim();
        Description  = description?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
