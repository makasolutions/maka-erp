namespace FSH.Modules.Auditing;

public sealed class AuditRecord
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime ReceivedAtUtc { get; set; }

    public int EventType { get; set; }
    public byte Severity { get; set; }

    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? Source { get; set; }

    public long Tags { get; set; }

    // ── Denormalized entity-change fields ──────────────────────────────────
    // Extracted from PayloadJson when EventType == EntityChange so that UI
    // filters ("show changes to Brand", "key = abc-123") can use index probes
    // instead of jsonb containment operators or sequential scans.
    // Null for non-entity events (Security, Activity, Exception).

    /// <summary>The simple class name from EntityChangeEventPayload.EntityName (e.g. "Brand").</summary>
    public string? EntityName { get; set; }

    /// <summary>The primary-key string from EntityChangeEventPayload.Key (e.g. a GUID).</summary>
    public string? EntityKey { get; set; }

    /// <summary>The operation name from EntityChangeEventPayload.Operation (e.g. "Insert", "Update", "Delete").</summary>
    public string? EntityOperation { get; set; }

    public string PayloadJson { get; set; } = default!;
}