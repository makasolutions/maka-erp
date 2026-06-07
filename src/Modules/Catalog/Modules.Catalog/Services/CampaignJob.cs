using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Catalog.Services;

/// <summary>
/// Hangfire jobs that start/end a campaign (Fase 4). Jobs run without HTTP/tenant
/// context, so each restores the Finbuckle tenant in a fresh scope before touching
/// the tenant-filtered <see cref="CatalogDbContext"/>. Both are idempotent.
/// </summary>
public sealed class CampaignJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignJob> _logger;

    public CampaignJob(IServiceScopeFactory scopeFactory, ILogger<CampaignJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>Start: mark Running and snapshot each item's pre-campaign base price.</summary>
    public async Task RunStartAsync(Guid campaignId, string tenantId, CancellationToken cancellationToken)
    {
        var (db, ok) = await ResolveAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!ok || db is null) return;

        var campaign = await db.PriceLists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == campaignId && p.ListKind == PriceListKind.Campaign, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null || campaign.CampaignStatus is not CampaignStatus.Scheduled) return;  // idempotent

        // Snapshot the current default-list base price per variation.
        var variationIds = campaign.Items.Select(i => i.VariationId).ToList();
        var basePrices = await (
            from item in db.PriceListItems.AsNoTracking()
            join list in db.PriceLists.AsNoTracking() on item.PriceListId equals list.Id
            where list.IsDefault && list.OwnerId == null && variationIds.Contains(item.VariationId)
            select new { item.VariationId, item.Price })
            .ToDictionaryAsync(x => x.VariationId, x => x.Price, cancellationToken)
            .ConfigureAwait(false);

        foreach (var item in campaign.Items)
            if (basePrices.TryGetValue(item.VariationId, out var bp))
                item.SnapshotPreCampaign(bp);

        campaign.TransitionCampaign(CampaignStatus.Running);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Campaign {CampaignId} started (tenant {TenantId}).", campaignId, tenantId);
    }

    /// <summary>End: mark Ended — effective price reverts to the segment lists.</summary>
    public async Task RunEndAsync(Guid campaignId, string tenantId, CancellationToken cancellationToken)
    {
        var (db, ok) = await ResolveAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (!ok || db is null) return;

        var campaign = await db.PriceLists
            .FirstOrDefaultAsync(p => p.Id == campaignId && p.ListKind == PriceListKind.Campaign, cancellationToken)
            .ConfigureAwait(false);
        if (campaign is null || campaign.CampaignStatus is not CampaignStatus.Running) return;  // idempotent

        campaign.TransitionCampaign(CampaignStatus.Ended);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Campaign {CampaignId} ended (tenant {TenantId}).", campaignId, tenantId);
    }

    private async Task<(CatalogDbContext? Db, bool Ok)> ResolveAsync(string tenantId, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var scope = _scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await store.GetAsync(tenantId).ConfigureAwait(false);
        if (tenant is null)
        {
            _logger.LogWarning("Campaign job skipped: tenant '{TenantId}' not found.", tenantId);
            scope.Dispose();
            return (null, false);
        }
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);
        return (scope.ServiceProvider.GetRequiredService<CatalogDbContext>(), true);
    }
}
