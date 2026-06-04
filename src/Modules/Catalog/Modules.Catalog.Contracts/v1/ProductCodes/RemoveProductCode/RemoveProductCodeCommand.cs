using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductCodes.RemoveProductCode;

public sealed record RemoveProductCodeCommand(
    Guid ProductId,
    Guid VariationId,
    Guid CodeId) : ICommand<Guid>;
