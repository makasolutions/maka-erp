using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.PriceProposals.GetPriceProposals;

public sealed record PriceProposalDto(
    Guid      Id,
    Guid      BatchId,
    Guid      PriceListId,
    Guid      VariationId,
    string?   VariationSku,
    Guid      SupplierId,
    string    SupplierCode,
    decimal?  OldPrice,
    decimal   NewPrice,
    PriceProposalStatus Status,
    string?   ChangeReason,
    string?   SourceReference,
    DateTime  CreatedAtUtc,
    string    CreatedByUserId,
    DateTime? DecidedAtUtc,
    string?   DecidedByUserId);
