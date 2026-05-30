using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Id == command.BrandId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.BrandId} not found.");

        brand.Update(
            command.Code,
            command.Name,
            command.Description,
            command.LogoUrl,
            command.IsActive,
            command.IsVisible);

        bool slugTaken = await dbContext.Brands
            .AnyAsync(b => b.Slug == brand.Slug && b.Id != brand.Id, cancellationToken)
            .ConfigureAwait(false);
        if (slugTaken)
        {
            throw new CustomException(
                $"Another brand with name '{command.Name}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        bool codeTaken = await dbContext.Brands
            .AnyAsync(b => b.Code == brand.Code && b.Id != brand.Id, cancellationToken)
            .ConfigureAwait(false);
        if (codeTaken)
        {
            throw new CustomException(
                $"Another brand with code '{brand.Code}' already exists.",
                (IEnumerable<string>?)null,
                HttpStatusCode.Conflict);
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return brand.Id;
    }
}
