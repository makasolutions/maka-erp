using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// PriceListItem — precio de una variación dentro de una lista — spec §2.14 + §15.
/// SalePrice + rango de fechas vive aquí (no en Product): dentro del rango el
/// precio efectivo es SalePrice; fuera, Price.
/// Toda actualización de precio registra un PriceListItemHistory (inmutable).
/// </summary>
public sealed class PriceListItem : BaseEntity<Guid>
{
    public Guid      PriceListId     { get; private set; }
    public Guid      VariationId     { get; private set; }
    public decimal   Price           { get; private set; }  // COP
    public decimal?  MinQuantity     { get; private set; }

    // Fase 3 — en listas derivadas, true si el usuario fijó el precio a mano
    // (no se recalcula al cambiar la lista por defecto). false = sigue el %.
    public bool      IsManualOverride { get; private set; }

    // Precio de oferta (§15) — efectivo dentro del rango de fechas
    public decimal?  SalePrice       { get; private set; }
    public DateTime? SalePriceFrom   { get; private set; }
    public DateTime? SalePriceTo     { get; private set; }

    public DateTime  CreatedAt       { get; private set; }
    public string    CreatedByUserId { get; private set; } = default!;
    public DateTime? UpdatedAtUtc    { get; private set; }

    public ICollection<PriceListItemHistory> History { get; private set; } = new List<PriceListItemHistory>();

    private PriceListItem() { }

    public static PriceListItem Create(
        Guid priceListId,
        Guid variationId,
        decimal price,
        string createdByUserId,
        decimal? minQuantity = null,
        decimal? salePrice = null,
        DateTime? salePriceFrom = null,
        DateTime? salePriceTo = null,
        bool isManualOverride = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(price);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);

        return new PriceListItem
        {
            Id               = Guid.CreateVersion7(),
            PriceListId      = priceListId,
            VariationId      = variationId,
            Price            = price,
            MinQuantity      = minQuantity,
            IsManualOverride = isManualOverride,
            SalePrice        = salePrice,
            SalePriceFrom    = salePriceFrom,
            SalePriceTo      = salePriceTo,
            CreatedAt        = DateTime.UtcNow,
            CreatedByUserId  = createdByUserId,
        };
    }

    /// <summary>Marca si el precio fue fijado manualmente (excluido del recálculo).</summary>
    public void SetManualOverride(bool isManualOverride) => IsManualOverride = isManualOverride;

    /// <summary>
    /// Aplica un precio derivado de la lista por defecto (recálculo automático).
    /// No marca override y registra el cambio en History.
    /// </summary>
    public void ApplyDerived(decimal newPrice, string changedByUserId)
    {
        if (IsManualOverride) return;       // respeta el override del usuario
        if (newPrice == Price) return;      // nada que cambiar
        ChangePrice(newPrice, changedByUserId, changeReason: "auto:default", sourceReference: "auto:default");
        IsManualOverride = false;           // ChangePrice no lo toca; lo dejamos explícito
    }

    /// <summary>
    /// Cambia el precio y registra el cambio en History (spec §7 — obligatorio).
    /// </summary>
    public void ChangePrice(
        decimal newPrice,
        string changedByUserId,
        string? changeReason = null,
        string? sourceReference = null,
        decimal? newSalePrice = null,
        DateTime? newSalePriceFrom = null,
        DateTime? newSalePriceTo = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(newPrice);
        ArgumentException.ThrowIfNullOrWhiteSpace(changedByUserId);

        var history = PriceListItemHistory.Record(
            priceListItemId: Id,
            variationId: VariationId,
            oldPrice: Price,
            newPrice: newPrice,
            changedByUserId: changedByUserId,
            oldSalePrice: SalePrice,
            newSalePrice: newSalePrice,
            oldSalePriceFrom: SalePriceFrom,
            newSalePriceFrom: newSalePriceFrom,
            oldSalePriceTo: SalePriceTo,
            newSalePriceTo: newSalePriceTo,
            changeReason: changeReason,
            sourceReference: sourceReference);

        Price         = newPrice;
        SalePrice     = newSalePrice;
        SalePriceFrom = newSalePriceFrom;
        SalePriceTo   = newSalePriceTo;
        UpdatedAtUtc  = DateTime.UtcNow;

        History.Add(history);
    }
}
