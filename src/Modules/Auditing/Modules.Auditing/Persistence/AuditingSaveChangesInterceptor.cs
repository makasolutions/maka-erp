using FSH.Modules.Auditing.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Auditing.Persistence;

/// <summary>
/// Captures EF Core entity changes at SaveChanges to produce an EntityChange event.
///
/// SINGLETON (scope-safe): NO captura <c>IAuditScope</c> (scoped) en el ctor — lo resuelve LAZY
/// desde el scope ambiente en SaveChanges. Necesario porque los DbContext con Wolverine tienen
/// options singleton y EF resuelve los interceptores desde el root provider (un interceptor scoped
/// rompe esa resolución → era el 3.º que faltaba para el login 500). En HTTP usa el IAuditScope del
/// request; en background (jobs Hangfire, sin HttpContext) usa un scope FRESCO — <c>HttpAuditScope</c>
/// lee tenant/trace desde AsyncLocal (Finbuckle / Activity.Current, poblados por el FshJobActivator),
/// que fluyen al scope fresco → el contexto de auditoría del job se preserva, idéntico a hoy.
/// </summary>
public sealed class AuditingSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditPublisher _publisher;
    private readonly TimeProvider _timeProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;

    public AuditingSaveChangesInterceptor(
        IAuditPublisher publisher,
        TimeProvider timeProvider,
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory)
    {
        _publisher = publisher;
        _timeProvider = timeProvider;
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
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
        if (diffs.Count == 0) return result;

        // Resolución LAZY del IAuditScope desde el scope ambiente: el del request (HTTP) o uno fresco
        // (background). HttpAuditScope lee tenant/trace desde AsyncLocal (Finbuckle/Activity), que
        // fluye al scope fresco → el contexto del job se preserva (idéntico a hoy).
        var requestServices = _httpContextAccessor.HttpContext?.RequestServices;
        IServiceScope? backgroundScope = null;
        try
        {
            var auditScope = requestServices?.GetService<IAuditScope>();
            if (auditScope is null)
            {
                backgroundScope = _scopeFactory.CreateScope();
                auditScope = backgroundScope.ServiceProvider.GetRequiredService<IAuditScope>();
            }

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
                    tenantId: auditScope.TenantId,
                    userId: auditScope.UserId,
                    userName: auditScope.UserName,
                    traceId: auditScope.TraceId,
                    spanId: auditScope.SpanId,
                    correlationId: auditScope.CorrelationId,
                    requestId: auditScope.RequestId,
                    source: ctx.GetType().Name,
                    tags: AuditTag.None,
                    payload: payload);

                await _publisher.PublishAsync(env, cancellationToken);
            }
        }
        finally
        {
            backgroundScope?.Dispose();
        }

        return result;
    }
}