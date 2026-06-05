namespace FSH.Modules.Catalog.Contracts.v1.TaxRates.GetTaxRates;

public sealed record TaxRateDto(
    Guid    Id,
    string  Name,
    decimal Rate,
    string? Description,
    bool    IsDefault,
    bool    IsActive);
