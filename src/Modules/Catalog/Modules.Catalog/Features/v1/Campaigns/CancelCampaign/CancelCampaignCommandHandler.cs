using FSH.Framework.Core.Exceptions;
using FSH.Framework.Jobs.Services;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CancelCampaign;

public sealed class CancelCampaignCommandHandler(CatalogDbContext db, IJobService jobs)
    : ICommandHandler<CancelCampaignCommand, Guid>
{
    public async ValueTask<Guid> Handle(CancelCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.PriceLists
            .FirstOrDefaultAsync(p => p.Id == command.CampaignId && p.ListKind == PriceListKind.Campaign, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Campaign {command.CampaignId} not found.");

        if (campaign.StartJobId is { } s) jobs.Delete(s);
        if (campaign.EndJobId is { } e) jobs.Delete(e);

        campaign.TransitionCampaign(CampaignStatus.Cancelled);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return campaign.Id;
    }
}
