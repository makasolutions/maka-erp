using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

public sealed record UpdateBrandCommand(
    Guid BrandId,
    string Code,
    string Name,
    string? Description = null,
    string? LogoUrl = null,
    bool IsActive = true,
    bool IsVisible = true) : ICommand<Guid>;
