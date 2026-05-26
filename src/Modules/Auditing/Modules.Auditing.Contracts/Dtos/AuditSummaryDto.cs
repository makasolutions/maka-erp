using FSH.Modules.Auditing.Contracts;

namespace FSH.Modules.Auditing.Contracts.Dtos;

public sealed class AuditSummaryDto
{
    public Guid Id { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public AuditEventType EventType { get; set; }

    public AuditSeverity Severity { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public string? TraceId { get; set; }

    public string? CorrelationId { get; set; }

    public string? RequestId { get; set; }

    public string? Source { get; set; }

    public AuditTag Tags { get; set; }

    // Populated for EntityChange events only; null for other event types.
    public string? EntityName { get; set; }
    public string? EntityKey { get; set; }
    public string? EntityOperation { get; set; }
}