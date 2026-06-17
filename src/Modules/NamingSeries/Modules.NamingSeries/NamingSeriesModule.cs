using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.Modules;
using FSH.Modules.NamingSeries.Contracts;
using FSH.Modules.NamingSeries.Contracts.Authorization;
using FSH.Modules.NamingSeries.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Wolverine.EntityFrameworkCore;

[assembly: FshModule(typeof(FSH.Modules.NamingSeries.NamingSeriesModule), 530)]

namespace FSH.Modules.NamingSeries;

public sealed class NamingSeriesModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(NamingSeriesPermissions.All);

        // ADR-0001/0004/0007 — el DbContext se enrola con Wolverine para que la publicación
        // de NamingSeriesThresholdReachedIntegrationEvent vaya por el outbox transaccional
        // del módulo. La operación de Allocate (SELECT FOR UPDATE) usa la conexión del caller
        // (módulo cliente), NO este DbContext — pero la publicación del evento de umbral sí
        // pasa por este DbContext en escenarios de admin CRUD que toquen la serie.
        // ADR-0001/0004/0007 — outbox transaccional Wolverine sobre NamingSeriesDbContext.
        builder.Services.AddDbContextWithWolverineIntegration<NamingSeriesDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var dbConfig = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FSH.Framework.Shared.Persistence.DatabaseOptions>>().Value;
            options.ConfigureHeroDatabase(dbConfig.Provider, dbConfig.ConnectionString, dbConfig.MigrationsAssembly, env.IsDevelopment());
            options.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>());
        }, wolverineDatabaseSchema: NamingSeriesDbContext.Schema);
        builder.Services.AddEventingForDbContext<NamingSeriesDbContext>();
        builder.Services.AddIntegrationEventPublisher<NamingSeriesDbContext>();

        builder.Services.AddScoped<IDbInitializer, NamingSeriesDbInitializer>();

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<NamingSeriesDbContext>(name: "db:naming-series", failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(IApplicationBuilder app) { }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        // Endpoints CRUD se mapean en una iteración separada (Features/v1).
        // Para este PR el módulo expone solo el DbContext, el dominio y el contrato del allocator.
        ArgumentNullException.ThrowIfNull(endpoints);
    }
}
