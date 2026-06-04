using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.CreateProduct;

public sealed record CreateProductCommand(
    string       Name,
    string?      Slug,
    ProductType  Type,
    string?      ShortDescription,
    Guid?        BrandId,
    Guid?        TaxRateId,
    Guid?        ShippingClassId,
    string?      DefaultSku,
    bool         IsPublic = false) : ICommand<Guid>;
