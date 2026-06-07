using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceListItem;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceListItem;

public sealed class UpdatePriceListItemCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<UpdatePriceListItemCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdatePriceListItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var item = await db.PriceListItems
            .Where(i => i.Id == command.ItemId && i.PriceListId == command.PriceListId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Price list item {command.ItemId} not found in list {command.PriceListId}.");

        var list = await db.PriceLists
            .AsNoTracking()
            .Where(p => p.Id == command.PriceListId)
            .Select(p => new { p.IsDefault })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);

        string userId = currentUser.GetUserId().ToString();

        // ChangePrice records an immutable history entry automatically (spec §7).
        item.ChangePrice(
            command.Price,
            userId,
            command.ChangeReason,
            sourceReference: null,
            newSalePrice: command.SalePrice,
            newSalePriceFrom: command.SalePriceFrom,
            newSalePriceTo: command.SalePriceTo);

        // An explicit edit on a derived list pins the price (excluded from recalc).
        if (!list.IsDefault)
            item.SetManualOverride(true);
        else
            // Changing the base price cascades to derived lists.
            await PriceRecalculator.RecalculateDerivedAsync(db, userId, item.VariationId, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id;
    }
}
