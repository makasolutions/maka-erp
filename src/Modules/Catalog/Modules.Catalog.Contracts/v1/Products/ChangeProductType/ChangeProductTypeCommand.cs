using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.ChangeProductType;

/// <summary>Convert a product between Simple and Variable (only).</summary>
public sealed record ChangeProductTypeCommand(Guid ProductId, ProductType NewType) : ICommand<Guid>;
