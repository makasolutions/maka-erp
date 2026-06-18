using FSH.Framework.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.SharedRecords.Data;

public sealed class SharedRecordsDbInitializer(
    SharedRecordsDbContext dbContext,
    ILogger<SharedRecordsDbInitializer> logger) : IDbInitializer
{
    public async Task MigrateAsync(CancellationToken cancellationToken)
    {
        if ((await dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).Any())
        {
            await dbContext.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("[SharedRecords] applied migrations");
        }
    }

    // Sin seed: los controles genéricos no siembran datos (la clasificación vive en Lookups/AddressLabel).
    public Task SeedAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
