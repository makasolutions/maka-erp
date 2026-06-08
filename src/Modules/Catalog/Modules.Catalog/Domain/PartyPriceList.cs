using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Asignación de una lista de precios a un tercero (cliente o proveedor).
/// Determina qué tarifa aplica a ese tercero (base para Ventas y CxC/CxP, y para
/// el costo que ve un distribuidor según el convenio). Tenant-aislada.
/// </summary>
public sealed class PartyPriceList : BaseEntity<Guid>
{
    public Guid      PartyId     { get; private set; }
    public Guid      PriceListId { get; private set; }
    public DateTime? ValidFrom   { get; private set; }
    public DateTime? ValidTo     { get; private set; }
    public bool      IsActive    { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private PartyPriceList() { }

    public static PartyPriceList Create(Guid partyId, Guid priceListId, DateTime? validFrom = null, DateTime? validTo = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        if (priceListId == Guid.Empty) throw new ArgumentException("PriceListId requerido.", nameof(priceListId));
        return new PartyPriceList
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            PriceListId = priceListId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(DateTime? validFrom, DateTime? validTo, bool isActive)
    {
        ValidFrom = validFrom;
        ValidTo = validTo;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
