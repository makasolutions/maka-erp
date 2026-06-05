using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.UpdateTaxRate;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.UpdateTaxRate;

public sealed class UpdateTaxRateCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateTaxRateCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateTaxRateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var tax = await db.TaxRates
            .Where(t => t.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"TaxRate {command.Id} not found.");

        if (command.IsDefault)
        {
            await db.TaxRates
                .Where(t => t.IsDefault && t.Id != command.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDefault, false), cancellationToken)
                .ConfigureAwait(false);
        }

        tax.Update(command.Name, command.Rate, command.Description, command.IsDefault, command.IsActive);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tax.Id;
    }
}
