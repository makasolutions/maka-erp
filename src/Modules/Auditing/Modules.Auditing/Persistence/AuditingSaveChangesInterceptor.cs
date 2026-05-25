using FSH.Modules.Auditing.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Captures EF Core entity changes at SaveChanges to produce an EntityChange event.
/// Registered as Scoped so it can safely inject the ambient IAuditScope, which
/// provides TenantId/UserId/UserName from the current HTTP or Hangfire execution context.
/// </summary>
public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly IAuditScope _auditScope;

    public AuditingSaveChangesInterceptor(
        IAuditPublisher publisher,
        TimeProvider timeProvider,
        IAuditScope auditScope)
    {
        _publisher = publisher;
        _timeProvider = timeProvider;
        _auditScope = auditScope;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var ctx = eventData.Context;
        if (ctx is null) return result;

        // Never audit the audit store's own writes — it would recurse: each flush would
        // capture the AuditRecord inserts, whose PayloadJson embeds the prior PayloadJson,
        // growing exponentially until System.Text.Json rejects the payload.
        if (ctx is AuditDbContext) return result;

        var entries = ctx.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();

        if (entries.Length == 0) return result;

        var diffs = EntityDiffBuilder.Build(entries);

        if (diffs.Count > 0)
        {
            foreach (var group in diffs.GroupBy(d => (d.DbContext, d.Schema, d.Table, d.EntityName, d.Key, d.Operation)))
            {
                var payload = new EntityChangeEventPayload(
                    DbContext: group.Key.DbContext,
                    Schema: group.Key.Schema,
                    Table: group.Key.Table,
                    EntityName: group.Key.EntityName,
                    Key: group.Key.Key,
                    Operation: group.Key.Operation,
                    Changes: group.SelectMany(g => g.Changes).ToList(),
                    TransactionId: ctx.Database.CurrentTransaction?.TransactionId.ToString());

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var env = new AuditEnvelope(
                    id: Guid.CreateVersion7(),
                    occurredAtUtc: now,
                    receivedAtUtc: now,
                    eventType: AuditEventType.EntityChange,
                    severity: AuditSeverity.Information,
                    tenantId: _auditScope.TenantId,
                    userId: _auditScope.UserId,
                    userName: _auditScope.UserName,
                    traceId: _auditScope.TraceId,
                    spanId: _auditScope.SpanId,
                    correlationId: _auditScope.CorrelationId,
                    requestId: _auditScope.RequestId,
                    source: ctx.GetType().Name,
                    tags: AuditTag.None,
                    payload: payload);

                await _publisher.PublishAsync(env, cancellationToken);
            }
        }

        return result;
    }
}