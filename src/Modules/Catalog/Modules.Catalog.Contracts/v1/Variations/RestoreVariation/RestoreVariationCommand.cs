using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.RestoreVariation;

public sealed record RestoreVariationCommand(Guid ProductId, Guid Id) : ICommand<Guid>;
