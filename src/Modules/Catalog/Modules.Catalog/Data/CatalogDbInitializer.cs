using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Persistence;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Data;

public sealed class CatalogDbInitializer(
    CatalogDbContext dbContext,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<CatalogDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[Catalog] applied migrations");
        }
    }

    /// <summary>
    /// Seeds configuration data required by every tenant: TaxRates + ShippingClasses.
    /// Idempotent — checks before inserting.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await SeedTaxRatesAsync(cancellationToken).ConfigureAwait(false);
        await SeedShippingClassesAsync(cancellationToken).ConfigureAwait(false);

        // Marketplace catalog (Google taxonomy + brands + industries) only in `global`.
        var tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.Equals(tenantId, MultitenancyConstants.Global.Id, StringComparison.Ordinal))
        {
            await new GlobalCatalogSeeder(dbContext, logger).SeedAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SeedTaxRatesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.TaxRates.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        dbContext.TaxRates.AddRange(
            TaxRate.Create("IVA 19%", 0.19m, "Tarifa general IVA Colombia", isDefault: true),
            TaxRate.Create("IVA 5%", 0.05m, "Tarifa reducida IVA Colombia"),
            TaxRate.Create("Exento", 0.00m, "Bienes y servicios exentos de IVA"));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Catalog] seeded TaxRates (IVA 19%, IVA 5%, Exento)");
    }

    private async Task SeedShippingClassesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ShippingClasses.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        dbContext.ShippingClasses.AddRange(
            ShippingClass.Create("Normal", "Productos estándar sin restricción de envío"),
            ShippingClass.Create("Frágil", "Requiere embalaje especial y manejo cuidadoso"),
            ShippingClass.Create("Sobredimensionado", "Supera 50cm en alguna dimensión o más de 25kg"));

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("[Catalog] seeded ShippingClasses (Normal, Frágil, Sobredimensionado)");
    }
}
