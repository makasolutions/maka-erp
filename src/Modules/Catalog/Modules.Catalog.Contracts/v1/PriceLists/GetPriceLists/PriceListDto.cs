namespace FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceLists;

public sealed record PriceListDto(
    Guid      Id,
    string    Name,
    string?   Description,
    string    CustomerSegment,
    DateTime  ValidFrom,
    DateTime? ValidTo,
    bool      IsActive,
    int       ItemCount,
    DateTime  CreatedAtUtc,
    DateTime? UpdatedAtUtc);
