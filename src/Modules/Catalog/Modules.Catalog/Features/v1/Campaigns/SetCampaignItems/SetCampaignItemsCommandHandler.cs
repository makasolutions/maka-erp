using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.SetCampaignItems;

/// <summary>Replaces the variations a campaign applies to, with their campaign price.</summary>
public sealed class SetCampaignItemsCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<SetCampaignItemsCommand, int>
{
    public async ValueTask<int> Handle(SetCampaignItemsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.PriceLists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == command.CampaignId && p.ListKind == PriceListKind.Campaign, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Campaign {command.CampaignId} not found.");

        db.PriceListItems.RemoveRange(campaign.Items);

        string userId = currentUser.GetUserId().ToString();
        foreach (var input in command.Items.DistinctBy(i => i.VariationId))
            db.PriceListItems.Add(PriceListItem.Create(campaign.Id, input.VariationId, input.Price, userId));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return command.Items.DistinctBy(i => i.VariationId).Count();
    }
}
