using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Prices.GetEffectivePrice;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetEffectivePrice;

public sealed class GetEffectivePriceQueryValidator : AbstractValidator<GetEffectivePriceQuery>
{
    public GetEffectivePriceQueryValidator()
    {
        RuleFor(x => x.VariationId).NotEmpty();
    }
}
