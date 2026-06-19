using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.Identity.EntityFrameworkCore;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Identity.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Identity.Data;

public class IdentityDbContext : MultiTenantIdentityDbContext<FshUser,
    FshRole,
    string,
    IdentityUserClaim<string>,
    IdentityUserRole<string>,
    IdentityUserLogin<string>,
    FshRoleClaim,
    IdentityUserToken<string>,
    IdentityUserPasskey<string>>
{
    private readonly DatabaseOptions _settings;
    private new AppTenantInfo TenantInfo { get; set; }
    private readonly IHostEnvironment _environment;

    public DbSet<PasswordHistory> PasswordHistories => Set<PasswordHistory>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<Group> Groups => Set<Group>();

    public DbSet<GroupRole> GroupRoles => Set<GroupRole>();

    public DbSet<UserGroup> UserGroups => Set<UserGroup>();

    public DbSet<ImpersonationGrant> ImpersonationGrants => Set<ImpersonationGrant>();

    public DbSet<TenantLocalization> TenantLocalizations => Set<TenantLocalization>();

    public DbSet<TenantAppearance> TenantAppearances => Set<TenantAppearance>();

    public IdentityDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<IdentityDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options)
    {
        ArgumentNullException.ThrowIfNull(multiTenantContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);

        _environment = environment;
        _settings = settings.Value;
        TenantInfo = multiTenantContextAccessor.MultiTenantContext.TenantInfo!;
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityDbContext).Assembly);

        // Default-on tenant isolation — any entity not marked IGlobalEntity gets
        // IsMultiTenant() applied automatically. ImpersonationGrant opts out via
        // IGlobalEntity. ASP.NET Identity tables (Users/Roles/Claims/etc.) are
        // already marked IsMultiTenant in IdentityConfigurations.cs; the auto-apply
        // detects that annotation and skips re-applying.
        builder.ApplyTenantIsolationByDefault();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!string.IsNullOrWhiteSpace(TenantInfo?.ConnectionString))
        {
            optionsBuilder.ConfigureHeroDatabase(
                _settings.Provider,
                TenantInfo.ConnectionString,
                _settings.MigrationsAssembly,
                _environment.IsDevelopment());
        }
    }
}