using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Products.ListTrashedProducts;

public sealed record TrashedProductDto(
    Guid            Id,
    string          Name,
    string          Slug,
    ProductType     Type,
    ProductStatus   Status,
    DateTimeOffset? DeletedOnUtc,
    string?         DeletedBy);
