using FSH.Framework.Shared.Persistence;
using FSH.Modules.Auditing.Contracts;
using FSH.Modules.Auditing.Contracts.Dtos;
using Mediator;

namespace FSH.Modules.Auditing.Contracts.v1.GetAudits;

public sealed class GetAuditsQuery : IPagedQuery, IQuery<PagedResponse<AuditSummaryDto>>
{
    public int? PageNumber { get; set; }

    public int? PageSize { get; set; }

    public string? Sort { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public string? TenantId { get; set; }

    public string? UserId { get; set; }

    public AuditEventType? EventType { get; set; }

    public AuditSeverity? Severity { get; set; }

    public AuditTag? Tags { get; set; }

    public string? Source { get; set; }

    public string? CorrelationId { get; set; }

    public string? TraceId { get; set; }

    public string? Search { get; set; }

    // ── Entity-change filters (require the denormalized columns added in
    //    the Auditing_AddEntityColumns migration) ─────────────────────────

    /// <summary>Exact or partial match on EntityName (e.g. "Brand", "Category").</summary>
    public string? EntityName { get; set; }

    /// <summary>Exact match on EntityKey (the entity's primary key string).</summary>
    public string? EntityKey { get; set; }

    /// <summary>Exact match on EntityOperation (e.g. "Insert", "Update", "Delete").</summary>
    public string? EntityOperation { get; set; }
}