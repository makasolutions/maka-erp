using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.NamingSeries.Data;

/// <summary>
/// Design-time factory para que <c>dotnet ef migrations add</c> pueda construir
/// el <see cref="NamingSeriesDbContext"/> sin pasar por el grafo DI del API
/// (que requiere servicios scoped no disponibles fuera de un scope HTTP).
/// </summary>
public sealed class NamingSeriesDbContextFactory : IDesignTimeDbContextFactory<NamingSeriesDbContext>
{
    public NamingSeriesDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["DatabaseOptions:ConnectionString"]
            ?? "Host=localhost;Port=5432;Database=maka_erp_dev;Username=maka_user;Password=maka_dev_2026";
        var migrationsAssembly = configuration["DatabaseOptions:MigrationsAssembly"]
            ?? "FSH.Starter.Migrations.PostgreSQL";

        var optionsBuilder = new DbContextOptionsBuilder<NamingSeriesDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly(migrationsAssembly));

        var dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = "POSTGRESQL",
            ConnectionString = connectionString,
            MigrationsAssembly = migrationsAssembly,
        });

        return new NamingSeriesDbContext(
            new DesignTimeTenantAccessor(),
            optionsBuilder.Options,
            dbOptions,
            new DesignTimeHostEnvironment());
    }

    private sealed class DesignTimeTenantAccessor : IMultiTenantContextAccessor<AppTenantInfo>
    {
        public IMultiTenantContext<AppTenantInfo> MultiTenantContext { get; set; } = new MultiTenantContext<AppTenantInfo>(new AppTenantInfo());
        IMultiTenantContext IMultiTenantContextAccessor.MultiTenantContext => MultiTenantContext;
    }

    private sealed class DesignTimeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "FSH.Starter.Api";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
