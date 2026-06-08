using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// KPI ponderado del scorecard de proveedores (Fase F). Catálogo parametrizable por tenant:
/// cada KPI tiene un peso (%) que entra en el cálculo del score ponderado.
/// </summary>
public sealed class ScorecardKpi : BaseEntity<Guid>
{
    public string  Code      { get; private set; } = default!;
    public string  Name      { get; private set; } = default!;
    public decimal Weight    { get; private set; }   // 0–100
    public int     SortOrder { get; private set; }
    public bool    IsActive  { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private ScorecardKpi() { }

    public static ScorecardKpi Create(string code, string name, decimal weight, int sortOrder = 0, bool isActive = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ScorecardKpi
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = name.Trim(),
            Weight = weight,
            SortOrder = sortOrder,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string name, decimal weight, int sortOrder, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Weight = weight;
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Scorecard de evaluación de un proveedor por período. El score ponderado y la letra se
/// recalculan a partir de los criterios. Inmutable cuando <see cref="ScorecardStatus.Cerrado"/>.
/// </summary>
public sealed class SupplierScorecard : BaseEntity<Guid>
{
    public Guid            SupplierId    { get; private set; }
    public string          PeriodLabel   { get; private set; } = default!;
    public DateTime        PeriodStart   { get; private set; }
    public ScorecardStatus Status        { get; private set; }
    public string?         Notes         { get; private set; }
    public decimal         WeightedScore { get; private set; }
    public ScorecardGrade  Grade         { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<ScorecardCriterion> _criteria = [];
    public IReadOnlyCollection<ScorecardCriterion> Criteria => _criteria.AsReadOnly();

    private SupplierScorecard() { }

    public static SupplierScorecard Create(Guid supplierId, string periodLabel, DateTime periodStart, string? notes)
    {
        if (supplierId == Guid.Empty) throw new ArgumentException("SupplierId requerido.", nameof(supplierId));
        ArgumentException.ThrowIfNullOrWhiteSpace(periodLabel);
        return new SupplierScorecard
        {
            Id = Guid.CreateVersion7(),
            SupplierId = supplierId,
            PeriodLabel = periodLabel.Trim(),
            PeriodStart = periodStart.Kind == DateTimeKind.Utc ? periodStart : DateTime.SpecifyKind(periodStart, DateTimeKind.Utc),
            Status = ScorecardStatus.Borrador,
            Notes = notes?.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string periodLabel, DateTime periodStart, string? notes)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(periodLabel);
        PeriodLabel = periodLabel.Trim();
        PeriodStart = periodStart.Kind == DateTimeKind.Utc ? periodStart : DateTime.SpecifyKind(periodStart, DateTimeKind.Utc);
        Notes = notes?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ReplaceCriteria(IEnumerable<ScorecardCriterion> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        EnsureMutable();
        _criteria.Clear();
        foreach (var c in criteria) _criteria.Add(c);
        Recompute();
    }

    /// <summary>WeightedScore = Σ(score·peso)/Σ(peso) (1–5); Grade por umbrales.</summary>
    public void Recompute() => RecomputeFrom(_criteria);

    /// <summary>
    /// Recalcula el score a partir de una lista externa de criterios (cuando se gestionan a
    /// nivel de DbContext en el Update, sin poblar la colección rastreada).
    /// </summary>
    public void RecomputeFrom(IReadOnlyCollection<ScorecardCriterion> criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        decimal totalWeight = criteria.Sum(c => c.Weight);
        WeightedScore = totalWeight > 0
            ? Math.Round(criteria.Sum(c => c.Score * c.Weight) / totalWeight, 2)
            : 0m;
        Grade = GradeFor(WeightedScore);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static ScorecardGrade GradeFor(decimal score) => score switch
    {
        >= 4.5m => ScorecardGrade.A,
        >= 3.5m => ScorecardGrade.B,
        >= 2.5m => ScorecardGrade.C,
        >= 1.5m => ScorecardGrade.D,
        _       => ScorecardGrade.F,
    };

    public void Close()
    {
        if (Status == ScorecardStatus.Cerrado) return;
        Recompute();
        Status = ScorecardStatus.Cerrado;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool IsMutable => Status != ScorecardStatus.Cerrado;

    private void EnsureMutable()
    {
        if (Status == ScorecardStatus.Cerrado)
            throw new InvalidOperationException("Un scorecard cerrado es inmutable.");
    }
}

/// <summary>Criterio evaluado de un scorecard (snapshot del KPI al momento de evaluar).</summary>
public sealed class ScorecardCriterion : BaseEntity<Guid>
{
    public Guid    ScorecardId { get; private set; }
    public string  KpiCode     { get; private set; } = default!;
    public string  KpiName     { get; private set; } = default!;
    public decimal Weight      { get; private set; }   // snapshot
    public int     Score       { get; private set; }   // 1–5
    public string? Comment     { get; private set; }

    private ScorecardCriterion() { }

    public static ScorecardCriterion Create(Guid scorecardId, string kpiCode, string kpiName, decimal weight, int score, string? comment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kpiCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(kpiName);
        return new ScorecardCriterion
        {
            Id = Guid.CreateVersion7(),
            ScorecardId = scorecardId,
            KpiCode = kpiCode.Trim(),
            KpiName = kpiName.Trim(),
            Weight = weight,
            Score = score,
            Comment = comment?.Trim(),
        };
    }
}
