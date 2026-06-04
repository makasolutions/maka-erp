using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// TaxRate — spec §2.3. Seed: IVA 19% (default), IVA 5%, Exento 0%.
/// </summary>
public sealed class TaxRate : BaseEntity<Guid>
{
    public string   Name        { get; private set; } = default!;  // "IVA 19%"
    public decimal  Rate        { get; private set; }              // 0.19
    public string?  Description { get; private set; }
    public bool     IsDefault   { get; private set; }
    public bool     IsActive    { get; private set; }
    public Guid?    OwnerId     { get; private set; }  // null = global

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private TaxRate() { }

    public static TaxRate Create(string name, decimal rate, string? description = null, bool isDefault = false, Guid? ownerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(rate);

        return new TaxRate
        {
            Id           = Guid.CreateVersion7(),
            Name         = name.Trim(),
            Rate         = rate,
            Description  = description?.Trim(),
            IsDefault    = isDefault,
            IsActive     = true,
            OwnerId      = ownerId,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string name, decimal rate, string? description, bool isDefault, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(rate);
        Name         = name.Trim();
        Rate         = rate;
        Description  = description?.Trim();
        IsDefault    = isDefault;
        IsActive     = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
