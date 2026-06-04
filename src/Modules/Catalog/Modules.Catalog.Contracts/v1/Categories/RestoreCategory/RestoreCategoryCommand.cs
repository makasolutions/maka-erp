using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.RestoreCategory;

public sealed record RestoreCategoryCommand(Guid Id) : ICommand<Guid>;
