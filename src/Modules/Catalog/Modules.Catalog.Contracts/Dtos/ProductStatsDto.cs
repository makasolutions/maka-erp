namespace FSH.Modules.Catalog.Contracts.Dtos;

/// <summary>
/// Catalog-wide product counts for dashboard KPI cards. Computed server-side in
/// a single query so the UI doesn't fan out one count request per metric.
/// </summary>
public sealed record ProductStatsDto(long Total, long Active, long Visible);
