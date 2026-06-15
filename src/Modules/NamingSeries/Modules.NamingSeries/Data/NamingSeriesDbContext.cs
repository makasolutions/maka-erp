using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.NamingSeries.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.NamingSeries.Data;

public sealed class NamingSeriesDbContext : BaseDbContext
{
    public const string Schema = "naming";

    public NamingSeriesDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<NamingSeriesDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<FSH.Modules.NamingSeries.Domain.NamingSeries> Series => Set<FSH.Modules.NamingSeries.Domain.NamingSeries>();
    public DbSet<NamingSeriesAllocation> Allocations => Set<NamingSeriesAllocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NamingSeriesDbContext).Assembly);
        // base.OnModelCreating runs LAST — applies tenant + soft-delete filters
        base.OnModelCreating(modelBuilder);
    }
}
