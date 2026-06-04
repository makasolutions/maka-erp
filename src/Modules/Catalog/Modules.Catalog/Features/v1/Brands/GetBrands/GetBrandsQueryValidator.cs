using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Brands.GetBrands;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrands;

public sealed class GetBrandsQueryValidator : AbstractValidator<GetBrandsQuery>
{
    public GetBrandsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).When(x => x.PageSize.HasValue);
        RuleFor(x => x.Search).MaximumLength(200).When(x => x.Search is not null);
    }
}
