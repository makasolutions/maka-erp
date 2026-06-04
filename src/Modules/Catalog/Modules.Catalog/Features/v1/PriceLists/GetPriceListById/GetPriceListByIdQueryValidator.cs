using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.GetPriceListById;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.GetPriceListById;

public sealed class GetPriceListByIdQueryValidator : AbstractValidator<GetPriceListByIdQuery>
{
    public GetPriceListByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
