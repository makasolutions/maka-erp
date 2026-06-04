using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// PriceBulkProposal — propuesta de cambio de precio generada por importación
/// masiva desde CSV de proveedor (spec §16). Pendiente de aprobación de un operador.
/// Al aprobar: actualiza/crea el PriceListItem + registra PriceListItemHistory.
/// Las propuestas de un mismo CSV comparten BatchId.
/// </summary>
public sealed class PriceBulkProposal : BaseEntity<Guid>
{
    public Guid     BatchId         { get; private set; }
    public Guid     PriceListId     { get; private set; }
    public Guid     VariationId     { get; private set; }
    public Guid     SupplierId      { get; private set; }
    public string   SupplierCode    { get; private set; } = default!;
    public Guid?    PriceListItemId { get; private set; }  // item existente, o null = crear nuevo
    public decimal? OldPrice        { get; private set; }  // null si no había item
    public decimal  NewPrice        { get; private set; }

    public PriceProposalStatus Status { get; private set; }

    public string?  ChangeReason    { get; private set; }
    public string?  SourceReference { get; private set; }

    public DateTime  CreatedAtUtc    { get; private set; }
    public string    CreatedByUserId { get; private set; } = default!;
    public DateTime? DecidedAtUtc    { get; private set; }
    public string?   DecidedByUserId { get; private set; }

    private PriceBulkProposal() { }

    public static PriceBulkProposal Create(
        Guid batchId,
        Guid priceListId,
        Guid variationId,
        Guid supplierId,
        string supplierCode,
        decimal newPrice,
        string createdByUserId,
        Guid? priceListItemId = null,
        decimal? oldPrice = null,
        string? changeReason = null,
        string? sourceReference = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        ArgumentOutOfRangeException.ThrowIfNegative(newPrice);

        return new PriceBulkProposal
        {
            Id              = Guid.CreateVersion7(),
            BatchId         = batchId,
            PriceListId     = priceListId,
            VariationId     = variationId,
            SupplierId      = supplierId,
            SupplierCode    = supplierCode.Trim(),
            PriceListItemId = priceListItemId,
            OldPrice        = oldPrice,
            NewPrice        = newPrice,
            Status          = PriceProposalStatus.Pending,
            ChangeReason    = changeReason,
            SourceReference = sourceReference,
            CreatedAtUtc    = DateTime.UtcNow,
            CreatedByUserId = createdByUserId,
        };
    }

    public void Approve(string decidedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decidedByUserId);
        if (Status != PriceProposalStatus.Pending) return;
        Status          = PriceProposalStatus.Approved;
        DecidedAtUtc    = DateTime.UtcNow;
        DecidedByUserId = decidedByUserId;
    }

    public void Reject(string decidedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decidedByUserId);
        if (Status != PriceProposalStatus.Pending) return;
        Status          = PriceProposalStatus.Rejected;
        DecidedAtUtc    = DateTime.UtcNow;
        DecidedByUserId = decidedByUserId;
    }
}
