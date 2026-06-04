using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand<Guid>;
