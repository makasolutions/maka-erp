using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// PriceList — lista de precios por segmento de cliente — spec §2.14.
/// Solo una lista activa por CustomerSegment + OwnerId (ValidTo IS NULL).
/// CustomerSegment: "retail"|"wholesale"|"vip"|"b2b"|"dropshipping" (claim "price_segment").
/// </summary>
public sealed class PriceList : BaseEntity<Guid>
{
    public string    Name            { get; private set; } = default!;
    public string?   Description     { get; private set; }
    public string    CustomerSegment { get; private set; } = default!;
    public DateTime  ValidFrom       { get; private set; }
    public DateTime? ValidTo         { get; private set; }  // null = lista vigente
    public bool      IsActive        { get; private set; }
    public Guid?     OwnerId         { get; private set; }  // null = global

    public DateTime  CreatedAtUtc    { get; private set; }
    public DateTime? UpdatedAtUtc    { get; private set; }

    public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();

    private PriceList() { }

    public static PriceList Create(
        string name,
        string customerSegment,
        DateTime validFrom,
        string? description = null,
        Guid? ownerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerSegment);

        return new PriceList
        {
            Id              = Guid.CreateVersion7(),
            Name            = name.Trim(),
            Description     = description?.Trim(),
            CustomerSegment = customerSegment.Trim().ToLowerInvariant(),
            ValidFrom       = validFrom,
            ValidTo         = null,
            IsActive        = true,
            OwnerId         = ownerId,
            CreatedAtUtc    = DateTime.UtcNow,
        };
    }

    /// <summary>Cierra la lista (al crear una nueva del mismo segmento) — spec §7.</summary>
    public void Close(DateTime validTo)
    {
        ValidTo      = validTo;
        IsActive     = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
