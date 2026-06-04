using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.ArchiveProduct;

public sealed record ArchiveProductCommand(Guid Id) : ICommand<Guid>;
