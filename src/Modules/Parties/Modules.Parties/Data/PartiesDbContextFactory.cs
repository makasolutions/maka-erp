using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Parties.Data;

/// <summary>
/// Design-time factory para <c>dotnet ef</c>. Aunque Parties NO usa la integración Wolverine,
/// necesita esta factory: tras añadir la integración Wolverine EF (Fases 1-3), la enumeración
/// del host-provider que hace EF en design-time intenta construir TODOS los DbContexts y los
/// contextos Wolverine lanzan 'Cannot resolve scoped ISaveChangesInterceptor from root provider'.
/// Esa excepción aborta cualquier operación <c>dotnet ef</c> sobre CUALQUIER contexto que no tenga
/// su propia factory (que cortocircuita la enumeración). Ver ADR-0007. Patrón idéntico a
/// <c>TenantDbContextFactory</c> y a las 5 factories de los módulos Wolverine.
/// </summary>
public sealed class PartiesDbContextFactory : IDesignTimeDbContextFactory<PartiesDbContext>
{
    public PartiesDbContext CreateDbContext(string[] args)
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

        var optionsBuilder = new DbContextOptionsBuilder<PartiesDbContext>();
        optionsBuilder.UseNpgsql(connectionString, b => b.MigrationsAssembly(migrationsAssembly));

        var dbOptions = Options.Create(new DatabaseOptions
        {
            Provider = "POSTGRESQL",
            ConnectionString = connectionString,
            MigrationsAssembly = migrationsAssembly,
        });

        return new PartiesDbContext(
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
