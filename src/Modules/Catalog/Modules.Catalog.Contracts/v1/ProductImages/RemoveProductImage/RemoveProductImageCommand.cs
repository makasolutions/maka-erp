using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductImages.RemoveProductImage;

public sealed record RemoveProductImageCommand(
    Guid ProductId,
    Guid ImageId) : ICommand<Guid>;
