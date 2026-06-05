using FSH.Modules.Catalog.Contracts.v1.TaxRates.CreateTaxRate;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.CreateTaxRate;

public sealed class CreateTaxRateCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreateTaxRateCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateTaxRateCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Only one default tax rate at a time (§2.3 seed: IVA 19% is default).
        if (command.IsDefault)
        {
            await db.TaxRates
                .Where(t => t.IsDefault)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsDefault, false), cancellationToken)
                .ConfigureAwait(false);
        }

        var tax = TaxRate.Create(command.Name, command.Rate, command.Description, command.IsDefault);
        db.TaxRates.Add(tax);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return tax.Id;
    }
}
