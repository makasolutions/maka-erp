using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Brands.UpdateBrand;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Brands.UpdateBrand;

public sealed class UpdateBrandCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateBrandCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var brand = await db.Brands
            .Where(b => !b.IsDeleted && b.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Brand {command.Id} not found.");

        string slug = CreateBrandCommandHandler.BuildSlug(command.Slug, command.Name);

        bool slugExists = await db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.Slug == slug && b.Id != command.Id)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (slugExists)
            throw new CustomException("El slug ya existe.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        brand.Update(
            command.Name,
            slug,
            command.Description,
            command.LogoUrl,
            command.WebsiteUrl,
            command.CountryOfOrigin,
            command.IsActive);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return brand.Id;
    }
}
