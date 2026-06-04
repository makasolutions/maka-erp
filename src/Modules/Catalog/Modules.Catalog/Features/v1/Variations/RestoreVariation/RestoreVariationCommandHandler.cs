using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Variations.RestoreVariation;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.RestoreVariation;

public sealed class RestoreVariationCommandHandler(CatalogDbContext db)
    : ICommandHandler<RestoreVariationCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestoreVariationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Verify product exists
        bool productExists = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == command.ProductId)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {command.ProductId} not found.");

        var variation = await db.Variations
            .IgnoreQueryFilters()
            .Where(v => v.IsDeleted && v.ProductId == command.ProductId && v.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Variation {command.Id} not found in trash for product {command.ProductId}.");

        variation.Restore();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return variation.Id;
    }
}
