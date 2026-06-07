using FSH.Framework.Shared.Persistence;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributes;

public sealed record GetAttributesQuery : IPagedQuery, IQuery<PagedResponse<AttributeDto>>
{
    public int?    PageNumber { get; set; } = 1;
    public int?    PageSize   { get; set; } = 50;
    public string? Sort       { get; set; }
    public string? Search     { get; set; }
    public bool?   IsUsedForVariations { get; set; }
    public Guid?   CategoryId { get; set; }
}
