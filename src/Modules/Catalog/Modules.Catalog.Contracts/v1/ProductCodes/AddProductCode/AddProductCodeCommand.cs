using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductCodes.AddProductCode;

public sealed record AddProductCodeCommand(
    Guid    ProductId,
    Guid    VariationId,
    string  CodeType,
    string  Code,
    Guid?   SupplierId = null,
    bool    IsPrimary  = false) : ICommand<Guid>;
