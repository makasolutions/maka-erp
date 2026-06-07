using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.SetCampaignItems;

public sealed class SetCampaignItemsCommandValidator : AbstractValidator<SetCampaignItemsCommand>
{
    public SetCampaignItemsCommandValidator()
    {
        RuleFor(x => x.CampaignId).NotEmpty();
        RuleFor(x => x.Items).NotNull();
        RuleForEach(x => x.Items).ChildRules(i =>
        {
            i.RuleFor(x => x.VariationId).NotEmpty();
            i.RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        });
    }
}
