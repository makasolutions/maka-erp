using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceList;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceList;

public sealed class UpdatePriceListCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdatePriceListCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePriceListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var list = await db.PriceLists
            .Where(p => p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Price list {command.Id} not found.");

        // Default toggle — only one default per owner.
        if (command.IsDefault && !list.IsDefault)
        {
            var others = await db.PriceLists
                .Where(p => p.IsDefault && p.OwnerId == null && p.Id != command.Id)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var d in others) d.ClearDefault();
            list.MakeDefault();
        }
        else if (!command.IsDefault && list.IsDefault)
        {
            list.ClearDefault();
        }

        list.Update(
            command.Name,
            command.Description,
            command.ValidFrom,
            command.ValidTo,
            command.IsActive,
            command.AdjustmentPercent,
            command.RoundEnabled);

        // If this is a derived list, recompute its non-override items (the % or the
        // rounding toggle may have changed).
        if (!list.IsDefault && command.AdjustmentPercent is { } pct)
            await PriceRecalculator.RecalculateListAsync(
                db, currentUser.GetUserId().ToString(), list.Id, pct, command.RoundEnabled, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return list.Id;
    }
}
