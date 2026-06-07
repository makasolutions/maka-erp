using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Campaigns;

namespace FSH.Modules.Catalog.Features.v1.Campaigns.CreateCampaign;

public sealed class CreateCampaignCommandValidator : AbstractValidator<CreateCampaignCommand>
{
    public CreateCampaignCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description is not null);
        RuleFor(x => x.ValidTo).GreaterThan(x => x.ValidFrom);
    }
}
