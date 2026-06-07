using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// PriceList — lista de precios por segmento de cliente — spec §2.14 (Fase 3).
/// Exactamente una lista <see cref="IsDefault"/> (la principal, con precios base).
/// Las demás listas de tipo Segment derivan su precio aplicando
/// <see cref="AdjustmentPercent"/> sobre la lista por defecto.
/// <see cref="PriceListKind.Campaign"/> = oferta temporal con vigencia (Fase 4).
/// </summary>
public sealed class PriceList : BaseEntity<Guid>
{
    public string    Name              { get; private set; } = default!;
    public string?   Description       { get; private set; }
    public string    CustomerSegment   { get; private set; } = default!;
    public DateTime  ValidFrom         { get; private set; }
    public DateTime? ValidTo           { get; private set; }  // null = sin fin
    public bool      IsActive          { get; private set; }
    public bool      IsDefault         { get; private set; }
    public decimal?  AdjustmentPercent { get; private set; }  // null en la default; +/− en derivadas
    public PriceListKind ListKind      { get; private set; }
    public Guid?     OwnerId           { get; private set; }  // null = global

    public DateTime  CreatedAtUtc      { get; private set; }
    public DateTime? UpdatedAtUtc      { get; private set; }

    public ICollection<PriceListItem> Items { get; private set; } = new List<PriceListItem>();

    private PriceList() { }

    public static PriceList Create(
        string name,
        string customerSegment,
        DateTime validFrom,
        string? description = null,
        Guid? ownerId = null,
        bool isDefault = false,
        decimal? adjustmentPercent = null,
        PriceListKind listKind = PriceListKind.Segment,
        DateTime? validTo = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(customerSegment);

        return new PriceList
        {
            Id                = Guid.CreateVersion7(),
            Name              = name.Trim(),
            Description       = description?.Trim(),
            CustomerSegment   = customerSegment.Trim().ToLowerInvariant(),
            ValidFrom         = validFrom,
            ValidTo           = validTo,
            IsActive          = true,
            IsDefault         = isDefault,
            AdjustmentPercent = isDefault ? null : adjustmentPercent,
            ListKind          = listKind,
            OwnerId           = ownerId,
            CreatedAtUtc      = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string? description,
        DateTime validFrom,
        DateTime? validTo,
        bool isActive,
        decimal? adjustmentPercent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name              = name.Trim();
        Description       = description?.Trim();
        ValidFrom         = validFrom;
        ValidTo           = validTo;
        IsActive          = isActive;
        AdjustmentPercent = IsDefault ? null : adjustmentPercent;
        UpdatedAtUtc      = DateTime.UtcNow;
    }

    /// <summary>Marca esta lista como la principal (única). La default no lleva %.</summary>
    public void MakeDefault()
    {
        IsDefault         = true;
        AdjustmentPercent = null;
        UpdatedAtUtc      = DateTime.UtcNow;
    }

    public void ClearDefault()
    {
        IsDefault    = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Cierra la lista (al crear una nueva del mismo segmento) — spec §7.</summary>
    public void Close(DateTime validTo)
    {
        ValidTo      = validTo;
        IsActive     = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
