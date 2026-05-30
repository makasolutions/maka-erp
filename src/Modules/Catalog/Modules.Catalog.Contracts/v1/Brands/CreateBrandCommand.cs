using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands;

public sealed record CreateBrandCommand(
    string Code,
    string Name,
    string? Description = null,
    string? LogoUrl = null,
    bool IsActive = true,
    bool IsVisible = true) : ICommand<Guid>;
