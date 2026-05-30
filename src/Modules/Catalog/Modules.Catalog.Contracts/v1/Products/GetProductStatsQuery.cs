using FSH.Modules.Catalog.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products;

/// <summary>
/// Catalog-wide product counts (total / active / visible) for KPI cards.
/// </summary>
public sealed record GetProductStatsQuery() : IQuery<ProductStatsDto>;
