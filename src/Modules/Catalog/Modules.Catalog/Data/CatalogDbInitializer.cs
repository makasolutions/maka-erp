using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Data;

public sealed class CatalogDbInitializer(
    CatalogDbContext dbContext,
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
    /// Catalog seed is handled by CatalogDbSeeder (Fase C1 Paso 5).
    /// A fresh tenant comes up with an empty catalog until the seeder runs.
    /// </summary>
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
