using FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;

public sealed class CreatePriceListCommandHandler(CatalogDbContext db)
    : ICommandHandler<CreatePriceListCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePriceListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string segment = command.CustomerSegment.Trim().ToLowerInvariant();
        DateTime validFrom = command.ValidFrom ?? DateTime.UtcNow;

        // Only one active list (ValidTo IS NULL) per segment — close the current one first (spec §7).
        var currentActive = await db.PriceLists
            .Where(p => p.CustomerSegment == segment && p.ValidTo == null && p.OwnerId == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var existing in currentActive)
            existing.Close(validFrom);

        var priceList = PriceList.Create(
            command.Name,
            segment,
            validFrom,
            command.Description);

        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return priceList.Id;
    }
}
