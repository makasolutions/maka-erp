using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Parties.Data;

public sealed class PartiesDbContext : BaseDbContext
{
    public const string Schema = "parties";

    public PartiesDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<PartiesDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Party>           Parties   => Set<Party>();
    public DbSet<PartyAddress>    Addresses => Set<PartyAddress>();
    public DbSet<PartyContact>    Contacts  => Set<PartyContact>();
    public DbSet<PartyChannel>    Channels  => Set<PartyChannel>();
    public DbSet<PartyTeamMember> TeamMembers => Set<PartyTeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PartiesDbContext).Assembly);
        base.OnModelCreating(modelBuilder); // LAST — tenant + soft-delete filters

        // Unique identification per tenant (shadow TenantId only exists after base):
        // a same NIT/cédula can live in different tenants, but not duplicate within one.
        modelBuilder.Entity<Party>()
            .HasIndex("TenantId", nameof(Party.IdentificationTypeCode), nameof(Party.IdentificationNumber))
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");
    }
}
