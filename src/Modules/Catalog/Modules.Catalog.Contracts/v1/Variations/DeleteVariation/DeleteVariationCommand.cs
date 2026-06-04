using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.DeleteVariation;

public sealed record DeleteVariationCommand(Guid ProductId, Guid Id) : ICommand<Guid>;
