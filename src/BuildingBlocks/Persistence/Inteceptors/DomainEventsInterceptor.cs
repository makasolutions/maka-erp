using FSH.Framework.Core.Domain;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Framework.Persistence.Inteceptors;

/// <summary>
/// Entity Framework interceptor that automatically publishes domain events after saving changes.
///
/// SINGLETON (scope-safe): NO captura <c>IPublisher</c> (scoped) en el ctor — lo resuelve LAZY desde
/// el scope ambiente en SaveChanges. Esto permite registrarlo singleton (root-resolvable), necesario
/// porque los DbContext con Wolverine tienen options singleton y EF resuelve los interceptores desde
/// el root provider. En HTTP usa el publisher del MISMO scope del request (tenant/user intactos);
/// en background (jobs/seeders, sin HttpContext) usa un scope fresco — el tenant fluye por AsyncLocal
/// (Finbuckle) y no hay current user en background (equivalente al scope del job).
/// </summary>
public sealed class DomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DomainEventsInterceptor> _logger;

    public DomainEventsInterceptor(
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory,
        ILogger<DomainEventsInterceptor> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Called before changes are saved to the database.
    /// </summary>
    /// <param name="eventData">Contextual information about the DbContext being saved.</param>
    /// <param name="result">The result to be returned from SaveChanges.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Called after changes have been saved to the database. Publishes all domain events from tracked entities.
    /// </summary>
    /// <param name="eventData">Contextual information about the completed save operation.</param>
    /// <param name="result">The number of state entries written to the database.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    /// <exception cref="ArgumentNullException">Thrown when eventData is null.</exception>
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context == null)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var domainEvents = context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .SelectMany(e =>
            {
                var pending = e.Entity.DomainEvents.ToArray();
                e.Entity.ClearDomainEvents();
                return pending;
            })
            .ToArray();

        if (domainEvents.Length == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Publishing {Count} domain events...", domainEvents.Length);
        }

        // Resolución LAZY del IPublisher desde el scope ambiente: el del request en HTTP (idéntico a
        // hoy), o uno fresco en background (jobs/seeders sin HttpContext). El scope fresco se dispone
        // al final, tras despachar todos los eventos.
        IServiceScope? backgroundScope = null;
        try
        {
            var requestServices = _httpContextAccessor.HttpContext?.RequestServices;
            IPublisher publisher;
            if (requestServices is not null)
            {
                publisher = requestServices.GetRequiredService<IPublisher>();
            }
            else
            {
                backgroundScope = _scopeFactory.CreateScope();
                publisher = backgroundScope.ServiceProvider.GetRequiredService<IPublisher>();
            }

            foreach (var domainEvent in domainEvents)
            {
                try
                {
                    await publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Domain event handler failures must not roll back or fail the already-committed save.
                    // The event was collected after SaveChanges completed — the data is persisted.
                    // Handlers that need guaranteed delivery should use the outbox pattern.
                    _logger.LogError(ex, "Failed to publish domain event {EventType}", domainEvent.GetType().Name);
                }
            }
        }
        finally
        {
            backgroundScope?.Dispose();
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}