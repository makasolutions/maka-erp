using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Lookups.Data;

public sealed class LookupsDbContext : BaseDbContext
{
    public const string Schema = "lookups";

    public LookupsDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<LookupsDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<BasicTable>  BasicTables  => Set<BasicTable>();
    public DbSet<BasicRecord> BasicRecords => Set<BasicRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LookupsDbContext).Assembly);
        // base.OnModelCreating runs LAST — applies tenant + soft-delete filters
        base.OnModelCreating(modelBuilder);
    }
}
