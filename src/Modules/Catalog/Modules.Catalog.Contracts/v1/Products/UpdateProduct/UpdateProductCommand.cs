using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid     Id,
    string   Name,
    string?  ShortDescription,
    string?  Description,
    string?  TechnicalSpecs,
    Guid?    BrandId,
    Guid?    TaxRateId,
    Guid?    ShippingClassId,
    bool     IsVirtual,
    bool     IsDownloadable,
    bool     IsPublic,
    decimal? Weight,
    string   WeightUnit,
    decimal? DimensionLength,
    decimal? DimensionWidth,
    decimal? DimensionHeight,
    string   DimensionUnit,
    string?  SeoTitle,
    string?  SeoDescription,
    string?  SeoKeywords,
    string?  Specs = null) : ICommand<Guid>;
