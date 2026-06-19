using FSH.Framework.Eventing.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Eventing;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registra el <see cref="IIntegrationEventPublisher{TDbContext}"/> que publica cada evento
    /// vía el outbox transaccional de Wolverine sobre <typeparamref name="TDbContext"/>. Cada
    /// módulo lo registra con su DbContext. Requiere que el DbContext esté registrado previamente
    /// con <c>services.AddDbContextWithWolverineIntegration&lt;TDbContext&gt;(...)</c> para que
    /// <c>IDbContextOutbox&lt;TDbContext&gt;</c> resuelva.
    /// </summary>
    public static IServiceCollection AddIntegrationEventPublisher<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<
            IIntegrationEventPublisher<TDbContext>,
            IntegrationEventPublisher<TDbContext>>();

        return services;
    }
}
