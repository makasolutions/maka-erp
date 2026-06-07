using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Jobs.Services;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Services;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.UpdateCampaign;

/// <summary>
/// Edits a campaign's name/validity and reschedules its start/end jobs at the new
/// dates. Status returns to Scheduled so the (re)scheduled start job applies it.
/// </summary>
public sealed class UpdateCampaignCommandHandler(
    CatalogDbContext db,
    IJobService jobs,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    : ICommandHandler<UpdateCampaignCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateCampaignCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var campaign = await db.PriceLists
            .FirstOrDefaultAsync(p => p.Id == command.Id && p.ListKind == PriceListKind.Campaign, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Campaign {command.Id} not found.");

        if (campaign.CampaignStatus is CampaignStatus.Cancelled)
            throw new CustomException("La campaña está cancelada y no puede editarse.");

        string tenantId = tenantAccessor.MultiTenantContext?.TenantInfo?.Identifier
            ?? throw new InvalidOperationException("No tenant context for rescheduling campaign jobs.");

        campaign.Update(
            command.Name,
            command.Description,
            command.ValidFrom,
            command.ValidTo,
            isActive: true,
            adjustmentPercent: null);

        // Reschedule: delete old jobs, schedule new ones at the new dates.
        if (campaign.StartJobId is { } oldStart) jobs.Delete(oldStart);
        if (campaign.EndJobId is { } oldEnd) jobs.Delete(oldEnd);

        string startJobId = jobs.Schedule<CampaignJob>(
            j => j.RunStartAsync(campaign.Id, tenantId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(command.ValidFrom, DateTimeKind.Utc)));
        string endJobId = jobs.Schedule<CampaignJob>(
            j => j.RunEndAsync(campaign.Id, tenantId, CancellationToken.None),
            new DateTimeOffset(DateTime.SpecifyKind(command.ValidTo, DateTimeKind.Utc)));

        campaign.SetCampaignJobs(startJobId, endJobId);
        campaign.TransitionCampaign(CampaignStatus.Scheduled);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return campaign.Id;
    }
}
