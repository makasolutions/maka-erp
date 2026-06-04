namespace FSH.Modules.Catalog.Contracts.v1.Brands.ListTrashedBrands;

public sealed record TrashedBrandDto(
    Guid            Id,
    string          Name,
    string          Slug,
    bool            IsActive,
    DateTimeOffset? DeletedOnUtc,
    string?         DeletedBy);
