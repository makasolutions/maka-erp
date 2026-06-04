using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.CreateBrand;

public sealed record CreateBrandCommand(
    string  Name,
    string? Slug,
    string? Description,
    string? LogoUrl,
    string? WebsiteUrl,
    string? CountryOfOrigin,
    bool    IsActive = true) : ICommand<Guid>;
