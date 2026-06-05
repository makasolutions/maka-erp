using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.TenantProducts.UpdateTenantProduct;

public sealed record UpdateTenantProductCommand(
    Guid     Id,
    string?  NameOverride,
    string?  ShortDescriptionOverride,
    string?  DescriptionOverride,
    string?  TechnicalSpecsOverride,
    string?  SeoTitleOverride,
    string?  SeoDescriptionOverride,
    decimal? DropshippingPrice,
    decimal? DropshippingMinQty,
    bool     IsActive,
    bool     IsPublic) : ICommand<Guid>;
