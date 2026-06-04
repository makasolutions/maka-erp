using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands.DeleteBrand;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.DeleteBrand;

public sealed class DeleteBrandCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await db.Brands
            .Where(b => !b.IsDeleted && b.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.Id} not found.");

        bool hasProducts = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.BrandId == command.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (hasProducts)
            throw new CustomException(
                "No se puede eliminar la marca porque tiene productos activos asociados.",
                Enumerable.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);

        db.Brands.Remove(brand);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return brand.Id;
    }
}
