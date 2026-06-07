using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.DuplicateProduct;

/// <summary>Duplicate a product (codes are cleared on the copy). Returns the new id.</summary>
public sealed record DuplicateProductCommand(Guid ProductId) : ICommand<Guid>;
