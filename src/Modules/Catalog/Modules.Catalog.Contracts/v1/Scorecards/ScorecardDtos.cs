using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Scorecards;

public sealed record ScorecardKpiDto(Guid Id, string Code, string Name, decimal Weight, int SortOrder, bool IsActive);

public sealed record ScorecardCriterionInput(string KpiCode, string KpiName, decimal Weight, int Score, string? Comment);

public sealed record ScorecardCriterionDto(Guid Id, string KpiCode, string KpiName, decimal Weight, int Score, string? Comment);

/// <summary>Fila de listado de scorecards.</summary>
public sealed record ScorecardDto(
    Guid            Id,
    Guid            SupplierId,
    string?         SupplierName,
    string          PeriodLabel,
    DateTime        PeriodStart,
    ScorecardStatus Status,
    decimal         WeightedScore,
    ScorecardGrade  Grade);

/// <summary>Detalle de un scorecard (con criterios).</summary>
public sealed record ScorecardDetailDto(
    Guid            Id,
    Guid            SupplierId,
    string?         SupplierName,
    string          PeriodLabel,
    DateTime        PeriodStart,
    ScorecardStatus Status,
    decimal         WeightedScore,
    ScorecardGrade  Grade,
    string?         Notes,
    bool            IsMutable,
    IReadOnlyList<ScorecardCriterionDto> Criteria);

/// <summary>Fila de ranking de proveedores (último scorecard por proveedor).</summary>
public sealed record SupplierRankingDto(
    Guid           SupplierId,
    string?        SupplierName,
    string         PeriodLabel,
    decimal        WeightedScore,
    ScorecardGrade Grade);

/// <summary>Punto de la serie de tendencia de un proveedor.</summary>
public sealed record ScoreTrendPointDto(string PeriodLabel, DateTime PeriodStart, decimal WeightedScore);
