using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Brands.DeleteBrand;

public sealed record DeleteBrandCommand(Guid Id) : ICommand<Guid>;
