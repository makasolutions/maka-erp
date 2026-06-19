namespace FSH.Framework.Eventing.Abstractions;

/// <summary>
/// Fachada de publicación de integration events. Tras la Fase 5 (eliminación del bus propio)
/// la implementación rutea SIEMPRE por el outbox transaccional de Wolverine sobre
/// <typeparamref name="TDbContext"/> (envelope persistido en el flush del mismo DbContext).
/// El publicador del módulo NO conoce el transporte subyacente.
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
    /// Encola el evento en el outbox EF de Wolverine. El envelope queda en el
    /// <c>MessageContext</c> (memoria del proceso); NO se persiste a la tabla
    /// <c>wolverine_outgoing_envelopes</c> hasta que se invoque
    /// <see cref="SaveChangesAndFlushAsync"/>.
    ///
    /// El call site debe cerrar el flujo con <see cref="SaveChangesAndFlushAsync"/> para
    /// garantizar persistencia. Llamar solo a <c>PublishAsync</c> seguido de
    /// <c>dbContext.SaveChangesAsync</c> NO persiste el envelope (causa raíz original de la
    /// "5ª capa" — ver wolverine-phase1-followups.md).
    /// </summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;

    /// <summary>
    /// Persiste los cambios pendientes del DbContext del scope Y flushea los eventos encolados
    /// por <see cref="PublishAsync"/> a su almacenamiento durable. Invoca
    /// <c>dbContextOutbox.SaveChangesAndFlushMessagesAsync</c> de Wolverine, que (a) committea la
    /// tx EF persistiendo cualquier cambio del DbContext y (b) flushea los envelopes Wolverine
    /// encolados a <c>wolverine_outgoing_envelopes</c>. Retorna <c>Task</c> sin recuento de filas.
    /// </summary>
    Task SaveChangesAndFlushAsync(CancellationToken cancellationToken = default);
}
