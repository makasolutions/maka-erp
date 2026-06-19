extern alias api;
extern alias migrator;
using System.Reflection;
using Amazon.S3;
using Amazon.S3.Model;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Mailing;
using FSH.Framework.Mailing.Services;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using FSH.Modules.Multitenancy.Data;
using FSH.Framework.Web.Modules;
using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;

namespace Integration.Tests.Infrastructure;

public sealed class FshWebApplicationFactory : WebApplicationFactory<api::Program>, IAsyncLifetime
{
    private const string MinioAccessKey = "minioadmin";
    private const string MinioSecretKey = "minioadmin";
    private const string MinioBucket = "fsh-integration-test-uploads";

    /// <summary>
    /// Snapshot de los <see cref="ServiceDescriptor"/> del host compartido (capturado en
    /// ConfigureServices, tras todas las registraciones de módulos). Permite a un guard test
    /// inspeccionar lifetimes (p.ej. que ningún ISaveChangesInterceptor sea Scoped) SIN construir
    /// un 2.º host — construir/disponer un 2.º WebApplicationFactory pisaría y dispondría el estático
    /// global JobStorage.Current de Hangfire, rompiendo cada test posterior que cree tenants.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> CapturedServices { get; private set; } = [];

    private static readonly SemaphoreSlim _migrationLock = new(1, 1);
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fsh_integration_tests")
        .WithUsername("postgres")
        .WithPassword("integration_test_pwd")
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder("minio/minio:latest")
        .WithUsername(MinioAccessKey)
        .WithPassword(MinioSecretKey)
        .WithAutoRemove(true)
        .WithCleanUp(true)
        .Build();

    // CAPA 2 — entrega local in-process: Wolverine ya NO usa RabbitMQ, así que el harness no
    // levanta broker. La entrega de integration events se valida por las local durable queues.

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());
        await CreateMinioBucketAsync();

        // Force host creation via the Server property (no leaked HttpClient)
        _ = Server;

        // Run migrations and seed data for the root tenant.
        // We use a semaphore to prevent multiple test classes (which might share the same DB)
        // from attempting to migrate simultaneously.
        await _migrationLock.WaitAsync();
        try
        {
            await ProvisionRootTenantAsync();
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _minio.DisposeAsync();
    }

    /// <summary>The MinIO endpoint URL exposed to the host configuration; useful for tests that need to PUT bytes directly.</summary>
    public string MinioServiceUrl => _minio.GetConnectionString();

    private async Task CreateMinioBucketAsync()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = _minio.GetConnectionString(),
            ForcePathStyle = true,
            UseHttp = true,
            AuthenticationRegion = "us-east-1"
        };

        using var client = new AmazonS3Client(
            new Amazon.Runtime.BasicAWSCredentials(MinioAccessKey, MinioSecretKey),
            config);

        try
        {
            await client.PutBucketAsync(new PutBucketRequest { BucketName = MinioBucket });
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "BucketAlreadyOwnedByYou" || ex.ErrorCode == "BucketAlreadyExists")
        {
            // Idempotent across factory re-creations.
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DatabaseOptions:Provider"] = "POSTGRESQL",
                ["DatabaseOptions:ConnectionString"] = _postgres.GetConnectionString(),
                ["DatabaseOptions:MigrationsAssembly"] = "FSH.Starter.Migrations.PostgreSQL",
                ["CachingOptions:Redis"] = "",
                ["JwtOptions:Issuer"] = TestConstants.JwtIssuer,
                ["JwtOptions:Audience"] = TestConstants.JwtAudience,
                ["JwtOptions:SigningKey"] = TestConstants.JwtSigningKey,
                ["JwtOptions:AccessTokenMinutes"] = "30",
                ["JwtOptions:RefreshTokenDays"] = "7",
                ["OriginOptions:OriginUrl"] = "http://localhost",
                ["OpenTelemetryOptions:Enabled"] = "false",
                // CAPA 2 — entrega local in-process; sin RabbitMQ. Provider queda como dato muerto
                // (Wolverine ya no lo lee tras quitar UseRabbitMq); InMemory documenta "sin broker".
                ["EventingOptions:Provider"] = "InMemory",
                ["Serilog:MinimumLevel:Default"] = "Warning",
                ["Serilog:MinimumLevel:Override:Microsoft.EntityFrameworkCore"] = "Fatal",
                ["Serilog:MinimumLevel:Override:Npgsql"] = "Fatal",
                ["Serilog:MinimumLevel:Override:FSH.Framework.Eventing"] = "Information",
                ["Serilog:WriteTo:0:Name"] = "Console",
                ["Serilog:WriteTo:0:Args:restrictedToMinimumLevel"] = "Warning",
                ["Serilog:WriteTo:1:Name"] = "",
                ["MailOptions:UseSendGrid"] = "false",
                ["HangfireOptions:Username"] = "admin",
                ["HangfireOptions:Password"] = "integration-test-hangfire-pwd",
                ["HangfireOptions:Route"] = "/jobs",
                ["RateLimitingOptions:Enabled"] = "false",
                ["PasswordPolicy:EnforcePasswordExpiry"] = "false",
                ["Seed:DemoPassword"] = "Password123!",
                ["Seed:DefaultAdminPassword"] = TestConstants.DefaultPassword,
                ["SecurityHeadersOptions:Enabled"] = "false",
                ["Storage:Provider"] = "s3",
                ["Storage:S3:Bucket"] = MinioBucket,
                ["Storage:S3:ServiceUrl"] = _minio.GetConnectionString(),
                ["Storage:S3:AccessKey"] = MinioAccessKey,
                ["Storage:S3:SecretKey"] = MinioSecretKey,
                ["Storage:S3:ForcePathStyle"] = "true",
                ["Storage:S3:PublicRead"] = "false",
                ["Storage:S3:Region"] = "us-east-1",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Smoke test ADR-0001 — extender el discovery de Wolverine para incluir el
            // handler test-only Phase1SmokeMessageHandler. API canónica de Wolverine 6.8
            // para overrides de test: ConfigureWolverine es additive (se compone con el
            // UseWolverine del Program.cs sin reemplazarlo). IncludeType<T> agrega UN solo
            // tipo handler, sin escanear el assembly entero — cero contaminación de otros tests.
            services.ConfigureWolverine(opts =>
            {
                opts.Discovery.IncludeType(typeof(Integration.Tests.Tests.Platform.Phase1SmokeMessageHandler));
                // Consumers test-only de los publicadores reales. CAPA 2: entrega LOCAL in-process
                // — el evento publicado se rutea por la local durable queue a TODOS los handlers
                // descubiertos para ese tipo (el handler real + estos consumers test-only). Ya no
                // hace falta listener RabbitMQ: Wolverine los invoca localmente.
                opts.Discovery.IncludeType(typeof(Integration.Tests.Tests.Platform.UserRegisteredE2EConsumer));
                opts.Discovery.IncludeType(typeof(Integration.Tests.Tests.Platform.TokenGeneratedE2EConsumer));
                opts.Discovery.IncludeType(typeof(Integration.Tests.Tests.Platform.FileFinalizedE2EConsumer));
            });

            // Singletons sink donde los consumers test-only graban para asertar.
            services.AddSingleton<Integration.Tests.Tests.Platform.UserRegisteredCollector>();
            services.AddSingleton<Integration.Tests.Tests.Platform.TokenGeneratedCollector>();
            services.AddSingleton<Integration.Tests.Tests.Platform.FileFinalizedCollector>();

            // Remove hosted services that depend on infrastructure not available in tests or cause race conditions:
            // - RolePermissionSyncHostedService (queries identity schema before migrations run)
            // - Hangfire server + stale lock cleanup (we register our own InMemory server below)
            var hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType?.Name == "RolePermissionSyncHostedService" ||
                     d.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.Ordinal) == true ||
                     d.ImplementationType?.Name == "HangfireStaleLockCleanupService"))
                .ToList();
            foreach (var service in hostedServicesToRemove)
            {
                services.Remove(service);
            }

            services.AddHangfire(config => config.UseInMemoryStorage());
            services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(1);
                options.Queues = ["default", "email"];
                options.WorkerCount = 2;
            });
            services.TryAddTransient<IJobService, HangfireService>();

            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.RequireHttpsMetadata = false);

            // Replace real mail service with a no-op to avoid SMTP errors and Hangfire retries
            services.RemoveAll<IMailService>();
            services.AddSingleton<IMailService, NoOpMailService>();

            // Detailed errors in tests instead of generic "An unexpected error occurred"
            var existingHandlers = services.Where(d =>
                d.ServiceType == typeof(Microsoft.AspNetCore.Diagnostics.IExceptionHandler)).ToList();
            foreach (var h in existingHandlers) services.Remove(h);
            services.AddExceptionHandler<DetailedTestExceptionHandler>();

            // AddHeroStorage reads `Storage:Provider` eagerly at registration time, before the test
            // factory's in-memory configuration overlay is applied — so the production registration
            // wires up LocalStorageService. Replace it with the S3 stack pointed at the MinIO
            // testcontainer here, after all module registrations have run.
            RewireStorageForS3(services);

            // Snapshot final del collection (solo metadatos, sin provider) para el guard test de
            // lifetimes — evita que el guard construya un 2.º host (ver doc de CapturedServices).
            CapturedServices = services.ToList();
        });
    }

    private void RewireStorageForS3(IServiceCollection services)
    {
        var toRemove = services
            .Where(d => d.ServiceType == typeof(FSH.Framework.Storage.Services.IStorageService)
                     || d.ServiceType == typeof(FSH.Framework.Storage.Local.LocalStorageService)
                     || d.ServiceType == typeof(FSH.Framework.Storage.S3.S3StorageService)
                     || d.ServiceType == typeof(IAmazonS3))
            .ToList();
        foreach (var d in toRemove) services.Remove(d);

        services.Configure<FSH.Framework.Storage.S3.S3StorageOptions>(opts =>
        {
            opts.Bucket = MinioBucket;
            opts.ServiceUrl = _minio.GetConnectionString();
            opts.AccessKey = MinioAccessKey;
            opts.SecretKey = MinioSecretKey;
            opts.ForcePathStyle = true;
            opts.PublicRead = false;
            opts.Region = "us-east-1";
        });

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = _minio.GetConnectionString(),
                ForcePathStyle = true,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            };
            return new AmazonS3Client(
                new Amazon.Runtime.BasicAWSCredentials(MinioAccessKey, MinioSecretKey),
                config);
        });
        services.AddTransient<FSH.Framework.Storage.S3.S3StorageService>();
        services.AddTransient<FSH.Framework.Storage.Services.IStorageService>(sp =>
            sp.GetRequiredService<FSH.Framework.Storage.S3.S3StorageService>());
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Disable ValidateOnBuild for .NET 10 minimal API dual-host model
        builder.UseServiceProviderFactory(new DefaultServiceProviderFactory(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = false
        }));

        ResetModuleLoader();
        return base.CreateHost(builder);
    }

    private async Task ProvisionRootTenantAsync()
    {
        // 1. Explicitly migrate the tenant catalog FIRST.
        using (var scope = Services.CreateScope())
        {
            var tenantDbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
            await tenantDbContext.Database.MigrateAsync();

            // 2. Seed Root Tenant if missing (ensures we don't wait for background service)
            var rootTenant = await tenantDbContext.TenantInfo.FindAsync(MultitenancyConstants.Root.Id);
            if (rootTenant is null)
            {
                rootTenant = new AppTenantInfo(
                    MultitenancyConstants.Root.Id,
                    MultitenancyConstants.Root.Name,
                    string.Empty,
                    MultitenancyConstants.Root.EmailAddress,
                    issuer: MultitenancyConstants.Root.Issuer);

                var validUpto = DateTime.UtcNow.AddYears(1);
                rootTenant.SetValidity(validUpto);
                await tenantDbContext.TenantInfo.AddAsync(rootTenant);
                await tenantDbContext.SaveChangesAsync();
            }

            // 3. Run all module migrations (identity, audit, webhook schemas)
            var setter = scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>();
            setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(rootTenant);

            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.MigrateAsync(CancellationToken.None);
            }

            // 4. Seed all modules (admin user, roles, permissions, groups)
            foreach (var init in scope.ServiceProvider.GetServices<IDbInitializer>())
            {
                await init.SeedAsync(CancellationToken.None);
            }

            // 5. Run the role-permission syncer through the production code path.
            var syncer = scope.ServiceProvider.GetRequiredService<FSH.Modules.Identity.Authorization.RolePermissionSyncer>();
            await syncer.SyncAsync(CancellationToken.None);
        }

        // 6. Wolverine schema setup (Fase 1, ADR-0001/0004) — reusamos el MISMO
        // helper que DbMigrator usa en producción (Step 2b). Cero duplicación
        // de lógica entre prod y test. La connection string del Testcontainer
        // se pasa por parámetro: el helper es agnóstico al origen de la cadena.
        // Lista de schemas alineada con DbMigrator/Program.cs.
        var setupLogger = Services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(FshWebApplicationFactory));
        var cs = _postgres.GetConnectionString();
        foreach (var schema in new[] { "identity", "files", "chat", "notifications", "webhooks" })
        {
            await migrator::FSH.Starter.DbMigrator.WolverineSchemaSetup.ApplyAsync(cs, schema, setupLogger, CancellationToken.None);
        }
    }

    private static void ResetModuleLoader()
    {
        var type = typeof(ModuleLoader);
        var modulesField = type.GetField("_modules", BindingFlags.Static | BindingFlags.NonPublic);
        var loadedField = type.GetField("_modulesLoaded", BindingFlags.Static | BindingFlags.NonPublic);

        if (modulesField?.GetValue(null) is System.Collections.IList list)
        {
            list.Clear();
        }

        loadedField?.SetValue(null, false);
    }
}
