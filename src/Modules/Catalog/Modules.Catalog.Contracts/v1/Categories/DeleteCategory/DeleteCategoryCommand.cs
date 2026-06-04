using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(Guid Id) : ICommand<Guid>;
