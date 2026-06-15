using FSH.Framework.Eventing.Abstractions;
using FSH.Framework.Eventing.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace FSH.Framework.Eventing;

/// <summary>
/// ADR-0005 — implementación del switch por evento durante la migración a Wolverine.
///
/// Por cada evento entrante, consulta <c>EventingOptions.IntegrationEventRouting[typeof(TEvent).Name]</c>:
///   - <c>"Wolverine"</c>: publica vía <c>IDbContextOutbox&lt;TDbContext&gt;.PublishAsync</c> con
///                          <c>DeliveryOptions.TenantId</c> propagado desde
///                          <c>integrationEvent.TenantId</c> (que el publicador ya lee del
///                          Finbuckle context al construir el evento — INV-9 estructural).
///                          El envelope queda enrolado en el outbox EF; se persistirá en el
///                          próximo <c>SaveChangesAsync</c> del DbContext del scope.
///   - cualquier otro valor (incluido ausencia / <c>"Legacy"</c>): publica vía <c>IOutboxStore.AddAsync</c>
///                          del bus propio (estado pre-Wolverine).
///
/// Cuando Fase 5 borre el bus propio, esta clase se reduce a la única ruta Wolverine sin
/// cambios en los call sites (los publicadores siguen inyectando <c>IIntegrationEventPublisher&lt;T&gt;</c>).
/// </summary>
internal sealed class IntegrationEventPublisher<TDbContext>(
    IDbContextOutbox<TDbContext> wolverineOutbox,
    IOutboxStore legacyOutbox,
    IOptionsMonitor<EventingOptions> options,
    ILogger<IntegrationEventPublisher<TDbContext>> logger)
    : IIntegrationEventPublisher<TDbContext>
    where TDbContext : DbContext
{
    private const string WolverineRoute = "Wolverine";

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var eventName = typeof(TEvent).Name;
        var routing = options.CurrentValue.IntegrationEventRouting;
        var route = routing.TryGetValue(eventName, out var r) ? r : "Legacy";

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "[ADR-0005] {EventName} → {Route} (routing dict has {Count} entries)",
                eventName, route, routing.Count);
        }

        if (string.Equals(route, WolverineRoute, StringComparison.OrdinalIgnoreCase))
        {
            // INV-9 — el evento ya carga TenantId (poblado por el publicador desde el
            // Finbuckle context). Lo propagamos al envelope para que metrics/correlation
            // de Wolverine quede tenant-aware. Fase 3 puede mover esto a un IEnvelopeRule
            // global si la repetición lo justifica.
            var delivery = new DeliveryOptions { TenantId = integrationEvent.TenantId };

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug(
                    "Routing integration event {EventName} via Wolverine (tenant={TenantId})",
                    eventName, integrationEvent.TenantId);
            }

            await wolverineOutbox.PublishAsync(integrationEvent, delivery).ConfigureAwait(false);
            return;
        }

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Routing integration event {EventName} via legacy IOutboxStore", eventName);
        }

        await legacyOutbox.AddAsync(integrationEvent, cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default)
    {
        // Una sola llamada cubre ambas rutas:
        //
        //   - Ruta Wolverine: PublishAsync encoló el envelope en el MessageContext del outbox EF.
        //     SaveChangesAndFlushMessagesAsync committea la tx EF (persistiendo cualquier cambio
        //     de dominio acumulado) y flushea el envelope a wolverine_outgoing_envelopes.
        //
        //   - Ruta Legacy: PublishAsync hizo dbContext.OutboxMessages.Add(...) (entity en estado
        //     Added). El SaveChanges interno de SaveChangesAndFlushMessagesAsync lo persiste
        //     junto con el resto. El flush de Wolverine sobre cero envelopes encolados es no-op.
        //
        // Resultado: el call site (UserRegistrationService, etc.) llama un único método al
        // final del flujo y no necesita conocer la ruta efectiva.
        await wolverineOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
