using FSH.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.EntityFrameworkCore;

namespace FSH.Framework.Eventing;

/// <summary>
/// Publicación de integration events vía el outbox transaccional de Wolverine sobre
/// <typeparamref name="TDbContext"/>. Tras la Fase 5 (eliminación del bus de eventos legacy)
/// ésta es la única ruta: ya no hay switch por evento ni <c>IntegrationEventRouting</c>.
///
/// El publicador del módulo inyecta <c>IIntegrationEventPublisher&lt;TDbContext&gt;</c> y no
/// conoce el transporte subyacente. Cada módulo lo registra con su DbContext vía
/// <c>services.AddIntegrationEventPublisher&lt;TDbContext&gt;()</c>; requiere que el DbContext
/// esté enrolado con <c>AddDbContextWithWolverineIntegration&lt;TDbContext&gt;()</c> para que
/// <c>IDbContextOutbox&lt;TDbContext&gt;</c> resuelva.
/// </summary>
internal sealed class IntegrationEventPublisher<TDbContext>(
    IDbContextOutbox<TDbContext> wolverineOutbox,
    ILogger<IntegrationEventPublisher<TDbContext>> logger)
    : IIntegrationEventPublisher<TDbContext>
    where TDbContext : DbContext
{
    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // INV-9 — el evento ya carga TenantId (poblado por el publicador desde el Finbuckle
        // context). Lo propagamos al envelope para que metrics/correlation de Wolverine quede
        // tenant-aware. El envelope queda enrolado en el outbox EF; se persiste en el próximo
        // SaveChangesAndFlushAsync del DbContext del scope.
        var delivery = new DeliveryOptions { TenantId = integrationEvent.TenantId };

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "Routing integration event {EventName} via Wolverine (tenant={TenantId})",
                typeof(TEvent).Name, integrationEvent.TenantId);
        }

        await wolverineOutbox.PublishAsync(integrationEvent, delivery).ConfigureAwait(false);
    }

    public async Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default)
    {
        // Committea la tx EF (persistiendo cualquier cambio de dominio acumulado en el DbContext)
        // y flushea los envelopes encolados a wolverine_outgoing_envelopes. El call site llama un
        // único método al final del flujo y no necesita conocer el transporte.
        await wolverineOutbox.SaveChangesAndFlushMessagesAsync(cancellationToken).ConfigureAwait(false);
    }
}
