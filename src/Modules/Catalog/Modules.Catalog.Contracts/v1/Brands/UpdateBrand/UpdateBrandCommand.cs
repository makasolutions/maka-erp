using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.UpdateBrand;

public sealed record UpdateBrandCommand(
    Guid    Id,
    string  Name,
    string? Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? CountryOfOrigin,
    bool    IsActive) : ICommand<Guid>;
