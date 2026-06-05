using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.DeleteTaxRate;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.DeleteTaxRate;

public sealed class DeleteTaxRateCommandHandler(CatalogDbContext db)
    : ICommandHandler<DeleteTaxRateCommand, Guid>
{
    public async ValueTask<Guid> Handle(DeleteTaxRateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tax = await db.TaxRates
            .Where(t => t.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"TaxRate {command.Id} not found.");

        bool inUse = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.TaxRateId == command.Id, cancellationToken)
            .ConfigureAwait(false);

        if (inUse)
            throw new CustomException(
                "No se puede eliminar el impuesto porque está asignado a uno o más productos.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        db.TaxRates.Remove(tax);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tax.Id;
    }
}
