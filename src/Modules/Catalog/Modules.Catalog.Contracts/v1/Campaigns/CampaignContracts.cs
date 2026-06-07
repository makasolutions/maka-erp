using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Campaigns;

/// <summary>Create a campaign (offer) with a validity window. Schedules Hangfire start/end jobs.</summary>
public sealed record CreateCampaignCommand(
    string    Name,
    DateTime  ValidFrom,
    DateTime  ValidTo,
    string?   Description = null) : ICommand<Guid>;

public sealed record CampaignItemInput(Guid VariationId, decimal Price);

/// <summary>Replace the products/variations a campaign applies to, with their campaign price.</summary>
public sealed record SetCampaignItemsCommand(
    Guid CampaignId,
    IReadOnlyList<CampaignItemInput> Items) : ICommand<int>;

/// <summary>Cancel a campaign: delete its scheduled jobs and mark it cancelled.</summary>
public sealed record CancelCampaignCommand(Guid CampaignId) : ICommand<Guid>;
