using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CancelCampaign;

public sealed class CancelCampaignCommandValidator : AbstractValidator<CancelCampaignCommand>
{
    public CancelCampaignCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
    }
}
