using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands.RestoreBrand;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.RestoreBrand;

public sealed class RestoreBrandCommandHandler(CatalogDbContext db)
    : ICommandHandler<RestoreBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await db.Brands
            .IgnoreQueryFilters()
            .Where(b => b.IsDeleted && b.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.Id} not found in trash.");

        brand.Restore();

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return brand.Id;
    }
}
