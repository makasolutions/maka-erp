using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceLists;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceLists;

public sealed class GetPriceListsQueryValidator : AbstractValidator<GetPriceListsQuery>
{
    public GetPriceListsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .When(x => x.PageSize.HasValue);
    }
}
