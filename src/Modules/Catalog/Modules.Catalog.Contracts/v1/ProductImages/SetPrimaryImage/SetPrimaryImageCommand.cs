using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductImages.SetPrimaryImage;

public sealed record SetPrimaryImageCommand(
    Guid ProductId,
    Guid ImageId) : ICommand<Guid>;
