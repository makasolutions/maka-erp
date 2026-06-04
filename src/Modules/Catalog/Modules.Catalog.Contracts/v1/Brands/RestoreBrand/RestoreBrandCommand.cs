using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.RestoreBrand;

public sealed record RestoreBrandCommand(Guid Id) : ICommand<Guid>;
