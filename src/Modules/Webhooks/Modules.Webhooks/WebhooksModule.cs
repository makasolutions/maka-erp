using Asp.Versioning;
using FSH.Framework.Eventing;
using FSH.Framework.Persistence;
using Wolverine.EntityFrameworkCore;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Web.HttpResilience;
using FSH.Framework.Web.Modules;
using FSH.Modules.Webhooks.Contracts.Authorization;
using FSH.Modules.Webhooks.Data;
using FSH.Modules.Webhooks.Features.v1.CreateWebhookSubscription;
using FSH.Modules.Webhooks.Features.v1.DeleteWebhookSubscription;
using FSH.Modules.Webhooks.Features.v1.GetWebhookDeliveries;
using FSH.Modules.Webhooks.Features.v1.GetWebhookSubscriptions;
using FSH.Modules.Webhooks.Features.v1.TestWebhookSubscription;
using FSH.Framework.Eventing.Abstractions;
using FSH.Modules.Webhooks.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

[assembly: FshModule(typeof(FSH.Modules.Webhooks.WebhooksModule), 400)]

namespace FSH.Modules.Webhooks;

public sealed class WebhooksModule : IModule
{
    public void ConfigureServices(IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        PermissionConstants.Register(WebhooksPermissions.All);
        // ADR-0001/0005 · Fase 3 — Wolverine outbox transaccional sobre WebhookDbContext.
        builder.Services.AddDbContextWithWolverineIntegration<WebhookDbContext>((sp, options) =>
        {
            var env = sp.GetRequiredService<Microsoft.Extensions.Hosting.IHostEnvironment>();
            var dbConfig = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FSH.Framework.Shared.Persistence.DatabaseOptions>>().Value;
            options.ConfigureHeroDatabase(dbConfig.Provider, dbConfig.ConnectionString, dbConfig.MigrationsAssembly, env.IsDevelopment());
            options.AddInterceptors(sp.GetServices<Microsoft.EntityFrameworkCore.Diagnostics.ISaveChangesInterceptor>());
        }, wolverineDatabaseSchema: "webhooks");
        builder.Services.AddIntegrationEventPublisher<WebhookDbContext>();
        builder.Services.AddScoped<IDbInitializer, WebhookDbInitializer>();
        builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
        builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        builder.Services.AddScoped<WebhookDispatchJob>();

        // Open-generic integration-event bridge — every IIntegrationEvent the bus
        // publishes is fanned out to matching tenant webhook subscriptions. Closed
        // handler types are materialized per event type by DI.
        builder.Services.AddScoped(
            typeof(IIntegrationEventHandler<>),
            typeof(WebhookFanoutHandler<>));

        builder.Services.AddHttpClient("Webhooks")
            .AddHeroResilience(builder.Configuration);

        builder.Services.AddHealthChecks()
            .AddDbContextCheck<WebhookDbContext>(
                name: "db:webhooks",
                failureStatus: HealthStatus.Unhealthy);
    }

    public void ConfigureMiddleware(Microsoft.AspNetCore.Builder.IApplicationBuilder app)
    {
        // No custom middleware needed
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var versionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .ReportApiVersions()
            .Build();

        // NOTE (§18.4 #14): do NOT call .RequireAuthorization() here. Doing so attaches an
        // "authenticated-only" policy that DISABLES the global permission FallbackPolicy, so
        // the per-endpoint .RequirePermission() metadata is ignored and authorization fails
        // OPEN to any authenticated user. Other modules (Catalog, etc.) omit it and rely on
        // the fallback policy that evaluates the RequiredPermission metadata.
        var group = endpoints
            .MapGroup("api/v{version:apiVersion}/webhooks")
            .WithTags("Webhooks")
            .WithApiVersionSet(versionSet);

        group.MapCreateWebhookSubscriptionEndpoint();
        group.MapDeleteWebhookSubscriptionEndpoint();
        group.MapGetWebhookSubscriptionsEndpoint();
        group.MapGetWebhookDeliveriesEndpoint();
        group.MapTestWebhookSubscriptionEndpoint();
    }
}
