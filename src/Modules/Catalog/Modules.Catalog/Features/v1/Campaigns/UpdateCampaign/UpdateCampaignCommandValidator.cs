using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.UpdateCampaign;

public sealed class UpdateCampaignCommandValidator : AbstractValidator<UpdateCampaignCommand>
{
    public UpdateCampaignCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description is not null);
        RuleFor(x => x.ValidTo).GreaterThan(x => x.ValidFrom);
    }
}
