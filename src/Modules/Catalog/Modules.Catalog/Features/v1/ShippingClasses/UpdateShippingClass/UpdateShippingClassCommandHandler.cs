using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.UpdateShippingClass;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.UpdateShippingClass;

public sealed class UpdateShippingClassCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateShippingClassCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateShippingClassCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingClass = await db.ShippingClasses
            .Where(s => s.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"ShippingClass {command.Id} not found.");

        shippingClass.Update(command.Name, command.Description);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return shippingClass.Id;
    }
}
