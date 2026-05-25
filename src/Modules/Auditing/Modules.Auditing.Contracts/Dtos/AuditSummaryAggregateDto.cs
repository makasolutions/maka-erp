using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing.Contracts.Dtos;

public sealed class AuditSummaryAggregateDto
{
    public IDictionary<AuditEventType, long> EventsByType { get; init; } =
        new Dictionary<AuditEventType, long>();

    public IDictionary<AuditSeverity, long> EventsBySeverity { get; init; } =
        new Dictionary<AuditSeverity, long>();

    public IDictionary<string, long> EventsBySource { get; init; } =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, long> EventsByTenant { get; init; } =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Distinct entity names seen in the window, sorted by change count descending.
    /// Populated only for EntityChange events. Used to drive the entity-name
    /// dropdown in the audit filter bar.
    /// </summary>
    public IReadOnlyList<string> TopEntityNames { get; init; } = [];
}