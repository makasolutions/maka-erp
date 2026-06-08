using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Scorecards;

// ── KPI catalog ──
public sealed record UpsertScorecardKpiCommand(
    Guid?   Id,
    string  Code,
    string  Name,
    decimal Weight,
    int     SortOrder,
    bool    IsActive) : ICommand<Guid>;

public sealed record DeleteScorecardKpiCommand(Guid Id) : ICommand;

/// <summary>Siembra los KPIs por defecto si el catálogo del tenant está vacío. Devuelve cuántos creó.</summary>
public sealed record SeedDefaultKpisCommand : ICommand<int>;

// ── Scorecards ──
public sealed record CreateSupplierScorecardCommand(
    Guid     SupplierId,
    string   PeriodLabel,
    DateTime PeriodStart,
    string?  Notes,
    IReadOnlyList<ScorecardCriterionInput>? Criteria) : ICommand<Guid>;

public sealed record UpdateSupplierScorecardCommand(
    Guid     Id,
    string   PeriodLabel,
    DateTime PeriodStart,
    string?  Notes,
    IReadOnlyList<ScorecardCriterionInput>? Criteria) : ICommand<Guid>;

public sealed record CloseScorecardCommand(Guid Id) : ICommand;

public sealed record DeleteScorecardCommand(Guid Id) : ICommand;
