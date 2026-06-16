using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2;
using FSH.Modules.Parties.Domain.V2.Credit;
using FSH.Modules.Parties.Domain.V2.Profiles;
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

    // --- v1 (productivo, NO se toca) ---
    public DbSet<Party>           Parties   => Set<Party>();
    public DbSet<PartyAddress>    Addresses => Set<PartyAddress>();
    public DbSet<PartyContact>    Contacts  => Set<PartyContact>();
    public DbSet<PartyChannel>    Channels  => Set<PartyChannel>();
    public DbSet<PartyTeamMember> TeamMembers => Set<PartyTeamMember>();

    // --- v2 (PR-B, tablas nuevas ADITIVAS — coexisten con v1, sin nav desde Party hasta PR-D) ---
    public DbSet<CustomerProfile>   CustomerProfiles   => Set<CustomerProfile>();
    public DbSet<SupplierProfile>   SupplierProfiles   => Set<SupplierProfile>();
    public DbSet<ContactProfile>    ContactProfiles    => Set<ContactProfile>();
    public DbSet<PartnerProfile>    PartnerProfiles    => Set<PartnerProfile>();
    public DbSet<EmployeeProfile>   EmployeeProfiles   => Set<EmployeeProfile>();
    public DbSet<CreditAccount>     CreditAccounts     => Set<CreditAccount>();
    public DbSet<CreditMovement>    CreditMovements    => Set<CreditMovement>();
    public DbSet<PartyHold>         PartyHolds         => Set<PartyHold>();
    public DbSet<PartyCiiuActivity> PartyCiiuActivities => Set<PartyCiiuActivity>();

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
