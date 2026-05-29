using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAuditSummary;

public sealed class GetAuditSummaryQuery : IQuery<AuditSummaryAggregateDto>
{
    public DateTime? FromUtc { get; init; }

    public DateTime? ToUtc { get; init; }

    public string? TenantId { get; init; }

    // ── Optional filters — mirror GetAuditsQuery so the KPI aggregates reflect
    //    exactly the same filtered set the grid is showing. ──────────────────

    public string? UserId { get; init; }

    public AuditEventType? EventType { get; init; }

    public AuditSeverity? Severity { get; init; }

    public AuditTag? Tags { get; init; }

    public string? Source { get; init; }

    public string? Search { get; init; }

    public string? EntityName { get; init; }

    public string? EntityKey { get; init; }

    public string? EntityOperation { get; init; }
}