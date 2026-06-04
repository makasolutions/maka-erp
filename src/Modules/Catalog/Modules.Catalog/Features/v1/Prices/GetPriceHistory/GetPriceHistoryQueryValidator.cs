using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Prices.GetPriceHistory;

namespace FSH.Modules.Catalog.Features.v1.Prices.GetPriceHistory;

public sealed class GetPriceHistoryQueryValidator : AbstractValidator<GetPriceHistoryQuery>
{
    public GetPriceHistoryQueryValidator()
    {
        RuleFor(x => x.VariationId).NotEmpty();
    }
}
