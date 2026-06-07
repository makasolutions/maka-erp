using System.Net;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.AddPriceListItem;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.AddPriceListItem;

public sealed class AddPriceListItemCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<AddPriceListItemCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddPriceListItemCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var list = await db.PriceLists
            .AsNoTracking()
            .Where(p => p.Id == command.PriceListId)
            .Select(p => new { p.IsDefault })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (list is null)
            throw new NotFoundException($"Price list {command.PriceListId} not found.");

        bool variationExists = await db.Variations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .AnyAsync(v => v.Id == command.VariationId, cancellationToken)
            .ConfigureAwait(false);

        if (!variationExists)
            throw new NotFoundException($"Variation {command.VariationId} not found.");

        bool duplicate = await db.PriceListItems
            .AsNoTracking()
            .AnyAsync(i => i.PriceListId == command.PriceListId && i.VariationId == command.VariationId, cancellationToken)
            .ConfigureAwait(false);

        if (duplicate)
            throw new CustomException(
                "La variación ya tiene un precio en esta lista. Usa actualizar en su lugar.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        var item = PriceListItem.Create(
            command.PriceListId,
            command.VariationId,
            command.Price,
            currentUser.GetUserId().ToString(),
            command.MinQuantity,
            command.SalePrice,
            command.SalePriceFrom,
            command.SalePriceTo);

        db.PriceListItems.Add(item);

        // Setting the base price (default list) cascades to derived lists.
        if (list.IsDefault)
            await PriceRecalculator.RecalculateDerivedAsync(
                db, currentUser.GetUserId().ToString(), command.VariationId, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return item.Id;
    }
}
