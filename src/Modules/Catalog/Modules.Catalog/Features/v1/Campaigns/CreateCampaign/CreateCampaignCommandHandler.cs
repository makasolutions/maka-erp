using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Services;
using Mediator;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CreateCampaign;

public sealed class CreateCampaignCommandHandler(
    CatalogDbContext db,
    IJobService jobs,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<CreateCampaignCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Identifier
            ?? throw new InvalidOperationException("No tenant context for scheduling campaign jobs.");

        var campaign = PriceList.Create(
            command.Name,
            customerSegment: "campaign",
            validFrom: command.ValidFrom,
            description: command.Description,
            listKind: PriceListKind.Campaign,
            validTo: command.ValidTo);

        db.PriceLists.Add(campaign);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Schedule start/end jobs; jobs restore tenant context internally.
        string startJobId = jobs.Schedule<CampaignJob>(
            j => j.RunStartAsync(campaign.Id, tenantId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(command.ValidFrom, DateTimeKind.Utc)));
        string endJobId = jobs.Schedule<CampaignJob>(
            j => j.RunEndAsync(campaign.Id, tenantId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(command.ValidTo, DateTimeKind.Utc)));

        campaign.SetCampaignJobs(startJobId, endJobId);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return campaign.Id;
    }
}
