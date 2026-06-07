using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceLists;

public sealed record PriceListDto(
    Guid      Id,
    string    Name,
    string?   Description,
    string    CustomerSegment,
    DateTime  ValidFrom,
    DateTime? ValidTo,
    bool      IsActive,
    bool      IsDefault,
    decimal?  AdjustmentPercent,
    bool      RoundEnabled,
    PriceListKind ListKind,
    CampaignStatus? CampaignStatus,
    int       ItemCount,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc);
