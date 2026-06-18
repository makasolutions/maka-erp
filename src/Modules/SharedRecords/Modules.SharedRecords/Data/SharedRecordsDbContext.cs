using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.SharedRecords.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.SharedRecords.Data;

public sealed class SharedRecordsDbContext : BaseDbContext
{
    public const string Schema = "shared";

    public SharedRecordsDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<SharedRecordsDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Phone> Phones => Set<Phone>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SharedRecordsDbContext).Assembly);

        base.OnModelCreating(modelBuilder); // LAST — tenant + soft-delete filters

        // Invariante "una sola principal por owner": índice único PARCIAL por (TenantId, OwnerType,
        // OwnerId) WHERE IsPrimary. Por tenant (shadow TenantId solo existe tras base) → dos owners
        // distintos (o el mismo owner en tenants distintos) tienen cada uno su propia principal.
        // Excluye soft-deleted para que una principal borrada no bloquee una nueva.
        modelBuilder.Entity<Address>()
            .HasIndex("TenantId", nameof(Address.OwnerType), nameof(Address.OwnerId))
            .IsUnique()
            .HasDatabaseName("ux_shared_addresses_primary_per_owner")
            .HasFilter("\"IsPrimary\" = TRUE AND \"IsDeleted\" = FALSE");

        modelBuilder.Entity<Phone>()
            .HasIndex("TenantId", nameof(Phone.OwnerType), nameof(Phone.OwnerId))
            .IsUnique()
            .HasDatabaseName("ux_shared_phones_primary_per_owner")
            .HasFilter("\"IsPrimary\" = TRUE AND \"IsDeleted\" = FALSE");
    }
}
