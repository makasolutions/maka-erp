namespace FSH.Modules.Catalog.Contracts.v1.ProductCodes.GetProductCodes;

public sealed record ProductCodeDto(
    Guid    Id,
    Guid    VariationId,
    string  CodeType,
    string  Code,
    bool    IsPrimary,
    Guid?   SupplierId,
    DateTime CreatedAtUtc);
