using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.GetPriceProposals;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.GetPriceProposals;

public sealed class GetPriceProposalsQueryValidator : AbstractValidator<GetPriceProposalsQuery>
{
    public GetPriceProposalsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}
