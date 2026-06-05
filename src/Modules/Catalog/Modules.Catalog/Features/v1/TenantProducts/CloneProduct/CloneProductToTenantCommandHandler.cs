using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.CloneProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.CloneProduct;

public sealed class CloneProductToTenantCommandHandler(CatalogDbContext db)
    : ICommandHandler<CloneProductToTenantCommand, Guid>
{
    public async ValueTask<Guid> Handle(CloneProductToTenantCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool canonicalExists = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.Id == command.CanonicalProductId, cancellationToken)
            .ConfigureAwait(false);

        if (!canonicalExists)
            throw new NotFoundException($"Canonical product {command.CanonicalProductId} not found.");

        // One tenant clone per canonical (the tenant filter is applied automatically).
        bool alreadyCloned = await db.Set<TenantProduct>()
            .AsNoTracking()
            .AnyAsync(tp => tp.CanonicalProductId == command.CanonicalProductId, cancellationToken)
            .ConfigureAwait(false);

        if (alreadyCloned)
            throw new CustomException(
                "Este producto ya fue clonado al catálogo del tenant.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var clone = TenantProduct.Create(command.CanonicalProductId);
        db.Set<TenantProduct>().Add(clone);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return clone.Id;
    }
}
