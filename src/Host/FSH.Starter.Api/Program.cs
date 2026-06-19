using FSH.Framework.Web;
using FSH.Framework.Web.Modules;
using FSH.Modules.Auditing;
using FSH.Modules.Identity;
using FSH.Modules.Identity.Contracts.v1.Tokens.TokenGeneration;
using FSH.Modules.Identity.Features.v1.Tokens.TokenGeneration;
using FSH.Modules.Multitenancy;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantStatus;
using FSH.Modules.Webhooks;
using FSH.Modules.Billing;
using FSH.Modules.Catalog;
using FSH.Modules.Tickets;
using FSH.Modules.Multitenancy.Features.v1.GetTenantStatus;
using System.Reflection;
using FSH.Framework.Eventing.Tenant;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    static void Require(IConfiguration config, string key)
    {
        if (string.IsNullOrWhiteSpace(config[key]))
        {
            throw new InvalidOperationException($"Missing required configuration '{key}' in Production.");
        }
    }

    var config = builder.Configuration;
    Require(config, "DatabaseOptions:ConnectionString");
    Require(config, "CachingOptions:Redis");
    Require(config, "JwtOptions:SigningKey");
}

builder.Services.AddMediator(o =>
{
    o.ServiceLifetime = ServiceLifetime.Scoped;
    o.Assemblies = [
        typeof(GenerateTokenCommand),
        typeof(GenerateTokenCommandHandler),
        typeof(GetTenantStatusQuery),
        typeof(GetTenantStatusQueryHandler),
        typeof(FSH.Modules.Auditing.Contracts.AuditEnvelope),
        typeof(FSH.Modules.Auditing.Persistence.AuditDbContext),
        typeof(FSH.Modules.Webhooks.Contracts.v1.CreateWebhookSubscription.CreateWebhookSubscriptionCommand),
        typeof(FSH.Modules.Webhooks.WebhooksModule),
        typeof(FSH.Modules.Billing.Contracts.BillingContractsMarker),
        typeof(FSH.Modules.Billing.BillingModule),
        typeof(FSH.Modules.Catalog.Contracts.CatalogContractsMarker),
        typeof(FSH.Modules.Catalog.CatalogModule),
        typeof(FSH.Modules.Lookups.Contracts.LookupsContractsMarker),
        typeof(FSH.Modules.Lookups.LookupsModule),
        typeof(FSH.Modules.SharedRecords.Contracts.SharedRecordsContractsMarker),
        typeof(FSH.Modules.SharedRecords.SharedRecordsModule),
        // NamingSeries (ADR-0007): sin commands/queries Mediator en este PR (solo dominio + DbContext +
        // contrato del allocator). Re-añadir cuando lleguen Features/v1 con handlers.
        typeof(FSH.Modules.Parties.Contracts.PartiesContractsMarker),
        typeof(FSH.Modules.Parties.PartiesModule),
        typeof(FSH.Modules.Hr.Contracts.HrContractsMarker),
        typeof(FSH.Modules.Hr.HrModule),
        typeof(FSH.Modules.Tickets.Contracts.TicketsContractsMarker),
        typeof(FSH.Modules.Tickets.TicketsModule),
        typeof(FSH.Modules.Files.Contracts.v1.Commands.RequestUploadUrlCommand),
        typeof(FSH.Modules.Files.FilesModule),
        typeof(FSH.Modules.Chat.Contracts.v1.Commands.CreateChannelCommand),
        typeof(FSH.Modules.Chat.ChatModule),
        typeof(FSH.Modules.Notifications.Contracts.v1.Commands.MarkNotificationReadCommand),
        typeof(FSH.Modules.Notifications.NotificationsModule)];
});

// ─────────────────────────────────────────────────────────────────────────────
// Wolverine v3 (ADR-0001 / ADR-0004 / ADR-0005). Fase 5: el bus de eventos propio
// (RabbitMqEventBus + IOutboxStore + OutboxDispatcher) fue ELIMINADO. Wolverine es
// la ÚNICA vía de integration events (mensajería + outbox transaccional).
//
// Disciplina de convivencia con Mediator (no negociable):
//   - Mediator source-gen sigue siendo el bus IN-PROCESS para comandos/queries.
//   - Wolverine SOLO maneja integration events (mensajería + outbox transaccional).
//   - NUNCA llamar bus.InvokeAsync desde código de comandos — eso es Mediator.
//   - Discovery selectivo: SOLO el assembly de Identity en esta fase para que
//     Wolverine no descubra handlers de otros módulos.
//
// Plan completo: docs/specs/platform/migration-wolverine.md
// ─────────────────────────────────────────────────────────────────────────────
builder.Host.UseWolverine(opts =>
{
    // Connection string + opts de RabbitMQ se leen DENTRO del callback (no fuera)
    // porque WebApplicationFactory inyecta config vía ConfigureAppConfiguration que
    // sólo se aplica durante Build(); capturar el valor afuera dejaría a Wolverine
    // apuntando al Postgres real en lugar del Testcontainer en los tests.
    var identityConnectionString =
        builder.Configuration["DatabaseOptions:ConnectionString"]
        ?? throw new InvalidOperationException("DatabaseOptions:ConnectionString requerido para Wolverine.");


    // Discovery EXPLÍCITA por tipo. La convención (IncludeAssembly) NO descubre estos handlers
    // en este setup: DisableConventionalDiscovery neutraliza el matching por convención (probado
    // con evidencia — "Searching assembly … found no handlers"), así que IncludeAssembly quedaba
    // muerto. Cada handler de integration event se registra a mano acá.
    //
    // 🚩 FOOTGUN (no lo olvides): si agregás un integration event handler nuevo y NO lo registrás
    // con IncludeType acá, Wolverine NO lo descubre → NO se ejecuta. Es un bug SILENCIOSO (no hay
    // error de compilación; el evento se publica y nadie lo consume). SIEMPRE añadí su IncludeType.
    opts.Discovery.DisableConventionalDiscovery();

    // Handlers de dominio (static handler classes — forma canónica Wolverine). Overload
    // IncludeType(Type): el genérico IncludeType<T> no acepta clases static.
    opts.Discovery.IncludeType(typeof(FSH.Modules.Identity.Events.UserRegisteredEmailHandler));
    opts.Discovery.IncludeType(typeof(FSH.Modules.Identity.Events.TokenGeneratedLogHandler));
    opts.Discovery.IncludeType(typeof(FSH.Modules.Notifications.IntegrationEventHandlers.MentionedInChannelIntegrationEventHandler));

    // WebhookFanoutHandler<T> es open-generic; Wolverine no descubre genéricos abiertos por
    // convención. Un closed-generic explícito por cada evento. IMPORTANT: añadir aquí cuando un
    // nuevo evento deba disparar webhooks.
    opts.Discovery.IncludeType<FSH.Modules.Webhooks.Services.WebhookFanoutHandler<FSH.Modules.Identity.Contracts.Events.UserRegisteredIntegrationEvent>>();
    opts.Discovery.IncludeType<FSH.Modules.Webhooks.Services.WebhookFanoutHandler<FSH.Modules.Identity.Contracts.Events.TokenGeneratedIntegrationEvent>>();
    opts.Discovery.IncludeType<FSH.Modules.Webhooks.Services.WebhookFanoutHandler<FSH.Modules.Files.Contracts.Events.FileFinalizedIntegrationEvent>>();
    opts.Discovery.IncludeType<FSH.Modules.Webhooks.Services.WebhookFanoutHandler<FSH.Modules.Chat.Contracts.Events.MentionedInChannelIntegrationEvent>>();

    // Outbox/inbox transaccional respaldado en Postgres, schema "identity"
    // (mismo schema del módulo). Wolverine NO usa migraciones EF: gestiona su
    // propio schema (las 4 tablas wolverine_outgoing_envelopes,
    // wolverine_incoming_envelopes, wolverine_dead_letters, wolverine_nodes).
    //
    // Setup del schema Wolverine — DECISIÓN CERRADA EN FASE 1:
    // El setup vive en DbMigrator (Step 2b, FSH.Starter.DbMigrator/WolverineSchemaSetup.cs),
    // que es el orquestador único de schemas del sistema (EF + Wolverine). El API
    // arranca cuando DbMigrator ya completó (k8s pre-install hook / Aspire dependency).
    // AutoBuildMessageStorageOnStartup queda DESHABILITADO aquí adrede — un solo
    // path de bootstrap, no dos rivales. El harness de tests (FshWebApplicationFactory)
    // reusa el mismo helper post-migraciones, así que prod y test comparten lógica.
    opts.PersistMessagesWithPostgresql(identityConnectionString, "identity");

    // Enrola los DbContexts ya registrados con AddDbContextWithWolverineIntegration<T>
    // (en Fase 1, solo IdentityDbContext via IdentityModule). Cualquier
    // bus.PublishAsync llamado dentro del scope de un IdentityDbContext queda
    // capturado y el envelope se persiste en identity.wolverine_outgoing_envelopes
    // como parte del SaveChangesAsync del propio DbContext — outbox transaccional
    // estructural, cierre de ADR-0001 sobre Identity.
    //
    // La doc de Wolverine 6.8 exige que los DbContexts se registren ANTES de esta
    // llamada en la misma IServiceCollection — IdentityModule lo hace durante
    // AddModules(), que corre antes de builder.Build() que es cuando este callback
    // UseWolverine se materializa.
    //
    // CAPA 3 / C1 — ORDEN CRÍTICO: el middleware de tenant se registra ANTES de
    // UseEntityFrameworkCoreTransactions adrede. El frame EF-tx construye el DbContext del
    // handler al inicio del procesamiento; Finbuckle captura el TenantInfo en construcción.
    // Si el tenant middleware corriera DESPUÉS, el DbContext se construiría con TenantInfo null
    // → MultiTenantException al SaveChanges del handler que escribe (MentionedInChannel).
    // Registrándolo antes, el tenant queda seteado antes de que el EF-tx construya el DbContext.
    opts.UseTenantContextMiddleware();
    opts.UseEntityFrameworkCoreTransactions();

    // Durabilidad sobre TODAS las local queues — sin esto, bus.PublishAsync de un
    // mensaje sin destino remoto rutea a una queue in-memory volátil y el envelope
    // NUNCA toca el outbox EF (aunque el DbContext esté enrolado). Habilitar la
    // durabilidad local es lo que cierra estructuralmente ADR-0001: cualquier
    // PublishAsync dentro del scope de un IdentityDbContext queda persistido en
    // identity.wolverine_outgoing_envelopes como parte del SaveChangesAsync.
    opts.Policies.UseDurableLocalQueues();

    // (UseTenantContextMiddleware se registra arriba, antes de UseEntityFrameworkCoreTransactions
    // — ver CAPA 3 / C1.)

    // CAPA 2 — entrega LOCAL in-process. Los integration events se entregan a sus handlers
    // (descubiertos vía IncludeType arriba) por las local durable queues de Wolverine
    // (respaldadas en Postgres: UseDurableLocalQueues + PersistMessagesWithPostgresql). NO hay
    // routing a RabbitMQ: en un monolito modular el publicador y los handlers viven en el MISMO
    // proceso; el round-trip por broker era latencia + dependencia de broker + flakiness inútiles
    // (ningún consumidor externo consume estos exchanges — verificado por grep). Dev ya no
    // necesita RabbitMQ para el eventing.
    //
    // 🔭 Si en el futuro un evento DEBE salir del proceso hacia un sistema EXTERNO real,
    // reintroducir opts.UseRabbitMq(...) + opts.PublishMessage<TEvent>().ToRabbitExchange(...)
    // SOLO para ese evento — reservar el broker para salida externa genuina.
});

var moduleAssemblies = new Assembly[]
{
    typeof(IdentityModule).Assembly,
    typeof(MultitenancyModule).Assembly,
    typeof(AuditingModule).Assembly,
    typeof(FSH.Modules.Files.FilesModule).Assembly,
    typeof(WebhooksModule).Assembly,
    typeof(BillingModule).Assembly,
    typeof(CatalogModule).Assembly,
    typeof(FSH.Modules.Lookups.LookupsModule).Assembly,
    typeof(FSH.Modules.SharedRecords.SharedRecordsModule).Assembly,
    typeof(FSH.Modules.NamingSeries.NamingSeriesModule).Assembly,
    typeof(FSH.Modules.Parties.PartiesModule).Assembly,
    typeof(FSH.Modules.Hr.HrModule).Assembly,
    typeof(TicketsModule).Assembly,
    typeof(FSH.Modules.Chat.ChatModule).Assembly,
    typeof(FSH.Modules.Notifications.NotificationsModule).Assembly,
};

builder.AddHeroPlatform(o =>
{
    o.EnableCaching = true;
    o.EnableMailing = true;
    o.EnableJobs = true;
    o.EnableQuotas = true;
    o.EnableSse = true;
    o.EnableRealtime = true;
});

builder.AddModules(moduleAssemblies);

// Demo data (acme, globex, demo users, sample catalog/tickets/chat) is provisioned
// by the DbMigrator's `seed-demo` verb — not by the API. The API never mutates data
// on startup. See src/Host/FSH.Starter.DbMigrator/README.md.

var app = builder.Build();

app.UseHeroMultiTenantDatabases();
app.UseHeroPlatform(p =>
{
    p.MapModules = true;
    p.ServeStaticFiles = true;
    p.UseQuotas = true;
    p.MapSseEndpoints = true;
    p.MapRealtime = true;
});

app.MapGet("/", () => Results.Ok(new { message = "hello world!" }))
   .WithTags("PlayGround")
   .AllowAnonymous();
await app.RunAsync();