namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// ADR-0005 — fachada de publicación de integration events durante la migración a Wolverine.
/// La implementación consulta <c>EventingOptions.IntegrationEventRouting</c> y rutea cada
/// evento por:
///   - <c>"Wolverine"</c> → outbox transaccional de Wolverine sobre <typeparamref name="TDbContext"/>
///                          (envelope persistido en <c>SaveChangesAsync</c> del mismo DbContext).
///   - <c>"Legacy"</c>    → <c>IOutboxStore</c> del bus propio (estado actual).
///
/// El publicador del módulo NO conoce el bus subyacente; cuando Fase 5 borre el bus propio,
/// la implementación se simplifica a una única ruta sin tocar los call sites.
///
/// Tipo genérico por <typeparamref name="TDbContext"/> porque la API canónica de Wolverine
/// (<c>IDbContextOutbox&lt;TDbContext&gt;</c>) es genérica por DbContext: cada módulo registra su
/// publisher tipado vía <c>services.AddIntegrationEventPublisher&lt;TDbContext&gt;()</c>.
/// </summary>
// S2326 suprimido a propósito: TDbContext es un marker genérico para que el contenedor DI
// resuelva instancias distintas por DbContext de módulo, no un parámetro usado en miembros.
#pragma warning disable S2326
public interface IIntegrationEventPublisher<TDbContext>
#pragma warning restore S2326
    where TDbContext : class
{
    /// <summary>
    /// Encola el evento para publicación por la ruta configurada para su tipo en
    /// <c>EventingOptions.IntegrationEventRouting</c>.
    ///
    /// IMPORTANTE — semántica de "encolar" según la ruta:
    ///   - <c>"Wolverine"</c>: el envelope queda en el <c>MessageContext</c> del outbox EF
    ///     (memoria del proceso). NO se persiste a la tabla <c>wolverine_outgoing_envelopes</c>
    ///     hasta que se invoque <see cref="SaveChangesAndFlushAsync"/> (o el equivalente
    ///     de Wolverine <c>SaveChangesAndFlushMessagesAsync</c>).
    ///   - <c>"Legacy"</c>: el envelope se añade al <c>IOutboxStore</c> del bus propio
    ///     (EF entity <c>OutboxMessages</c> en estado <c>Added</c>). Se persiste cuando
    ///     <see cref="SaveChangesAndFlushAsync"/> (o un <c>SaveChangesAsync</c> propio)
    ///     se invoque sobre el DbContext del scope.
    ///
    /// En ambos casos, el call site debe cerrar el flujo con <see cref="SaveChangesAndFlushAsync"/>
    /// para garantizar persistencia. Llamar solo a <c>PublishAsync</c> seguido de
    /// <c>dbContext.SaveChangesAsync</c> NO persiste el envelope en la ruta Wolverine
    /// (causa raíz original de la "5ª capa" — ver wolverine-phase1-followups.md).
    /// </summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Persiste los cambios pendientes del DbContext del scope Y flushea los eventos encolados
    /// por <see cref="PublishAsync"/> a su almacenamiento durable.
    ///
    /// Implementación uniforme cross-ruta: invoca <c>dbContextOutbox.SaveChangesAndFlushMessagesAsync</c>
    /// de Wolverine, que (a) committea la tx EF persistiendo cualquier cambio del DbContext
    /// (incluido el <c>OutboxMessages</c> entity que la ruta Legacy añade), y (b) flushea los
    /// envelopes Wolverine encolados a <c>wolverine_outgoing_envelopes</c>. Sobre cero envelopes
    /// Wolverine encolados el flush es no-op, así que la semántica del bus propio (Legacy) queda
    /// preservada sin ramificación.
    ///
    /// La API subyacente de Wolverine (<c>IDbContextOutbox.SaveChangesAndFlushMessagesAsync</c>)
    /// retorna <c>Task</c> sin recuento de filas; mantenemos esa firma aquí.
    /// </summary>
    Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default);
}
