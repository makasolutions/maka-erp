using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FSH.Modules.Catalog.Data;

public sealed class CatalogDbContext : BaseDbContext
{
    public const string Schema = "catalog";

    public CatalogDbContext(
        IMultiTenantContextAccessor<AppTenantInfo> multiTenantContextAccessor,
        DbContextOptions<CatalogDbContext> options,
        IOptions<DatabaseOptions> settings,
        IHostEnvironment environment) : base(multiTenantContextAccessor, options, settings, environment) { }

    public DbSet<Brand>            Brands           => Set<Brand>();
    public DbSet<Category>         Categories       => Set<Category>();
    public DbSet<TaxRate>          TaxRates         => Set<TaxRate>();
    public DbSet<ShippingClass>    ShippingClasses  => Set<ShippingClass>();
    public DbSet<CatalogAttribute> Attributes       => Set<CatalogAttribute>();
    public DbSet<CategoryAttribute> CategoryAttributes => Set<CategoryAttribute>();
    public DbSet<Product>          Products         => Set<Product>();
    public DbSet<ProductVariation> Variations       => Set<ProductVariation>();
    public DbSet<ProductCode>      ProductCodes     => Set<ProductCode>();
    public DbSet<PriceList>        PriceLists       => Set<PriceList>();
    public DbSet<PriceListItem>    PriceListItems   => Set<PriceListItem>();
    public DbSet<PriceBulkProposal> PriceBulkProposals => Set<PriceBulkProposal>();
    public DbSet<TenantProduct>    TenantProducts   => Set<TenantProduct>();
    public DbSet<SupplierBrand>    SupplierBrands   => Set<SupplierBrand>();
    public DbSet<SupplierProduct>  SupplierProducts => Set<SupplierProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        // base.OnModelCreating runs LAST — rule from database.md
        base.OnModelCreating(modelBuilder);
    }
}
