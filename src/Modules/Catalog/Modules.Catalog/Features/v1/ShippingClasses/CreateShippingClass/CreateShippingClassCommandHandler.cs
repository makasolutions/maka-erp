using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.CreateShippingClass;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.CreateShippingClass;

public sealed class CreateShippingClassCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateShippingClassCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateShippingClassCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var shippingClass = ShippingClass.Create(command.Name, command.Description);
        db.ShippingClasses.Add(shippingClass);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return shippingClass.Id;
    }
}
