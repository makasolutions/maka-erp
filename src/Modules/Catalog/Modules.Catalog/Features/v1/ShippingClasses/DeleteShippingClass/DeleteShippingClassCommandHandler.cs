using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.DeleteShippingClass;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.DeleteShippingClass;

public sealed class DeleteShippingClassCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteShippingClassCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteShippingClassCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingClass = await db.ShippingClasses
            .Where(s => s.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ShippingClass {command.Id} not found.");

        bool inUse = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.ShippingClassId == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
            throw new CustomException(
                "No se puede eliminar la clase de envío porque está asignada a uno o más productos.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        db.ShippingClasses.Remove(shippingClass);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return shippingClass.Id;
    }
}
