using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.PublishProduct;

public sealed record PublishProductCommand(Guid Id) : ICommand<Guid>;
