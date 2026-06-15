using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Webhooks.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Webhooks.Services;

/// <summary>
/// Open-generic bridge that fans every published integration event out to the
/// tenant's active webhook subscriptions, then enqueues a delivery job per
/// subscription via <see cref="IWebhookDispatcher"/>.
///
/// Registered as an open generic in <c>WebhooksModule</c> for the legacy bus
/// (DI materializes a closed handler for any <typeparamref name="TEvent"/>),
/// AND registrado como closed-generic explicit en <c>Program.cs</c> para cada
/// evento migrado a Wolverine (vía <c>opts.Discovery.IncludeType</c>). Cada vez
/// que un evento nuevo migre a Wolverine, hay que añadir su registro cerrado.
///
/// Tenant scoping: el evento carga su propio <c>TenantId</c> y el
/// <see cref="FSH.Framework.Eventing.Tenant.TenantContextMiddleware"/> global de
/// Wolverine restaura el Finbuckle <c>ITenantInfo</c> ANTES de invocar este handler
/// (Fase 3 Paso 3). El antiguo bloque de restauración manual se eliminó. Eventos
/// con <c>TenantId</c> nulo se descartan — las subscriptions son tenant-only.
///
/// Wolverine descubre <c>HandleAsync</c> por convención. Para closed generics
/// registrados via <c>IncludeType</c>, el codegen materializa la firma correcta.
/// </summary>
public sealed class WebhookFanoutHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly WebhookDbContext _db;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly IEventSerializer _serializer;
    private readonly ILogger<WebhookFanoutHandler<TEvent>> _logger;

    public WebhookFanoutHandler(
        WebhookDbContext db,
        IWebhookDispatcher dispatcher,
        IEventSerializer serializer,
        ILogger<WebhookFanoutHandler<TEvent>> logger)
    {
        _db = db;
        _dispatcher = dispatcher;
        _serializer = serializer;
        _logger = logger;
    }

    public async Task HandleAsync(TEvent @event, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (string.IsNullOrWhiteSpace(@event.TenantId))
        {
            // Global events are not deliverable via tenant-scoped subscriptions.
            return;
        }

        var eventType = typeof(TEvent).Name;

        // Pull active subscriptions for this tenant; filter by event type in memory
        // because EventsCsv stores a CSV blob, not a join table — there are typically
        // 0–20 subscriptions per tenant so in-memory matching is fine.
        //
        // IMPORTANT: scope by the event's TenantId explicitly via IgnoreQueryFilters
        // por consistencia con la doctrina del bus propio (el dispatcher background
        // anteriormente NO carry tenant). Con Wolverine + TenantContextMiddleware el
        // Finbuckle filter ya está poblado correctamente, pero mantenemos el patrón
        // para que el handler funcione igual si por alguna razón el middleware no se
        // ejecuta (ej. invocación in-process out-of-pipeline).
        var subscriptions = await _db.Subscriptions
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.IsActive && EF.Property<string>(s, "TenantId") == @event.TenantId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var matching = subscriptions.Where(s => s.MatchesEvent(eventType)).ToList();
        if (matching.Count == 0)
        {
            return;
        }

        var payload = _serializer.Serialize(@event);
        foreach (var subscription in matching)
        {
            try
            {
                await _dispatcher
                    .EnqueueAsync(@event.TenantId, subscription.Id, eventType, payload, ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One bad subscription must not abort fan-out to others — the dispatcher
                // job itself has its own retry policy, this catch is for synchronous
                // enqueue-side failures (Hangfire transient errors etc).
                _logger.LogWarning(
                    ex,
                    "Failed to enqueue webhook delivery for subscription {SubscriptionId} (tenant {TenantId}, event {EventType})",
                    subscription.Id,
                    @event.TenantId,
                    eventType);
            }
        }
    }
}
