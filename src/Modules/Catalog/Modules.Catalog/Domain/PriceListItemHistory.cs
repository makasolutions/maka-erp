using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// PriceListItemHistory — registro INMUTABLE de cada cambio de precio — spec §2.14 + §15.
/// Nunca se edita ni elimina. Creado vía PriceListItem.ChangePrice().
/// </summary>
public sealed class PriceListItemHistory : BaseEntity<Guid>
{
    public Guid     PriceListItemId  { get; private set; }
    public Guid     VariationId      { get; private set; }

    // Precio normal
    public decimal  OldPrice         { get; private set; }
    public decimal  NewPrice         { get; private set; }

    // Precio de oferta (§15)
    public decimal?  OldSalePrice     { get; private set; }
    public decimal?  NewSalePrice     { get; private set; }
    public DateTime? OldSalePriceFrom { get; private set; }
    public DateTime? NewSalePriceFrom { get; private set; }
    public DateTime? OldSalePriceTo   { get; private set; }
    public DateTime? NewSalePriceTo   { get; private set; }

    // Auditoría
    public DateTime ChangedAt        { get; private set; }
    public string   ChangedByUserId  { get; private set; } = default!;
    public string?  ChangeReason     { get; private set; }
    public string?  SourceReference  { get; private set; }

    private PriceListItemHistory() { }

    internal static PriceListItemHistory Record(
        Guid priceListItemId,
        Guid variationId,
        decimal oldPrice,
        decimal newPrice,
        string changedByUserId,
        decimal? oldSalePrice = null,
        decimal? newSalePrice = null,
        DateTime? oldSalePriceFrom = null,
        DateTime? newSalePriceFrom = null,
        DateTime? oldSalePriceTo = null,
        DateTime? newSalePriceTo = null,
        string? changeReason = null,
        string? sourceReference = null)
        => new()
        {
            Id               = Guid.CreateVersion7(),
            PriceListItemId  = priceListItemId,
            VariationId      = variationId,
            OldPrice         = oldPrice,
            NewPrice         = newPrice,
            OldSalePrice     = oldSalePrice,
            NewSalePrice     = newSalePrice,
            OldSalePriceFrom = oldSalePriceFrom,
            NewSalePriceFrom = newSalePriceFrom,
            OldSalePriceTo   = oldSalePriceTo,
            NewSalePriceTo   = newSalePriceTo,
            ChangedAt        = DateTime.UtcNow,
            ChangedByUserId  = changedByUserId,
            ChangeReason     = changeReason,
            SourceReference  = sourceReference,
        };
}
