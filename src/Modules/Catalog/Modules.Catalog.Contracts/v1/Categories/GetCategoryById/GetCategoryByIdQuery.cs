using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryDetailDto>;
