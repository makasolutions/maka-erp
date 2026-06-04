using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.RestoreProduct;

public sealed record RestoreProductCommand(Guid Id) : ICommand<Guid>;
