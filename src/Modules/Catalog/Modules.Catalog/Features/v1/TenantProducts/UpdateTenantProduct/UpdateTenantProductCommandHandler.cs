using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.UpdateTenantProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.UpdateTenantProduct;

public sealed class UpdateTenantProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateTenantProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateTenantProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tenantProduct = await db.Set<TenantProduct>()
            .Where(tp => tp.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"TenantProduct {command.Id} not found.");

        tenantProduct.UpdateOverrides(
            command.NameOverride,
            command.ShortDescriptionOverride,
            command.DescriptionOverride,
            command.TechnicalSpecsOverride,
            command.SeoTitleOverride,
            command.SeoDescriptionOverride,
            command.DropshippingPrice,
            command.DropshippingMinQty,
            command.IsActive,
            command.IsPublic);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tenantProduct.Id;
    }
}
