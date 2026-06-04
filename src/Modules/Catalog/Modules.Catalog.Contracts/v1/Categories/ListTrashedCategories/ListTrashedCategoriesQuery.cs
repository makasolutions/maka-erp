using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Categories.ListTrashedCategories;

public sealed record ListTrashedCategoriesQuery : IPagedQuery, IQuery<PagedResponse<TrashedCategoryDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public string? Search     { get; set; }
}
