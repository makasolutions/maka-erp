using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;

public sealed record AddProductImageCommand(
    Guid    ProductId,
    string  Url,
    string? AltText   = null,
    bool    IsPrimary = false,
    int     SortOrder = 0) : ICommand<Guid>;
